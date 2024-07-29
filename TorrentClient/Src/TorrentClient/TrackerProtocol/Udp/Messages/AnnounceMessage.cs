using System.Net;

using DefensiveProgrammingFramework;

using TorrentClient.Extensions;
using TorrentClient.TrackerProtocol.Udp.Messages.Messages;

namespace TorrentClient.TrackerProtocol.Udp.Messages;

/// <summary>
///     The announce message.
/// </summary>
public class AnnounceMessage : TrackerMessage
{
    /// <summary>
    ///     The action length in bytes.
    /// </summary>
    private const int ActionLength = 4;

    /// <summary>
    ///     The connection identifier length.
    /// </summary>
    private const int ConnectionIdLength = 8;

    /// <summary>
    ///     The downloaded length in bytes.
    /// </summary>
    private const int DownloadedLength = 8;

    /// <summary>
    ///     The information hash length in bytes.
    /// </summary>
    private const int InfoHashLength = 20;

    /// <summary>
    ///     The IP address length in bytes.
    /// </summary>
    private const int IpAddressLength = 4;

    /// <summary>
    ///     The key length in bytes.
    /// </summary>
    private const int KeyLength = 4;

    /// <summary>
    ///     The left length in bytes.
    /// </summary>
    private const int LeftLength = 8;

    /// <summary>
    ///     The number want length in bytes.
    /// </summary>
    private const int NumWantLength = 4;

    /// <summary>
    ///     The peer identifier length in bytes.
    /// </summary>
    private const int PeerIdLength = 20;

    /// <summary>
    ///     The port length in bytes.
    /// </summary>
    private const int PortLength = 2;

    /// <summary>
    ///     The tracking event length in bytes.
    /// </summary>
    private const int TrackingEventLength = 4;

    /// <summary>
    ///     The transaction identifier length in bytes.
    /// </summary>
    private const int TransactionIdLength = 4;

    /// <summary>
    ///     The uploaded length in bytes.
    /// </summary>
    private const int UploadedLength = 8;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AnnounceMessage" /> class.
    /// </summary>
    /// <param name="connectionId">The connection identifier.</param>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="infoHash">The information hash.</param>
    /// <param name="peerId">The peer identifier.</param>
    /// <param name="downloaded">The downloaded.</param>
    /// <param name="left">The left.</param>
    /// <param name="uploaded">The uploaded.</param>
    /// <param name="trackingEvent">The tracking event.</param>
    /// <param name="key">The key.</param>
    /// <param name="numberWanted">The number wanted.</param>
    /// <param name="endpoint">The endpoint.</param>
    public AnnounceMessage(long connectionId, int transactionId, string infoHash, string peerId, long downloaded, long left, long uploaded, TrackingEvent trackingEvent, uint key, int numberWanted, IPEndPoint endpoint)
        : base(TrackingAction.Announce, transactionId)
    {
        infoHash.CannotBeNullOrEmpty();
        infoHash.Length.MustBeEqualTo(40);
        peerId.CannotBeNullOrEmpty();
        peerId.Length.MustBeGreaterThanOrEqualTo(20);
        downloaded.MustBeGreaterThanOrEqualTo(0);
        left.MustBeGreaterThanOrEqualTo(0);
        uploaded.MustBeGreaterThanOrEqualTo(0);
        endpoint.CannotBeNull();

        ConnectionId = connectionId;
        InfoHash = infoHash;
        PeerId = peerId;
        Downloaded = downloaded;
        Left = left;
        Uploaded = uploaded;
        TrackingEvent = trackingEvent;
        Key = key;
        NumberWanted = numberWanted;
        Endpoint = endpoint;
    }

    /// <summary>
    ///     Gets the connection identifier.
    /// </summary>
    /// <value>
    ///     The connection identifier.
    /// </value>
    public long ConnectionId { get; }

    /// <summary>
    ///     Gets the downloaded byte count.
    /// </summary>
    /// <value>
    ///     The downloaded byte count.
    /// </value>
    public long Downloaded { get; }

    /// <summary>
    ///     Gets the endpoint.
    /// </summary>
    /// <value>
    ///     The endpoint.
    /// </value>
    public IPEndPoint Endpoint { get; }

    /// <summary>
    ///     Gets the info hash.
    /// </summary>
    /// <value>
    ///     The info hash.
    /// </value>
    public string InfoHash { get; }

    /// <summary>
    ///     Gets the key.
    /// </summary>
    /// <value>
    ///     The key.
    /// </value>
    public uint Key { get; }

    /// <summary>
    ///     Gets the left byte count.
    /// </summary>
    /// <value>
    ///     The left.
    /// </value>
    public long Left { get; }

