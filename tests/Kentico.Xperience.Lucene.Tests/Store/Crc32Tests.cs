using Kentico.Xperience.Lucene.Core.Store;

namespace Kentico.Xperience.Lucene.Tests.Store;

[TestFixture]
public class Crc32Tests
{
    [Test]
    public void InitialValue_IsZero()
    {
        var crc = new Crc32();

        Assert.That(crc.Value, Is.EqualTo(0));
    }


    [Test]
    public void Update_WithKnownInput_ProducesExpectedChecksum()
    {
        var crc = new Crc32();
        byte[] data = "123456789"u8.ToArray();

        crc.Update(data, 0, data.Length);

        // CRC32 of "123456789" is a well-known test value: 0xCBF43926
        Assert.That(crc.Value, Is.EqualTo(0xCBF43926L));
    }


    [Test]
    public void Update_WithEmptyInput_DoesNotChangeValue()
    {
        var crc = new Crc32();
        byte[] data = [];

        crc.Update(data, 0, 0);

        Assert.That(crc.Value, Is.EqualTo(0));
    }


    [Test]
    public void Update_WithSameDataTwice_ProducesDifferentChecksum()
    {
        var crc = new Crc32();
        byte[] data = "hello"u8.ToArray();

        crc.Update(data, 0, data.Length);
        long firstChecksum = crc.Value;

        crc.Update(data, 0, data.Length);
        long secondChecksum = crc.Value;

        Assert.That(secondChecksum, Is.Not.EqualTo(firstChecksum));
    }


    [Test]
    public void Reset_AfterUpdate_ResetsToInitialValue()
    {
        var crc = new Crc32();
        byte[] data = "hello"u8.ToArray();

        crc.Update(data, 0, data.Length);
        long afterUpdate = crc.Value;
        crc.Reset();

        Assert.That(crc.Value, Is.EqualTo(0));
        Assert.That(crc.Value, Is.Not.EqualTo(afterUpdate));
    }


    [Test]
    public void Update_WithOffset_OnlyProcessesDataFromOffset()
    {
        var crc1 = new Crc32();
        var crc2 = new Crc32();
        byte[] data = [0x00, 0x01, 0x02, 0x03, 0x04];

        // Process only bytes [1,2,3] using offset
        crc1.Update(data, 1, 3);

        // Process same bytes [1,2,3] directly
        byte[] subset = [0x01, 0x02, 0x03];
        crc2.Update(subset, 0, 3);

        Assert.That(crc1.Value, Is.EqualTo(crc2.Value));
    }


    [Test]
    public void Value_ForSingleZeroByte_IsCorrect()
    {
        var crc = new Crc32();
        byte[] data = [0x00];

        crc.Update(data, 0, 1);

        // CRC32 of a single 0x00 byte
        Assert.That(crc.Value, Is.EqualTo(0xD202EF8DL));
    }
}
