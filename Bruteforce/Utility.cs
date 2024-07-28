using System.Security.Cryptography;

namespace Bruteforce;

public class Utility
{
    public static bool IsEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
            return false;

        var areEqual = true;

        for (var j = 0; j < left.Length; j++)
            areEqual &= left[j] == right[j];

        return areEqual;
    }

    public static byte[] StringToByteArray(string hex) =>
        Enumerable.Range(0, hex.Length)
            .Where(x => x % 2 == 0)
            .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
            .ToArray();

    public static byte[] GetHash(byte[] data) =>
        SHA1.HashData(data);
}

public static class ThreadExtension
{
    public static void WaitAll(this IEnumerable<Thread> threads)
    {
        foreach (var thread in threads)
            thread.Join();
    }
}
