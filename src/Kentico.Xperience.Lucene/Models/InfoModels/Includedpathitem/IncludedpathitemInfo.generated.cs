using System;
using System.Data;
using System.Runtime.Serialization;
using System.Collections.Generic;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using CMS;

[assembly: RegisterObjectType(typeof(IncludedpathitemInfo), IncludedpathitemInfo.OBJECT_TYPE)]

namespace CMS
{
    /// <summary>
    /// Data container class for <see cref="IncludedpathitemInfo"/>.
    /// </summary>
    [Serializable]
    public partial class IncludedpathitemInfo : AbstractInfo<IncludedpathitemInfo, IIncludedpathitemInfoProvider>
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "lucene.includedpathitem";


        /// <summary>
        /// Type information.
        /// </summary>
#warning "You will need to configure the type info."
        public static readonly ObjectTypeInfo TYPEINFO = new ObjectTypeInfo(typeof(IncludedpathitemInfoProvider), OBJECT_TYPE, "lucene.includedpathitem", "LuceneIncludedPathItemId", null, null, null, null, null, null, null)
        {
            TouchCacheDependencies = true,
            DependsOn = new List<ObjectDependency>()
            {
                new ObjectDependency("LuceneIndexItemId", "IndexitemInfo", ObjectDependencyEnum.Required),
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
        protected IncludedpathitemInfo(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }


        /// <summary>
        /// Creates an empty instance of the <see cref="IncludedpathitemInfo"/> class.
        /// </summary>
        public IncludedpathitemInfo()
            : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instances of the <see cref="IncludedpathitemInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public IncludedpathitemInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}