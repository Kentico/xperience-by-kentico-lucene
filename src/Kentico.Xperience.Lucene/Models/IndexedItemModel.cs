namespace Kentico.Xperience.Lucene.Models
{
    public class IndexedItemModel
    {
        public string LanguageName { get; set; }
        public string TypeName { get; set; }
        public string ChannelName { get; set; }
        public Guid WebPageItemGuid { get; set; }
        public string WebPageItemTreePath { get; set; }
    }
}
