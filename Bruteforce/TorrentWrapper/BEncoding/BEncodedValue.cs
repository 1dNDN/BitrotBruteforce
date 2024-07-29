using TorrentClient.BEncoding;

namespace Bruteforce.TorrentWrapper.BEncoding;

/// <summary>
///     The Base class for all BEncoded values.
/// </summary>
public abstract class BEncodedValue
{
    /// <summary>
    ///     Interface for all BEncoded values
    /// </summary>
    /// <param name="data">The byte array containing the BEncoded data</param>
    /// <returns>The decoded BEncoded value.</returns>
    public static BEncodedValue Decode(byte[] data)
    {
        BEncodedValue value = null;

        using (var ms = new MemoryStream(data))
        {
            using (var stream = new RawReader(ms))
            {
                value = Decode(stream);
            }
        }

        return value;
    }

    /// <summary>
    ///     Decodes the specified buffer.
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="length">The length.</param>
    /// <param name="strictDecoding">if set to <c>true</c> [strict decoding].</param>
    /// <returns>The decoded BEncoded value.</returns>
    public static BEncodedValue Decode(byte[] buffer, int offset, int length, bool strictDecoding)
    {
        BEncodedValue value = null;

        using (var reader = new RawReader(new MemoryStream(buffer, offset, length), strictDecoding))
        {
            value = Decode(reader);
        }

        return value;
    }

    /// <summary>
    ///     Decode BEncoded data in the given RawReader
    /// </summary>
    /// <param name="reader">The RawReader containing the BEncoded data</param>
    /// <returns>BEncodedValue containing the data that was in the stream</returns>
    public static BEncodedValue Decode(RawReader reader)
    {
        BEncodedValue data;
        var peekByte = reader.PeekByte();

        if (peekByte == 'i')
        {
            // integer
            data = new BEncodedNumber();
            data.DecodeInternal(reader);

            return data;
        }

        if (peekByte == 'd')
        {
            // dictionary
            data = new BEncodedDictionary();
            data.DecodeInternal(reader);

            return data;
        }

        if (peekByte == 'l')
        {
            // list
            data = new BEncodedList();
            data.DecodeInternal(reader);

            return data;
        }

        if (peekByte == '0' ||
            peekByte == '1' ||
            peekByte == '2' ||
            peekByte == '3' ||
            peekByte == '4' ||
            peekByte == '5' ||
            peekByte == '6' ||
            peekByte == '7' ||
            peekByte == '8' ||
            peekByte == '9')
        {
            // string
            data = new BEncodedString();
            data.DecodeInternal(reader);

            return data;
        }

        throw new ArgumentException("Could not find what value to decode.");
    }

    /// <summary>
    ///     Encodes the BEncodedValue into a byte array.
    /// </summary>
    /// <returns>Byte array containing the BEncoded Data.</returns>
    public byte[] Encode()
    {
        var buffer = new byte[LengthInBytes()];

        if (Encode(buffer, 0) != buffer.Length)
            throw new ArgumentException("Error encoding the data");

        return buffer;
    }

    /// <summary>
    ///     Encodes the BEncodedValue into the supplied buffer
    /// </summary>
    /// <param name="buffer">The buffer to encode the information to</param>
    /// <param name="offset">The offset in the buffer to start writing the data</param>
    /// <returns>The number of bytes encoded.</returns>
    public abstract int Encode(byte[] buffer, int offset);

    /// <summary>
    ///     Returns the size of the byte[] needed to encode this BEncodedValue
    /// </summary>
    /// <returns>The length in bytes.</returns>
    public abstract int LengthInBytes();

    /// <summary>
    ///     Decodes the stream into respected value.
    /// </summary>
    /// <param name="reader">The reader.</param>
    internal abstract void DecodeInternal(RawReader reader);
}
