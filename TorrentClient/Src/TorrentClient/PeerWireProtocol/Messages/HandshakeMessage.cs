using System.Text;

using DefensiveProgrammingFramework;

using TorrentClient.Extensions;

namespace TorrentClient.PeerWireProtocol.Messages;

/// <summary>
///     The handshake message.
/// </summary>
public class HandshakeMessage : PeerMessage
{
    /// <summary>
    ///     The protocol name.
    /// </summary>
    public const string ProtocolName = "BitTorrent protocol";

    /// <summary>
    ///     The extended messaging flag.
    /// </summary>
    private const byte ExtendedMessagingFlag = 0x10;

    /// <summary>
    ///     The fast peers flag.
    /// </summary>
    private const byte FastPeersFlag = 0x04;

    /// <summary>
    ///     The information hash length in bytes.
    /// </summary>
    private const int InfoHashLength = 20;

    /// <summary>
    ///     The name length length in bytes.
    /// </summary>
    private const int NameLengthLength = 1;

    /// <summary>
    ///     The peer identifier length in bytes.
    /// </summary>
    private const int PeerIdLength = 20;

    /// <summary>
    ///     The reserved length in bytes.
    /// </summary>
    private const int ReservedLength = 8;

    /// <summary>
    ///     The zeroed bits.
    /// </summary>
    private static readonly byte[] ZeroedBits = new byte[8];

    /// <summary>
    ///     Initializes a new instance of the <see cref="HandshakeMessage" /> class.
    /// </summary>
    /// <param name="infoHash">The information hash.</param>
    /// <param name="peerId">The peer unique identifier.</param>
    /// <param name="protocolString">The protocol string.</param>
    /// <param name="supportsFastPeer">if set to <c>true</c> the peer supports fast peer.</param>
    /// <param name="supportsExtendedMessaging">if set to <c>true</c> the peer supports extended messaging.</param>
    /// <exception cref="TorrentClient.Exceptions.PeerWireProtocolException">
    ///     The engine does not support fast peer, but fast peer was requested
    ///     or
    ///     The engine does not support extended, but extended was requested
    /// </exception>
    public HandshakeMessage(string infoHash, string peerId, string protocolString = ProtocolName, bool supportsFastPeer = false, bool supportsExtendedMessaging = false)
    {
        infoHash.CannotBeNullOrEmpty();
        infoHash.Length.MustBeEqualTo(40);
        peerId.CannotBeNullOrEmpty();
        peerId.Length.MustBeGreaterThanOrEqualTo(20);
        protocolString.CannotBeNullOrEmpty();

        InfoHash = infoHash;
        PeerId = peerId;
        ProtocolString = protocolString;
        ProtocolStringLength = protocolString.Length;
        SupportsFastPeer = supportsFastPeer;
        SupportsExtendedMessaging = supportsExtendedMessaging;
    }

    /// <summary>
    ///     Prevents a default instance of the <see cref="HandshakeMessage" /> class from being created.
    /// </summary>
    private HandshakeMessage()
    {
    }

    /// <summary>
    ///     Gets the information hash.
    /// </summary>
    /// <value>
    ///     The information hash.
    /// </value>
    public string InfoHash { get; }

    /// <summary>
    ///     Gets the length in bytes.
    /// </summary>
    /// <value>
    ///     The length in bytes.
    /// </value>
    public override int Length => NameLengthLength + ProtocolString.Length + ReservedLength + InfoHashLength + PeerIdLength;

    /// <summary>
    ///     Gets the peer unique identifier.
    /// </summary>
    /// <value>
    ///     The peer unique identifier.
    /// </value>
    public string PeerId { get; }

    /// <summary>
    ///     Gets the protocol string.
    /// </summary>
    /// <value>
    ///     The protocol string.
    /// </value>
    public string ProtocolString { get; }

