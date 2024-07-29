using System.Diagnostics;
using System.Net;

using DefensiveProgrammingFramework;

using TorrentClient.PeerWireProtocol;
using TorrentClient.PeerWireProtocol.Messages;

namespace TorrentClient;

/// <summary>
///     The peer.
/// </summary>
public sealed class Peer : IDisposable
{
    /// <summary>
    ///     The thread locking object.
    /// </summary>
    private readonly object locker = new();

    /// <summary>
    ///     The communicator.
    /// </summary>
    private PeerCommunicator communicator;

    /// <summary>
    ///     The download message queue.
    /// </summary>
    private Queue<PeerMessage> downloadMessageQueue = new();

    /// <summary>
    ///     The download message queue locker
    /// </summary>
    private readonly object downloadMessageQueueLocker = new();

    /// <summary>
    ///     The download speed in bytes per second.
    /// </summary>
    private decimal downloadSpeed = 0;

    /// <summary>
    ///     The peer is currently downloading flag.
    /// </summary>
    private bool isDownloading = false;

    /// <summary>
    ///     The peer is keeping alive flag.
    /// </summary>
    private bool isKeepingAlive = false;

    /// <summary>
    ///     The the peer is currently uploading flag.
    /// </summary>
    private bool isUploading = false;

    /// <summary>
    ///     The last message received time.
    /// </summary>
    private DateTime? lastMessageReceivedTime;

    /// <summary>
    ///     The last message sent time.
    /// </summary>
    private DateTime? lastMessageSentTime;

    /// <summary>
    ///     The local peer identifier.
    /// </summary>
    private readonly string localPeerId;

    /// <summary>
    ///     The piece manager.
    /// </summary>
    private readonly PieceManager pieceManager;

    /// <summary>
    ///     The previously downloaded byte count.
    /// </summary>
    private long previouslyDownloaded = 0;

    /// <summary>
    ///     The previously uploaded byte count.
    /// </summary>
    private long previouslyUploaded = 0;

    /// <summary>
    ///     The send message queue.
    /// </summary>
    private Queue<PeerMessage> sendMessageQueue = new();

    /// <summary>
    ///     The send message queue locker.
    /// </summary>
    private readonly object sendMessageQueueLocker = new();

    /// <summary>
    ///     The stopwatch.
    /// </summary>
    private readonly Stopwatch stopwatch = Stopwatch.StartNew();

    /// <summary>
    ///     The upload message queue.
    /// </summary>
    private Queue<PeerMessage> uploadMessageQueue = new();

    /// <summary>
    ///     The upload message queue locker.
    /// </summary>
    private readonly object uploadMessageQueueLocker = new();

    /// <summary>
    ///     The upload speed in bytes per second.
    /// </summary>
    private decimal uploadSpeed = 0;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Peer" /> class.
    /// </summary>
    /// <param name="communicator">The communicator.</param>
    /// <param name="pieceManager">The piece manager.</param>
    /// <param name="localPeerId">The local peer identifier.</param>
    /// <param name="peerId">The peer identifier.</param>
    public Peer(PeerCommunicator communicator, PieceManager pieceManager, string localPeerId, string peerId = null)
    {
        communicator.CannotBeNull();
        pieceManager.CannotBeNull();
        localPeerId.CannotBeNullOrEmpty();

        PeerId = peerId;

        this.localPeerId = localPeerId;

        BitField = new bool[pieceManager.PieceCount];

        HandshakeState = peerId == null ? HandshakeState.SentButNotReceived : HandshakeState.SendAndReceived;
        SeedingState = SeedingState.Choked;
        LeechingState = LeechingState.Uninterested;

        Downloaded = 0;
        Uploaded = 0;

        this.communicator = communicator;
        this.communicator.MessageReceived += Communicator_MessageReceived;
        this.communicator.CommunicationError += Communicator_CommunicationError;

        this.pieceManager = pieceManager;
        this.pieceManager.PieceCompleted += PieceManager_PieceCompleted;

        Endpoint = this.communicator.Endpoint;

        StartSending();
        StartDownloading();
        StartUploading();
        StartKeepingConnectionAlive();

        // send handshake
        EnqueueSendMessage(new HandshakeMessage(this.pieceManager.TorrentInfoHash, localPeerId));
    }

    /// <summary>
    ///     Prevents a default instance of the <see cref="Peer" /> class from being created.
    /// </summary>
    private Peer()
    {
    }

