package com.paisho.engine.pieces;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collection;
import java.util.List;

import com.google.common.collect.ImmutableList;
import com.paisho.engine.Alliance;
import com.paisho.engine.board.Board;
import com.paisho.engine.board.BoardUtils;
import com.paisho.engine.board.Move;
import com.paisho.engine.board.Point;

public class Chrysanthemum extends Piece {
	
	private final static int GRID_SIZE = BoardUtils.GRID_SIZE;
	private final static int MAX_MOVES = 4;
	private final static int[] CANDIDATE_VECTOR_COORDINATES_NORTHSOUTH = { -GRID_SIZE, GRID_SIZE }; //fixed array of all possible move offsets
	private final static int[] CANDIDATE_VECTOR_COORDINATES_EASTWEST = { -1, 1 };
	private final List<PieceType> harmonicPieces = Arrays.asList(PieceType.ROSE, PieceType.RHODODENDRON, PieceType.LOTUS);
	private final List<PieceType> disharmonicPieces = Arrays.asList(PieceType.LILY, PieceType.ORCHID);
	public boolean isInHarmony;

	public Chrysanthemum(final int piecePosition, final Alliance pieceAlliance) {
		super(PieceType.CHRYSANTHEMUM, piecePosition, pieceAlliance, true);
	}

	public Chrysanthemum(final Alliance pieceAlliance,
				   final int piecePosition,
				   final boolean isFirstMove) {
		super(PieceType.CHRYSANTHEMUM, piecePosition, pieceAlliance, isFirstMove);
	}

	@Override
	public Collection<Move> calculateLegalMoves(final Board board) {
		
		final List<Move> legalMoves = new ArrayList<>(); // holds all possible moves
		
		for(final int candidateCoordinateOffsetNorthSouth : CANDIDATE_VECTOR_COORDINATES_NORTHSOUTH) { //loops through vector coordinates
			
			int candidateDestinationCoordinate = this.piecePosition; // stores the current position of this piece instance
			int moveCount = 0;

			while(BoardUtils.isValidPointCoordinate(candidateDestinationCoordinate) && moveCount < MAX_MOVES) { // vector runs until it finds an an illegal point or it has run 3 times
			
				if(moveCount < 2) { // checks if the coordinate candidate is valid
					
					candidateDestinationCoordinate += candidateCoordinateOffsetNorthSouth; 
					moveCount ++;
					
					final Point candidateDestinationPoint = board.getPoint(candidateDestinationCoordinate);
				
					if(BoardUtils.isValidPointCoordinate(candidateDestinationCoordinate) && candidateDestinationPoint.isPointOccupied()) { // If the point is open, it's a legal move
						break;
					}
				}
				
				if(BoardUtils.isValidPointCoordinate(candidateDestinationCoordinate) && moveCount >= 2) {
					
					boolean isFirstLoop = true;
					
					for(final int candidateCoordinateOffsetEastWest : CANDIDATE_VECTOR_COORDINATES_EASTWEST) {
						
						if(isFirstLoop) {
							moveCount = 2;
							isFirstLoop = false;
						}else {
							moveCount = 0;
						}
						
						while(BoardUtils.isValidPointCoordinate(candidateDestinationCoordinate) && moveCount < MAX_MOVES) {
						
							candidateDestinationCoordinate += candidateCoordinateOffsetEastWest;
							moveCount ++;
							
							final Point candidateDestinationPoint = board.getPoint(candidateDestinationCoordinate);
							
							if(candidateDestinationPoint.isPointOccupied() && moveCount < MAX_MOVES) {
								break;
							}
							if(!candidateDestinationPoint.isPointOccupied() && moveCount == MAX_MOVES && !isInDisharmony(candidateDestinationCoordinate, board) && !BoardUtils.LIGHT_GARDEN.contains(candidateDestinationCoordinate) && !BoardUtils.GATES.contains(candidateDestinationCoordinate)) {
								if(!legalMoves.contains(new Move.NeutralMove(board, this, candidateDestinationCoordinate))) {
									legalMoves.add(new Move.NeutralMove(board, this, candidateDestinationCoordinate)); // Adds a NeutralMove to legalMoves
								}
							}
							else if (candidateDestinationPoint.isPointOccupied() && moveCount == MAX_MOVES && !isInDisharmony(candidateDestinationCoordinate, board) && !BoardUtils.LIGHT_GARDEN.contains(candidateDestinationCoordinate) && !BoardUtils.GATES.contains(candidateDestinationCoordinate)) {

								final Piece pieceAtDestination = candidateDestinationPoint.getPiece(); // get the piece at candidate coordinate
								final Alliance pieceAlliance = pieceAtDestination.getPieceAlliance(); // get that piece's alliance

								if(this.pieceAlliance != pieceAlliance) { // if alliance is opposite the player's
									if(!legalMoves.contains(new Move.CaptureMove(board, this, candidateDestinationCoordinate, pieceAtDestination))) {
										if (disharmonicPieces.contains(pieceAtDestination.getPieceType())) {
											legalMoves.add(new Move.CaptureMove(board, this, candidateDestinationCoordinate, pieceAtDestination));
										}
									}
								}
							}
						}
					}
				}
			}
		}
		for(final int candidateCoordinateOffsetNorthSouth : CANDIDATE_VECTOR_COORDINATES_EASTWEST) { //loops through vector coordinates
			
			int candidateDestinationCoordinate = this.piecePosition; // stores the current position of this piece instance
			int moveCount = 0;

			while(BoardUtils.isValidPointCoordinate(candidateDestinationCoordinate) && moveCount < MAX_MOVES) { // vector runs until it finds an an illegal point or it has run 3 times
			
				if(moveCount < 2) { // checks if the coordinate candidate is valid
					
					candidateDestinationCoordinate += candidateCoordinateOffsetNorthSouth; 
					moveCount ++;
					
					final Point candidateDestinationPoint = board.getPoint(candidateDestinationCoordinate);
				
					if(BoardUtils.isValidPointCoordinate(candidateDestinationCoordinate) && candidateDestinationPoint.isPointOccupied()) { // If the point is open, it's a legal move
						break;
					}
				}
				
				if(BoardUtils.isValidPointCoordinate(candidateDestinationCoordinate) && moveCount >= 2) {
					
					boolean isFirstLoop = true;
					
					for(final int candidateCoordinateOffsetEastWest : CANDIDATE_VECTOR_COORDINATES_NORTHSOUTH) {
						
						if(isFirstLoop) {
							moveCount = 2;
							isFirstLoop = false;
						}else {
							moveCount = 0;
						}
						
						while(BoardUtils.isValidPointCoordinate(candidateDestinationCoordinate) && moveCount < MAX_MOVES) {
						
							candidateDestinationCoordinate += candidateCoordinateOffsetEastWest;
							moveCount ++;
							
							final Point candidateDestinationPoint = board.getPoint(candidateDestinationCoordinate);
							
							if(candidateDestinationPoint.isPointOccupied() && moveCount < MAX_MOVES) {
								break;
							}

							if(!candidateDestinationPoint.isPointOccupied() && moveCount == MAX_MOVES && !isInDisharmony(candidateDestinationCoordinate, board) && !BoardUtils.LIGHT_GARDEN.contains(candidateDestinationCoordinate) && !BoardUtils.GATES.contains(candidateDestinationCoordinate)) {
								if(!legalMoves.contains(new Move.NeutralMove(board, this, candidateDestinationCoordinate))) {
									legalMoves.add(new Move.NeutralMove(board, this, candidateDestinationCoordinate)); // Adds a NeutralMove to legalMoves
								}
							}
							else if (candidateDestinationPoint.isPointOccupied() && moveCount == MAX_MOVES && !isInDisharmony(candidateDestinationCoordinate, board) && !BoardUtils.LIGHT_GARDEN.contains(candidateDestinationCoordinate) && !BoardUtils.GATES.contains(candidateDestinationCoordinate)) {

								final Piece pieceAtDestination = candidateDestinationPoint.getPiece(); // get the piece at candidate coordinate
								final Alliance pieceAlliance = pieceAtDestination.getPieceAlliance(); // get that piece's alliance

								if(this.pieceAlliance != pieceAlliance) { // if alliance is opposite the player's
									if(!legalMoves.contains(new Move.CaptureMove(board, this, candidateDestinationCoordinate, pieceAtDestination))) {
										if(disharmonicPieces.contains(pieceAtDestination.getPieceType())) {
											legalMoves.add(new Move.CaptureMove(board, this, candidateDestinationCoordinate, pieceAtDestination));
										}
									}
								}
							}
						}
					}
				}
			}
		}
		return ImmutableList.copyOf(legalMoves);
	}

