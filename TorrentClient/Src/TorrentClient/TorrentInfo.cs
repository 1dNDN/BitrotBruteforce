using System.Collections.ObjectModel;
using System.Text;

using DefensiveProgrammingFramework;

using TorrentClient.BEncoding;
using TorrentClient.Exceptions;
using TorrentClient.Extensions;
using TorrentClient.PeerWireProtocol.Messages;

namespace TorrentClient;

/// <summary>
///     The torrent file.
/// </summary>
public sealed class TorrentInfo
{
    /// <summary>
    ///     The dictionary.
    /// </summary>
    private readonly BEncodedDictionary dictionary;

    /// <summary>
    ///     Prevents a default instance of the <see cref="TorrentInfo" /> class from being created.
    /// </summary>
    private TorrentInfo()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="TorrentInfo" /> class.
    /// </summary>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="infoHash">The hash.</param>
    /// <param name="pieceLength">Length of the piece.</param>
    /// <param name="piecesHashValues">The pieces.</param>
    /// <param name="isPrivate">if set to <c>true</c> this torrent is private.</param>
    /// <param name="announceList">The announce list.</param>
    /// <param name="creationDate">The creation date.</param>
    /// <param name="comment">The comment.</param>
    /// <param name="createdBy">The created by.</param>
    /// <param name="encoding">The encoding.</param>
    /// <param name="files">The files.</param>
    private TorrentInfo(BEncodedDictionary dictionary, string infoHash, long pieceLength, IEnumerable<string> piecesHashValues, bool isPrivate, IEnumerable<Uri> announceList, DateTime? creationDate, string comment, string createdBy, Encoding encoding, IEnumerable<TorrentFileInfo> files)
    {
        dictionary.CannotBeNull();
        infoHash.CannotBeNullOrEmpty();
        infoHash.Length.MustBeEqualTo(40);
        pieceLength.MustBeGreaterThan(0);
        piecesHashValues.CannotBeNullOrEmpty();
        announceList.CannotBeNullOrEmpty();
        encoding.CannotBeNull();
        files.CannotBeNullOrEmpty();

        this.dictionary = dictionary;
        InfoHash = infoHash;
        PieceLength = pieceLength;
        PieceHashes = piecesHashValues;
        IsPrivate = isPrivate;
        AnnounceList = announceList;
        CreationDate = creationDate;
        Comment = comment;
        CreatedBy = createdBy;
        Encoding = encoding;
        Files = files;
        Length = files.Sum(x => x.Length);
        PiecesCount = piecesHashValues.Count();
        BlockLength = PeerMessage.DefaultBlockLength;
        BlocksCount = (int)(pieceLength / PeerMessage.DefaultBlockLength);
    }

    /// <summary>
    ///     Gets the announce URI list.
    /// </summary>
    /// <value>
    ///     The announce URI list.
    /// </value>
    public IEnumerable<Uri> AnnounceList { get; private set; }

    /// <summary>
    ///     Gets the length of the block.
    /// </summary>
    /// <value>
    ///     The length of the block.
    /// </value>
    public int BlockLength { get; }

    /// <summary>
    ///     Gets the blocks count.
    /// </summary>
    /// <value>
    ///     The blocks count.
    /// </value>
    public int BlocksCount { get; private set; }

    /// <summary>
    ///     Gets the comment.
    /// </summary>
    /// <value>
    ///     The comment.
    /// </value>
    public string Comment { get; private set; }

    /// <summary>
    ///     Gets the created by.
    /// </summary>
    /// <value>
    ///     The created by.
    /// </value>
    public string CreatedBy { get; private set; }

    /// <summary>
    ///     Gets the creation date.
    /// </summary>
    /// <value>
    ///     The creation date.
    /// </value>
    public DateTime? CreationDate { get; private set; }

    /// <summary>
    ///     Gets the encoding.
    /// </summary>
    /// <value>
    ///     The encoding.
    /// </value>
    public Encoding Encoding { get; private set; }

    /// <summary>
    ///     Gets the files.
    /// </summary>
    /// <value>
    ///     The files.
    /// </value>
    public IEnumerable<TorrentFileInfo> Files { get; }

