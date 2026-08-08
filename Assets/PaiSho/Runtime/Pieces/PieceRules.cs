using System.Collections.Generic;
using PaiSho.Board;
using PaiSho.Game;

namespace PaiSho.Pieces
{
    public static class PieceRules
    {
        public const int SpringFlowerCount = 6;
        public const int HandSize = 7;
        public const int TurnsBeforeHoldRelease = 3;
        public const int TurnsBeforeGlobalHarmony = 6;

        public static bool IsFlower(PieceType type) => IsBasicFlower(type) || IsSpecialFlower(type);

        public static bool IsBasicFlower(PieceType type)
        {
            return type == PieceType.Jasmine || type == PieceType.Rose || type == PieceType.Lily ||
                   type == PieceType.Jade || type == PieceType.Chrysanthemum || type == PieceType.Rhododendron;
        }

        public static bool IsNonFlower(PieceType type)
        {
            return type == PieceType.Boat || type == PieceType.Rock ||
                   type == PieceType.Knotweed || type == PieceType.Wheel;
        }

        public static bool IsSpecialFlower(PieceType type)
        {
            return type == PieceType.Lotus || type == PieceType.Orchid;
        }

        public static bool IsWhiteFlower(PieceType type)
        {
            return type == PieceType.Jasmine || type == PieceType.Lily || type == PieceType.Jade;
        }

        public static bool IsRedFlower(PieceType type)
        {
            return type == PieceType.Rose || type == PieceType.Chrysanthemum || type == PieceType.Rhododendron;
        }

        /// <summary>
        /// Basic Pai Sho spring placement (IPSA 1st ed.):
        /// White Flowers cannot be placed in Red-only gardens;
        /// Red Flowers cannot be placed in White-only gardens.
        /// Mixed borders (light+dark) and Neutral are allowed. Ports are not.
        /// </summary>
        public static bool IsValidSpringFlowerGarden(PieceType type, int coordinate)
        {
            if (BoardUtils.IsPort(coordinate))
                return false;

            // White flowers may not sit in Red-only (dark, not mixed) gardens.
            if (IsWhiteFlower(type) && BoardUtils.IsRedOnlyGarden(coordinate))
                return false;

            // Red flowers may not sit in White-only (light, not mixed) gardens.
            if (IsRedFlower(type) && BoardUtils.IsWhiteOnlyGarden(coordinate))
                return false;

            return true;
        }

        /// <summary>Basic Pai Sho: basic flowers enter play only through their designated port.</summary>
        public static bool IsValidPortEntry(Player player, PieceType type, int coordinate)
        {
            if (!BoardUtils.IsPort(coordinate))
                return false;

            return type switch
            {
                PieceType.Jasmine => coordinate == BoardUtils.GetHomePort(player),
                PieceType.Rose => coordinate == BoardUtils.GetForeignPort(player),
                PieceType.Lily or PieceType.Chrysanthemum => BoardUtils.IsEastOrWestPort(coordinate),
                PieceType.Jade or PieceType.Rhododendron => coordinate == BoardUtils.GetMidPort(),
                _ => false
            };
        }

        public static IEnumerable<int> GetLegalEntryPorts(Player player, PieceType type)
        {
            if (!IsBasicFlower(type))
                yield break;

            switch (type)
            {
                case PieceType.Jasmine:
                    yield return BoardUtils.GetHomePort(player);
                    break;
                case PieceType.Rose:
                    yield return BoardUtils.GetForeignPort(player);
                    break;
                case PieceType.Lily:
                case PieceType.Chrysanthemum:
                    yield return BoardUtils.GetEastPort();
                    yield return BoardUtils.GetWestPort();
                    break;
                case PieceType.Jade:
                case PieceType.Rhododendron:
                    yield return BoardUtils.GetMidPort();
                    break;
            }
        }

        public static string DescribeRequiredPort(PieceType type)
        {
            return type switch
            {
                PieceType.Jasmine => "your Home port",
                PieceType.Rose => "the Foreign port",
                PieceType.Lily or PieceType.Chrysanthemum => "the East or West port",
                PieceType.Jade or PieceType.Rhododendron => "the Mid port",
                _ => "a port"
            };
        }

        /// <summary>
        /// Basic Pai Sho movement landing (IPSA 1st ed.):
        /// A White Flower cannot end its move in a Red-only garden;
        /// a Red Flower cannot end its move in a White-only garden.
        /// Neutral and mixed border spaces are allowed.
        /// </summary>
        public static bool IsValidBasicFlowerLanding(PieceType type, int coordinate)
        {
            if (BoardUtils.IsPort(coordinate))
                return false;

            if (IsWhiteFlower(type) && BoardUtils.IsRedOnlyGarden(coordinate))
                return false;

            if (IsRedFlower(type) && BoardUtils.IsWhiteOnlyGarden(coordinate))
                return false;

            return true;
        }

        /// <summary>
        /// Ancient Pai Sho placement only (inverted gardens):
        /// White Flowers on Red Gardens, Red Flowers on White Gardens.
        /// </summary>
        public static bool IsValidAncientFlowerPlacement(PieceType type, int coordinate)
        {
            if (BoardUtils.IsPort(coordinate))
                return false;

            if (IsWhiteFlower(type))
            {
                GardenType garden = BoardUtils.GetGardenType(coordinate);
                return garden == GardenType.DarkGarden || garden == GardenType.MixedGarden;
            }

            if (IsRedFlower(type))
            {
                GardenType garden = BoardUtils.GetGardenType(coordinate);
                return garden == GardenType.LightGarden || garden == GardenType.MixedGarden;
            }

            return false;
        }

        public static bool IsNeutralGarden(int coordinate) =>
            BoardUtils.GetGardenType(coordinate) == GardenType.NeutralGarden;

        public static bool IsColoredGarden(int coordinate)
        {
            GardenType garden = BoardUtils.GetGardenType(coordinate);
            return garden == GardenType.LightGarden ||
                   garden == GardenType.DarkGarden ||
                   garden == GardenType.MixedGarden;
        }

        public static bool IsAwayFromPorts(int coordinate)
        {
            if (BoardUtils.IsPort(coordinate))
                return false;

            foreach (int neighbor in BoardUtils.GetAdjacentCoordinates(coordinate))
            {
                if (BoardUtils.IsPort(neighbor))
                    return false;
            }

            return true;
        }
    }
}
