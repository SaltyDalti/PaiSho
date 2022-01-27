package com.paisho.engine.player;

import com.paisho.engine.Alliance;
import com.paisho.engine.board.Board;
import com.paisho.engine.board.Move;
import com.paisho.engine.pieces.Piece;

import java.util.Collection;

public class OpponentPlayer extends Player{


    public OpponentPlayer(Board board, Collection<Move> legalMoves, Collection<Move> opponentMoves, Collection<Piece> INVENTORY) {
        super(board, legalMoves, opponentMoves, INVENTORY);
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
