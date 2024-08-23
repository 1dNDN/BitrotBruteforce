// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

using Bruteforce;
using Bruteforce.TorrentWrapper;
using Bruteforce.TorrentWrapper.Extensions;


// DoWork(@"C:\Users\nikit\RiderProjects\Bruteforce\orelli2.torrent", @"C:\Users\nikit\RiderProjects\Bruteforce");

// var args = Environment.GetCommandLineArgs();

if (args.Length < 2)
    Console.WriteLine("Ты сначала спиздани, че надо, а потом суету наводи");

var pathToTorrent = args[1];
var pathToDir = args[2];

var doRepair = false;

if(args.Length == 3)
{
    doRepair = args[3] == "y";
}


DoWork(pathToTorrent, pathToDir, doRepair);

void DoWork(string pathToTorrent, string pathToDir, bool doRepair)
{
    TorrentInfo.TryLoad(pathToTorrent, out var torrent);

    if(torrent.Files.Length == 1 && torrent.Files[0].FilePath.Contains("Posobie_dlja_samoubijz"))
        Console.WriteLine("Нихуя себе, сегодня хуярим петуха!");
    else 
        Console.WriteLine("Опять хуйню прислали");
    
    var pieces = PersistenceManager.Verify(pathToDir, torrent);

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
        {
            Console.WriteLine("Тут одни нули, хуйня выходит");
            continue;
        }

        var sw = Stopwatch.StartNew();

        var bitIndex = BruteforceParallel.Bruteforce(piece.Bytes, piece.Hash.ToByteArrayFromHex());
        Console.WriteLine(bitIndex);

        if (doRepair && bitIndex > 0)
        {
            PersistenceManager.FlipBit(pathToDir, torrent, piece.Index, bitIndex);
        }
        
        sw.Stop();
        Console.WriteLine($"Прохуярило: {sw.Elapsed} времени на {piece.Bytes.Length} байт хуйни");
    }
}
