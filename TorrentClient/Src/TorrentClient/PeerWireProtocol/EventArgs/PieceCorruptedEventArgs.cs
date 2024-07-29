﻿using DefensiveProgrammingFramework;

namespace TorrentClient.PeerWireProtocol;

/// <summary>
///     The piece corrupted event arguments.
/// </summary>
public sealed class PieceCorruptedEventArgs : EventArgs
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PieceCorruptedEventArgs" /> class.
    /// </summary>
    /// <param name="pieceIndex">Index of the piece.</param>
    public PieceCorruptedEventArgs(int pieceIndex)
    {
        pieceIndex.MustBeGreaterThanOrEqualTo(0);

        PieceIndex = pieceIndex;
    }

    /// <summary>
    ///     Prevents a default instance of the <see cref="PieceCorruptedEventArgs" /> class from being created.
    /// </summary>
    private PieceCorruptedEventArgs()
    {
    }

    /// <summary>
    ///     Gets the index of the piece.
    /// </summary>
    /// <value>
    ///     The index of the piece.
    /// </value>
    public int PieceIndex { get; private set; }
}