    /// <summary>
    ///     Gets the bit field.
    /// </summary>
    /// <value>
    ///     The bit field.
    /// </value>
    public bool[] BitField { get; }

    /// <summary>
    ///     Gets the downloaded byte count.
    /// </summary>
    /// <value>
    ///     The bytes downloaded.
    /// </value>
    public long Downloaded { get; private set; }

    /// <summary>
    ///     Gets the download speed in bytes per second.
    /// </summary>
    /// <value>
    ///     The download speed in bytes per second.
    /// </value>
    public decimal DownloadSpeed
    {
        get {
            UpdateTrafficParameters(0, 0);

            return downloadSpeed;
        }
    }

    /// <summary>
    ///     Gets the endpoint.
    /// </summary>
    /// <value>
    ///     The endpoint.
    /// </value>
    public IPEndPoint Endpoint { get; }

    /// <summary>
    ///     Gets the state of the handshake.
    /// </summary>
    /// <value>
    ///     The state of the handshake.
    /// </value>
    public HandshakeState HandshakeState { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether this object is disposed.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this object is disposed; otherwise, <c>false</c>.
    /// </value>
    public bool IsDisposed { get; private set; }

    /// <summary>
    ///     Gets the state of the leeching.
    /// </summary>
    /// <value>
    ///     The state of the leeching.
    /// </value>
    public LeechingState LeechingState { get; private set; }

    /// <summary>
    ///     Gets the peer identifier.
    /// </summary>
    /// <value>
    ///     The peer identifier.
    /// </value>
    public string PeerId { get; private set; }

    /// <summary>
    ///     Gets the state of the seeding.
    /// </summary>
    /// <value>
    ///     The state of the seeding.
    /// </value>
    public SeedingState SeedingState { get; private set; }

    /// <summary>
    ///     Gets the uploaded byte count.
    /// </summary>
    /// <value>
    ///     The uploaded byte count.
    /// </value>
    public long Uploaded { get; private set; }

    /// <summary>
    ///     Gets the upload speed in bytes per second.
    /// </summary>
    /// <value>
    ///     The upload speed in bytes per second.
    /// </value>
    public decimal UploadSpeed
    {
        get {
            UpdateTrafficParameters(0, 0);

            return uploadSpeed;
        }
    }

    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        if (!IsDisposed)
        {
            IsDisposed = true;

            Debug.WriteLine($"disposing peer {Endpoint}");

            if (communicator != null &&
                !communicator.IsDisposed)
            {
                communicator.Dispose();
                communicator = null;
            }
        }
    }

    /// <summary>
    ///     Occurs when peer communication error has occurred.
    /// </summary>
    public event EventHandler<PeerCommunicationErrorEventArgs> CommunicationErrorOccurred;

    /// <summary>
    ///     Checks if object is disposed.
    /// </summary>
    private void CheckIfObjectIsDisposed()
    {
        if (IsDisposed)
            throw new ObjectDisposedException(GetType().Name);
    }

    /// <summary>
    ///     Handles the CommunicationError event of the communicator control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
    private void Communicator_CommunicationError(object sender, CommunicationErrorEventArgs e) =>
        OnCommunicationErrorOccurred(this, new PeerCommunicationErrorEventArgs(e.ErrorMessage, true));

    /// <summary>
    ///     Handles the MessageReceived event of the Communicator control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="PeerMessgeReceivedEventArgs" /> instance containing the event data.</param>
    private void Communicator_MessageReceived(object sender, PeerMessgeReceivedEventArgs e)
    {
        ProcessRecievedMessage(e.Message);

        UpdateTrafficParameters(e.Message.Length, 0);
    }

    /// <summary>
    ///     De-queues the download messages.
    /// </summary>
    /// <returns>
    ///     The de-queued messages.
    /// </returns>
    private IEnumerable<PeerMessage> DequeueDownloadMessages()
    {
        IEnumerable<PeerMessage> messages;

        lock (downloadMessageQueueLocker)
        {
            messages = downloadMessageQueue;

            // TODO: optimize this by using a wait handler
            downloadMessageQueue = new Queue<PeerMessage>();
        }

        return messages;
    }

