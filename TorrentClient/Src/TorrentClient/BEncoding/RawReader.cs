﻿using DefensiveProgrammingFramework;

namespace TorrentClient.BEncoding;

/// <summary>
///     The raw reader.
/// </summary>
public class RawReader : Stream
{
    /// <summary>
    ///     The has peek flag.
    /// </summary>
    private bool hasPeek;

    /// <summary>
    ///     The input stream.
    /// </summary>
    private readonly Stream input;

    /// <summary>
    ///     The peeked data.
    /// </summary>
    private readonly byte[] peeked;

    /// <summary>
    ///     The strict decoding flag.
    /// </summary>
    private readonly bool strictDecoding;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RawReader" /> class.
    /// </summary>
    /// <param name="input">The input.</param>
    public RawReader(Stream input)
        : this(input, true)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="RawReader" /> class.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="strictDecoding">if set to <c>true</c> [strict decoding].</param>
    public RawReader(Stream input, bool strictDecoding)
    {
        input.CannotBeNull();

        this.input = input;
        peeked = new byte[1];
        this.strictDecoding = strictDecoding;
    }

    /// <summary>
    ///     Gets a value indicating whether the current stream supports reading.
    /// </summary>
    /// <value>true if the stream supports reading; otherwise, false.</value>
    public override bool CanRead => input.CanRead;

    /// <summary>
    ///     Gets a value indicating whether the current stream supports seeking.
    /// </summary>
    /// <value>true if the stream supports seeking; otherwise, false.</value>
    public override bool CanSeek => input.CanSeek;

    /// <summary>
    ///     Gets a value indicating whether the current stream supports writing.
    /// </summary>
    /// <value>true if the stream supports writing; otherwise, false.</value>
    public override bool CanWrite => false;

    /// <summary>
    ///     Gets the length in bytes of the stream.
    /// </summary>
    /// <value>A value representing the length of the stream in bytes.</value>
    public override long Length => input.Length;

    /// <summary>
    ///     Gets or sets the position within the current stream.
    /// </summary>
    /// <value>The current position within the stream.</value>
    public override long Position
    {
        get {
            if (hasPeek)
                return input.Position - 1;

            return input.Position;
        }

        set {
            if (value != Position)
            {
                hasPeek = false;
                input.Position = value;
            }
        }
    }

    /// <summary>
    ///     Gets a value indicating whether to use strict decoding.
    /// </summary>
    /// <value>
    ///     <c>true</c> if to use strict decoding; otherwise, <c>false</c>.
    /// </value>
    public bool StrictDecoding => strictDecoding;

    /// <summary>
    ///     When overridden in a derived class, clears all buffers for this stream and causes any buffered data to be written
    ///     to the underlying device.
    /// </summary>
    public override void Flush() =>
        throw new NotSupportedException();

    /// <summary>
    ///     Peeks the byte.
    /// </summary>
    /// <returns>The peek byte.</returns>
    public int PeekByte()
    {
        if (!hasPeek)
            hasPeek = Read(peeked, 0, 1) == 1;

        return hasPeek ? peeked[0] : -1;
    }

    /// <summary>
    ///     When overridden in a derived class, reads a sequence of bytes from the current stream and advances the position
    ///     within the stream by the number of bytes read.
    /// </summary>
    /// <param name="buffer">
    ///     An array of bytes. When this method returns, the buffer contains the specified byte array with the
    ///     values between <paramref name="offset" /> and (<paramref name="offset" /> + <paramref name="count" /> - 1) replaced
    ///     by the bytes read from the current source.
    /// </param>
    /// <param name="offset">
    ///     The zero-based byte offset in <paramref name="buffer" /> at which to begin storing the data read
    ///     from the current stream.
    /// </param>
    /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
    /// <returns>
    ///     The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many
    ///     bytes are not currently available, or zero (0) if the end of the stream has been reached.
    /// </returns>
    public override int Read(byte[] buffer, int offset, int count)
    {
        buffer.CannotBeNull();
        offset.MustBeGreaterThanOrEqualTo(0);
        count.MustBeGreaterThanOrEqualTo(0);

        var read = 0;

        if (hasPeek &&
            count > 0)
        {
            hasPeek = false;
            buffer[offset] = peeked[0];
            offset++;
            count--;
            read++;
        }

        read += input.Read(buffer, offset, count);

        return read;
    }

    /// <summary>
    ///     Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the end
    ///     of the stream.
    /// </summary>
    /// <returns>
    ///     The unsigned byte cast to an integer, or -1 if at the end of the stream.
    /// </returns>
    public override int ReadByte()
    {
        if (hasPeek)
        {
            hasPeek = false;

            return peeked[0];
        }

        return base.ReadByte();
    }

    /// <summary>
    ///     When overridden in a derived class, sets the position within the current stream.
    /// </summary>
    /// <param name="offset">A byte offset relative to the <paramref name="origin" /> parameter.</param>
    /// <param name="origin">
    ///     A value of type <see cref="<see cref="System.IO.SeekOrigin" />" /> indicating the reference point
    ///     used to obtain the new position.
    /// </param>
    /// <returns>
    ///     The new position within the current stream.
    /// </returns>
    public override long Seek(long offset, SeekOrigin origin)
    {
        offset.MustBeGreaterThanOrEqualTo(0);

        long val;

        if (hasPeek &&
            origin == SeekOrigin.Current)
            val = input.Seek(offset - 1, origin);
        else
            val = input.Seek(offset, origin);

        hasPeek = false;

        return val;
    }

    /// <summary>
    ///     When overridden in a derived class, sets the length of the current stream.
    /// </summary>
    /// <param name="value">The desired length of the current stream in bytes.</param>
    public override void SetLength(long value)
    {
        value.MustBeGreaterThanOrEqualTo(0);

        throw new NotSupportedException();
    }

    /// <summary>
    ///     When overridden in a derived class, writes a sequence of bytes to the current stream and advances the current
    ///     position within this stream by the number of bytes written.
    /// </summary>
    /// <param name="buffer">
    ///     An array of bytes. This method copies <paramref name="count" /> bytes from
    ///     <paramref name="buffer" /> to the current stream.
    /// </param>
    /// <param name="offset">
    ///     The zero-based byte offset in <paramref name="buffer" /> at which to begin copying bytes to the
    ///     current stream.
    /// </param>
    /// <param name="count">The number of bytes to be written to the current stream.</param>
    public override void Write(byte[] buffer, int offset, int count)
    {
        buffer.CannotBeNullOrEmpty();
        offset.MustBeGreaterThanOrEqualTo(0);
        count.MustBeLessThanOrEqualTo(0);

        throw new NotSupportedException();
    }
}
