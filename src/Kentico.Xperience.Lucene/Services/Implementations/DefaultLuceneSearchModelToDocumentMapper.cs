using System.Reflection;
using CMS.Core;
using Kentico.Xperience.Lucene.Attributes;
using Kentico.Xperience.Lucene.Models;
using Lucene.Net.Documents;
using Newtonsoft.Json;

namespace Kentico.Xperience.Lucene.Services.Implementations
{
    /// <summary>
    /// Default mapper based on attributes.
    /// </summary>
    public class DefaultLuceneSearchModelToDocumentMapper : ILuceneSearchModelToDocumentMapper
    {
        private readonly IEventLogService eventLogService;

        /// <summary>
        /// Default mapper based on attributes.
        /// </summary>
        public DefaultLuceneSearchModelToDocumentMapper(IEventLogService eventLogService) => this.eventLogService = eventLogService;

        /// <inheritdoc />
        public virtual Document MapModelToDocument(LuceneIndex luceneIndex, LuceneSearchModel model)
        {
            var document = new Document();
            MapModelProps(luceneIndex, model, document);
            return document;
        }

        protected void MapModelProps(LuceneIndex luceneIndex, LuceneSearchModel model, Document document)
        {
            var properties = luceneIndex.LuceneSearchModelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                try
                {
                    // use reflcection to get attributes only onnce in static code
                    object? val = prop.GetValue(model);
                    if (val != null)
                    {
                        if (Attribute.IsDefined(prop, typeof(TextFieldAttribute)))
                        {
                            var textFieldAttribute = prop.GetCustomAttributes<TextFieldAttribute>(false).FirstOrDefault();
                            document.Add(new TextField(prop.Name, val?.ToString(), GetStoreFromAttribute(textFieldAttribute)));
                        }
                        if (Attribute.IsDefined(prop, typeof(StringFieldAttribute)))
                        {
                            var stringFieldAttribute = prop.GetCustomAttributes<StringFieldAttribute>(false).FirstOrDefault();
                            document.Add(new StringField(prop.Name, val?.ToString(), GetStoreFromAttribute(stringFieldAttribute)));
                        }
                        else if (Attribute.IsDefined(prop, typeof(Int32FieldAttribute)))
                        {
                            var intFieldAttribute = prop.GetCustomAttributes<Int32FieldAttribute>(false).FirstOrDefault();
                            document.Add(new Int32Field(prop.Name, (int?)val ?? 0, stored: GetStoreFromAttribute(intFieldAttribute)));
                        }
                        else if (Attribute.IsDefined(prop, typeof(Int64FieldAttribute)))
                        {
                            var intFieldAttribute = prop.GetCustomAttributes<Int64FieldAttribute>(false).FirstOrDefault();
                            document.Add(new Int64Field(prop.Name, (long?)val ?? 0, GetStoreFromAttribute(intFieldAttribute)));
                        }
                        else if (Attribute.IsDefined(prop, typeof(SingleFieldAttribute)))
                        {
                            var intFieldAttribute = prop.GetCustomAttributes<SingleFieldAttribute>(false).FirstOrDefault();
                            document.Add(new SingleField(prop.Name, (float?)val ?? 0, GetStoreFromAttribute(intFieldAttribute)));
                        }
                        else if (Attribute.IsDefined(prop, typeof(DoubleFieldAttribute)))
                        {
                            var intFieldAttribute = prop.GetCustomAttributes<DoubleFieldAttribute>(false).FirstOrDefault();
                            document.Add(new DoubleField(prop.Name, (double?)val ?? 0, GetStoreFromAttribute(intFieldAttribute)));
                        }
                    }
                }
                catch (Exception ex)
                {
                    eventLogService.LogException(nameof(DefaultLuceneSearchModelToDocumentMapper), nameof(MapModelProps), ex, 0, $"Failed to map the model to Lucene Document. Index '{luceneIndex.IndexName}' model '{JsonConvert.SerializeObject(model)}.'");
                }
            }

        }

        private Field.Store GetStoreFromAttribute(BaseFieldAttribute? attribute) => attribute != null && attribute.Store ? Field.Store.YES : Field.Store.NO;

    }
}
