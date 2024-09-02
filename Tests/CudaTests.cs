using static Bruteforce.Utility;

namespace Tests;

[NonParallelizable]
public class CudaTests
{
    private Random _random;
    int _countOfThreads;

    [SetUp]
    public void Setup()
    {
        _random = new Random();
        _countOfThreads = Environment.ProcessorCount;
    }

    [Test]
    public void TestCorrectFile()
    {
        var data = new byte[128];

        _random.NextBytes(data);

        var hash = GetHash(data);

        var result = Bruteforce.BruteforceCuda.Bruteforce(data, hash);
        
        Assert.AreEqual(-2, result);
    }
    
    [Test]
    public void TestErrorInBit1()
    {
        var data = new byte[128];

        _random.NextBytes(data);

        var hash = GetHash(data);
        
        data[0] = (byte)(data[0] ^ 0b0000_0001);

        var result = Bruteforce.BruteforceCuda.Bruteforce(data, hash);
        
        Assert.AreEqual(0, result);
    }
    
    [Test]
    public void TestErrorInBit2()
    {
        var data = new byte[72];

        _random.NextBytes(data);

        var hash = GetHash(data);
        
        data[0] = (byte)(data[0] ^ 0b0000_0010);

        var result = Bruteforce.BruteforceCuda.Bruteforce(data, hash);
        
        Assert.AreEqual(1, result);
    }
    
    [Test]
    public void TestErrorInBit9()
    {
        var data = new byte[128];

        _random.NextBytes(data);

        var hash = GetHash(data);
        
        data[1] = (byte)(data[1] ^ 0b0000_0001);

        var result = Bruteforce.BruteforceCuda.Bruteforce(data, hash);
        
        Assert.AreEqual(8, result);
    }
    
    [Test]
    public void TestErrorInByte100()
    {
        var data = new byte[128];

        _random.NextBytes(data);

        var hash = GetHash(data);
        
        data[100] = (byte)(data[100] ^ 0b0000_1000);

        var result = Bruteforce.BruteforceCuda.Bruteforce(data, hash);
        
        Assert.AreEqual(803, result);
    }
}
