package com.paisho.engine.player;

import com.google.common.collect.ImmutableList;
import com.paisho.engine.Alliance;
import com.paisho.engine.board.Board;
import com.paisho.engine.board.Move;
import com.paisho.engine.pieces.Piece;

import java.util.Collection;

public abstract class Player {

    protected final Board board;
    protected final Collection<Move> legalMoves;
    protected final Collection<Piece> INVENTORY;
    public final boolean hasHarmoniousRing = false;
    public static final int NUM_FLOWERS = 36;

    Player(final Board board, final Collection<Move> legalMoves, final Collection<Move> opponentMoves, final Collection<Piece> INVENTORY) {

        this.board = board;
        this.legalMoves = ImmutableList.copyOf(legalMoves);
        this.INVENTORY = ImmutableList.copyOf(INVENTORY);
    }

    public Collection<Move> getLegalMoves() {
        return this.legalMoves;
    }

    public boolean isMoveLegal(final Move move) {
       return this.legalMoves.contains(move);
    }

    public boolean hasHarmoniousRing(){
        return this.hasHarmoniousRing;
    }

    public boolean isDraw() {
        return false; //TODO
    }

    public MoveTransition makeMove(final Move move ) {

        if(!isMoveLegal(move)) {
            return new MoveTransition(this.board, move, MoveStatus.ILLEGAL_MOVE);
        }
        final Board transitionBoard = move.execute();

        return new MoveTransition(transitionBoard, move, MoveStatus.DONE);
    }

    public abstract Collection<Piece> getActivePieces();
    public abstract Alliance getAlliance();
    public abstract Player getOpponent();

}
