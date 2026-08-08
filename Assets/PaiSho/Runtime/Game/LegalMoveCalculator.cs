using System.Collections.Generic;
using PaiSho.Board;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public static class LegalMoveCalculator
    {
        private static readonly int[] SlideVectors = { -BoardUtils.GridSize, BoardUtils.GridSize, -1, 1 };
        private static readonly int[] NorthSouth = { -BoardUtils.GridSize, BoardUtils.GridSize };
        private static readonly int[] EastWest = { -1, 1 };

        private const int UnlimitedRange = 64;
        private const int BoatMaxPushDistance = 2;
        private const int LShapeLegSteps = 2;

        /// <summary>
        /// Basic Pai Sho (IPSA): White Flowers may not end in Red (dark) gardens;
        /// Red Flowers may not end in White (light) gardens. Neutral is allowed.
        /// </summary>
        private enum GardenLanding
        {
            AnyNonPort,
            AvoidDark,
            AvoidLight
        }

        private static bool skipDisharmonyCheck;

        public static List<LegalMove> GetLegalMoves(Piece piece)
        {
            skipDisharmonyCheck = false;
            return ComputeLegalMoves(piece);
        }

        public static List<LegalMove> GetLegalMovesIgnoringDisharmony(Piece piece)
        {
            skipDisharmonyCheck = true;
            try
            {
                return ComputeLegalMoves(piece);
            }
            finally
            {
                skipDisharmonyCheck = false;
            }
        }

        public static bool IsReachableIgnoringDisharmony(Piece piece, int coordinate)
        {
            foreach (LegalMove move in GetLegalMovesIgnoringDisharmony(piece))
            {
                if (move.Coordinate == coordinate)
                    return true;
            }

            return false;
        }

        public static List<int> GetDisharmonyBlockedLandings(Piece piece)
        {
            var legal = new HashSet<int>();
            foreach (LegalMove move in GetLegalMoves(piece))
                legal.Add(move.Coordinate);

            var blocked = new List<int>();
            foreach (LegalMove move in GetLegalMovesIgnoringDisharmony(piece))
            {
                if (legal.Contains(move.Coordinate))
                    continue;

                if (IsDestinationInDisharmony(piece, move.Coordinate))
                    blocked.Add(move.Coordinate);
            }

            return blocked;
        }

        public static List<int> GetGardenBlockedLandings(Piece piece)
        {
            var legal = new HashSet<int>();
            foreach (LegalMove move in GetLegalMoves(piece))
                legal.Add(move.Coordinate);

            var blocked = new List<int>();
            foreach (LegalMove move in GetLegalMovesIgnoringDisharmony(piece))
            {
                if (legal.Contains(move.Coordinate))
                    continue;

                if (IsDestinationInDisharmony(piece, move.Coordinate))
                    continue;

                if (FailsGardenLanding(piece, move.Coordinate))
                    blocked.Add(move.Coordinate);
            }

            return blocked;
        }

        public static Piece GetDisharmonyBlocker(Piece mover, int destinationCoordinate)
        {
            var profile = PieceHarmonyProfiles.Get(mover.Type);

            foreach (int direction in BoardUtils.CardinalDirections)
            {
                int ray = destinationCoordinate;

                while (BoardUtils.IsValidPointCoordinate(ray))
                {
                    ray += direction;
                    if (!BoardUtils.IsValidPointCoordinate(ray))
                        break;

                    Piece found = BoardManager.Instance.GetPieceAt(ray);
                    if (found == null)
                        continue;

                    if (found.Owner == mover.Owner && profile.Disharmonic.Contains(found.Type))
                        return found;

                    break;
                }
            }

            return null;
        }

        private static List<LegalMove> ComputeLegalMoves(Piece piece)
        {
            var results = new List<LegalMove>();
            if (piece == null || piece.IsImmovable() || BoardManager.Instance == null)
                return results;

            switch (piece.Type)
            {
                case PieceType.Jasmine:
                    AddSlideMoves(piece, GetSpringBonus(piece, 3), GardenLanding.AvoidDark, results);
                    break;
                case PieceType.Rose:
                    AddSlideMoves(piece, 3, GardenLanding.AvoidLight, results);
                    break;
                case PieceType.Lily:
                    AddLShapeMoves(piece, LShapeLegSteps, GardenLanding.AvoidDark, results);
                    break;
                case PieceType.Jade:
                    AddSlideMoves(piece, GetSpringBonus(piece, 5), GardenLanding.AvoidDark, results);
                    break;
                case PieceType.Chrysanthemum:
                    AddLShapeMoves(piece, LShapeLegSteps, GardenLanding.AvoidLight, results);
                    break;
                case PieceType.Rhododendron:
                    AddSlideMoves(piece, 5, GardenLanding.AvoidLight, results);
                    break;
                case PieceType.Lotus:
                    AddSlideMoves(piece, 3, GardenLanding.AnyNonPort, results, canMoveOver: true);
                    break;
                case PieceType.Orchid:
                    AddSlideMoves(piece, 3, GardenLanding.AnyNonPort, results, canMoveOver: true);
                    break;
                case PieceType.Boat:
                    AddBoatMoves(piece, results);
                    break;
                case PieceType.Wheel:
                    AddWheelMoves(piece, results);
                    break;
            }

            return results;
        }

        public static List<int> GetLegalPlacements(Player player, PieceType type)
        {
            var results = new List<int>();
            if (BoardManager.Instance == null || PlacementValidator.Instance == null)
                return results;

            bool playPhase = GameStateManager.Instance != null && GameStateManager.Instance.IsPlayPhase();
            if (playPhase && PieceRules.IsBasicFlower(type))
            {
                foreach (int port in PieceRules.GetLegalEntryPorts(player, type))
                {
                    if (PlacementValidator.Instance.CanPlace(player, type, port))
                        results.Add(port);
                }

                return results;
            }

            for (int i = 0; i < BoardUtils.NumPoints; i++)
            {
                if (!BoardUtils.IsValidPointCoordinate(i))
                    continue;

                if (PlacementValidator.Instance.CanPlace(player, type, i))
                    results.Add(i);
            }

            return results;
        }

        private static int GetSpringBonus(Piece piece, int baseMax)
        {
            if (SeasonManager.Instance == null)
                return baseMax;

            if (SeasonManager.Instance.GetCurrentSeason() == Season.Spring &&
                (piece.Type == PieceType.Jasmine || piece.Type == PieceType.Lily || piece.Type == PieceType.Jade))
                return baseMax + 1;

            return baseMax;
        }

        private static void AddBoatMoves(Piece piece, List<LegalMove> results)
        {
            foreach (int offset in SlideVectors)
                AddRayMoves(piece, offset, GardenLanding.AnyNonPort, results, unlimited: true, canJump: false, maxPushDistance: BoatMaxPushDistance);
        }

        private static void AddWheelMoves(Piece piece, List<LegalMove> results)
        {
            foreach (int offset in SlideVectors)
                AddRayMoves(piece, offset, GardenLanding.AnyNonPort, results, unlimited: true, canJump: true, maxPushDistance: 0);
        }

        private static void AddRayMoves(
            Piece piece,
            int offset,
            GardenLanding landing,
            List<LegalMove> results,
            bool unlimited,
            bool canJump,
            int maxPushDistance)
        {
            int maxSteps = unlimited ? UnlimitedRange : 0;
            int coordinate = piece.BoardCoordinate;
            int steps = 0;

            while (steps < maxSteps)
            {
                coordinate += offset;
                steps++;

                if (!BoardUtils.IsValidPointCoordinate(coordinate))
                    break;

                Piece occupant = BoardManager.Instance.GetPieceAt(coordinate);
                if (occupant == null)
                {
                    if (CanLandOn(piece, coordinate, landing))
                        AddUnique(results, new LegalMove(coordinate, false));
                    continue;
                }

                if (!canJump && maxPushDistance > 0 && !occupant.IsImmovable())
                {
                    int pushTo = FindMaxPushDestination(coordinate, offset, maxPushDistance);
                    if (pushTo >= 0 && CanLandOn(piece, coordinate, landing))
                    {
                        AddUnique(results, new LegalMove(
                            coordinate,
                            false,
                            null,
                            new PushMove(occupant, pushTo)));
                    }

                    break;
                }

                if (occupant.Owner != piece.Owner)
                {
                    if (CanLandOn(piece, coordinate, landing) && CanCapture(piece, occupant))
                        AddUnique(results, new LegalMove(coordinate, true, occupant));

                    if (!canJump)
                        break;

                    continue;
                }

                if (canJump)
                    continue;

                break;
            }
        }

        private static int FindMaxPushDestination(int blockerCoordinate, int offset, int maxPushDistance)
        {
            int best = -1;
            for (int distance = 1; distance <= maxPushDistance; distance++)
            {
                int destination = blockerCoordinate + offset * distance;
                if (!BoardUtils.IsValidPointCoordinate(destination) || BoardUtils.IsPort(destination))
                    break;

                if (BoardManager.Instance.GetPieceAt(destination) != null)
                    break;

                best = destination;
            }

            return best;
        }

        private static void AddSlideMoves(
            Piece piece,
            int maxSteps,
            GardenLanding landing,
            List<LegalMove> results,
            bool canMoveOver = false)
        {
            bool moveOver = canMoveOver || piece.CanMoveOver();

            foreach (int offset in SlideVectors)
            {
                int coordinate = piece.BoardCoordinate;
                int steps = 0;

                while (steps < maxSteps)
                {
                    coordinate += offset;
                    steps++;

                    if (!BoardUtils.IsValidPointCoordinate(coordinate))
                        break;

                    Piece occupant = BoardManager.Instance.GetPieceAt(coordinate);
                    if (occupant == null)
                    {
                        if (CanLandOn(piece, coordinate, landing))
                            AddUnique(results, new LegalMove(coordinate, false));
                    }
                    else if (occupant.Owner != piece.Owner)
                    {
                        if (CanLandOn(piece, coordinate, landing) &&
                            CanCapture(piece, occupant))
                        {
                            AddUnique(results, new LegalMove(coordinate, true, occupant));
                        }

                        if (!moveOver)
                            break;
                    }
                    else if (!moveOver)
                    {
                        break;
                    }
                }
            }
        }

        private static void AddLShapeMoves(Piece piece, int secondarySteps, GardenLanding landing, List<LegalMove> results)
        {
            AddLShapePrimary(piece, NorthSouth, EastWest, secondarySteps, landing, results);
            AddLShapePrimary(piece, EastWest, NorthSouth, secondarySteps, landing, results);
        }

        private static void AddLShapePrimary(
            Piece piece,
            int[] primaryAxes,
            int[] secondaryAxes,
            int secondarySteps,
            GardenLanding landing,
            List<LegalMove> results)
        {
            foreach (int primaryOffset in primaryAxes)
            {
                int coordinate = piece.BoardCoordinate;
                int moveCount = 0;

                while (moveCount < 2)
                {
                    coordinate += primaryOffset;
                    moveCount++;

                    if (!BoardUtils.IsValidPointCoordinate(coordinate))
                        break;

                    if (BoardManager.Instance.GetPieceAt(coordinate) != null)
                        break;
                }

                if (moveCount < 2 || !BoardUtils.IsValidPointCoordinate(coordinate))
                    continue;

                foreach (int secondaryOffset in secondaryAxes)
                {
                    int branchCoordinate = coordinate;
                    int branchCount = 0;

                    while (BoardUtils.IsValidPointCoordinate(branchCoordinate) && branchCount < secondarySteps)
                    {
                        branchCoordinate += secondaryOffset;
                        branchCount++;

                        Piece occupant = BoardManager.Instance.GetPieceAt(branchCoordinate);
                        if (occupant != null && branchCount < secondarySteps)
                            break;

                        if (branchCount == secondarySteps)
                            TryAddLanding(piece, branchCoordinate, landing, results, occupant);
                    }
                }
            }
        }

        private static void TryAddLanding(
            Piece piece,
            int coordinate,
            GardenLanding landing,
            List<LegalMove> results,
            Piece occupant = null)
        {
            occupant ??= BoardManager.Instance.GetPieceAt(coordinate);

            if (occupant == null)
            {
                if (CanLandOn(piece, coordinate, landing))
                    AddUnique(results, new LegalMove(coordinate, false));
                return;
            }

            if (occupant.Owner != piece.Owner &&
                CanLandOn(piece, coordinate, landing) &&
                CanCapture(piece, occupant))
            {
                AddUnique(results, new LegalMove(coordinate, true, occupant));
            }
        }

        private static bool CanLandOn(Piece piece, int coordinate, GardenLanding landing)
        {
            if (BoardUtils.IsPort(coordinate))
                return false;

            switch (landing)
            {
                case GardenLanding.AvoidDark:
                    if (BoardUtils.IsRedOnlyGarden(coordinate))
                        return false;
                    break;
                case GardenLanding.AvoidLight:
                    if (BoardUtils.IsWhiteOnlyGarden(coordinate))
                        return false;
                    break;
            }

            if (!skipDisharmonyCheck && IsDestinationInDisharmony(piece, coordinate))
                return false;

            return true;
        }

        private static bool FailsGardenLanding(Piece piece, int coordinate)
        {
            if (BoardUtils.IsPort(coordinate))
                return false;

            switch (piece.Type)
            {
                case PieceType.Jasmine:
                case PieceType.Lily:
                case PieceType.Jade:
                    return BoardUtils.IsRedOnlyGarden(coordinate);
                case PieceType.Rose:
                case PieceType.Chrysanthemum:
                case PieceType.Rhododendron:
                    return BoardUtils.IsWhiteOnlyGarden(coordinate);
                default:
                    return false;
            }
        }

        private static bool CanCapture(Piece attacker, Piece defender)
        {
            if (attacker == null || defender == null)
                return false;

            if (attacker.Owner == defender.Owner)
                return false;

            if (!defender.CanBeCaptured())
                return false;

            // Dragon Orchid is the aggressor: it may capture any opposing tile it can land on.
            // It does not form disharmony pairs (see Piece.CanFormDisharmony), so it must bypass that gate.
            if (attacker.Type == PieceType.Orchid)
                return true;

            if (!attacker.CanFormDisharmony() || !defender.CanBeDisharmonized())
                return false;

            return PieceHarmonyProfiles.Get(attacker.Type).Disharmonic.Contains(defender.Type);
        }

        public static bool IsDestinationInDisharmony(Piece mover, int destinationCoordinate)
        {
            var profile = PieceHarmonyProfiles.Get(mover.Type);

            foreach (int direction in BoardUtils.CardinalDirections)
            {
                int ray = destinationCoordinate;

                while (BoardUtils.IsValidPointCoordinate(ray))
                {
                    ray += direction;
                    if (!BoardUtils.IsValidPointCoordinate(ray))
                        break;

                    Piece found = BoardManager.Instance.GetPieceAt(ray);
                    if (found == null)
                        continue;

                    if (found.Owner == mover.Owner && profile.Disharmonic.Contains(found.Type))
                        return true;

                    break;
                }
            }

            return false;
        }

        public static bool TryGetLMovePath(Piece piece, int landing, List<int> path)
        {
            path.Clear();
            if (piece == null || BoardManager.Instance == null)
                return false;

            if (piece.Type != PieceType.Lily && piece.Type != PieceType.Chrysanthemum)
                return false;

            int[] northSouth = { -BoardUtils.GridSize, BoardUtils.GridSize };
            int[] eastWest = { -1, 1 };

            if (TryTraceLPath(piece.BoardCoordinate, landing, northSouth, eastWest, path))
                return true;

            return TryTraceLPath(piece.BoardCoordinate, landing, eastWest, northSouth, path);
        }

        private static bool TryTraceLPath(
            int from,
            int landing,
            int[] primaryAxes,
            int[] secondaryAxes,
            List<int> path)
        {
            foreach (int primaryOffset in primaryAxes)
            {
                int coordinate = from;
                int moveCount = 0;

                while (moveCount < LShapeLegSteps)
                {
                    coordinate += primaryOffset;
                    moveCount++;

                    if (!BoardUtils.IsValidPointCoordinate(coordinate))
                        break;

                    if (BoardManager.Instance.GetPieceAt(coordinate) != null)
                        break;
                }

                if (moveCount < LShapeLegSteps || !BoardUtils.IsValidPointCoordinate(coordinate))
                    continue;

                foreach (int secondaryOffset in secondaryAxes)
                {
                    var candidate = new List<int> { coordinate };
                    int branchCoordinate = coordinate;
                    int branchCount = 0;

                    while (BoardUtils.IsValidPointCoordinate(branchCoordinate) && branchCount < LShapeLegSteps)
                    {
                        branchCoordinate += secondaryOffset;
                        branchCount++;

                        Piece occupant = BoardManager.Instance.GetPieceAt(branchCoordinate);
                        if (occupant != null && branchCount < LShapeLegSteps)
                            break;

                        candidate.Add(branchCoordinate);

                        if (branchCount == LShapeLegSteps && branchCoordinate == landing)
                        {
                            path.Clear();
                            path.AddRange(candidate);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static void AddUnique(List<LegalMove> results, LegalMove move)
        {
            foreach (var existing in results)
            {
                if (existing.Coordinate == move.Coordinate &&
                    existing.HasPush == move.HasPush &&
                    (!move.HasPush || existing.Push.ToCoordinate == move.Push.ToCoordinate))
                {
                    return;
                }
            }

            results.Add(move);
        }
    }
}