    /// <summary>
    ///     Gets the length of the protocol string.
    /// </summary>
    /// <value>
    ///     The length of the protocol string.
    /// </value>
    public int ProtocolStringLength { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether the peer supports extended messaging.
    /// </summary>
    /// <value>
    ///     <c>true</c> if the peer supports extended messaging; otherwise, <c>false</c>.
    /// </value>
    public bool SupportsExtendedMessaging { get; }

    /// <summary>
    ///     Gets a value indicating whether the peer supports fast peer.
    /// </summary>
    /// <value>
    ///     <c>true</c> if the peer supports fast peer; otherwise, <c>false</c>.
    /// </value>
    public bool SupportsFastPeer { get; }

    /// <summary>
    ///     Decodes the message.
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    /// <param name="offsetFrom">The offset.</param>
    /// <param name="offsetTo">The offset to.</param>
    /// <param name="message">The message.</param>
    /// <param name="isIncomplete">if set to <c>true</c> the message is incomplete.</param>
    /// <returns>
    ///     True if decoding was successful; false otherwise.
    /// </returns>
    public static bool TryDecode(byte[] buffer, ref int offsetFrom, int offsetTo, out HandshakeMessage message, out bool isIncomplete)
    {
        byte protocolStringLength;
        string protocolString;
        bool supportsExtendedMessaging;
        bool supportsFastPeer;
        string infoHash;
        string peerId;

        message = null;
        isIncomplete = false;

        if (buffer != null &&
            buffer.Length > offsetFrom + NameLengthLength + ReservedLength + InfoHashLength + PeerIdLength &&
            offsetFrom >= 0 &&
            offsetTo >= offsetFrom &&
            offsetTo <= buffer.Length)
        {
            protocolStringLength = ReadByte(buffer, ref offsetFrom); // first byte is length

            if (buffer.Length >= offsetFrom + protocolStringLength + ReservedLength + InfoHashLength + PeerIdLength)
            {
                protocolString = ReadString(buffer, ref offsetFrom, protocolStringLength);

                // increment offset first so that the indices are consistent between Encoding and Decoding
                offsetFrom += ReservedLength;

                supportsExtendedMessaging = (ExtendedMessagingFlag & buffer[offsetFrom - 3]) == ExtendedMessagingFlag;
                supportsFastPeer = (FastPeersFlag & buffer[offsetFrom - 1]) == FastPeersFlag;

                infoHash = ReadBytes(buffer, ref offsetFrom, 20).ToHexaDecimalString();
                peerId = ToPeerId(ReadBytes(buffer, ref offsetFrom, 20));

                if (protocolStringLength == 19 &&
                    protocolString == ProtocolName &&
                    infoHash.Length == 40 &&
                    peerId != null &&
                    peerId.Length >= 20 &&
                    peerId.IsNotNullOrEmpty())
                {
                    if (offsetFrom <= offsetTo)
                        message = new HandshakeMessage(infoHash, peerId, protocolString, supportsFastPeer, supportsExtendedMessaging);
                    else
                        isIncomplete = true;
                }
            }
        }

        return message != null;
    }

    /// <summary>
    ///     Encodes the message.
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    /// <param name="offset">The offset.</param>
    /// <returns>
    ///     The encoded peer message.
    /// </returns>
    public override int Encode(byte[] buffer, int offset)
    {
        buffer.CannotBeNullOrEmpty();
        offset.MustBeGreaterThanOrEqualTo(0);
        offset.MustBeLessThan(buffer.Length);

        var written = offset;

        Write(buffer, ref written, (byte)ProtocolString.Length);
        Write(buffer, ref written, ProtocolString);
        Write(buffer, ref written, ZeroedBits);

        if (SupportsExtendedMessaging)
            buffer[written - 3] |= ExtendedMessagingFlag;

        if (SupportsFastPeer)
            buffer[written - 1] |= FastPeersFlag;

        Write(buffer, ref written, InfoHash.ToByteArray());
        Write(buffer, ref written, FromPeerId(PeerId));

        return CheckWritten(written - offset);
    }

    /// <summary>
    ///     Determines whether the specified <see cref="object" />, is equal to this instance.
    /// </summary>
    /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
    /// <returns>
    ///     <c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object obj)
    {
        var msg = obj as HandshakeMessage;

        if (msg == null)
            return false;

        if (InfoHash != msg.InfoHash)
            return false;

        return InfoHash == msg.InfoHash &&
               PeerId == msg.PeerId &&
               ProtocolString == msg.ProtocolString &&
               SupportsFastPeer == msg.SupportsFastPeer &&
               SupportsExtendedMessaging == msg.SupportsExtendedMessaging;
    }

    /// <summary>
    ///     Returns a hash code for this instance.
    /// </summary>
    /// <returns>
    ///     A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
    /// </returns>
    public override int GetHashCode() =>
        InfoHash.GetHashCode(StringComparison.InvariantCulture) ^
        PeerId.GetHashCode(StringComparison.InvariantCulture) ^
        ProtocolString.GetHashCode(StringComparison.InvariantCulture) ^
        SupportsFastPeer.GetHashCode() ^
        SupportsExtendedMessaging.GetHashCode();

    /// <summary>
    ///     Returns a <see cref="string" /> that represents this instance.
    /// </summary>
    /// <returns>
    ///     A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        StringBuilder sb;

        sb = new StringBuilder();
        sb.Append("HandshakeMessage: ");
        sb.Append($"PeerID = {PeerId}, ");
        sb.Append($"InfoHash = {InfoHash}, ");
        sb.Append($"FastPeer = {SupportsFastPeer}, ");
        sb.Append($"ExtendedMessaging = {SupportsExtendedMessaging}");

        return sb.ToString();
    }
}
