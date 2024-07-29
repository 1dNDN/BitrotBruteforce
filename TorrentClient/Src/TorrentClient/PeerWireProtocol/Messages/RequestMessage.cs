using System.Text;

using DefensiveProgrammingFramework;

namespace TorrentClient.PeerWireProtocol.Messages;

/// <summary>
///     The request message.
/// </summary>
public class RequestMessage : PeerMessage
{
    /// <summary>
    ///     The message unique identifier.
    /// </summary>
    public const byte MessageId = 6;

    /// <summary>
    ///     The block length length in bytes.
    /// </summary>
    private const int BlockLengthLength = 4;

    /// <summary>
    ///     The block offset length in bytes.
    /// </summary>
    private const int BlockOffsetLength = 4;

    /// <summary>
    ///     The message unique identifier length in bytes.
    /// </summary>
    private const int MessageIdLength = 1;

    /// <summary>
    ///     The message length in bytes.
    /// </summary>
    private const int MessageLength = 13;

    /// <summary>
    ///     The message length in bytes.
    /// </summary>
    private const int MessageLengthLength = 4;

    /// <summary>
    ///     The piece index length in bytes.
    /// </summary>
    private const int PieceIndexLength = 4;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RequestMessage" /> class.
    /// </summary>
    /// <param name="pieceIndex">Index of the piece.</param>
    /// <param name="blockOffset">The block offset.</param>
    /// <param name="blockLength">Length of the block.</param>
    public RequestMessage(int pieceIndex, int blockOffset, int blockLength)
    {
        pieceIndex.MustBeGreaterThanOrEqualTo(0);
        blockOffset.MustBeGreaterThanOrEqualTo(0);
        blockLength.MustBeGreaterThanOrEqualTo(0);

        PieceIndex = pieceIndex;
        BlockOffset = blockOffset;
        BlockLength = blockLength;
    }

    /// <summary>
    ///     Prevents a default instance of the <see cref="RequestMessage" /> class from being created.
    /// </summary>
    private RequestMessage()
    {
    }

    /// <summary>
    ///     Gets the length of the block.
    /// </summary>
    /// <value>
    ///     The length of the block.
    /// </value>
    public int BlockLength { get; }

    /// <summary>
    ///     Gets the block offset.
    /// </summary>
    /// <value>
    ///     The block offset.
    /// </value>
    public int BlockOffset { get; }

    /// <summary>
    ///     Gets the length in bytes.
    /// </summary>
    /// <value>
    ///     The length in bytes.
    /// </value>
    public override int Length => MessageLengthLength + MessageIdLength + PieceIndexLength + BlockOffsetLength + BlockLengthLength;

    /// <summary>
    ///     Gets the index of the piece.
    /// </summary>
    /// <value>
    ///     The index of the piece.
    /// </value>
    public int PieceIndex { get; }

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
    public static bool TryDecode(byte[] buffer, ref int offsetFrom, int offsetTo, out RequestMessage message, out bool isIncomplete)
    {
        int messageLength;
        byte messageId;
        int pieceIndex;
        int blockOffset;
        int blockLength;

        message = null;
        isIncomplete = false;

        if (buffer != null &&
            buffer.Length >= offsetFrom + MessageLengthLength + MessageIdLength + PieceIndexLength + BlockOffsetLength + BlockLengthLength &&
            offsetFrom >= 0 &&
            offsetTo >= offsetFrom &&
            offsetTo <= buffer.Length)
        {
            messageLength = ReadInt(buffer, ref offsetFrom);
            messageId = ReadByte(buffer, ref offsetFrom);
            pieceIndex = ReadInt(buffer, ref offsetFrom);
            blockOffset = ReadInt(buffer, ref offsetFrom);
            blockLength = ReadInt(buffer, ref offsetFrom);

            if (messageLength == MessageIdLength + PieceIndexLength + BlockOffsetLength + BlockLengthLength &&
                messageId == MessageId &&
                pieceIndex >= 0 &&
                blockOffset >= 0 &&
                blockLength >= 0)
            {
                if (offsetFrom <= offsetTo)
                    message = new RequestMessage(pieceIndex, blockOffset, blockLength);
                else
                    isIncomplete = true;
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

        Write(buffer, ref written, MessageLength);
        Write(buffer, ref written, MessageId);
        Write(buffer, ref written, PieceIndex);
        Write(buffer, ref written, BlockOffset);
        Write(buffer, ref written, BlockLength);

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
        var msg = obj as RequestMessage;

        return msg == null
            ? false
            : PieceIndex == msg.PieceIndex &&
              BlockOffset == msg.BlockOffset &&
              BlockLength == msg.BlockLength;
    }

    /// <summary>
    ///     Returns a hash code for this instance.
    /// </summary>
    /// <returns>
    ///     A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
    /// </returns>
    public override int GetHashCode() =>
        PieceIndex.GetHashCode() ^
        BlockLength.GetHashCode() ^
        BlockOffset.GetHashCode();

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
        sb.Append("RequestMessage: ");
        sb.Append($"PieceIndex = {PieceIndex}, ");
        sb.Append($"BlockOffset = {BlockOffset}, ");
        sb.Append($"BlockLength = {BlockLength}");

        return sb.ToString();
    }
}
