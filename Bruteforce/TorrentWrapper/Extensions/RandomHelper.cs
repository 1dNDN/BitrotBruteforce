using System.Text;

namespace Bruteforce.TorrentWrapper.Extensions;

/// <summary>
///     Random helper.
/// </summary>
public static class RandomHelper
{
    /// <summary>
    ///     The random generator.
    /// </summary>
    public static readonly Random Random = new(DateTime.UtcNow.Millisecond);

    /// <summary>
    ///     Generates a random string with the given length.
    /// </summary>
    /// <param name="size">Size of the string</param>
    /// <param name="stringCasing">The string casing.</param>
    /// <param name="characters">The allowed characters.</param>
    /// <returns>Random string</returns>
    public static string RandomString(int size, StringCasing stringCasing, params char[] characters)
    {
        if (characters == null ||
            characters.Length == 0)
            return string.Empty;

        var builder = new StringBuilder();

        for (var i = 0; i < size; i++)
            builder.Append(characters[Random.Next(0, characters.Length - 1)]);

        return builder.ToString().ToString(stringCasing);
    }

    /// <summary>
    ///     Generates a random string with the given length.
    /// </summary>
    /// <param name="size">Size of the string</param>
    /// <param name="characters">The characters.</param>
    /// <returns>Random string</returns>
    public static string RandomString(int size, string characters)
    {
        if (string.IsNullOrEmpty(characters))
            return string.Empty;

        return RandomString(size, StringCasing.None, characters.ToCharArray());
    }
}
