package com.tests.paisho.engine.board;

import com.paisho.engine.board.Board;
import org.junit.Test;

import static org.junit.Assert.*;


public class BoardTest {

    @Test
    public void initialBoard() {
        final Board board = Board.createStandardBoard();
        assertEquals(board.currentPlayer().getLegalMoves().size(), 11);
        assertEquals(board.currentPlayer().getOpponent().getLegalMoves().size(), 11);
        assertEquals(board.currentPlayer(), board.hostPlayer());
        assertEquals(board.currentPlayer().getOpponent(), board.opponentPlayer());
    }

}