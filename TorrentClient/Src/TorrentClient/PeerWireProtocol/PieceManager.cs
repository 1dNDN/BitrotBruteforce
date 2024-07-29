using System.Timers;

using DefensiveProgrammingFramework;

using Timer = System.Timers.Timer;

namespace TorrentClient.PeerWireProtocol;

/// <summary>
///     The piece manager.
/// </summary>
public sealed class PieceManager : IDisposable
{
    /// <summary>
    ///     The checkout timeout.
    /// </summary>
    private readonly TimeSpan checkoutTimeout = TimeSpan.FromSeconds(120);

    /// <summary>
    ///     The timer timeout.
    /// </summary>
    private readonly TimeSpan timerTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     The check out piece index / check out time dictionary.
    /// </summary>
    private readonly Dictionary<int, DateTime> checkouts;

    /// <summary>
    ///     The thread locker.
    /// </summary>
    private readonly object locker = new();

    /// <summary>
    ///     The piece hashes.
    /// </summary>
    private readonly IEnumerable<string> pieceHashes;

    /// <summary>
    ///     The pieces count.
    /// </summary>
    private readonly int piecesCount = 0;

    /// <summary>
    ///     The present pieces count.
    /// </summary>
    private int presentPiecesCount = 0;

    /// <summary>
    ///     The checkout timer.
    /// </summary>
    private Timer timer;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PieceManager" /> class.
    /// </summary>
    /// <param name="torrentInfoHash">The torrent information hash.</param>
    /// <param name="torrentLength">Length of the torrent.</param>
    /// <param name="pieceHashes">The piece hashes.</param>
    /// <param name="pieceLength">Length of the piece.</param>
    /// <param name="blockLength">Length of the block.</param>
    /// <param name="bitField">The bit field.</param>
    public PieceManager(string torrentInfoHash, long torrentLength, IEnumerable<string> pieceHashes, long pieceLength, int blockLength, PieceStatus[] bitField)
    {
        torrentInfoHash.CannotBeNullOrEmpty();
        pieceHashes.CannotBeNullOrEmpty();
        pieceLength.MustBeGreaterThan(0);
        ((long)blockLength).MustBeLessThanOrEqualTo(pieceLength);
        (pieceLength % blockLength).MustBeEqualTo(0);
        bitField.CannotBeNull();
        bitField.Length.MustBeEqualTo(pieceHashes.Count());

        PieceLength = pieceLength;
        BlockLength = blockLength;
        BlockCount = (int)(pieceLength / blockLength);

        TorrentInfoHash = torrentInfoHash;
        TorrentLength = torrentLength;

        this.pieceHashes = pieceHashes;

        BitField = bitField;

        for (var i = 0; i < BitField.Length; i++)
        {
            if (BitField[i] != PieceStatus.Ignore)
                piecesCount++;

            if (bitField[i] == PieceStatus.Present)
                presentPiecesCount++;
        }

        checkouts = new Dictionary<int, DateTime>();

        // setup checkout timer
        timer = new Timer();
        timer.Interval = timerTimeout.TotalMilliseconds;
        timer.Elapsed += Timer_Elapsed;
        timer.Enabled = true;
        timer.Start();
    }

    /// <summary>
    ///     Prevents a default instance of the <see cref="PieceManager" /> class from being created.
    /// </summary>
    private PieceManager()
    {
    }

    /// <summary>
    ///     Gets the bit field.
    /// </summary>
    /// <value>
    ///     The bit field.
    /// </value>
    public PieceStatus[] BitField { get; }

    /// <summary>
    ///     Gets the block count.
    /// </summary>
    /// <value>
    ///     The block count.
    /// </value>
    public int BlockCount { get; }

    /// <summary>
    ///     Gets the length of the block.
    /// </summary>
    /// <value>
    ///     The length of the block.
    /// </value>
    public int BlockLength { get; }

