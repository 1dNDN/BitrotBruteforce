using System.Globalization;
using System.Net;
using System.Text;
using System.Web;

using DefensiveProgrammingFramework;

using TorrentClient.Extensions;
using TorrentClient.PeerWireProtocol.Messages;
using TorrentClient.TrackerProtocol.Udp.Messages.Messages;

namespace TorrentClient.TrackerProtocol.Http.Messages;

/// <summary>
///     The announce message.
/// </summary>
public class AnnounceMessage
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AnnounceMessage" /> class.
    /// </summary>
    /// <param name="infohash">The info hash.</param>
    /// <param name="peerId">The peer unique identifier.</param>
    /// <param name="port">The port.</param>
    /// <param name="bytesUploaded">The bytes uploaded.</param>
    /// <param name="bytesDownloaded">The bytes downloaded.</param>
    /// <param name="bytesLeft">The bytes left.</param>
    /// <param name="peersWantedCount">The peers wanted count.</param>
    /// <param name="trackingEvent">The tracking event.</param>
    public AnnounceMessage(string infohash, string peerId, int port, long bytesUploaded, long bytesDownloaded, long bytesLeft, int peersWantedCount, TrackingEvent trackingEvent)
    {
        infohash.CannotBeNullOrEmpty();
        infohash.Length.MustBeEqualTo(40);
        peerId.CannotBeNullOrEmpty();
        peerId.Length.MustBeGreaterThanOrEqualTo(20);
        port.MustBeGreaterThanOrEqualTo(IPEndPoint.MinPort);
        port.MustBeLessThanOrEqualTo(IPEndPoint.MaxPort);
        bytesUploaded.MustBeGreaterThanOrEqualTo(0);
        bytesDownloaded.MustBeGreaterThanOrEqualTo(0);
        bytesLeft.MustBeGreaterThanOrEqualTo(0);
        peersWantedCount.MustBeGreaterThanOrEqualTo(0);

        BytesDownloaded = bytesDownloaded;
        BytesUploaded = bytesUploaded;
        BytesLeft = bytesLeft;
        TrackingEvent = trackingEvent;
        InfoHash = infohash;
        PeerId = peerId;
        Port = port;
        PeersWantedCount = peersWantedCount;
    }

    /// <summary>
    ///     Prevents a default instance of the <see cref="AnnounceMessage" /> class from being created.
    /// </summary>
    private AnnounceMessage()
    {
    }

    /// <summary>
    ///     Gets the bytes downloaded.
    /// </summary>
    /// <value>
    ///     The bytes downloaded.
    /// </value>
    public long BytesDownloaded { get; }

    /// <summary>
    ///     Gets the bytes left.
    /// </summary>
    /// <value>
    ///     The bytes left.
    /// </value>
    public long BytesLeft { get; }

    /// <summary>
    ///     Gets the bytes uploaded.
    /// </summary>
    /// <value>
    ///     The bytes uploaded.
    /// </value>
    public long BytesUploaded { get; }

    /// <summary>
    ///     Gets the information hash.
    /// </summary>
    /// <value>
    ///     The information hash.
    /// </value>
    public string InfoHash { get; }

    /// <summary>
    ///     Gets the peer unique identifier.
    /// </summary>
    /// <value>
    ///     The peer unique identifier.
    /// </value>
    public string PeerId { get; }

    /// <summary>
    ///     Gets the peers wanted count.
    /// </summary>
    /// <value>
    ///     The peers wanted count.
    /// </value>
    public int PeersWantedCount { get; }

    /// <summary>
    ///     Gets the port.
    /// </summary>
    /// <value>
    ///     The port.
    /// </value>
    public int Port { get; }

    /// <summary>
    ///     Gets the tracking event.
    /// </summary>
    /// <value>
    ///     The tracking event.
    /// </value>
    public TrackingEvent TrackingEvent { get; }

    /// <summary>
    ///     Encodes the message.
    /// </summary>
    /// <returns>The encoded message.</returns>
    public string Encode()
    {
        Dictionary<string, object> parameters;

        parameters = new Dictionary<string, object>();
        parameters.Add("info_hash", HttpUtility.UrlEncode(InfoHash.ToByteArray()));
        parameters.Add("peer_id", Encoding.ASCII.GetString(Message.FromPeerId(PeerId)));
        parameters.Add("port", Port);
        parameters.Add("uploaded", BytesUploaded);
        parameters.Add("downloaded", BytesDownloaded);
        parameters.Add("left", BytesLeft);
        parameters.Add("numwant", PeersWantedCount);
        parameters.Add("event", TrackingEvent.ToString().ToLower(CultureInfo.InvariantCulture));

        return string.Join("&", parameters.Select(x => $"{x.Key}={x.Value}"));
    }
}