    /// <summary>
    ///     Gets the length in bytes.
    /// </summary>
    /// <value>
    ///     The length in bytes.
    /// </value>
    public override int Length => ConnectionIdLength + ActionLength + TransactionIdLength + InfoHashLength + PeerIdLength + DownloadedLength + LeftLength + UploadedLength + TrackingEventLength + IpAddressLength + KeyLength + NumWantLength + PortLength;

    /// <summary>
    ///     Gets the number of wanted peers.
    /// </summary>
    /// <value>
    ///     The number of wanted peers.
    /// </value>
    public int NumberWanted { get; }

    /// <summary>
    ///     Gets the peer unique identifier.
    /// </summary>
    /// <value>
    ///     The peer unique identifier.
    /// </value>
    public string PeerId { get; }

    /// <summary>
    ///     Gets the tracking event.
    /// </summary>
    /// <value>
    ///     The tracking event.
    /// </value>
    public TrackingEvent TrackingEvent { get; }

    /// <summary>
    ///     Gets the uploaded byte count.
    /// </summary>
    /// <value>
    ///     The uploaded byte count.
    /// </value>
    public long Uploaded { get; }

    /// <summary>
    ///     Decodes the message.
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="message">The message.</param>
    /// <returns>
    ///     True if decoding was successful; false otherwise.
    /// </returns>
    public static bool TryDecode(byte[] buffer, int offset, out AnnounceMessage message)
    {
        long connectionId;
        long action;
        int transactionId;
        string infoHash;
        string peerId;
        long downloaded;
        long left;
        long uploaded;
        int trackingEvent;
        int ipaddress;
        uint key;
        int numberWanted;
        ushort port;

        message = null;

        if (buffer != null &&
            buffer.Length >= offset + ConnectionIdLength + ActionLength + TransactionIdLength + InfoHashLength + PeerIdLength + DownloadedLength + LeftLength + UploadedLength + TrackingEventLength + IpAddressLength + KeyLength + NumWantLength + PortLength &&
            offset >= 0)
        {
            connectionId = ReadLong(buffer, ref offset);
            action = ReadInt(buffer, ref offset);
            transactionId = ReadInt(buffer, ref offset);
            infoHash = ReadBytes(buffer, ref offset, 20).ToHexaDecimalString();
            peerId = ToPeerId(ReadBytes(buffer, ref offset, 20));
            downloaded = ReadLong(buffer, ref offset);
            left = ReadLong(buffer, ref offset);
            uploaded = ReadLong(buffer, ref offset);
            trackingEvent = ReadInt(buffer, ref offset);
            ipaddress = ReadInt(buffer, ref offset);
            key = (uint)ReadInt(buffer, ref offset);
            numberWanted = ReadInt(buffer, ref offset);
            port = (ushort)ReadShort(buffer, ref offset);

            if (connectionId >= 0 &&
                action == (int)TrackingAction.Announce &&
                transactionId >= 0 &&
                infoHash.IsNotNullOrEmpty() &&
                peerId.IsNotNullOrEmpty() &&
                downloaded >= 0 &&
                left >= 0 &&
                uploaded >= 0 &&
                trackingEvent >= 0 &&
                trackingEvent <= 3 &&
                port >= IPEndPoint.MinPort &&
                port <= IPEndPoint.MaxPort)
                message = new AnnounceMessage(connectionId, transactionId, infoHash, peerId, downloaded, left, uploaded, (TrackingEvent)trackingEvent, key, numberWanted, new IPEndPoint(new IPAddress(BitConverter.GetBytes(ipaddress)), port));
        }

        return message != null;
    }

    /// <summary>
    ///     Encodes the message.
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    /// <param name="offset">The offset.</param>
    /// <returns>The number of bytes written.</returns>
    public override int Encode(byte[] buffer, int offset)
    {
        buffer.CannotBeNullOrEmpty();
        offset.MustBeGreaterThanOrEqualTo(0);
        offset.MustBeLessThan(buffer.Length);

        var written = offset;

        Write(buffer, ref written, ConnectionId);
        Write(buffer, ref written, (int)Action);
        Write(buffer, ref written, TransactionId);
        Write(buffer, ref written, InfoHash.ToByteArray());
        Write(buffer, ref written, FromPeerId(PeerId));
        Write(buffer, ref written, Downloaded);
        Write(buffer, ref written, Left);
        Write(buffer, ref written, Uploaded);
        Write(buffer, ref written, (int)TrackingEvent);
        Write(buffer, ref written, Endpoint.Address == IPAddress.Loopback ? 0 : BitConverter.ToInt32(Endpoint.Address.GetAddressBytes(), 0));
        Write(buffer, ref written, Key);
        Write(buffer, ref written, NumberWanted);
        Write(buffer, ref written, (ushort)Endpoint.Port);

        return written - offset;
    }

    /// <summary>
    ///     Returns a <see cref="string" /> that represents this instance.
    /// </summary>
    /// <returns>
    ///     A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString() =>
        "UdpTrackerAnnounceMessage";
}
