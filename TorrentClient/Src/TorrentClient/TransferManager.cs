using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

using DefensiveProgrammingFramework;

using TorrentClient.Extensions;
using TorrentClient.PeerWireProtocol;
using TorrentClient.TrackerProtocol;
using TorrentClient.TrackerProtocol.Http;
using TorrentClient.TrackerProtocol.Udp;
using TorrentClient.TrackerProtocol.Udp.Messages.Messages;

namespace TorrentClient;

/// <summary>
///     The torrent transfer manager.
/// </summary>
public sealed class TransferManager : IDisposable
{
    /// <summary>
    ///     The downloaded bytes count.
    /// </summary>
    private long downloaded = 0;

    /// <summary>
    ///     The peers.
    /// </summary>
    private Dictionary<IPEndPoint, Peer> peers = new();

    /// <summary>
    ///     The persistence manager.
    /// </summary>
    private readonly PersistenceManager persistenceManager;

    /// <summary>
    ///     The piece manager.
    /// </summary>
    private PieceManager pieceManager;

    /// <summary>
    ///     The throttling manager.
    /// </summary>
    private readonly ThrottlingManager throttlingManager;

    /// <summary>
    ///     The trackers.
    /// </summary>
    private IDictionary<Uri, Tracker> trackers = new Dictionary<Uri, Tracker>();

    /// <summary>
    ///     The uploaded bytes count.
    /// </summary>
    private long uploaded = 0;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TransferManager" /> class.
    /// </summary>
    /// <param name="listeningPort">The port.</param>
    /// <param name="torrentInfo">The torrent information.</param>
    /// <param name="throttlingManager">The throttling manager.</param>
    /// <param name="persistenceManager">The persistence manager.</param>
    public TransferManager(int listeningPort, TorrentInfo torrentInfo, ThrottlingManager throttlingManager, PersistenceManager persistenceManager)
    {
        listeningPort.MustBeGreaterThanOrEqualTo(IPEndPoint.MinPort);
        listeningPort.MustBeLessThanOrEqualTo(IPEndPoint.MaxPort);
        torrentInfo.CannotBeNull();
        throttlingManager.CannotBeNull();
        persistenceManager.CannotBeNull();

        Tracker tracker = null;

        PeerId = "-AB1100-" + "0123456789ABCDEF".Random(24);

        Debug.WriteLine($"creating torrent manager for torrent {torrentInfo.InfoHash}");
        Debug.WriteLine($"local peer id {PeerId}");

        TorrentInfo = torrentInfo;

        this.throttlingManager = throttlingManager;

        this.persistenceManager = persistenceManager;

        // initialize trackers
        foreach (var trackerUri in torrentInfo.AnnounceList)
        {
            if (trackerUri.Scheme == "http" ||
                trackerUri.Scheme == "https")
                tracker = new HttpTracker(trackerUri, PeerId, torrentInfo.InfoHash, listeningPort);
            else if (trackerUri.Scheme == "udp")
                tracker = new UdpTracker(trackerUri, PeerId, torrentInfo.InfoHash, listeningPort);

            if (tracker != null)
            {
                tracker.TrackingEvent = TrackingEvent.Started;
                tracker.Announcing += Tracker_Announcing;
                tracker.Announced += Tracker_Announced;
                tracker.TrackingFailed += Tracker_TrackingFailed;
                tracker.BytesLeftToDownload = TorrentInfo.Length - Downloaded;
                tracker.WantedPeerCount = 30; // we can handle 30 peers at a time

                trackers.Add(trackerUri, tracker);
            }
            else
            {
                // unsupported tracker protocol
                Debug.WriteLine($"unsupported tracker protocol {trackerUri.Scheme}");
            }
        }
    }

    /// <summary>
    ///     Prevents a default instance of the <see cref="TransferManager" /> class from being created.
    /// </summary>
    private TransferManager()
    {
    }

