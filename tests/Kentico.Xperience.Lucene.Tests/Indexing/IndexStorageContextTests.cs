using Kentico.Xperience.Lucene.Core.Indexing;

namespace Kentico.Xperience.Lucene.Tests.Indexing;

[TestFixture]
public class IndexStorageContextTests
{
    private ILuceneIndexStorageStrategy strategy = null!;
    private const string IndexRoot = "~/App_Data/LuceneSearch/TestIndex";


    [SetUp]
    public void SetUp()
    {
        strategy = Substitute.For<ILuceneIndexStorageStrategy>();

        strategy.FormatPath(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<bool>())
            .Returns(args => $"{args[0]}/i-g{(int)args[1]:0000000}-p_{(bool)args[2]}");

        strategy.FormatTaxonomyPath(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<bool>())
            .Returns(args => $"{args[0]}/i-g{(int)args[1]:0000000}-p_{(bool)args[2]}_taxonomy");
    }


    [Test]
    public void GetPublishedIndex_NoExistingIndices_ReturnsDefaultPublishedGeneration1()
    {
        strategy.GetExistingIndices(Arg.Any<string>())
            .Returns(Enumerable.Empty<IndexStorageModel>());

        var context = CreateContext();

        var result = context.GetPublishedIndex();

        Assert.Multiple(() =>
        {
            Assert.That(result.Generation, Is.EqualTo(1));
            Assert.That(result.IsPublished, Is.True);
        });
    }


    [Test]
    public void GetPublishedIndex_WithMultiplePublishedIndices_ReturnsHighestGeneration()
    {
        var indices = new[]
        {
            new IndexStorageModel($"{IndexRoot}/i-g0000001-p_True", $"{IndexRoot}/i-g0000001-p_True_taxonomy", 1, true),
            new IndexStorageModel($"{IndexRoot}/i-g0000002-p_True", $"{IndexRoot}/i-g0000002-p_True_taxonomy", 2, true),
            new IndexStorageModel($"{IndexRoot}/i-g0000003-p_True", $"{IndexRoot}/i-g0000003-p_True_taxonomy", 3, true),
        };

        strategy.GetExistingIndices(Arg.Any<string>())
            .Returns(indices);

        var context = CreateContext();

        var result = context.GetPublishedIndex();

        Assert.That(result.Generation, Is.EqualTo(3));
    }


    [Test]
    public void GetPublishedIndex_WithMixedPublishedAndUnpublished_ReturnsHighestPublished()
    {
        var indices = new[]
        {
            new IndexStorageModel($"{IndexRoot}/i-g0000001-p_True", $"{IndexRoot}/i-g0000001-p_True_taxonomy", 1, true),
            new IndexStorageModel($"{IndexRoot}/i-g0000002-p_True", $"{IndexRoot}/i-g0000002-p_True_taxonomy", 2, true),
            new IndexStorageModel($"{IndexRoot}/i-g0000003-p_False", $"{IndexRoot}/i-g0000003-p_False_taxonomy", 3, false),
        };

        strategy.GetExistingIndices(Arg.Any<string>())
            .Returns(indices);

        var context = CreateContext();

        var result = context.GetPublishedIndex();

        Assert.Multiple(() =>
        {
            Assert.That(result.Generation, Is.EqualTo(2));
            Assert.That(result.IsPublished, Is.True);
        });
    }


    [Test]
    public void GetNextGeneration_NoExistingIndices_ReturnsGeneration1Unpublished()
    {
        strategy.GetExistingIndices(Arg.Any<string>())
            .Returns(Enumerable.Empty<IndexStorageModel>());

        var context = CreateContext();

        var result = context.GetNextGeneration();

        Assert.Multiple(() =>
        {
            Assert.That(result.Generation, Is.EqualTo(1));
            Assert.That(result.IsPublished, Is.False);
        });
    }


    [Test]
    public void GetNextGeneration_WithLastPublishedIndex_ReturnsNextGenerationUnpublished()
    {
        var indices = new[]
        {
            new IndexStorageModel($"{IndexRoot}/i-g0000002-p_True", $"{IndexRoot}/i-g0000002-p_True_taxonomy", 2, true),
        };

        strategy.GetExistingIndices(Arg.Any<string>())
            .Returns(indices);

        var context = CreateContext();

        var result = context.GetNextGeneration();

        Assert.Multiple(() =>
        {
            Assert.That(result.Generation, Is.EqualTo(3));
            Assert.That(result.IsPublished, Is.False);
        });
    }


