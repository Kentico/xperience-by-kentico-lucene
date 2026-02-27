namespace Kentico.Xperience.Lucene.Core.Store;

/// <summary>
/// Simple CRC32 implementation for checksum calculation.
/// </summary>
internal class Crc32
{
    private static readonly uint[] table = CreateTable();
    private uint crc = 0xFFFFFFFF;

    public long Value => crc ^ 0xFFFFFFFF;

    public void Update(byte[] buffer, int offset, int length)
    {
        for (int i = offset; i < offset + length; i++)
        {
            crc = (crc >> 8) ^ table[(crc ^ buffer[i]) & 0xFF];
        }
    }

    public void Reset() => crc = 0xFFFFFFFF;

    private static uint[] CreateTable()
    {
        var resultTable = new uint[256];
        const uint polynomial = 0xEDB88320;

        for (uint i = 0; i < 256; i++)
        {
            uint entry = i;
            for (int j = 0; j < 8; j++)
            {
                if ((entry & 1) == 1)
                {
                    entry = (entry >> 1) ^ polynomial;
                }
                else
                {
                    entry >>= 1;
                }
            }
            resultTable[i] = entry;
        }

        return resultTable;
    }
}
