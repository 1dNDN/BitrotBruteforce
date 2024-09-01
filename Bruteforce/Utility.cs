using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Bruteforce;

public static class Utility
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
    
    public static byte[] GetHash(byte[] data)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var hash = new byte[20];
        
            OpenSSLCl.SHA1(data, data.Length, hash);
        
            return hash;
        }

        // 7,65 ГБ/с
        return SHA1.HashData(data);
    }
    
    public static void WaitAll(this IEnumerable<Thread> threads)
    {
        foreach (var thread in threads)
            thread.Join();
    }

    public static bool IsRestoreable(this byte[] array)
    {
        return array.Any(b => b != 0);
    }

    public static void FlipBit(this byte[] data, int bitIndex)
    {
        data[bitIndex >> 3] = (byte)(data[bitIndex >> 3] ^ (1 << (bitIndex % 8)));
    }
}

//unsigned char *SHA1(const unsigned char *d, size_t n, unsigned char *md)
// __attribute__ ((__bounded__(__buffer__, 1, 2)));

// 32,71 ГБ/с
public static class OpenSSLCl
{
    [DllImport("libs/libcrypto-3-x64_cl", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SHA1(byte[] d, int n, byte[] md);
}
