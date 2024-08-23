using Bruteforce.TorrentWrapper;

namespace Tests;

public class PersistenceTests
{
    private Random _random;

    [SetUp]
    public void Setup()
    {
        _random = new Random();
    }
    
    //var data = File.ReadAllBytes("C:\\Users\\nikit\\RiderProjects\\Bruteforce\\Tests\\TestData\\Damaged\\Petukh\\Posobie_dlja_samoubijz.part06.rar")
    [Test]
    public void TestVerifyCorrectFile()
    {
        const string pathToDir = "TestData/Undamaged";
        const string pathToTorrentFile = "TestData/Petukh.torrent";
        
        var loadSuccess = TorrentInfo.TryLoad(pathToTorrentFile, out var torrent);
        
        Assert.AreEqual(7, torrent.PiecesCount);
        Assert.IsTrue(loadSuccess);
        
        var pieces = PersistenceManager.Verify(pathToDir, torrent);
        
        Assert.IsEmpty(pieces);
    }
    
    [Test]
    public void TestVerifyBrokenFile()
    {
        const string pathToDir = "TestData/Damaged";
        const string pathToTorrentFile = "TestData/Petukh.torrent";
        
        var loadSuccess = TorrentInfo.TryLoad(pathToTorrentFile, out var torrent);
    
        Assert.AreEqual(7, torrent.PiecesCount);
        Assert.IsTrue(loadSuccess);
        
        var pieces = PersistenceManager.Verify(pathToDir, torrent);
        
        Assert.AreEqual(1, pieces.Count);
        Assert.AreEqual("F206F638CE6B2942D3BF2C288982FB875BCD5401", pieces[0].Hash);
        Assert.AreEqual(3, pieces[0].Index);
    }
}
