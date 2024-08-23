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


    public static void Copy(string sourceDirectory, string targetDirectory)
    {
        var diSource = new DirectoryInfo(sourceDirectory);
        var diTarget = new DirectoryInfo(targetDirectory);

        CopyAll(diSource, diTarget);
    }

    public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
    {
        Directory.CreateDirectory(target.FullName);

        // Copy each file into the new directory.
        foreach (var fi in source.GetFiles())
            fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);

        // Copy each subdirectory using recursion.
        foreach (var diSourceSubDir in source.GetDirectories())
        {
            var nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);

            CopyAll(diSourceSubDir, nextTargetSubDir);
        }
    }
}

public static class ThreadExtension
{
    public static void WaitAll(this IEnumerable<Thread> threads)
    {
        foreach (var thread in threads)
            thread.Join();
    }
}
