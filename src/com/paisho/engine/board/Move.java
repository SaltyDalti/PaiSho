package com.paisho.engine.board;

import com.paisho.engine.pieces.Piece;

public abstract class Move {
	
	protected final Board board;
	protected final Piece movedPiece;
	protected final int destinationCoordinate;
	protected final boolean isFirstMove;

	public static final Move NULL_MOVE = new NullMove();

	private Move(final Board board, final Piece movedPiece, final int destinationCoordinate) {
		
		this.board = board;
		this.movedPiece = movedPiece;
		this.destinationCoordinate = destinationCoordinate;
		this.isFirstMove = movedPiece.isFirstMove();
	}

	private Move(final Board board, final int destinationCoordinate) {
		this.board = board;
		this.destinationCoordinate = destinationCoordinate;
		this.movedPiece = null;
		this.isFirstMove = false;
	}

	@Override
	public int hashCode() {
		final int prime = 31;
		int result = 1;

		result = prime * result + this.destinationCoordinate;
		result = prime * result + this.movedPiece.hashCode();
		result = prime * result + this.movedPiece.getPiecePosition();
		return result;
	}

	@Override
	public boolean equals(final Object other) {
		if(this == other) {
			return true;
		}
		if(!(other instanceof Move)) {
			return false;
		}
		final Move otherMove = (Move) other;
		return getCurrentCoordinate() == otherMove.getCurrentCoordinate() &&
				getDestinationCoordinate() == otherMove.getDestinationCoordinate() &&
				getMovedPiece().equals(otherMove.getMovedPiece());
	}

	public int getCurrentCoordinate() {
		return this.getMovedPiece().getPiecePosition();
	}

	public int getDestinationCoordinate() {
		return this.destinationCoordinate;
	}

	public Piece getMovedPiece() {
		return this.movedPiece;
	}

	public boolean isCapture() {
		return false;
	}

	public Piece getCapturedPiece() {
		return null;
	}

	public Board execute() {

		final Board.Builder builder = new Board.Builder();

		for(final Piece piece : this.board.currentPlayer().getActivePieces()) {
			if(!this.movedPiece.equals(piece)) {
				builder.setPiece(piece);
			}
		}
		for(final Piece piece : this.board.currentPlayer().getOpponent().getActivePieces()) {
			builder.setPiece(piece);
		}
		//move the moved piece!
		builder.setPiece(this.movedPiece.movePiece(this));
		builder.setMoveMaker(this.board.currentPlayer().getOpponent().getAlliance());

		return builder.build();
	}

	public static final class NeutralMove extends Move {

		public NeutralMove(final Board board,
						   final Piece movedPiece,
						   final int destinationCoordinate) {
			super(board, movedPiece, destinationCoordinate);
		}

		@Override
		public boolean equals(final Object other) {
			return this == other || other instanceof NeutralMove && super.equals(other);
		}

		@Override
		public String toString() {
			return movedPiece.getPieceType().toString() + BoardUtils.getPositionAtCoordinate(this.destinationCoordinate);
		}

	}
	
	public static class CaptureMove extends Move {

		final Piece capturedPiece;
		
		public CaptureMove(final Board board, 
				    final Piece movedPiece, 
				    final int destinationCoordinate,
				    final Piece capturedPiece) {
			super(board, movedPiece, destinationCoordinate);
			this.capturedPiece = capturedPiece;
		}

		@Override
		public int hashCode() {
			return this.capturedPiece.hashCode() + super.hashCode();
		}

		@Override
		public boolean equals(final Object other) {
			if(this == other) {
				return true;
			}
			if(!(other instanceof CaptureMove)) {
				return false;
			}
			final CaptureMove otherCaptureMove = (CaptureMove) other;
			return super.equals(otherCaptureMove) && getCapturedPiece().equals(otherCaptureMove.getCapturedPiece());
		}

		@Override
		public boolean isCapture() {
			return true;
		}

		@Override
		public Piece getCapturedPiece() {
			return this.capturedPiece;
		}
	}

	public static final class NullMove extends Move {

		public NullMove() {
			super(null, -1);
		}

		@Override
		public Board execute() {
			throw new RuntimeException("cannot execute the null move!");
		}

		@Override
		public int getCurrentCoordinate() {
			return -1;
		}
	}

	public static class MoveFactory {

		private MoveFactory() {
			throw new RuntimeException("Not instantiable!");
		}

		public static Move createMove(final Board board,
									  final int currentCoordinate,
									  final int destinationCoordinate) {

			for (final Move move : board.getAllLegalMoves()) {
				if (move.getCurrentCoordinate() == currentCoordinate &&
						move.getDestinationCoordinate() == destinationCoordinate) {
					return move;
				}
			}
			return NULL_MOVE;
		}
	}
}
