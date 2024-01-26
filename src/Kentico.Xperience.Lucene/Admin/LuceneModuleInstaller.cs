using System.Reflection;
using CMS.Core;
using CMS.DataEngine;
using CMS.FormEngine;
using CMS.Modules;

namespace Kentico.Xperience.Lucene.Admin;

internal class LuceneModuleInstaller
{
    private readonly IResourceInfoProvider resourceProvider;
    private readonly ILuceneModuleVersionInfoProvider moduleVersionProvider;
    private readonly IConversionService conversion;

    public LuceneModuleInstaller(IResourceInfoProvider resourceProvider, ILuceneModuleVersionInfoProvider moduleVersionProvider, IConversionService conversion)
    {
        this.resourceProvider = resourceProvider;
        this.moduleVersionProvider = moduleVersionProvider;
        this.conversion = conversion;
    }

    public void Install()
    {
        ValidateInstalledVersion();

        var resource = InstallResource();

        InstallLuceneModuleVersionInfo(resource);
        InstallLuceneItemInfo(resource);
        InstallLuceneLanguageInfo(resource);
        InstallLuceneIndexPathItemInfo(resource);
        InstallLuceneContentTypeItemInfo(resource);
        SetInitialVersion(moduleVersionProvider);
    }

    private void ValidateInstalledVersion()
    {
        string queryText = $"""
            IF OBJECT_ID('KenticoLucene_LuceneModuleVersion', 'U') IS NOT NULL
                SELECT TOP 1 LuceneModuleVersionNumber FROM KenticoLucene_LuceneModuleVersion;
            ELSE
                SELECT '' as LuceneModuleVersionNumber;
            """;
        var ds = ConnectionHelper.ExecuteQuery(queryText, [], QueryTypeEnum.SQLQuery);

        string databaseVersion = conversion.GetString(ds.Tables[0].Rows[0][0], "");

        // Not yet installed
        if (string.IsNullOrEmpty(databaseVersion))
        {
            return;
        }

        string assemblyVersion = moduleVersionProvider.GetAssemblyVersionNumber();

        if (string.Equals(databaseVersion, assemblyVersion))
        {
            return;
        }

        string errorMessage = $"""
            The {Assembly.GetExecutingAssembly().GetName()} integration does not match the installed version.
            Package version - {assemblyVersion}
            Installed version - {databaseVersion}

            You must first run "dotnet run --kxp-update" to update this integration from the package.
            """;

        throw new InvalidOperationException(errorMessage);
    }

    private ResourceInfo InstallResource()
    {
        // Temporary way to handle previous versions until the module has been renamed
        var resourceInfo = resourceProvider.Get("CMS.Integration.Lucene") ?? new ResourceInfo();

        resourceInfo.ResourceDisplayName = "Kentico Integration - Lucene";
        resourceInfo.ResourceName = "CMS.Integration.Lucene";
        resourceInfo.ResourceDescription = "Kentico Lucene custom data";
        resourceInfo.ResourceIsInDevelopment = false;
        if (resourceInfo.HasChanged)
        {
            resourceProvider.Set(resourceInfo);
        }

        return resourceInfo;
    }

    private static void InstallLuceneModuleVersionInfo(ResourceInfo resource)
    {
        var luceneModuleVersionInfo = DataClassInfoProvider.GetDataClassInfo(LuceneModuleVersionInfo.OBJECT_TYPE);

        if (luceneModuleVersionInfo is not null)
        {
            return;
        }

        luceneModuleVersionInfo = DataClassInfo.New(LuceneModuleVersionInfo.OBJECT_TYPE);

        luceneModuleVersionInfo.ClassName = LuceneModuleVersionInfo.TYPEINFO.ObjectClassName;
        luceneModuleVersionInfo.ClassTableName = LuceneModuleVersionInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        luceneModuleVersionInfo.ClassDisplayName = "Lucene Module Version";
        luceneModuleVersionInfo.ClassType = ClassType.OTHER;
        luceneModuleVersionInfo.ClassResourceID = resource.ResourceID;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(LuceneModuleVersionInfo.LuceneModuleVersionId));

