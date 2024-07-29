using System.Text;

using DefensiveProgrammingFramework;

using TorrentClient.TrackerProtocol.Udp.Messages.Messages;

namespace TorrentClient.TrackerProtocol.Udp.Messages;

/// <summary>
///     The UDP error message.
/// </summary>
public class ErrorMessage : TrackerMessage
{
    /// <summary>
    ///     The action length in bytes.
    /// </summary>
    private const int ActionLength = 4;

    /// <summary>
    ///     The transaction identifier length in bytes.
    /// </summary>
    private const int TransactionIdLength = 4;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ErrorMessage" /> class.
    /// </summary>
    /// <param name="transactionId">The transaction unique identifier.</param>
    /// <param name="errorMessage">The error.</param>
    public ErrorMessage(int transactionId, string errorMessage)
        : base(TrackingAction.Error, transactionId)
    {
        errorMessage.CannotBeNullOrEmpty();

        ErrorText = errorMessage;
    }

    /// <summary>
    ///     Gets the error text.
    /// </summary>
    /// <value>
    ///     The error text.
    /// </value>
    public string ErrorText { get; }

    /// <summary>
    ///     Gets the length in bytes.
    /// </summary>
    /// <value>
    ///     The length in bytes.
    /// </value>
    public override int Length => ActionLength + TransactionIdLength + Encoding.ASCII.GetByteCount(ErrorText);

    /// <summary>
    ///     Decodes the message.
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="message">The message.</param>
    /// <returns>
    ///     True if decoding was successful; false otherwise.
    /// </returns>
    public static bool TryDecode(byte[] buffer, int offset, out ErrorMessage message)
    {
        int action;
        int transactionId;
        string errorMessage;

        message = null;

        if (buffer != null &&
            buffer.Length >= offset + ActionLength + TransactionIdLength &&
            offset >= 0)
        {
            action = ReadInt(buffer, ref offset);
            transactionId = ReadInt(buffer, ref offset);

            if (action == (int)TrackingAction.Error &&
                transactionId >= 0)
            {
                errorMessage = ReadString(buffer, ref offset, buffer.Length - offset);

                if (errorMessage.IsNotNullOrEmpty())
                    message = new ErrorMessage(transactionId, errorMessage);
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

        Write(buffer, ref written, (int)Action);
        Write(buffer, ref written, TransactionId);
        Write(buffer, ref written, ErrorText);

        return written - offset;
    }
}
