using CMS.Base;
using CMS.DataEngine;
using CMS.FormEngine;
using CMS;

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
        var luceneItemInfo = DataClassInfoProvider.GetDataClassInfo(IndexitemInfo.OBJECT_TYPE);
        if (luceneItemInfo is not null)
            return;

        luceneItemInfo = DataClassInfo.New(IndexitemInfo.OBJECT_TYPE);

        luceneItemInfo.ClassName = IndexitemInfo.OBJECT_TYPE;
        luceneItemInfo.ClassTableName = IndexitemInfo.OBJECT_TYPE.Replace(".", "_");
        luceneItemInfo.ClassDisplayName = "Lucene Index Item";
        luceneItemInfo.ClassType = ClassType.OTHER;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(IndexitemInfo.LuceneIndexItemId));

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
        var pathItem = DataClassInfoProvider.GetDataClassInfo(IncludedpathitemInfo.OBJECT_TYPE);

        if (pathItem is not null)
            return;

        pathItem = DataClassInfo.New(IncludedpathitemInfo.OBJECT_TYPE);

        pathItem.ClassName = IncludedpathitemInfo.OBJECT_TYPE;
        pathItem.ClassTableName = IncludedpathitemInfo.OBJECT_TYPE.Replace(".", "_");
        pathItem.ClassDisplayName = "Lucene Path Item";
        pathItem.ClassType = ClassType.OTHER;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(IncludedpathitemInfo.LuceneIncludedPathItemId));

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
            Name = nameof(IndexitemInfo.LuceneIndexItemId),
            AllowEmpty = false,
            Visible = true,
            DataType = "integer",
            ReferenceToObjectType = nameof(IndexitemInfo),
            ReferenceType = ObjectDependencyEnum.Required
        };

        formInfo.AddFormItem(formItem);

        pathItem.ClassFormDefinition = formInfo.GetXmlDefinition();

        DataClassInfoProvider.SetDataClassInfo(pathItem);
    }

    private void InstallLuceneLanguageInfo()
    {
        string languageInfoName = IndexedlanguageInfo.OBJECT_TYPE;
        string idName = nameof(IndexedlanguageInfo.IndexedLanguageId);
        var language = DataClassInfoProvider.GetDataClassInfo(languageInfoName);

        if (language is not null)
            return;

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
            Name = nameof(IndexitemInfo.LuceneIndexItemId),
            AllowEmpty = false,
            Visible = true,
            DataType = "integer",
            ReferenceToObjectType = nameof(IndexitemInfo),
            ReferenceType = ObjectDependencyEnum.Required,
        };

        formInfo.AddFormItem(formItem);

        language.ClassFormDefinition = formInfo.GetXmlDefinition();

        DataClassInfoProvider.SetDataClassInfo(language);
    }

    private void InstallLuceneContentTypeItemInfo()
    {
        var contentType = DataClassInfoProvider.GetDataClassInfo(ContenttypeitemInfo.OBJECT_TYPE);

        if (contentType is not null)
            return;

        contentType = DataClassInfo.New();

        contentType.ClassName = ContenttypeitemInfo.OBJECT_TYPE;
        contentType.ClassTableName = ContenttypeitemInfo.OBJECT_TYPE.Replace(".", "_");
        contentType.ClassDisplayName = "Lucene Type Item";
        contentType.ClassType = ClassType.OTHER;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(ContenttypeitemInfo.LuceneContentTypeItemId));

        var formItem = new FormFieldInfo
        {
            Name = "TypeName",
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
            Name = nameof(IncludedpathitemInfo.LuceneIncludedPathItemId),
            AllowEmpty = false,
            Visible = true,
            DataType = "integer",
            ReferenceToObjectType = nameof(IncludedpathitemInfo),
            ReferenceType = ObjectDependencyEnum.Required,
        };

        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(IndexitemInfo.LuceneIndexItemId),
            AllowEmpty = false,
            Visible = true,
            DataType= "integer",
            ReferenceToObjectType = nameof(IndexitemInfo),
            ReferenceType = ObjectDependencyEnum.Required
        };

        formInfo.AddFormItem(formItem);

        contentType.ClassFormDefinition = formInfo.GetXmlDefinition();

        DataClassInfoProvider.SetDataClassInfo(contentType);
    }
}
