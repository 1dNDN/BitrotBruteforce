using System.Security.Cryptography;

namespace Bruteforce;

public class BrokenPiece(byte[] bytes, long index, string hash)
{
    public byte[] Bytes { get; set; } = bytes;

    public long Index { get; set; } = index;

    public string Hash { get; } = hash;

    public bool Restoreable => Bytes.Any(b => b != 0);
}
