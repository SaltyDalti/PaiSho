package com.paisho.engine;

import com.paisho.engine.player.HostPlayer;
import com.paisho.engine.player.OpponentPlayer;
import com.paisho.engine.player.Player;

public enum Alliance {
	HOST {
		@Override
		public Player choosePlayer(final HostPlayer hostPlayer, final OpponentPlayer opponentPlayer) {
			return hostPlayer;
		}

		@Override
		public boolean isHost() {
			return true;
		}

		@Override
		public boolean isOpponent() {
			return false;
		}
		},
	OPPONENT {
		@Override
		public Player choosePlayer(final HostPlayer hostPlayer, final OpponentPlayer opponentPlayer) {
			return opponentPlayer;
		}

		@Override
		public boolean isHost() {
			return false;
		}

		@Override
		public boolean isOpponent() {
			return true;
		}
	};

	public abstract boolean isHost();
	public abstract boolean isOpponent();

	public abstract Player choosePlayer(final HostPlayer hostPlayer, final OpponentPlayer opponentPlayer);
}
