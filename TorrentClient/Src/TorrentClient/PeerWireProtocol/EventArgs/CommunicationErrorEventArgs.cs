using DefensiveProgrammingFramework;

namespace TorrentClient.PeerWireProtocol;

/// <summary>
///     The communication error event arguments.
/// </summary>
public sealed class CommunicationErrorEventArgs : EventArgs
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CommunicationErrorEventArgs" /> class.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    public CommunicationErrorEventArgs(string errorMessage)
    {
        errorMessage.CannotBeNullOrEmpty();

        ErrorMessage = errorMessage;
    }

    /// <summary>
    ///     Prevents a default instance of the <see cref="CommunicationErrorEventArgs" /> class from being created.
    /// </summary>
    private CommunicationErrorEventArgs()
    {
    }

    /// <summary>
    ///     Gets the error message.
    /// </summary>
    /// <value>
    ///     The error message.
    /// </value>
    public string ErrorMessage { get; private set; }
}
