using CMS.Base;
using CMS.DataEngine;
using CMS.FormEngine;

namespace Kentico.Xperience.Lucene.Admin;

internal class LuceneModuleInstaller
{
    public void Install()
    {
        using (new CMSActionContext { ContinuousIntegrationAllowObjectSerialization = false })
        {
            InstallModuleClasses();
        }
    }

    private void InstallModuleClasses()
    {
        InstallLuceneItemInfo();
        InstallLuceneLanguageInfo();
        InstallLuceneIndexPathItemInfo();
        InstallLuceneContentTypeItemInfo();
    }

    private static void InstallLuceneItemInfo()
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

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(LuceneIndexItemInfo.LuceneIndexItemId));

        var formItem = new FormFieldInfo
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

    private void InstallLuceneIndexPathItemInfo()
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

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(LuceneIncludedPathItemInfo.LuceneIncludedPathItemId));

        var formItem = new FormFieldInfo
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
            DataType = "integer",
            ReferenceToObjectType = nameof(LuceneIndexItemInfo),
            ReferenceType = ObjectDependencyEnum.Required
        };

        formInfo.AddFormItem(formItem);

        pathItem.ClassFormDefinition = formInfo.GetXmlDefinition();

        DataClassInfoProvider.SetDataClassInfo(pathItem);
    }

    private void InstallLuceneLanguageInfo()
    {
        string idName = nameof(LuceneIndexLanguageItemInfo.LuceneIndexLanguageItemID);
        var language = DataClassInfoProvider.GetDataClassInfo(LuceneIndexLanguageItemInfo.OBJECT_TYPE);

        if (language is not null)
        {
            return;
        }

        language = DataClassInfo.New();

        language.ClassName = LuceneIndexLanguageItemInfo.TYPEINFO.ObjectClassName;
        language.ClassTableName = LuceneIndexLanguageItemInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        language.ClassDisplayName = "Lucene Indexed Language Item";
        language.ClassType = ClassType.OTHER;

        var formInfo = FormHelper.GetBasicFormDefinition(idName);

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
            Name = nameof(LuceneIndexLanguageItemInfo.LuceneIndexLanguageItemIndexItemId),
            AllowEmpty = false,
            Visible = true,
            DataType = "integer",
            ReferenceToObjectType = nameof(LuceneIndexItemInfo),
            ReferenceType = ObjectDependencyEnum.Required,
        };

        formInfo.AddFormItem(formItem);

        language.ClassFormDefinition = formInfo.GetXmlDefinition();

        DataClassInfoProvider.SetDataClassInfo(language);
    }

    private void InstallLuceneContentTypeItemInfo()
    {
        var contentType = DataClassInfoProvider.GetDataClassInfo(LuceneContentTypeItemInfo.OBJECT_TYPE);

        if (contentType is not null)
        {
            return;
        }

        contentType = DataClassInfo.New();

        contentType.ClassName = LuceneContentTypeItemInfo.TYPEINFO.ObjectClassName;
        contentType.ClassTableName = LuceneContentTypeItemInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        contentType.ClassDisplayName = "Lucene Type Item";
        contentType.ClassType = ClassType.OTHER;

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
            DataType = "integer",
            ReferenceToObjectType = nameof(LuceneIncludedPathItemInfo),
            ReferenceType = ObjectDependencyEnum.Required,
        };

        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(LuceneContentTypeItemInfo.LuceneContentTypeItemIndexItemId),
            AllowEmpty = false,
            Visible = true,
            DataType = "integer",
            ReferenceToObjectType = nameof(LuceneIndexItemInfo),
            ReferenceType = ObjectDependencyEnum.Required
        };

        formInfo.AddFormItem(formItem);

        contentType.ClassFormDefinition = formInfo.GetXmlDefinition();

        DataClassInfoProvider.SetDataClassInfo(contentType);
    }
}
