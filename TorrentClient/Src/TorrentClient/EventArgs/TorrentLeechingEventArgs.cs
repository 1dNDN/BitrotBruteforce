using DefensiveProgrammingFramework;

namespace TorrentClient;

/// <summary>
///     The torrent leeching event arguments.
/// </summary>
public sealed class TorrentLeechingEventArgs : EventArgs
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TorrentLeechingEventArgs" /> class.
    /// </summary>
    /// <param name="torrentInfo">The torrent information.</param>
    public TorrentLeechingEventArgs(TorrentInfo torrentInfo)
    {
        torrentInfo.CannotBeNull();

        TorrentInfo = torrentInfo;
    }

    /// <summary>
    ///     Prevents a default instance of the <see cref="TorrentLeechingEventArgs" /> class from being created.
    /// </summary>
    private TorrentLeechingEventArgs()
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
