using System.Collections.Generic;
using UnityEngine;
using PaiSho.Board;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    /// <summary>
    /// Intentional Pai Sho AI: opening plant-and-awaken, middlegame weave-and-encircle,
    /// endgame finish. One-ply probes plus focused two-ply replies on critical seats
    /// (near-ring, behind, stuck gardens) so it can see opponent counterplay.
    /// </summary>
    public static class AiScorer
    {
        private static readonly float CenterColumn = BoardUtils.GetColumn(BoardUtils.MiddleGate);
        private static readonly float CenterRow = BoardUtils.GetRow(BoardUtils.MiddleGate);

        private enum GardenPhase
        {
            Opening,
            Middlegame,
            Endgame
        }

        private struct ScoredAction
        {
            public GameAction Action;
            public int Score;
            public int ProgressScore;
            public int RingDelta;
            public int RingAfter;
            public int QuadDelta;
            public int CycleDelta;
            public bool FillsGap;
            public bool IsDisrupt;
        }

        public static GameAction PickAction(List<GameAction> actions, Player player)
        {
            if (actions == null || actions.Count == 0)
                return default;

            AiScorerWeights w = AiScorerWeights.Active;
            Player opponent = OpponentOf(player);
            int ownRing = HarmonyRingDetector.GetRingProgress(player);
            int oppRing = HarmonyRingDetector.GetRingProgress(opponent);
            int ownEnclosing = HarmonyRingDetector.GetEnclosingCycleLength(player);
            int ownAnyCycle = HarmonyRingDetector.GetLongestCycleLength(player);
            float ownSpan = HarmonyRingDetector.GetHarmonicAngularSpanDegrees(player);
            bool opponentThreat = oppRing >= HarmonyRingDetector.MinRingSize - 1;
            bool closingOwnRing = ownRing >= HarmonyRingDetector.MinRingSize - 2;
            bool oneFromVictory = ownRing >= HarmonyRingDetector.MinRingSize - 1;
            // Contest earlier — don't wait until the human is already at ring 2+.
            bool behindOnRing = oppRing > ownRing || (oppRing >= 1 && ownRing == 0);
            int ownPieceCount = CountOwnedPieces(player);
            float[] ownAngles = CollectGardenAngles(player);
            int ownHarmony = CountHarmonized(player);
            int oppHarmony = CountHarmonized(opponent);
            int ownQuads = CountFilledQuadrants(player);
            bool[] quadMask = BuildQuadrantMask(player);
            int turn = GameManager.Instance != null ? GameManager.Instance.GetTurnNumber() : 1;
            GardenPhase phase = ResolvePhase(turn, ownPieceCount, ownHarmony, ownRing, opponentThreat || behindOnRing);
            bool stuckPrettyGarden = ownHarmony >= 4 && ownQuads >= 3 && ownEnclosing == 0 && !opponentThreat;
            bool contesting = behindOnRing || oppHarmony >= ownHarmony + 2 || oppRing >= 1;
            bool softPressure = turn >= w.softPressureTurn;
            bool hardForce = turn >= w.hardForceProgressTurn;

            var scored = new List<ScoredAction>(actions.Count);
            foreach (GameAction action in actions)
            {
                int score = ScoreAction(
                    action, player, ownRing, opponentThreat, ownPieceCount, ownAngles, w, phase, quadMask);

                int linksCreated = CountLinksActionWouldCreate(action, player);
                if (linksCreated > 0)
                {
                    score += linksCreated * w.harmonyCreationBias;
                    if (phase == GardenPhase.Opening)
                        score += w.awakenIntoLinkBias / 2;

                    if (behindOnRing && !IsDisruptAction(action, player, opponent))
                        score -= w.behindRingSelfishLinkPenalty;
                }

                score += AiPlanMemory.ScoreRepetitionPenalty(action, w);
                score += AiPlanMemory.ScoreDiversityBonus(action, w);
                score += AiStudyLibrary.ScoreActionBias(action);

                bool isDisrupt = IsDisruptAction(action, player, opponent);
                if (isDisrupt && (behindOnRing || opponentThreat || contesting))
                    score += w.behindRingDisruptBonus + w.blockOpponentRingWeight / 2;

                if (contesting)
                {
                    if (isDisrupt)
                        score += w.contestPressureBonus;
                    if (linksCreated > 0 && !behindOnRing)
                        score += w.ringRaceBonus / 3;
                }

                bool isCapture = false;
                bool awakens = false;
                int orchidThreats = 0;

                if (action.Kind == GameActionKind.Move && action.Piece != null)
                {
                    if (action.Piece.Type == PieceType.Wheel && linksCreated == 0)
                        score -= w.aimlessWheelPenalty;

                    bool fillsGap = DestinationFillsMissingQuadrant(action.Coordinate, quadMask);
                    if (fillsGap && (action.Piece.InHarmony || linksCreated > 0 ||
                                     !action.Piece.HasMovedSincePlaced))
                    {
                        score += w.emptyQuadrantBonus;
                        if (stuckPrettyGarden)
                            score += w.stuckGardenDrive;
                    }

                    awakens = !action.Piece.HasMovedSincePlaced &&
                              (PieceRules.IsBasicFlower(action.Piece.Type) ||
                               PieceRules.IsSpecialFlower(action.Piece.Type));
                    if (awakens && linksCreated > 0)
                        score += w.awakenIntoLinkBias;
                    else if (CountDormantPartnersOnRay(action.Coordinate, action.Piece, player) > 0)
                        score += w.dormantPartnerInviteBonus;

                    if (PlacementValidator.Instance != null)
                    {
                        foreach (LegalMove move in PlacementValidator.Instance.GetLegalMoves(action.Piece))
                        {
                            if (move.Coordinate != action.Coordinate)
                                continue;
                            isCapture = move.IsCapture;
                            break;
                        }
                    }

                    if (action.Piece.Type == PieceType.Orchid)
                    {
                        orchidThreats = CountOrchidThreatsFrom(action.Coordinate, player, action.Piece.BoardCoordinate);
                        if (orchidThreats > 0)
                            score += orchidThreats * w.orchidThreatWeight;
                    }
                }

                if (action.Kind == GameActionKind.Place && action.PlaceType == PieceType.Knotweed &&
                    (behindOnRing || opponentThreat || contesting))
                {
                    score += w.behindRingDisruptBonus / 2;
                }

                if (phase == GardenPhase.Opening && action.Kind == GameActionKind.Place)
                    score += w.openingPlaceBias;
                if (phase != GardenPhase.Opening && action.Kind == GameActionKind.Place && ownPieceCount >= 8)
                    score -= w.midgamePlacePenalty;

                if ((oneFromVictory || opponentThreat || behindOnRing || phase == GardenPhase.Endgame) &&
                    (action.Kind == GameActionKind.Revive || action.Kind == GameActionKind.Freeze))
                {
                    score -= w.endgameMomentumTax;
                }

                if (stuckPrettyGarden && action.Kind == GameActionKind.Move)
                    score += Mathf.RoundToInt(
                        AngularGapFill(action.Coordinate, ownAngles) * w.stuckGardenDrive);

                int progressScore = 0;
                if (linksCreated > 0)
                    progressScore += linksCreated * 4;
                if (isCapture)
                {
                    progressScore += 6;
                    score += w.progressCaptureBoost;
                }
                if (isDisrupt)
                    progressScore += 7;
                if (awakens)
                {
                    progressScore += 5;
                    score += w.awakenProgressBoost;
                }
                if (orchidThreats > 0)
                    progressScore += orchidThreats * 3;

                if (softPressure && progressScore > 0)
                    score += Mathf.RoundToInt(progressScore * (w.lateGameProgressMultiplier - 1f) * 40f);

                if (phase == GardenPhase.Opening && w.scoreNoise > 0)
                    score += Random.Range(0, w.scoreNoise + 1);
                else if (!oneFromVictory && !opponentThreat && !stuckPrettyGarden && !behindOnRing &&
                         w.scoreNoise > 0)
                {
                    score += Random.Range(0, w.scoreNoise);
                }

                scored.Add(new ScoredAction
                {
                    Action = action,
                    Score = score,
                    ProgressScore = progressScore,
                    RingDelta = 0,
                    RingAfter = ownRing,
                    QuadDelta = 0,
                    CycleDelta = 0,
                    FillsGap = action.Kind == GameActionKind.Move &&
                               DestinationFillsMissingQuadrant(action.Coordinate, quadMask),
                    IsDisrupt = isDisrupt
                });
            }

            scored.Sort((a, b) => b.Score.CompareTo(a.Score));

            int probeExtra = 0;
            if (closingOwnRing || opponentThreat || stuckPrettyGarden || behindOnRing || contesting)
                probeExtra = 12;
            else if (phase == GardenPhase.Middlegame)
                probeExtra = 4;

            int probeCount = Mathf.Min(w.lookaheadCandidateCap + probeExtra, scored.Count);

            var finalized = new List<ScoredAction>(scored.Count);

            for (int i = 0; i < scored.Count; i++)
            {
                ScoredAction entry = scored[i];
                int score = entry.Score;
                int progressScore = entry.ProgressScore;
                int ringDelta = 0;
                int ringAfter = ownRing;
                int quadDelta = 0;
                int cycleDelta = 0;

                if (i < probeCount)
                {
                    LookaheadResult look = EvaluateOnePlyProgress(
                        entry.Action,
                        player,
                        ownHarmony,
                        ownRing,
                        ownQuads,
                        ownEnclosing,
                        ownAnyCycle,
                        ownSpan,
                        w,
                        stuckPrettyGarden);
                    score += look.Score;
                    ringDelta = look.RingDelta;
                    ringAfter = look.RingAfter;
                    quadDelta = look.QuadDelta;
                    cycleDelta = look.EnclosingDelta;

                    if (ringDelta > 0)
                        progressScore += 10 + ringDelta * 5;
                    if (cycleDelta > 0)
                        progressScore += 8;
                    if (quadDelta > 0)
                        progressScore += 4;
                    if (ringAfter >= HarmonyRingDetector.MinRingSize)
                        progressScore += 20;

                    if (softPressure && (ringDelta > 0 || cycleDelta > 0 || quadDelta > 0))
                    {
                        score += Mathf.RoundToInt(
                            (ringDelta * 8 + cycleDelta * 6 + quadDelta * 4) *
                            (w.lateGameProgressMultiplier - 1f) * 50f);
                    }

                    if (ringAfter >= HarmonyRingDetector.MinRingSize)
                        score += w.ringCompleteBias;
                    else if (ringDelta > 0)
                        score += ringDelta * w.ringCloseBias;
                    else if (ringDelta < 0 && ownRing > 0)
                        score -= (-ringDelta) * w.breakOwnRingPenalty;

                    if (look.EnclosingDelta > 0)
                        score += look.EnclosingDelta * w.enclosingCycleWeight;
                    if (look.AnyCycleDelta > 0 && ownEnclosing == 0)
                        score += look.AnyCycleDelta * w.anyCycleWeight;
                    if (look.SpanDelta > 0f)
                        score += Mathf.RoundToInt(look.SpanDelta * w.angularSpanWeight);

                    if (stuckPrettyGarden && (quadDelta > 0 || look.EnclosingDelta > 0 || look.SpanDelta > 8f))
                        score += w.stuckGardenDrive;

                    // 2-ply: only on the sharpest seats — see their best reply before we commit.
                    bool wantTwoPly = i < w.twoPlyOurMoveCap &&
                                      (closingOwnRing || oneFromVictory || opponentThreat ||
                                       behindOnRing || contesting || stuckPrettyGarden || ringAfter >= 3 ||
                                       look.EnclosingDelta > 0);
                    if (wantTwoPly)
                        score -= EvaluateWorstOpponentReply(entry.Action, player, opponent, w);
                }

                entry.Score = score;
                entry.ProgressScore = progressScore;
                entry.RingDelta = ringDelta;
                entry.RingAfter = ringAfter;
                entry.QuadDelta = quadDelta;
                entry.CycleDelta = cycleDelta;
                finalized.Add(entry);
            }

            IList<ScoredAction> pool = finalized;
            if (hardForce)
            {
                var progressOnly = new List<ScoredAction>();
                foreach (ScoredAction entry in finalized)
                {
                    if (entry.ProgressScore > 0)
                        progressOnly.Add(entry);
                }

                if (progressOnly.Count > 0)
                    pool = progressOnly;
            }

            int bestScore = int.MinValue;
            var top = new List<ScoredAction>();
            var ringImprovers = new List<ScoredAction>();
            var structureImprovers = new List<ScoredAction>();
            var disruptors = new List<ScoredAction>();

            foreach (ScoredAction entry in pool)
            {
                if (entry.RingDelta > 0 || entry.RingAfter >= HarmonyRingDetector.MinRingSize || entry.CycleDelta > 0)
                    ringImprovers.Add(entry);

                if (entry.RingDelta > 0 || entry.CycleDelta > 0 || entry.QuadDelta > 0 || entry.FillsGap)
                    structureImprovers.Add(entry);

                if (entry.IsDisrupt)
                    disruptors.Add(entry);

                if (entry.Score > bestScore)
                {
                    bestScore = entry.Score;
                    top.Clear();
                    top.Add(entry);
                }
                else if (entry.Score == bestScore)
                {
                    top.Add(entry);
                }
            }

            if (top.Count == 0)
                return actions[0];

            if ((oneFromVictory || closingOwnRing) && ringImprovers.Count > 0)
                return PickBestByRing(ringImprovers).Action;

            if ((behindOnRing || contesting) && disruptors.Count > 0)
                return PickBestByScore(disruptors).Action;

            if (stuckPrettyGarden && structureImprovers.Count > 0)
                return PickBestByStructure(structureImprovers).Action;

            // Race the ring even when not yet "behind" — prefer structure that grows enclosure.
            if (phase != GardenPhase.Opening && ringImprovers.Count > 0 &&
                (ownRing > 0 || ownEnclosing > 0 || contesting))
                return PickBestByRing(ringImprovers).Action;

            if (opponentThreat && (disruptors.Count > 0 || ringImprovers.Count > 0))
            {
                if (disruptors.Count > 0)
                    return PickBestByScore(disruptors).Action;
                return PickBestByRing(ringImprovers).Action;
            }

            return top[Random.Range(0, top.Count)].Action;
        }

        private static bool IsDisruptAction(GameAction action, Player player, Player opponent)
        {
            if (action.Kind == GameActionKind.Place && action.PlaceType == PieceType.Knotweed)
                return CountAdjacentEnemyHarmony(action.Coordinate, player) > 0;

            if (action.Kind != GameActionKind.Move || action.Piece == null || PlacementValidator.Instance == null)
                return false;

            foreach (LegalMove move in PlacementValidator.Instance.GetLegalMoves(action.Piece))
            {
                if (move.Coordinate != action.Coordinate || !move.IsCapture || move.CaptureTarget == null)
                    continue;

                Piece target = move.CaptureTarget;
                if (target.Owner != opponent)
                    return false;

                return target.InHarmony || HarmonyRingDetector.IsPieceInBestEnclosingCycle(opponent, target);
            }

            return false;
        }

        private static ScoredAction PickBestByScore(List<ScoredAction> candidates)
        {
            int best = int.MinValue;
            var top = new List<ScoredAction>();
            foreach (ScoredAction entry in candidates)
            {
                if (entry.Score > best)
                {
                    best = entry.Score;
                    top.Clear();
                    top.Add(entry);
                }
                else if (entry.Score == best)
                {
                    top.Add(entry);
                }
            }

            return top[Random.Range(0, top.Count)];
        }

        private static ScoredAction PickBestByRing(List<ScoredAction> candidates)
        {
            int bestKey = int.MinValue;
            var best = new List<ScoredAction>();
            foreach (ScoredAction entry in candidates)
            {
                int key = entry.RingAfter * 100000 + entry.CycleDelta * 10000 + entry.RingDelta * 1000 +
                          entry.Score;
                if (key > bestKey)
                {
                    bestKey = key;
                    best.Clear();
                    best.Add(entry);
                }
                else if (key == bestKey)
                {
                    best.Add(entry);
                }
            }

            return best[Random.Range(0, best.Count)];
        }

        private static ScoredAction PickBestByStructure(List<ScoredAction> candidates)
        {
            int bestKey = int.MinValue;
            var best = new List<ScoredAction>();
            foreach (ScoredAction entry in candidates)
            {
                int key = entry.CycleDelta * 100000
                          + entry.RingDelta * 50000
                          + entry.QuadDelta * 10000
                          + (entry.FillsGap ? 5000 : 0)
                          + entry.Score;
                if (key > bestKey)
                {
                    bestKey = key;
                    best.Clear();
                    best.Add(entry);
                }
                else if (key == bestKey)
                {
                    best.Add(entry);
                }
            }

            return best[Random.Range(0, best.Count)];
        }

        private static GardenPhase ResolvePhase(
            int turn,
            int ownPieces,
            int ownHarmony,
            int ownRing,
            bool opponentThreat)
        {
            if (opponentThreat || ownRing >= HarmonyRingDetector.MinRingSize - 2)
                return GardenPhase.Endgame;

            if (turn <= 22 || ownPieces <= 5 || ownHarmony == 0)
                return GardenPhase.Opening;

            if (ownHarmony >= 4 && ownPieces >= 7)
                return GardenPhase.Middlegame;

            return turn <= 40 ? GardenPhase.Opening : GardenPhase.Middlegame;
        }

        public static int ScoreAction(GameAction action, Player player)
        {
            AiScorerWeights w = AiScorerWeights.Active;
            int ownRing = HarmonyRingDetector.GetRingProgress(player);
            int ownPieces = CountOwnedPieces(player);
            int ownHarmony = CountHarmonized(player);
            int turn = GameManager.Instance != null ? GameManager.Instance.GetTurnNumber() : 1;
            bool opponentThreat = HarmonyRingDetector.IsOnePieceFromVictory(OpponentOf(player));
            GardenPhase phase = ResolvePhase(turn, ownPieces, ownHarmony, ownRing, opponentThreat);
            return ScoreAction(
                action,
                player,
                ownRing,
                opponentThreat,
                ownPieces,
                CollectGardenAngles(player),
                w,
                phase,
                BuildQuadrantMask(player));
        }

        private static int ScoreAction(
            GameAction action,
            Player player,
            int ownRing,
            bool opponentThreat,
            int ownPieceCount,
            float[] ownAngles,
            AiScorerWeights w,
            GardenPhase phase,
            bool[] quadMask)
        {
            return action.Kind switch
            {
                GameActionKind.Move => ScoreMove(
                    action.Piece, action.Coordinate, player, ownRing, opponentThreat, ownAngles, w, phase, quadMask),
                GameActionKind.Place => ScorePlace(
                    action.PlaceType, action.Coordinate, player, ownRing, opponentThreat, ownPieceCount, ownAngles, w, phase, quadMask),
                GameActionKind.Revive => ScoreRevive(action.Piece, player, ownRing, w),
                GameActionKind.Freeze => ScoreFreeze(action.Piece, player, ownRing, w),
                GameActionKind.BoatLoad => ScoreBoatLoad(action, player, w),
                GameActionKind.BoatUnload => ScoreBoatUnload(action, player, w),
                GameActionKind.WheelRotate => ScoreWheelRotate(action, player, w),
                _ => 0
            };
        }

        private static int ScoreBoatLoad(GameAction action, Player player, AiScorerWeights w)
        {
            if (action.Piece == null || BoardManager.Instance == null)
                return 0;

            Piece passenger = BoardManager.Instance.GetPieceAt(action.Coordinate);
            if (passenger == null)
                return 8;

            int score = 18 + w.boatStackWeight;
            if (passenger.WiltLevel > 0)
                score += 12;
            if (!passenger.InHarmony)
                score += 10;
            return score;
        }

        private static int ScoreBoatUnload(GameAction action, Player player, AiScorerWeights w)
        {
            if (action.Piece == null || BoatManager.Instance == null)
                return 0;

            Piece cargo = BoatManager.Instance.GetCargo(action.Piece);
            if (cargo == null)
                return 0;

            int score = 22 + w.boatUnloadSetupWeight / 2;
            int partners = CountActiveHarmonyLinks(action.Coordinate, cargo, vacate: -1);
            if (partners > 0)
                score += partners * w.harmonyLinkWeight / 2 + w.boatUnloadSetupWeight;

            // Unload next to an enemy that the flower could later contest.
            foreach (int neighbor in BoardUtils.GetAdjacentCoordinates(action.Coordinate))
            {
                Piece near = BoardManager.Instance.GetPieceAt(neighbor);
                if (near != null && near.Owner != player)
                    score += w.boatUnloadSetupWeight / 3;
            }

            return score;
        }

        private static int ScoreWheelRotate(GameAction action, Player player, AiScorerWeights w)
        {
            if (action.Piece == null || BoardManager.Instance == null)
                return 0;

            int score = 12;
            int adjacentFriends = 0;
            int adjacentHarmonic = 0;
            foreach (int neighbor in BoardUtils.GetAdjacentCoordinates(action.Piece.BoardCoordinate))
            {
                Piece near = BoardManager.Instance.GetPieceAt(neighbor);
                if (near == null || near.Owner != player)
                    continue;

                adjacentFriends++;
                if (near.InHarmony || near.CanFormHarmony())
                    adjacentHarmonic++;
            }

            score += adjacentFriends * 6;
            score += adjacentHarmonic * w.wheelRotateHarmonyWeight / 2;
            if (adjacentHarmonic >= 2)
                score += w.wheelRotateHarmonyWeight;
            return score;
        }

        private static int CountOrchidThreatsFrom(int coordinate, Player player, int vacate)
        {
            if (BoardManager.Instance == null)
                return 0;

            int threats = 0;
            foreach (int neighbor in BoardUtils.GetAdjacentCoordinates(coordinate))
            {
                if (neighbor == vacate)
                    continue;

                Piece near = BoardManager.Instance.GetPieceAt(neighbor);
                if (near == null || near.Owner == player)
                    continue;
                if (!near.CanBeCaptured())
                    continue;

                threats++;
            }

            return threats;
        }

        private static int ScoreMove(
            Piece piece,
            int coordinate,
            Player player,
            int ownRing,
            bool opponentThreat,
            float[] ownAngles,
            AiScorerWeights w,
            GardenPhase phase,
            bool[] quadMask)
        {
            if (piece == null)
                return 0;

            int score = phase == GardenPhase.Opening ? 12 : 16;
            Piece captureTarget = null;

            if (PlacementValidator.Instance != null)
            {
                foreach (LegalMove move in PlacementValidator.Instance.GetLegalMoves(piece))
                {
                    if (move.Coordinate != coordinate)
                        continue;

                    if (move.IsCapture)
                    {
                        captureTarget = move.CaptureTarget;
                        score += ScoreCapture(captureTarget, player, opponentThreat, w);
                        if (phase == GardenPhase.Opening && captureTarget != null && !captureTarget.InHarmony)
                            score -= 40;
                    }

                    break;
                }
            }

            int partnersHere = CountActiveHarmonyLinks(piece.BoardCoordinate, piece, vacate: -1);
            int partnersThere = CountActiveHarmonyLinks(coordinate, piece, vacate: piece.BoardCoordinate);
            bool firstAwakening = !piece.HasMovedSincePlaced &&
                                  (PieceRules.IsBasicFlower(piece.Type) || PieceRules.IsSpecialFlower(piece.Type));

            if (firstAwakening)
            {
                score += w.awakenFirstMoveBonus;
                if (partnersThere > 0)
                    score += partnersThere * w.harmonyLinkWeight + w.newHarmonyBonus + w.awakenIntoLinkBias;
                else if (phase == GardenPhase.Opening)
                    score -= w.idleMovePenalty / 2;
                else
                    score -= w.idleMovePenalty;
            }
            else
            {
                score += ScoreHarmonyWeave(
                    partnersHere, partnersThere, piece.InHarmony, ownRing, ownAngles, coordinate, w);
            }

            // Circulation is a last resort — never the main plan in middlegame.
            if (partnersThere == 0 && phase != GardenPhase.Endgame)
            {
                if (piece.TurnsSinceMoved >= 3)
                    score += w.circulateStaleBonus * Mathf.Min(piece.TurnsSinceMoved - 2, 3);
                if (piece.WiltLevel > 0)
                    score += w.circulateWiltBonus / 2;
            }
            else if (piece.WiltLevel > 0 && partnersThere > 0)
            {
                score += w.circulateWiltBonus / 3;
            }

            if (partnersThere > 0 || piece.InHarmony || firstAwakening)
            {
                float[] angles = AnglesWithoutCoordinate(ownAngles, piece.BoardCoordinate);
                score += ScoreEnclosure(coordinate, angles, w);
                if (DestinationFillsMissingQuadrant(coordinate, quadMask))
                    score += w.emptyQuadrantBonus;
            }
            else if (phase == GardenPhase.Middlegame && DestinationFillsMissingQuadrant(coordinate, quadMask))
            {
                score += w.emptyQuadrantBonus / 2;
            }

            score += RingBeltBias(coordinate) * w.ringBeltBias;
            score += Mathf.RoundToInt(AngularGapFill(coordinate, ownAngles) * (w.angularSpreadWeight * 0.35f));

            if (piece.Type == PieceType.Wheel && partnersThere == 0)
                score -= 15;

            if (opponentThreat && captureTarget == null && partnersThere == 0)
                score -= 40;

            return score;
        }

        private static int ScorePlace(
            PieceType type,
            int coordinate,
            Player player,
            int ownRing,
            bool opponentThreat,
            int ownPieceCount,
            float[] ownAngles,
            AiScorerWeights w,
            GardenPhase phase,
            bool[] quadMask)
        {
            int score = phase == GardenPhase.Opening ? 18 : 8;

            if (PieceRules.IsBasicFlower(type))
                score += w.flowerPlaceWeight;
            if (PieceRules.IsSpecialFlower(type))
                score += w.specialPlaceWeight;
            if (type == PieceType.Wheel)
                score += w.wheelPlaceWeight;
            if (type == PieceType.Boat)
                score += w.boatStackWeight / 2;

            if (type == PieceType.Knotweed)
            {
                int drainTargets = CountAdjacentEnemyHarmony(coordinate, player);
                score += drainTargets * w.knotweedDrainWeight;
                if (opponentThreat && drainTargets > 0)
                    score += w.opponentRingThreatPenalty / 2;
            }

            if (type == PieceType.Lotus && PotManager.Instance != null &&
                PotManager.Instance.IsLotusBlooming(player))
                score += 30;

            bool isFlower = PieceRules.IsBasicFlower(type) ||
                            PieceRules.IsSpecialFlower(type) ||
                            type == PieceType.Lotus;

            if (isFlower)
            {
                int awakePartners = CountSetupPartners(coordinate, player, type, requireAwake: true);
                int dormantPartners = CountSetupPartners(coordinate, player, type, requireAwake: false);
                score += awakePartners * (w.unmovedSetupWeight + 50);
                score += dormantPartners * w.unmovedSetupWeight;
                score += Mathf.RoundToInt(AngularGapFill(coordinate, ownAngles) * (w.angularSpreadWeight * 0.65f));
                score += ScoreEnclosure(coordinate, ownAngles, w) / 2;
                score += RingBeltBias(coordinate) * w.ringBeltBias;

                if (DestinationFillsMissingQuadrant(coordinate, quadMask))
                    score += w.emptyQuadrantBonus / 2;

                if (awakePartners == 0 && dormantPartners == 0)
                    score -= w.idleMovePenalty / 2;

                if (ownPieceCount >= 7 && awakePartners == 0)
                    score -= w.overcrowdedPlacePenalty;
                if (ownPieceCount >= 10)
                    score -= w.overcrowdedPlacePenalty;
                if (phase == GardenPhase.Middlegame && ownPieceCount >= 8)
                    score -= w.midgamePlacePenalty;
            }
            else
            {
                score += RingBeltBias(coordinate) * (w.ringBeltBias / 2);
            }

            return score;
        }

        private static int ScoreHarmonyWeave(
            int partnersHere,
            int partnersThere,
            bool wasInHarmony,
            int ownRing,
            float[] ownAngles,
            int coordinate,
            AiScorerWeights w)
        {
            int score = partnersThere * w.harmonyLinkWeight;

            if (partnersThere >= 2)
                score += w.multiLinkBonus;

            if (partnersThere > partnersHere)
                score += (partnersThere - partnersHere) * w.newHarmonyBonus;

            if (wasInHarmony && partnersThere < partnersHere)
                score -= (partnersHere - partnersThere) * w.breakHarmonyPenalty;

            if (partnersThere == 0)
                score -= w.idleMovePenalty;

            score += Mathf.RoundToInt(AngularGapFill(coordinate, ownAngles) * w.angularSpreadWeight);

            if (ownRing > 0 && partnersThere > 0)
                score += ownRing * w.ringProgressWeight;
            if (ownRing == HarmonyRingDetector.MinRingSize - 1 && partnersThere > 0)
                score += w.ringOneAwayBonus;
            if (ownRing == HarmonyRingDetector.MinRingSize - 2 && partnersThere >= 2)
                score += w.ringOneAwayBonus / 2;

            return score;
        }

        private static int ScoreRevive(Piece piece, Player player, int ownRing, AiScorerWeights w)
        {
            if (piece == null)
                return 0;

            // Momentum spends the whole turn — rescue harmonic seats only, never farm wilt.
            int score = w.reviveWeight + piece.WiltLevel * 8;
            score -= w.momentumOverusePenalty;

            if (!piece.InHarmony)
            {
                score -= w.nonHarmonicRevivePenalty;
                return score;
            }

            if (piece.WiltLevel < 2)
                score -= 80;

            score += w.wiltRescueBonus;
            score += ownRing * 25;

            // Only "worth a turn" when the garden is already shaping a ring.
            if (ownRing < HarmonyRingDetector.MinRingSize - 2)
                score -= w.endgameMomentumTax / 2;

            if (ownRing >= HarmonyRingDetector.MinRingSize - 1)
                score += w.ringOneAwayBonus / 3;

            return score;
        }

        private static int ScoreFreeze(Piece piece, Player player, int ownRing, AiScorerWeights w)
        {
            if (piece == null)
                return 0;

            int score = w.freezeWeight - w.momentumOverusePenalty;
            if (!piece.InHarmony)
                score -= 70;

            if (piece.InHarmony)
                score += 30 + ownRing * 20;

            if (ownRing >= HarmonyRingDetector.MinRingSize - 1 && piece.InHarmony)
                score += 60;

            return score;
        }

        private static int ScoreCapture(Piece target, Player player, bool opponentThreat, AiScorerWeights w)
        {
            if (target == null)
                return w.captureWeight;

            int score = w.captureWeight;
            Player opponent = OpponentOf(player);
            int ownRing = HarmonyRingDetector.GetRingProgress(player);
            int oppRing = HarmonyRingDetector.GetRingProgress(opponent);
            bool behind = oppRing >= 2 && ownRing < oppRing;

            if (target.InHarmony)
                score += 80 + (behind ? w.behindRingDisruptBonus / 2 : 0);

            if ((opponentThreat || behind) && target.InHarmony)
                score += w.defensiveCaptureBonus;

            if (HarmonyRingDetector.IsPieceInBestEnclosingCycle(opponent, target))
                score += w.cyclePieceCaptureBonus + (behind ? w.behindRingDisruptBonus / 2 : 0);

            if (HarmonyRingDetector.TryGetBestEnclosingCycle(opponent, out List<Piece> cycle) &&
                cycle != null &&
                cycle.Contains(target))
            {
                score += w.opponentRingThreatPenalty;
            }

            return score;
        }

        private static int CountAdjacentEnemyHarmony(int coordinate, Player player)
        {
            int count = 0;
            foreach (int neighbor in BoardUtils.GetAdjacentCoordinates(coordinate))
            {
                Piece piece = BoardManager.Instance.GetPieceAt(neighbor);
                if (piece != null && piece.Owner != player && piece.InHarmony)
                    count++;
            }

            return count;
        }

        /// <summary>
        /// How many real harmony links a move/place action would create right now.
        /// Basic flower places create 0 (they must move first).
        /// </summary>
        private static int CountLinksActionWouldCreate(GameAction action, Player player)
        {
            if (action.Kind != GameActionKind.Move || action.Piece == null)
                return 0;

            Piece piece = action.Piece;
            if (piece.Owner != player || !piece.CanFormHarmony())
                return 0;

            return CountActiveHarmonyLinks(action.Coordinate, piece, vacate: piece.BoardCoordinate);
        }

        /// <summary>
        /// Count partners that would form a real <see cref="HarmonyManager.IsHarmony"/> link
        /// if <paramref name="movingPiece"/> were at <paramref name="coordinate"/> and able to contribute
        /// (i.e. after this move for flowers). Vacating cell is treated as empty for path checks.
        /// </summary>
        private static int CountActiveHarmonyLinks(int coordinate, Piece movingPiece, int vacate)
        {
            if (BoardManager.Instance == null || movingPiece == null || !movingPiece.CanFormHarmony())
                return 0;

            if (PieceRules.IsFlower(movingPiece.Type) && BoardUtils.IsGate(coordinate))
                return 0;

            if (IsAdjacentToKnotweedAt(coordinate, vacate))
                return 0;

            int count = 0;
            foreach (int direction in BoardUtils.CardinalDirections)
            {
                int ray = coordinate;
                while (BoardUtils.IsValidPointCoordinate(ray))
                {
                    ray += direction;
                    if (!BoardUtils.IsValidPointCoordinate(ray))
                        break;

                    if (ray == vacate)
                        continue;

                    Piece found = BoardManager.Instance.GetPieceAt(ray);
                    if (found == null)
                        continue;

                    if (ReferenceEquals(found, movingPiece))
                        break;

                    if (WouldHarmonizeWithPartner(movingPiece, coordinate, found, vacate))
                        count++;

                    break;
                }
            }

            return count;
        }

        /// <summary>
        /// Placement setup: harmonic-type neighbors on a clear ray (awake and/or still dormant).
        /// </summary>
        private static int CountSetupPartners(
            int coordinate,
            Player player,
            PieceType placeType,
            bool requireAwake)
        {
            if (BoardManager.Instance == null)
                return 0;

            int count = 0;
            foreach (int direction in BoardUtils.CardinalDirections)
            {
                int ray = coordinate;
                while (BoardUtils.IsValidPointCoordinate(ray))
                {
                    ray += direction;
                    if (!BoardUtils.IsValidPointCoordinate(ray))
                        break;

                    Piece found = BoardManager.Instance.GetPieceAt(ray);
                    if (found == null)
                        continue;

                    if (found.Owner != player || found.IsGhost || !found.CanFormHarmony())
                        break;

                    bool awake = found.CanContributeToHarmony();
                    if (requireAwake != awake)
                        break;

                    if (TypesCanHarmonize(placeType, player, found) && found.WiltLevel < 2)
                        count++;

                    break;
                }
            }

            return count;
        }

        private static bool WouldHarmonizeWithPartner(
            Piece movingPiece,
            int atCoordinate,
            Piece partner,
            int vacate)
        {
            if (partner == null || partner.IsGhost || partner.Owner != movingPiece.Owner)
                return false;

            if (!partner.CanFormHarmony() || !partner.CanContributeToHarmony())
                return false;

            if (partner.WiltLevel >= 2)
                return false;

            if (!movingPiece.CanHarmonizeWith(partner))
                return false;

            if (PieceRules.IsFlower(partner.Type) && BoardUtils.IsGate(partner.BoardCoordinate))
                return false;

            if (IsAdjacentToKnotweedAt(partner.BoardCoordinate, vacate))
                return false;

            return AreAlignedWithoutBlockersVacating(atCoordinate, partner.BoardCoordinate, vacate);
        }

        private static bool TypesCanHarmonize(PieceType placeType, Player owner, Piece partner)
        {
            if (partner == null)
                return false;

            if (placeType == PieceType.Lotus)
            {
                return partner.IsFlower() &&
                       PotManager.Instance != null &&
                       PotManager.Instance.IsLotusBlooming(owner);
            }

            if (partner.Type == PieceType.Lotus && partner.IsBlooming() && PieceRules.IsBasicFlower(placeType))
                return true;

            if (!PieceRules.IsBasicFlower(placeType) || !partner.IsFlower())
                return false;

            return PieceHarmonyProfiles.Get(placeType).Harmonic.Contains(partner.Type);
        }

        private static bool AreAlignedWithoutBlockersVacating(int from, int to, int vacate)
        {
            if (from == to)
                return false;

            int rowDelta = BoardUtils.GetRow(to) - BoardUtils.GetRow(from);
            int colDelta = BoardUtils.GetColumn(to) - BoardUtils.GetColumn(from);

            if (rowDelta != 0 && colDelta != 0)
                return false;

            int rowStep = rowDelta == 0 ? 0 : (rowDelta > 0 ? 1 : -1);
            int colStep = colDelta == 0 ? 0 : (colDelta > 0 ? 1 : -1);
            int row = BoardUtils.GetRow(from) + rowStep;
            int col = BoardUtils.GetColumn(from) + colStep;

            while (row != BoardUtils.GetRow(to) || col != BoardUtils.GetColumn(to))
            {
                int coordinate = row * BoardUtils.GridSize + col;
                if (coordinate != vacate)
                {
                    Piece blocker = BoardManager.Instance.GetPieceAt(coordinate);
                    if (blocker != null)
                        return false;
                }

                row += rowStep;
                col += colStep;
            }

            return true;
        }

        private static bool IsAdjacentToKnotweedAt(int coordinate, int vacate)
        {
            if (BoardManager.Instance == null)
                return false;

            foreach (int check in BoardUtils.GetAdjacentCoordinates(coordinate))
            {
                if (check == vacate)
                    continue;

                Piece neighbor = BoardManager.Instance.GetPieceAt(check);
                if (neighbor != null && neighbor.Type == PieceType.Knotweed)
                    return true;
            }

            return false;
        }

        private static int RingBeltBias(int coordinate)
        {
            int centerRow = BoardUtils.GetRow(BoardUtils.MiddleGate);
            int centerCol = BoardUtils.GetColumn(BoardUtils.MiddleGate);
            int distance = Mathf.Abs(BoardUtils.GetRow(coordinate) - centerRow) +
                           Mathf.Abs(BoardUtils.GetColumn(coordinate) - centerCol);

            // Prefer a living ring around the gate, not the pit or the rim.
            if (distance >= 2 && distance <= 5)
                return 2;
            if (distance == 1 || distance == 6)
                return 1;
            return 0;
        }

        private static int ScoreEnclosure(int coordinate, float[] existingAngles, AiScorerWeights w)
        {
            var occupied = new bool[4];
            void Mark(float angle)
            {
                float deg = ((angle * Mathf.Rad2Deg) % 360f + 360f) % 360f;
                int q = Mathf.Clamp(Mathf.FloorToInt(deg / 90f), 0, 3);
                occupied[q] = true;
            }

            if (existingAngles != null)
            {
                foreach (float a in existingAngles)
                    Mark(a);
            }

            Mark(AngleFromCenter(coordinate));

            int filled = 0;
            for (int i = 0; i < 4; i++)
            {
                if (occupied[i])
                    filled++;
            }

            int score = filled * (w.enclosureWeight / 4);
            if (filled >= 3)
                score += w.enclosureWeight / 2;
            if (filled >= 4)
                score += w.enclosureWeight;
            return score;
        }

        private static float[] AnglesWithoutCoordinate(float[] angles, int excludeCoordinate)
        {
            if (angles == null || angles.Length == 0)
                return System.Array.Empty<float>();

            float exclude = AngleFromCenter(excludeCoordinate);
            var kept = new List<float>(angles.Length);
            foreach (float a in angles)
            {
                if (Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, exclude * Mathf.Rad2Deg)) < 2f)
                    continue;
                kept.Add(a);
            }

            return kept.ToArray();
        }

        private static int CountOwnedPieces(Player player)
        {
            if (BoardManager.Instance == null)
                return 0;

            int count = 0;
            foreach (Piece piece in BoardManager.Instance.GetAllPieces())
            {
                if (piece != null && piece.Owner == player && !piece.IsGhost)
                    count++;
            }

            return count;
        }

        private static float[] CollectGardenAngles(Player player)
        {
            if (BoardManager.Instance == null)
                return System.Array.Empty<float>();

            var angles = new List<float>();
            foreach (Piece piece in BoardManager.Instance.GetAllPieces())
            {
                if (piece == null || piece.Owner != player || piece.IsGhost)
                    continue;

                // Prefer angles of living / harmonic flowers when shaping the ring.
                if (piece.InHarmony || (piece.IsFlower() && piece.WiltLevel == 0))
                    angles.Add(AngleFromCenter(piece.BoardCoordinate));
            }

            return angles.ToArray();
        }

        private struct LookaheadResult
        {
            public int Score;
            public int RingDelta;
            public int RingAfter;
            public int QuadDelta;
            public int EnclosingDelta;
            public int AnyCycleDelta;
            public float SpanDelta;
        }

        private struct SearchMoveState
        {
            public Piece Piece;
            public int From;
            public int To;
            public bool WasAwakened;
            public List<(Piece piece, bool inHarmony)> HarmonySnapshot;
        }

        private static bool TryBeginSearchMove(GameAction action, out SearchMoveState state)
        {
            state = default;
            if (action.Kind != GameActionKind.Move || action.Piece == null || BoardManager.Instance == null)
                return false;

            if (PlacementValidator.Instance != null)
            {
                foreach (LegalMove move in PlacementValidator.Instance.GetLegalMoves(action.Piece))
                {
                    if (move.Coordinate != action.Coordinate)
                        continue;
                    if (move.IsCapture || move.HasPush)
                        return false;
                    break;
                }
            }

            Piece piece = action.Piece;
            int from = piece.BoardCoordinate;
            int to = action.Coordinate;
            if (from == to || from < 0)
                return false;

            var snapshot = new List<(Piece piece, bool inHarmony)>();
            foreach (Piece p in BoardManager.Instance.GetAllPieces())
            {
                if (p != null)
                    snapshot.Add((p, p.InHarmony));
            }

            if (!BoardManager.Instance.TryRelocateForSearch(piece, to))
                return false;

            state = new SearchMoveState
            {
                Piece = piece,
                From = from,
                To = to,
                WasAwakened = piece.HasMovedSincePlaced,
                HarmonySnapshot = snapshot
            };
            piece.HasMovedSincePlaced = true;
            BoardManager.Instance.RefreshHarmonyFlagsOnly();
            return true;
        }

        private static void EndSearchMove(SearchMoveState state)
        {
            if (state.Piece == null || BoardManager.Instance == null)
                return;

            BoardManager.Instance.TryRelocateForSearch(state.Piece, state.From);
            state.Piece.HasMovedSincePlaced = state.WasAwakened;
            if (state.HarmonySnapshot == null)
                return;

            foreach (var pair in state.HarmonySnapshot)
            {
                if (pair.piece != null)
                    pair.piece.InHarmony = pair.inHarmony;
            }
        }

        /// <summary>
        /// After we hypothetically move, estimate the opponent's most damaging quiet reply.
        /// Returned value is subtracted from our candidate score (minimax-style).
        /// </summary>
        private static int EvaluateWorstOpponentReply(
            GameAction ourAction,
            Player player,
            Player opponent,
            AiScorerWeights w)
        {
            if (!TryBeginSearchMove(ourAction, out SearchMoveState ourState))
                return 0;

            int ourRingAfterOurMove = HarmonyRingDetector.GetRingProgress(player);
            int ourEncAfterOurMove = HarmonyRingDetector.GetEnclosingCycleLength(player);
            int oppRingBeforeReply = HarmonyRingDetector.GetRingProgress(opponent);
            float oppSpanBeforeReply = HarmonyRingDetector.GetHarmonicAngularSpanDegrees(opponent);

            int worstHarm = ScoreOpponentCaptureThreats(opponent, player, w);

            List<GameAction> replies = ActionGenerator.GetAllLegalActions(opponent);
            if (replies == null || replies.Count == 0)
            {
                EndSearchMove(ourState);
                return worstHarm;
            }

            replies.Sort((a, b) =>
                RoughReplyPriority(b, opponent).CompareTo(RoughReplyPriority(a, opponent)));

            int checkedReplies = 0;
            for (int i = 0; i < replies.Count && checkedReplies < w.twoPlyReplyCap; i++)
            {
                GameAction reply = replies[i];
                if (reply.Kind != GameActionKind.Move)
                    continue;

                if (!TryBeginSearchMove(reply, out SearchMoveState replyState))
                    continue;

                checkedReplies++;
                int oppRingAfter = HarmonyRingDetector.GetRingProgress(opponent);
                int ourRingNow = HarmonyRingDetector.GetRingProgress(player);
                int ourEncNow = HarmonyRingDetector.GetEnclosingCycleLength(player);
                float oppSpanAfter = HarmonyRingDetector.GetHarmonicAngularSpanDegrees(opponent);

                int harm = (oppRingAfter - oppRingBeforeReply) * w.twoPlyOpponentRingWeight;
                if (oppRingAfter >= HarmonyRingDetector.MinRingSize)
                    harm += w.ringCompleteBias / 2;

                if (ourRingNow < ourRingAfterOurMove)
                    harm += (ourRingAfterOurMove - ourRingNow) * w.twoPlyOurRingLossWeight;
                if (ourEncNow < ourEncAfterOurMove)
                    harm += (ourEncAfterOurMove - ourEncNow) * w.enclosingCycleWeight;

                float spanGain = oppSpanAfter - oppSpanBeforeReply;
                if (spanGain > 0f)
                    harm += Mathf.RoundToInt(spanGain * w.twoPlyOpponentSpanWeight);

                if (harm > worstHarm)
                    worstHarm = harm;

                EndSearchMove(replyState);
            }

            EndSearchMove(ourState);
            return Mathf.Max(0, worstHarm);
        }

        private static int RoughReplyPriority(GameAction action, Player actor)
        {
            if (action.Kind != GameActionKind.Move || action.Piece == null)
                return int.MinValue;

            int score = CountLinksActionWouldCreate(action, actor) * 100;
            score += RingBeltBias(action.Coordinate) * 10;
            if (!action.Piece.HasMovedSincePlaced)
                score += 40;
            return score;
        }

        private static int ScoreOpponentCaptureThreats(Player opponent, Player player, AiScorerWeights w)
        {
            if (PlacementValidator.Instance == null || BoardManager.Instance == null)
                return 0;

            int threat = 0;
            foreach (Piece piece in BoardManager.Instance.GetAllPieces())
            {
                if (piece == null || piece.Owner != opponent || piece.IsGhost)
                    continue;

                foreach (LegalMove move in PlacementValidator.Instance.GetLegalMoves(piece))
                {
                    if (!move.IsCapture || move.CaptureTarget == null)
                        continue;

                    Piece target = move.CaptureTarget;
                    if (target.Owner != player)
                        continue;

                    int local = w.defensiveCaptureBonus / 2;
                    if (target.InHarmony)
                        local += w.behindRingDisruptBonus / 2;
                    if (HarmonyRingDetector.IsPieceInBestEnclosingCycle(player, target))
                        local += w.cyclePieceCaptureBonus;
                    if (local > threat)
                        threat = local;
                }
            }

            return threat;
        }

        /// <summary>
        /// Silently try the move, measure garden progress, then revert.
        /// </summary>
        private static LookaheadResult EvaluateOnePlyProgress(
            GameAction action,
            Player player,
            int harmonyBefore,
            int ringBefore,
            int quadsBefore,
            int enclosingBefore,
            int anyCycleBefore,
            float spanBefore,
            AiScorerWeights w,
            bool stuckPrettyGarden)
        {
            var empty = new LookaheadResult
            {
                Score = 0,
                RingDelta = 0,
                RingAfter = ringBefore,
                QuadDelta = 0,
                EnclosingDelta = 0,
                AnyCycleDelta = 0,
                SpanDelta = 0f
            };

            if (!TryBeginSearchMove(action, out SearchMoveState state))
                return empty;

            int harmonyAfter = CountHarmonized(player);
            int ringAfter = HarmonyRingDetector.GetRingProgress(player);
            int quadsAfter = CountFilledQuadrants(player);
            int enclosingAfter = HarmonyRingDetector.GetEnclosingCycleLength(player);
            int anyCycleAfter = HarmonyRingDetector.GetLongestCycleLength(player);
            float spanAfter = HarmonyRingDetector.GetHarmonicAngularSpanDegrees(player);

            EndSearchMove(state);

            int deltaHarmony = harmonyAfter - harmonyBefore;
            int deltaRing = ringAfter - ringBefore;
            int deltaQuads = quadsAfter - quadsBefore;
            int deltaEnclosing = enclosingAfter - enclosingBefore;
            int deltaAnyCycle = anyCycleAfter - anyCycleBefore;
            float deltaSpan = spanAfter - spanBefore;

            int score = deltaHarmony * w.lookaheadHarmonyWeight
                        + deltaRing * w.lookaheadRingWeight
                        + deltaQuads * w.lookaheadQuadWeight
                        + deltaEnclosing * w.enclosingCycleWeight
                        + (enclosingBefore == 0 ? deltaAnyCycle * w.anyCycleWeight : 0)
                        + Mathf.RoundToInt(deltaSpan * w.angularSpanWeight);

            if (stuckPrettyGarden)
            {
                score += deltaQuads * w.stuckGardenDrive;
                score += deltaEnclosing * w.stuckGardenDrive;
                if (deltaSpan > 0f)
                    score += Mathf.RoundToInt(deltaSpan * w.angularSpanWeight * 2f);
                if (deltaRing == 0 && deltaQuads == 0 && deltaEnclosing == 0 && deltaHarmony <= 0 &&
                    deltaSpan <= 0f)
                {
                    score -= w.stuckGardenDrive;
                }
            }

            if (deltaHarmony <= 0 && deltaRing <= 0 && deltaQuads <= 0 && deltaEnclosing <= 0 &&
                deltaSpan <= 0f)
            {
                score -= w.noProgressPenalty;
            }

            return new LookaheadResult
            {
                Score = score,
                RingDelta = deltaRing,
                RingAfter = ringAfter,
                QuadDelta = deltaQuads,
                EnclosingDelta = deltaEnclosing,
                AnyCycleDelta = deltaAnyCycle,
                SpanDelta = deltaSpan
            };
        }

        private static bool[] BuildQuadrantMask(Player player)
        {
            var occupied = new bool[4];
            if (BoardManager.Instance == null)
                return occupied;

            foreach (Piece piece in BoardManager.Instance.GetAllPieces())
            {
                if (piece == null || piece.Owner != player || piece.IsGhost)
                    continue;
                if (!(piece.InHarmony || (piece.IsFlower() && piece.WiltLevel == 0)))
                    continue;

                occupied[QuadrantOf(piece.BoardCoordinate)] = true;
            }

            return occupied;
        }

        private static int QuadrantOf(int coordinate)
        {
            float dx = BoardUtils.GetColumn(coordinate) - CenterColumn;
            float dy = BoardUtils.GetRow(coordinate) - CenterRow;
            float deg = ((Mathf.Atan2(dy, dx) * Mathf.Rad2Deg) % 360f + 360f) % 360f;
            return Mathf.Clamp(Mathf.FloorToInt(deg / 90f), 0, 3);
        }

        private static bool DestinationFillsMissingQuadrant(int coordinate, bool[] quadMask)
        {
            if (quadMask == null || quadMask.Length < 4)
                return false;

            int q = QuadrantOf(coordinate);
            return !quadMask[q];
        }

        private static int CountDormantPartnersOnRay(int coordinate, Piece movingPiece, Player player)
        {
            if (BoardManager.Instance == null || movingPiece == null)
                return 0;

            int count = 0;
            foreach (int direction in BoardUtils.CardinalDirections)
            {
                int ray = coordinate;
                while (BoardUtils.IsValidPointCoordinate(ray))
                {
                    ray += direction;
                    if (!BoardUtils.IsValidPointCoordinate(ray))
                        break;

                    if (ray == movingPiece.BoardCoordinate)
                        continue;

                    Piece found = BoardManager.Instance.GetPieceAt(ray);
                    if (found == null)
                        continue;

                    if (found.Owner == player &&
                        !found.IsGhost &&
                        found.CanFormHarmony() &&
                        !found.CanContributeToHarmony() &&
                        movingPiece.CanHarmonizeWith(found))
                    {
                        count++;
                    }

                    break;
                }
            }

            return count;
        }

        private static int CountHarmonized(Player player)
        {
            if (BoardManager.Instance == null)
                return 0;

            int count = 0;
            foreach (Piece piece in BoardManager.Instance.GetAllPieces())
            {
                if (piece != null && piece.Owner == player && !piece.IsGhost && piece.InHarmony)
                    count++;
            }

            return count;
        }

        private static int CountFilledQuadrants(Player player)
        {
            if (BoardManager.Instance == null)
                return 0;

            var occupied = new bool[4];
            foreach (Piece piece in BoardManager.Instance.GetAllPieces())
            {
                if (piece == null || piece.Owner != player || piece.IsGhost)
                    continue;
                if (!(piece.InHarmony || (piece.IsFlower() && piece.WiltLevel == 0)))
                    continue;

                float dx = BoardUtils.GetColumn(piece.BoardCoordinate) - CenterColumn;
                float dy = BoardUtils.GetRow(piece.BoardCoordinate) - CenterRow;
                float deg = ((Mathf.Atan2(dy, dx) * Mathf.Rad2Deg) % 360f + 360f) % 360f;
                int q = Mathf.Clamp(Mathf.FloorToInt(deg / 90f), 0, 3);
                occupied[q] = true;
            }

            int filled = 0;
            for (int i = 0; i < 4; i++)
            {
                if (occupied[i])
                    filled++;
            }

            return filled;
        }

        private static float AngleFromCenter(int coordinate)
        {
            float dx = BoardUtils.GetColumn(coordinate) - CenterColumn;
            float dy = BoardUtils.GetRow(coordinate) - CenterRow;
            return Mathf.Atan2(dy, dx);
        }

        private static float AngularGapFill(int coordinate, float[] existingAngles)
        {
            if (existingAngles == null || existingAngles.Length == 0)
                return 0.4f;

            float angle = AngleFromCenter(coordinate);
            var sorted = new List<float>(existingAngles);
            sorted.Sort();

            float bestGap = 0f;
            float bestMid = sorted[0];
            for (int i = 0; i < sorted.Count; i++)
            {
                float a = sorted[i];
                float b = i + 1 < sorted.Count ? sorted[i + 1] : sorted[0] + Mathf.PI * 2f;
                float gap = b - a;
                if (gap > bestGap)
                {
                    bestGap = gap;
                    bestMid = a + gap * 0.5f;
                }
            }

            if (bestGap < 0.35f)
                return 0.1f;

            float delta = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, bestMid * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
            float closeness = 1f - Mathf.Clamp01(delta / Mathf.Max(bestGap * 0.5f, 0.2f));
            return closeness * Mathf.Clamp01(bestGap / Mathf.PI);
        }

        private static Player OpponentOf(Player player) =>
            player == Player.Host ? Player.Opponent : Player.Host;
    }
}
