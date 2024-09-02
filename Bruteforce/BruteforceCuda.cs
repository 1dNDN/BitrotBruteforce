using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static Bruteforce.Utility;

namespace Bruteforce;

public partial class BruteforceCuda
{
    // CudaBitrotFinder.dll
    // void __declspec(dllexport) bruteforceBits(unsigned char* pieceData, unsigned char* pieceHash, size_t pieceSize, unsigned int* result)
    // В result попадает индекс бита, который нужно флипнуть, либо 4294967295 (-1 в unsigned) если хеш-сумма не найдена
    [LibraryImport("CudaBitrotFinder")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void bruteforceBits(byte[] pieceData, byte[] pieceHash, int pieceSize, ref uint result);

    public static int Bruteforce(byte[] data, byte[] hash)
    {
        if (IsEqual(hash, GetHash(data)))
            return -2;

        var result = uint.MaxValue;

        bruteforceBits(data, hash, data.Length, ref result);

        if (result == uint.MaxValue)
            return -3;

        return (int)result;
    }
}
