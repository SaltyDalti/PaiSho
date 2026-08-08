using System.Collections.Generic;
using UnityEngine;
using PaiSho;
using PaiSho.Pieces;

namespace PaiSho.Game
{
    /// <summary>
    /// Board-life feedback on flower inlays via emission only (keeps engraved art readable).
    /// Whole-tile MPB only for ghost veil and victory flash.
    /// </summary>
    public class PieceStateAnimator : MonoBehaviour
    {
        public enum LifeState
        {
            Idle,
            Bud,
            Harmony,
            Wilt,
            HeavyWilt,
            Bloom,
            Ghost
        }

        private static readonly Color HarmonyGlow = new(0.55f, 0.92f, 0.62f, 1f);
        private static readonly Color HostSpringGlow = new(0.32f, 0.95f, 0.48f, 1f);
        private static readonly Color OpponentSpringGlow = new(1f, 0.48f, 0.72f, 1f);
        private static readonly Color BloomGlow = new(1f, 0.88f, 0.55f, 1f);
        private static readonly Color WiltDim = new(0.62f, 0.48f, 0.38f, 1f);
        private static readonly Color GhostGlow = new(0.62f, 0.78f, 0.98f, 1f);
        private static readonly Color SeasonGlow = new(0.78f, 0.92f, 1f, 1f);

        private Piece piece;
        private LifeState lifeState = LifeState.Idle;
        private float phase;
        private float oneShotTimer;
        private float oneShotStrength;
        private float seasonalBoostTimer;
        private OneShotKind activeOneShot = OneShotKind.None;
        private bool suspended;

        private readonly List<Renderer> flowerRenderers = new();
        private readonly List<MaterialPropertyBlock> flowerBlocks = new();
        private readonly List<Renderer> bodyRenderers = new();
        private readonly List<MaterialPropertyBlock> bodyBlocks = new();
        private readonly List<FlowerPose> flowerPoses = new();
        private readonly HashSet<Renderer> emissionReadyRenderers = new();

        private GameObject harmonyAuraRoot;
        private Transform harmonyAuraPulse;
        private Transform harmonyOuterRing;
        private Renderer harmonyAuraRenderer;
        private Renderer harmonyOuterRenderer;
        private Color harmonyAuraBaseColor;
        private float harmonyPhaseOffset;
        private float choreographyIntensity;
        private float choreographyVelocity;
        private Vector3 flowerRestCenter;
        private bool flowerRestsCaptured;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private enum OneShotKind
        {
            None,
            HarmonyEnter,
            HarmonyExit,
            Drain,
            Revive,
            Freeze,
            BloomOpen,
            Victory,
            MomentumSpark
        }

        private readonly struct FlowerPose
        {
            public readonly Transform Transform;
            public readonly Vector3 LocalPosition;
            public readonly Quaternion LocalRotation;

            public FlowerPose(Transform transform)
            {
                Transform = transform;
                LocalPosition = transform.localPosition;
                LocalRotation = transform.localRotation;
            }
        }

        public static PieceStateAnimator Ensure(Piece piece)
        {
            if (piece == null)
                return null;

            if (piece.BoardCoordinate < 0)
            {
                var existing = piece.GetComponent<PieceStateAnimator>();
                if (existing != null)
                    existing.enabled = false;
                return existing;
            }

            var animator = piece.GetComponent<PieceStateAnimator>();
            if (animator == null)
                animator = piece.gameObject.AddComponent<PieceStateAnimator>();
            animator.enabled = true;
            animator.Bind(piece);
            return animator;
        }

        public void Bind(Piece owner)
        {
            piece = owner;
            harmonyPhaseOffset = owner.BoardCoordinate >= 0
                ? (owner.BoardCoordinate * 0.173f) % (Mathf.PI * 2f)
                : Random.Range(0f, Mathf.PI * 2f);
            if (phase <= 0.001f)
                phase = harmonyPhaseOffset;
            CacheRenderers();
            SyncFromPiece(immediate: true);
        }

        public void Suspend()
        {
            suspended = true;
            // Keep emission — clearing it during place/move travel caused spring glow to flash off.
            ResetFlowerMotion();
        }

