using System.Diagnostics;

using DefensiveProgrammingFramework;

using TorrentClient.Exceptions;
using TorrentClient.Extensions;
using TorrentClient.PeerWireProtocol;

namespace TorrentClient;

/// <summary>
///     The persistence manager.
/// </summary>
public sealed class PersistenceManager : IDisposable
{
    /// <summary>
    ///     The file info / file stream dictionary.
    /// </summary>
    private readonly Dictionary<TorrentFileInfo, FileStream> files;

    /// <summary>
    ///     The thread locker.
    /// </summary>
    private readonly object locker = new();

    /// <summary>
    ///     The piece hashes.
    /// </summary>
    private readonly IEnumerable<string> pieceHashes;

    /// <summary>
    ///     The piece length.
    /// </summary>
    private readonly long pieceLength;

    /// <summary>
    ///     The torrent length in bytes.
    /// </summary>
    private long torrentLength;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PersistenceManager" /> class.
    /// </summary>
    /// <param name="directoryPath">The directory path.</param>
    /// <param name="torrentLength">Length of the torrent.</param>
    /// <param name="pieceLength">Length of the piece.</param>
    /// <param name="pieceHashes">The piece hashes.</param>
    /// <param name="files">The files.</param>
    public PersistenceManager(string directoryPath, long torrentLength, long pieceLength, IEnumerable<string> pieceHashes, IEnumerable<TorrentFileInfo> files)
    {
        directoryPath.CannotBeNullOrEmpty();
        directoryPath.MustBeValidDirectoryPath();
        files.CannotBeNullOrEmpty();
        pieceLength.MustBeGreaterThan(0);
        pieceHashes.CannotBeNullOrEmpty();

        Debug.WriteLine($"creating persistence manager for {Path.GetFullPath(directoryPath)}");

        DirectoryPath = directoryPath;
        this.torrentLength = torrentLength;
        this.pieceLength = pieceLength;
        this.pieceHashes = pieceHashes;

        // initialize file handlers
        this.files = new Dictionary<TorrentFileInfo, FileStream>();

        foreach (var file in files)
            if (file.Download)
            {
                CreateFile(Path.Combine(DirectoryPath, file.FilePath), file.Length);

                this.files.Add(file, new FileStream(Path.Combine(DirectoryPath, file.FilePath), FileMode.Open, FileAccess.ReadWrite, FileShare.None));
            }
    }

    /// <summary>
    ///     Prevents a default instance of the <see cref="PersistenceManager" /> class from being created.
    /// </summary>
    private PersistenceManager()
    {
    }

    /// <summary>
    ///     Gets the directory path.
    /// </summary>
    /// <value>
    ///     The directory path.
    /// </value>
    public string DirectoryPath { get; }

