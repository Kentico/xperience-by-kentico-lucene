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
        var luceneItemInfo = DataClassInfoProvider.GetDataClassInfo(IndexItemInfo.OBJECT_TYPE);
        if (luceneItemInfo is not null)
        {
            return;
        }

        luceneItemInfo = DataClassInfo.New(IndexItemInfo.OBJECT_TYPE);

        luceneItemInfo.ClassName = IndexItemInfo.OBJECT_TYPE;
        luceneItemInfo.ClassTableName = IndexItemInfo.OBJECT_TYPE.Replace(".", "_");
        luceneItemInfo.ClassDisplayName = "Lucene Index Item";
        luceneItemInfo.ClassType = ClassType.OTHER;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(IndexItemInfo.LuceneIndexItemId));

        var formItem = new FormFieldInfo
        {
            Name = "IndexName",
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
            Name = "ChannelName",
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
            Name = "StrategyName",
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
            Name = "RebuildHook",
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
        var pathItem = DataClassInfoProvider.GetDataClassInfo(IncludedPathItemInfo.OBJECT_TYPE);

        if (pathItem is not null)
        {
            return;
        }

        pathItem = DataClassInfo.New(IncludedPathItemInfo.OBJECT_TYPE);

        pathItem.ClassName = IncludedPathItemInfo.OBJECT_TYPE;
        pathItem.ClassTableName = IncludedPathItemInfo.OBJECT_TYPE.Replace(".", "_");
        pathItem.ClassDisplayName = "Lucene Path Item";
        pathItem.ClassType = ClassType.OTHER;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(IncludedPathItemInfo.LuceneIncludedPathItemId));

        var formItem = new FormFieldInfo
        {
            Name = "AliasPath",
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
            Name = nameof(IndexItemInfo.LuceneIndexItemId),
            AllowEmpty = false,
            Visible = true,
            DataType = "integer",
            ReferenceToObjectType = nameof(IndexItemInfo),
            ReferenceType = ObjectDependencyEnum.Required
        };

        formInfo.AddFormItem(formItem);

        pathItem.ClassFormDefinition = formInfo.GetXmlDefinition();

        DataClassInfoProvider.SetDataClassInfo(pathItem);
    }

    private void InstallLuceneLanguageInfo()
    {
        string languageInfoName = IndexedLanguageInfo.OBJECT_TYPE;
        string idName = nameof(IndexedLanguageInfo.IndexedLanguageId);
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
            Name = "languageCode",
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
            Name = nameof(IndexItemInfo.LuceneIndexItemId),
            AllowEmpty = false,
            Visible = true,
            DataType = "integer",
            ReferenceToObjectType = nameof(IndexItemInfo),
            ReferenceType = ObjectDependencyEnum.Required,
        };

        formInfo.AddFormItem(formItem);

        language.ClassFormDefinition = formInfo.GetXmlDefinition();

        DataClassInfoProvider.SetDataClassInfo(language);
    }

    private void InstallLuceneContentTypeItemInfo()
    {
        var contentType = DataClassInfoProvider.GetDataClassInfo(ContentTypeItemInfo.OBJECT_TYPE);

        if (contentType is not null)
        {
            return;
        }

        contentType = DataClassInfo.New();

        contentType.ClassName = ContentTypeItemInfo.OBJECT_TYPE;
        contentType.ClassTableName = ContentTypeItemInfo.OBJECT_TYPE.Replace(".", "_");
        contentType.ClassDisplayName = "Lucene Type Item";
        contentType.ClassType = ClassType.OTHER;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(ContentTypeItemInfo.LuceneContentTypeItemId));

        var formItem = new FormFieldInfo
        {
            Name = nameof(ContentTypeItemInfo.ContentTypeName),
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
            Name = nameof(IncludedPathItemInfo.LuceneIncludedPathItemId),
            AllowEmpty = false,
            Visible = true,
            DataType = "integer",
            ReferenceToObjectType = nameof(IncludedPathItemInfo),
            ReferenceType = ObjectDependencyEnum.Required,
        };

        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(IndexItemInfo.LuceneIndexItemId),
            AllowEmpty = false,
            Visible = true,
            DataType = "integer",
            ReferenceToObjectType = nameof(IndexItemInfo),
            ReferenceType = ObjectDependencyEnum.Required
        };

        formInfo.AddFormItem(formItem);

        contentType.ClassFormDefinition = formInfo.GetXmlDefinition();

        DataClassInfoProvider.SetDataClassInfo(contentType);
    }
}
