using DefensiveProgrammingFramework;

using TorrentClient.Extensions;
using TorrentClient.TrackerProtocol.Udp.Messages.Messages;

namespace TorrentClient.TrackerProtocol.Udp.Messages;

/// <summary>
///     The scrape message.
/// </summary>
public class ScrapeMessage : TrackerMessage
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
    ///     The information hash length in bytes.
    /// </summary>
    private const int InfoHashLength = 20;

    /// <summary>
    ///     The transaction identifier length in bytes.
    /// </summary>
    private const int TransactionIdLength = 4;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ScrapeMessage" /> class.
    /// </summary>
    /// <param name="connectionId">The connection identifier.</param>
    /// <param name="transactionId">The transaction unique identifier.</param>
    /// <param name="infoHashes">The info hashes.</param>
    public ScrapeMessage(long connectionId, int transactionId, IEnumerable<string> infoHashes)
        : base(TrackingAction.Scrape, transactionId)
    {
        connectionId.MustBeGreaterThanOrEqualTo(0);
        infoHashes.CannotBeNull();

        ConnectionId = connectionId;
        InfoHashes = infoHashes;
    }

    /// <summary>
    ///     Gets the connection identifier.
    /// </summary>
    /// <value>
    ///     The connection identifier.
    /// </value>
    public long ConnectionId { get; }

    /// <summary>
    ///     Gets the information hashes.
    /// </summary>
    /// <value>
    ///     The information hashes.
    /// </value>
    public IEnumerable<string> InfoHashes { get; }

    /// <summary>
    ///     Gets the length in bytes.
    /// </summary>
    /// <value>
    ///     The length in bytes.
    /// </value>
    public override int Length => ConnectionIdLength + ActionLength + TransactionIdLength + InfoHashes.Count() * 20;

    /// <summary>
    ///     Decodes the message.
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="message">The message.</param>
    /// <returns>
    ///     True if decoding was successful; false otherwise.
    /// </returns>
    public static bool TryDecode(byte[] buffer, int offset, out ScrapeMessage message)
    {
        long connectionId;
        int action;
        int transactionId;
        var infoHashes = new List<string>();

        message = null;

        if (buffer != null &&
            buffer.Length >= offset + ConnectionIdLength + ActionLength + TransactionIdLength &&
            offset >= 0)
        {
            connectionId = ReadLong(buffer, ref offset);
            action = ReadInt(buffer, ref offset);
            transactionId = ReadInt(buffer, ref offset);

            if (connectionId >= 0 &&
                action == (int)TrackingAction.Scrape &&
                transactionId >= 0)
            {
                while (offset <= buffer.Length - InfoHashLength)
                    infoHashes.Add(ReadBytes(buffer, ref offset, InfoHashLength).ToHexaDecimalString());

                message = new ScrapeMessage(connectionId, transactionId, infoHashes);
            }
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

        foreach (var infoHash in InfoHashes)
            Write(buffer, ref written, infoHash.ToByteArray());

        return written - offset;
    }
}