    /// <summary>
    ///     Gets the completed percentage.
    /// </summary>
    /// <value>
    ///     The completed percentage.
    /// </value>
    public decimal CompletedPercentage => pieceManager.CompletedPercentage;

    /// <summary>
    ///     Gets the downloaded bytes count.
    /// </summary>
    /// <value>
    ///     The downloaded bytes count.
    /// </value>
    public long Downloaded
    {
        get {
            lock (((IDictionary)peers).SyncRoot)
            {
                return downloaded + peers.Values.Sum(x => x.Downloaded);
            }
        }
    }

    /// <summary>
    ///     Gets the download speed in bytes per second.
    /// </summary>
    /// <value>
    ///     The download speed in bytes per second.
    /// </value>
    public decimal DownloadSpeed
    {
        get {
            lock (((IDictionary)peers).SyncRoot)
            {
                return peers.Values.Sum(x => x.DownloadSpeed);
            }
        }
    }

    /// <summary>
    ///     Gets a value indicating whether the object is disposed.
    /// </summary>
    /// <value>
    ///     <c>true</c> if the object is disposed; otherwise, <c>false</c>.
    /// </value>
    public bool IsDisposed { get; private set; }

    /// <summary>
    ///     Gets the leeching peer count.
    /// </summary>
    /// <value>
    ///     The leeching peer count.
    /// </value>
    public int LeechingPeerCount
    {
        get {
            CheckIfObjectIsDisposed();

            lock (((IDictionary)peers).SyncRoot)
            {
                return peers.Values.Count(x => x.LeechingState == LeechingState.Interested);
            }
        }
    }

    /// <summary>
    ///     Gets the peer count.
    /// </summary>
    /// <value>
    ///     The peer count.
    /// </value>
    public int PeerCount
    {
        get {
            CheckIfObjectIsDisposed();

            lock (((IDictionary)peers).SyncRoot)
            {
                return peers.Count;
            }
        }
    }

    /// <summary>
    ///     Gets the peer identifier.
    /// </summary>
    /// <value>
    ///     The peer identifier.
    /// </value>
    public string PeerId { get; }

    /// <summary>
    ///     Gets the seeding peer count.
    /// </summary>
    /// <value>
    ///     The seeding peer count.
    /// </value>
    public int SeedingPeerCount
    {
        get {
            CheckIfObjectIsDisposed();

            lock (((IDictionary)peers).SyncRoot)
            {
                return peers.Values.Count(x => x.SeedingState == SeedingState.Unchoked);
            }
        }
    }

    /// <summary>
    ///     Gets the start time.
    /// </summary>
    /// <value>
    ///     The start time.
    /// </value>
    public DateTime StartTime { get; private set; }

    /// <summary>
    ///     Gets the torrent information.
    /// </summary>
    /// <value>
    ///     The torrent information.
    /// </value>
    public TorrentInfo TorrentInfo { get; }

    /// <summary>
    ///     Gets the uploaded bytes count.
    /// </summary>
    /// <value>
    ///     The uploaded bytes count.
    /// </value>
    public long Uploaded
    {
        get {
            lock (((IDictionary)peers).SyncRoot)
            {
                return uploaded + peers.Values.Sum(x => x.Uploaded);
            }
        }
    }

    /// <summary>
    ///     Gets the upload speed in bytes per second.
    /// </summary>
    /// <value>
    ///     The upload speed in bytes per second.
    /// </value>
    public decimal UploadSpeed
    {
        get {
            lock (((IDictionary)peers).SyncRoot)
            {
                return peers.Values.Sum(x => x.UploadSpeed);
            }
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

            Debug.WriteLine($"disposing torrent manager for torrent {TorrentInfo.InfoHash}");

            Stop();

            lock (((IDictionary)trackers).SyncRoot)
            {
                if (trackers != null)
                {
                    trackers.Clear();
                    trackers = null;
                }
            }

            lock (((IDictionary)peers).SyncRoot)
            {
                if (peers != null)
                {
                    peers.Clear();
                    peers = null;
                }
            }

            if (pieceManager != null)
            {
                pieceManager.Dispose();
                pieceManager = null;
            }
        }
    }

