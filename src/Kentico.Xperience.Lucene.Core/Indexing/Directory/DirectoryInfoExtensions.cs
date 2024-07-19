using System.Collections.Concurrent;
using System.Text;

using DirectoryInfo = CMS.IO.DirectoryInfo;
using FileInfo = CMS.IO.FileInfo;
using Path = CMS.IO.Path;

namespace Kentico.Xperience.Lucene.Core.Indexing;
public static class DirectoryInfoExtensions
{
    private static readonly ConcurrentDictionary<string, string> fileCanonPathCache = new();

    public static string GetCanonicalPath(this FileInfo path)
    {
        string absPath = path.FullName; // LUCENENET NOTE: This internally calls GetFullPath(), which resolves relative paths
        byte[] result = Encoding.UTF8.GetBytes(absPath);

        if (fileCanonPathCache.TryGetValue(absPath, out string? canonPath) && canonPath != null)
        {
            return canonPath;
        }

        int numSeparators = 1;
        for (int i = 0; i < result.Length; i++)
        {
            if (result[i] == Path.DirectorySeparatorChar)
            {
                numSeparators++;
            }
        }
        int[] sepLocations = new int[numSeparators];
        int rootLoc = 0;
        if (Path.DirectorySeparatorChar == '\\')
        {
            if (result[0] == '\\')
            {
                rootLoc = (result.Length > 1 && result[1] == '\\') ? 1 : 0;
            }
            else
            {
                rootLoc = 2; // skip drive i.e. c:
            }
        }
        byte[] newResult = new byte[result.Length + 1];
        int newLength = 0, lastSlash = 0, foundDots = 0;
        sepLocations[lastSlash] = rootLoc;
        for (int i = 0; i <= result.Length; i++)
        {
            if (i < rootLoc)
            {
                newResult[newLength++] = (byte)char.ToUpperInvariant((char)result[i]);
            }
            else
            {
                if (i == result.Length || result[i] == Path.DirectorySeparatorChar)
                {
                    if (i == result.Length && foundDots == 0)
                    {
                        break;
                    }
                    if (foundDots == 1)
                    {
                        foundDots = 0;
                        continue;
                    }
                    if (foundDots > 1)
                    {
                        lastSlash = lastSlash > (foundDots - 1) ? lastSlash
                                - (foundDots - 1) : 0;
                        newLength = sepLocations[lastSlash] + 1;
                        foundDots = 0;
                        continue;
                    }
                    sepLocations[++lastSlash] = newLength;
                    newResult[newLength++] = (byte)Path.DirectorySeparatorChar;
                    continue;
                }
                if (result[i] == '.')
                {
                    foundDots++;
                    continue;
                }
                if (foundDots > 0)
                {
                    for (int j = 0; j < foundDots; j++)
                    {
                        newResult[newLength++] = (byte)'.';
                    }
                }
                newResult[newLength++] = result[i];
                foundDots = 0;
            }
        }
        if (newLength > (rootLoc + 1)
                && newResult[newLength - 1] == Path.DirectorySeparatorChar)
        {
            newLength--;
        }
        newResult[newLength] = 0;
        newLength = newResult.Length;
        canonPath = fileCanonPathCache.GetOrAdd(
            absPath,
            k => Encoding.UTF8.GetString(newResult, 0, newLength).TrimEnd('\0')); // LUCENENET: Eliminate null terminator char
        return canonPath;
    }

    public static string GetCanonicalPath(this DirectoryInfo path)
    {
        string absPath = path.FullName;
        byte[] result = Encoding.UTF8.GetBytes(absPath);

        if (fileCanonPathCache.TryGetValue(absPath, out string? canonPath) && canonPath != null)
        {
            return canonPath;
        }

        int numSeparators = 1;
        for (int i = 0; i < result.Length; i++)
        {
            if (result[i] == Path.DirectorySeparatorChar)
            {
                numSeparators++;
            }
        }
        int[] sepLocations = new int[numSeparators];
        int rootLoc = 0;
        if (Path.DirectorySeparatorChar == '\\')
        {
            if (result[0] == '\\')
            {
                rootLoc = result.Length > 1 && result[1] == '\\' ? 1 : 0;
            }
            else
            {
                rootLoc = 2;
            }
        }
        byte[] newResult = new byte[result.Length + 1];
        int newLength = 0, lastSlash = 0, foundDots = 0;
        sepLocations[lastSlash] = rootLoc;
        for (int i = 0; i <= result.Length; i++)
        {
            if (i < rootLoc)
            {
                newResult[newLength++] = (byte)char.ToUpperInvariant((char)result[i]);
            }
            else
            {
                if (i == result.Length || result[i] == Path.DirectorySeparatorChar)
                {
                    if (i == result.Length && foundDots == 0)
                    {
                        break;
                    }
                    if (foundDots == 1)
                    {
                        foundDots = 0;
                        continue;
                    }
                    if (foundDots > 1)
                    {
                        lastSlash = lastSlash > foundDots - 1 ? lastSlash
                                - (foundDots - 1) : 0;
                        newLength = sepLocations[lastSlash] + 1;
                        foundDots = 0;
                        continue;
                    }
                    sepLocations[++lastSlash] = newLength;
                    newResult[newLength++] = (byte)Path.DirectorySeparatorChar;
                    continue;
                }
                if (result[i] == '.')
                {
                    foundDots++;
                    continue;
                }
                if (foundDots > 0)
                {
                    for (int j = 0; j < foundDots; j++)
                    {
                        newResult[newLength++] = (byte)'.';
                    }
                }
                newResult[newLength++] = result[i];
                foundDots = 0;
            }
        }
        if (newLength > rootLoc + 1
                && newResult[newLength - 1] == Path.DirectorySeparatorChar)
        {
            newLength--;
        }
        newResult[newLength] = 0;
        newLength = newResult.Length;
        canonPath = fileCanonPathCache.GetOrAdd(
            absPath,
            k => Encoding.UTF8.GetString(newResult, 0, newLength).TrimEnd('\0')); // LUCENENET: Eliminate null terminator char
        return canonPath;
    }
}
