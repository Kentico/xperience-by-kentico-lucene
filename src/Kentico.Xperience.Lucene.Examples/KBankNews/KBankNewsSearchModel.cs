using CMS.DocumentEngine;
using Kentico.Xperience.Lucene.Attributes;
using Kentico.Xperience.Lucene.Models;

namespace Kentico.Xperience.Lucene.Examples.KBankNews
{

    // [IncludedPath("/news/%", ContentTypes = new string[] { NewsPage.CLASS_NAME })]
    [IncludedPath("/%", ContentTypes = new string[] { NewsPage_CLASS_NAME })]
    public class KBankNewsSearchModel : LuceneSearchModel
    {
        public const string IndexName = "KBank-News";
        private const string NewsPage_CLASS_NAME = "Kentico.NewsPage";

        [TextField(true)]
        //[ Source(new string[] { nameof(NewsPage.Title), nameof(TreeNode.DocumentName) })]
        [Source(new string[] { nameof(Title), nameof(TreeNode.DocumentName) })]
        public string? Title { get; set; }

        [TextField(true)]
        public string? Summary { get; set; }

        [TextField(false)]
        public string? NewsText { get; set; }

        [TextField(false)]
        public string? AllContent { get; set; }

        [StringField(true)]
        public string? NewsType { get; set; }
    }
}