    /// <summary>
    ///     De-queues the send messages.
    /// </summary>
    /// <returns>
    ///     The de-queued messages.
    /// </returns>
    private IEnumerable<PeerMessage> DequeueSendMessages()
    {
        IEnumerable<PeerMessage> messages;

        lock (sendMessageQueueLocker)
        {
            messages = sendMessageQueue;

            sendMessageQueue = new Queue<PeerMessage>();
        }

        return messages;
    }

    /// <summary>
    ///     De-queues the upload messages.
    /// </summary>
    /// <returns>
    ///     The de-queued messages.
    /// </returns>
    private IEnumerable<PeerMessage> DequeueUploadMessages()
    {
        IEnumerable<PeerMessage> messages;

        lock (uploadMessageQueueLocker)
        {
            messages = uploadMessageQueue;

            uploadMessageQueue = new Queue<PeerMessage>();
        }

        return messages;
    }

    /// <summary>
    ///     The downloading thread.
    /// </summary>
    private void Download()
    {
        var timeout = TimeSpan.FromMilliseconds(250);
        var chokeTimeout = TimeSpan.FromSeconds(10);
        var stopwatch = Stopwatch.StartNew();
        PieceMessage pm;
        Piece piece = null;
        var bitFieldData = Array.Empty<bool>();
        var pieceData = Array.Empty<byte>();
        var unchokeMessagesSent = 0;

        if (!isDownloading)
        {
            isDownloading = true;

            communicator.PieceData = new byte[pieceManager.PieceLength];

            while (!IsDisposed)
            {
                if (pieceManager.IsComplete)
                    break;

                // process messages
                foreach (var message in DequeueDownloadMessages())
                    if (message is PieceMessage)
                    {
                        pm = message as PieceMessage;

                        if (piece != null &&
                            piece.PieceIndex == pm.PieceIndex &&
                            piece.BitField[pm.BlockOffset / pieceManager.BlockLength] == false)
                        {
                            // update piece
                            piece.PutBlock(pm.BlockOffset);

                            if (piece.IsCompleted ||
                                piece.IsCorrupted)
                                // remove piece in order to start a next one
                                piece = null;
                        }
                    }
                    else if (message is ChokeMessage)
                    {
                        SeedingState = SeedingState.Choked;

                        piece = null;
                    }
                    else if (message is UnchokeMessage)
                    {
                        SeedingState = SeedingState.Unchoked;

                        unchokeMessagesSent = 0;
                    }

                if (HandshakeState == HandshakeState.SendAndReceived)
                {
                    if (SeedingState == SeedingState.Choked)
                    {
                        if (stopwatch.Elapsed > chokeTimeout)
                        {
                            // choked -> send interested
                            EnqueueSendMessage(new InterestedMessage());

                            if (++unchokeMessagesSent > 10)
                                OnCommunicationErrorOccurred(this, new PeerCommunicationErrorEventArgs($"Choked for more than {TimeSpan.FromSeconds(chokeTimeout.TotalSeconds * 10)}.", true));

                            stopwatch.Restart();
                        }
                        else
                        {
                            Thread.Sleep(timeout);
                        }
                    }
                    else if (SeedingState == SeedingState.Unchoked)
                    {
                        if (piece == null)
                        {
                            // find a missing piece
                            for (var pieceIndex = 0; pieceIndex < BitField.Length; pieceIndex++)
                                if (pieceManager.BitField[pieceIndex] == PieceStatus.Missing)
                                    if (BitField[pieceIndex] ||
                                        pieceManager.IsEndGame)
                                    {
                                        pieceData = pieceData.Length == pieceManager.GetPieceLength(pieceIndex) ? pieceData : new byte[pieceManager.GetPieceLength(pieceIndex)];
                                        bitFieldData = bitFieldData.Length == pieceManager.GetBlockCount(pieceIndex) ? bitFieldData : new bool[pieceManager.GetBlockCount(pieceIndex)];

                                        // check it out
                                        piece = pieceManager.CheckOut(pieceIndex, pieceData, bitFieldData);

                                        if (piece != null)
                                        {
                                            communicator.PieceData = pieceData;

                                            break;
                                        }
                                    }

                            if (piece != null)
                                // request blocks from the missing piece
                                for (var i = 0; i < piece.BitField.Length; i++)
                                    if (!piece.BitField[i])
                                        EnqueueSendMessage(new RequestMessage(piece.PieceIndex, (int)piece.GetBlockOffset(i), (int)piece.GetBlockLength(piece.GetBlockOffset(i))));
                        }
                    }
                }

                Thread.Sleep(timeout);
            }

            isDownloading = false;
        }
    }

