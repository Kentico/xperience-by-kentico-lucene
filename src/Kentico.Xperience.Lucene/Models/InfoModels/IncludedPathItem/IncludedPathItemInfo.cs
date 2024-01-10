using System;
using System.Data;
using System.Runtime.Serialization;
using System.Collections.Generic;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using Kentico.Xperience.Lucene.Models;

[assembly: RegisterObjectType(typeof(IncludedPathItemInfo), IncludedPathItemInfo.OBJECT_TYPE)]

namespace Kentico.Xperience.Lucene.Models
{
    /// <summary>
    /// Data container class for <see cref="IncludedPathItemInfo"/>.
    /// </summary>
    [Serializable]
    public partial class IncludedPathItemInfo : AbstractInfo<IncludedPathItemInfo, IIncludedPathItemInfoProvider>
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "lucene.includedpathitem";


        /// <summary>
        /// Type information.
        /// </summary>
        public static readonly ObjectTypeInfo TYPEINFO = new(typeof(IncludedPathItemInfoProvider), OBJECT_TYPE, "lucene.includedpathitem", "LuceneIncludedPathItemId", null, null, null, null, null, null, null)
        {
            TouchCacheDependencies = true,
            DependsOn = new List<ObjectDependency>()
            {
                new("LuceneIndexItemId", "IndexItemInfo", ObjectDependencyEnum.Required),
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
        public virtual string AliasPath
        {
            get => ValidationHelper.GetString(GetValue(nameof(AliasPath)), String.Empty);
            set => SetValue(nameof(AliasPath), value);
        }


        /// <summary>
        /// Lucene index item id.
        /// </summary>
        [DatabaseField]
        public virtual int LuceneIndexItemId
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(LuceneIndexItemId)), 0);
            set => SetValue(nameof(LuceneIndexItemId), value);
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
        protected IncludedPathItemInfo(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }


        /// <summary>
        /// Creates an empty instance of the <see cref="IncludedPathItemInfo"/> class.
        /// </summary>
        public IncludedPathItemInfo()
            : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instances of the <see cref="IncludedPathItemInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public IncludedPathItemInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}
