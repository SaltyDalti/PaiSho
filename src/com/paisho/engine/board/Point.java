package com.paisho.engine.board;

import java.util.HashMap;
import java.util.Map;

import com.google.common.collect.ImmutableMap;
import com.paisho.engine.pieces.Piece;

public abstract class Point {
	
	protected final int pointCoordinate; // protected so only accessible by subclasses
	
	private static final Map<Integer, EmptyPoint> EMPTY_POINTS_CACHE = createAllPossibleEmptyPoints(); // creates a map of empty points using the method createAllPossibleEmptyPoints();
	
	private static Map<Integer, EmptyPoint> createAllPossibleEmptyPoints() {
		
		final Map<Integer, EmptyPoint> emptyPointMap = new HashMap<>();
		
		for(int i = 0; i < BoardUtils.NUM_POINTS; i++) { // loops through total number of points on the board
			emptyPointMap.put(i,  new EmptyPoint(i));
		}
		return ImmutableMap.copyOf(emptyPointMap);
	}

	public static Point createPoint(final int pointCoordinate, final Piece piece) {
		return piece != null ? new OccupiedPoint(pointCoordinate, piece) : EMPTY_POINTS_CACHE.get(pointCoordinate);
	}

	private Point(final int pointCoordinate) {
		this.pointCoordinate = pointCoordinate;
	}

	public abstract boolean isPointOccupied();
	
	public abstract Piece getPiece();

	public int getPointCoordinate() {
		return this.pointCoordinate;
	}

	public static final class EmptyPoint extends Point {  // subclass for open points
		
		private EmptyPoint(final int coordinate) {
			super(coordinate);
		}
		
		@Override
		public String toString() {
			return "·";
		}
		
		@Override
		public boolean isPointOccupied() {
			return false;
		}
		
		@Override
		public Piece getPiece() {
			return null;
		}
	}
	
	public static final class OccupiedPoint extends Point {  // subclass for occupied points
		
		private final Piece pieceOnPoint;
		
		private OccupiedPoint(int pointCoordinate, final Piece pieceOnPoint) {
			super(pointCoordinate);
			this.pieceOnPoint = pieceOnPoint;
		}
		
		@Override
		public String toString() {
			return getPiece().getPieceAlliance().isHost() ? getPiece().toString().toLowerCase() : getPiece().toString().toUpperCase();
		}
		
		@Override
		public boolean isPointOccupied() {
			return true;
		}
		
		@Override
		public Piece getPiece() {
			return this.pieceOnPoint;
		}
	}
}
