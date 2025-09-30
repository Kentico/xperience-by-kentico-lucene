using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.FormEngine;
using CMS.Modules;

namespace Kentico.Xperience.Lucene.Core;

/// <summary>
/// Provides functionality to install and configure the Lucene module for Kentico Xperience.
/// </summary>
/// <param name="resourceProvider">The <see cref="IInfoProvider{ResourceInfo}"/> used to retrieve <see cref="ResourceInfo"/>.</param>
public class LuceneModuleInstaller(IInfoProvider<ResourceInfo> resourceProvider)
{
    private readonly IInfoProvider<ResourceInfo> resourceProvider = resourceProvider;


    /// <summary>
    /// Installs the necessary resources and configurations for the Lucene integration.
    /// </summary>
    public void Install()
    {
        var resource = resourceProvider.Get("CMS.Integration.Lucene")
            // Handle v4.0.0 resource name manually until migrations are enabled
            ?? resourceProvider.Get("Kentico.Xperience.Lucene")
            ?? new ResourceInfo();

        InitializeResource(resource);
        InstallLuceneItemInfo(resource);
        InstallLuceneLanguageInfo(resource);
        InstallLuceneIndexPathItemInfo(resource);
        InstallLuceneContentTypeItemInfo(resource);
        InstallLuceneReusableContentTypeItemInfo(resource);
        InstallLuceneIndexAssemblyVersionItemInfo(resource);
        
        // Migrate existing data to populate missing channel names
        MigrateExistingData();
    }


    /// <summary>
    /// Initializes the specified resource with predefined values for integration with Kentico and Lucene.
    /// </summary>
    /// <param name="resource">The <see cref="ResourceInfo"/> object to initialize. </param>
    /// <returns>The updated <see cref="ResourceInfo"/> object with initialized values for display name, resource name,
    /// description, and development status.</returns>
    public ResourceInfo InitializeResource(ResourceInfo resource)
    {
        resource.ResourceDisplayName = "Kentico Integration - Lucene";

        // Prefix ResourceName with "CMS" to prevent C# class generation
        // Classes are already available through the library itself
        resource.ResourceName = "CMS.Integration.Lucene";
        resource.ResourceDescription = "Kentico Lucene custom data";
        resource.ResourceIsInDevelopment = false;
        if (resource.HasChanged)
        {
            resourceProvider.Set(resource);
        }

        return resource;
    }


    /// <summary>
    /// Configures and installs the <see cref="LuceneIndexItemInfo"/> for the specified resource.
    /// </summary>
    /// <param name="resource">The <see cref="ResourceInfo"/> for which the Lucene index item information is being installed.</param>
    public void InstallLuceneItemInfo(ResourceInfo resource)
    {
        var info = DataClassInfoProvider.GetDataClassInfo(LuceneIndexItemInfo.OBJECT_TYPE) ?? DataClassInfo.New(LuceneIndexItemInfo.OBJECT_TYPE);

        info.ClassName = LuceneIndexItemInfo.TYPEINFO.ObjectClassName;
        info.ClassTableName = LuceneIndexItemInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        info.ClassDisplayName = "Lucene Index Item";
        info.ClassType = ClassType.OTHER;
        info.ClassResourceID = resource.ResourceID;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(LuceneIndexItemInfo.LuceneIndexItemId));

        var formItem = new FormFieldInfo
        {
            Name = nameof(LuceneIndexItemInfo.LuceneIndexItemGuid),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = "guid",
            Enabled = true,
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
            Name = nameof(LuceneIndexItemInfo.LuceneIndexItemAnalyzerName),
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

        // Add obsolete channel name field to handle legacy data gracefully
        // Make it optional and hidden to avoid UI issues
#pragma warning disable CS0612 // Type or member is obsolete
        formItem = new FormFieldInfo
        {
            Name = nameof(LuceneIndexItemInfo.LuceneIndexItemChannelName),
            AllowEmpty = true,
            Visible = false,
            Precision = 0,
            Size = 100,
            DataType = "text",
            Enabled = false
        };
#pragma warning restore CS0612 // Type or member is obsolete
        formInfo.AddFormItem(formItem);

        SetFormDefinition(info, formInfo);

        if (info.HasChanged)
        {
            DataClassInfoProvider.SetDataClassInfo(info);
        }
    }


