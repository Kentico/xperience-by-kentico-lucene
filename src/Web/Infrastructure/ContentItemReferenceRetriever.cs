using CMS.ContentEngine;
using CMS.DataEngine.Internal;

using System.Collections.Generic;

namespace DancingGoat.Infrastructure
{
    public class ContentItemReferenceRetriever
    {
        public IEnumerable<ContentItemReference> Retrieve(string serializedReferences)
        {
            // temporary retrieve image references; will be replaced with implementation from PL team
            return JsonDataTypeConverter.ConvertToModels<ContentItemReference>(serializedReferences);
        }
    }
}
