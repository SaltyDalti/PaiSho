package com.paisho.engine.board;

import java.util.*;

public class BoardUtils {
	
	public final static int GRID_SIZE = 19;
	public final static int NUM_POINTS = GRID_SIZE * GRID_SIZE;
	public final static int RESERVE_SIZE = 54;

	public final static ArrayList<Integer> LEGAL_POINTS = new ArrayList<>();
	public final static int[] ALL_DIRECTIONS = { -GRID_SIZE, GRID_SIZE, -1, 1 };
	public static final List<String> ALGEBRAIC_NOTATION = initializeAlgebraicNotation();
	public final Map<String, Integer> POSITION_TO_COORDINATE = initializePositionToCoordinateMap();
	public static final int START_TILE_INDEX = 0;

	public final static ArrayList<Integer> LIGHT_GARDEN = new ArrayList<>(Arrays.asList(84,102,103,120,121,122,138,139,140,141,156,157,158,159,160,
																					   200,201,202,203,204,219,220,221,222,238,239,240,257,258,276));
	public final static ArrayList<Integer> DARK_GARDEN = new ArrayList<>(Arrays.asList(86,105,106,124,125,126,143,144,145,146,162,163,164,165,166,
																					   194,195,196,197,198,214,215,216,217,234,235,236,254,255,274));
	public final static ArrayList<Integer> GATES = new ArrayList<>(Arrays.asList(28,172,180,188,332));
	public final static int NORTH_GATE = 332;
	public final static int EAST_GATE = 172;
	public final static int SOUTH_GATE = 28;
	public final static int WEST_GATE = 188;
	public final static int MIDDLE_GATE = 180;
	
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