        var formItem = new FormFieldInfo
        {
            Name = nameof(LuceneModuleVersionInfo.LuceneModuleVersionGuid),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = FieldDataType.DateTime,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(LuceneModuleVersionInfo.LuceneModuleVersionLastModified),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = FieldDataType.Guid,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(LuceneModuleVersionInfo.LuceneModuleVersionNumber),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = FieldDataType.Text,
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        luceneModuleVersionInfo.ClassFormDefinition = formInfo.GetXmlDefinition();

        DataClassInfoProvider.SetDataClassInfo(luceneModuleVersionInfo);
    }

    private static void InstallLuceneItemInfo(ResourceInfo resource)
    {
        var luceneItemInfo = DataClassInfoProvider.GetDataClassInfo(LuceneIndexItemInfo.OBJECT_TYPE);
        if (luceneItemInfo is not null)
        {
            return;
        }

        luceneItemInfo = DataClassInfo.New(LuceneIndexItemInfo.OBJECT_TYPE);

        luceneItemInfo.ClassName = LuceneIndexItemInfo.TYPEINFO.ObjectClassName;
        luceneItemInfo.ClassTableName = LuceneIndexItemInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        luceneItemInfo.ClassDisplayName = "Lucene Index Item";
        luceneItemInfo.ClassType = ClassType.OTHER;
        luceneItemInfo.ClassResourceID = resource.ResourceID;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(LuceneIndexItemInfo.LuceneIndexItemId));

        var formItem = new FormFieldInfo
        {
            Name = nameof(LuceneIndexItemInfo.LuceneIndexItemGuid),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = "guid",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(LuceneIndexItemInfo.LuceneIndexItemIndexName),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            Size = 100,
            DataType = "text",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(LuceneIndexItemInfo.LuceneIndexItemChannelName),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            Size = 100,
            DataType = "text",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(LuceneIndexItemInfo.LuceneIndexItemStrategyName),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            Size = 100,
            DataType = "text",
            Enabled = true
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(LuceneIndexItemInfo.LuceneIndexItemRebuildHook),
            AllowEmpty = true,
            Visible = true,
            Precision = 0,
            Size = 100,
            DataType = "text",
            Enabled = true
        };

        formInfo.AddFormItem(formItem);

        luceneItemInfo.ClassFormDefinition = formInfo.GetXmlDefinition();

