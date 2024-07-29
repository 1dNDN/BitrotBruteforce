using System.Diagnostics;

namespace Bruteforce.TorrentWrapper;

/// <summary>
///     The torrent client.
/// </summary>
public sealed class TorrentClient
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TorrentClient" /> class.
    /// </summary>
    /// <param name="listeningPort">The listening port.</param>
    /// <param name="baseDirectory">The base directory, where all torrents are persisted.</param>
    public TorrentClient(string baseDirectory)
    {
        Debug.WriteLine("creating torrent client");
        Debug.WriteLine($"base directory: {Path.GetFullPath(baseDirectory)}");

        BaseDirectory = baseDirectory;
    }

    /// <summary>
    ///     Gets the base directory.
    /// </summary>
    /// <value>
    ///     The base directory.
    /// </value>
    public string BaseDirectory { get; }

    /// <summary>
    ///     Starts the specified torrent.
    /// </summary>
    /// <param name="torrentInfo">The torrent information.</param>
    public void Verify(TorrentInfo torrentInfo)
    {
        Debug.WriteLine($"starting torrent {torrentInfo.InfoHash}");

        var persistenceManager = new PersistenceManager(BaseDirectory, torrentInfo.PieceLength, torrentInfo.PieceHashes, torrentInfo.Files);
        persistenceManager.Verify();
    }
}
