using System.Collections.Generic;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public readonly struct CapturePotResolvedSlot
    {
        public readonly CapturePotDisplayGroup Group;
        public readonly int DisplaySlot;
        public readonly int StackIndex;

        public CapturePotResolvedSlot(CapturePotDisplayGroup group, int displaySlot, int stackIndex)
        {
            Group = group;
            DisplaySlot = displaySlot;
            StackIndex = stackIndex;
        }
    }

    /// <summary>
    /// Maps captured pieces to compact display lanes (priority order, gaps collapse upward).
    /// </summary>
    public static class CapturePotLayoutResolver
    {
        public static CapturePotResolvedSlot Resolve(IReadOnlyList<Piece> visuals, int pieceIndex)
        {
            var plan = BuildPlacementPlan(visuals);
            Piece piece = visuals[pieceIndex];
            return plan.TryGetValue(piece, out CapturePotResolvedSlot slot)
                ? slot
                : new CapturePotResolvedSlot(CapturePotDisplayOrder.GetGroup(piece.Type), 0, 0);
        }

        public static Dictionary<Piece, CapturePotResolvedSlot> BuildPlacementPlan(IReadOnlyList<Piece> visuals)
        {
            var plan = new Dictionary<Piece, CapturePotResolvedSlot>();
            if (visuals == null || visuals.Count == 0)
                return plan;

            foreach (CapturePotDisplayGroup group in new[] { CapturePotDisplayGroup.Flowers, CapturePotDisplayGroup.SpecialAndOther })
            {
                var presentTypes = new HashSet<PieceType>();
                for (int i = 0; i < visuals.Count; i++)
                {
                    Piece piece = visuals[i];
                    if (piece == null || CapturePotDisplayOrder.GetGroup(piece.Type) != group)
                        continue;

                    presentTypes.Add(piece.Type);
                }

                if (presentTypes.Count == 0)
                    continue;

                var typeToLane = new Dictionary<PieceType, int>();
                int lane = 0;
                foreach (PieceType priorityType in CapturePotDisplayOrder.GetPriorityOrder(group))
                {
                    if (!presentTypes.Contains(priorityType))
                        continue;

                    typeToLane[priorityType] = lane;
                    lane++;
                }

                for (int i = 0; i < visuals.Count; i++)
                {
                    Piece piece = visuals[i];
                    if (piece == null || CapturePotDisplayOrder.GetGroup(piece.Type) != group)
                        continue;

                    int stackIndex = CountSameTypeBefore(visuals, i);
                    int displaySlot = typeToLane.TryGetValue(piece.Type, out int mappedLane)
                        ? mappedLane
                        : 0;

                    plan[piece] = new CapturePotResolvedSlot(group, displaySlot, stackIndex);
                }
            }

            return plan;
        }

        private static int CountSameTypeBefore(IReadOnlyList<Piece> visuals, int index)
        {
            PieceType type = visuals[index].Type;
            int count = 0;
            for (int i = 0; i < index; i++)
            {
                if (visuals[i] != null && visuals[i].Type == type)
                    count++;
            }

            return count;
        }
    }
}
