package com.paisho.engine.pieces;

import java.util.ArrayList;
import java.util.Collection;
import java.util.List;

import com.paisho.engine.Alliance;
import com.paisho.engine.board.Board;
import com.paisho.engine.board.BoardUtils;
import com.paisho.engine.board.Move;
import com.paisho.engine.board.Point;
import com.paisho.engine.player.Player;

public abstract class Piece {

	protected final PieceType pieceType;
	protected final int piecePosition;
	protected final Alliance pieceAlliance;
	protected final boolean isFirstMove;
	private final int cachedHashCode;
	
	Piece(final PieceType pieceType, final int piecePosition, final Alliance pieceAlliance, final boolean isFirstMove) {
		this.pieceType = pieceType;
		this.pieceAlliance = pieceAlliance;
		this.piecePosition = piecePosition;
		this.cachedHashCode = computeHashCode();
		this.isFirstMove = isFirstMove;
//		this.isInHarmony = isInHarmony();
	}

	private int computeHashCode(){
		int result = pieceType.hashCode();
		result = 31 * result + pieceAlliance.hashCode();
		result = 31 * result + piecePosition;
		result = 31 * result + (isFirstMove ? 1 : 0);
		return result;
	}

	@Override
	public boolean equals(final Object other) {
		if(this == other) {
			return true;
		}
		if(!(other instanceof Piece)) {
			return false;
		}
		final Piece otherPiece = (Piece) other;
		return piecePosition == otherPiece.getPiecePosition() && pieceType == otherPiece.getPieceType() &&
				pieceAlliance == otherPiece.getPieceAlliance() && isFirstMove == otherPiece.isFirstMove();
	}

	@Override
	public int hashCode() {
		return this.cachedHashCode;
	}


	public int getPiecePosition() {
		return this.piecePosition;
	}
	
	public Alliance getPieceAlliance() {
		return this.pieceAlliance;
	}

	public boolean isFirstMove() {
		return this.isFirstMove;
	}

	public PieceType getPieceType() {
		return this.pieceType;
	}

	public abstract Collection<Move> calculateLegalMoves(final Board board);

	public abstract Piece movePiece(Move move);

	public abstract Collection<Piece> getPiecesInHarmony(final Board board);

	public PieceType getPieceType(Piece piece) {
		return piece.getPieceType();
	}

	public int getPieceValue() {
		return this.pieceType.getPieceValue();
	}

	public enum PieceType {
		
		JASMINE("J", 1),
		LILY("L", 2),
		JADE("D", 3),
		ROSE("R", 4),
		CHRYSANTHEMUM("C", 5),
		RHODODENDRON("H", 6),
		ORCHID("O", 7),
		LOTUS("S", 8);

		private String pieceName;
		private int pieceValue;
		
		PieceType(final String pieceName, final int pieceValue){
			this.pieceName = pieceName;
			this.pieceValue = pieceValue;
		}
		
		@Override
		public String toString() {
			return this.pieceName;
		}

		public int getPieceValue() {
			return this.pieceValue;
		}
		
	}
}
