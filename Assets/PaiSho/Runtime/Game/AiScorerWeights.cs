using UnityEngine;

namespace PaiSho.Game
{
    /// <summary>
    /// Tunable weights for intentional garden play:
    /// open with purpose, weave and encircle, finish cleanly.
    /// </summary>
    [CreateAssetMenu(fileName = "AiScorerWeights", menuName = "PaiSho/AI Scorer Weights")]
    public class AiScorerWeights : ScriptableObject
    {
        [Header("Weave harmony")]
        public int harmonyLinkWeight = 200;
        public int multiLinkBonus = 180;
        public int newHarmonyBonus = 160;
        public int breakHarmonyPenalty = 240;
        public int awakenFirstMoveBonus = 160;
        public int harmonyCreationBias = 900;
        public int awakenIntoLinkBias = 520;

        [Header("Spread / enclose the garden")]
        public int angularSpreadWeight = 130;
        public int enclosureWeight = 180;
        public int ringProgressWeight = 180;
        public int ringOneAwayBonus = 520;
        public int ringBeltBias = 22;
        public int unmovedSetupWeight = 65;
        public int emptyQuadrantBonus = 280;
        public int stuckGardenDrive = 450;
        public int enclosingCycleWeight = 520;
        public int anyCycleWeight = 180;
        public int angularSpanWeight = 5;

        [Header("Structure / finesse")]
        public int openingPlaceBias = 90;
        public int midgamePlacePenalty = 70;
        public int unusedPieceBonus = 140;
        public int monopolyMovePenalty = 320;
        public int dormantPartnerInviteBonus = 110;

        [Header("Keep the board moving")]
        public int circulateStaleBonus = 18;
        public int circulateWiltBonus = 28;
        public int idleMovePenalty = 140;
        public int overcrowdedPlacePenalty = 160;
        public int reverseMovePenalty = 850;
        public int samePieceRepeatPenalty = 300;
        public int noProgressPenalty = 300;
        public int aimlessWheelPenalty = 240;

        [Header("One-ply planning")]
        public int lookaheadHarmonyWeight = 100;
        public int lookaheadRingWeight = 720;
        public int lookaheadQuadWeight = 140;
        public int lookaheadCandidateCap = 24;

        [Header("Two-ply replies (critical positions only)")]
        public int twoPlyOurMoveCap = 8;
        public int twoPlyReplyCap = 6;
        public int twoPlyOpponentRingWeight = 700;
        public int twoPlyOurRingLossWeight = 900;
        public int twoPlyOpponentSpanWeight = 3;

        [Header("Planting")]
        public int flowerPlaceWeight = 85;
        public int specialPlaceWeight = 45;
        public int wheelPlaceWeight = 15;
        public int boatStackWeight = 35;
        public int knotweedDrainWeight = 120;

        [Header("Endgame / finish the garden")]
        public int ringCloseBias = 1800;
        public int ringCompleteBias = 7500;
        public int breakOwnRingPenalty = 1200;
        public int endgameMomentumTax = 450;
        public int nonHarmonicRevivePenalty = 320;

        [Header("Tend / defend")]
        public int captureWeight = 160;
        public int defensiveCaptureBonus = 360;
        public int opponentRingThreatPenalty = 480;
        public int behindRingDisruptBonus = 780;
        public int behindRingSelfishLinkPenalty = 280;
        public int cyclePieceCaptureBonus = 480;
        public int contestPressureBonus = 340;
        public int ringRaceBonus = 420;
        public int reviveWeight = 10;
        public int freezeWeight = 8;
        public int wiltRescueBonus = 35;
        public int momentumOverusePenalty = 180;

        [Header("Late-game progress pressure")]
        public int softPressureTurn = 100;
        public int hardForceProgressTurn = 140;
        [Range(1f, 2.5f)] public float lateGameProgressMultiplier = 1.6f;
        public int orchidThreatWeight = 140;
        public int blockOpponentRingWeight = 220;
        public int progressCaptureBoost = 50;
        public int awakenProgressBoost = 40;
        public int boatUnloadSetupWeight = 90;
        public int wheelRotateHarmonyWeight = 70;
        public int samePieceCoordRepeatPenalty = 420;

        [Header("Exploration")]
        [Range(0, 8)] public int scoreNoise = 1;

        private static AiScorerWeights runtimeFallback;

        public static AiScorerWeights Active
        {
            get
            {
                if (AiSelfPlayRunner.ActiveWeights != null)
                    return AiSelfPlayRunner.ActiveWeights;

                if (runtimeFallback == null)
                    runtimeFallback = CreateInstance<AiScorerWeights>();

                return runtimeFallback;
            }
        }
    }
}
