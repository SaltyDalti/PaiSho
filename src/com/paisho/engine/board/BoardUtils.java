package com.paisho.engine.board;

import java.util.*;

public class BoardUtils {
	
	public final static int GRID_SIZE = 19;
	public final static int NUM_POINTS = GRID_SIZE * GRID_SIZE;

	public final static ArrayList<Integer> LEGAL_POINTS = new ArrayList<>();
	public final static int[] ALL_DIRECTIONS = { -GRID_SIZE, GRID_SIZE, -1, 1 };
	public static final List<String> ALGEBRAIC_NOTATION = initializeAlgebraicNotation();
	public final Map<String, Integer> POSITION_TO_COORDINATE = initializePositionToCoordinateMap();
	public static final int START_TILE_INDEX = 0;
	
	private BoardUtils() {
		throw new RuntimeException("You cannot instantiate me!");
	}
	
 	public static void makeBoard() {
		for(int i = 0; i < NUM_POINTS; i++) {
			if((24 < i && i < 32) || 
			   (41 < i && i < 53) ||
			   (59 < i && i < 73) ||
			   (77 < i && i < 93) ||
			   (96 < i && i <112) ||
			   (114< i && i <132) ||
			   (133< i && i <151) ||
			   (152< i && i <170) ||
			   (171< i && i <189) ||
			   (190< i && i <208) ||
			   (209< i && i <227) ||
			   (228< i && i <246) ||
			   (248< i && i <264) ||
			   (267< i && i <283) ||
			   (287< i && i <301) ||
			   (307< i && i <319) ||
			   (328< i && i <336) ){
				LEGAL_POINTS.add(i);
			}
		}
	}
	
	public static boolean isValidPointCoordinate(int pieceCoordinate) {
		
		boolean coordinateChecker = true;
				
		for (int coordinate : LEGAL_POINTS) { // iterates through all legal coordinates
			if (coordinate == pieceCoordinate) { // breaks and returns true if point it legal
				coordinateChecker = true;
				break;
			} else { // if nothing is found returns false
				coordinateChecker = false;
			}
		}
		return coordinateChecker;
	}

	private Map<String, Integer> initializePositionToCoordinateMap() {
		final Map<String, Integer> positionToCoordinate = new HashMap<>();
		for (int i = START_TILE_INDEX; i < BoardUtils.NUM_POINTS; i++) {
			positionToCoordinate.put(ALGEBRAIC_NOTATION.get(i), i);
		}
		return Collections.unmodifiableMap(positionToCoordinate);
	}

	private static List<String> initializeAlgebraicNotation() { //fixme
		List<String> coordinateList = new ArrayList<>();
		//TODO come up with letter convention
		for(int point = 0; point < NUM_POINTS; point++) {
			coordinateList.add("point");
		}

		return Collections.unmodifiableList(coordinateList);
	}

	public int getCoordinateAtPosition(final String position) {
		return POSITION_TO_COORDINATE.get(position);
	}

	public static String getPositionAtCoordinate(final int coordinate) {
		return ALGEBRAIC_NOTATION.get(coordinate);
	}
}
