using System.Net;
using System.Net.Sockets;

using DefensiveProgrammingFramework;

namespace TorrentClient.PeerWireProtocol;

/// <summary>
///     The asynchronous connect data.
/// </summary>
public sealed class AsyncConnectData
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncConnectData" /> class.
    /// </summary>
    /// <param name="endpoint">The endpoint.</param>
    /// <param name="tcp">The TCP.</param>
    public AsyncConnectData(IPEndPoint endpoint, TcpClient tcp)
    {
        endpoint.CannotBeNull();
        tcp.CannotBeNull();

        Endpoint = endpoint;
        Tcp = tcp;
    }

    /// <summary>
    ///     Prevents a default instance of the <see cref="AsyncConnectData" /> class from being created.
    /// </summary>
    private AsyncConnectData()
    {
    }

    /// <summary>
    ///     Gets the endpoint.
    /// </summary>
    /// <value>
    ///     The endpoint.
    /// </value>
    public IPEndPoint Endpoint { get; private set; }

    /// <summary>
    ///     Gets the TCP client.
    /// </summary>
    /// <value>
    ///     The TCP client.
    /// </value>
    public TcpClient Tcp { get; private set; }
}
