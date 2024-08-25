// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

using Bruteforce;
using Bruteforce.TorrentWrapper;
using Bruteforce.TorrentWrapper.Extensions;
using System.CommandLine;



var torrentPath = new Argument<string>("torrent", description: "Путь к торрент-файлу");
var dataPath = new Argument<string>("data", description: "Путь к данным");

var restore = new Option<bool>("--restore", description: "Должна ли утилита самостоятельно восстановить данные");
restore.AddAlias("-r");

var bruteCommand = new Command("brute", "Найти и исправить битрот") {
    torrentPath,
    dataPath,
    restore
};

bruteCommand.SetHandler(Worker.DoWork, torrentPath, dataPath, restore);

var indexOfPiece = new Argument<int>("piece-index", description: "Номер части (отсчет с нуля)");
var indexOfBit = new Argument<int>("bit-index", description: "Номер бита (отсчет с нулевого бита части)");


var restoreCommand = new Command("restore", "Исправить битрот с известным местом") {
    torrentPath,
    dataPath,
    indexOfPiece,
    indexOfBit
};

restoreCommand.SetHandler(Worker.DoWork, torrentPath, dataPath, indexOfPiece, indexOfBit);

var destination = new Argument<string>("destination", description: "Путь к файлу с частью");

var extractCommand = new Command("extract", "Вытащить из торрента часть") {
    torrentPath,
    dataPath,
    destination,
    indexOfPiece
};

extractCommand.SetHandler(Worker.Extract, torrentPath, dataPath, destination, indexOfPiece);

var insertCommand = new Command("insert", "Записать в торрент часть") {
    torrentPath,
    dataPath,
    destination,
    indexOfPiece
};

extractCommand.SetHandler(Worker.Insert, torrentPath, dataPath, destination, indexOfPiece);

var rootCommand = new RootCommand {
    bruteCommand,
    restoreCommand,
    extractCommand,
    insertCommand
};

rootCommand.Description = "Хуярим всякую хуйню с похуяренными жизнью торрентами";

return rootCommand.Invoke(args);

// https://learn.microsoft.com/en-us/dotnet/standard/commandline/get-started-tutorial

class Worker
{
    public static void DoWork(string torrentPath, string dataPath, bool doRepair)
    {
        TorrentInfo.TryLoad(torrentPath, out var torrent);

        if (torrent.Files.Length == 1 && torrent.Files[0].FilePath.Contains("Posobie_dlja_samoubijz"))
            Console.WriteLine("Нихуя себе, сегодня хуярим петуха!");
        else
            Console.WriteLine("Опять хуйню прислали");

        var pieces = PersistenceManager.Verify(dataPath, torrent);

        Console.WriteLine($"Похуевило {pieces.Count} частям");

        foreach (var piece in pieces)
        {
            foreach (var b in piece.Hash.ToByteArrayFromHex())
            {
                Console.Write($"{b} ");
            }

            Console.WriteLine();

            Console.WriteLine($"Хуярим часть номер {piece.Index}");

            if (!piece.Restoreable)
            {
                Console.WriteLine("Тут одни нули, хуйня выходит");
                continue;
            }

            var sw = Stopwatch.StartNew();

            var bitIndex = BruteforceParallel.Bruteforce(piece.Bytes, piece.Hash.ToByteArrayFromHex());
            Console.WriteLine(bitIndex);

            if (doRepair && bitIndex > 0)
            {
                PersistenceManager.FlipBit(dataPath, torrent, piece.Index, bitIndex);
            }

            sw.Stop();
            Console.WriteLine($"Прохуярило: {sw.Elapsed} времени на {piece.Bytes.Length} байт хуйни");
        }
    }

    public static void DoWork(string torrentPath, string dataPath, int pieceIndex, int bitIndex)
    {
        TorrentInfo.TryLoad(torrentPath, out var torrent);

        if (torrent.Files.Length == 1 && torrent.Files[0].FilePath.Contains("Posobie_dlja_samoubijz"))
            Console.WriteLine("Нихуя себе, сегодня хуярим петуха!");
        else
            Console.WriteLine("Опять хуйню прислали");

        var pieces = PersistenceManager.Verify(dataPath, torrent);

        Console.WriteLine($"Похуевило {pieces.Count} частям");

        foreach (var piece in pieces)
        {
            Console.WriteLine($"Хуярим часть номер {piece.Index}");

            PersistenceManager.FlipBit(dataPath, torrent, piece.Index, bitIndex);
            Console.WriteLine("Нахуярили");
        }
    }

    public static void Extract(string torrentPath, string dataPath, string destination, int pieceIndex)
    {
        TorrentInfo.TryLoad(torrentPath, out var torrent);

        var bytes = PersistenceManager.Get(dataPath, torrent.Files, torrent.PieceLength, pieceIndex);
        
        File.WriteAllBytes(destination, bytes);
        
        Console.WriteLine($"Успешно насрато {bytes.Length} байтов хуйни в {destination}");
    }
    
    public static void Insert(string torrentPath, string dataPath, string destination, int pieceIndex)
    {
        TorrentInfo.TryLoad(torrentPath, out var torrent);
        
        var bytes = File.ReadAllBytes(destination);
        
        PersistenceManager.Put(dataPath, torrent.Files, torrent.PieceLength, pieceIndex, bytes);
        
        Console.WriteLine($"Успешно всрато {bytes.Length} байтов хуйни в {destination}");
    }
}
