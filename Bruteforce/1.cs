// // See https://aka.ms/new-console-template for more information
//
// using System.Diagnostics;
// using System.Runtime.CompilerServices;
// using System.Runtime.Intrinsics.Arm;
// using System.Security.Cryptography;
//
// Console.WriteLine("Hello, World!");
// var originalBin = File.ReadAllBytes("C:\\Users\\nikit\\Downloads\\Telegram Desktop\\piece_563");
// var originalHash = StringToByteArray("234210f5c309c17584bec0e70d78f23b06eef7ac");
//
// var sw = Stopwatch.StartNew();
//
// var threads = new List<Thread>();
//
// for (var i = 0; i < 8; i++)
// {
//     var threadBin = new byte[originalBin.Length];
//     originalBin.CopyTo(threadBin, 0);
//
//     var i1 = i;
//     var thread = new Thread(() => FindBitrot(threadBin, originalHash, i1));
//     thread.Start();
//     threads.Add(thread);
// }
//
// threads.WaitAll();
//
// sw.Stop();
// Console.WriteLine($"Elapsed: {sw.Elapsed} for {originalBin.Length} bytes");
// foreach (var b in originalHash)
// {
//     Console.Write($"{b} ");
// }
//
// static byte[] StringToByteArray(string hex) {
//     return Enumerable.Range(0, hex.Length)
//         .Where(x => x % 2 == 0)
//         .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
//         .ToArray();
// }
//
// void FindBitrot(byte[] bytes, byte[] originalHash, int index)
// {
//
//     bytes[(bytes.Length * index) >> 3] = (byte)(bytes[0] ^ 0b0000_0001);
//
//     for (var i = (bytes.Length * index) + 1; i < bytes.Length * (index + 1); i++)
//     {
//         bytes[(i - 1) >> 3] = (byte)(bytes[(i - 1) >> 3] ^ 1 << ((i - 1) % 8));
//         bytes[i >> 3] = (byte)(bytes[i >> 3] ^ 1 << ((i) % 8));
//
//         var newHash = SHA1.HashData(bytes);
//         var areEqual = true;
//
//         for (var j = 0; j < originalHash.Length; j++)
//         {
//             areEqual &= originalHash[j] == newHash[j];
//         }
//
//         if (areEqual)
//         {
//             Console.WriteLine($"Stop at {i}");
//             break;
//         }
//     }
// }
//
// public static class ThreadExtension {
//     public static void WaitAll (this IEnumerable<Thread> threads) {
//         if (threads != null) {
//             foreach (Thread thread in threads) {
//                 thread.Join();
//             }
//         }
//     }
// }
