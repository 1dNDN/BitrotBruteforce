using System.Collections.ObjectModel;
using System.Text;

using Bruteforce.TorrentWrapper.BEncoding;
using Bruteforce.TorrentWrapper.Extensions;

using TorrentClient.BEncoding;

namespace Bruteforce.TorrentWrapper;

/// <summary>
///     The torrent file.
/// </summary>
public sealed class TorrentInfo
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TorrentInfo" /> class.
    /// </summary>
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
    private TorrentInfo(string infoHash,
        long pieceLength,
        string[] piecesHashValues,
        bool isPrivate,
        IEnumerable<Uri> announceList,
        DateTime? creationDate,
        string comment,
        string createdBy,
        Encoding encoding,
        TorrentFileInfo[] files)
    {
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
    }

    /// <summary>
    ///     Gets the announce URI list.
    /// </summary>
    /// <value>
    ///     The announce URI list.
    /// </value>
    public IEnumerable<Uri> AnnounceList { get; private set; }

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
    public TorrentFileInfo[] Files { get; }

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
    public string[] PieceHashes { get; private set; }

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
        var md5SumKey = new BEncodedString("md5sum");
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
        catch (ArgumentException)
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

                        System.Array.Copy(info[piecesKey].As<BEncodedString>().TextBytes, i, tmpBytes, 0, tmpBytes.Length);

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

                    if (info.ContainsKey(md5SumKey) &&
                        info[md5SumKey] is BEncodedString)
                        fileHash = info[md5SumKey].As<BEncodedString>().Text;
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

                            if (item.As<BEncodedDictionary>().ContainsKey(md5SumKey) &&
                                item.As<BEncodedDictionary>()[md5SumKey] is BEncodedString)
                                fileHash = item.As<BEncodedDictionary>()[md5SumKey].As<BEncodedString>().Text;
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
                info.Encode().CalculateSha1Hash().ToHexaDecimalString(),
                pieceLength,
                pieceHashes.ToArray(),
                isPrivate,
                new ReadOnlyCollection<Uri>(announceList),
                creationDate,
                comment,
                createdBy,
                encoding,
                files.ToArray());

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
    public static bool TryLoad(string torrentInfoFilePath, out TorrentInfo torrentInfo) =>
        TryLoad(File.ReadAllBytes(torrentInfoFilePath), out torrentInfo);
}
