using Kentico.Xperience.Lucene.Core.Indexing;

namespace Kentico.Xperience.Lucene.Tests.Indexing;

[TestFixture]
public class LuceneIndexChannelConfigurationTests
{
    [Test]
    public void Constructor_WithIncludedPath_StoresChannelAndPath()
    {
        var path = new LuceneIndexIncludedPath("/");

        var configuration = new LuceneIndexChannelConfiguration(
            "DancingGoatPages",
            "Dancing Goat",
            [path]);

        Assert.Multiple(() =>
        {
            Assert.That(configuration.WebsiteChannelName, Is.EqualTo("DancingGoatPages"));
            Assert.That(configuration.ChannelDisplayName, Is.EqualTo("Dancing Goat"));
            Assert.That(configuration.IncludedPaths, Is.EquivalentTo([path]));
        });
    }

    [Test]
    public void Constructor_WithoutIncludedPaths_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new LuceneIndexChannelConfiguration("DancingGoatPages", "Dancing Goat", []));
    }
}
