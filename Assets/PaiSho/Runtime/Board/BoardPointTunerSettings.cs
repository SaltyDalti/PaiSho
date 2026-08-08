using System;
using System.Text;
using UnityEngine;
using PaiSho.Game;

namespace PaiSho.Board
{
    public enum GardenPaintMode
    {
        Cycle = 0,
        Light = 1,
        Dark = 2,
        Mixed = 3,
        Neutral = 4
    }

    [Serializable]
    public class BoardPointTunerSettings
    {
        public bool showMarkers = true;
        public bool showLabels = true;
        public bool colorByGarden = true;
        public float markerDiameterScale = 0.11999999731779099f;
        public float markerHeightOffset = 0.08588027954101563f;
        public float markerAlpha = 0.8500000238418579f;
        public Color markerColor = new Color(0.2f, 0.85f, 1f, 0.8500000238418579f);

        public float gridSpacingScale = 0.895176112651825f;
        public float spacingFineTune = 0.9954929351806641f;
        public float gridOffsetX;
        public float gridOffsetZ;
        public float gridYawDegrees;
        public float tileHeight;
        public float tileHeightOffset = 0.010704225860536099f;
        public float colliderScale = 0.8647887706756592f;

        public float boardModelOffsetX;
        public float boardModelOffsetY;
        public float boardModelOffsetZ;

        public GardenPaintMode gardenPaintMode = GardenPaintMode.Cycle;
        public int[] lightGardenCoordinates = Array.Empty<int>();
        public int[] darkGardenCoordinates = Array.Empty<int>();

        public void SyncMarkerColorAlpha()
        {
            markerColor.a = markerAlpha;
        }

        public void PullFromDefaults()
        {
            BoardAlignmentDefaults.ApplyTo(this);
            PullGardensFromBoardUtilsDefaults();
        }

        public void PullGardensFromBoardUtilsDefaults()
        {
            BoardUtils.GetDefaultGardens(out lightGardenCoordinates, out darkGardenCoordinates);
        }

        public void PullGardensFromActive()
        {
            BoardUtils.GetActiveGardens(out lightGardenCoordinates, out darkGardenCoordinates);
        }

        public void ApplyGardensToRuntime()
        {
            BoardUtils.SetRuntimeGardens(lightGardenCoordinates, darkGardenCoordinates);
        }

        public void PullFromLayout(BoardLayout layout)
        {
            if (layout == null)
                return;

            gridSpacingScale = layout.GridSpacingScale;
            spacingFineTune = layout.SpacingFineTune;
            tileHeight = layout.TileHeight;
            tileHeightOffset = layout.PointHeightOffset;
            colliderScale = layout.BoardPointColliderScale;
            gridOffsetX = layout.GridOffset.x;
            gridOffsetZ = layout.GridOffset.y;
            gridYawDegrees = layout.GridYawDegrees;
            Vector3 modelOffset = layout.BoardModelOffset;
            boardModelOffsetX = modelOffset.x;
            boardModelOffsetY = modelOffset.y;
            boardModelOffsetZ = modelOffset.z;
            PullGardensFromActive();
        }

        public string ToJson()
        {
            SyncMarkerColorAlpha();
            PullGardensFromActive();
            return JsonUtility.ToJson(this, true);
        }

        public string ToShareableReport()
        {
            SyncMarkerColorAlpha();
            PullGardensFromActive();
            var builder = new StringBuilder();
            builder.AppendLine("=== Pai Sho Board Point Tuner Export ===");
            builder.AppendLine($"Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine();
            builder.AppendLine("Paste this block to your agent to apply these values:");
            builder.AppendLine();
            builder.AppendLine(ToJson());
            builder.AppendLine();
            builder.AppendLine("--- Readable summary ---");
            builder.AppendLine($"gridSpacingScale: {gridSpacingScale:F4}");
            builder.AppendLine($"spacingFineTune: {spacingFineTune:F4}");
            builder.AppendLine($"gridOffset: ({gridOffsetX:F4}, {gridOffsetZ:F4})");
            builder.AppendLine($"gridYawDegrees: {gridYawDegrees:F2}");
            builder.AppendLine($"tileHeight: {tileHeight:F4}");
            builder.AppendLine($"tileHeightOffset: {tileHeightOffset:F4}");
            builder.AppendLine($"markerDiameterScale: {markerDiameterScale:F4}");
            builder.AppendLine($"markerHeightOffset: {markerHeightOffset:F4}");
            builder.AppendLine($"markerColor: #{ColorUtility.ToHtmlStringRGBA(markerColor)}");
            builder.AppendLine($"colliderScale: {colliderScale:F4}");
            builder.AppendLine(
                $"boardModelOffset: ({boardModelOffsetX:F4}, {boardModelOffsetY:F4}, {boardModelOffsetZ:F4})");
            builder.AppendLine();
            builder.AppendLine("--- Ports (fixed coordinates) ---");
            builder.AppendLine($"South / Host Home: {BoardUtils.SouthGate}");
            builder.AppendLine($"North / Host Foreign: {BoardUtils.NorthGate}");
            builder.AppendLine($"East: {BoardUtils.EastGate}");
            builder.AppendLine($"West: {BoardUtils.WestGate}");
            builder.AppendLine($"Mid: {BoardUtils.MiddleGate}");
            builder.AppendLine();
            builder.AppendLine($"--- Light / White gardens ({lightGardenCoordinates.Length}) ---");
            builder.AppendLine(string.Join(", ", lightGardenCoordinates));
            builder.AppendLine();
            builder.AppendLine($"--- Dark / Red gardens ({darkGardenCoordinates.Length}) ---");
            builder.AppendLine(string.Join(", ", darkGardenCoordinates));
            return builder.ToString();
        }

        public static string GardenName(GardenType garden)
        {
            return garden switch
            {
                GardenType.LightGarden => "Light/White",
                GardenType.DarkGarden => "Dark/Red",
                GardenType.MixedGarden => "Mixed (both)",
                GardenType.Port => "Port",
                _ => "Neutral"
            };
        }
    }
}
