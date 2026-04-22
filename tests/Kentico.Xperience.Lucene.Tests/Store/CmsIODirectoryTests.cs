using Kentico.Xperience.Lucene.Core.Store;

namespace Kentico.Xperience.Lucene.Tests.Store;

[TestFixture]
public class CmsIODirectoryRestoreCodecNameCaseTests
{
    [TestCase("_a_lucene41_0.doc", "_a_Lucene41_0.doc")]
    [TestCase("_a_lucene40_0.doc", "_a_Lucene40_0.doc")]
    [TestCase("_b_lucene45_0.dvd", "_b_Lucene45_0.dvd")]
    [TestCase("_c_lucene42_0.dvm", "_c_Lucene42_0.dvm")]
    [TestCase("_a_LUCENE41_0.doc", "_a_Lucene41_0.doc")]
    public void RestoreCodecNameCase_KnownCodecName_RestoresCanonicalCasing(string input, string expected)
    {
        string result = CmsIODirectory.RestoreCodecNameCase(input);

        Assert.That(result, Is.EqualTo(expected));
    }


    [TestCase("segments_1")]
    [TestCase("segments.gen")]
    [TestCase("write.lock")]
    [TestCase("_a.cfe")]
    [TestCase("_a.cfs")]
    [TestCase("_b.si")]
    public void RestoreCodecNameCase_FilesWithoutCodecName_AreUnchanged(string input)
    {
        string result = CmsIODirectory.RestoreCodecNameCase(input);

        Assert.That(result, Is.EqualTo(input));
    }


    [TestCase("_a_unknownformat_0.doc")]
    [TestCase("_b_mycodec_1.dvd")]
    public void RestoreCodecNameCase_UnknownCodecName_IsUnchanged(string input)
    {
        string result = CmsIODirectory.RestoreCodecNameCase(input);

        Assert.That(result, Is.EqualTo(input));
    }
}
