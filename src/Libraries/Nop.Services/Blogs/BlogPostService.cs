using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Blogs;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Stores;
using Nop.Data;
using Nop.Services.Caching;
using Nop.Services.Events;

namespace Nop.Services.Blogs
{
    /// <summary>
    /// Represents the blog post service implementation
    /// </summary>
    public partial class BlogPostService : Service<BlogPost>, IBlogPostService
    {
        #region Fields

        private readonly CatalogSettings _catalogSettings;
        private readonly ICacheKeyService _cacheKeyService;
        private readonly IRepository<BlogPost> _blogPostRepository;
        private readonly IRepository<StoreMapping> _storeMappingRepository;
        private readonly IStaticCacheManager _staticCacheManager;

        #endregion

        #region Ctor

        public BlogPostService(CatalogSettings catalogSettings,
            ICacheKeyService cacheKeyService,
            IEventPublisher eventPublisher,
            IRepository<BlogPost> blogPostRepository,
            IRepository<StoreMapping> storeMappingRepository,
            IStaticCacheManager staticCacheManager) : base(eventPublisher, blogPostRepository, staticCacheManager)
        {
            _catalogSettings = catalogSettings;
            _cacheKeyService = cacheKeyService;
            _blogPostRepository = blogPostRepository;
            _storeMappingRepository = storeMappingRepository;
            _staticCacheManager = staticCacheManager;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets all blog posts
        /// </summary>
        /// <param name="storeId">The store identifier; pass 0 to load all records</param>
        /// <param name="languageId">Language identifier; 0 if you want to get all records</param>
        /// <param name="dateFrom">Filter by created date; null if you want to get all records</param>
        /// <param name="dateTo">Filter by created date; null if you want to get all records</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <param name="title">Filter by blog post title</param>
        /// <returns>Blog posts</returns>
        public virtual IPagedList<BlogPost> GetAllBlogPosts(int storeId = 0, int languageId = 0,
            DateTime? dateFrom = null, DateTime? dateTo = null,
            int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false, string title = null)
        {
            return GetAllPaged(query =>
            {
                if (dateFrom.HasValue)
                    query = query.Where(b => dateFrom.Value <= (b.StartDateUtc ?? b.CreatedOnUtc));
                if (dateTo.HasValue)
                    query = query.Where(b => dateTo.Value >= (b.StartDateUtc ?? b.CreatedOnUtc));
                if (languageId > 0)
                    query = query.Where(b => languageId == b.LanguageId);
                if (!string.IsNullOrEmpty(title))
                    query = query.Where(b => b.Title.Contains(title));
                if (!showHidden)
                {
                    query = query.Where(b => !b.StartDateUtc.HasValue || b.StartDateUtc <= DateTime.UtcNow);
                    query = query.Where(b => !b.EndDateUtc.HasValue || b.EndDateUtc >= DateTime.UtcNow);
                }

                if (storeId > 0 && !_catalogSettings.IgnoreStoreLimitations)
                {
                    //Store mapping
                    query = from bp in query
                            join sm in _storeMappingRepository.Table
                            on new { c1 = bp.Id, c2 = nameof(BlogPost) } equals new { c1 = sm.EntityId, c2 = sm.EntityName } into bp_sm
                            from sm in bp_sm.DefaultIfEmpty()
                            where !bp.LimitedToStores || storeId == sm.StoreId
                            select bp;

                    query = query.Distinct();
                }

                query = query.OrderByDescending(b => b.StartDateUtc ?? b.CreatedOnUtc);

                return query;
            }, pageIndex, pageSize);
        }

        /// <summary>
        /// Gets all blog posts
        /// </summary>
        /// <param name="storeId">The store identifier; pass 0 to load all records</param>
        /// <param name="languageId">Language identifier. 0 if you want to get all blog posts</param>
        /// <param name="tag">Tag</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Blog posts</returns>
        public virtual IPagedList<BlogPost> GetAllBlogPostsByTag(int storeId = 0,
            int languageId = 0, string tag = "",
            int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false)
        {
            tag = tag.Trim();

            //we load all records and only then filter them by tag
            var blogPostsAll = GetAllBlogPosts(storeId: storeId, languageId: languageId, showHidden: showHidden);
            var taggedBlogPosts = new List<BlogPost>();
            foreach (var blogPost in blogPostsAll)
            {
                var tags = ParseTags(blogPost);
                if (!string.IsNullOrEmpty(tags.FirstOrDefault(t => t.Equals(tag, StringComparison.InvariantCultureIgnoreCase))))
                    taggedBlogPosts.Add(blogPost);
            }

            //server-side paging
            var result = new PagedList<BlogPost>(taggedBlogPosts, pageIndex, pageSize);
            return result;
        }

        /// <summary>
        /// Gets all blog post tags
        /// </summary>
        /// <param name="storeId">The store identifier; pass 0 to load all records</param>
        /// <param name="languageId">Language identifier. 0 if you want to get all blog posts</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Blog post tags</returns>
        public virtual IList<BlogPostTag> GetAllBlogPostTags(int storeId, int languageId, bool showHidden = false)
        {
            var cacheKey = _cacheKeyService.PrepareKeyForDefaultCache(NopBlogsDefaults.BlogTagsModelCacheKey, languageId, storeId, showHidden);

            var blogPostTags = _staticCacheManager.Get(cacheKey, () =>
            {
                var rezBlogPostTags = new List<BlogPostTag>();

                var blogPosts = GetAllBlogPosts(storeId, languageId, showHidden: showHidden);

                foreach (var blogPost in blogPosts)
                {
                    var tags = ParseTags(blogPost);
                    foreach (var tag in tags)
                    {
                        var foundBlogPostTag = rezBlogPostTags.Find(bpt =>
                            bpt.Name.Equals(tag, StringComparison.InvariantCultureIgnoreCase));
                        if (foundBlogPostTag == null)
                        {
                            foundBlogPostTag = new BlogPostTag
                            {
                                Name = tag,
                                BlogPostCount = 1
                            };
                            rezBlogPostTags.Add(foundBlogPostTag);
                        }
                        else
                            foundBlogPostTag.BlogPostCount++;
                    }
                }

                return rezBlogPostTags;
            });

            return blogPostTags;
        }

        /// <summary>
        /// Returns all posts published between the two dates.
        /// </summary>
        /// <param name="blogPosts">Source</param>
        /// <param name="dateFrom">Date from</param>
        /// <param name="dateTo">Date to</param>
        /// <returns>Filtered posts</returns>
        public virtual IList<BlogPost> GetPostsByDate(IList<BlogPost> blogPosts, DateTime dateFrom, DateTime dateTo)
        {
            if (blogPosts == null)
                throw new ArgumentNullException(nameof(blogPosts));

            return blogPosts
                .Where(p => dateFrom.Date <= (p.StartDateUtc ?? p.CreatedOnUtc) && (p.StartDateUtc ?? p.CreatedOnUtc).Date <= dateTo)
                .ToList();
        }

        /// <summary>
        /// Parse tags
        /// </summary>
        /// <param name="blogPost">Blog post</param>
        /// <returns>Tags</returns>
        public virtual IList<string> ParseTags(BlogPost blogPost)
        {
            if (blogPost == null)
                throw new ArgumentNullException(nameof(blogPost));

            if (blogPost.Tags == null)
                return new List<string>();

            var tags = blogPost.Tags.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(tag => tag.Trim())
                .Where(tag => !string.IsNullOrEmpty(tag)).ToList();

            return tags;
        }

        /// <summary>
        /// Get a value indicating whether a blog post is available now (availability dates)
        /// </summary>
        /// <param name="blogPost">Blog post</param>
        /// <param name="dateTime">Datetime to check; pass null to use current date</param>
        /// <returns>Result</returns>
        public virtual bool BlogPostIsAvailable(BlogPost blogPost, DateTime? dateTime = null)
        {
            if (blogPost == null)
                throw new ArgumentNullException(nameof(blogPost));

            if (blogPost.StartDateUtc.HasValue && blogPost.StartDateUtc.Value >= (dateTime ?? DateTime.UtcNow))
                return false;

            if (blogPost.EndDateUtc.HasValue && blogPost.EndDateUtc.Value <= (dateTime ?? DateTime.UtcNow))
                return false;

            return true;
        }

        #endregion
    }
}