    [Test]
    public void GetNextGeneration_WithLastUnpublishedIndex_ReturnsSameGenerationUnpublished()
    {
        var indices = new[]
        {
            new IndexStorageModel($"{IndexRoot}/i-g0000002-p_False", $"{IndexRoot}/i-g0000002-p_False_taxonomy", 2, false),
        };

        strategy.GetExistingIndices(Arg.Any<string>())
            .Returns(indices);

        var context = CreateContext();

        var result = context.GetNextGeneration();

        Assert.Multiple(() =>
        {
            Assert.That(result.Generation, Is.EqualTo(2));
            Assert.That(result.IsPublished, Is.False);
        });
    }


    [Test]
    public void GetLastGeneration_NoExistingIndices_ReturnsDefaultWithSpecifiedPublishedState()
    {
        strategy.GetExistingIndices(Arg.Any<string>())
            .Returns(Enumerable.Empty<IndexStorageModel>());

        var context = CreateContext();

        var resultPublished = context.GetLastGeneration(true);
        var resultUnpublished = context.GetLastGeneration(false);

        Assert.Multiple(() =>
        {
            Assert.That(resultPublished.Generation, Is.EqualTo(1));
            Assert.That(resultPublished.IsPublished, Is.True);
            Assert.That(resultUnpublished.Generation, Is.EqualTo(1));
            Assert.That(resultUnpublished.IsPublished, Is.False);
        });
    }


    [Test]
    public void GetLastGeneration_WithExistingIndices_ReturnsHighestGeneration()
    {
        var indices = new[]
        {
            new IndexStorageModel($"{IndexRoot}/i-g0000001-p_True", $"{IndexRoot}/i-g0000001-p_True_taxonomy", 1, true),
            new IndexStorageModel($"{IndexRoot}/i-g0000005-p_True", $"{IndexRoot}/i-g0000005-p_True_taxonomy", 5, true),
            new IndexStorageModel($"{IndexRoot}/i-g0000003-p_False", $"{IndexRoot}/i-g0000003-p_False_taxonomy", 3, false),
        };

        strategy.GetExistingIndices(Arg.Any<string>())
            .Returns(indices);

        var context = CreateContext();

        var result = context.GetLastGeneration(false);

        Assert.That(result.Generation, Is.EqualTo(5));
    }


    [Test]
    public void GetNextOrOpenNextGeneration_NoExistingIndices_ReturnsGeneration1Unpublished()
    {
        strategy.GetExistingIndices(Arg.Any<string>())
            .Returns(Enumerable.Empty<IndexStorageModel>());

        var context = CreateContext();

        var result = context.GetNextOrOpenNextGeneration();

        Assert.Multiple(() =>
        {
            Assert.That(result.Generation, Is.EqualTo(1));
            Assert.That(result.IsPublished, Is.False);
        });
    }


    [Test]
    public void GetNextOrOpenNextGeneration_LastIsUnpublished_ReturnsSameGeneration()
    {
        var indices = new[]
        {
            new IndexStorageModel($"{IndexRoot}/i-g0000003-p_False", $"{IndexRoot}/i-g0000003-p_False_taxonomy", 3, false),
        };

        strategy.GetExistingIndices(Arg.Any<string>())
            .Returns(indices);

        var context = CreateContext();

        var result = context.GetNextOrOpenNextGeneration();

        Assert.Multiple(() =>
        {
            Assert.That(result.Generation, Is.EqualTo(3));
            Assert.That(result.IsPublished, Is.False);
        });
    }


    [Test]
    public void GetNextOrOpenNextGeneration_LastIsPublished_ReturnsNextGeneration()
    {
        var indices = new[]
        {
            new IndexStorageModel($"{IndexRoot}/i-g0000002-p_True", $"{IndexRoot}/i-g0000002-p_True_taxonomy", 2, true),
        };

        strategy.GetExistingIndices(Arg.Any<string>())
            .Returns(indices);

        var context = CreateContext();

        var result = context.GetNextOrOpenNextGeneration();

        Assert.Multiple(() =>
        {
            Assert.That(result.Generation, Is.EqualTo(3));
            Assert.That(result.IsPublished, Is.False);
        });
    }


