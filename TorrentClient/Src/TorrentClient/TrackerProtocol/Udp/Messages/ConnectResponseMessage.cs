using DefensiveProgrammingFramework;

using TorrentClient.TrackerProtocol.Udp.Messages.Messages;

namespace TorrentClient.TrackerProtocol.Udp.Messages;

/// <summary>
///     The connect response message.
/// </summary>
public class ConnectResponseMessage : TrackerMessage
{
    /// <summary>
    ///     The action length in bytes.
    /// </summary>
    private const int ActionLength = 4;

    /// <summary>
    ///     The connection identifier length in bytes.
    /// </summary>
    private const int ConnectionIdLength = 8;

    /// <summary>
    ///     The transaction identifier length in bytes.
    /// </summary>
    private const int TransactionIdLength = 4;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConnectResponseMessage" /> class.
    /// </summary>
    /// <param name="connectionId">The connection identifier.</param>
    /// <param name="transactionId">The transaction identifier.</param>
    public ConnectResponseMessage(long connectionId, int transactionId)
        : base(TrackingAction.Connect, transactionId)
    {
        ConnectionId = connectionId;
    }

    /// <summary>
    ///     Gets the connection identifier.
    /// </summary>
    /// <value>
    ///     The connection identifier.
    /// </value>
    public long ConnectionId { get; }

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
    public static bool TryDecode(byte[] buffer, int offset, out ConnectResponseMessage message)
    {
        long connectionId;
        int action;
        int transactionId;

        message = null;

        if (buffer != null &&
            buffer.Length >= offset + ConnectionIdLength + ActionLength + TransactionIdLength &&
            offset >= 0)
        {
            action = ReadInt(buffer, ref offset);
            transactionId = ReadInt(buffer, ref offset);
            connectionId = ReadLong(buffer, ref offset);

            if (action == (int)TrackingAction.Connect &&
                transactionId >= 0)
                message = new ConnectResponseMessage(connectionId, transactionId);
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

        Write(buffer, ref written, (int)Action);
        Write(buffer, ref written, TransactionId);
        Write(buffer, ref written, ConnectionId);

        return CheckWritten(written - offset);
    }

    /// <summary>
    ///     Returns a <see cref="string" /> that represents this instance.
    /// </summary>
    /// <returns>
    ///     A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString() =>
        "UdpTrackerConnectResponseMessage";
}