	@Override
	public Chrysanthemum movePiece(final Move move) {
		return new Chrysanthemum(move.getDestinationCoordinate(), move.getMovedPiece().getPieceAlliance());
	}


	private boolean isInDisharmony(int candidateDestinationCoordinate, Board board) {
		boolean isInDisharmony = false;
		for (final int direction : BoardUtils.ALL_DIRECTIONS) {

			int ray = candidateDestinationCoordinate;

			while (BoardUtils.isValidPointCoordinate(ray)) {

				ray += direction;
				final Point rayPoint = board.getPoint(ray);

				if (rayPoint.isPointOccupied()) {
					final Piece pieceToCheck = rayPoint.getPiece();
					final Alliance pieceToCheckAlliance = pieceToCheck.getPieceAlliance();
					final PieceType typeOfPieceToCheck = getPieceType(pieceToCheck);

					if (this.pieceAlliance == pieceToCheckAlliance && disharmonicPieces.contains(typeOfPieceToCheck)) {
						isInDisharmony = true;
					} else{
						break;
					}
				}
			}
		}
		return isInDisharmony;
	}

	@Override
	public Collection<Piece> getPiecesInHarmony(final Board board) {

		final List<Piece> piecesInHarmony = new ArrayList<>();

		for (final int direction : BoardUtils.ALL_DIRECTIONS) {

			int ray = this.piecePosition;

			while (BoardUtils.isValidPointCoordinate(ray)) {

				ray += direction;
				final Point rayPoint = board.getPoint(ray);

				if (rayPoint.isPointOccupied()) {
					final Piece pieceToCheck = rayPoint.getPiece();
					final Alliance pieceToCheckAlliance = pieceToCheck.getPieceAlliance();
					final PieceType typeOfPieceToCheck = getPieceType(pieceToCheck);

					if (this.pieceAlliance == pieceToCheckAlliance && harmonicPieces.contains(typeOfPieceToCheck)) {
						piecesInHarmony.add(pieceToCheck);
					} else{
						break;
					}
				}
			}
		}
		isInHarmony = !piecesInHarmony.isEmpty();
		return ImmutableList.copyOf(piecesInHarmony);
	}

	@Override
	public PieceType getPieceType() {
		return this.pieceType;
	}

	@Override
	public String toString() {
		return PieceType.CHRYSANTHEMUM.toString();
	}
}