        DataClassInfoProvider.SetDataClassInfo(luceneItemInfo);
    }

    private static void InstallLuceneIndexPathItemInfo(ResourceInfo resource)
    {
        var pathItem = DataClassInfoProvider.GetDataClassInfo(LuceneIncludedPathItemInfo.OBJECT_TYPE);
        if (pathItem is not null)
        {
            return;
        }

        pathItem = DataClassInfo.New(LuceneIncludedPathItemInfo.OBJECT_TYPE);
        pathItem.ClassName = LuceneIncludedPathItemInfo.TYPEINFO.ObjectClassName;
        pathItem.ClassTableName = LuceneIncludedPathItemInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        pathItem.ClassDisplayName = "Lucene Path Item";
        pathItem.ClassType = ClassType.OTHER;
        pathItem.ClassResourceID = resource.ResourceID;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(LuceneIncludedPathItemInfo.LuceneIncludedPathItemId));

        var formItem = new FormFieldInfo
        {
            Name = nameof(LuceneIncludedPathItemInfo.LuceneIncludedPathItemGuid),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = "guid",
            Enabled = true,
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(LuceneIncludedPathItemInfo.LuceneIncludedPathItemAliasPath),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            Size = 100,
            DataType = "text",
            Enabled = true,
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(LuceneIncludedPathItemInfo.LuceneIncludedPathItemIndexItemId),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = "integer",
            ReferenceToObjectType = LuceneIndexItemInfo.OBJECT_TYPE,
            ReferenceType = ObjectDependencyEnum.Required
        };

        formInfo.AddFormItem(formItem);

        pathItem.ClassFormDefinition = formInfo.GetXmlDefinition();

        DataClassInfoProvider.SetDataClassInfo(pathItem);
    }

    private static void InstallLuceneLanguageInfo(ResourceInfo resource)
    {
        var language = DataClassInfoProvider.GetDataClassInfo(LuceneIndexLanguageItemInfo.OBJECT_TYPE);
        if (language is not null)
        {
            return;
        }

        language = DataClassInfo.New(LuceneIndexLanguageItemInfo.OBJECT_TYPE);
        language.ClassName = LuceneIndexLanguageItemInfo.TYPEINFO.ObjectClassName;
        language.ClassTableName = LuceneIndexLanguageItemInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        language.ClassDisplayName = "Lucene Indexed Language Item";
        language.ClassType = ClassType.OTHER;
        language.ClassResourceID = resource.ResourceID;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(LuceneIndexLanguageItemInfo.LuceneIndexLanguageItemID));

        var formItem = new FormFieldInfo
        {
            Name = nameof(LuceneIndexLanguageItemInfo.LuceneIndexLanguageItemName),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            Size = 100,
            DataType = "text",
            Enabled = true,
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(LuceneIndexLanguageItemInfo.LuceneIndexLanguageItemGuid),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = "guid",
            Enabled = true
        };

        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(LuceneIndexLanguageItemInfo.LuceneIndexLanguageItemIndexItemId),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = "integer",
            ReferenceToObjectType = LuceneIndexItemInfo.OBJECT_TYPE,
            ReferenceType = ObjectDependencyEnum.Required,
        };

        formInfo.AddFormItem(formItem);

        language.ClassFormDefinition = formInfo.GetXmlDefinition();

        DataClassInfoProvider.SetDataClassInfo(language);
    }

    private static void InstallLuceneContentTypeItemInfo(ResourceInfo resource)
    {
        var contentType = DataClassInfoProvider.GetDataClassInfo(LuceneContentTypeItemInfo.OBJECT_TYPE);
        if (contentType is not null)
        {
            return;
        }

        contentType = DataClassInfo.New(LuceneContentTypeItemInfo.OBJECT_TYPE);
        contentType.ClassName = LuceneContentTypeItemInfo.TYPEINFO.ObjectClassName;
        contentType.ClassTableName = LuceneContentTypeItemInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        contentType.ClassDisplayName = "Lucene Type Item";
        contentType.ClassType = ClassType.OTHER;
        contentType.ClassResourceID = resource.ResourceID;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(LuceneContentTypeItemInfo.LuceneContentTypeItemId));

        var formItem = new FormFieldInfo
        {
            Name = nameof(LuceneContentTypeItemInfo.LuceneContentTypeItemContentTypeName),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            Size = 100,
            DataType = "text",
            Enabled = true,
            IsUnique = false
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(LuceneContentTypeItemInfo.LuceneContentTypeItemIncludedPathItemId),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = "integer",
            ReferenceToObjectType = LuceneIncludedPathItemInfo.OBJECT_TYPE,
            ReferenceType = ObjectDependencyEnum.Required,
        };

        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(LuceneContentTypeItemInfo.LuceneContentTypeItemGuid),
            Enabled = true,
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = "guid",
        };

        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(LuceneContentTypeItemInfo.LuceneContentTypeItemIndexItemId),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = "integer",
            ReferenceToObjectType = LuceneIndexItemInfo.OBJECT_TYPE,
            ReferenceType = ObjectDependencyEnum.Required
        };

        formInfo.AddFormItem(formItem);

        contentType.ClassFormDefinition = formInfo.GetXmlDefinition();

        DataClassInfoProvider.SetDataClassInfo(contentType);
    }

    private static void SetInitialVersion(ILuceneModuleVersionInfoProvider moduleVersionProvider)
    {
        var versions = moduleVersionProvider.Get().GetEnumerableTypedResult().ToList();

        if (versions.Count > 0)
        {
            return;
        }

        string version = moduleVersionProvider.GetAssemblyVersionNumber();

        var initialVersion = new LuceneModuleVersionInfo
        {
            LuceneModuleVersionNumber = version
        };
        moduleVersionProvider.Set(initialVersion);
    }
}
