using System.Diagnostics;

using Bruteforce.TorrentWrapper.Extensions;

namespace Bruteforce.TorrentWrapper;

/// <summary>
///     The persistence manager.
/// </summary>
public sealed class PersistenceManager
{
    /// <summary>
    ///     The file info / file stream dictionary.
    /// </summary>
    private readonly Dictionary<TorrentFileInfo, FileStream> _files;

    /// <summary>
    ///     The thread locker.
    /// </summary>
    private readonly object _locker = new();

    /// <summary>
    ///     The piece hashes.
    /// </summary>
    private readonly IEnumerable<string> _pieceHashes;

    /// <summary>
    ///     The piece length.
    /// </summary>
    private readonly long _pieceLength;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PersistenceManager" /> class.
    /// </summary>
    /// <param name="directoryPath">The directory path.</param>
    /// <param name="pieceLength">Length of the piece.</param>
    /// <param name="pieceHashes">The piece hashes.</param>
    /// <param name="files">The files.</param>
    public PersistenceManager(string directoryPath, long pieceLength, string[] pieceHashes, TorrentFileInfo[] files)
    {
        Debug.WriteLine($"creating persistence manager for {Path.GetFullPath(directoryPath)}");

        DirectoryPath = directoryPath;
        _pieceLength = pieceLength;
        _pieceHashes = pieceHashes;

        // initialize file handlers
        _files = new Dictionary<TorrentFileInfo, FileStream>();

        foreach (var file in files)
        {
            CreateFile(Path.Combine(DirectoryPath, file.FilePath), file.Length);

            _files.Add(file, new FileStream(Path.Combine(DirectoryPath, file.FilePath), FileMode.Open, FileAccess.ReadWrite, FileShare.None));
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
    ///     Verifies the specified file if it corresponds with the piece hashes.
    /// </summary>
    /// <returns>
    ///     The bit field.
    /// </returns>
    public PieceStatus[] Verify()
    {
        var bitField = new PieceStatus[_pieceHashes.Count()];
        long pieceStart;
        long pieceEnd;
        long previousPieceIndex = 0;
        long torrentStartOffset = 0;
        long torrentEndOffset;
        long fileOffset;
        var pieceData = new byte[_pieceLength];
        var pieceOffset = 0;
        var length = 0;

        lock (_locker)
        {
            foreach (var file in _files)
            {
                torrentEndOffset = torrentStartOffset + file.Key.Length;

                pieceStart = (torrentStartOffset - torrentStartOffset % _pieceLength) / _pieceLength;

                pieceEnd = (torrentEndOffset - torrentEndOffset % _pieceLength) / _pieceLength;
                pieceEnd -= torrentEndOffset % _pieceLength == 0
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
                            _pieceHashes.ElementAt((int)previousPieceIndex),
                            pieceData.CalculateSha1Hash(
                                    0,
                                    pieceOffset)
                                .ToHexaDecimalString());

                        bitField[previousPieceIndex] = pieceStatus;

                        previousPieceIndex = pieceIndex;
                        pieceOffset = 0;
                    }

                    fileOffset = (pieceIndex - pieceStart) * _pieceLength;
                    fileOffset -= pieceIndex > pieceStart ? torrentStartOffset % _pieceLength : 0;

                    length = (int)Math.Min(_pieceLength - pieceOffset, file.Key.Length - fileOffset);

                    Read(file.Value, fileOffset, length, pieceData, pieceOffset);

                    pieceOffset += length;
                }

                torrentStartOffset = torrentEndOffset;
            }

            // last piece
            bitField[previousPieceIndex] = GetStatus(
                _pieceHashes.ElementAt((int)previousPieceIndex),
                pieceData.CalculateSha1Hash(
                        0,
                        pieceOffset)
                    .ToHexaDecimalString());
        }

        return bitField;
    }

    /// <summary>
    ///     Creates the file.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="fileLength">Length of the file in bytes.</param>
    /// <returns>True if file was created; false otherwise.</returns>
    private bool CreateFile(string filePath, long fileLength)
    {
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
    private PieceStatus GetStatus(string pieceHash, string calculatedPieceHash)
    {
        for (var i = 0; i < pieceHash.Length; i++)
            if (pieceHash[i] != calculatedPieceHash[i])
                return PieceStatus.Missing;

        return PieceStatus.Present;
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
        if (stream.Length >= offset + length)
        {
            stream.Position = offset;
            stream.Read(buffer, bufferOffset, length);
        }
        else
        {
            throw new ArgumentException("Incorrect file length.");
        }
    }
}