    /// <summary>
    ///     Gets the completed percentage.
    /// </summary>
    /// <value>
    ///     The completed percentage.
    /// </value>
    public decimal CompletedPercentage { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether all pieces are complete.
    /// </summary>
    /// <value>
    ///     <c>true</c> if all pieces are complete; otherwise, <c>false</c>.
    /// </value>
    public bool IsComplete
    {
        get {
            CheckIfObjectIsDisposed();

            return presentPiecesCount == piecesCount;
        }
    }

    /// <summary>
    ///     Gets a value indicating whether the object is disposed.
    /// </summary>
    /// <value>
    ///     <c>true</c> if object is disposed; otherwise, <c>false</c>.
    /// </value>
    public bool IsDisposed { get; }

    /// <summary>
    ///     Gets a value indicating whether it is end game (only 5% pieces is missing).
    /// </summary>
    /// <value>
    ///     <c>true</c> if it is end game; otherwise, <c>false</c>.
    /// </value>
    public bool IsEndGame
    {
        get {
            CheckIfObjectIsDisposed();

            return CompletedPercentage >= 0.95m;
        }
    }

    /// <summary>
    ///     Gets the piece count.
    /// </summary>
    /// <value>
    ///     The piece count.
    /// </value>
    public int PieceCount
    {
        get {
            CheckIfObjectIsDisposed();

            return BitField.Length;
        }
    }

    /// <summary>
    ///     Gets the length of the piece.
    /// </summary>
    /// <value>
    ///     The length of the piece.
    /// </value>
    public long PieceLength { get; }

    /// <summary>
    ///     Gets the torrent information hash.
    /// </summary>
    /// <value>
    ///     The torrent information hash.
    /// </value>
    public string TorrentInfoHash { get; private set; }

    /// <summary>
    ///     Gets the length of the torrent.
    /// </summary>
    /// <value>
    ///     The length of the torrent.
    /// </value>
    public long TorrentLength { get; }

    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        CheckIfObjectIsDisposed();

        if (timer != null)
        {
            timer.Stop();
            timer.Enabled = false;
            timer.Dispose();
            timer = null;
        }
    }

    /// <summary>
    ///     Occurs when piece has completed.
    /// </summary>
    public event EventHandler<PieceCompletedEventArgs> PieceCompleted;

    /// <summary>
    ///     Occurs when block is requested.
    /// </summary>
    public event EventHandler<PieceRequestedEventArgs> PieceRequested;

    /// <summary>
    ///     Checks out the piece.
    /// </summary>
    /// <param name="pieceIndex">Index of the piece.</param>
    /// <param name="pieceData">The piece data.</param>
    /// <param name="bitField">The bit field.</param>
    /// <returns>
    ///     The piece.
    /// </returns>
    public Piece CheckOut(int pieceIndex, byte[] pieceData = null, bool[] bitField = null)
    {
        pieceIndex.MustBeGreaterThanOrEqualTo(0);
        pieceIndex.MustBeLessThanOrEqualTo(PieceCount);
        pieceData.IsNotNull().Then(() => pieceData.LongLength.MustBeEqualTo(GetPieceLength(pieceIndex)));
        bitField.IsNotNull().Then(() => bitField.Length.MustBeEqualTo(GetBlockCount(pieceIndex)));

        CheckIfObjectIsDisposed();

        Piece piece = null;
        string hash;
        var pieceLength = PieceLength;
        var blockCount = BlockCount;

        if (pieceData != null)
            Array.Clear(pieceData, 0, pieceData.Length);

        if (bitField != null)
            Array.Clear(bitField, 0, bitField.Length);

        lock (locker)
        {
            // only missing pieces can be checked out
            if (BitField[pieceIndex] == PieceStatus.Missing ||
                (BitField[pieceIndex] == PieceStatus.CheckedOut &&
                 IsEndGame))
            {
                hash = pieceHashes.ElementAt(pieceIndex);
                pieceLength = GetPieceLength(pieceIndex);
                blockCount = GetBlockCount(pieceIndex);

                piece = new Piece(pieceIndex, hash, pieceLength, BlockLength, blockCount, pieceData, bitField);
                piece.Completed += Piece_Completed;
                piece.Corrupted += Piece_Corrupted;

                BitField[pieceIndex] = PieceStatus.CheckedOut;

                if (!checkouts.ContainsKey(pieceIndex))
                    checkouts.Add(pieceIndex, DateTime.UtcNow);
            }
        }

        return piece;
    }

    /// <summary>
    ///     Gets the block count.
    /// </summary>
    /// <param name="pieceIndex">Index of the piece.</param>
    /// <returns>The block count.</returns>
    public int GetBlockCount(int pieceIndex)
    {
        pieceIndex.MustBeGreaterThanOrEqualTo(0);
        pieceIndex.MustBeLessThan(PieceCount);

        CheckIfObjectIsDisposed();

        var pieceLength = GetPieceLength(pieceIndex);
        long blockLength = BlockLength;
        var remainder = pieceLength % blockLength;
        long blockCount;

        blockCount = (pieceLength - remainder) / blockLength;
        blockCount += remainder > 0 ? 1 : 0;

        return (int)blockCount;
    }

