namespace TorrentClient.Extensions;

/// <summary>
///     The exception extensions.
/// </summary>
public static class ExceptionExtensions
{
    /// <summary>
    ///     Formats the specified exception.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>Fhe formatted exception.</returns>
    public static string Format(this Exception exception)
    {
        var message = string.Empty;

        while (exception != null)
        {
            message += exception.GetType() + ": " + exception.Message.Trim();
            message += Environment.NewLine;
            message += exception.StackTrace;
            message += Environment.NewLine;

            exception = exception.InnerException;
        }

        return message;
    }
}
