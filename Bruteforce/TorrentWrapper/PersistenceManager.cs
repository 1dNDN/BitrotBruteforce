using System.Diagnostics;

using Bruteforce.TorrentWrapper.Extensions;

namespace Bruteforce.TorrentWrapper;

/// <summary>
///     The persistence manager.
/// </summary>
public static class PersistenceManager
{
    public static List<BrokenPiece> Verify(string directoryPath, TorrentInfo torrentInfo)
    {
        return Verify(directoryPath, torrentInfo.PieceLength, torrentInfo.PieceHashes, torrentInfo.Files);
    }

    /// <summary>
    ///     Verifies the specified file if it corresponds with the piece hashes.
    /// </summary>
    /// <returns>
    ///     The bit field.
    /// </returns>
    public static List<BrokenPiece> Verify(string directoryPath, long pieceLength, string[] pieceHashes, TorrentFileInfo[] files)
    {
        var innerFiles = new Dictionary<TorrentFileInfo, FileStream>();

        foreach (var file in files)
        {
            CreateFile(Path.Combine(directoryPath, file.FilePath), file.Length);

            innerFiles.Add(file, new FileStream(Path.Combine(directoryPath, file.FilePath), FileMode.Open, FileAccess.ReadWrite, FileShare.None));
        }

        
        var brokenPieces = new List<BrokenPiece>();
        long previousPieceIndex = 0;
        long torrentStartOffset = 0;
        var pieceData = new byte[pieceLength];
        var pieceOffset = 0;

        foreach (var file in innerFiles)
        {
            var torrentEndOffset = torrentStartOffset + file.Key.Length;

            var pieceStart = (torrentStartOffset - torrentStartOffset % pieceLength) / pieceLength;

            var pieceEnd = (torrentEndOffset - torrentEndOffset % pieceLength) / pieceLength;
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
                    var hash = pieceHashes.ElementAt((int)previousPieceIndex);
                    var pieceStatus = GetStatus(
                        hash,
                        pieceData.CalculateSha1Hash(
                                0,
                                pieceOffset)
                            .ToHexaDecimalString());

                    if(pieceStatus == PieceStatus.Missing)
                    {
                        Console.WriteLine(previousPieceIndex);
                        
                        var pieceDataCopy = new byte[pieceOffset];
                        pieceData.CopyTo(pieceDataCopy, 0);

                        brokenPieces.Add(new BrokenPiece(pieceDataCopy, previousPieceIndex, hash));
                    }

                    previousPieceIndex = pieceIndex;
                    pieceOffset = 0;
                }

                var fileOffset = (pieceIndex - pieceStart) * pieceLength;
                fileOffset -= pieceIndex > pieceStart ? torrentStartOffset % pieceLength : 0;

                var length = (int)Math.Min(pieceLength - pieceOffset, file.Key.Length - fileOffset);

                Read(file.Value, fileOffset, length, pieceData, pieceOffset);

                pieceOffset += length;
            }

            torrentStartOffset = torrentEndOffset;
        }

        var hash2 = pieceHashes.ElementAt((int)previousPieceIndex);
        var pieceStatus2 = GetStatus(
            hash2,
            pieceData.CalculateSha1Hash(
                    0,
                    pieceOffset)
                .ToHexaDecimalString());

        if(pieceStatus2 == PieceStatus.Missing)
        {
            Console.WriteLine(previousPieceIndex);
            
            var pieceDataCopy = new byte[pieceOffset];
            pieceData.Take(pieceOffset).ToArray().CopyTo(pieceDataCopy, 0);

            brokenPieces.Add(new BrokenPiece(pieceDataCopy, previousPieceIndex, hash2));
        }

        return brokenPieces;
    }

    /// <summary>
    ///     Creates the file.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="fileLength">Length of the file in bytes.</param>
    /// <returns>True if file was created; false otherwise.</returns>
    private static bool CreateFile(string filePath, long fileLength)
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
    private static PieceStatus GetStatus(string pieceHash, string calculatedPieceHash)
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
    private static void Read(FileStream stream, long offset, int length, byte[] buffer, int bufferOffset)
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
