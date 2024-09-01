// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

using Bruteforce;
using Bruteforce.TorrentWrapper;
using Bruteforce.TorrentWrapper.Extensions;
using System.CommandLine;



var torrentPath = new Argument<string>("torrent", description: "Path to .torrent");
var dataPath = new Argument<string>("data", description: "Path to data");

var restore = new Option<bool>("--restore", description: "Change the data on disk after finding the matching");
restore.AddAlias("-r");

var threads = new Option<int>("--threads", description: "Number of threads to use, default: all available");
threads.AddAlias("-t");
threads.SetDefaultValue(Environment.ProcessorCount);

var bruteCommand = new Command("brute", "Find and fix bitrot") {
    torrentPath,
    dataPath,
    restore,
    threads
};

bruteCommand.SetHandler(Worker.DoWork, torrentPath, dataPath, restore, threads);

var indexOfPiece = new Argument<int>("piece-index", description: "Part number (starts with 0)");
var indexOfBit = new Argument<int>("bit-index", description: "Bit number (starts with 0 inside the part)");


var restoreCommand = new Command("restore", "Fix bitrot at known location") {
    torrentPath,
    dataPath,
    indexOfPiece,
    indexOfBit
};

restoreCommand.SetHandler(Worker.DoWork, torrentPath, dataPath, indexOfPiece, indexOfBit);

var destination = new Argument<string>("destination", description: "Path to torrent part file");

var extractCommand = new Command("extract", "Extract tor. piece out of torrent data at index") {
    torrentPath,
    dataPath,
    destination,
    indexOfPiece
};

extractCommand.SetHandler(Worker.Extract, torrentPath, dataPath, destination, indexOfPiece);

var insertCommand = new Command("insert", "Insert the tor. piece into torrent data at index") {
    torrentPath,
    dataPath,
    destination,
    indexOfPiece
};

insertCommand.SetHandler(Worker.Insert, torrentPath, dataPath, destination, indexOfPiece);

var rootCommand = new RootCommand {
    bruteCommand,
    restoreCommand,
    extractCommand,
    insertCommand
};

rootCommand.Description = "Utility to manipulate 1-bit-broken bitrotten torrents";

return rootCommand.Invoke(args);

// https://learn.microsoft.com/en-us/dotnet/standard/commandline/get-started-tutorial

class Worker
{
    public static void DoWork(string torrentPath, string dataPath, bool doRepair, int countOfThreads)
    {
        TorrentInfo.TryLoad(torrentPath, out var torrent);

        if (torrent.Files.Length == 1 && torrent.Files[0].FilePath.Contains("Posobie_dlja_samoubijz"))
            Console.WriteLine("Nihuya sebe, segodnya huyarim petuha!");
        else
            Console.WriteLine("Loaded .torrent, processing");

        var pieces = PersistenceManager.Verify(dataPath, torrent);

        Console.WriteLine($"Found {pieces.Count} broken pieces");

        foreach (var piece in pieces)
        {
            foreach (var b in piece.Hash.ToByteArrayFromHex())
            {
                Console.Write($"{b} ");
            }

            Console.WriteLine();

            Console.WriteLine($"Processing piece no. {piece.Index}");

            if (!piece.Restoreable)
            {
                Console.WriteLine("Piece is full of zeros, can't fix it.");
                continue;
            }

            var sw = Stopwatch.StartNew();

            var bitIndex = BruteforceParallel.Bruteforce(piece.Bytes, piece.Hash.ToByteArrayFromHex(), countOfThreads);
            Console.WriteLine(bitIndex);

            if (doRepair && bitIndex > 0)
            {
                PersistenceManager.FlipBit(dataPath, torrent, piece.Index, bitIndex);
            }

            sw.Stop();

            var countOfCalculatedBytes = 0;
            var totalLength = piece.Bytes.Length;

            if (bitIndex < 0)
            {
                countOfCalculatedBytes = piece.Bytes.Length;
            }
            else
            {
                var byteIndex = bitIndex / 8;

                var bytesPerThread =  piece.Bytes.Length / countOfThreads;
                
                if(bytesPerThread * countOfThreads < byteIndex)
                {
                    countOfCalculatedBytes = piece.Bytes.Length;
                }
                else
                {
                    var workedBytesPerThread = byteIndex % bytesPerThread;
                    countOfCalculatedBytes = workedBytesPerThread * countOfThreads;
                }
            }
            
            long elapsedIterations = countOfCalculatedBytes * 8;
            long countOfHashesPerIteration = totalLength / 64;
            var countOfHashes = countOfHashesPerIteration * elapsedIterations;

            var countOfBytes = totalLength * elapsedIterations;
            var speedHashes = countOfHashes / sw.Elapsed.TotalSeconds;
            var speedBytes = countOfBytes / sw.Elapsed.TotalSeconds;

            Console.WriteLine($"Time spent: {sw.Elapsed} for {totalLength} bytes of data");
            Console.WriteLine($"Amount of hashes per run: {countOfHashesPerIteration}. \n" +
                              $"Amount of runs: {elapsedIterations}. \n" +
                              $"Total hashes: {countOfHashes}. \n" +
                              $"Speed: {(speedHashes / 1_000_000_000):N3} GH/s or {(speedBytes / 1_000_000_000):N2} GB/s");
        }
    }

    public static void DoWork(string torrentPath, string dataPath, int pieceIndex, int bitIndex)
    {
        TorrentInfo.TryLoad(torrentPath, out var torrent);

        if (torrent.Files.Length == 1 && torrent.Files[0].FilePath.Contains("Posobie_dlja_samoubijz"))
            Console.WriteLine("Nihua sebe, segodnya huyarim petuha!");
        else
            Console.WriteLine("Loaded .torrent, processing");

        var pieces = PersistenceManager.Verify(dataPath, torrent);

        Console.WriteLine($"Found {pieces.Count} broken pieces");

        foreach (var piece in pieces)
        {
            Console.WriteLine($"Processing piece no. {piece.Index}");

            PersistenceManager.FlipBit(dataPath, torrent, piece.Index, bitIndex);
            Console.WriteLine("Done processing piece");
        }
    }

    public static void Extract(string torrentPath, string dataPath, string destination, int pieceIndex)
    {
        TorrentInfo.TryLoad(torrentPath, out var torrent);

        var bytes = PersistenceManager.Get(dataPath, torrent.Files, torrent.PieceLength, pieceIndex);
        
        File.WriteAllBytes(destination, bytes);
        
        Console.WriteLine($"Successfully extracted {bytes.Length} bytes of data to {destination}");
    }
    
    public static void Insert(string torrentPath, string dataPath, string destination, int pieceIndex)
    {
        TorrentInfo.TryLoad(torrentPath, out var torrent);
        
        var bytes = File.ReadAllBytes(destination);
        
        PersistenceManager.Put(dataPath, torrent.Files, torrent.PieceLength, pieceIndex, bytes);
        
        Console.WriteLine($"Successfully inserted {bytes.Length} bytes of data into {destination}");
    }
}
