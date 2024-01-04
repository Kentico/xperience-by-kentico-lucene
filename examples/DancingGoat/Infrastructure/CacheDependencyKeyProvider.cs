using System;

using CMS.DocumentEngine;
using CMS.Helpers;

namespace DancingGoat.Infrastructure
{
    /// <summary>
    /// Provides methods to format cache dependencies.
    /// </summary>
    public static class CacheDependencyKeyProvider
    {
        /// <summary>
        /// Gets dependency cache key for all pages of given page type.
        /// </summary>
        /// <param name="siteName">Site name.</param>
        /// <param name="className">Class name representing a page type.</param>
        /// <remarks>If class name not provided, dependency key for all pages is returned.</remarks>
        public static string GetDependencyCacheKeyForPageType(string siteName, string className)
        {
            if (string.IsNullOrEmpty(className))
            {
                return DocumentDependencyCacheKeysBuilder.GetChildNodesDependencyCacheKey(siteName, "/");
            }

            return DocumentDependencyCacheKeysBuilder.GetAllNodesCacheKey(siteName, className);
        }


        /// <summary>
        /// Gets dependency cache key for given object type.
        /// </summary>
        /// <param name="objectType">Object type.</param>
        public static string GetDependencyCacheKeyForObjectType(string objectType)
        {
            if (string.IsNullOrEmpty(objectType))
            {
                throw new NotSupportedException("The object type needs to be provided.");
            }

            return CacheHelper.GetCacheItemName(null, objectType, "all");
        }
    }
}