    /// <summary>
    ///     Gets the length of the block in bytes.
    /// </summary>
    /// <param name="pieceIndex">Index of the piece.</param>
    /// <param name="blockIndex">The block offset.</param>
    /// <returns>
    ///     The length of the block in bytes.
    /// </returns>
    public int GetBlockLength(int pieceIndex, int blockIndex)
    {
        pieceIndex.MustBeGreaterThanOrEqualTo(0);
        pieceIndex.MustBeLessThan(PieceCount);
        blockIndex.MustBeGreaterThanOrEqualTo(0);
        blockIndex.MustBeLessThan(BlockCount);

        long pieceLength;
        long blockCount;

        CheckIfObjectIsDisposed();

        blockCount = GetBlockCount(pieceIndex);

        if (blockIndex == blockCount - 1)
        {
            pieceLength = GetPieceLength(pieceIndex);

            if (pieceLength % BlockLength != 0)
                // last block can be shorter
                return (int)(pieceLength % BlockLength);
        }

        return BlockLength;
    }

    /// <summary>
    ///     Gets the piece.
    /// </summary>
    /// <param name="pieceIndex">Index of the piece.</param>
    /// <returns>The piece.</returns>
    public Piece GetPiece(int pieceIndex)
    {
        pieceIndex.MustBeGreaterThanOrEqualTo(0);
        pieceIndex.MustBeLessThan(PieceCount);

        CheckIfObjectIsDisposed();

        var e = new PieceRequestedEventArgs(pieceIndex);

        OnPieceRequested(this, e);

        if (e.PieceData != null)
            return new Piece(pieceIndex, pieceHashes.ElementAt(pieceIndex), PieceLength, BlockLength, BlockCount);

        return null;
    }

    /// <summary>
    ///     Gets the length of the piece in bytes.
    /// </summary>
    /// <param name="pieceIndex">Index of the piece.</param>
    /// <returns>
    ///     The length of the piece in bytes.
    /// </returns>
    public long GetPieceLength(int pieceIndex)
    {
        pieceIndex.MustBeGreaterThanOrEqualTo(0);
        pieceIndex.MustBeLessThan(PieceCount);

        CheckIfObjectIsDisposed();

        if (pieceIndex == PieceCount - 1)
            if (TorrentLength % PieceLength != 0)
                // last piece can be shorter
                return TorrentLength % PieceLength;

        return PieceLength;
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
    ///     Called when piece has completed.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnPieceCompleted(object sender, PieceCompletedEventArgs e)
    {
        sender.CannotBeNull();
        e.CannotBeNull();

        if (PieceCompleted != null)
            PieceCompleted(sender, e);
    }

    /// <summary>
    ///     Called when piece is requested.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnPieceRequested(object sender, PieceRequestedEventArgs e)
    {
        sender.CannotBeNull();
        e.CannotBeNull();

        if (PieceRequested != null)
            PieceRequested(sender, e);
    }

    /// <summary>
    ///     Handles the Completed event of the Piece control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="PieceCompletedEventArgs" /> instance containing the event data.</param>
    private void Piece_Completed(object sender, PieceCompletedEventArgs e)
    {
        lock (locker)
        {
            // only pieces not yet downloaded can be checked in
            if (BitField[e.PieceIndex] == PieceStatus.Missing ||
                BitField[e.PieceIndex] == PieceStatus.CheckedOut)
            {
                BitField[e.PieceIndex] = PieceStatus.Present;

                presentPiecesCount++;
                CompletedPercentage = presentPiecesCount / (decimal)piecesCount;

                if (checkouts.ContainsKey(e.PieceIndex))
                    checkouts.Remove(e.PieceIndex);

                OnPieceCompleted(this, new PieceCompletedEventArgs(e.PieceIndex, e.PieceData));
            }
        }
    }

    /// <summary>
    ///     Handles the Corrupted event of the Piece control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
    private void Piece_Corrupted(object sender, EventArgs e)
    {
        // Ignore
    }

    /// <summary>
    ///     Handles the Elapsed event of the Timer control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.Timers.ElapsedEventArgs" /> instance containing the event data.</param>
    private void Timer_Elapsed(object sender, ElapsedEventArgs e)
    {
        DateTime checkoutTime;
        int pieceIndex;
        var checkoutsToRemove = new HashSet<int>();

        Thread.CurrentThread.Name = "piece manager checker";

        timer.Interval = TimeSpan.FromDays(1).TotalMilliseconds;

        lock (locker)
        {
            foreach (var checkOut in checkouts)
            {
                pieceIndex = checkOut.Key;
                checkoutTime = checkOut.Value;

                if (DateTime.UtcNow - checkoutTime > checkoutTimeout)
                    checkoutsToRemove.Add(checkOut.Key);
            }

            foreach (var checkoutToRemove in checkoutsToRemove)
            {
                checkouts.Remove(checkoutToRemove);

                // checkout timeout -> mark piece as missing, giving other peers a chance to download it
                BitField[checkoutToRemove] = PieceStatus.Missing;
            }
        }

        timer.Interval = timerTimeout.TotalMilliseconds;
    }
}
