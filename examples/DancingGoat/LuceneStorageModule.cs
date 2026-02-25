using CMS;
using CMS.Core;
using CMS.DataEngine;
using CMS.IO;

using DancingGoat.Lucene;

using Kentico.Xperience.AzureStorage;
using Kentico.Xperience.Cloud;
using Kentico.Xperience.Lucene.Core.Store;


// Registers the Lucene storage module into the system
[assembly: RegisterModule(typeof(LuceneStorageModule))]

namespace DancingGoat.Lucene;

/// <summary>
/// Module responsible for configuring Lucene index storage using CMS.IO.
/// This allows Lucene indexes to be stored on Azure Blob Storage in cloud environments
/// and on the local filesystem during development.
/// </summary>
/// <remarks>
/// <para>
/// This module maps the path "lucene/indexes" to the appropriate storage
/// provider based on the current environment:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Production/Staging/QA: Azure Blob Storage</description>
/// </item>
/// <item>
/// <description>Development: Local filesystem ($StorageLucene/test-index, etc.)</description>
/// </item>
/// </list>
/// </remarks>
public class LuceneStorageModule : Module
{
    /// <summary>
    /// Local directory used for development storage of Lucene indexes.
    /// </summary>
    private const string LOCAL_STORAGE_LUCENE_DIRECTORY_NAME = "$StorageLucene";


    /// <summary>
    /// Container name within Azure Blob Storage for Lucene indexes.
    /// </summary>
    private const string CONTAINER_NAME = "lucene";


    private IWebHostEnvironment? mEnvironment;


    /// <summary>
    /// Gets the web hosting environment information.
    /// </summary>
    public IWebHostEnvironment Environment
    {
        get
        {
            return mEnvironment ??= Service.Resolve<IWebHostEnvironment>();
        }
    }


    /// <summary>
    /// Module constructor.
    /// </summary>
    public LuceneStorageModule()
        : base(nameof(LuceneStorageModule))
    {
    }


    /// <summary>
    /// Initializes the module and configures storage providers for Lucene indexes.
    /// </summary>
    protected override void OnInit()
    {
        base.OnInit();

        if (Environment.IsQa() ||
            Environment.IsUat() ||
            Environment.IsEnvironment(CloudEnvironments.Custom) ||
            Environment.IsEnvironment(CloudEnvironments.Staging) ||
            Environment.IsProduction())
        {
            // Map Lucene indexes to Azure Blob Storage in cloud environments
            MapAzureStoragePath($"~/{LuceneStorageOptions.LUCENE_INDEX_PATH}/");
        }
        else
        {
            // Map Lucene indexes to local filesystem in development
            MapLocalStoragePath($"~/{LuceneStorageOptions.LUCENE_INDEX_PATH}/");
        }
    }


    /// <summary>
    /// Maps a virtual path to Azure Blob Storage.
    /// </summary>
    /// <param name="path">The path to map (e.g., "lucene/indexes").</param>
    private static void MapAzureStoragePath(string path)
    {
        // Creates a new StorageProvider instance for Azure
        var provider = AzureStorageProvider.Create();

        // Specifies the target container for Lucene indexes
        provider.CustomRootPath = CONTAINER_NAME;
        provider.PublicExternalFolderObject = false;

        StorageHelper.MapStoragePath(path, provider);
    }


    /// <summary>
    /// Maps a virtual path to local filesystem storage.
    /// </summary>
    /// <param name="path">The path to map (e.g., "lucene/indexes").</param>
    private static void MapLocalStoragePath(string path)
    {
        // Creates a new StorageProvider instance for local storage
        var provider = StorageProvider.CreateFileSystemStorageProvider();

        provider.CustomRootPath = $"{LOCAL_STORAGE_LUCENE_DIRECTORY_NAME}/{CONTAINER_NAME}";

        StorageHelper.MapStoragePath(path, provider);
    }
}
