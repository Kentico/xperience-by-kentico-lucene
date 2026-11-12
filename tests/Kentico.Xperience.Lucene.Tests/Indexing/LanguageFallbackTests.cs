using CMS.Core;
using CMS.Tests;

using Kentico.Xperience.Lucene.Admin;
using Kentico.Xperience.Lucene.Core.Indexing;

using Lucene.Net.Util;

using Microsoft.Extensions.Hosting;

namespace Kentico.Xperience.Lucene.Tests.Indexing;

[TestFixture]
public class LanguageFallbackTests : UnitTests
{
    protected override void RegisterTestServices()
    {
        base.RegisterTestServices();
        Service.Use<IHostEnvironment>(Substitute.For<IHostEnvironment>());
    }


    [Test]
    public void NewIndex_UsesLanguageFallbacksByDefault()
    {
        var indexModel = new LuceneIndexModel();
        var configuration = new LuceneConfigurationModel();

        Assert.Multiple(() =>
        {
            Assert.That(indexModel.UseLanguageFallbacks, Is.True);
            Assert.That(configuration.UseLanguageFallbacks, Is.True);
        });
    }


    [TestCase(true)]
    [TestCase(false)]
    public void ConfigurationRoundTrip_PreservesLanguageFallbackSetting(bool useLanguageFallbacks)
    {
        var indexModel = new LuceneIndexModel
        {
            UseLanguageFallbacks = useLanguageFallbacks
        };

        var configuration = new LuceneConfigurationModel(indexModel);
        var mappedModel = configuration.ToLuceneModel();

        Assert.Multiple(() =>
        {
            Assert.That(configuration.UseLanguageFallbacks, Is.EqualTo(useLanguageFallbacks));
            Assert.That(mappedModel.UseLanguageFallbacks, Is.EqualTo(useLanguageFallbacks));
        });
    }


    [TestCase(true)]
    [TestCase(false)]
    public void RuntimeIndex_UsesConfiguredLanguageFallbackSetting(bool useLanguageFallbacks)
    {
        var indexModel = new LuceneIndexModel
        {
            IndexName = "LanguageFallbackTestIndex",
            UseLanguageFallbacks = useLanguageFallbacks
        };

        var index = new LuceneIndex(indexModel, [], [], LuceneVersion.LUCENE_48);
        using var analyzer = index.LuceneAnalyzer;

        Assert.That(index.UseLanguageFallbacks, Is.EqualTo(useLanguageFallbacks));
    }
}
