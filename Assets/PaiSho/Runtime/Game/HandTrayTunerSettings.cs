using System;
using System.Text;
using UnityEngine;
using PaiSho;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    [Serializable]
    public class HandTrayTunerSettings
    {
        public const int MaxSlots = PieceRules.HandSize;

        public Player editingPlayer = Player.Host;

        public float standSouthCells = 2.35f;
        public float standEastCells = 2.5f;
        public float standLiftOffset;
        public float standYawOffset;
        public float standScaleMultiplier = 1f;
        public Vector3 standExtraOffset;

        public Vector3 trayLocalOffset;
        public Vector3 trayLocalEuler;

        public Vector3 opponentTrayLocalOffset;
        public Vector3 opponentTrayLocalEuler;

        public bool previewAllSlots = true;
        public bool useManualSlotPositions = true;
        public float autoSlotSpacing = 0.92f;

        public Vector3[] slotLocalPositions = CreateDefaultSlotPositions();
        public Vector3[] slotLocalEuler = CreateDefaultSlotEuler();
        public Vector3[] opponentSlotLocalPositions = CreateDefaultSlotPositions();
        public Vector3[] opponentSlotLocalEuler = CreateDefaultSlotEuler();

        public static Vector3[] CreateDefaultSlotPositions()
        {
            var positions = new Vector3[MaxSlots];
            for (int i = 0; i < MaxSlots; i++)
                positions[i] = new Vector3((i - (MaxSlots - 1) * 0.5f) * 0.38f, 0f, 0f);
            return positions;
        }

        public static Vector3[] CreateDefaultSlotEuler()
        {
            return new Vector3[MaxSlots];
        }

        public void EnsureSlotArrays()
        {
            if (slotLocalPositions == null || slotLocalPositions.Length != MaxSlots)
                slotLocalPositions = CreateDefaultSlotPositions();

            if (slotLocalEuler == null || slotLocalEuler.Length != MaxSlots)
                slotLocalEuler = CreateDefaultSlotEuler();

            if (opponentSlotLocalPositions == null || opponentSlotLocalPositions.Length != MaxSlots)
                opponentSlotLocalPositions = CreateDefaultSlotPositions();

            if (opponentSlotLocalEuler == null || opponentSlotLocalEuler.Length != MaxSlots)
                opponentSlotLocalEuler = CreateDefaultSlotEuler();
        }

        public void PullFromDefaults()
        {
            HandTrayAlignmentDefaults.ApplyHostTo(this);
            HandTrayAlignmentDefaults.ApplyOpponentTo(this);
        }

        public Vector3[] GetSlotPositions(Player player)
        {
            return player == Player.Host ? slotLocalPositions : opponentSlotLocalPositions;
        }

        public Vector3[] GetSlotEuler(Player player)
        {
            return player == Player.Host ? slotLocalEuler : opponentSlotLocalEuler;
        }

        public ref Vector3 TrayLocalOffsetFor(Player player)
        {
            return ref (player == Player.Host ? ref trayLocalOffset : ref opponentTrayLocalOffset);
        }

        public ref Vector3 TrayLocalEulerFor(Player player)
        {
            return ref (player == Player.Host ? ref trayLocalEuler : ref opponentTrayLocalEuler);
        }

        public void SeedLinearSlotLayout(float spacingCells, Player player)
        {
            EnsureSlotArrays();
            Vector3[] positions = GetSlotPositions(player);
            Vector3[] euler = GetSlotEuler(player);
            for (int i = 0; i < MaxSlots; i++)
            {
                positions[i] = new Vector3((i - (MaxSlots - 1) * 0.5f) * spacingCells, 0f, 0f);
                euler[i] = Vector3.zero;
            }
        }

        public string ToJson()
        {
            EnsureSlotArrays();
            return JsonUtility.ToJson(this, true);
        }

        public string ToShareableReport()
        {
            EnsureSlotArrays();
            var builder = new StringBuilder();
            builder.AppendLine("=== Pai Sho Hand Tray Tuner Export ===");
            builder.AppendLine($"Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine();
            builder.AppendLine("Paste this block to your agent to apply these values:");
            builder.AppendLine();
            builder.AppendLine(ToJson());
            builder.AppendLine();
            builder.AppendLine("--- Host (south stand) ---");
            AppendTraySummary(builder, Player.Host);
            builder.AppendLine();
            builder.AppendLine("--- Opponent (north stand) ---");
            AppendTraySummary(builder, Player.Opponent);
            return builder.ToString();
        }

        private void AppendTraySummary(StringBuilder builder, Player player)
        {
            Vector3 trayOffset = TrayLocalOffsetFor(player);
            Vector3 trayEuler = TrayLocalEulerFor(player);
            Vector3[] positions = GetSlotPositions(player);
            Vector3[] euler = GetSlotEuler(player);

            if (player == Player.Host)
            {
                builder.AppendLine($"standSouthCells: {standSouthCells:F4}");
                builder.AppendLine($"standEastCells: {standEastCells:F4}");
                builder.AppendLine($"standLiftOffset: {standLiftOffset:F4}");
                builder.AppendLine($"standYawOffset: {standYawOffset:F4}");
                builder.AppendLine($"standScaleMultiplier: {standScaleMultiplier:F4}");
                builder.AppendLine(
                    $"standExtraOffset: ({standExtraOffset.x:F4}, {standExtraOffset.y:F4}, {standExtraOffset.z:F4})");
            }

            builder.AppendLine(
                $"trayLocalOffset: ({trayOffset.x:F4}, {trayOffset.y:F4}, {trayOffset.z:F4})");
            builder.AppendLine(
                $"trayLocalEuler: ({trayEuler.x:F4}, {trayEuler.y:F4}, {trayEuler.z:F4})");
            builder.AppendLine($"useManualSlotPositions: {useManualSlotPositions}");
            builder.AppendLine($"autoSlotSpacing: {autoSlotSpacing:F4}");
            for (int i = 0; i < MaxSlots; i++)
            {
                Vector3 slotEuler = euler[i];
                builder.AppendLine(
                    $"slot{i} pos: ({positions[i].x:F4}, {positions[i].y:F4}, {positions[i].z:F4}) " +
                    $"euler: ({slotEuler.x:F4}, {slotEuler.y:F4}, {slotEuler.z:F4})");
            }
        }
    }
}
