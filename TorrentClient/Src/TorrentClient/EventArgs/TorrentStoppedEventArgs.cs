using DefensiveProgrammingFramework;

namespace TorrentClient;

/// <summary>
///     The torrent stopped event arguments.
/// </summary>
public sealed class TorrentStoppedEventArgs : EventArgs
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TorrentStoppedEventArgs" /> class.
    /// </summary>
    /// <param name="torrentInfo">The torrent information.</param>
    public TorrentStoppedEventArgs(TorrentInfo torrentInfo)
    {
        torrentInfo.CannotBeNull();

        TorrentInfo = torrentInfo;
    }

    /// <summary>
    ///     Prevents a default instance of the <see cref="TorrentStoppedEventArgs" /> class from being created.
    /// </summary>
    private TorrentStoppedEventArgs()
    {
    }

    /// <summary>
    ///     Gets the torrent information.
    /// </summary>
    /// <value>
    ///     The torrent information.
    /// </value>
    public TorrentInfo TorrentInfo { get; private set; }
}