    /// <summary>
    ///     Enqueues the download message.
    /// </summary>
    /// <param name="message">The message.</param>
    private void EnqueueDownloadMessage(PeerMessage message)
    {
        message.CannotBeNull();

        lock (downloadMessageQueueLocker)
        {
            downloadMessageQueue.Enqueue(message);
        }
    }

    /// <summary>
    ///     Enqueues the send message.
    /// </summary>
    /// <param name="message">The message.</param>
    private void EnqueueSendMessage(PeerMessage message)
    {
        message.CannotBeNull();

        lock (sendMessageQueueLocker)
        {
            sendMessageQueue.Enqueue(message);
        }
    }

    /// <summary>
    ///     Enqueues the upload message.
    /// </summary>
    /// <param name="message">The message.</param>
    private void EnqueueUploadMessage(PeerMessage message)
    {
        message.CannotBeNull();

        lock (uploadMessageQueueLocker)
        {
            uploadMessageQueue.Enqueue(message);
        }
    }

    /// <summary>
    ///     Keeps the connection alive.
    /// </summary>
    private void KeepAlive()
    {
        var keepAliveTimeout = TimeSpan.FromSeconds(60);
        var timeout = TimeSpan.FromSeconds(10);

        if (!isKeepingAlive)
        {
            isKeepingAlive = true;

            while (!IsDisposed)
                if (!isDownloading &&
                    !isUploading)
                {
                    break;
                }
                else if (lastMessageSentTime == null &&
                         lastMessageReceivedTime == null)
                {
                    Thread.Sleep(timeout);
                }
                else if (DateTime.UtcNow - lastMessageSentTime > keepAliveTimeout ||
                         DateTime.UtcNow - lastMessageReceivedTime > keepAliveTimeout)
                {
                    OnCommunicationErrorOccurred(this, new PeerCommunicationErrorEventArgs($"No message exchanged in over {keepAliveTimeout}.", true));

                    break;
                }
                else
                {
                    Thread.Sleep(timeout);
                }

            isKeepingAlive = false;
        }
    }

    /// <summary>
    ///     Called when peer communication error has occurred.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnCommunicationErrorOccurred(object sender, PeerCommunicationErrorEventArgs e)
    {
        sender.CannotBeNull();
        e.CannotBeNull();

        if (CommunicationErrorOccurred != null)
            CommunicationErrorOccurred(sender, e);
    }

    /// <summary>
    ///     Handles the PieceCompleted event of the PieceManager control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="PieceCompletedEventArgs" /> instance containing the event data.</param>
    private void PieceManager_PieceCompleted(object sender, PieceCompletedEventArgs e)
    {
        if (HandshakeState == HandshakeState.SendAndReceived)
            EnqueueSendMessage(new HaveMessage(e.PieceIndex));
    }

    /// <summary>
    ///     Processes the received message.
    /// </summary>
    /// <param name="message">The message.</param>
    private void ProcessRecievedMessage(PeerMessage message)
    {
        CheckIfObjectIsDisposed();

        lock (locker)
        {
            Debug.WriteLine($"{Endpoint} <- {message}");

            lastMessageReceivedTime = DateTime.UtcNow;

            if (message is HandshakeMessage)
            {
                ProcessRecievedMessage(message as HandshakeMessage);
            }
            else if (message is ChokeMessage)
            {
                ProcessRecievedMessage(message as ChokeMessage);
            }
            else if (message is UnchokeMessage)
            {
                ProcessRecievedMessage(message as UnchokeMessage);
            }
            else if (message is InterestedMessage)
            {
                ProcessRecievedMessage(message as InterestedMessage);
            }
            else if (message is UninterestedMessage)
            {
                ProcessRecievedMessage(message as UninterestedMessage);
            }
            else if (message is HaveMessage)
            {
                ProcessRecievedMessage(message as HaveMessage);
            }
            else if (message is BitFieldMessage)
            {
                ProcessRecievedMessage(message as BitFieldMessage);
            }
            else if (message is RequestMessage)
            {
                ProcessRecievedMessage(message as RequestMessage);
            }
            else if (message is PieceMessage)
            {
                ProcessRecievedMessage(message as PieceMessage);
            }
            else if (message is CancelMessage)
            {
                ProcessRecievedMessage(message as CancelMessage);
            }
            else if (message is PortMessage)
            {
                // TODO
            }
            else if (message is KeepAliveMessage)
            {
                // do nothing
            }
        }
    }

