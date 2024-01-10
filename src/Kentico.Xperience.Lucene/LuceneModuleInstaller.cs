using CMS.Base;
using CMS.DataEngine;
using CMS.FormEngine;
using Kentico.Xperience.Lucene.Models;

namespace Kentico.Xperience.Lucene;

public class LuceneModuleInstaller
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

    private void InstallLuceneItemInfo()
    {
        var luceneItemInfo = DataClassInfoProvider.GetDataClassInfo(LuceneIndexItemInfo.OBJECT_TYPE);
        if (luceneItemInfo is not null)
        {
            return;
        }

        luceneItemInfo = DataClassInfo.New(LuceneIndexItemInfo.OBJECT_TYPE);

        luceneItemInfo.ClassName = LuceneIndexItemInfo.OBJECT_TYPE;
        luceneItemInfo.ClassTableName = LuceneIndexItemInfo.OBJECT_TYPE.Replace(".", "_");
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

        pathItem.ClassName = LuceneIncludedPathItemInfo.OBJECT_TYPE;
        pathItem.ClassTableName = LuceneIncludedPathItemInfo.OBJECT_TYPE.Replace(".", "_");
        pathItem.ClassDisplayName = "Lucene Path Item";
        pathItem.ClassType = ClassType.OTHER;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(LuceneIncludedPathItemInfo.LuceneIncludedPathItemId));

        var formItem = new FormFieldInfo
        {
            Name = nameof(LuceneIncludedPathItemInfo.LuceneIncludedPathAliasPath),
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
            Name = nameof(LuceneIncludedPathItemInfo.LuceneIncludedPathIndexItemId),
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
        string languageInfoName = LuceneIndexedLanguageInfo.OBJECT_TYPE;
        string idName = nameof(LuceneIndexedLanguageInfo.LuceneIndexedLanguageId);
        var language = DataClassInfoProvider.GetDataClassInfo(languageInfoName);

        if (language is not null)
        {
            return;
        }

        language = DataClassInfo.New();

        language.ClassName = languageInfoName;
        language.ClassTableName = languageInfoName.Replace(".", "_");
        language.ClassDisplayName = "Lucene Indexed Language Item";
        language.ClassType = ClassType.OTHER;

        var formInfo = FormHelper.GetBasicFormDefinition(idName);

        var formItem = new FormFieldInfo
        {
            Name = nameof(LuceneIndexedLanguageInfo.LuceneIndexedLanguageName),
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
            Name = nameof(LuceneIndexedLanguageInfo.LuceneIndexedLanguageIndexItemId),
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

        contentType.ClassName = LuceneContentTypeItemInfo.OBJECT_TYPE;
        contentType.ClassTableName = LuceneContentTypeItemInfo.OBJECT_TYPE.Replace(".", "_");
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
