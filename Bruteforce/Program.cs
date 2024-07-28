// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;

using Bruteforce;

Console.WriteLine("Hello, World!");

var originalBin = File.ReadAllBytes("C:\\Users\\nikit\\Downloads\\Telegram Desktop\\piece_563");
var originalHash = Utility.StringToByteArray("234210f5c309c17584bec0e70d78f23b06eef7ac");

var sw = Stopwatch.StartNew();



originalBin[0] = (byte)(originalBin[0] ^ 0b0000_0001);

for (int i = 1; i < originalBin.Length * sizeof(byte); i++)
{
    originalBin[(i - 1) >> 3] = (byte)(originalBin[(i - 1) >> 3] ^ 1 << ((i - 1) % sizeof(byte)));
    originalBin[i >> 3] = (byte)(originalBin[i >> 3] ^ 1 << ((i) % sizeof(byte)));

    var newHash = SHA1.HashData(originalBin);
    bool areEqual = true;

    for (var j = 0; j < originalHash.Length; j++)
    {
        areEqual &= originalHash[j] == newHash[j];
    }


    if (areEqual)
    {
        Console.WriteLine($"Stop at {i}");
        break;
    }
}

sw.Stop();
Console.WriteLine($"Elapsed: {sw.Elapsed} for {originalBin.Length} bytes");
foreach (var b in originalHash)
{
    Console.Write($"{b} ");
}