    /// <summary>
    ///     Processes the received message.
    /// </summary>
    /// <param name="message">The message.</param>
    private void ProcessRecievedMessage(CancelMessage message)
    {
        message.CannotBeNull();

        if (HandshakeState == HandshakeState.SendAndReceived)
        {
            if (message.PieceIndex >= 0 &&
                message.PieceIndex < pieceManager.PieceCount &&
                message.BlockOffset >= 0 &&
                message.BlockOffset < pieceManager.PieceLength &&
                message.BlockOffset % pieceManager.BlockLength == 0 &&
                message.BlockLength == pieceManager.GetBlockLength(message.PieceIndex, message.BlockOffset / pieceManager.BlockLength))
                EnqueueUploadMessage(message);
            else
                OnCommunicationErrorOccurred(this, new PeerCommunicationErrorEventArgs("Invalid cancel message.", false));
        }
        else
        {
            OnCommunicationErrorOccurred(this, new PeerCommunicationErrorEventArgs("Invalid message sequence.", true));
        }
    }

    /// <summary>
    ///     Processes the received message.
    /// </summary>
    /// <param name="message">The message.</param>
    private void ProcessRecievedMessage(PieceMessage message)
    {
        message.CannotBeNull();

        if (HandshakeState == HandshakeState.SendAndReceived)
        {
            if (message.PieceIndex >= 0 &&
                message.PieceIndex < pieceManager.PieceCount &&
                message.BlockOffset >= 0 &&
                message.BlockOffset < pieceManager.PieceLength &&
                message.BlockOffset % pieceManager.BlockLength == 0 &&
                message.Data.Length == pieceManager.GetPieceLength(message.PieceIndex))
                EnqueueDownloadMessage(message);
            else
                OnCommunicationErrorOccurred(this, new PeerCommunicationErrorEventArgs("Invalid piece message.", false));
        }
        else
        {
            OnCommunicationErrorOccurred(this, new PeerCommunicationErrorEventArgs("Invalid message sequence.", true));
        }
    }

    /// <summary>
    ///     Processes the received message.
    /// </summary>
    /// <param name="message">The message.</param>
    private void ProcessRecievedMessage(RequestMessage message)
    {
        message.CannotBeNull();

        if (HandshakeState == HandshakeState.SendAndReceived)
        {
            if (message.PieceIndex >= 0 &&
                message.PieceIndex < pieceManager.BlockCount &&
                message.BlockOffset >= 0 &&
                message.BlockOffset < pieceManager.GetBlockCount(message.PieceIndex) &&
                message.BlockOffset / pieceManager.BlockLength == 0 &&
                message.BlockLength == pieceManager.GetBlockLength(message.PieceIndex, message.BlockOffset / pieceManager.BlockLength))
                EnqueueUploadMessage(message);
            else
                OnCommunicationErrorOccurred(this, new PeerCommunicationErrorEventArgs("Invalid request message.", false));
        }
        else
        {
            OnCommunicationErrorOccurred(this, new PeerCommunicationErrorEventArgs("Invalid message sequence.", true));
        }
    }

    /// <summary>
    ///     Processes the received message.
    /// </summary>
    /// <param name="message">The message.</param>
    private void ProcessRecievedMessage(BitFieldMessage message)
    {
        message.CannotBeNull();

        if (HandshakeState == HandshakeState.SendAndReceived)
        {
            if (message.BitField.Length >= pieceManager.BlockCount)
            {
                for (var i = 0; i < BitField.Length; i++)
                    BitField[i] = message.BitField[i];

                // notify downloading thread
                EnqueueDownloadMessage(message);
            }
            else
            {
                OnCommunicationErrorOccurred(this, new PeerCommunicationErrorEventArgs("Invalid bit field message.", true));
            }
        }
        else
        {
            OnCommunicationErrorOccurred(this, new PeerCommunicationErrorEventArgs("Invalid message sequence.", true));
        }
    }

