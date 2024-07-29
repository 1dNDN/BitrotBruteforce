
using static Bruteforce.Utility;

namespace Bruteforce;

public class BruteforceParallel
{
    
    public static int Bruteforce(byte[] data, byte[] hash)
    {
        var lockObj = new object();
        var result = -3;

        if (IsEqual(hash, GetHash(data)))
            return -2;
        
        var threads = new List<Thread>();

        var threadCount = Environment.ProcessorCount;
        //var threadCount = 2;

        var chunkSize = data.Length / threadCount;
        var overChunk = data.Length % threadCount;
        
        for (var i = 0; i < threadCount; i++)
        {
            var threadBin = new byte[data.Length];
            data.CopyTo(threadBin, 0);

            var start = i * chunkSize * 8;
            var end = start + chunkSize * 8 - 1;

            if (i == threadCount - 1)
                end += overChunk * 8;
            
            var thread = new Thread(() => Bruteforce(threadBin, hash, start + 1, end, ref lockObj, ref result));
            thread.Start();
            threads.Add(thread);
        }
        
        threads.WaitAll();

        return result;
    }

    /// <summary>
    /// Перебирает битроты
    /// </summary>
    /// <param name="data">Данные для перебирания</param>
    /// <param name="hash">Оригинальный корректный хеш SHA1</param>
    /// <param name="from">Индекс откуда перебираем, в битах</param>
    /// <param name="to">Индекс докуда перебираем, в битах</param>
    /// <returns></returns>
    public static void Bruteforce(byte[] data, byte[] hash, int from, int to, ref object lockObj, ref int result)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(from, 0);

        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(to, data.Length * 8);

        // считаем, что мы тут одни этот массив данных занимаем
        data[(from) >> 3] = (byte)(data[(from) >> 3] ^ 0b0000_0001);
        
        // кейс когда битрот в первом бите
        if (IsEqual(hash, GetHash(data)))
        {
            lock (lockObj)
            {
                if(result == -3) 
                    result = 0;

                return;
            }
        }

        for (var i = from; i < to; i++)
        {
            lock (lockObj)
                if (result != -3)
                    return;
            
            data[(i - 1) >> 3] = (byte)(data[(i - 1) >> 3] ^ (1 << ((i - 1) % 8)));
            data[i >> 3] = (byte)(data[i >> 3] ^ (1 << (i % 8)));

            var newHash = GetHash(data);

            if (IsEqual(hash, newHash))
            {
                lock (lockObj)
                {
                    if(result == -3) 
                        result = i;

                    return;
                }
            }
        }

    }
}
