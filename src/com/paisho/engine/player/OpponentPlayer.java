package com.paisho.engine.player;

import com.paisho.engine.Alliance;
import com.paisho.engine.board.Board;
import com.paisho.engine.board.Move;
import com.paisho.engine.pieces.Piece;

import java.util.Collection;

public class OpponentPlayer extends Player{

    public OpponentPlayer(final Board board, final Collection<Move> opponentStandardLegalMoves, final Collection<Move> hostStandardLegalMoves) {

        super(board, opponentStandardLegalMoves, hostStandardLegalMoves);
    }

    @Override
    public Collection<Piece> getActivePieces() {
        return this.board.getOpponentPieces();
    }

    @Override
    public Alliance getAlliance() {
        return Alliance.OPPONENT;
    }

    @Override
    public Player getOpponent() {
        return this.board.hostPlayer();
    }

}
