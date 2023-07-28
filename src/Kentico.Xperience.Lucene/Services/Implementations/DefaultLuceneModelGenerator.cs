using System.Reflection;
using CMS.Core;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.FormEngine;
using CMS.MediaLibrary;

using Kentico.Content.Web.Mvc;
using Kentico.Xperience.Lucene.Attributes;
using Kentico.Xperience.Lucene.Models;

namespace Kentico.Xperience.Lucene.Services
{
    /// <summary>
    /// Default implementation of <see cref="ILuceneModelGenerator"/>.
    /// </summary>
    internal class DefaultLuceneModelGenerator : ILuceneModelGenerator
    {
        private readonly IConversionService conversionService;
        private readonly IEventLogService eventLogService;
        private readonly IMediaFileInfoProvider mediaFileInfoProvider;
        private readonly IMediaFileUrlRetriever mediaFileUrlRetriever;
        private readonly Dictionary<string, string[]> cachedIndexedColumns = new();
        private readonly string[] ignoredPropertiesForTrackingChanges = new string[] {
            nameof(LuceneSearchModel.ObjectID),
            nameof(LuceneSearchModel.Url),
            nameof(LuceneSearchModel.ClassName)
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultLuceneModelGenerator"/> class.
        /// </summary>
        public DefaultLuceneModelGenerator(IConversionService conversionService,
            IEventLogService eventLogService,
            IMediaFileInfoProvider mediaFileInfoProvider,
            IMediaFileUrlRetriever mediaFileUrlRetriever)
        {
            this.conversionService = conversionService;
            this.eventLogService = eventLogService;
            this.mediaFileInfoProvider = mediaFileInfoProvider;
            this.mediaFileUrlRetriever = mediaFileUrlRetriever;
        }


        /// <inheritdoc/>
        public async Task<LuceneSearchModel> GetTreeNodeData(LuceneQueueItem queueItem)
        {
            var luceneIndex = IndexStore.Instance.GetIndex(queueItem.IndexName) ?? throw new Exception($"LuceneIndex {queueItem.IndexName} not found!");

            var data = Activator.CreateInstance(luceneIndex.LuceneSearchModelType) as LuceneSearchModel ?? throw new Exception($"Faild to create instance of {luceneIndex.LuceneSearchModelType}");

            await MapChangedProperties(luceneIndex, queueItem, data!);
            MapCommonProperties(queueItem.Node, data!);
            data = await luceneIndex.LuceneIndexingStrategy.OnIndexingNode(queueItem.Node, data);
            return data;
        }

        /// <summary>
        /// Converts the assets from the <paramref name="node"/>'s value into absolute URLs.
        /// </summary>
        /// <remarks>Logs an error if the definition of the <paramref name="columnName"/> can't
        /// be found.</remarks>
        /// <param name="node">The <see cref="TreeNode"/> the value was loaded from.</param>
        /// <param name="nodeValue">The original value of the column.</param>
        /// <param name="columnName">The name of the column the value was loaded from.</param>
        /// <returns>An list of absolute URLs, or an empty list.</returns>
        private IEnumerable<string> GetAssetUrlsForColumn(TreeNode node, object nodeValue, string columnName)
        {
            string strValue = conversionService.GetString(nodeValue, string.Empty);
            if (string.IsNullOrEmpty(strValue))
            {
                return Enumerable.Empty<string>();
            }

            // Ensure field is Asset type
            var dataClassInfo = DataClassInfoProvider.GetDataClassInfo(node.ClassName, false);
            var formInfo = new FormInfo(dataClassInfo.ClassFormDefinition);
            var field = formInfo.GetFormField(columnName);
            if (field == null)
            {
                eventLogService.LogError(nameof(DefaultLuceneModelGenerator), nameof(GetAssetUrlsForColumn), $"Unable to load field definition for content type '{node.ClassName}' column name '{columnName}.'");
                return Enumerable.Empty<string>();
            }

            if (!field.DataType.Equals(FieldDataType.Assets, StringComparison.OrdinalIgnoreCase))
            {
                return Enumerable.Empty<string>();
            }

            var dataType = DataTypeManager.GetDataType(typeof(IEnumerable<AssetRelatedItem>));
            if (dataType.Convert(strValue, null, null) is not IEnumerable<AssetRelatedItem> assets)
            {
                return Enumerable.Empty<string>();
            }

            var mediaFiles = mediaFileInfoProvider.Get().ForAssets(assets);

            return mediaFiles.Select(file => mediaFileUrlRetriever.Retrieve(file).RelativePath);
        }


        /// <summary>
        /// Gets the names of all database columns which are indexed by the passed index,
        /// minus those listed in <see cref="ignoredPropertiesForTrackingChanges"/>.
        /// </summary>
        /// <param name="luceneIndex">The index to load columns for.</param>
        /// <returns>The database columns that are indexed.</returns>
        private string[] GetIndexedColumnNames(LuceneIndex luceneIndex)
        {
            if (cachedIndexedColumns.TryGetValue(luceneIndex.IndexName, out string[]? value))
            {
                return value;
            }

            // Don't include properties with SourceAttribute at first, check the sources and add to list after
            var indexedColumnNames = luceneIndex.LuceneSearchModelType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => !Attribute.IsDefined(prop, typeof(SourceAttribute))).Select(prop => prop.Name).ToList();
            var propertiesWithSourceAttribute = luceneIndex.LuceneSearchModelType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(prop => Attribute.IsDefined(prop, typeof(SourceAttribute)));
            foreach (var property in propertiesWithSourceAttribute)
            {
                var sourceAttribute = property.GetCustomAttributes<SourceAttribute>(false).FirstOrDefault();
                if (sourceAttribute == null)
                {
                    continue;
                }

                indexedColumnNames.AddRange(sourceAttribute.Sources);
            }

            // Remove column names from LuceneSearchModel that aren't database columns
            indexedColumnNames.RemoveAll(col => ignoredPropertiesForTrackingChanges.Contains(col));

            string[] indexedColumns = indexedColumnNames.ToArray();
            cachedIndexedColumns.Add(luceneIndex.IndexName, indexedColumns);

            return indexedColumns;
        }


