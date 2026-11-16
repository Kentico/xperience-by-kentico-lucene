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
    public void Constructor_WithoutDisplayName_UsesChannelNameAsDisplayName()
    {
        var path = new LuceneIndexIncludedPath("/");

        var configuration = new LuceneIndexChannelConfiguration("DancingGoatPages", [path]);

        Assert.Multiple(() =>
        {
            Assert.That(configuration.WebsiteChannelName, Is.EqualTo("DancingGoatPages"));
            Assert.That(configuration.ChannelDisplayName, Is.EqualTo("DancingGoatPages"));
            Assert.That(configuration.IncludedPaths, Is.EquivalentTo([path]));
        });
    }

    [Test]
    public void Constructor_WithoutIncludedPaths_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new LuceneIndexChannelConfiguration("DancingGoatPages", "Dancing Goat", []));
    }

    [Test]
    public void Constructor_WithoutDisplayNameAndIncludedPaths_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new LuceneIndexChannelConfiguration("DancingGoatPages", []));
    }

    [Test]
    public void Constructor_WithoutChannelName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new LuceneIndexChannelConfiguration(" ", [new LuceneIndexIncludedPath("/")]));
    }
}
