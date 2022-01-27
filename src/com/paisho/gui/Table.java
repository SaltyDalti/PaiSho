package com.paisho.gui;

import com.paisho.engine.board.Board;
import com.paisho.engine.board.BoardUtils;
import com.paisho.engine.board.Move;
import com.paisho.engine.board.Point;
import com.paisho.engine.pieces.Piece;
import com.paisho.engine.player.MoveTransition;

import javax.imageio.ImageIO;
import javax.swing.*;
import java.awt.*;
import java.awt.event.MouseEvent;
import java.awt.event.MouseListener;
import java.awt.image.BufferedImage;
import java.io.File;
import java.io.IOException;
import java.util.ArrayList;
import java.util.Collection;
import java.util.Collections;
import java.util.List;

import static javax.swing.SwingUtilities.isLeftMouseButton;
import static javax.swing.SwingUtilities.isRightMouseButton;

public class Table {

    private final GameHistoryPanel gameHistoryPanel;
    private final TakenPiecesPanel takenPiecesPanel;
    private final PlayerHandPanel playerHandPanel;
    private Board paishoBoard;
    private final MoveLog moveLog;

    private Point sourcePoint;
    private Point destinationPoint;
    private Piece humanMovedPiece;

    private boolean highlightLegalMoves;

    public final static int boardSize = 1000;
    public final static int tileSize = 60   ;

    private final static Dimension OUTER_FRAME_DIMENSION = new Dimension(boardSize+260,boardSize+60);
    private final static Dimension BOARD_POINT_DIMENSION = new Dimension(boardSize, boardSize);
    private final static Dimension POINT_PANEL_DIMENSION = new Dimension(tileSize, tileSize);

    private final static String defaultPieceImagesPath = "Artwork/Pieces/";

