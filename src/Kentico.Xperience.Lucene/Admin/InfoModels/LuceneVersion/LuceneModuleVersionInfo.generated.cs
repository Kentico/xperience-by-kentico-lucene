using System.Data;
using System.Runtime.Serialization;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using Kentico.Xperience.Lucene.Admin;

[assembly: RegisterObjectType(typeof(LuceneModuleVersionInfo), LuceneModuleVersionInfo.OBJECT_TYPE)]

namespace Kentico.Xperience.Lucene.Admin
{
    /// <summary>
    /// Data container class for <see cref="LuceneModuleVersionInfo"/>.
    /// </summary>
    [Serializable]
    public partial class LuceneModuleVersionInfo : AbstractInfo<LuceneModuleVersionInfo, ILuceneModuleVersionInfoProvider>
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "kenticolucene.lucenemoduleversion";


        /// <summary>
        /// Type information.
        /// </summary>
        public static readonly ObjectTypeInfo TYPEINFO = new(typeof(LuceneModuleVersionInfoProvider), OBJECT_TYPE, "KenticoLucene.LuceneModuleVersion", nameof(LuceneModuleVersionId), nameof(LuceneModuleVersionLastModified), nameof(LuceneModuleVersionGuid), null, null, null, null, null)
        {
            TouchCacheDependencies = true,
            ContinuousIntegrationSettings =
            {
                Enabled = true,
            },
        };


        /// <summary>
        /// Lucene module version id.
        /// </summary>
        [DatabaseField]
        public virtual int LuceneModuleVersionId
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(LuceneModuleVersionId)), 0);
            set => SetValue(nameof(LuceneModuleVersionId), value);
        }

        /// <summary>
        /// Lucene module version last modified.
        /// </summary>
        [DatabaseField]
        public virtual DateTime LuceneModuleVersionLastModified
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(LuceneModuleVersionLastModified)), default);
            set => SetValue(nameof(LuceneModuleVersionLastModified), value);
        }


        /// <summary>
        /// Lucene module version Guid.
        /// </summary>
        [DatabaseField]
        public virtual Guid LuceneModuleVersionGuid
        {
            get => ValidationHelper.GetGuid(GetValue(nameof(LuceneModuleVersionGuid)), default);
            set => SetValue(nameof(LuceneModuleVersionGuid), value);
        }


        /// <summary>
        /// Rebuild hook.
        /// </summary>
        [DatabaseField]
        public virtual string LuceneModuleVersionNumber
        {
            get => ValidationHelper.GetString(GetValue(nameof(LuceneModuleVersionNumber)), String.Empty);
            set => SetValue(nameof(LuceneModuleVersionNumber), value, String.Empty);
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
        protected LuceneModuleVersionInfo(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }


        /// <summary>
        /// Creates an empty instance of the <see cref="LuceneModuleVersionInfo"/> class.
        /// </summary>
        public LuceneModuleVersionInfo()
            : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instances of the <see cref="LuceneModuleVersionInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public LuceneModuleVersionInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}
