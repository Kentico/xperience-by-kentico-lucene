using Kentico.Xperience.Lucene.Core.Indexing;

namespace Kentico.Xperience.Lucene.Tests.Indexing;

[TestFixture]
public class LuceneIndexModelTests
{
    [TestCase("")]
    [TestCase("   ")]
    public void DisplayName_WhenBlank_FallsBackToIndexName(string stored)
    {
        var model = new LuceneIndexModel { IndexName = "Legacy", DisplayName = stored };

        Assert.That(model.DisplayName, Is.EqualTo("Legacy"));
    }


    [Test]
    public void DisplayName_WhenSet_IsReturned()
    {
        var model = new LuceneIndexModel { IndexName = "Legacy", DisplayName = "Pretty name" };

        Assert.That(model.DisplayName, Is.EqualTo("Pretty name"));
    }
}
