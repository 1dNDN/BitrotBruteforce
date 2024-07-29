using System.Security.Cryptography;

namespace Bruteforce.TorrentWrapper.Extensions;

/// <summary>
///     The cryptography extensions.
/// </summary>
public static class CryptoExtensions
{
    /// <summary>
    ///     Calculates the 128 bit SHA hash.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="length">The length.</param>
    /// <returns>The 128 bit SHA hash.</returns>
    public static byte[] CalculateSha1Hash(this byte[] data, int offset, int length) =>
        CalculateHashSha(data, offset, length);

    /// <summary>
    ///     Calculates the 128 bit SHA hash.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <returns>
    ///     The 128 bit SHA hash.
    /// </returns>
    public static byte[] CalculateSha1Hash(this byte[] data) =>
        CalculateHashSha(data, 0, data.Length);

    /// <summary>
    ///     Calculates the hash of a data using SHA hash algorithm.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="length">The length.</param>
    /// <returns>
    ///     Calculated hash
    /// </returns>
    private static byte[] CalculateHashSha(this byte[] data, int offset, int length)
    {
        return SHA1.HashData(data.AsSpan(offset, length));
    }
}
