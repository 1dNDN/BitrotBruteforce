using System.Runtime.Serialization;

namespace TorrentClient.Exceptions;

[Serializable]
public class TorrentInfoException : Exception
{
    public TorrentInfoException()
    {
    }

    public TorrentInfoException(string message)
        : base(message)
    {
    }

    public TorrentInfoException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    protected TorrentInfoException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