    /// <summary>
    ///     Processes the received message.
    /// </summary>
    /// <param name="message">The message.</param>
    private void ProcessRecievedMessage(ChokeMessage message)
    {
        message.CannotBeNull();

        if (HandshakeState == HandshakeState.SendAndReceived)
            EnqueueDownloadMessage(message);
        else
            OnCommunicationErrorOccurred(this, new PeerCommunicationErrorEventArgs("Invalid message sequence.", true));
    }

    /// <summary>
    ///     Processes the received message.
    /// </summary>
    /// <param name="message">The message.</param>
    private void ProcessRecievedMessage(UnchokeMessage message)
    {
        message.CannotBeNull();

        if (HandshakeState == HandshakeState.SendAndReceived)
            EnqueueDownloadMessage(message);
        else
            OnCommunicationErrorOccurred(this, new PeerCommunicationErrorEventArgs("Invalid message sequence.", true));
    }

    /// <summary>
    ///     Processes the received message.
    /// </summary>
    /// <param name="message">The message.</param>
    private void ProcessRecievedMessage(InterestedMessage message)
    {
        message.CannotBeNull();

        if (HandshakeState == HandshakeState.SendAndReceived)
            EnqueueDownloadMessage(message);
        else
            OnCommunicationErrorOccurred(this, new PeerCommunicationErrorEventArgs("Invalid message sequence.", true));
    }

    /// <summary>
    ///     Processes the received message.
    /// </summary>
    /// <param name="message">The message.</param>
    private void ProcessRecievedMessage(UninterestedMessage message)
    {
        message.CannotBeNull();

        if (HandshakeState == HandshakeState.SendAndReceived)
            EnqueueUploadMessage(message);
        else
            OnCommunicationErrorOccurred(this, new PeerCommunicationErrorEventArgs("Invalid message sequence.", true));
    }

    /// <summary>
    ///     Processes the received message.
    /// </summary>
    /// <param name="message">The message.</param>
    private void ProcessRecievedMessage(HaveMessage message)
    {
        message.CannotBeNull();

        if (HandshakeState == HandshakeState.SendAndReceived)
        {
            if (message.PieceIndex >= 0 &&
                message.PieceIndex < pieceManager.PieceCount)
            {
                BitField[message.PieceIndex] = true;

                // notify downloading thread
                EnqueueDownloadMessage(message);
            }
            else
            {
                OnCommunicationErrorOccurred(this, new PeerCommunicationErrorEventArgs("Invalid have message.", false));
            }
        }
        else
        {
            OnCommunicationErrorOccurred(this, new PeerCommunicationErrorEventArgs("Invalid message sequence.", true));
        }
    }

    /// <summary>
    ///     Processes the received message.
    /// </summary>
    /// <param name="message">The message.</param>
    private void ProcessRecievedMessage(HandshakeMessage message)
    {
        message.CannotBeNull();

        if (HandshakeState == HandshakeState.None ||
            HandshakeState == HandshakeState.SentButNotReceived)
        {
            if (message.InfoHash == pieceManager.TorrentInfoHash &&
                message.ProtocolString == HandshakeMessage.ProtocolName &&
                message.PeerId.IsNotNullOrEmpty() &&
                message.PeerId != localPeerId)
            {
                if (HandshakeState == HandshakeState.None)
                {
                    HandshakeState = HandshakeState.ReceivedButNotSent;
                    PeerId = message.PeerId;

                    // send a handshake
                    EnqueueSendMessage(new HandshakeMessage(pieceManager.TorrentInfoHash, localPeerId));

                    // send a bit field
                    EnqueueSendMessage(new BitFieldMessage(pieceManager.BitField.Select(x => x == PieceStatus.Present).ToArray()));
                }
                else if (HandshakeState == HandshakeState.SentButNotReceived)
                {
                    HandshakeState = HandshakeState.SendAndReceived;
                    PeerId = message.PeerId;

                    // send a bit field
                    EnqueueSendMessage(new BitFieldMessage(pieceManager.BitField.Select(x => x == PieceStatus.Present).ToArray()));
                }
            }
            else
            {
                OnCommunicationErrorOccurred(this, new PeerCommunicationErrorEventArgs("Invalid handshake message.", true));
            }
        }
        else
        {
            OnCommunicationErrorOccurred(this, new PeerCommunicationErrorEventArgs("Invalid message sequence.", true));
        }
    }