    /// <summary>
    ///     Occurs when torrent transfer has begun hashing.
    /// </summary>
    public event EventHandler<EventArgs> TorrentHashing;

    /// <summary>
    ///     Occurs when torrent is leeching.
    /// </summary>
    public event EventHandler<EventArgs> TorrentLeeching;

    /// <summary>
    ///     Occurs when torrent transfer has begun seeding.
    /// </summary>
    public event EventHandler<EventArgs> TorrentSeeding;

    /// <summary>
    ///     Occurs when torrent has started.
    /// </summary>
    public event EventHandler<EventArgs> TorrentStarted;

    /// <summary>
    ///     Occurs when torrent has stopped.
    /// </summary>
    public event EventHandler<EventArgs> TorrentStopped;

    /// <summary>
    ///     Adds the leecher.
    /// </summary>
    /// <param name="tcp">The TCP.</param>
    /// <param name="peerId">The peer identifier.</param>
    public void AddLeecher(TcpClient tcp, string peerId)
    {
        tcp.CannotBeNull();
        peerId.CannotBeNull();

        Peer peer;
        var maxLeechers = 10;

        lock (((IDictionary)peers).SyncRoot)
        {
            if (!peers.ContainsKey(tcp.Client.RemoteEndPoint as IPEndPoint))
            {
                if (LeechingPeerCount < maxLeechers)
                {
                    Debug.WriteLine($"adding leeching peer {tcp.Client.RemoteEndPoint} to torrent {TorrentInfo.InfoHash}");

                    // setup tcp client
                    tcp.ReceiveBufferSize = Math.Max(TorrentInfo.BlockLength, TorrentInfo.PieceHashes.Count()) + 100;
                    tcp.SendBufferSize = tcp.ReceiveBufferSize;
                    tcp.Client.ReceiveTimeout = (int)TimeSpan.FromSeconds(60).TotalMilliseconds;
                    tcp.Client.SendTimeout = tcp.Client.ReceiveTimeout;

                    // add new peer
                    peer = new Peer(new PeerCommunicator(throttlingManager, tcp), pieceManager, PeerId, peerId);
                    peer.CommunicationErrorOccurred += Peer_CommunicationErrorOccurred;

                    peers.Add(tcp.Client.RemoteEndPoint as IPEndPoint, peer);
                }
                else
                {
                    tcp.Close();
                }
            }
            else
            {
                tcp.Close();
            }
        }
    }

    /// <summary>
    ///     Starts this instance.
    /// </summary>
    public void Start()
    {
        CheckIfObjectIsDisposed();

        StartTime = DateTime.UtcNow;

        Debug.WriteLine($"starting torrent manager for torrent {TorrentInfo.InfoHash}");

        OnTorrentHashing(this, EventArgs.Empty);

        // initialize piece manager
        pieceManager = new PieceManager(TorrentInfo.InfoHash, TorrentInfo.Length, TorrentInfo.PieceHashes, TorrentInfo.PieceLength, TorrentInfo.BlockLength, persistenceManager.Verify());
        pieceManager.PieceCompleted += PieceManager_PieceCompleted;
        pieceManager.PieceRequested += PieceManager_PieceRequested;

        // start tracking
        lock (((IDictionary)trackers).SyncRoot)
        {
            foreach (var tracker in trackers.Values)
                tracker.StartTracking();
        }

        OnTorrentStarted(this, EventArgs.Empty);

        if (pieceManager.IsComplete)
            OnTorrentSeeding(this, EventArgs.Empty);
    }

