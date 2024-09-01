using System.Runtime.InteropServices;
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
}

//unsigned char *SHA1(const unsigned char *d, size_t n, unsigned char *md)
// __attribute__ ((__bounded__(__buffer__, 1, 2)));
// 11,23 ГБ/с
public static class LibreSSLDesktop
{
    [DllImport("libcrypto_libressl_onecore.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SHA1(byte[] d, int n, byte[] md);
}

// 12,02 ГБ/с
public static class LibreSSLOnecore
{
    [DllImport("libcrypto_libressl_desktop.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SHA1(byte[] d, int n, byte[] md);
}

// 7,55 ГБ/с
public static class LibreSSL
{
    [DllImport("libcrypto_libre.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SHA1(byte[] d, int n, byte[] md);
}

// 32,74 ГБ/с
public static class OpenSSLMingw
{
    [DllImport("libcrypto-3-x64_mingw_openssl.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SHA1(byte[] d, int n, byte[] md);
}

// 31,04 ГБ/с
public static class OpenSSLMingwNew
{
    [DllImport("libcrypto-3-x64_mingw_new.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SHA1(byte[] d, int n, byte[] md);
}

// 32,71 ГБ/с
public static class OpenSSLCl
{
    [DllImport("libcrypto-3-x64_cl.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SHA1(byte[] d, int n, byte[] md);
}

// 30.52 ГБ/с
public static class GnuTLS
{
    // Function: int gnutls_hash_fast (gnutls_digest_algorithm_t algorithm, const void * ptext, size_t ptext_len, void * digest)
    // algorithm: the hash algorithm to use
    // ptext: the data to hash
    // ptext_len: the length of data to hash
    // digest: is the output value of the hash
    
    // This convenience function will hash the given data and return output on a single call.
    //
    // Returns: Zero or a negative error code on error.

    [DllImport("libgnutls-30_mingw", CallingConvention = CallingConvention.Cdecl)]
    public static extern int gnutls_hash_fast(GnuTLS_Digest_Algorithm algorithm, byte[] ptext, int ptext_len, byte[] digest);

    public static int SHA1(byte[] data, int dataLength, byte[] digest)
    {
        return gnutls_hash_fast(GnuTLS_Digest_Algorithm.GNUTLS_DIG_SHA1, data, dataLength, digest);
    }

    public enum GnuTLS_Digest_Algorithm
    {
        GNUTLS_DIG_SHA1 = 3
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
