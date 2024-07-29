using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

using DefensiveProgrammingFramework;

using TorrentClient.Exceptions;
using TorrentClient.PeerWireProtocol.Messages;

namespace TorrentClient.PeerWireProtocol;

/// <summary>
///     The peer communicator.
/// </summary>
public sealed class PeerCommunicator : IDisposable
{
    /// <summary>
    ///     The locker.
    /// </summary>
    private readonly object locker = new();

    /// <summary>
    ///     The network stream.
    /// </summary>
    private NetworkStream stream;

    /// <summary>
    ///     The TCP client.
    /// </summary>
    private TcpClient tcp;

    /// <summary>
    ///     The throttling manager.
    /// </summary>
    private readonly ThrottlingManager tm;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PeerCommunicator" /> class.
    /// </summary>
    /// <param name="throttlingManager">The throttling manager.</param>
    /// <param name="tcp">The TCP.</param>
    public PeerCommunicator(ThrottlingManager throttlingManager, TcpClient tcp)
    {
        throttlingManager.CannotBeNull();
        tcp.CannotBeNull();

        PieceData = null;

        tm = throttlingManager;
        this.tcp = tcp;

        var data = new AsyncReadData(this.tcp.ReceiveBufferSize);

        stream = this.tcp.GetStream();
        stream.BeginRead(data.Buffer, data.OffsetStart, data.Buffer.Length, Receive, data);

        Endpoint = this.tcp.Client.RemoteEndPoint as IPEndPoint;
    }

    /// <summary>
    ///     Prevents a default instance of the <see cref="PeerCommunicator" /> class from being created.
    /// </summary>
    private PeerCommunicator()
    {
    }

    /// <summary>
    ///     Gets the endpoint.
    /// </summary>
    /// <value>
    ///     The endpoint.
    /// </value>
    public IPEndPoint Endpoint { get; }

    /// <summary>
    ///     Gets a value indicating whether the object is disposed.
    /// </summary>
    /// <value>
    ///     <c>true</c> if object is disposed; otherwise, <c>false</c>.
    /// </value>
    public bool IsDisposed { get; private set; }

