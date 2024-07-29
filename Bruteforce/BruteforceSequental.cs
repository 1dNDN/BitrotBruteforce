﻿using static Bruteforce.Utility;

namespace Bruteforce;

public static class BruteforceSequental
{
    /// <summary>
    /// Перебирает битроты
    /// </summary>
    /// <param name="data">Данные для перебирания</param>
    /// <param name="hash">Оригинальный корректный хеш SHA1</param>
    public static int Bruteforce(byte[] data, byte[] hash)
    {
        // кейс когда нет битрота
        if (IsEqual(hash, GetHash(data)))
            return -2;

        return Bruteforce(data, hash, 1, (data.Length - 1) * 8);
    }
    
    /// <summary>
    /// Перебирает битроты
    /// </summary>
    /// <param name="data">Данные для перебирания</param>
    /// <param name="hash">Оригинальный корректный хеш SHA1</param>
    /// <param name="from">Индекс откуда перебираем, в битах</param>
    /// <param name="to">Индекс докуда перебираем, в битах</param>
    /// <returns></returns>
    public static int Bruteforce(byte[] data, byte[] hash, int from, int to)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(from, 0);

        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(to, data.Length * 8);

        // считаем, что мы тут одни этот массив данных занимаем
        data[(from - 1) >> 3] = (byte)(data[(from - 1) >> 3] ^ 0b0000_0001);
        
        // кейс когда битрот в первом бите
        if (IsEqual(hash, GetHash(data)))
            return 0;

        for (var i = from; i < to; i++)
        {
            data[(i - 1) >> 3] = (byte)(data[(i - 1) >> 3] ^ 1 << ((i - 1) % 8));
            data[i >> 3] = (byte)(data[i >> 3] ^ 1 << ((i) % 8));

            var newHash = GetHash(data);

            if (IsEqual(hash, newHash))
                return i;
        }

        return -1;
    }
}
