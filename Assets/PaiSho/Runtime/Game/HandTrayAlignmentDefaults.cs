using UnityEngine;
using PaiSho;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    /// <summary>Player-tuned hand tray alignment from hand tray tuner export (2026-07-03).</summary>
    public static class HandTrayAlignmentDefaults
    {
        public const int MaxSlots = PieceRules.HandSize;

        public const float StandSouthCells = 4.468860149383545f;
        public const float StandEastCells = 3.2105300426483156f;
        public const float StandLiftOffset = -0.008748340420424939f;
        public const float StandYawOffset = 0f;
        public const float StandScaleMultiplier = 1.5f;
        public static readonly Vector3 StandExtraOffset = Vector3.zero;

        /// <summary>
        /// HandTray* localPosition under PlayerStand* as authored in GamePlay / GameBoardSetup prefab.
        /// Not the same as TrayLocalOffset (F9 tuner delta on top of a live tray).
        /// </summary>
        public static readonly Vector3 AuthoredTrayLocalPosition =
            new Vector3(3.3275645f, 1.5546582f, 0.90238065f);
        public static readonly Vector3 AuthoredTrayLocalEuler = Vector3.zero;
        public static readonly Vector3 AuthoredTrayLocalScale = Vector3.one;

        public static readonly Vector3 TrayLocalOffset =
            new Vector3(0.19078999757766725f, 0.015789499506354333f, 0f);
        public static readonly Vector3 TrayLocalEuler = Vector3.zero;

        public static readonly Vector3 OpponentTrayLocalOffset =
            new Vector3(0.19078999757766725f, 0.015789499506354333f, 0f);
        public static readonly Vector3 OpponentTrayLocalEuler = Vector3.zero;

        public const bool UseManualSlotPositions = true;
        public const float AutoSlotSpacing = 0.92f;

        /// <summary>Single spring draw tile uses the center rack slot (0-based).</summary>
        public const int SpringDrawSlotIndex = 3;

        private static readonly Vector3[] SlotLocalPositions =
        {
            new Vector3(-3.263159990310669f, -0.4300000071525574f, -0.026315800845623018f),
            new Vector3(-2.2139878273010256f, -0.4300000071525574f, 0.21052631735801698f),
            new Vector3(-1.1722828149795533f, -0.4300000071525574f, 0.32894739508628847f),
            new Vector3(-0.13057799637317658f, -0.4300000071525574f, 0.3684209883213043f),
            new Vector3(0.9111268520355225f, -0.4300000071525574f, 0.32894739508628847f),
            new Vector3(1.952831745147705f, -0.4201315939426422f, 0.2236841917037964f),
            new Vector3(2.9945366382598879f, -0.4300000071525574f, -0.02631578966975212f),
        };

        private static readonly Vector3[] SlotLocalEuler =
        {
            new Vector3(-55f, -15f, -1.184209942817688f),
            new Vector3(-55f, 0f, -7.697368621826172f),
            new Vector3(-55f, 0f, -2.96052622795105f),
            new Vector3(-55f, 0f, 0f),
            new Vector3(-55f, 0f, 3.5526316165924074f),
            new Vector3(-55f, 0f, 5.9210524559021f),
            new Vector3(-55f, 0f, 12.434209823608399f),
        };

        // Prefab/scene HandTrayOpponent Slot_* (GameBoardSetup.prefab / GamePlay.unity).
        private static readonly Vector3[] OpponentSlotLocalPositions =
        {
            new Vector3(-3.4f, -0.17f, -0.37f),
            new Vector3(-2.3508303f, -0.16999993f, -0.16132456f),
            new Vector3(-1.30912f, -0.16999993f, -0.056988552f),
            new Vector3(-0.26741815f, -0.16999993f, -0.017514557f),
            new Vector3(0.7742868f, -0.16999993f, -0.07107355f),
            new Vector3(1.8159899f, -0.16999993f, -0.20450556f),
            new Vector3(2.8576996f, -0.16999993f, -0.44042152f),
        };

        private static readonly Vector3[] OpponentSlotLocalEuler =
        {
            new Vector3(-55f, -15f, -1.184209942817688f),
            new Vector3(-55f, 0f, -7.6973700523376469f),
            new Vector3(-55f, 0f, -2.9605300426483156f),
            new Vector3(-55f, 0f, 0f),
            new Vector3(-55f, 0f, 3.5526299476623537f),
            new Vector3(-55f, 1.9014099836349488f, 5.921050071716309f),
            new Vector3(-55f, 0f, 12.434200286865235f),
        };

        public static void ApplyHostTo(HandTrayTunerSettings settings)
        {
            if (settings == null)
                return;

            settings.standSouthCells = StandSouthCells;
            settings.standEastCells = StandEastCells;
            settings.standLiftOffset = StandLiftOffset;
            settings.standYawOffset = StandYawOffset;
            settings.standScaleMultiplier = StandScaleMultiplier;
            settings.standExtraOffset = StandExtraOffset;
            settings.trayLocalOffset = TrayLocalOffset;
            settings.trayLocalEuler = TrayLocalEuler;
            settings.useManualSlotPositions = UseManualSlotPositions;
            settings.autoSlotSpacing = AutoSlotSpacing;
            settings.EnsureSlotArrays();

            for (int i = 0; i < MaxSlots; i++)
            {
                settings.slotLocalPositions[i] = SlotLocalPositions[i];
                settings.slotLocalEuler[i] = SlotLocalEuler[i];
            }
        }

        public static void ApplyOpponentTo(HandTrayTunerSettings settings)
        {
            if (settings == null)
                return;

            settings.opponentTrayLocalOffset = OpponentTrayLocalOffset;
            settings.opponentTrayLocalEuler = OpponentTrayLocalEuler;
            settings.EnsureSlotArrays();

            for (int i = 0; i < MaxSlots; i++)
            {
                settings.opponentSlotLocalPositions[i] = OpponentSlotLocalPositions[i];
                settings.opponentSlotLocalEuler[i] = OpponentSlotLocalEuler[i];
            }
        }

        public static void ApplyTo(HandTrayTunerSettings settings)
        {
            ApplyHostTo(settings);
            ApplyOpponentTo(settings);
        }

        public static Vector3 GetTrayLocalOffset(Player player)
        {
            return player == Player.Host ? TrayLocalOffset : OpponentTrayLocalOffset;
        }

        public static Vector3 GetTrayLocalEuler(Player player)
        {
            return player == Player.Host ? TrayLocalEuler : OpponentTrayLocalEuler;
        }

        public static Vector3 GetAuthoredTrayLocalPosition(Player player) => AuthoredTrayLocalPosition;

        public static Vector3 GetAuthoredTrayLocalEuler(Player player) => AuthoredTrayLocalEuler;

        public static Vector3 GetSlotPosition(int slotIndex, Player player)
        {
            if (slotIndex < 0 || slotIndex >= MaxSlots)
                return Vector3.zero;

            return player == Player.Host
                ? SlotLocalPositions[slotIndex]
                : OpponentSlotLocalPositions[slotIndex];
        }

        public static Vector3 GetSlotEuler(int slotIndex, Player player)
        {
            if (slotIndex < 0 || slotIndex >= MaxSlots)
                return Vector3.zero;

            return player == Player.Host
                ? SlotLocalEuler[slotIndex]
                : OpponentSlotLocalEuler[slotIndex];
        }
    }
}
