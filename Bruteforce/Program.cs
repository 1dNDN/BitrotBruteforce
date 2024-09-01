// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

using Bruteforce;
using Bruteforce.TorrentWrapper;
using Bruteforce.TorrentWrapper.Extensions;
using System.CommandLine;
using System.Text.RegularExpressions;


var torrentPath = new Argument<string>("torrent", description: "Path to .torrent");
var dataPath = new Argument<string>("data", description: "Path to data");

var restore = new Option<bool>("--restore", description: "Change the data on disk after finding the rotten bit");
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

var indexOfPiece = new Argument<int>("piece-index", description: "Part no. (starts with 0)");
var indexOfBit = new Argument<int>("bit-index", description: "Bit no. (starts with 0 inside the part)");


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

var pieceBinPath = new Argument<string>("pieceBinPath", description: "Path to piece blob in format: brokenpiece-<TorrentHash>-<PieceIndex>-<PieceHash>.tobrute");

var brutePieceCommand = new Command("brutepiece", "Bruteforce ready blob with known hash") {
    pieceBinPath,
    restore,
    threads
};

brutePieceCommand.SetHandler(Worker.BrutePieceBlob, pieceBinPath, restore, threads);

var destinationFolder = new Argument<string>("destination", description: "Path to piece blobs directory");

var brutePrepareCommand = new Command("bruteprepare", "Generate piece blobs for restoration at a later time") {
    torrentPath,
    dataPath,
    destinationFolder
};

brutePrepareCommand.SetHandler(Worker.BrutePrepare, torrentPath, dataPath, destinationFolder);

var rootCommand = new RootCommand {
    bruteCommand,
    restoreCommand,
    extractCommand,
    insertCommand,
    brutePieceCommand,
    brutePrepareCommand
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

        Console.WriteLine($"Found {pieces.Count} borken pieces");

        foreach (var piece in pieces)
        {
            BrutePieceFromTorrent(dataPath, doRepair, countOfThreads, piece, torrent);
        }
    }
    
    public static void BrutePrepare(string torrentPath, string dataPath, string destinationFolder)
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
            if(!piece.Restoreable)
            {
                Console.WriteLine($"Piece number {piece.Index} is full of zeros and can't be restored");
                continue;
            }
            
            // brokenpiece-<TorrentHash>-<PieceIndex>-<PieceHash>.tobrute
            var path = Path.Combine(destinationFolder, $"brokenpiece-{torrent.InfoHash.ToUpperInvariant()}-{piece.Index}-{piece.Hash}.tobrute");
            
            File.WriteAllBytes(path, piece.Bytes);
            
            Console.WriteLine($"Wrote blob for piece no. {piece.Index} to {path}");
        }
    }

    private static void BrutePieceFromTorrent(string dataPath, bool doRepair, int countOfThreads, BrokenPiece piece, TorrentInfo torrent)
    {
        foreach (var b in piece.Hash.ToByteArrayFromHex())
        {
            Console.Write($"{b} ");
        }

        Console.WriteLine();

        Console.WriteLine($"Processing piece no. {piece.Index}");

        if (!piece.Restoreable)
        {
            Console.WriteLine("Piece is full of zeros, can't fix it");
            return;
        }

        var sw = Stopwatch.StartNew();

        var bitIndex = BruteforceParallel.Bruteforce(piece.Bytes, piece.Hash.ToByteArrayFromHex(), countOfThreads);
        Console.WriteLine(bitIndex);

        if (doRepair && bitIndex > 0)
        {
            PersistenceManager.FlipBit(dataPath, torrent, piece.Index, bitIndex);
        }

        sw.Stop();

        MeasureSpeed(countOfThreads, piece.Bytes.Length, bitIndex, sw);
    }
    
    public static void BrutePieceBlob(string dataPath, bool doRepair, int countOfThreads)
    {
        var filename = Path.GetFileName(dataPath).ToUpperInvariant();
        
        string pattern = @"BROKENPIECE-([0-9A-F]{40})-([0-9]+)-([0-9A-F]{40}).TOBRUTE";
    
        var match = Regex.Match(filename, pattern);
        if (match.Groups.Count < 4)
        {
            throw new FileNotFoundException("Expected format brokenpiece-<TorrentHash>-<PieceIndex>-<PieceHash>.tobrute, got unreadable garbage instead");
        }

        var torrentHash = match.Groups[1].Value;
        var pieceIndex = match.Groups[2].Value;
        var pieceHash = match.Groups[3].Value;
        
        Console.WriteLine($"TorrentHash: {torrentHash}");
        Console.WriteLine($"Processing piece no.: {pieceIndex}");
        Console.WriteLine($"PieceHash: {pieceHash}");

        var newPath = Path.GetFileNameWithoutExtension(dataPath);
        
        var data = File.ReadAllBytes(dataPath);
        
        Console.WriteLine($"Piece size: {data.Length}");
    
        if (!data.IsRestoreable())
        {
            Console.WriteLine("Piece full of zeros, can't restore");
            MoveToFailed(dataPath, newPath, data);
            return;
        }
    
        var sw = Stopwatch.StartNew();
    
        var bitIndex = BruteforceParallel.Bruteforce(data, pieceHash.ToByteArrayFromHex(), countOfThreads);
        Console.WriteLine(bitIndex);

        if (bitIndex > 0)
        {
            Console.WriteLine("Successfully fixed blob!");

            if (doRepair)
            {
                data.FlipBit(bitIndex);

                var bruteok = newPath + ".bruteok";

                File.WriteAllBytes(bruteok, data);
                File.Delete(dataPath);

                Console.WriteLine($"New blob path: {bruteok}");
            }
        }
        else
        {
            Console.WriteLine("Excessive data corruption, failed to fix");
            
            if (doRepair)
                MoveToFailed(dataPath, newPath, data);
        }

        sw.Stop();
    
        MeasureSpeed(countOfThreads, data.Length, bitIndex, sw);
    }

    private static void MoveToFailed(string dataPath, string newPath, byte[] data)
    {
        var brutefailed = newPath + ".brutefailed";
            
        File.WriteAllBytes(brutefailed, data);
        File.Delete(dataPath);
            
        Console.WriteLine($"New blob path: {brutefailed}");
    }

    private static void MeasureSpeed(int countOfThreads, int totalLength, int bitIndex, Stopwatch sw)
    {
        var countOfCalculatedBytes = 0;

        if (bitIndex < 0)
        {
            countOfCalculatedBytes = totalLength;
        }
        else
        {
            var byteIndex = bitIndex / 8;

            var bytesPerThread =  totalLength / countOfThreads;
                
            if(bytesPerThread * countOfThreads < byteIndex)
            {
                countOfCalculatedBytes = totalLength;
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

        Console.WriteLine($"Time spent: {sw.Elapsed} on {totalLength} bytes of data");
        Console.WriteLine($"Amount of hashes per run: {countOfHashesPerIteration}. \n" +
                          $"Amount of runs: {elapsedIterations}. \n" +
                          $"Total hashes: {countOfHashes}. \n" +
                          $"Speed: {(speedHashes / 1_000_000_000):N3} GH/s or {(speedBytes / 1_000_000_000):N2} GB/s");
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
