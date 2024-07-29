using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

using DefensiveProgrammingFramework;

using TorrentClient.Extensions;
using TorrentClient.TrackerProtocol.Udp.Messages;
using TorrentClient.TrackerProtocol.Udp.Messages.Messages;

namespace TorrentClient.TrackerProtocol.Udp;

/// <summary>
///     The UDP tracker.
/// </summary>
public sealed class UdpTracker : Tracker
{
    /// <summary>
    ///     The transaction identifier.
    /// </summary>
    private readonly int transactionId;

    /// <summary>
    ///     The connection identifier.
    /// </summary>
    private long connectionId;

    /// <summary>
    ///     Initializes a new instance of the <see cref="UdpTracker" /> class.
    /// </summary>
    /// <param name="trackerUri">The tracker URI.</param>
    /// <param name="peerId">The peer identifier.</param>
    /// <param name="torrentInfoHash">The torrent information hash.</param>
    /// <param name="listeningPort">The listening port.</param>
    public UdpTracker(Uri trackerUri, string peerId, string torrentInfoHash, int listeningPort)
        : base(trackerUri, peerId, torrentInfoHash, listeningPort)
    {
        transactionId = new Random(DateTime.UtcNow.Millisecond).Next(0, int.MaxValue);
    }

    /// <summary>
    ///     Called when announce is requested.
    /// </summary>
    protected override void OnAnnounce()
    {
        TrackerMessage message;

        OnAnnouncing(this, EventArgs.Empty);

        message = new AnnounceMessage(connectionId, transactionId, TorrentInfoHash, PeerId, BytesDownloaded, BytesLeftToDownload, BytesUploaded, TrackingEvent, 0, WantedPeerCount, new IPEndPoint(IPAddress.Loopback, ListeningPort));

        Debug.WriteLine($"{TrackerUri} -> {message}");

        message = ExecuteUdpRequest(TrackerUri, message);

        if (message is AnnounceResponseMessage)
        {
            Debug.WriteLine($"{TrackerUri} <- {message}");

            OnAnnounced(this, new AnnouncedEventArgs(message.As<AnnounceResponseMessage>().Interval, message.As<AnnounceResponseMessage>().LeecherCount, message.As<AnnounceResponseMessage>().SeederCount, message.As<AnnounceResponseMessage>().Peers));
        }
    }

    /// <summary>
    ///     Called when starting tracking.
    /// </summary>
    protected override void OnStart()
    {
        TrackerMessage message;

        message = new ConnectMessage(transactionId);
        message = ExecuteUdpRequest(TrackerUri, message);

        if (message is ConnectResponseMessage)
        {
            if (message.TransactionId == transactionId)
                connectionId = message.As<ConnectResponseMessage>().ConnectionId;
            else
                // connect failed -> drop tracker
                Debug.WriteLine($"connecting to tracker {TrackerUri} failed");
        }
    }

    /// <summary>
    ///     Called when stopping tracking.
    /// </summary>
    protected override void OnStop()
    {
        TrackingEvent = TrackingEvent.Stopped;

        OnAnnounce();
    }

    /// <summary>
    ///     Executes the UDP request.
    /// </summary>
    /// <param name="uri">The URI.</param>
    /// <param name="message">The message.</param>
    /// <returns>
    ///     The tracker response message.
    /// </returns>
    private TrackerMessage ExecuteUdpRequest(Uri uri, TrackerMessage message)
    {
        uri.CannotBeNull();
        message.CannotBeNull();

        byte[] data = null;
        IAsyncResult asyncResult;
        var any = new IPEndPoint(IPAddress.Any, ListeningPort);

        try
        {
            using (var udp = new UdpClient())
            {
                udp.Client.SendTimeout = (int)TimeSpan.FromSeconds(5).TotalMilliseconds;
                udp.Client.ReceiveTimeout = (int)TimeSpan.FromSeconds(15).TotalMilliseconds;

                udp.Send(message.Encode(), message.Length, TrackerUri.Host, TrackerUri.Port);

                asyncResult = udp.BeginReceive(null, null);

                if (asyncResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5)))
                    data = udp.EndReceive(asyncResult, ref any);
                // timeout
            }
        }
        catch (SocketException ex)
        {
            Debug.WriteLine($"could not send message to UDP tracker {TrackerUri} for torrent {TorrentInfoHash}: {ex.Message}");

            return null;
        }

        if (TrackerMessage.TryDecode(data, 0, MessageType.Response, out message))
        {
            if (message is ErrorMessage)
                Debug.WriteLine($"error message received from UDP tracker {TrackerUri}: \"{message.As<ErrorMessage>().ErrorText}\"");

            return message;
        }

        return null;
    }
}
