package com.paisho.gui;

import com.google.common.primitives.Ints;
import com.paisho.engine.board.Move;
import com.paisho.engine.pieces.Piece;

import javax.imageio.ImageIO;
import javax.swing.*;
import javax.swing.border.EtchedBorder;
import java.awt.*;
import java.awt.image.BufferedImage;
import java.io.File;
import java.io.IOException;
import java.util.*;
import java.util.List;

public class TakenPiecesPanel extends JPanel {

    private final JPanel northPanel;
    private final JPanel southPanel;

    private static final Color PANEL_COLOR = Color.decode("#444444");
    private static final Dimension TAKEN_PIECES_DIMENSION = new Dimension(150, 80);
    private static final EtchedBorder PANEL_BORDER = new EtchedBorder(EtchedBorder.RAISED);

    public TakenPiecesPanel() {
        super(new BorderLayout());
        this.setBackground(PANEL_COLOR);
        this.setBorder(PANEL_BORDER);
        this.northPanel = new JPanel(new GridLayout(13, 4));
        this.southPanel = new JPanel(new GridLayout(13, 4));
        this.northPanel.setBackground(PANEL_COLOR);
        this.southPanel.setBackground(PANEL_COLOR);
        this.add(this.northPanel, BorderLayout.NORTH);
        this.add(this.southPanel, BorderLayout.SOUTH);
        setPreferredSize(TAKEN_PIECES_DIMENSION);
    }

    public void redo(final Table.MoveLog moveLog) {
        this.southPanel.removeAll();
        this.northPanel.removeAll();

        final List<Piece> hostTakenPieces = new ArrayList<>();
        final List<Piece> opponentTakenPieces = new ArrayList<>();

        for(final Move move : moveLog.getMoves()) {
            if(move.isCapture()) {
                final Piece takenPiece = move.getCapturedPiece();
                if(takenPiece.getPieceAlliance().isHost()) {
                    hostTakenPieces.add(takenPiece);
                }else if(takenPiece.getPieceAlliance().isOpponent()) {
                    opponentTakenPieces.add(takenPiece);
                }else{
                    throw new RuntimeException("should not reach here!");
                }
            }
        }

        Collections.sort(hostTakenPieces, new Comparator<Piece>() {
            @Override
            public int compare(Piece o1, Piece o2) {
                return Ints.compare(o1.getPieceValue(), o2.getPieceValue());
            }
        });

        Collections.sort(opponentTakenPieces, new Comparator<Piece>() {
            @Override
            public int compare(Piece o1, Piece o2) {
                return Ints.compare(o1.getPieceValue(), o2.getPieceValue());
            }
        });

        for(final Piece takenPiece : hostTakenPieces) {
            try {
                final BufferedImage image = ImageIO.read(new File("Artwork/Pieces/" + takenPiece.getPieceAlliance().toString().charAt(0)
                        + takenPiece.toString() + ".png"));
                final ImageIcon icon = new ImageIcon(image.getScaledInstance(30, 30, Image.SCALE_SMOOTH));
                final JLabel imageLabel = new JLabel(icon);
                this.southPanel.add(imageLabel);
            } catch(final IOException e){
                e.printStackTrace();
            }
        }

        for(final Piece takenPiece : opponentTakenPieces) {
            try {
                final BufferedImage image = ImageIO.read(new File("Artwork/Pieces/" + takenPiece.getPieceAlliance().toString().charAt(0)
                        + takenPiece.toString() + ".png"));
                final ImageIcon icon = new ImageIcon(image.getScaledInstance(30, 30, Image.SCALE_SMOOTH));
                final JLabel imageLabel = new JLabel(icon);
                this.southPanel.add(imageLabel);
            } catch(final IOException e){
                e.printStackTrace();
            }

            validate();
        }
    }
}