    /// <summary>
    ///     Stops this instance.
    /// </summary>
    public void Stop()
    {
        CheckIfObjectIsDisposed();

        Debug.WriteLine($"stopping torrent manager for torrent {TorrentInfo.InfoHash}");

        // stop peers
        lock (((IDictionary)peers).SyncRoot)
        {
            foreach (var peer in peers.Values)
            {
                peer.Dispose();

                downloaded += peer.Downloaded;
                uploaded += peer.Uploaded;
            }

            peers.Clear();
        }

        // stop tracking
        lock (((IDictionary)trackers).SyncRoot)
        {
            foreach (var tracker in trackers.Values)
                tracker.StopTracking();
        }

        OnTorrentStopped(this, EventArgs.Empty);

        pieceManager.Dispose();
        pieceManager = null;
    }

    /// <summary>
    ///     Adds the peer.
    /// </summary>
    /// <param name="endpoint">The endpoint.</param>
    private void AddSeeder(IPEndPoint endpoint)
    {
        endpoint.CannotBeNull();

        TcpClient tcp;

        lock (((IDictionary)peers).SyncRoot)
        {
            if (!peers.ContainsKey(endpoint))
            {
                // set up tcp client
                tcp = new TcpClient();
                tcp.ReceiveBufferSize = Math.Max(TorrentInfo.BlockLength, TorrentInfo.PieceHashes.Count()) + 100;
                tcp.SendBufferSize = tcp.ReceiveBufferSize;
                tcp.Client.ReceiveTimeout = (int)TimeSpan.FromSeconds(60).TotalMilliseconds;
                tcp.Client.SendTimeout = tcp.Client.ReceiveTimeout;
                tcp.BeginConnect(endpoint.Address, endpoint.Port, PeerConnected, new AsyncConnectData(endpoint, tcp));
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
    ///     Called when torrent is hashing.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnTorrentHashing(object sender, EventArgs e)
    {
        sender.CannotBeNull();
        e.CannotBeNull();

        if (TorrentHashing != null)
            TorrentHashing(sender, e);
    }

    /// <summary>
    ///     Called when torrent is leeching.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnTorrentLeeching(object sender, EventArgs e)
    {
        sender.CannotBeNull();
        e.CannotBeNull();

        if (TorrentLeeching != null)
            TorrentLeeching(sender, e);
    }

    /// <summary>
    ///     Called when torrent transfer has begun seeding.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnTorrentSeeding(object sender, EventArgs e)
    {
        sender.CannotBeNull();
        e.CannotBeNull();

        if (TorrentSeeding != null)
            TorrentSeeding(sender, e);
    }

    /// <summary>
    ///     Called when torrent has started.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnTorrentStarted(object sender, EventArgs e)
    {
        sender.CannotBeNull();
        e.CannotBeNull();

        if (TorrentStarted != null)
            TorrentStarted(sender, e);
    }

    /// <summary>
    ///     Called when torrent has stopped.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnTorrentStopped(object sender, EventArgs e)
    {
        sender.CannotBeNull();
        e.CannotBeNull();

        if (TorrentStopped != null)
            TorrentStopped(sender, e);
    }

    /// <summary>
    ///     Handles the CommunicationErrorOccurred event of the Peer control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
    private void Peer_CommunicationErrorOccurred(object sender, PeerCommunicationErrorEventArgs e)
    {
        Peer peer;

        peer = sender as Peer;

        if (e.IsFatal)
        {
            Debug.WriteLine($"fatal communication error occurred for peer {peer.Endpoint} on torrent {TorrentInfo.InfoHash}: {e.ErrorMessage}");

            lock (((IDictionary)peers).SyncRoot)
            {
                // update transfer parameters
                downloaded += peer.Downloaded;
                uploaded += peer.Uploaded;

                // something is wrong with the peer -> remove it from the list and close the connection
                if (peers.ContainsKey(peer.Endpoint))
                    peers.Remove(peer.Endpoint);

                // dispose of the peer
                peer.Dispose();
            }
        }
        else
        {
            Debug.WriteLine($"communication error occurred for peer {peer.Endpoint} on torrent {TorrentInfo.InfoHash}: {e.ErrorMessage}");
        }
    }

    /// <summary>
    ///     Peers the connected.
    /// </summary>
    /// <param name="ar">The async result.</param>
    private void PeerConnected(IAsyncResult ar)
    {
        AsyncConnectData data;
        TcpClient tcp;
        Peer peer;
        IPEndPoint endpoint;

        data = ar.AsyncState as AsyncConnectData;
        endpoint = data.Endpoint;

        try
        {
            tcp = data.Tcp;
            tcp.EndConnect(ar);

            lock (((IDictionary)peers).SyncRoot)
            {
                if (peers.ContainsKey(endpoint))
                {
                    // peer is already present
                    tcp.Close();
                    tcp = null;
                }
                else
                {
                    Debug.WriteLine($"adding seeding peer {endpoint} to torrent {TorrentInfo.InfoHash}");

                    // add new peer
                    peer = new Peer(new PeerCommunicator(throttlingManager, tcp), pieceManager, PeerId);
                    peer.CommunicationErrorOccurred += Peer_CommunicationErrorOccurred;

                    peers.Add(endpoint, peer);
                }
            }
        }
        catch (SocketException ex)
        {
            Debug.WriteLine($"could not connect to peer {endpoint}: {ex.Message}");
        }
        catch (IOException ex)
        {
            Debug.WriteLine($"connection to peer {endpoint} was closed: {ex.Message}");
        }
    }

    /// <summary>
    ///     Handles the PieceCompleted event of the PieceManager control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="PieceCompletedEventArgs" /> instance containing the event data.</param>
    private void PieceManager_PieceCompleted(object sender, PieceCompletedEventArgs e)
    {
        Debug.WriteLine($"piece {e.PieceIndex} completed for torrent {TorrentInfo.InfoHash}");

        // persist piece
        persistenceManager.Put(TorrentInfo.Files, TorrentInfo.PieceLength, e.PieceIndex, e.PieceData);

        if (pieceManager.CompletedPercentage == 1)
            OnTorrentSeeding(this, EventArgs.Empty);
        else
            OnTorrentLeeching(this, EventArgs.Empty);
    }

    /// <summary>
    ///     Handles the PieceRequested event of the PieceManager control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="PieceRequestedEventArgs" /> instance containing the event data.</param>
    private void PieceManager_PieceRequested(object sender, PieceRequestedEventArgs e)
    {
        Debug.WriteLine($"piece {e.PieceIndex} requested for torrent {TorrentInfo.InfoHash}");

        // get piece data
        e.PieceData = persistenceManager.Get(e.PieceIndex);
    }

    /// <summary>
    ///     Handles the Announced event of the Tracker control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="AnnouncedEventArgs" /> instance containing the event data.</param>
    private void Tracker_Announced(object sender, AnnouncedEventArgs e)
    {
        lock (((IDictionary)peers).SyncRoot)
        {
            foreach (var endpoint in e.Peers)
                try
                {
                    AddSeeder(endpoint);
                }
                catch (SocketException ex)
                {
                    Debug.WriteLine($"could not connect to peer {endpoint}: {ex.Message}");
                }
        }
    }

    /// <summary>
    ///     Handles the Announcing event of the Tracker control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
    private void Tracker_Announcing(object sender, EventArgs e)
    {
        Tracker tracker;

        tracker = sender as Tracker;
        tracker.BytesDownloaded = Downloaded;
        tracker.BytesLeftToDownload = TorrentInfo.Length - Downloaded;
        tracker.BytesUploaded = Uploaded;
        tracker.TrackingEvent = CompletedPercentage == 1 ? TrackingEvent.Completed : TrackingEvent.Started;
    }

    /// <summary>
    ///     Handles the TrackingFailed event of the Tracker control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="TrackingFailedEventArgs" /> instance containing the event data.</param>
    private void Tracker_TrackingFailed(object sender, TrackingFailedEventArgs e)
    {
        Debug.WriteLine($"tracking failed for tracker {e.TrackerUri} for torrent {TorrentInfo.InfoHash}: \"{e.FailureReason}\"");

        sender.As<Tracker>().Dispose();
    }
}
