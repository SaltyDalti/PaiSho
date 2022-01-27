package com.paisho.engine.board;

import java.util.ArrayList;
import java.util.Collection;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

import com.google.common.collect.ImmutableList;
import com.google.common.collect.ImmutableMap;
import com.google.common.collect.Iterables;
import com.paisho.engine.Alliance;
import com.paisho.engine.pieces.*;
import com.paisho.engine.pieces.Piece;
import com.paisho.engine.player.HostPlayer;
import com.paisho.engine.player.OpponentPlayer;
import com.paisho.engine.player.Player;

public class Board {

	private final List<Point> gameBoard; //list that will hold all of the possible points on the board
	private final List<Point> hostReserve;
	private final List<Point> opponentReserve;
	private final Collection<Piece> hostPieces;
	private final Collection<Piece> opponentPieces;
	private final Collection<Piece> hostReservePieces;
	private final Collection<Piece> opponentReservePieces;

	private final HostPlayer hostPlayer;
	private final OpponentPlayer opponentPlayer;
	private final Player currentPlayer;

	private Board(final Builder builder) {
		//TODO pull tiles from inventory to player hand
		this.gameBoard = createGameBoard(builder); //populates a list of points label 0 - 3##
		this.hostReserve = createPlayerReserve(builder); //populates list of points for hostRserve
		this.opponentReserve = createPlayerReserve(builder); //populates list of points for opponentReserve

		this.hostPieces = calculateActivePieces(this.gameBoard, Alliance.HOST);
		this.opponentPieces = calculateActivePieces(this.gameBoard, Alliance.OPPONENT);
		this.hostReservePieces = calculatePlayerReserve(this.hostReserve);
		this.opponentReservePieces = calculatePlayerReserve(this.opponentReserve);
		//TODO add tiles to player reserve somehow

		final Collection<Move> hostStandardLegalMoves = calculateLegalMoves(this.hostPieces);
		final Collection<Move> opponentStandardLegalMoves = calculateLegalMoves(this.opponentPieces);

		this.hostPlayer = new HostPlayer(this, hostStandardLegalMoves, opponentStandardLegalMoves, hostReservePieces);
		this.opponentPlayer = new OpponentPlayer(this, opponentStandardLegalMoves, hostStandardLegalMoves, opponentReservePieces);
		this.currentPlayer = builder.nextMoveMaker.choosePlayer(this.hostPlayer, this.opponentPlayer);
	}

	private ArrayList<Piece> getHostInventory() {
		final ArrayList<Piece> hostInventory = new ArrayList<>();
		for(Piece piece : hostPieces){
			if(piece.getPiecePosition() > 360) {
				hostInventory.add(piece);
			}
		}
		return hostInventory;
	}

	private ArrayList<Piece> getOpponentInventory() {
		final ArrayList<Piece> hostInventory = new ArrayList<>();
		for(Piece piece : opponentPieces){
			if(piece.getPiecePosition() == 360) {
				hostInventory.add(piece);
			}
		}
		return hostInventory;
	}

	@Override
	public String toString() {
		final StringBuilder builder = new StringBuilder();
		for(int i = 0; i < BoardUtils.NUM_POINTS; i++) {
			String pointText;
			if(!BoardUtils.LEGAL_POINTS.contains(i)){
				pointText = String.format("%3s", " ");
			}else{
				pointText = this.gameBoard.get(i).toString();
			}
			builder.append(String.format("%3s", pointText));

			if((i + 1) % BoardUtils.GRID_SIZE == 0) {
				builder.append("\n");
			}
		}
		return builder.toString();
	}

	public Player hostPlayer() {
		return this.hostPlayer;
	}

	public Player opponentPlayer() {
		return this.opponentPlayer;
	}

	public Player currentPlayer() {
		return this.currentPlayer;
	}

	public Collection<Piece> getHostPieces() {
		return this.hostPieces;
	}

	public Collection<Piece> getOpponentPieces() {
		return this.opponentPieces;
	}

