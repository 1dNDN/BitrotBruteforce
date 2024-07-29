// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

using Bruteforce;
using Bruteforce.TorrentWrapper;

using TorrentClient;

Console.WriteLine("Hello, World!");

var originalBin = File.ReadAllBytes("C:\\Users\\nikit\\Downloads\\Telegram Desktop\\piece_563");
var originalHash = Utility.StringToByteArray("234210f5c309c17584bec0e70d78f23b06eef7ac");

var sw = Stopwatch.StartNew();

Console.WriteLine(BruteforceParallel.Bruteforce(originalBin, originalHash));

sw.Stop();
Console.WriteLine($"Elapsed: {sw.Elapsed} for {originalBin.Length} bytes");
foreach (var b in originalHash)
{
    Console.Write($"{b} ");
}

TorrentInfo torrent;
TorrentInfo.TryLoad(@"C:\Users\nikit\RiderProjects\Bruteforce\Data\orelli.torrent", out torrent);

var torrentClient = new Bruteforce.TorrentWrapper.TorrentClient(@"E:\Torrents\fidel_windows_3.1.2");

torrentClient.Verify(); // start torrent client
torrentClient.Verify(torrent); // start torrent file
