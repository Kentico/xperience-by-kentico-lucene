using System;
using System.Data;
using System.Runtime.Serialization;
using System.Collections.Generic;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using Kentico.Xperience.Lucene.Models;

[assembly: RegisterObjectType(typeof(LuceneIncludedPathItemInfo), LuceneIncludedPathItemInfo.OBJECT_TYPE)]

namespace Kentico.Xperience.Lucene.Models
{
    /// <summary>
    /// Data container class for <see cref="LuceneIncludedPathItemInfo"/>.
    /// </summary>
    [Serializable]
    public partial class LuceneIncludedPathItemInfo : AbstractInfo<LuceneIncludedPathItemInfo, ILuceneIncludedPathItemInfoProvider>
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "lucene.luceneincludedpathitem";


        /// <summary>
        /// Type information.
        /// </summary>
        public static readonly ObjectTypeInfo TYPEINFO = new(typeof(LuceneIncludedPathItemInfoProvider), OBJECT_TYPE, "lucene.luceneincludedpathitem", "LuceneIncludedPathItemId", null, null, null, null, null, null, null)
        {
            TouchCacheDependencies = true,
            DependsOn = new List<ObjectDependency>()
            {
                new("LuceneIncludedPathIndexItemId", "LuceneIndexItemInfo", ObjectDependencyEnum.Required),
            },
        };


        /// <summary>
        /// Lucene included path item id.
        /// </summary>
        [DatabaseField]
        public virtual int LuceneIncludedPathItemId
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(LuceneIncludedPathItemId)), 0);
            set => SetValue(nameof(LuceneIncludedPathItemId), value);
        }


        /// <summary>
        /// Alias path.
        /// </summary>
        [DatabaseField]
        public virtual string LuceneIncludedPathAliasPath
        {
            get => ValidationHelper.GetString(GetValue(nameof(LuceneIncludedPathAliasPath)), String.Empty);
            set => SetValue(nameof(LuceneIncludedPathAliasPath), value);
        }


        /// <summary>
        /// Lucene index item id.
        /// </summary>
        [DatabaseField]
        public virtual int LuceneIncludedPathIndexItemId
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(LuceneIncludedPathIndexItemId)), 0);
            set => SetValue(nameof(LuceneIncludedPathIndexItemId), value);
        }


        /// <summary>
        /// Deletes the object using appropriate provider.
        /// </summary>
        protected override void DeleteObject()
        {
            Provider.Delete(this);
        }


        /// <summary>
        /// Updates the object using appropriate provider.
        /// </summary>
        protected override void SetObject()
        {
            Provider.Set(this);
        }


        /// <summary>
        /// Constructor for de-serialization.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Streaming context.</param>
        protected LuceneIncludedPathItemInfo(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }


        /// <summary>
        /// Creates an empty instance of the <see cref="LuceneIncludedPathItemInfo"/> class.
        /// </summary>
        public LuceneIncludedPathItemInfo()
            : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instances of the <see cref="LuceneIncludedPathItemInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public LuceneIncludedPathItemInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}