    /// <summary>
    ///     Gets or sets the piece data.
    /// </summary>
    /// <value>
    ///     The piece data.
    /// </value>
    public byte[] PieceData { get; set; }

    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        lock (locker)
        {
            if (!IsDisposed)
            {
                Debug.WriteLine($"disposing peer communicator for {tcp.Client.RemoteEndPoint}");

                IsDisposed = true;

                if (stream != null)
                {
                    stream.Flush();
                    stream.Close();
                    stream.Dispose();
                    stream = null;
                }

                if (tcp != null)
                {
                    tcp.Close();
                    tcp.Dispose();
                    tcp = null;
                }
            }
        }
    }

    /// <summary>
    ///     Occurs when communication error occurred.
    /// </summary>
    public event EventHandler<CommunicationErrorEventArgs> CommunicationError;

    /// <summary>
    ///     Occurs when a peer message has been received.
    /// </summary>
    public event EventHandler<PeerMessgeReceivedEventArgs> MessageReceived;

    /// <summary>
    ///     Sends the specified message.
    /// </summary>
    /// <param name="messages">The messages.</param>
    public void Send(IEnumerable<PeerMessage> messages)
    {
        messages.CannotBeNullOrEmpty();

        byte[] data = null;
        var offset = 0;

        CheckIfObjectIsDisposed();

        lock (locker)
        {
            if (!IsDisposed)
            {
                if (messages.Count() == 1)
                {
                    data = messages.ElementAt(0).Encode();
                }
                else
                {
                    data = new byte[messages.Sum(x => x.Length)];

                    foreach (var message in messages)
                    {
                        Buffer.BlockCopy(message.Encode(), 0, data, offset, message.Length);

                        offset += message.Length;
                    }
                }

                try
                {
                    stream.BeginWrite(data, 0, data.Length, Send, null);

                    tm.Write(data.Length);
                }
                catch (IOException ex)
                {
                    OnCommunicationError(this, new CommunicationErrorEventArgs(ex.Message));
                }
            }
        }
    }

    /// <summary>
    ///     Checks if object is disposed.
    /// </summary>
    private void CheckIfObjectIsDisposed()
    {
        if (IsDisposed)
            throw new ObjectDisposedException(GetType().Name);
    }

    /// <summary>
    ///     Called when a communication error occurs.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnCommunicationError(object sender, CommunicationErrorEventArgs e)
    {
        sender.CannotBeNull();
        e.CannotBeNull();

        if (CommunicationError != null)
            CommunicationError(sender, e);
    }

    /// <summary>
    ///     Called when a peer message has been received.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnMessageReceived(object sender, PeerMessgeReceivedEventArgs e)
    {
        sender.CannotBeNull();
        e.CannotBeNull();

        if (MessageReceived != null)
            MessageReceived(sender, e);
    }

    /// <summary>
    ///     Asynchronous write callback.
    /// </summary>
    /// <param name="ar">The async result.</param>
    private void Receive(IAsyncResult ar)
    {
        var data = ar.AsyncState as AsyncReadData;
        var bytesRead = 0;
        var offset = data.OffsetStart;
        PeerMessage message;
        PieceMessage pieceMessage;
        bool isIncomplete;

        lock (locker)
        {
            if (!IsDisposed)
                try
                {
                    // read data
                    bytesRead = stream.EndRead(ar);

                    if (bytesRead > 0)
                    {
                        data.OffsetEnd += bytesRead;

                        // walk through the array and try to decode messages
                        while (offset <= data.OffsetEnd)
                            if (PieceMessage.TryDecode(data.Buffer, ref offset, data.OffsetEnd, out pieceMessage, out isIncomplete, PieceData))
                            {
                                // successfully decoded message
                                OnMessageReceived(this, new PeerMessgeReceivedEventArgs(pieceMessage));

                                // remember where we left off
                                data.OffsetStart = offset;
                            }
                            else if (PeerMessage.TryDecode(data.Buffer, ref offset, data.OffsetEnd, out message, out isIncomplete))
                            {
                                // successfully decoded message
                                OnMessageReceived(this, new PeerMessgeReceivedEventArgs(message));

                                // remember where we left off
                                data.OffsetStart = offset;
                            }
                            else if (isIncomplete)
                            {
                                // message of variable length is present but incomplete -> stop advancing
                                break;
                            }
                            else
                            {
                                // move to next byte
                                offset++;
                            }

                        if (data.OffsetStart == data.OffsetEnd)
                        {
                            // reset offset
                            data.OffsetStart = 0;
                            data.OffsetEnd = 0;
                        }
                        else if (data.OffsetStart > 0 &&
                                 data.OffsetStart < data.OffsetEnd)
                        {
                            // move data to beginning of the buffere
                            for (var i = data.OffsetStart; i < data.OffsetEnd; i++)
                                data.Buffer[i - data.OffsetStart] = data.Buffer[i];

                            // reset offset
                            data.OffsetEnd = data.OffsetEnd - data.OffsetStart;
                            data.OffsetStart = 0;
                        }
                        else if (data.OffsetStart > data.OffsetEnd)
                        {
                            throw new PeerWireProtocolException("Invalid data.");
                        }

                        tm.Read(bytesRead);

                        // resume reading
                        if (!IsDisposed)
                            stream.BeginRead(data.Buffer, data.OffsetEnd, data.Buffer.Length - data.OffsetEnd, Receive, data);
                    }
                    else
                    {
                        // we received no data
                        Debug.WriteLine($"received no data from {Endpoint}");
                    }
                }
                catch (IOException ex)
                {
                    Debug.WriteLine($"could not read data from {Endpoint}: {ex.Message}");

                    OnCommunicationError(this, new CommunicationErrorEventArgs(ex.Message));
                }
        }
    }

    /// <summary>
    ///     Asynchronous send callback.
    /// </summary>
    /// <param name="ar">The async result.</param>
    private void Send(IAsyncResult ar)
    {
        if (!IsDisposed)
            stream.EndWrite(ar);
    }
}