    /// <summary>
    ///     The message sending thread.
    /// </summary>
    private void Send()
    {
        var timeout = TimeSpan.FromMilliseconds(250);
        IEnumerable<PeerMessage> messages;

        while (!IsDisposed)
        {
            messages = DequeueSendMessages();

            if (messages.Count() > 0)
                if (communicator != null &&
                    !communicator.IsDisposed)
                {
                    foreach (var message in messages)
                        Debug.WriteLine($"{Endpoint} -> {message}");

                    // send message
                    communicator.Send(messages);

                    UpdateTrafficParameters(0, messages.Sum(x => x.Length));
                }

            lastMessageSentTime = DateTime.UtcNow;

            Thread.Sleep(timeout);
        }
    }

    /// <summary>
    ///     Starts the downloading.
    /// </summary>
    private void StartDownloading()
    {
        Thread thread;

        if (!isDownloading)
        {
            thread = new Thread(Download);
            thread.IsBackground = true;
            thread.Name = PeerId + " downloader";
            thread.Start();
        }
    }

    /// <summary>
    ///     Starts the keeping connection alive.
    /// </summary>
    private void StartKeepingConnectionAlive()
    {
        Thread thread;

        if (isDownloading ||
            isUploading)
        {
            thread = new Thread(KeepAlive);
            thread.IsBackground = true;
            thread.Name = PeerId + " keeping alive";
            thread.Start();
        }
    }

    /// <summary>
    ///     Starts the sending.
    /// </summary>
    private void StartSending()
    {
        Thread thread;

        if (!isDownloading)
        {
            thread = new Thread(Send);
            thread.IsBackground = true;
            thread.Name = PeerId + " sender";
            thread.Start();
        }
    }

    /// <summary>
    ///     Starts the uploading.
    /// </summary>
    private void StartUploading()
    {
        Thread thread;

        if (!isUploading)
        {
            thread = new Thread(Upload);
            thread.IsBackground = true;
            thread.Name = PeerId + "uploader";
            thread.Start();
        }
    }

    /// <summary>
    ///     Updates the traffic parameters.
    /// </summary>
    /// <param name="downloaded">The downloaded byte count.</param>
    /// <param name="uploaded">The uploaded byte count.</param>
    private void UpdateTrafficParameters(long downloaded, long uploaded)
    {
        downloaded.MustBeGreaterThanOrEqualTo(0);
        uploaded.MustBeGreaterThanOrEqualTo(0);

        lock (locker)
        {
            previouslyDownloaded += downloaded;
            previouslyUploaded += uploaded;

            if (stopwatch.Elapsed > TimeSpan.FromSeconds(1))
            {
                downloadSpeed = previouslyDownloaded / (decimal)stopwatch.Elapsed.TotalSeconds;
                uploadSpeed = previouslyUploaded / (decimal)stopwatch.Elapsed.TotalSeconds;

                Downloaded += previouslyDownloaded;
                Uploaded += previouslyUploaded;

                previouslyDownloaded = 0;
                previouslyUploaded = 0;

                stopwatch.Restart();
            }
        }
    }

    /// <summary>
    ///     The uploading thread.
    /// </summary>
    private void Upload()
    {
        var timeout = TimeSpan.FromMilliseconds(250);
        Piece piece = null;
        RequestMessage rm;

        if (!isUploading)
        {
            isUploading = true;

            while (!IsDisposed)
            {
                foreach (var message in DequeueUploadMessages())
                    if (message is RequestMessage)
                    {
                        rm = message as RequestMessage;

                        if (piece == null ||
                            piece.PieceIndex != rm.PieceIndex)
                            // get the piece
                            piece = pieceManager.GetPiece(rm.PieceIndex);

                        if (piece != null &&
                            piece.PieceLength > rm.BlockOffset)
                            // return the piece
                            EnqueueSendMessage(new PieceMessage(rm.PieceIndex, rm.BlockOffset, (int)piece.GetBlockLength(rm.PieceIndex), piece.GetBlock(rm.PieceIndex)));
                        // invalid requeste received -> ignore
                    }
                    else if (message is CancelMessage)
                    {
                        // TODO
                    }
                    else if (message is InterestedMessage)
                    {
                        LeechingState = LeechingState.Interested;
                    }
                    else if (message is UninterestedMessage)
                    {
                        LeechingState = LeechingState.Uninterested;
                    }

                Thread.Sleep(timeout);
            }

            isUploading = false;
        }
    }
}
