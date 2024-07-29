// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

using Bruteforce;
using Bruteforce.TorrentWrapper;
using Bruteforce.TorrentWrapper.Extensions;

Console.WriteLine("Hello, World!");
TorrentInfo.TryLoad(@"C:\Users\nikit\RiderProjects\Bruteforce\Data\orelli.torrent", out var torrent);

var pieces = PersistenceManager.Verify(@"E:\Torrents\fidel_windows_3.1.2", torrent);; // start torrent file

Console.WriteLine($"Похуевило {pieces.Count} частям");

foreach (var piece in pieces)
{
    foreach (var b in piece.Hash.ToByteArrayFromHex())
    {
        Console.Write($"{b} ");
    }
    Console.WriteLine();
    
    Console.WriteLine($"Хуярим часть номер {piece.Index}");
    
    if(!piece.Restoreable)
        Console.WriteLine("Тут одни нули, хуйня выходит");
    
    var sw = Stopwatch.StartNew();

    Console.WriteLine(BruteforceParallel.Bruteforce(piece.Bytes, piece.Hash.ToByteArrayFromHex()));

    sw.Stop();
    Console.WriteLine($"Elapsed: {sw.Elapsed} for {piece.Bytes.Length} bytes");
    
}