    public Table() {
        JFrame gameFrame = new JFrame("PaiSho");
        gameFrame.setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);
        gameFrame.setLayout(new BorderLayout());
        gameFrame.setResizable(true);
        final JMenuBar tableMenuBar = createTableMenuBar();
        gameFrame.setJMenuBar(tableMenuBar);
        gameFrame.setSize(OUTER_FRAME_DIMENSION);
        this.paishoBoard = Board.createStandardBoard();
        this.gameHistoryPanel = new GameHistoryPanel();
        this.takenPiecesPanel = new TakenPiecesPanel();
        this.playerHandPanel = new PlayerHandPanel();
        BoardPanel boardPanel = new BoardPanel();
        this.moveLog = new MoveLog();
        this.highlightLegalMoves = true; //TODO change this to false eventually
        gameFrame.add(this.takenPiecesPanel, BorderLayout.WEST);
        gameFrame.add(boardPanel, BorderLayout.CENTER);
        gameFrame.add(this.gameHistoryPanel, BorderLayout.EAST);
        gameFrame.add(this.playerHandPanel, BorderLayout.SOUTH);
        gameFrame.setVisible(true);
        boardPanel.drawBoard();
    }

    private JMenuBar createTableMenuBar() {
        final JMenuBar tableMenuBar = new JMenuBar();
        tableMenuBar.add(createFileMenu());
        tableMenuBar.add(createPreferencesMenu());
        return tableMenuBar;
    }

    private JMenu createFileMenu() {
        final JMenu fileMenu = new JMenu("File");

        final JMenuItem openPGN = new JMenuItem("Load PGN File");
        openPGN.addActionListener(e -> System.out.println("open that pgn!"));
        fileMenu.add(openPGN);

        final JMenuItem exitMenuItem = new JMenuItem("Exit");
        exitMenuItem.addActionListener(e -> System.exit(0));
        fileMenu.add(exitMenuItem);

        return fileMenu;
    }

    private JMenu createPreferencesMenu() {

        final JMenu preferencesMenu = new JMenu("Preferences");
        final JCheckBoxMenuItem legalMoveHighlighterCheckbox = new JCheckBoxMenuItem("Highlight Legal Moves", false);

        legalMoveHighlighterCheckbox.addActionListener(e -> highlightLegalMoves = legalMoveHighlighterCheckbox.isSelected());
        preferencesMenu.add(legalMoveHighlighterCheckbox);

        return preferencesMenu;
    }

    private class BoardPanel extends JPanel {

        final List<PointPanel> boardPoints;
        private BufferedImage image;

        BoardPanel() {  //main big window to hold grid (layer 1)
            super(new GridLayout(BoardUtils.GRID_SIZE, BoardUtils.GRID_SIZE));
            this.boardPoints = new ArrayList<>();

            try {
                image = ImageIO.read(new File("Artwork/BoardFullCropped.png"));
            } catch (IOException e) {
                e.printStackTrace();
            }

            for(int i = 0; i < BoardUtils.NUM_POINTS; i++) {
                final PointPanel pointPanel = new PointPanel(this, i);
                this.boardPoints.add(pointPanel);
                add(pointPanel);
            }

            setPreferredSize(BOARD_POINT_DIMENSION);
            validate();
        }

        public void drawBoard() {
            removeAll();
            for (final PointPanel pointPanel : boardPoints) { //iterates through each point panel to draw the board
                add(pointPanel);
                pointPanel.drawPoint(pointPanel);
            }
            validate();
            repaint();
        }

        public void paintComponent(Graphics g) {
            super.paintComponent(g);
            g.drawImage(image,0,0, getWidth(), getHeight(),this);
        }
    }

    private class PointPanel extends JPanel {

        private final int pointId;


        PointPanel(final BoardPanel boardPoint, final int pointId) {

            super(new GridLayout(1,1));
            this.pointId = pointId;
            this.setOpaque(false);

            setPreferredSize(POINT_PANEL_DIMENSION);

            addMouseListener(new MouseListener() {
                @Override
                public void mouseClicked(final MouseEvent e) {
                    if (isRightMouseButton(e)) {
                        sourcePoint = null;
                        destinationPoint = null;
                        humanMovedPiece = null;
                    } else if (isLeftMouseButton(e)) {
                        if (sourcePoint == null) {
                            //first click
                            sourcePoint = paishoBoard.getPoint(pointId);
                            humanMovedPiece = sourcePoint.getPiece();
                            if (humanMovedPiece == null) {
                                sourcePoint = null;
                            }
                        } else {
                            //second click
                            destinationPoint = paishoBoard.getPoint(pointId);
                            final Move move = Move.MoveFactory.createMove(paishoBoard, sourcePoint.getPointCoordinate(), destinationPoint.getPointCoordinate());
                            final MoveTransition transition = paishoBoard.currentPlayer().makeMove(move);
                            if (transition.getMoveStatus().isDone()) {
                                paishoBoard = transition.getTransitionBoard();
                                moveLog.addMove(move);
                            }
                            sourcePoint = null;
                            destinationPoint = null;
                            humanMovedPiece = null;
                        }
                        SwingUtilities.invokeLater(() -> {
                            gameHistoryPanel.redo(paishoBoard, moveLog);
                            takenPiecesPanel.redo(moveLog);
                            boardPoint.drawBoard();
                        });
                    }
                }

                @Override
                public void mousePressed(final MouseEvent e) {

                }

                @Override
                public void mouseReleased(final MouseEvent e) {

                }

                @Override
                public void mouseEntered(final MouseEvent e) {

                }

                @Override
                public void mouseExited(final MouseEvent e) {

                }
            });

            validate();

        }

        public void drawPoint(final PointPanel pointPanel){
            removeAll();

            if (paishoBoard.getPoint(pointId).isPointOccupied()) {
                final PiecePanel piecePanel = new PiecePanel(pointId);
                add(piecePanel);
            }

            if (highlightLegalMoves) {
                for(final Move move : pieceLegalMoves(paishoBoard)) {
                    if(move.getDestinationCoordinate() == pointId) {
                        if(move.isCapture()) {
                            final RedPiecePanel redPiecePanel = new RedPiecePanel(pointId);
                            pointPanel.removeAll();
                            pointPanel.add(redPiecePanel);
                        }else {
                            final HighlightLegalMoves highlightLegalMoves = new HighlightLegalMoves();
                            pointPanel.add(highlightLegalMoves);
                        }
                    }
                }
            }

            //highlightHarmonies
            validate();
            repaint();
        }
    }

    private class PiecePanel extends JPanel {

        private BufferedImage image;

        PiecePanel(final int pointId) {

            super(new GridBagLayout());
            super.setOpaque(false);

            if (paishoBoard.getPoint(pointId).isPointOccupied()) {
                try {
                    image = ImageIO.read(new File(defaultPieceImagesPath + paishoBoard.getPoint(pointId).getPiece().getPieceAlliance().toString().charAt(0) +
                            paishoBoard.getPoint(pointId).getPiece().toString() + ".png"));
                } catch (IOException e) {
                    e.printStackTrace();
            }
                setPreferredSize(POINT_PANEL_DIMENSION);
                validate();
            }
        }

        public void paintComponent(Graphics g) {
            super.paintComponent(g);
            g.drawImage(image,0,0, getWidth(), getHeight(),this);
        }
    }

    private class RedPiecePanel extends JPanel {

        private BufferedImage image;
        public boolean isUnderAttack;

        RedPiecePanel(final int pointId) {

            super(new GridBagLayout());
            super.setOpaque(false);
            this.isUnderAttack = false;

            if (paishoBoard.getPoint(pointId).isPointOccupied()) {
                try {
                    image = ImageIO.read(new File("Artwork/PiecesUnderAttack/" + paishoBoard.getPoint(pointId).getPiece().getPieceAlliance().toString().charAt(0) +
                            paishoBoard.getPoint(pointId).getPiece().toString() + ".png"));
                } catch (IOException e) {
                    e.printStackTrace();
                }

                setPreferredSize(POINT_PANEL_DIMENSION);

                validate();
            }
        }

        public void paintComponent(Graphics g) {
            super.paintComponent(g);
            g.drawImage(image,0,0, getWidth(), getHeight(),this);
        }
    }

    private static class HighlightLegalMoves extends JPanel {

        private BufferedImage image;

        HighlightLegalMoves() {
            super(new GridBagLayout());
            super.setOpaque(false);

            try {
                image = ImageIO.read(new File("Artwork/Move/BlueDot.png"));
            } catch (IOException e) {
                e.printStackTrace();
            }

            setPreferredSize(POINT_PANEL_DIMENSION);
            validate();
            }

        public void paintComponent(Graphics g) {
            super.paintComponent(g);
            g.drawImage(image,0,0, getWidth(), getHeight(),this);
        }
    }

    private Collection<Move> pieceLegalMoves(final Board board) {
        if(humanMovedPiece != null && humanMovedPiece.getPieceAlliance() == board.currentPlayer().getAlliance()) {
            return humanMovedPiece.calculateLegalMoves(board);
        }
        return Collections.emptyList();
    }

    public static class MoveLog {
        private final List<Move> moves;

        MoveLog() {
            this.moves = new ArrayList<>();
        }

        public List<Move> getMoves() {
            return this.moves;
        }

        public void addMove(final Move move) {
            this.moves.add(move);
        }

        public int size() {
            return this.moves.size();
        }

        public void clear() {
            this.moves.clear();
        }

        public Move removeMove(int index) {
            return this.moves.remove(index);
        }

        public boolean removeMove(final Move move) {
            return this.moves.remove(move);
        }
    }
}
