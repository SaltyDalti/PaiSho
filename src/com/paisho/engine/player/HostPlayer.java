package com.paisho.engine.player;

import com.paisho.engine.Alliance;
import com.paisho.engine.board.Board;
import com.paisho.engine.board.Move;
import com.paisho.engine.pieces.Piece;

import java.util.ArrayList;
import java.util.Collection;

public class HostPlayer extends Player {


    public HostPlayer(Board board, Collection<Move> legalMoves, Collection<Move> opponentMoves, Collection<Piece> INVENTORY) {
        super(board, legalMoves, opponentMoves, INVENTORY);
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