    [Test]
    public void PublishIndex_DelegatesToStrategy()
    {
        var storageModel = new IndexStorageModel($"{IndexRoot}/i-g0000001-p_False", $"{IndexRoot}/i-g0000001-p_False_taxonomy", 1, false);

        var context = CreateContext();

        context.PublishIndex(storageModel);

        strategy.Received(1).PublishIndex(storageModel);
    }


    [Test]
    public void EnforceRetentionPolicy_WithSufficientGenerations_RemovesOldOnes()
    {
        var indices = new[]
        {
            new IndexStorageModel($"{IndexRoot}/i-g0000001-p_True", $"{IndexRoot}/i-g0000001-p_True_taxonomy", 1, true),
            new IndexStorageModel($"{IndexRoot}/i-g0000002-p_True", $"{IndexRoot}/i-g0000002-p_True_taxonomy", 2, true),
            new IndexStorageModel($"{IndexRoot}/i-g0000003-p_True", $"{IndexRoot}/i-g0000003-p_True_taxonomy", 3, true),
        };

        strategy.GetExistingIndices(Arg.Any<string>())
            .Returns(indices);
        strategy.ScheduleRemoval(Arg.Any<IndexStorageModel>()).Returns(true);

        // Keep only 1 published generation
        var context = new IndexStorageContext(strategy, IndexRoot, new IndexRetentionPolicy(1));

        context.EnforceRetentionPolicy();

        // Generations 1 and 2 should be scheduled for removal, generation 3 is kept
        strategy.Received(1).ScheduleRemoval(Arg.Is<IndexStorageModel>(m => m.Generation == 1));
        strategy.Received(1).ScheduleRemoval(Arg.Is<IndexStorageModel>(m => m.Generation == 2));
        strategy.DidNotReceive().ScheduleRemoval(Arg.Is<IndexStorageModel>(m => m.Generation == 3));
    }


    [Test]
    public void EnforceRetentionPolicy_KeepsTwoGenerations_RemovesOldest()
    {
        var indices = new[]
        {
            new IndexStorageModel($"{IndexRoot}/i-g0000001-p_True", $"{IndexRoot}/i-g0000001-p_True_taxonomy", 1, true),
            new IndexStorageModel($"{IndexRoot}/i-g0000002-p_True", $"{IndexRoot}/i-g0000002-p_True_taxonomy", 2, true),
            new IndexStorageModel($"{IndexRoot}/i-g0000003-p_True", $"{IndexRoot}/i-g0000003-p_True_taxonomy", 3, true),
        };

        strategy.GetExistingIndices(Arg.Any<string>())
            .Returns(indices);
        strategy.ScheduleRemoval(Arg.Any<IndexStorageModel>()).Returns(true);

        // Keep 2 published generations
        var context = new IndexStorageContext(strategy, IndexRoot, new IndexRetentionPolicy(2));

        context.EnforceRetentionPolicy();

        // Only generation 1 should be scheduled for removal
        strategy.Received(1).ScheduleRemoval(Arg.Is<IndexStorageModel>(m => m.Generation == 1));
        strategy.DidNotReceive().ScheduleRemoval(Arg.Is<IndexStorageModel>(m => m.Generation == 2));
        strategy.DidNotReceive().ScheduleRemoval(Arg.Is<IndexStorageModel>(m => m.Generation == 3));
    }


    [Test]
    public async Task DeleteIndex_DelegatesToStrategy()
    {
        strategy.DeleteIndex(Arg.Any<string>()).Returns(Task.FromResult(true));

        var context = CreateContext();

        var result = await context.DeleteIndex();

        Assert.That(result, Is.True);
        await strategy.Received(1).DeleteIndex(IndexRoot);
    }


    private IndexStorageContext CreateContext(int retainedGenerations = 1) =>
        new(strategy, IndexRoot, new IndexRetentionPolicy(retainedGenerations));
}