        public void Resume()
        {
            suspended = false;
            CacheRenderers();
            SyncFromPiece(immediate: true);
        }

        public void RefreshAfterBoardSeat()
        {
            ResetFlowerMotion();
            flowerRestsCaptured = false;
            CacheRenderers();
            SyncFromPiece(immediate: true);
        }

        public void NotifySeasonalBoost(float duration = 1.6f)
        {
            seasonalBoostTimer = Mathf.Max(seasonalBoostTimer, duration);
        }

        public void NotifyMomentumSpark() => PlayOneShot(OneShotKind.MomentumSpark, 0.55f, 1.1f);

        public void SyncFromPiece(bool immediate = false)
        {
            if (piece == null)
                piece = GetComponent<Piece>();
            if (piece == null)
                return;

            LifeState next = ResolveLifeState(piece);
            if (next == lifeState && !immediate)
                return;

            LifeState previous = lifeState;
            lifeState = next;

            if (!immediate)
            {
                if (previous != LifeState.Harmony && next == LifeState.Harmony)
                    PlayOneShot(OneShotKind.HarmonyEnter, 0.9f, 1.35f);
                else if (previous == LifeState.Harmony && next != LifeState.Harmony)
                    PlayOneShot(OneShotKind.HarmonyExit, 0.45f, 0.85f);
                else if (previous != LifeState.Bloom && next == LifeState.Bloom)
                    PlayOneShot(OneShotKind.BloomOpen, 0.75f, 1.2f);
                else if (!IsWilt(previous) && IsWilt(next))
                    PlayOneShot(OneShotKind.Drain, 0.45f, 1f);
                else if (previous == LifeState.Wilt && next == LifeState.HeavyWilt)
                    PlayOneShot(OneShotKind.Drain, 0.5f, 1.1f);
            }
        }

        public void NotifyHarmonyEntered()
        {
            if (flowerRenderers.Count == 0)
            {
                flowerRestsCaptured = false;
                CacheRenderers();
            }

            PlayOneShot(OneShotKind.HarmonyEnter, 0.9f, 1.35f);
        }

        public void NotifyHarmonyExited()
        {
            PlayOneShot(OneShotKind.HarmonyExit, 0.45f, 0.85f);
            // Kill sticky emission immediately — exit pulse alone was leaving HDR on the material.
            ClearFlowerMaterialEmission();
            choreographyIntensity = 0f;
            choreographyVelocity = 0f;
            ResetFlowerMotion();
        }

        /// <summary>Force harmony visuals if InHarmony but flower faces were missed on first cache.</summary>
        public void EnsureHarmonyPresentation()
        {
            if (piece == null || !piece.InHarmony)
                return;

            if (flowerRenderers.Count == 0)
            {
                flowerRestsCaptured = false;
                CacheRenderers();
            }

            if (lifeState != LifeState.Harmony)
                SyncFromPiece(immediate: true);
        }

        public void NotifyDrain() => PlayOneShot(OneShotKind.Drain, 0.4f, 1f);
        public void NotifyRevive() => PlayOneShot(OneShotKind.Revive, 0.5f, 1f);
        public void NotifyFreeze() => PlayOneShot(OneShotKind.Freeze, 0.45f, 0.9f);
        public void NotifyVictory() => PlayOneShot(OneShotKind.Victory, 0.9f, 1.25f);

        private void PlayOneShot(OneShotKind kind, float duration, float strength)
        {
            activeOneShot = kind;
            oneShotTimer = duration;
            oneShotStrength = strength;
        }

        private void CacheRenderers()
        {
            flowerRenderers.Clear();
            flowerBlocks.Clear();
            bodyRenderers.Clear();
            bodyBlocks.Clear();
            emissionReadyRenderers.Clear();

            WoodTheme.NormalizeFlowerInlayNames(gameObject);

            var candidates = new List<Renderer>();
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || renderer.sharedMaterial == null)
                    continue;

                string name = renderer.gameObject.name;
                if (name.Contains("TravelShadow") || name.Contains("HarmonyInlayAura") || name.Contains("HarmonyGlow"))
                    continue;

