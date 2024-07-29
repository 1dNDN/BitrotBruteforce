using DefensiveProgrammingFramework;

namespace TorrentClient;

/// <summary>
///     The torrent file information.
/// </summary>
public sealed class TorrentFileInfo
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TorrentFileInfo" /> class.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="md5hash">The md5hash.</param>
    /// <param name="length">The length.</param>
    public TorrentFileInfo(string filePath, string md5hash, long length)
    {
        filePath.MustBeValidFilePath();
        md5hash.IsNotNull().Then(() => md5hash.Length.MustBeEqualTo(32));
        length.MustBeGreaterThan(0);

        FilePath = filePath;
        Md5Hash = md5hash;
        Length = length;
        Download = true;
    }

    /// <summary>
    ///     Prevents a default instance of the <see cref="TorrentFileInfo" /> class from being created.
    /// </summary>
    private TorrentFileInfo()
    {
    }

    /// <summary>
    ///     Gets or sets a value indicating whether to download the specified file.
    /// </summary>
    /// <value>
    ///     <c>true</c> if download the specified file; otherwise, <c>false</c>.
    /// </value>
    public bool Download { get; set; }

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
