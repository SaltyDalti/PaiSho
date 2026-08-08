using System;
using System.Collections;
using UnityEngine;
using PaiSho;
using PaiSho.Board;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    public enum PiecePlacementMotion
    {
        Immediate,
        HoverDrop,
        TrayToBoard
    }

    /// <summary>Shared easing curves and tile motion paths for consistent board feel.</summary>
    public static class PieceMotion
    {
        public const float BoardHoverLift = 0.26f;
        public const float TrayDragLift = 0.16f;
        public const float MoveArcLift = 0.09f;

        /// <summary>Unity fake-null safe — destroyed tiles must abort mid-coroutine.</summary>
        private static bool IsAlive(Transform tile) => tile != null;

        public static Vector3 GetBoardHoverPosition(int coordinate)
        {
            if (BoardManager.Instance == null)
                return Vector3.zero;

            return BoardManager.Instance.GetPieceWorldPosition(coordinate) + Vector3.up * BoardHoverLift;
        }

        public static Vector3 GetAutoPlaceOrigin(Player player)
        {
            if (BoardManager.Instance == null)
                return Vector3.zero;

            int gate = player == Player.Host ? BoardUtils.SouthGate : BoardUtils.NorthGate;
            Vector3 gatePosition = BoardManager.Instance.GetPieceWorldPosition(gate);
            return gatePosition + Vector3.up * (BoardHoverLift * 0.85f);
        }

        public static float EaseInQuad(float t) => t * t;
        public static float EaseOutQuad(float t)
        {
            float u = 1f - t;
            return 1f - u * u;
        }

        public static float EaseInCubic(float t) => t * t * t;
        public static float EaseOutCubic(float t)
        {
            float u = 1f - t;
            return 1f - u * u * u;
        }

        public static float EaseInOutCubic(float t) =>
            t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;

        public static float EaseOutBack(float t, float overshoot = 1.4f)
        {
            float c1 = overshoot;
            float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        public static float EaseOutElastic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI) / 3f) + 1f;
        }

        public static float EaseOutQuart(float t)
        {
            float u = 1f - t;
            return 1f - u * u * u * u;
        }

        public static Vector3 QuadraticBezier(Vector3 start, Vector3 control, Vector3 end, float t)
        {
            float u = 1f - t;
            return u * u * start + 2f * u * t * control + t * t * end;
        }

        public static IEnumerator AnimateAnticipation(Transform tile, float liftAmount, float duration = 0.09f)
        {
            if (!IsAlive(tile))
                yield break;

            Vector3 start = tile.position;
            Vector3 peak = start + Vector3.up * liftAmount;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (!IsAlive(tile))
                    yield break;

                elapsed += Time.deltaTime;
                float u = EaseOutQuad(Mathf.Clamp01(elapsed / duration));
                tile.position = Vector3.Lerp(start, peak, u);
                yield return null;
            }
        }

        public static IEnumerator AnimateHoverDrop(
            Transform tile,
            Vector3 end,
            Quaternion endRotation,
            float duration)
        {
            yield return AnimateCeramicDropOnWood(tile, end, endRotation, duration);
        }

        /// <summary>
        /// Ceramic tile falling onto wood: gravity ease-in, hard contact, micro-damped settle.
        /// </summary>
        public static IEnumerator AnimateCeramicDropOnWood(
            Transform tile,
            Vector3 end,
            Quaternion endRotation,
            float duration,
            System.Action onImpact = null)
        {
            if (!IsAlive(tile))
                yield break;

            Vector3 start = tile.position;
            Quaternion startRotation = tile.rotation;
            float elapsed = 0f;
            bool impactFired = false;

            while (elapsed < duration)
            {
                if (!IsAlive(tile))
                    yield break;

                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / duration);

                // Horizontal drift settles quickly; vertical follows gravity (ease-in).
                float horizontal = EaseOutCubic(u);
                float vertical = EaseInQuad(u);

                float y = Mathf.Lerp(start.y, end.y, vertical);
                if (y <= end.y + 0.0002f && !impactFired)
                {
                    impactFired = true;
                    onImpact?.Invoke();
                }

                tile.position = new Vector3(
                    Mathf.Lerp(start.x, end.x, horizontal),
                    Mathf.Max(y, end.y),
                    Mathf.Lerp(start.z, end.z, horizontal));
                tile.rotation = Quaternion.Slerp(startRotation, endRotation, EaseOutCubic(u));
                yield return null;
            }

            if (!IsAlive(tile))
                yield break;

            tile.position = end;
            tile.rotation = endRotation;

            if (!impactFired)
                onImpact?.Invoke();

            yield return AnimateCeramicImpactSettle(tile, end);
        }

        public static float ComputeTravelArcHeight(Vector3 start, Vector3 end, float baseArcHeight)
        {
            float horizontal = Vector3.Distance(
                new Vector3(start.x, 0f, start.z),
                new Vector3(end.x, 0f, end.z));
            float vertical = Mathf.Max(0f, start.y - end.y);
            float trayLift = vertical * 0.55f;
            float travelLift = horizontal * 0.08f;
            return Mathf.Max(baseArcHeight + 0.06f, baseArcHeight + trayLift + travelLift);
        }

        public static float ComputeAnticipationLift(Vector3 start, Vector3 end)
        {
            float vertical = Mathf.Max(0f, start.y - end.y);
            return vertical > 0.06f ? 0.055f : 0.035f;
        }

        /// <summary>Smooth board travel — same arc used for moves and placements.</summary>
        public static IEnumerator AnimateBoardTravel(
            Transform tile,
            Vector3 start,
            Vector3 end,
            Quaternion startRotation,
            Quaternion endRotation,
            float duration,
            float arcHeight,
            System.Action onImpact = null)
        {
            if (!IsAlive(tile))
                yield break;

            Vector3 control = Vector3.Lerp(start, end, 0.5f) + Vector3.up * arcHeight;
            float elapsed = 0f;
            bool impactFired = false;
            const float impactPhase = 0.88f;

            while (elapsed < duration)
            {
                if (!IsAlive(tile))
                    yield break;

                elapsed += Time.deltaTime;
                float u = EaseInOutCubic(Mathf.Clamp01(elapsed / duration));

                tile.position = QuadraticBezier(start, control, end, u);
                tile.rotation = Quaternion.Slerp(startRotation, endRotation, EaseInOutCubic(u));

                if (u >= impactPhase && !impactFired)
                {
                    impactFired = true;
                    onImpact?.Invoke();
                }

                yield return null;
            }

            if (!IsAlive(tile))
                yield break;

            tile.position = end;
            tile.rotation = endRotation;

            if (!impactFired)
                onImpact?.Invoke();

            yield return AnimateCeramicImpactSettle(tile, end);
        }

        /// <summary>Alias kept for callers — routes to the unified travel arc.</summary>
        public static IEnumerator AnimateCeramicTrayToWood(
            Transform tile,
            Vector3 start,
            Vector3 end,
            Quaternion startRotation,
            Quaternion endRotation,
            float duration,
            float arcHeight,
            System.Action onImpact = null)
        {
            return AnimateBoardTravel(
                tile,
                start,
                end,
                startRotation,
                endRotation,
                duration,
                arcHeight,
                onImpact);
        }

        /// <summary>Post-impact micro-vibration — ceramic has almost no bounce on wood.</summary>
        public static IEnumerator AnimateCeramicImpactSettle(Transform tile, Vector3 rest, float duration = 0.1f)
        {
            if (!IsAlive(tile))
                yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (!IsAlive(tile))
                    yield break;

                elapsed += Time.deltaTime;
                float u = elapsed / duration;
                float damped = Mathf.Exp(-u * 16f);
                float micro = damped * Mathf.Sin(u * 42f) * 0.0011f;
                micro = Mathf.Max(0f, micro);
                tile.position = rest + Vector3.up * micro;
                yield return null;
            }

            if (!IsAlive(tile))
                yield break;

            tile.position = rest;
        }

        public static IEnumerator AnimateTrayToBoard(
            Transform tile,
            Vector3 start,
            Vector3 end,
            Quaternion startRotation,
            Quaternion endRotation,
            float duration,
            float arcHeight)
        {
            if (!IsAlive(tile))
                yield break;

            Vector3 control = Vector3.Lerp(start, end, 0.5f) + Vector3.up * arcHeight;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (!IsAlive(tile))
                    yield break;

                elapsed += Time.deltaTime;
                float u = EaseOutQuart(Mathf.Clamp01(elapsed / duration));
                tile.position = QuadraticBezier(start, control, end, u);
                tile.rotation = Quaternion.Slerp(startRotation, endRotation, EaseOutCubic(u));
                yield return null;
            }

            if (!IsAlive(tile))
                yield break;

            tile.position = end;
            tile.rotation = endRotation;
        }

        public static IEnumerator AnimateBoardMove(
            Transform tile,
            Vector3 start,
            Vector3 end,
            float duration,
            float arcHeight)
        {
            if (!IsAlive(tile))
                yield break;

            Quaternion endRotation = Quaternion.Euler(0f, tile.eulerAngles.y, 0f);
            yield return AnimateTrayToBoard(
                tile,
                start,
                end,
                tile.rotation,
                endRotation,
                duration,
                arcHeight);
        }

        public static IEnumerator AnimateSnap(
            Transform tile,
            Vector3 start,
            Vector3 end,
            Quaternion startRotation,
            Quaternion endRotation,
            float duration)
        {
            if (!IsAlive(tile))
                yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (!IsAlive(tile))
                    yield break;

                elapsed += Time.deltaTime;
                float u = EaseOutCubic(Mathf.Clamp01(elapsed / duration));
                tile.position = Vector3.Lerp(start, end, u);
                tile.rotation = Quaternion.Slerp(startRotation, endRotation, u);
                yield return null;
            }

            if (!IsAlive(tile))
                yield break;

            tile.position = end;
            tile.rotation = endRotation;
        }

        /// <summary>Ceramic tiles don't deform — a tiny rigid knock on landing only.</summary>
        public static IEnumerator AnimateCeramicKnock(Transform tile, float duration = 0.14f, float amplitude = 0.0035f)
        {
            if (!IsAlive(tile))
                yield break;

            Vector3 rest = tile.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (!IsAlive(tile))
                    yield break;

                elapsed += Time.deltaTime;
                float u = elapsed / duration;
                float knock = Mathf.Exp(-u * 9f) * Mathf.Sin(u * 22f) * amplitude;
                tile.position = rest + Vector3.up * knock;
                yield return null;
            }

            if (!IsAlive(tile))
                yield break;

            tile.position = rest;
        }

        public static IEnumerator AnimateLandingSettle(Transform tile, float duration = 0.18f)
        {
            yield return AnimateCeramicKnock(tile, duration);
        }

        public static IEnumerator AnimateSlide(
            Transform tile,
            Vector3 start,
            Vector3 end,
            float duration)
        {
            if (!IsAlive(tile))
                yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (!IsAlive(tile))
                    yield break;

                elapsed += Time.deltaTime;
                float u = EaseOutCubic(Mathf.Clamp01(elapsed / duration));
                tile.position = Vector3.Lerp(start, end, u);
                yield return null;
            }

            if (!IsAlive(tile))
                yield break;

            tile.position = end;
        }

        public static IEnumerator AnimateParallel(IEnumerator first, IEnumerator second)
        {
            bool firstDone = first == null;
            bool secondDone = second == null;

            while (!firstDone || !secondDone)
            {
                if (!firstDone && !first.MoveNext())
                    firstDone = true;

                if (!secondDone && !second.MoveNext())
                    secondDone = true;

                yield return null;
            }
        }

        public static IEnumerator AnimateCaptureToPot(
            Transform tile,
            Vector3 start,
            Vector3 end,
            float duration,
            float arcHeight)
        {
            if (!IsAlive(tile))
                yield break;

            Quaternion startRotation = tile.rotation;
            Vector3 control = Vector3.Lerp(start, end, 0.5f) + Vector3.up * arcHeight;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (!IsAlive(tile))
                    yield break;

                elapsed += Time.deltaTime;
                float u = EaseInOutCubic(Mathf.Clamp01(elapsed / duration));
                tile.position = QuadraticBezier(start, control, end, u);
                tile.rotation = Quaternion.Slerp(startRotation, Quaternion.identity, EaseOutCubic(u));
                yield return null;
            }

            if (!IsAlive(tile))
                yield break;

            tile.position = end;
            tile.rotation = Quaternion.identity;
        }
    }
}
