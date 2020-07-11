using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core.Caching;
using Nop.Core.Domain.Blogs;
using Nop.Data;
using Nop.Services.Caching;
using Nop.Services.Events;

namespace Nop.Services.Blogs
{
    /// <summary>
    /// Represents the blog comment service implementation
    /// </summary>
    public partial class BlogCommentService : Service<BlogComment>, IBlogCommentService
    {
        #region Fields

        private readonly ICacheKeyService _cacheKeyService;
        private readonly IRepository<BlogComment> _blogCommentRepository;
        private readonly IStaticCacheManager _staticCacheManager;

        #endregion

        #region Ctor

        public BlogCommentService(ICacheKeyService cacheKeyService,
            IEventPublisher eventPublisher,
            IRepository<BlogComment> blogCommentRepository,
            IStaticCacheManager staticCacheManager) : base(eventPublisher, blogCommentRepository, staticCacheManager)
        {
            _cacheKeyService = cacheKeyService;
            _blogCommentRepository = blogCommentRepository;
            _staticCacheManager = staticCacheManager;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets all comments
        /// </summary>
        /// <param name="customerId">Customer identifier; 0 to load all records</param>
        /// <param name="storeId">Store identifier; pass 0 to load all records</param>
        /// <param name="blogPostId">Blog post ID; 0 or null to load all records</param>
        /// <param name="approved">A value indicating whether to content is approved; null to load all records</param> 
        /// <param name="fromUtc">Item creation from; null to load all records</param>
        /// <param name="toUtc">Item creation to; null to load all records</param>
        /// <param name="commentText">Search comment text; null to load all records</param>
        /// <returns>Comments</returns>
        public virtual IList<BlogComment> GetAllComments(int customerId = 0, int storeId = 0, int? blogPostId = null,
            bool? approved = null, DateTime? fromUtc = null, DateTime? toUtc = null, string commentText = null)
        {
            return GetAll(query =>
            {
                if (approved.HasValue)
                    query = query.Where(comment => comment.IsApproved == approved);

                if (blogPostId > 0)
                    query = query.Where(comment => comment.BlogPostId == blogPostId);

                if (customerId > 0)
                    query = query.Where(comment => comment.CustomerId == customerId);

                if (storeId > 0)
                    query = query.Where(comment => comment.StoreId == storeId);

                if (fromUtc.HasValue)
                    query = query.Where(comment => fromUtc.Value <= comment.CreatedOnUtc);

                if (toUtc.HasValue)
                    query = query.Where(comment => toUtc.Value >= comment.CreatedOnUtc);

                if (!string.IsNullOrEmpty(commentText))
                    query = query.Where(c => c.CommentText.Contains(commentText));

                query = query.OrderBy(comment => comment.CreatedOnUtc);

                return query;
            });
        }

        /// <summary>
        /// Get the count of blog comments
        /// </summary>
        /// <param name="blogPost">Blog post</param>
        /// <param name="storeId">Store identifier; pass 0 to load all records</param>
        /// <param name="isApproved">A value indicating whether to count only approved or not approved comments; pass null to get number of all comments</param>
        /// <returns>Number of blog comments</returns>
        public virtual int GetBlogCommentsCount(BlogPost blogPost, int storeId = 0, bool? isApproved = null)
        {
            var query = _blogCommentRepository.Table.Where(comment => comment.BlogPostId == blogPost.Id);

            if (storeId > 0)
                query = query.Where(comment => comment.StoreId == storeId);

            if (isApproved.HasValue)
                query = query.Where(comment => comment.IsApproved == isApproved.Value);

            var cacheKey = _cacheKeyService.PrepareKeyForDefaultCache(NopBlogsDefaults.BlogCommentsNumberCacheKey, blogPost, storeId, isApproved);

            return _staticCacheManager.Get(cacheKey, () => query.Count());
        }

        #endregion
    }
}