    /// <summary>
    /// Configures and installs the <see cref="LuceneIncludedPathItemInfo"/> for the specified resource.
    /// </summary>
    /// <param name="resource">The <see cref="ResourceInfo"/> for which the Lucene index item information is being installed.</param>
    public void InstallLuceneIndexPathItemInfo(ResourceInfo resource)
    {
        var info = DataClassInfoProvider.GetDataClassInfo(LuceneIncludedPathItemInfo.OBJECT_TYPE) ?? DataClassInfo.New(LuceneIncludedPathItemInfo.OBJECT_TYPE);

        info.ClassName = LuceneIncludedPathItemInfo.TYPEINFO.ObjectClassName;
        info.ClassTableName = LuceneIncludedPathItemInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        info.ClassDisplayName = "Lucene Path Item";
        info.ClassType = ClassType.OTHER;
        info.ClassResourceID = resource.ResourceID;

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
            Name = nameof(LuceneIncludedPathItemInfo.LuceneIncludedPathItemChannelName),
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
            Name = nameof(LuceneIncludedPathItemInfo.LuceneIncludedPathItemIndexItemId),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = "integer",
            ReferenceToObjectType = LuceneIndexItemInfo.OBJECT_TYPE,
            ReferenceType = ObjectDependencyEnum.Required
        };

        formInfo.AddFormItem(formItem);

        SetFormDefinition(info, formInfo);