	private Collection<Move> calculateLegalMoves(final Collection<Piece> pieces) {
		final List<Move> legalMoves = new ArrayList<>();
		for(final Piece piece : pieces) {
			legalMoves.addAll(piece.calculateLegalMoves(this));
		}
		return ImmutableList.copyOf(legalMoves);
	}

	private Collection<Piece> calculateActivePieces(final List<Point> gameBoard, final Alliance alliance) { // tracks the total number of pieces for each alliance
		final List<Piece> activePieces = new ArrayList<>();
		
		for(final Point point : gameBoard) {
			if(point.isPointOccupied()) {
				final Piece piece = point.getPiece();
				if(piece.getPieceAlliance() == alliance) {
					activePieces.add(piece);
				}
			}
		}
		return ImmutableList.copyOf(activePieces);
	}

	private Collection<Piece> calculatePlayerReserve(final List<Point> reservePoints) {
		final List<Piece> playerReserve = new ArrayList<>();

		for(final Point point : reservePoints) {
			if(point.isPointOccupied()) {
				final Piece piece = point.getPiece();
				playerReserve.add(piece);
			}
		}
		return ImmutableList.copyOf(playerReserve);
	}

	public Point getPoint(final int pointCoordinate) {
		return gameBoard.get(pointCoordinate);
	}
	
	private static List<Point> createGameBoard(final Builder builder) { // this method returns a list
		BoardUtils.makeBoard();
		final Point[] points = new Point[BoardUtils.NUM_POINTS]; //create new array with total number of points
		
		for(int i = 0; i < BoardUtils.NUM_POINTS; i++) {  // loop through integers up to total number of points
			points[i] = Point.createPoint(i, builder.boardConfig.get(i)); //get the piece on pointID#[i] and assign it to a point with that ID
		}
		return ImmutableList.copyOf(points);
	}

	private static List<Point> createPlayerReserve(final Builder builder) {
		final Point[] points = new Point[BoardUtils.RESERVE_SIZE];

		for(int i = 0; i < BoardUtils.RESERVE_SIZE; i++) {
			points[i] = Point.createPoint(i, builder.boardConfig.get(i));
		}
		return ImmutableList.copyOf(points);
	}
	
	public static Board createStandardBoard() {
		final Builder builder = new Builder();

		builder.setPiece(new Jasmine(215, Alliance.HOST));
		builder.setPiece(new Lily(145, Alliance.HOST));
		builder.setPiece(new Jade(90, Alliance.HOST));
		builder.setPiece(new Rose(120, Alliance.HOST));
		builder.setPiece(new Chrysanthemum(202, Alliance.HOST));
		builder.setPiece(new Rhododendron(42, Alliance.HOST));

		builder.setPiece(new Jasmine(316, Alliance.OPPONENT));
		builder.setPiece(new Lily(144, Alliance.OPPONENT));
		builder.setPiece(new Jade(270, Alliance.OPPONENT));
		builder.setPiece(new Rose(117, Alliance.OPPONENT));
		builder.setPiece(new Chrysanthemum(148, Alliance.OPPONENT));
		builder.setPiece(new Rhododendron(212, Alliance.OPPONENT));


		//host to move
		builder.setMoveMaker(Alliance.HOST);
		
		return builder.build();
	}

    public Iterable<Move> getAllLegalMoves() {
		return Iterables.unmodifiableIterable(Iterables.concat(this.hostPlayer.getLegalMoves(), this.opponentPlayer.getLegalMoves()));
    }

    public static class Builder {
	
		Map<Integer, Piece> boardConfig;
		Alliance nextMoveMaker;
		
		public Builder() {
			this.boardConfig = new HashMap<>();
		}
		
		public Builder setPiece(final Piece piece) { // called when a piece is placed on a point
			this.boardConfig.put(piece.getPiecePosition(), piece); // puts (piece position, piece) in the boardConfig map (e.g. (25, Jasmine))
			return this;
		}

		public Builder setMoveMaker(final Alliance nextMoveMaker) {
			this.nextMoveMaker = nextMoveMaker;
			return this;
		}
		
		
		public Board build() {
			return new Board(this);
		}
	}
}