    /// <summary>
    ///     Gets a value indicating whether the object is disposed.
    /// </summary>
    /// <value>
    ///     <c>true</c> if object is disposed; otherwise, <c>false</c>.
    /// </value>
    public bool IsDisposed { get; private set; }

    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        if (!IsDisposed)
        {
            IsDisposed = true;

            Debug.WriteLine($"disposing persistence manager for {DirectoryPath}");

            foreach (var file in files)
            {
                file.Value.Close();
                file.Value.Dispose();
            }

            files.Clear();
        }
    }

    /// <summary>
    ///     Gets the data.
    /// </summary>
    /// <param name="pieceIndex">Index of the piece.</param>
    /// <returns>
    ///     The piece data.
    /// </returns>
    public byte[] Get(int pieceIndex)
    {
        pieceIndex.MustBeGreaterThanOrEqualTo(0);

        long pieceStart;
        long pieceEnd;
        long torrentStartOffset = 0;
        long torrentEndOffset;
        long fileOffset;
        byte[] pieceData;
        var pieceOffset = 0;
        var length = 0;

        CheckIfObjectIsDisposed();

        lock (locker)
        {
            // calculate length of the data read (it could be less than the specified piece length)
            foreach (var file in files)
            {
                torrentEndOffset = torrentStartOffset + file.Key.Length;

                pieceStart = (torrentStartOffset - torrentStartOffset % pieceLength) / pieceLength;

                pieceEnd = (torrentEndOffset - torrentEndOffset % pieceLength) / pieceLength;
                pieceEnd -= torrentEndOffset % pieceLength == 0 ? 1 : 0;

                if (pieceIndex >= pieceStart &&
                    pieceIndex <= pieceEnd)
                {
                    fileOffset = (pieceIndex - pieceStart) * pieceLength;
                    fileOffset -= pieceIndex > pieceStart ? torrentStartOffset % pieceLength : 0;

                    length += (int)Math.Min(pieceLength - length, file.Key.Length - fileOffset);
                }
                else if (pieceIndex < pieceStart)
                {
                    break;
                }

                torrentStartOffset += file.Key.Length;
            }

            if (length > 0)
            {
                pieceData = new byte[length];
                torrentStartOffset = 0;
                length = 0;

                // read the piece
                foreach (var file in files)
                {
                    torrentEndOffset = torrentStartOffset + file.Key.Length;

                    pieceStart = (torrentStartOffset - torrentStartOffset % pieceLength) / pieceLength;

                    pieceEnd = (torrentEndOffset - torrentEndOffset % pieceLength) / pieceLength;
                    pieceEnd -= torrentEndOffset % pieceLength == 0 ? 1 : 0;

                    if (pieceIndex >= pieceStart &&
                        pieceIndex <= pieceEnd)
                    {
                        fileOffset = (pieceIndex - pieceStart) * pieceLength;
                        fileOffset -= pieceIndex > pieceStart ? torrentStartOffset % pieceLength : 0;

                        length = (int)Math.Min(pieceLength - pieceOffset, file.Key.Length - fileOffset);

                        if (file.Key.Download)
                            Read(file.Value, fileOffset, length, pieceData, pieceOffset);

                        pieceOffset += length;
                    }
                    else if (pieceIndex < pieceStart)
                    {
                        break;
                    }

                    torrentStartOffset = torrentEndOffset;
                }
            }
            else
            {
                throw new TorrentPersistanceException("File cannot be empty.");
            }
        }

        return pieceData;
    }

    /// <summary>
    ///     Puts the data.
    /// </summary>
    /// <param name="files">The files.</param>
    /// <param name="pieceLength">Length of the piece.</param>
    /// <param name="pieceIndex">Index of the piece.</param>
    /// <param name="pieceData">The piece data.</param>
    public void Put(IEnumerable<TorrentFileInfo> files, long pieceLength, long pieceIndex, byte[] pieceData)
    {
        files.CannotBeNullOrEmpty();
        pieceLength.MustBeGreaterThan(0);
        pieceIndex.MustBeGreaterThanOrEqualTo(0);
        pieceData.CannotBeNullOrEmpty();

        long pieceStart;
        long pieceEnd;
        long torrentStartOffset = 0;
        long torrentEndOffset = 0;
        long fileOffset;
        var pieceOffset = 0;
        var length = 0;

        CheckIfObjectIsDisposed();

        lock (locker)
        {
            // verify length of the data written
            foreach (var file in files)
            {
                torrentEndOffset = torrentStartOffset + file.Length;

                pieceStart = (torrentStartOffset - torrentStartOffset % pieceLength) / pieceLength;

                pieceEnd = (torrentEndOffset - torrentEndOffset % pieceLength) / pieceLength;
                pieceEnd -= torrentEndOffset % pieceLength == 0 ? 1 : 0;

                if (pieceIndex >= pieceStart &&
                    pieceIndex <= pieceEnd)
                {
                    fileOffset = (pieceIndex - pieceStart) * pieceLength;
                    fileOffset -= pieceIndex > pieceStart ? torrentStartOffset % pieceLength : 0;

                    length += (int)Math.Min(pieceLength - length, file.Length - fileOffset);
                }
                else if (pieceIndex < pieceStart)
                {
                    break;
                }

                torrentStartOffset = torrentEndOffset;
            }

            if (length == pieceData.Length)
            {
                torrentStartOffset = 0;
                length = 0;

                // write the piece
                foreach (var file in this.files)
                {
                    torrentEndOffset = torrentStartOffset + file.Key.Length;

                    pieceStart = (torrentStartOffset - torrentStartOffset % pieceLength) / pieceLength;

                    pieceEnd = (torrentEndOffset - torrentEndOffset % pieceLength) / pieceLength;
                    pieceEnd -= torrentEndOffset % pieceLength == 0 ? 1 : 0;

                    if (pieceIndex >= pieceStart &&
                        pieceIndex <= pieceEnd)
                    {
                        fileOffset = (pieceIndex - pieceStart) * pieceLength;
                        fileOffset -= pieceIndex > pieceStart ? torrentStartOffset % pieceLength : 0;

                        length = (int)Math.Min(pieceLength - pieceOffset, file.Key.Length - fileOffset);

                        if (file.Key.Download)
                            Write(file.Value, fileOffset, length, pieceData, pieceOffset);

                        pieceOffset += length;
                    }
                    else if (pieceIndex < pieceStart)
                    {
                        break;
                    }

                    torrentStartOffset = torrentEndOffset;
                }
            }
            else
            {
                throw new TorrentPersistanceException("Invalid length.");
            }
        }
    }

    /// <summary>
    ///     Verifies the specified file if it corresponds with the piece hashes.
    /// </summary>
    /// <returns>
    ///     The bit field.
    /// </returns>
    public PieceStatus[] Verify()
    {
        var bitField = new PieceStatus[pieceHashes.Count()];
        long pieceStart;
        long pieceEnd;
        long previousPieceIndex = 0;
        long torrentStartOffset = 0;
        long torrentEndOffset;
        long fileOffset;
        var pieceData = new byte[pieceLength];
        var pieceOffset = 0;
        var length = 0;
        var ignore = false;
        var download = false;

        CheckIfObjectIsDisposed();

        lock (locker)
        {
            foreach (var file in files)
            {
                torrentEndOffset = torrentStartOffset + file.Key.Length;

                pieceStart = (torrentStartOffset - torrentStartOffset % pieceLength) / pieceLength;

                pieceEnd = (torrentEndOffset - torrentEndOffset % pieceLength) / pieceLength;
                pieceEnd -= torrentEndOffset % pieceLength == 0
                    ? 1
                    : 0;

                Debug.WriteLine($"verifying file {file.Value.Name}");

                for (var pieceIndex = pieceStart;
                     pieceIndex <= pieceEnd;
                     pieceIndex++)
                {
                    if (pieceIndex > previousPieceIndex)
                    {
                        var pieceStatus = GetStatus(
                            ignore,
                            download,
                            pieceHashes.ElementAt((int)previousPieceIndex),
                            pieceData.CalculateSha1Hash(
                                    0,
                                    pieceOffset)
                                .ToHexaDecimalString());
                        
                        if(pieceStatus == PieceStatus.Missing)
                            Console.WriteLine(previousPieceIndex);

                        bitField[previousPieceIndex] = pieceStatus;

                        previousPieceIndex = pieceIndex;
                        pieceOffset = 0;
                        ignore = false;
                        download = false;
                    }

                    fileOffset = (pieceIndex - pieceStart) * pieceLength;
                    fileOffset -= pieceIndex > pieceStart ? torrentStartOffset % pieceLength : 0;

                    length = (int)Math.Min(pieceLength - pieceOffset, file.Key.Length - fileOffset);

                    if (file.Key.Download)
                    {
                        Read(file.Value, fileOffset, length, pieceData, pieceOffset);

                        download = true;
                    }
                    else
                    {
                        ignore = true;
                    }

                    ignore = ignore && !file.Key.Download;
                    download = download || file.Key.Download;

                    pieceOffset += length;
                }

                torrentStartOffset = torrentEndOffset;
            }
            
            // last piece
            var status = GetStatus(
                ignore,
                download,
                pieceHashes.ElementAt((int)previousPieceIndex),
                pieceData.CalculateSha1Hash(
                        0,
                        pieceOffset)
                    .ToHexaDecimalString());

            if(status == PieceStatus.Missing)
                Console.WriteLine(previousPieceIndex);

            
            bitField[previousPieceIndex] = status;
        }

        return bitField;
    }

    /// <summary>
    ///     Checks if object is disposed.
    /// </summary>
    private void CheckIfObjectIsDisposed()
    {
        if (IsDisposed)
            throw new ObjectDisposedException("TorrentClient");
    }

    /// <summary>
    ///     Creates the file.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="fileLength">Length of the file in bytes.</param>
    /// <returns>True if file was created; false otherwise.</returns>
    private bool CreateFile(string filePath, long fileLength)
    {
        filePath.CannotBeNullOrEmpty();
        filePath.MustBeValidFilePath();
        fileLength.MustBeGreaterThan(0);

        CheckIfObjectIsDisposed();

        if (!Directory.Exists(Path.GetDirectoryName(filePath)))
        {
            Debug.WriteLine($"creating directory {Path.GetDirectoryName(filePath)}");

            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        }

        if (!File.Exists(filePath))
        {
            Debug.WriteLine($"creating file {filePath}");

            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.SetLength(fileLength);
                stream.Close();
            }

            return true;
        }

        return false;
    }

    /// <summary>
    ///     Gets the status.
    /// </summary>
    /// <param name="ignore">if set to <c>true</c> one of the piece parts is to be ignored.</param>
    /// <param name="download">if set to <c>true</c> one of the piece parts is to be downloaded.</param>
    /// <param name="pieceHash">The piece hash.</param>
    /// <param name="calculatedPieceHash">The calculated piece hash.</param>
    /// <returns>The piece status.</returns>
    private PieceStatus GetStatus(bool ignore, bool download, string pieceHash, string calculatedPieceHash)
    {
        pieceHash.CannotBeNullOrEmpty();
        calculatedPieceHash.CannotBeNullOrEmpty();
        pieceHash.Length.MustBeEqualTo(calculatedPieceHash.Length);

        if (download &&
            !ignore)
        {
            for (var i = 0; i < pieceHash.Length; i++)
                if (pieceHash[i] != calculatedPieceHash[i])
                    return PieceStatus.Missing;

            return PieceStatus.Present;
        }

        if (download &&
            ignore)
            return PieceStatus.Partial;

        if (!download &&
            ignore)
            return PieceStatus.Ignore;

        throw new TorrentPersistanceException("Invalid piece status.");
    }

    /// <summary>
    ///     Reads the specified data at the offset to the file path.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="length">The length.</param>
    /// <param name="buffer">The buffer.</param>
    /// <param name="bufferOffset">The buffer offset.</param>
    /// <exception cref="System.Exception">Incorrect file length.</exception>
    private void Read(FileStream stream, long offset, int length, byte[] buffer, int bufferOffset)
    {
        stream.CannotBeNull();
        offset.MustBeGreaterThanOrEqualTo(0);
        length.MustBeGreaterThan(0);
        buffer.CannotBeNullOrEmpty();
        bufferOffset.MustBeGreaterThanOrEqualTo(0);
        bufferOffset.MustBeLessThanOrEqualTo(buffer.Length - length);

        if (stream.Length >= offset + length)
        {
            stream.Position = offset;
            stream.Read(buffer, bufferOffset, length);
        }
        else
        {
            throw new TorrentPersistanceException("Incorrect file length.");
        }
    }

    /// <summary>
    ///     Writes the specified data at the offset to the file path.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="length">The length.</param>
    /// <param name="buffer">The buffer.</param>
    /// <param name="bufferOffset">The buffer offset.</param>
    /// <exception cref="System.Exception">Incorrect file length.</exception>
    private void Write(FileStream stream, long offset, int length, byte[] buffer, int bufferOffset = 0)
    {
        stream.CannotBeNull();
        offset.MustBeGreaterThanOrEqualTo(0);
        buffer.CannotBeNullOrEmpty();
        length.MustBeGreaterThan(0);
        length.MustBeLessThanOrEqualTo(buffer.Length);
        bufferOffset.MustBeGreaterThanOrEqualTo(0);
        bufferOffset.MustBeLessThanOrEqualTo(buffer.Length - length);

        if (stream.Length >= offset + length)
        {
            stream.Position = offset;
            stream.Write(buffer, bufferOffset, length);
        }
        else
        {
            throw new TorrentPersistanceException("Incorrect file length.");
        }
    }
}
