namespace Bruteforce.TorrentWrapper;

/// <summary>
///     The torrent file information.
/// </summary>
public sealed class TorrentFileInfo
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TorrentFileInfo" /> class.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="md5Hash">The md5hash.</param>
    /// <param name="length">The length.</param>
    public TorrentFileInfo(string filePath, string md5Hash, long length)
    {
        FilePath = filePath;
        Md5Hash = md5Hash;
        Length = length;
    }

    /// <summary>
    ///     Gets the file path.
    /// </summary>
    /// <value>
    ///     The file path.
    /// </value>
    public string FilePath { get; private set; }

    /// <summary>
    ///     Gets the length in bytes.
    /// </summary>
    /// <value>
    ///     The length in bytes.
    /// </value>
    public long Length { get; private set; }

    /// <summary>
    ///     Gets the MD5 hash.
    /// </summary>
    /// <value>
    ///     The MD5 hash.
    /// </value>
    public string Md5Hash { get; private set; }
}