        if (info.HasChanged)
        {
            DataClassInfoProvider.SetDataClassInfo(info);
        }
    }


    /// <summary>
    /// Configures and installs the <see cref="LuceneIndexAssemblyVersionItemInfo"/> for the specified resource.
    /// </summary>
    /// <param name="resource">The <see cref="ResourceInfo"/> for which the Lucene index item information is being installed.</param>
    public void InstallLuceneIndexAssemblyVersionItemInfo(ResourceInfo resource)
    {
        var info = DataClassInfoProvider.GetDataClassInfo(LuceneIndexAssemblyVersionItemInfo.OBJECT_TYPE) ?? DataClassInfo.New(LuceneIndexAssemblyVersionItemInfo.OBJECT_TYPE);

        info.ClassName = LuceneIndexAssemblyVersionItemInfo.TYPEINFO.ObjectClassName;
        info.ClassTableName = LuceneIndexAssemblyVersionItemInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        info.ClassDisplayName = "Lucene Index Assembly Version Item";
        info.ClassType = ClassType.OTHER;
        info.ClassResourceID = resource.ResourceID;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(LuceneIndexAssemblyVersionItemInfo.LuceneIndexAssemblyVersionItemID));

        var formItem = new FormFieldInfo
        {
            Name = nameof(LuceneIndexAssemblyVersionItemInfo.LuceneIndexAssemblyVersionItemGuid),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = "guid",
            Enabled = true,
        };
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(LuceneIndexAssemblyVersionItemInfo.LuceneIndexAssemblyVersionItemAssemblyVersion),
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
            Name = nameof(LuceneIndexAssemblyVersionItemInfo.LuceneIndexAssemblyVersionItemIndexItemId),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = "integer",
            ReferenceToObjectType = LuceneIndexItemInfo.OBJECT_TYPE,
            ReferenceType = ObjectDependencyEnum.Required
        };
        formInfo.AddFormItem(formItem);

        SetFormDefinition(info, formInfo);

        if (info.HasChanged)
        {
            DataClassInfoProvider.SetDataClassInfo(info);
        }
    }


    /// <summary>
    /// Configures and installs the <see cref="LuceneIndexLanguageItemInfo"/> for the specified resource.
    /// </summary>
    /// <param name="resource">The <see cref="ResourceInfo"/> for which the Lucene index item information is being installed.</param>
    public void InstallLuceneLanguageInfo(ResourceInfo resource)
    {
        var info = DataClassInfoProvider.GetDataClassInfo(LuceneIndexLanguageItemInfo.OBJECT_TYPE) ?? DataClassInfo.New(LuceneIndexLanguageItemInfo.OBJECT_TYPE);

        info.ClassName = LuceneIndexLanguageItemInfo.TYPEINFO.ObjectClassName;
        info.ClassTableName = LuceneIndexLanguageItemInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        info.ClassDisplayName = "Lucene Indexed Language Item";
        info.ClassType = ClassType.OTHER;
        info.ClassResourceID = resource.ResourceID;

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

        SetFormDefinition(info, formInfo);

        if (info.HasChanged)
        {
            DataClassInfoProvider.SetDataClassInfo(info);
        }
    }


    /// <summary>
    /// Configures and installs the <see cref="LuceneReusableContentTypeItemInfo"/> for the specified resource.
    /// </summary>
    /// <param name="resource">The <see cref="ResourceInfo"/> for which the Lucene index item information is being installed.</param>
    public void InstallLuceneReusableContentTypeItemInfo(ResourceInfo resource)
    {
        var info = DataClassInfoProvider.GetDataClassInfo(LuceneReusableContentTypeItemInfo.OBJECT_TYPE) ?? DataClassInfo.New(LuceneReusableContentTypeItemInfo.OBJECT_TYPE);

        info.ClassName = LuceneReusableContentTypeItemInfo.TYPEINFO.ObjectClassName;
        info.ClassTableName = LuceneReusableContentTypeItemInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        info.ClassDisplayName = "Lucene Reusable Content Type Item";
        info.ClassType = ClassType.OTHER;
        info.ClassResourceID = resource.ResourceID;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(LuceneReusableContentTypeItemInfo.LuceneReusableContentTypeItemId));

        var formItem = new FormFieldInfo
        {
            Name = nameof(LuceneReusableContentTypeItemInfo.LuceneReusableContentTypeItemContentTypeName),
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
            Name = nameof(LuceneReusableContentTypeItemInfo.LuceneReusableContentTypeItemGuid),
            Enabled = true,
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = "guid",
        };

        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(LuceneReusableContentTypeItemInfo.LuceneReusableContentTypeItemIndexItemId),
            AllowEmpty = false,
            Visible = true,
            Precision = 0,
            DataType = "integer",
            ReferenceToObjectType = LuceneIndexItemInfo.OBJECT_TYPE,
            ReferenceType = ObjectDependencyEnum.Required
        };

        formInfo.AddFormItem(formItem);

        SetFormDefinition(info, formInfo);

        if (info.HasChanged)
        {
            DataClassInfoProvider.SetDataClassInfo(info);
        }
    }


    /// <summary>
    /// Configures and installs the <see cref="LuceneContentTypeItemInfo"/> for the specified resource.
    /// </summary>
    /// <param name="resource">The <see cref="ResourceInfo"/> for which the Lucene index item information is being installed.</param>
    public void InstallLuceneContentTypeItemInfo(ResourceInfo resource)
    {
        var info = DataClassInfoProvider.GetDataClassInfo(LuceneContentTypeItemInfo.OBJECT_TYPE) ?? DataClassInfo.New(LuceneContentTypeItemInfo.OBJECT_TYPE);

        info.ClassName = LuceneContentTypeItemInfo.TYPEINFO.ObjectClassName;
        info.ClassTableName = LuceneContentTypeItemInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        info.ClassDisplayName = "Lucene Type Item";
        info.ClassType = ClassType.OTHER;
        info.ClassResourceID = resource.ResourceID;

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

        SetFormDefinition(info, formInfo);

        if (info.HasChanged)
        {
            DataClassInfoProvider.SetDataClassInfo(info);
        }
    }


    /// <summary>
    /// Ensure that the form is upserted with any existing form
    /// </summary>
    /// <param name="info"></param>
    /// <param name="form"></param>
    private static void SetFormDefinition(DataClassInfo info, FormInfo form)
    {
        if (info.ClassID > 0)
        {
            var existingForm = new FormInfo(info.ClassFormDefinition);
            existingForm.CombineWithForm(form, new());
            info.ClassFormDefinition = existingForm.GetXmlDefinition();
        }
        else
        {
            info.ClassFormDefinition = form.GetXmlDefinition();
        }
    }

    /// <summary>
    /// Migrates existing data to ensure compatibility with channel configuration changes.
    /// Populates missing channel names in LuceneIncludedPathItemInfo records.
    /// </summary>
    private void MigrateExistingData()
    {
        try
        {
            // Get all included path items without a channel name
            var pathsWithoutChannel = LuceneIncludedPathItemInfo.Provider.Get()
                .WhereEmpty(nameof(LuceneIncludedPathItemInfo.LuceneIncludedPathItemChannelName))
                .Or()
                .WhereNull(nameof(LuceneIncludedPathItemInfo.LuceneIncludedPathItemChannelName))
                .ToList();

            if (!pathsWithoutChannel.Any())
            {
                return; // No migration needed
            }

            // Get the first available website channel to use as default
            var defaultChannel = ChannelInfo.Provider.Get()
                .WhereEquals(nameof(ChannelInfo.ChannelType), ChannelType.Website.ToString())
                .TopN(1)
                .FirstOrDefault();

            if (defaultChannel == null)
            {
                return; // No website channels available, migration cannot proceed
            }

            // Update all paths without channel names to use the default channel
            foreach (var pathItem in pathsWithoutChannel)
            {
                pathItem.LuceneIncludedPathItemChannelName = defaultChannel.ChannelName;
                pathItem.Update();
            }
        }
        catch
        {
            // Migration is best-effort - don't fail installation if migration encounters issues
            // The constructor changes should handle these cases gracefully anyway
        }
    }
}
