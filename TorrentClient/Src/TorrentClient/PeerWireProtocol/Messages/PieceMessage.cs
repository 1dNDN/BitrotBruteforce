using DefensiveProgrammingFramework;

using TorrentClient.Extensions;

namespace TorrentClient.PeerWireProtocol.Messages;

/// <summary>
///     The piece message.
/// </summary>
public class PieceMessage : PeerMessage
{
    /// <summary>
    ///     The message unique identifier.
    /// </summary>
    public const byte MessageId = 7;

    /// <summary>
    ///     The block offset length.
    /// </summary>
    private const int BlockOffsetLength = 4;

    /// <summary>
    ///     The message unique identifier length in bytes.
    /// </summary>
    private const int MessageIdLength = 1;

    /// <summary>
    ///     The message length in bytes.
    /// </summary>
    private const int MessageLengthLength = 4;

    /// <summary>
    ///     The piece index length.
    /// </summary>
    private const int PieiceIndexLength = 4;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PieceMessage" /> class.
    /// </summary>
    /// <param name="pieceIndex">Index of the piece.</param>
    /// <param name="blockOffset">The block offset.</param>
    /// <param name="blockDataLength">Length of the block data.</param>
    /// <param name="data">The block data.</param>
    public PieceMessage(int pieceIndex, int blockOffset, int blockDataLength, byte[] data)
    {
        pieceIndex.MustBeGreaterThanOrEqualTo(0);
        blockOffset.MustBeGreaterThanOrEqualTo(0);
        blockDataLength.MustBeGreaterThan(0);
        data.CannotBeNullOrEmpty();

        PieceIndex = pieceIndex;
        BlockOffset = blockOffset;
        BlockDataLength = blockDataLength;
        Data = data;
    }

    /// <summary>
    ///     Prevents a default instance of the <see cref="PieceMessage" /> class from being created.
    /// </summary>
    private PieceMessage()
    {
    }

    /// <summary>
    ///     Gets the length of the block data.
    /// </summary>
    /// <value>
    ///     The length of the block data.
    /// </value>
    public int BlockDataLength { get; }

    /// <summary>
    ///     Gets the block offset.
    /// </summary>
    /// <value>
    ///     The block offset.
    /// </value>
    public int BlockOffset { get; }

    /// <summary>
    ///     Gets the data.
    /// </summary>
    /// <value>
    ///     The data.
    /// </value>
    public byte[] Data { get; }

    /// <summary>
    ///     Gets the length in bytes.
    /// </summary>
    /// <value>
    ///     The length in bytes.
    /// </value>
    public override int Length => MessageLengthLength + MessageIdLength + PieiceIndexLength + BlockOffsetLength + BlockDataLength;

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
    /// <param name="offsetFrom">The offset from.</param>
    /// <param name="offsetTo">The offset to.</param>
    /// <param name="message">The message.</param>
    /// <param name="isIncomplete">if set to <c>true</c> the message is incomplete.</param>
    /// <param name="destination">The destination data array.</param>
    /// <returns>
    ///     True if decoding was successful; false otherwise.
    /// </returns>
    public static bool TryDecode(byte[] buffer, ref int offsetFrom, int offsetTo, out PieceMessage message, out bool isIncomplete, byte[] destination = null)
    {
        int messageLength;
        byte messageId;
        int pieceIndex;
        int blockOffset;
        var blockDataLength = 0;
        var destinationOffset = 0;
        var offsetFrom2 = offsetFrom;

        message = null;
        isIncomplete = false;

        if (buffer != null &&
            buffer.Length > offsetFrom2 + MessageLengthLength + MessageIdLength + PieiceIndexLength + BlockOffsetLength &&
            offsetFrom2 >= 0 &&
            offsetFrom2 < buffer.Length &&
            offsetTo >= offsetFrom2 &&
            offsetTo <= buffer.Length)
        {
            messageLength = ReadInt(buffer, ref offsetFrom2);
            messageId = ReadByte(buffer, ref offsetFrom2);
            pieceIndex = ReadInt(buffer, ref offsetFrom2);
            blockOffset = ReadInt(buffer, ref offsetFrom2);
            blockDataLength = messageLength - MessageIdLength - PieiceIndexLength - BlockOffsetLength;

            if (messageLength > MessageIdLength + PieiceIndexLength + BlockOffsetLength &&
                messageId == MessageId &&
                pieceIndex >= 0 &&
                blockOffset >= 0 &&
                blockDataLength >= 0)
            {
                if (offsetFrom2 + blockDataLength <= offsetTo)
                {
                    if (destination == null)
                    {
                        destination = new byte[blockDataLength];
                        destinationOffset = 0;
                    }
                    else
                    {
                        destinationOffset = blockOffset;
                    }

                    Copy(buffer, offsetFrom2, destination, ref destinationOffset, blockDataLength);

                    message = new PieceMessage(pieceIndex, blockOffset, blockDataLength, destination);
                    offsetFrom = offsetFrom2 + blockDataLength;
                }
                else
                {
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

        Write(buffer, ref written, MessageIdLength + PieiceIndexLength + BlockOffsetLength + Data.Length);
        Write(buffer, ref written, MessageId);
        Write(buffer, ref written, PieceIndex);
        Write(buffer, ref written, BlockOffset);
        Write(buffer, ref written, Data);

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
        var msg = obj as PieceMessage;

        if (msg == null)
            return false;

        if (PieceIndex == msg.PieceIndex &&
            BlockOffset == msg.BlockOffset &&
            Data.ToHexaDecimalString() == msg.Data.ToHexaDecimalString())
            return true;

        return false;
    }

    /// <summary>
    ///     Returns a hash code for this instance.
    /// </summary>
    /// <returns>
    ///     A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
    /// </returns>
    public override int GetHashCode()
    {
        int hash;

        hash = PieceIndex.GetHashCode() ^
               BlockOffset.GetHashCode() ^
               Data.ToHexaDecimalString().GetHashCode(StringComparison.InvariantCulture);

        return hash;
    }

    /// <summary>
    ///     Returns a <see cref="string" /> that represents this instance.
    /// </summary>
    /// <returns>
    ///     A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString() =>
        $"PieceMessage: PieceIndex = {PieceIndex}, BlockOffset = {BlockOffset}, BlockData = byte[{Data.Length}]";
}
