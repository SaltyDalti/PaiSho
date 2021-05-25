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
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;
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

    private final JFrame gameFrame;
    private final GameHistoryPanel gameHistoryPanel;
    private final TakenPiecesPanel takenPiecesPanel;
    private final BoardPanel boardPanel;
    private Board paishoBoard;
    private final MoveLog moveLog;

    private Point sourcePoint;
    private Point destinationPoint;
    private Piece humanMovedPiece;

    private boolean highlightLegalMoves;

    private final static int boardSize = 600;

    private final static Dimension OUTER_FRAME_DIMENSION = new Dimension(boardSize+300,boardSize+60);
    private final static Dimension BOARD_POINT_DIMENSION = new Dimension(boardSize, boardSize);
    private final static Dimension POINT_PANEL_DIMENSION = new Dimension(10, 10);

    private final static String defaultPieceImagesPath = "Artwork/Pieces/";
    private final static String defaultPanelImagesPath = "Artwork/BoardPanels/panel_";

    public Table() {
        this.gameFrame = new JFrame("PaiSho");
        this.gameFrame.setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);
        this.gameFrame.setLayout(new BorderLayout());
        this.gameFrame.setResizable(false);
        final JMenuBar tableMenuBar = createTableMenuBar();
        this.gameFrame.setJMenuBar(tableMenuBar);
        this.gameFrame.setSize(OUTER_FRAME_DIMENSION);
        this.paishoBoard = Board.createStandardBoard();
        this.gameHistoryPanel = new GameHistoryPanel();
        this.takenPiecesPanel = new TakenPiecesPanel();
        this.boardPanel = new BoardPanel();
        this.moveLog = new MoveLog();
        this.highlightLegalMoves = false;
        this.gameFrame.add(this.takenPiecesPanel, BorderLayout.WEST);
        this.gameFrame.add(this.boardPanel, BorderLayout.CENTER);
        this.gameFrame.add(this.gameHistoryPanel, BorderLayout.EAST);
        this.gameFrame.setVisible(true);
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
        openPGN.addActionListener(new ActionListener() {
            @Override
            public void actionPerformed(ActionEvent e) {
                System.out.println("open that pgn!");
            }
        });
        fileMenu.add(openPGN);

        final JMenuItem exitMenuItem = new JMenuItem("Exit");
        exitMenuItem.addActionListener(new ActionListener() {
            @Override
            public void actionPerformed(ActionEvent e) {
                System.exit(0);
            }
        });
        fileMenu.add(exitMenuItem);

        return fileMenu;
    }

    private JMenu createPreferencesMenu() {

        final JMenu preferencesMenu = new JMenu("Preferences");
        final JCheckBoxMenuItem legalMoveHighlighterCheckbox = new JCheckBoxMenuItem("Highlight Legal Moves", false);

        legalMoveHighlighterCheckbox.addActionListener(new ActionListener() {
            @Override
            public void actionPerformed(ActionEvent e) {
                highlightLegalMoves = legalMoveHighlighterCheckbox.isSelected();
            }
        });
        preferencesMenu.add(legalMoveHighlighterCheckbox);

        return preferencesMenu;
    }

    private class BoardPanel extends JPanel {

        final List<PointPanel> boardPoints;

        BoardPanel() {
            super(new GridLayout(BoardUtils.GRID_SIZE, BoardUtils.GRID_SIZE));
            this.boardPoints = new ArrayList<>();
            for(int i = 0; i < BoardUtils.NUM_POINTS; i++) {
                final PointPanel pointPanel = new PointPanel(this, i);
                this.boardPoints.add(pointPanel);
                add(pointPanel);
            }
            setPreferredSize(BOARD_POINT_DIMENSION);
            validate();
        }

        public void drawBoard(final Board board) {
            removeAll();
            for(final PointPanel pointPanel : boardPoints) {
                pointPanel.drawPoint(board);
                add(pointPanel);
            }
            validate();
            repaint();
        }
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

    private class PointPanel extends JPanel {

        private BufferedImage image;
        private final int pointId;

        PointPanel(final BoardPanel boardPoint, final int pointId) {

            super(new GridBagLayout());
            this.pointId = pointId;

            try {
                image = ImageIO.read(new File(defaultPanelImagesPath + (pointId+1) + ".jpg"));
            } catch (IOException e) {
                e.printStackTrace();
            }

            setPreferredSize(POINT_PANEL_DIMENSION);
            assignPointPieceIcon(paishoBoard);

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
                        SwingUtilities.invokeLater(new Runnable() {
                            @Override
                            public void run() {
                                gameHistoryPanel.redo(paishoBoard, moveLog);
                                takenPiecesPanel.redo(moveLog);
                                boardPoint.drawBoard(paishoBoard);
                            }
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

        public void drawPoint(final Board board){
            assignPointPieceIcon(board);
            highlightLegalMoves(board);
            validate();
            repaint();
        }

        public void paintComponent(Graphics g) {
            super.paintComponent(g);
            g.drawImage(image,0,0, getWidth(), getHeight(),this);
        }

        private void assignPointPieceIcon(final Board board) {
            removeAll();
            if (board.getPoint(this.pointId).isPointOccupied()) {
                try {
                    final BufferedImage image =
                            ImageIO.read(new File(defaultPieceImagesPath + board.getPoint(this.pointId).getPiece().getPieceAlliance().toString().charAt(0) +
                                    board.getPoint(this.pointId).getPiece().toString() + ".png"));
                            add(new JLabel(new ImageIcon(image.getScaledInstance(34, 34, Image.SCALE_SMOOTH))));
                } catch (IOException e) {
                    e.printStackTrace();
                }
            }
        }

        private void highlightLegalMoves(final Board board) {
            if(highlightLegalMoves) {
                for(final Move move : pieceLegalMoves(board)) {
                    if(move.getDestinationCoordinate() == this.pointId) {
                        try {
                            final BufferedImage image = ImageIO.read(new File("Artwork/Move/BlueDot.png"));
                            add(new JLabel(new ImageIcon(image.getScaledInstance(5, 5, Image.SCALE_SMOOTH))));
                        } catch(Exception e) {
                            e.printStackTrace();
                        }
                    }
                }
            }
        }

        private Collection<Move> pieceLegalMoves(final Board board) {
            if(humanMovedPiece != null && humanMovedPiece.getPieceAlliance() == board.currentPlayer().getAlliance()) {
                return humanMovedPiece.calculateLegalMoves(board);
            }
            return Collections.emptyList();
        }
    }
}