                if (WoodTheme.IsFlowerVisualRenderer(renderer))
                    candidates.Add(renderer);
                else if (!name.Contains("GlowDisc") && !name.Contains("HarmonyOuterGlow"))
                {
                    bodyRenderers.Add(renderer);
                    bodyBlocks.Add(new MaterialPropertyBlock());
                }
            }

            // Jasmine exports parent "Jasmine Face" + child "Jasmine Face_Mat0".
            // Animating both double-applies motion; keep leaf mesh inlays only.
            for (int i = 0; i < candidates.Count; i++)
            {
                Renderer candidate = candidates[i];
                if (candidate == null)
                    continue;

                bool hasFlowerDescendant = false;
                for (int j = 0; j < candidates.Count; j++)
                {
                    if (i == j || candidates[j] == null)
                        continue;
                    if (candidates[j].transform.IsChildOf(candidate.transform))
                    {
                        hasFlowerDescendant = true;
                        break;
                    }
                }

                if (hasFlowerDescendant)
                    continue;

                flowerRenderers.Add(candidate);
                flowerBlocks.Add(new MaterialPropertyBlock());
            }

            // Never recapture rest poses while the inlay is floating — that baked the lift in forever.
            if (!flowerRestsCaptured || flowerPoses.Count != flowerRenderers.Count || !FlowerPosesMatchRenderers())
            {
                ResetFlowerMotion();
                flowerPoses.Clear();
                for (int i = 0; i < flowerRenderers.Count; i++)
                    flowerPoses.Add(new FlowerPose(flowerRenderers[i].transform));
                flowerRestsCaptured = flowerPoses.Count > 0;
                flowerRestCenter = ComputeFlowerRestCenter();
            }
        }

        private bool FlowerPosesMatchRenderers()
        {
            if (flowerPoses.Count != flowerRenderers.Count)
                return false;

            for (int i = 0; i < flowerPoses.Count; i++)
            {
                if (flowerPoses[i].Transform != flowerRenderers[i].transform)
                    return false;
            }

            return true;
        }

        private Vector3 ComputeFlowerRestCenter()
        {
            if (flowerPoses.Count == 0)
                return Vector3.up * 0.012f;

            Vector3 center = Vector3.zero;
            for (int i = 0; i < flowerPoses.Count; i++)
                center += flowerPoses[i].LocalPosition;
            return center / flowerPoses.Count;
        }

        private void LateUpdate()
        {
            if (piece == null || suspended || !enabled)
                return;

            if (piece.BoardCoordinate < 0)
            {
                enabled = false;
                ResetFlowerMotion();
                ClearFlowerMaterialEmission();
                ClearPropertyBlocks();
                return;
            }

            phase += Time.deltaTime;
            if (oneShotTimer > 0f)
                oneShotTimer -= Time.deltaTime;
            if (seasonalBoostTimer > 0f)
                seasonalBoostTimer -= Time.deltaTime;

            ApplyVisual();

            if (oneShotTimer <= 0f)
                activeOneShot = OneShotKind.None;
        }

        private void ApplyVisual()
        {
            Color accent = WoodTheme.GetFlowerAccent(piece.Type);
            float animPhase = phase;
            float pulse = 0.5f + 0.5f * Mathf.Sin(animPhase * ResolveBreatheSpeed());

            // Motion lift/spin is harmony-only (and lotus bloom). Spring buds stay planted.
            float targetIntensity = lifeState switch
            {
                LifeState.Harmony => 1f,
                LifeState.Bloom => 0.78f,
                _ => 0f
            };

            if (activeOneShot == OneShotKind.HarmonyEnter)
            {
                float burst = Mathf.Clamp01(oneShotTimer / 0.9f) * oneShotStrength;
                targetIntensity = Mathf.Max(targetIntensity, burst);
            }
            else if (activeOneShot == OneShotKind.BloomOpen)
            {
                float burst = Mathf.Clamp01(oneShotTimer / 0.75f) * oneShotStrength;
                targetIntensity = Mathf.Max(targetIntensity, burst * 0.85f);
            }
            else if (activeOneShot == OneShotKind.HarmonyExit)
            {
                targetIntensity = 0f;
            }

            choreographyIntensity = Mathf.SmoothDamp(
                choreographyIntensity,
                targetIntensity,
                ref choreographyVelocity,
                targetIntensity > choreographyIntensity ? 0.12f : 0.18f);

            if (targetIntensity <= 0.001f && choreographyIntensity < 0.02f)
            {
                choreographyIntensity = 0f;
                choreographyVelocity = 0f;
            }

            Color harmonyTint = Color.Lerp(
                HarmonyGlow,
                WoodTheme.GetOwnerHarmonyColor(piece.Owner),
                lifeState == LifeState.Bloom ? 0.35f : 0.55f);

            // Spring buds: precise owner tint — host green / opponent pink — static, no pulse.
            Color flowerEmission = lifeState switch
            {
                LifeState.Harmony => harmonyTint * (1.35f + 0.75f * pulse),
                LifeState.Bloom => Color.Lerp(accent, BloomGlow, 0.45f) * (1.55f + 0.85f * pulse),
                LifeState.Bud => ResolveSpringGlow(piece.Owner) * 1.15f,
                LifeState.Ghost => GhostGlow * (0.75f + 0.4f * pulse),
                LifeState.Wilt => Color.Lerp(accent, WiltDim, 0.55f) * 0.35f,
                LifeState.HeavyWilt => WiltDim * 0.16f,
                _ => Color.black
            };

            if (seasonalBoostTimer > 0f)
                flowerEmission += SeasonGlow * (0.75f * Mathf.Clamp01(seasonalBoostTimer) * pulse);

            if (activeOneShot is OneShotKind.HarmonyEnter or OneShotKind.BloomOpen or OneShotKind.Revive or OneShotKind.MomentumSpark)
                flowerEmission += harmonyTint * (1.65f * Mathf.Clamp01(oneShotTimer) * oneShotStrength);
            if (activeOneShot == OneShotKind.Freeze)
                flowerEmission += GhostGlow * (0.55f * Mathf.Clamp01(oneShotTimer));
            if (activeOneShot is OneShotKind.Drain or OneShotKind.HarmonyExit)
                flowerEmission = Color.Lerp(flowerEmission, Color.black, 0.75f);

            ApplyFlowerEmission(flowerEmission);
            ApplyFlowerMotion(animPhase, pulse);
            ApplyHarmonyAura(animPhase, pulse, flowerEmission, harmonyTint);

            float bodyAmount = 0f;
            float bodyTintMix = 0f;
            Color bodyTint = GhostGlow;
            if (lifeState == LifeState.Ghost)
            {
                bodyAmount = 0.02f;
                bodyTintMix = 0.22f;
            }

            if (activeOneShot == OneShotKind.Victory)
            {
                bodyAmount += 0.06f * Mathf.Clamp01(oneShotTimer) * oneShotStrength;
                bodyTintMix = Mathf.Max(bodyTintMix, 0.12f * Mathf.Clamp01(oneShotTimer));
                bodyTint = accent;
            }

            bool bodyNeedsBlock = bodyAmount > 0.001f || bodyTintMix > 0.001f;
            ApplyBodyBlocks(bodyNeedsBlock, accent * bodyAmount, bodyTint, bodyTintMix);
        }

        private void ApplyFlowerEmission(Color emission)
        {
            if (emission.maxColorComponent <= 0.001f)
            {
                ClearFlowerMaterialEmission();
                return;
            }

            Color hdrEmission = emission * 3.2f;

            for (int i = 0; i < flowerRenderers.Count; i++)
            {
                Renderer renderer = flowerRenderers[i];
                if (renderer == null)
                    continue;

                Material shared = renderer.sharedMaterial;
                if (shared == null || !shared.HasProperty(EmissionColorId))
                {
                    renderer.SetPropertyBlock(null);
                    continue;
                }

                Material material = EnsureEmissionReady(renderer, shared);

                MaterialPropertyBlock block = flowerBlocks[i];
                block.Clear();
                block.SetColor(EmissionColorId, hdrEmission);
                renderer.SetPropertyBlock(block);

                if (material != null)
                    material.SetColor(EmissionColorId, hdrEmission);
            }
        }

        private void ClearFlowerMaterialEmission()
        {
            for (int i = 0; i < flowerRenderers.Count; i++)
            {
                Renderer renderer = flowerRenderers[i];
                if (renderer == null)
                    continue;

                renderer.SetPropertyBlock(null);

                // Prefer the instanced material created by EnsureEmissionReady.
                Material material = renderer.sharedMaterial;
                if (emissionReadyRenderers.Contains(renderer))
                    material = renderer.material;

                if (material == null || !material.HasProperty(EmissionColorId))
                    continue;

                material.SetColor(EmissionColorId, Color.black);
                material.DisableKeyword("_EMISSION");
            }
        }

        private Material EnsureEmissionReady(Renderer renderer, Material shared)
        {
            if (renderer == null)
                return null;

            Material material = renderer.material;
            if (material.HasProperty(EmissionColorId))
            {
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            emissionReadyRenderers.Add(renderer);
            return material;
        }

        private void ApplyFlowerMotion(float animPhase, float pulse)
        {
            if (choreographyIntensity <= 0.001f)
            {
                ResetFlowerMotion();
                return;
            }

            float intensity = choreographyIntensity;
            float footprint = Mathf.Max(transform.lossyScale.x, 0.35f);
            // High float so a tiny wobble never digs the inlay into ceramic.
            float lift = footprint * intensity * 0.24f;
            float bob = footprint * intensity * Mathf.Sin(animPhase * 3.1f) * 0.014f;
            float minClearance = footprint * intensity * 0.18f;
            float netLift = Mathf.Max(minClearance, lift + bob);
            float spin = animPhase * 26f * intensity;
            // Tiny wobble only — kept small relative to lift clearance.
            float wobble = 2.2f * intensity;
            float tiltX = Mathf.Sin(animPhase * 2.4f) * wobble;
            float tiltZ = Mathf.Cos(animPhase * 1.9f) * wobble;
            Quaternion magicalRot =
                Quaternion.AngleAxis(spin, Vector3.up) *
                Quaternion.Euler(tiltX, 0f, tiltZ);

            Vector3 offset = Vector3.up * netLift;

            for (int i = 0; i < flowerPoses.Count; i++)
            {
                FlowerPose pose = flowerPoses[i];
                if (pose.Transform == null)
                    continue;

                pose.Transform.localPosition = pose.LocalPosition + offset;
                pose.Transform.localRotation = pose.LocalRotation * magicalRot;
            }
        }

        private void ApplyHarmonyAura(float animPhase, float pulse, Color flowerEmission, Color harmonyTint)
        {
            // Aura rides with harmony motion only — never for spring buds.
            bool showAura = lifeState is LifeState.Harmony or LifeState.Bloom
                            && choreographyIntensity > 0.01f
                            && flowerEmission.maxColorComponent > 0.01f;
            if (!showAura)
            {
                if (harmonyAuraRoot != null)
                    harmonyAuraRoot.SetActive(false);
                return;
            }

            EnsureHarmonyAuraRoot(harmonyTint);
            harmonyAuraRoot.SetActive(true);

            float intensity = choreographyIntensity;
            float footprint = Mathf.Max(transform.lossyScale.x, 0.35f);
            float innerDiameter = footprint * (0.52f + 0.12f * pulse * intensity);
            float outerDiameter = footprint * (0.78f + 0.16f * pulse * intensity);
            float lift = footprint * intensity * 0.22f;
            float bob = footprint * intensity * Mathf.Sin(animPhase * 2.8f) * 0.012f;
            float minClearance = footprint * intensity * 0.16f;
            float netLift = Mathf.Max(minClearance, lift + bob);

            harmonyAuraRoot.transform.localPosition = flowerRestCenter + Vector3.up * netLift;
            harmonyAuraRoot.transform.localRotation = Quaternion.Euler(0f, animPhase * 18f * intensity, 0f);

            if (harmonyAuraPulse != null)
            {
                harmonyAuraPulse.localRotation = Quaternion.Euler(0f, -animPhase * 54f * intensity, 0f);
                harmonyAuraPulse.localScale = new Vector3(innerDiameter, 0.004f, innerDiameter);
            }

            if (harmonyOuterRing != null)
            {
                harmonyOuterRing.localRotation = Quaternion.Euler(0f, animPhase * 38f * intensity, 0f);
                harmonyOuterRing.localScale = new Vector3(outerDiameter, 0.003f, outerDiameter);
            }

            Color aura = Color.Lerp(harmonyAuraBaseColor, flowerEmission, 0.65f);
            aura.a = 0.55f + 0.28f * pulse * intensity;

            if (harmonyAuraRenderer != null)
            {
                harmonyAuraRenderer.material.color = aura;
                if (harmonyAuraRenderer.material.HasProperty(EmissionColorId))
                    harmonyAuraRenderer.material.SetColor(EmissionColorId, aura * 3.6f);
            }

            if (harmonyOuterRenderer != null)
            {
                Color outer = harmonyTint;
                outer.a = 0.28f + 0.22f * pulse * intensity;
                harmonyOuterRenderer.material.color = outer;
                if (harmonyOuterRenderer.material.HasProperty(EmissionColorId))
                    harmonyOuterRenderer.material.SetColor(EmissionColorId, outer * 2.8f);
            }
        }

        private void EnsureHarmonyAuraRoot(Color harmonyTint)
        {
            if (harmonyAuraRoot != null)
            {
                harmonyAuraBaseColor = new Color(harmonyTint.r, harmonyTint.g, harmonyTint.b, 0.62f);
                return;
            }

            harmonyAuraBaseColor = new Color(harmonyTint.r, harmonyTint.g, harmonyTint.b, 0.62f);

            harmonyAuraRoot = new GameObject("HarmonyInlayAura");
            harmonyAuraRoot.transform.SetParent(transform, false);

            var innerDisc = WoodTheme.CreateGlowDiscLocal(1f, harmonyAuraBaseColor, 0f);
            harmonyAuraPulse = innerDisc.transform;
            harmonyAuraPulse.SetParent(harmonyAuraRoot.transform, false);

            var outerDisc = WoodTheme.CreateGlowDiscLocal(1f, new Color(harmonyTint.r, harmonyTint.g, harmonyTint.b, 0.35f), 0.001f);
            harmonyOuterRing = outerDisc.transform;
            harmonyOuterRing.SetParent(harmonyAuraRoot.transform, false);

            var halo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            halo.name = "HarmonyGlow";
            halo.transform.SetParent(harmonyAuraRoot.transform, false);
            halo.transform.localPosition = Vector3.up * 0.008f;
            halo.transform.localScale = new Vector3(0.14f, 0.04f, 0.14f);
            Object.Destroy(halo.GetComponent<Collider>());
            harmonyAuraRenderer = halo.GetComponent<Renderer>();
            WoodTheme.ApplyEmissiveColorPublic(harmonyAuraRenderer, harmonyAuraBaseColor, 2.8f);

            var outerHalo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            outerHalo.name = "HarmonyOuterGlow";
            outerHalo.transform.SetParent(harmonyAuraRoot.transform, false);
            outerHalo.transform.localPosition = Vector3.up * 0.004f;
            outerHalo.transform.localScale = new Vector3(0.22f, 0.018f, 0.22f);
            Object.Destroy(outerHalo.GetComponent<Collider>());
            harmonyOuterRenderer = outerHalo.GetComponent<Renderer>();
            WoodTheme.ApplyEmissiveColorPublic(harmonyOuterRenderer, harmonyAuraBaseColor, 1.8f);
        }

        private void ResetFlowerMotion()
        {
            for (int i = 0; i < flowerPoses.Count; i++)
            {
                FlowerPose pose = flowerPoses[i];
                if (pose.Transform == null)
                    continue;
                pose.Transform.localPosition = pose.LocalPosition;
                pose.Transform.localRotation = pose.LocalRotation;
            }

            if (harmonyAuraRoot != null)
                harmonyAuraRoot.SetActive(false);
        }

        private void ApplyBodyBlocks(bool needsBlock, Color emission, Color tintTarget, float tintMix)
        {
            if (!needsBlock)
            {
                ClearList(bodyRenderers);
                return;
            }

            for (int i = 0; i < bodyRenderers.Count; i++)
            {
                Renderer renderer = bodyRenderers[i];
                if (renderer == null)
                    continue;

                Material shared = renderer.sharedMaterial;
                if (shared == null)
                {
                    renderer.SetPropertyBlock(null);
                    continue;
                }

                MaterialPropertyBlock block = bodyBlocks[i];
                block.Clear();

                if (tintMix > 0.001f)
                    ApplyBaseTint(block, shared, tintTarget, tintMix);

                if (emission.maxColorComponent > 0.001f && shared.HasProperty(EmissionColorId))
                    block.SetColor(EmissionColorId, emission);

                renderer.SetPropertyBlock(block);
            }
        }

        private static void ApplyBaseTint(MaterialPropertyBlock block, Material shared, Color tintTarget, float mix)
        {
            if (shared.HasProperty(BaseColorId))
            {
                Color baseColor = shared.GetColor(BaseColorId);
                block.SetColor(BaseColorId, Color.Lerp(baseColor, tintTarget, mix));
                return;
            }

            if (shared.HasProperty(ColorId))
            {
                Color baseColor = shared.GetColor(ColorId);
                block.SetColor(ColorId, Color.Lerp(baseColor, tintTarget, mix));
            }
        }

        private float ResolveBreatheSpeed()
        {
            return lifeState switch
            {
                LifeState.Harmony => 2.2f,
                LifeState.Bloom => 2.8f,
                LifeState.Bud => 1.6f,
                LifeState.Ghost => 3.5f,
                LifeState.Wilt => 0.9f,
                LifeState.HeavyWilt => 0.65f,
                _ => 1.4f
            };
        }

        private void ClearPropertyBlocks()
        {
            ClearList(flowerRenderers);
            ClearList(bodyRenderers);
        }

        private static void ClearList(List<Renderer> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null)
                    continue;
                list[i].SetPropertyBlock(null);
            }
        }

        private void OnDisable()
        {
            ResetFlowerMotion();
            ClearFlowerMaterialEmission();
            ClearPropertyBlocks();
        }

        private static bool IsWilt(LifeState state) =>
            state is LifeState.Wilt or LifeState.HeavyWilt;

        private static LifeState ResolveLifeState(Piece piece)
        {
            if (piece.IsGhost)
                return LifeState.Ghost;
            if (piece.Type == PieceType.Lotus && piece.IsBlooming())
                return LifeState.Bloom;

            // Spring glow until first move or after 3 completed play turns — whichever comes first.
            // Must beat wilt: placement-phase aging used to extinguish buds mid-opening.
            bool springGlowActive =
                piece.IsFlower() &&
                !piece.HasMovedSincePlaced &&
                (GameManager.Instance == null || !GameManager.Instance.IsSpringGlowEnded());
            if (springGlowActive)
                return LifeState.Bud;

            if (piece.WiltLevel >= 2)
                return LifeState.HeavyWilt;
            if (piece.WiltLevel == 1)
                return LifeState.Wilt;
            if (piece.InHarmony)
                return LifeState.Harmony;

            return LifeState.Idle;
        }

        private static Color ResolveSpringGlow(Player owner) =>
            owner == Player.Host ? HostSpringGlow : OpponentSpringGlow;

        /// <summary>Tray spring-draw: host green / opponent pink, static.</summary>
        public static void ApplySpringBudLook(GameObject visual, PieceType type, Player owner = Player.Host)
        {
            if (visual == null)
                return;

            Color emission = ResolveSpringGlow(owner) * 1.15f;

            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !WoodTheme.IsFlowerVisualRenderer(renderer))
                    continue;

                Material shared = renderer.sharedMaterial;
                if (shared == null || !shared.HasProperty(EmissionColorId))
                    continue;

                Material material = renderer.material;
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

                var block = new MaterialPropertyBlock();
                block.SetColor(EmissionColorId, emission * 3.2f);
                renderer.SetPropertyBlock(block);
                material.SetColor(EmissionColorId, emission * 3.2f);
            }
        }
    }
}
