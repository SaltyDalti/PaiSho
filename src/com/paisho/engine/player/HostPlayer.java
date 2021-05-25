package com.paisho.engine.player;

import com.paisho.engine.Alliance;
import com.paisho.engine.board.Board;
import com.paisho.engine.board.Move;
import com.paisho.engine.pieces.Piece;

import java.util.Collection;

public class HostPlayer extends Player {

    public HostPlayer(final Board board, final Collection<Move> hostStandardLegalMoves, final Collection<Move> opponentStandardLegalMoves) {

        super(board, hostStandardLegalMoves, opponentStandardLegalMoves);
    }

    @Override
    public Collection<Piece> getActivePieces() {
        return this.board.getHostPieces();
    }

    @Override
    public Alliance getAlliance() {
        return Alliance.HOST;
    }

    @Override
    public Player getOpponent() {
        return this.board.opponentPlayer();
    }
}
