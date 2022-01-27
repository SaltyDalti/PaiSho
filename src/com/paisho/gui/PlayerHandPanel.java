package com.paisho.gui;

import com.paisho.engine.board.BoardUtils;

import javax.swing.*;
import java.awt.*;

public class PlayerHandPanel extends JPanel{

    private static final Color PANEL_COLOR = Color.decode("#444444");
    private static final Dimension HAND_PANEL_DIMENSION = new Dimension(Table.boardSize-300, 80);

    public PlayerHandPanel() {
        final JPanel handPanel = new JPanel(new GridLayout(BoardUtils.GRID_SIZE, 1));
        this.setBackground(PANEL_COLOR);
        this.add(handPanel);
        setPreferredSize(HAND_PANEL_DIMENSION);
    }

}
