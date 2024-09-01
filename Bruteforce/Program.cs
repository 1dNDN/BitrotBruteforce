// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

using Bruteforce;
using Bruteforce.TorrentWrapper;
using Bruteforce.TorrentWrapper.Extensions;
using System.CommandLine;
using System.Text.RegularExpressions;


var torrentPath = new Argument<string>("torrent", description: "Путь к торрент-файлу");
var dataPath = new Argument<string>("data", description: "Путь к данным");

var restore = new Option<bool>("--restore", description: "Должна ли утилита самостоятельно восстановить данные");
restore.AddAlias("-r");

var threads = new Option<int>("--threads", description: "Сколько потоков использовать, по умолчанию все доступные");
threads.AddAlias("-t");
threads.SetDefaultValue(Environment.ProcessorCount);

var bruteCommand = new Command("brute", "Найти и исправить битрот") {
    torrentPath,
    dataPath,
    restore,
    threads
};

bruteCommand.SetHandler(Worker.DoWork, torrentPath, dataPath, restore, threads);

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

insertCommand.SetHandler(Worker.Insert, torrentPath, dataPath, destination, indexOfPiece);

var pieceBinPath = new Argument<string>("pieceBinPath", description: "Путь к блобу части с именем в формате brokenpiece-<TorrentHash>-<PieceIndex>-<PieceHash>.tobrute");

var brutePieceCommand = new Command("brutepiece", "Сбрутить готовый блоб с известным хешем") {
    pieceBinPath,
    restore,
    threads
};

brutePieceCommand.SetHandler(Worker.BrutePieceBlob, pieceBinPath, restore, threads);

var destinationFolder = new Argument<string>("destination", description: "Путь к папке с частями");

var brutePrepareCommand = new Command("bruteprepare", "Сгенерировать файлы с блобами для последующего восстановления") {
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

rootCommand.Description = "Хуярим всякую хуйню с похуяренными жизнью торрентами";

return rootCommand.Invoke(args);

// https://learn.microsoft.com/en-us/dotnet/standard/commandline/get-started-tutorial

class Worker
{
    public static void DoWork(string torrentPath, string dataPath, bool doRepair, int countOfThreads)
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
            BrutePieceFromTorrent(dataPath, doRepair, countOfThreads, piece, torrent);
        }
    }
    
    public static void BrutePrepare(string torrentPath, string dataPath, string destinationFolder)
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
            if(!piece.Restoreable)
            {
                Console.WriteLine($"Часть номер {piece.Index} состоит исключительно из нулей и не подлежит расхуевливанию");
                continue;
            }
            
            // brokenpiece-<TorrentHash>-<PieceIndex>-<PieceHash>.tobrute
            var path = Path.Combine(destinationFolder, $"brokenpiece-{torrent.InfoHash.ToUpperInvariant()}-{piece.Index}-{piece.Hash}.tobrute");
            
            File.WriteAllBytes(path, piece.Bytes);
            
            Console.WriteLine($"Записали хуйню номер {piece.Index} по адресу {path}");
        }
    }

    private static void BrutePieceFromTorrent(string dataPath, bool doRepair, int countOfThreads, BrokenPiece piece, TorrentInfo torrent)
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
            throw new FileNotFoundException("Ожидался формат BROKENPIECE-<TorrentHash>-<PieceIndex>-<PieceHash>.TOBRUTE, а пришла хуйня");
        }

        var torrentHash = match.Groups[1].Value;
        var pieceIndex = match.Groups[2].Value;
        var pieceHash = match.Groups[3].Value;
        
        Console.WriteLine($"TorrentHash: {torrentHash}");
        Console.WriteLine($"Хуярим часть номер: {pieceIndex}");
        Console.WriteLine($"PieceHash: {pieceHash}");

        var newPath = Path.GetFileNameWithoutExtension(dataPath);
        
        var data = File.ReadAllBytes(dataPath);
        
        Console.WriteLine($"Piece size: {data.Length}");
    
        if (!data.IsRestoreable())
        {
            Console.WriteLine("Тут одни нули, хуйня выходит");
            MoveToFailed(dataPath, newPath, data);
            return;
        }
    
        var sw = Stopwatch.StartNew();
    
        var bitIndex = BruteforceParallel.Bruteforce(data, pieceHash.ToByteArrayFromHex(), countOfThreads);
        Console.WriteLine(bitIndex);

        if (bitIndex > 0)
        {
            Console.WriteLine("Успешно расхуевлено!");

            if (doRepair)
            {
                data.FlipBit(bitIndex);

                var bruteok = newPath + ".bruteok";

                File.WriteAllBytes(bruteok, data);
                File.Delete(dataPath);

                Console.WriteLine($"Новый путь {bruteok}");
            }
        }
        else
        {
            Console.WriteLine("Избыточно нахуевлено, расхуевить не получилось");
            
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
            
        Console.WriteLine($"Новый путь {brutefailed}");
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

        Console.WriteLine($"Прохуярило: {sw.Elapsed} времени на {totalLength} байт хуйни");
        Console.WriteLine($"Число хешей на проход: {countOfHashesPerIteration}. \n" +
                          $"Число проходов: {elapsedIterations}. \n" +
                          $"Число хешей всего: {countOfHashes}. \n" +
                          $"Скорость: {(speedHashes / 1_000_000_000):N3} гигахешей в секунду или {(speedBytes / 1_000_000_000):N2} ГБ/с");
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