    /// <summary>
    ///     Gets the information hash.
    /// </summary>
    /// <value>
    ///     The information hash.
    /// </value>
    public string InfoHash { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether the torrent is private.
    /// </summary>
    /// <value>
    ///     <c>true</c> if the torrent is private; otherwise, <c>false</c>.
    /// </value>
    public bool IsPrivate { get; private set; }

    /// <summary>
    ///     Gets the total torrent length in bytes.
    /// </summary>
    /// <value>
    ///     The length in bytes.
    /// </value>
    public long Length { get; }

    /// <summary>
    ///     Gets the piece hashes.
    /// </summary>
    /// <value>
    ///     The piece hashes.
    /// </value>
    public IEnumerable<string> PieceHashes { get; private set; }

    /// <summary>
    ///     Gets the length of the piece in bytes.
    /// </summary>
    /// <value>
    ///     The length of the piece in bytes.
    /// </value>
    public long PieceLength { get; }

    /// <summary>
    ///     Gets the pieces count.
    /// </summary>
    /// <value>
    ///     The pieces count.
    /// </value>
    public int PiecesCount { get; }

    /// <summary>
    ///     Tries to load the torrent info from the specified binary data.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <param name="torrentInfo">The torrent information.</param>
    /// <returns>True if torrent was loaded successfully, false otherwise.</returns>
    public static bool TryLoad(byte[] data, out TorrentInfo torrentInfo)
    {
        data.CannotBeNullOrEmpty();

        BEncodedValue value;
        BEncodedDictionary general;
        BEncodedDictionary info;
        var files = new List<TorrentFileInfo>();
        long pieceLength;
        var pieceHashes = new List<string>();
        var isPrivate = false;
        Uri tmpUri;
        var announceList = new List<Uri>();
        DateTime? creationDate = null;
        string comment = null;
        string createdBy = null;
        var encoding = Encoding.ASCII;
        string filePath;
        long fileLength = 0;
        string fileHash;
        string tmpString;
        var infoKey = new BEncodedString("info");
        var pieceLengthKey = new BEncodedString("piece length");
        var piecesKey = new BEncodedString("pieces");
        var privateKey = new BEncodedString("private");
        var nameKey = new BEncodedString("name");
        var lengthKey = new BEncodedString("length");
        var md5sumKey = new BEncodedString("md5sum");
        var filesKey = new BEncodedString("files");
        var pathKey = new BEncodedString("path");
        var announceKey = new BEncodedString("announce");
        var announceListKey = new BEncodedString("announce-list");
        var creationDateKey = new BEncodedString("creation date");
        var commentKey = new BEncodedString("comment");
        var createdByKey = new BEncodedString("created by");
        var encodingKey = new BEncodedString("encoding");

        torrentInfo = null;

        try
        {
            value = BEncodedValue.Decode(data);
        }
        catch (BEncodingException)
        {
            return false;
        }

        if (value is BEncodedDictionary)
        {
            general = value as BEncodedDictionary;

            if (general.ContainsKey(infoKey) &&
                general[infoKey] is BEncodedDictionary)
            {
                info = general[infoKey] as BEncodedDictionary;

                // piece length
                if (info.ContainsKey(pieceLengthKey) &&
                    info[pieceLengthKey] is BEncodedNumber)
                    pieceLength = info[pieceLengthKey].As<BEncodedNumber>().Number;
                else
                    return false;

                // pieces
                if (info.ContainsKey(piecesKey) &&
                    info[piecesKey] is BEncodedString &&
                    info[piecesKey].As<BEncodedString>().TextBytes.Length % 20 == 0)
                {
                    for (var i = 0; i < info[piecesKey].As<BEncodedString>().TextBytes.Length; i += 20)
                    {
                        var tmpBytes = new byte[20];

                        Array.Copy(info[piecesKey].As<BEncodedString>().TextBytes, i, tmpBytes, 0, tmpBytes.Length);

                        pieceHashes.Add(tmpBytes.ToHexaDecimalString());
                    }

                    if (pieceHashes.Count == 0)
                        return false;
                }
                else
                {
                    return false;
                }

                // is private
                if (info.ContainsKey(privateKey) &&
                    info[privateKey] is BEncodedNumber)
                    isPrivate = info[privateKey].As<BEncodedNumber>().Number == 1;

                // files
                if (info.ContainsKey(nameKey) &&
                    info[nameKey] is BEncodedString &&
                    info.ContainsKey(lengthKey) &&
                    info[lengthKey] is BEncodedNumber)
                {
                    // single file
                    filePath = info[nameKey].As<BEncodedString>().Text;
                    fileLength = info[lengthKey].As<BEncodedNumber>().Number;

                    if (info.ContainsKey(md5sumKey) &&
                        info[md5sumKey] is BEncodedString)
                        fileHash = info[md5sumKey].As<BEncodedString>().Text;
                    else
                        fileHash = null;

                    files.Add(new TorrentFileInfo(filePath, fileHash, fileLength));
                }
                else if (info.ContainsKey(nameKey) &&
                         info[nameKey] is BEncodedString &&
                         info.ContainsKey(filesKey) &&
                         info[filesKey] is BEncodedList)
                {
                    tmpString = info[nameKey].As<BEncodedString>().Text;

                    // multi file
                    foreach (var item in info[filesKey].As<BEncodedList>())
                        if (item is BEncodedDictionary &&
                            item.As<BEncodedDictionary>().ContainsKey(pathKey) &&
                            item.As<BEncodedDictionary>()[pathKey] is BEncodedList &&
                            item.As<BEncodedDictionary>()[pathKey].As<BEncodedList>().All(x => x is BEncodedString) &&
                            item.As<BEncodedDictionary>().ContainsKey(lengthKey) &&
                            item.As<BEncodedDictionary>()[lengthKey] is BEncodedNumber)
                        {
                            filePath = Path.Combine(tmpString, Path.Combine(item.As<BEncodedDictionary>()[pathKey].As<BEncodedList>().Select(x => x.As<BEncodedString>().Text).ToArray()));
                            fileLength = item.As<BEncodedDictionary>()[lengthKey].As<BEncodedNumber>().Number;

                            if (item.As<BEncodedDictionary>().ContainsKey(md5sumKey) &&
                                item.As<BEncodedDictionary>()[md5sumKey] is BEncodedString)
                                fileHash = item.As<BEncodedDictionary>()[md5sumKey].As<BEncodedString>().Text;
                            else
                                fileHash = null;

                            files.Add(new TorrentFileInfo(filePath, fileHash, fileLength));
                        }
                        else
                        {
                            return false;
                        }

                    if (files.Count == 0)
                        return false;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            // announce
            if (general.ContainsKey(announceKey) &&
                general[announceKey] is BEncodedString &&
                Uri.TryCreate(general[announceKey].As<BEncodedString>().Text, UriKind.Absolute, out tmpUri))
                announceList.Add(tmpUri);
            else
                return false;

            // announce list
            if (general.ContainsKey(announceListKey) &&
                general[announceListKey] is BEncodedList)
            {
                foreach (var item in general[announceListKey].As<BEncodedList>())
                    if (item is BEncodedList)
                        foreach (var item2 in item.As<BEncodedList>())
                            if (Uri.TryCreate(item2.As<BEncodedString>().Text, UriKind.Absolute, out tmpUri))
                                announceList.Add(tmpUri);

                announceList = announceList.Select(x => x.AbsoluteUri).Distinct().Select(x => new Uri(x)).ToList();
            }

            // creation adte
            if (general.ContainsKey(creationDateKey) &&
                general[creationDateKey] is BEncodedNumber)
                creationDate = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(general[creationDateKey].As<BEncodedNumber>().Number).ToLocalTime();

            // comment
            if (general.ContainsKey(commentKey) &&
                general[commentKey] is BEncodedString)
                comment = general[commentKey].As<BEncodedString>().Text;

            // created by
            if (general.ContainsKey(createdByKey) &&
                general[createdByKey] is BEncodedString)
                createdBy = general[createdByKey].As<BEncodedString>().Text;

            // encoding
            if (general.ContainsKey(encodingKey) &&
                general[encodingKey] is BEncodedString)
                if (general[encodingKey].As<BEncodedString>().Text == "UTF8")
                    encoding = Encoding.UTF8;

            torrentInfo = new TorrentInfo(
                general,
                info.Encode().CalculateSha1Hash().ToHexaDecimalString(),
                pieceLength,
                new ReadOnlyCollection<string>(pieceHashes),
                isPrivate,
                new ReadOnlyCollection<Uri>(announceList),
                creationDate,
                comment,
                createdBy,
                encoding,
                new ReadOnlyCollection<TorrentFileInfo>(files));

            return true;
        }

        return false;
    }

    /// <summary>
    ///     Tries to load the torrent info from the specified file path.
    /// </summary>
    /// <param name="torrentInfoFilePath">The torrent information file path.</param>
    /// <param name="torrentInfo">The torrent information.</param>
    /// <returns>
    ///     True if torrent was loaded successfully, false otherwise.
    /// </returns>
    public static bool TryLoad(string torrentInfoFilePath, out TorrentInfo torrentInfo)
    {
        torrentInfoFilePath.MustBeValidFilePath();
        torrentInfoFilePath.MustFileExist();

        return TryLoad(File.ReadAllBytes(torrentInfoFilePath), out torrentInfo);
    }

    /// <summary>
    ///     Encodes the torrent information to binary data.
    /// </summary>
    /// <returns>The torrent information binary data.</returns>
    public byte[] Encode() =>
        dictionary.Encode();

    /// <summary>
    ///     Gets the block count.
    /// </summary>
    /// <param name="pieceIndex">Index of the piece.</param>
    /// <returns>The block count.</returns>
    public int GetBlockCount(int pieceIndex)
    {
        pieceIndex.MustBeGreaterThanOrEqualTo(0);
        pieceIndex.MustBeLessThan(PiecesCount);

        return (int)Math.Ceiling(GetPieceLength(pieceIndex) / (decimal)BlockLength);
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
        pieceIndex.MustBeLessThan(PiecesCount);
        blockIndex.MustBeGreaterThanOrEqualTo(0);
        blockIndex.MustBeLessThan(GetBlockCount(pieceIndex));

        long pieceLength;
        long blockCount;

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
    ///     Gets the specified file end piece index.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>The file end piece index.</returns>
    public int GetEndPieceIndex(string filePath)
    {
        filePath.CannotBeNullOrEmpty();

        TorrentFileInfo info;
        var pieceStart = 0;
        int pieceEnd;

        for (var i = 0; i < Files.Count(); i++)
        {
            info = Files.ElementAt(i);

            pieceEnd = pieceStart + (int)Math.Ceiling(info.Length / (decimal)PieceLength) - 1;

            if (info.FilePath == filePath)
                return pieceEnd;

            pieceStart = pieceEnd + 1;
        }

        throw new TorrentInfoException("File path is not present.");
    }

    /// <summary>
    ///     Gets the file information for the specified piece.
    /// </summary>
    /// <param name="pieceIndex">Index of the piece.</param>
    /// <returns>The file information.</returns>
    public TorrentFileInfo GetFile(int pieceIndex)
    {
        pieceIndex.MustBeGreaterThanOrEqualTo(0);
        pieceIndex.MustBeLessThanOrEqualTo(PiecesCount);

        TorrentFileInfo info;
        var pieceStart = 0;
        int pieceEnd;

        for (var i = 0; i < Files.Count(); i++)
        {
            info = Files.ElementAt(i);

            pieceEnd = pieceStart + (int)Math.Ceiling(info.Length / (decimal)PieceLength) - 1;

            if (pieceIndex >= pieceStart &&
                pieceIndex <= pieceEnd)
                return info;

            pieceStart = pieceEnd + 1;
        }

        throw new TorrentInfoException($"Piece {pieceIndex} not found.");
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
        pieceIndex.MustBeLessThan(PiecesCount);

        if (pieceIndex == PiecesCount - 1)
            if (Length % PieceLength != 0)
                // last piece can be shorter
                return Length % PieceLength;

        return PieceLength;
    }

    /// <summary>
    ///     Gets the specified file start piece index.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>The file start piece index.</returns>
    public int GetStartPieceIndex(string filePath)
    {
        filePath.CannotBeNullOrEmpty();

        TorrentFileInfo info;
        var pieceStart = 0;
        int pieceEnd;

        for (var i = 0; i < Files.Count(); i++)
        {
            info = Files.ElementAt(i);

            pieceEnd = pieceStart + (int)Math.Ceiling(info.Length / (decimal)PieceLength) - 1;

            if (info.FilePath == filePath)
                return pieceStart;

            pieceStart = pieceEnd + 1;
        }

        throw new TorrentInfoException($"File {filePath} does not exist.");
    }

    /// <summary>
    ///     Saves the torrent information to the specified file path.
    /// </summary>
    /// <param name="torrentInfoFilePath">The torrent information file path.</param>
    public void Save(string torrentInfoFilePath)
    {
        torrentInfoFilePath.MustBeValidFilePath();

        if (File.Exists(torrentInfoFilePath))
            File.Delete(torrentInfoFilePath);

        File.WriteAllBytes(torrentInfoFilePath, dictionary.Encode());
    }
}
