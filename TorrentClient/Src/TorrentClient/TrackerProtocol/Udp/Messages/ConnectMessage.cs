using DefensiveProgrammingFramework;

using TorrentClient.TrackerProtocol.Udp.Messages.Messages;

namespace TorrentClient.TrackerProtocol.Udp.Messages;

/// <summary>
///     The connect message.
/// </summary>
public class ConnectMessage : TrackerMessage
{
    /// <summary>
    ///     The action length in bytes.
    /// </summary>
    private const int ActionLength = 4;

    /// <summary>
    ///     The default connection identifier
    /// </summary>
    private const long ConnectionId = 0x41727101980;

    /// <summary>
    ///     The connection identifier length in bytes.
    /// </summary>
    private const int ConnectionIdLength = 8;

    /// <summary>
    ///     The transaction identifier length in bytes.
    /// </summary>
    private const int TransactionIdLength = 4;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConnectMessage" /> class.
    /// </summary>
    /// <param name="transactionId">The transaction identifier.</param>
    public ConnectMessage(int transactionId)
        : base(TrackingAction.Connect, transactionId)
    {
    }

    /// <summary>
    ///     Prevents a default instance of the <see cref="ConnectMessage" /> class from being created.
    /// </summary>
    private ConnectMessage()
        : this(DateTime.UtcNow.GetHashCode())
    {
    }

    /// <summary>
    ///     Gets the length in bytes.
    /// </summary>
    /// <value>
    ///     The length in bytes.
    /// </value>
    public override int Length => ConnectionIdLength + ActionLength + TransactionIdLength;

    /// <summary>
    ///     Decodes the message.
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="message">The message.</param>
    /// <returns>
    ///     True if decoding was successful; false otherwise.
    /// </returns>
    public static bool TryDecode(byte[] buffer, int offset, out ConnectMessage message)
    {
        long connectionId;
        int action;
        int transactionId;

        message = null;

        if (buffer != null &&
            buffer.Length >= offset + ConnectionIdLength + ActionLength + TransactionIdLength &&
            offset >= 0)
        {
            connectionId = ReadLong(buffer, ref offset);
            action = ReadInt(buffer, ref offset);
            transactionId = ReadInt(buffer, ref offset);

            if (connectionId == ConnectionId &&
                action == (int)TrackingAction.Connect &&
                transactionId >= 0)
                message = new ConnectMessage(transactionId);
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

        Write(buffer, ref written, ConnectionId);
        Write(buffer, ref written, (int)Action);
        Write(buffer, ref written, TransactionId);

        return CheckWritten(written - offset);
    }

    /// <summary>
    ///     Returns a <see cref="string" /> that represents this instance.
    /// </summary>
    /// <returns>
    ///     A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString() =>
        "UdpTrackerConnectMessage";
}
