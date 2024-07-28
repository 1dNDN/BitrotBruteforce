using static Bruteforce.Utility;

namespace Tests;

public class SequentalTests
{
    private Random _random;

    [SetUp]
    public void Setup()
    {
        _random = new Random();
    }

    [Test]
    public void TestCorrectFile()
    {
        var data = new byte[128];

        _random.NextBytes(data);

        var hash = GetHash(data);

        var result = Bruteforce.BruteforceSequental.Bruteforce(data, hash);
        
        Assert.AreEqual(-2, result);
    }
    
    [Test]
    public void TestErrorInBit1()
    {
        var data = new byte[128];

        _random.NextBytes(data);

        var hash = GetHash(data);
        
        data[0] = (byte)(data[0] ^ 0b0000_0001);

        var result = Bruteforce.BruteforceSequental.Bruteforce(data, hash);
        
        Assert.AreEqual(0, result);
    }
    
    [Test]
    public void TestErrorInBit2()
    {
        var data = new byte[128];

        _random.NextBytes(data);

        var hash = GetHash(data);
        
        data[0] = (byte)(data[0] ^ 0b0000_0010);

        var result = Bruteforce.BruteforceSequental.Bruteforce(data, hash);
        
        Assert.AreEqual(1, result);
    }
    
    [Test]
    public void TestErrorInBit9()
    {
        var data = new byte[128];

        _random.NextBytes(data);

        var hash = GetHash(data);
        
        data[1] = (byte)(data[1] ^ 0b0000_0001);

        var result = Bruteforce.BruteforceSequental.Bruteforce(data, hash);
        
        Assert.AreEqual(8, result);
    }
    
    [Test]
    public void TestErrorInByte100()
    {
        var data = new byte[128];

        _random.NextBytes(data);

        var hash = GetHash(data);
        
        data[100] = (byte)(data[100] ^ 0b0000_1000);

        var result = Bruteforce.BruteforceSequental.Bruteforce(data, hash);
        
        Assert.AreEqual(803, result);
    }
}