        /// <summary>
        /// Gets the <paramref name="node"/> value using the <paramref name="property"/>
        /// name, or the property's <see cref="SourceAttribute"/> if specified.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> to load a value from.</param>
        /// <param name="property">The Lucene search model property.</param>
        /// <param name="indexingStrategy">The indexing strategy.</param>
        /// <param name="columnsToUpdate">A list of columns to retrieve values for. Columns not present
        /// in this list will return <c>null</c>.</param>
        private async Task<object?> GetNodeValue(TreeNode node, PropertyInfo property, ILuceneIndexingStrategy indexingStrategy, IEnumerable<string> columnsToUpdate)
        {
            object? nodeValue = null;
            string usedColumn = property.Name;
            if (Attribute.IsDefined(property, typeof(SourceAttribute)))
            {
                // Property uses SourceAttribute, loop through column names until a non-null value is found
                var sourceAttribute = property.GetCustomAttributes<SourceAttribute>(false).FirstOrDefault();
                foreach (string? source in sourceAttribute!.Sources.Where(s => columnsToUpdate.Contains(s)))
                {
                    nodeValue = node.GetValue(source);
                    if (nodeValue != null)
                    {
                        usedColumn = source;
                        break;
                    }
                }
            }
            else
            {
                if (!columnsToUpdate.Contains(property.Name))
                {
                    return null;
                }

                nodeValue = node.GetValue(property.Name);
            }

            // Convert node value to URLs if necessary
            if (nodeValue != null && Attribute.IsDefined(property, typeof(MediaUrlsAttribute)))
            {
                nodeValue = GetAssetUrlsForColumn(node, nodeValue, usedColumn);
            }

            nodeValue = await indexingStrategy.OnIndexingProperty(node, property.Name, usedColumn, nodeValue);

            return nodeValue;
        }


        /// <summary>
        /// Adds values to the <paramref name="data"/> by retriving the indexed columns of the index
        /// and getting values from the <see cref="LuceneQueueItem.Node"/>.
        /// </summary>
        private async Task MapChangedProperties(LuceneIndex luceneIndex, LuceneQueueItem queueItem, LuceneSearchModel data)
        {
            var columnsToUpdate = new List<string>();
            string[] indexedColumns = GetIndexedColumnNames(luceneIndex);
            if (queueItem.TaskType is LuceneTaskType.CREATE or LuceneTaskType.UPDATE)
            {
                columnsToUpdate.AddRange(indexedColumns);
            }

            var properties = luceneIndex.LuceneSearchModelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                object? nodeValue = await GetNodeValue(queueItem.Node, prop, luceneIndex.LuceneIndexingStrategy, columnsToUpdate);
                if (nodeValue == null)
                {
                    continue;
                }

                // TODO: map based on PropertyType
                if (Attribute.IsDefined(prop, typeof(TextFieldAttribute)))
                {
                    prop.SetValue(data, nodeValue.ToString());
                }
                else if (Attribute.IsDefined(prop, typeof(StringFieldAttribute)))
                {
                    prop.SetValue(data, nodeValue.ToString());
                }
                else if (Attribute.IsDefined(prop, typeof(Int32FieldAttribute)))
                {
                    prop.SetValue(data, (int)nodeValue);
                }
                else if (Attribute.IsDefined(prop, typeof(Int64FieldAttribute)))
                {
                    prop.SetValue(data, (long)nodeValue);
                }
                else if (Attribute.IsDefined(prop, typeof(SingleFieldAttribute)))
                {
                    prop.SetValue(data, (float)nodeValue);
                }
                else if (Attribute.IsDefined(prop, typeof(DoubleFieldAttribute)))
                {
                    prop.SetValue(data, (double)nodeValue);
                }
                else
                {
                    // TODO: log some warning or implement default to text field
                }
            }
        }


        /// <summary>
        /// Sets values in the <paramref name="data"/> object using the common search model properties
        /// located within the <see cref="LuceneSearchModel"/> class.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> to load values from.</param>
        /// <param name="data">The data object based on <see cref="LuceneSearchModel"/>.</param>
        private static void MapCommonProperties(TreeNode node, LuceneSearchModel data)
        {
            data.ObjectID = node.DocumentID.ToString();
            data.ClassName = node.ClassName;

            string url;
            try
            {
                url = DocumentURLProvider.GetAbsoluteUrl(node);
            }
            catch (Exception)
            {
                // GetAbsoluteUrl can throw an exception when processing a page update LuceneQueueItem
                // and the page was deleted before the update task has processed. In this case, upsert an
                // empty URL
                url = string.Empty;
            }

            data.Url = url;
        }
    }
}
