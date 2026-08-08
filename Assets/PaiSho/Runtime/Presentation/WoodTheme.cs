using UnityEngine;
using PaiSho.Board;
using PaiSho.Game;
using PaiSho.Pieces;

namespace PaiSho
{
    public static class WoodTheme
    {
        // Board surface — hinoki tea-house tones
        public static readonly Color TableWood = Hex("#3A2A1C");
        public static readonly Color FrameWood = Hex("#241810");
        public static readonly Color LightGardenWood = Hex("#E4CF9A");
        public static readonly Color DarkGardenWood = Hex("#5C3A22");
        public static readonly Color NeutralPathWood = Hex("#D8C08A");
        public static readonly Color PortWood = Hex("#4A3020");
        public static readonly Color GateWood = Hex("#362218");
        public static readonly Color GridLineWood = Hex("#1A120C");
        public static readonly Color BoardBaseWood = Hex("#C4A060");

        // Tile body — glazed ceramic (host) and terracotta (opponent)
        public static readonly Color TileWoodBase = Hex("#E8E2D8");
        public static readonly Color HostCeramic = Hex("#F4F0EA");
        public static readonly Color OpponentTerracotta = Hex("#9E4A38");
        public static readonly Color HostWood = HostCeramic;
        public static readonly Color OpponentWood = OpponentTerracotta;

        /// <summary>Resting clearance so ceramic tiles sit on the wood rather than clipping into it.</summary>
        public const float BoardSurfaceRestClearance = 0.012f;

        public static Material CreateWoodMaterial(Color color, float smoothness = 0.45f)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                         ?? Shader.Find("Standard");

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else
                material.color = color;

            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 0.05f);

            return material;
        }

        public static Material CreateCeramicMaterial(Color color, float smoothness = 0.78f, float metallic = 0.02f)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                         ?? Shader.Find("Standard");

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else
                material.color = color;

            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);

            return material;
        }

        public static Material CreateTerracottaMaterial(Color color, float smoothness = 0.62f)
        {
            return CreateCeramicMaterial(color, smoothness, 0.04f);
        }

        public static void ApplyWood(Renderer renderer, Color color, float smoothness = 0.45f)
        {
            if (renderer == null)
                return;
            renderer.material = CreateWoodMaterial(color, smoothness);
        }

        public static void ApplyTexturedWood(
            Renderer renderer,
            string textureResourcePath,
            Color tint,
            float smoothness = 0.45f,
            float tiling = 2f)
        {
            if (renderer == null)
                return;
            renderer.material = CreateTexturedMaterial(textureResourcePath, tint, smoothness, tiling);
        }

        public static void ApplyTiledSurface(
            Renderer renderer,
            string textureResourcePath,
            float smoothness = 0.3f,
            float tiling = 4f)
        {
            if (renderer == null)
                return;
            renderer.material = CreateTexturedMaterial(textureResourcePath, Color.white, smoothness, tiling);
        }

        public static Material CreateTexturedMaterial(
            string textureResourcePath,
            Color tint,
            float smoothness = 0.45f,
            float tiling = 2f)
        {
            Texture2D texture = Resources.Load<Texture2D>(textureResourcePath);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                         ?? Shader.Find("Standard");

            var material = new Material(shader);
            Color baseColor = tint;

            if (texture != null)
            {
                material.mainTexture = texture;
                if (material.HasProperty("_BaseMap"))
                    material.SetTexture("_BaseMap", texture);

                Vector2 scale = new Vector2(tiling, tiling);
                material.mainTextureScale = scale;
                if (material.HasProperty("_BaseMap"))
                    material.SetTextureScale("_BaseMap", scale);
            }

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", baseColor);
            else
                material.color = baseColor;

            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 0.04f);

            return material;
        }

        public static Color GetGardenColor(GardenType garden)
        {
            return garden switch
            {
                GardenType.LightGarden => LightGardenWood,
                GardenType.DarkGarden => DarkGardenWood,
                GardenType.Port => PortWood,
                GardenType.NeutralGarden => NeutralPathWood,
                _ => NeutralPathWood
            };
        }

        public static Color GetSurfaceColor(int coordinate)
        {
            if (BoardUtils.IsGate(coordinate))
                return DarkGardenWood;

            GardenType garden = BoardUtils.GetGardenType(coordinate);
            return garden switch
            {
                GardenType.LightGarden => LightGardenWood,
                GardenType.DarkGarden => DarkGardenWood,
                GardenType.Port => DarkGardenWood,
                GardenType.NeutralGarden => NeutralPathWood,
                _ => NeutralPathWood
            };
        }

        public static GameObject CreateBoardSurface(BoardLayout layout)
        {
            var surfaceRoot = new GameObject("BoardSurface");
            if (layout == null)
                return surfaceRoot;

            if (layout.UsePhotoBoard && BoardTextureLoader.IsAvailable())
                return surfaceRoot;

            if (layout.UseModelBoard && BoardVisualLoader.IsModelAvailable())
                return surfaceRoot;

            float patchSize = layout.CellSpacing * 1.02f;
            float patchY = layout.TileHeight - 0.02f;
            var gardensRoot = new GameObject("GardenPatches").transform;
            gardensRoot.SetParent(surfaceRoot.transform, false);

            for (int i = 0; i < BoardUtils.NumPoints; i++)
            {
                if (!BoardUtils.IsValidPointCoordinate(i))
                    continue;

                Vector3 position = layout.CoordinateToWorld(i);
                position.y = patchY;
                CreateGardenPatch(gardensRoot, position, patchSize, GetSurfaceColor(i));
            }

            CreateCardinalGrid(surfaceRoot.transform, layout);
            return surfaceRoot;
        }

        public static GameObject CreateMoveGemMarker(BoardLayout layout, int coordinate, bool isCapture, bool isMomentum = false)
        {
            var root = new GameObject(isCapture ? "CaptureMarker" : isMomentum ? "MomentumMarker" : "MoveMarker");
            if (layout == null)
                return root;

            float dotSize = layout.CellSpacing * 0.13f;
            float ringSize = layout.CellSpacing * 0.24f;
            Vector3 world = layout.GetSurfaceWorldPosition(coordinate, 0.04f);
            root.transform.position = world;

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "LacquerRing";
            ring.transform.SetParent(root.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.002f, 0f);
            ring.transform.localScale = new Vector3(ringSize, 0.004f, ringSize);
            DestroyCollider(ring);
            ApplyGemColor(ring.GetComponent<Renderer>(), JapaneseTheme.SumiInk, 0.7f, 0.2f);

            // Colorblind Assist: capture/momentum markers get a distinct silhouette, not just a different hue.
            bool shapeVariants = GameSession.ColorblindAssist;
            PrimitiveType dotShape = shapeVariants && isCapture
                ? PrimitiveType.Cube
                : shapeVariants && isMomentum
                    ? PrimitiveType.Capsule
                    : PrimitiveType.Sphere;

            var dot = GameObject.CreatePrimitive(dotShape);
            dot.name = "LacquerDot";
            dot.transform.SetParent(root.transform, false);
            dot.transform.localPosition = new Vector3(0f, dotSize * 0.35f, 0f);
            dot.transform.localRotation = dotShape == PrimitiveType.Cube ? Quaternion.Euler(0f, 45f, 0f) : Quaternion.identity;
            dot.transform.localScale = new Vector3(dotSize, dotSize * 0.55f, dotSize);
            DestroyCollider(dot);

            Color dotColor = isMomentum
                ? JapaneseTheme.MomentumMarker
                : isCapture
                    ? JapaneseTheme.CaptureMarker
                    : JapaneseTheme.MoveMarker;
            ApplyGemColor(dot.GetComponent<Renderer>(), dotColor, 0.92f, isCapture ? 1.15f : 0.85f);

            var highlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            highlight.name = "LacquerHighlight";
            highlight.transform.SetParent(dot.transform, false);
            highlight.transform.localPosition = new Vector3(dotSize * 0.12f, dotSize * 0.18f, -dotSize * 0.08f);
            highlight.transform.localScale = Vector3.one * 0.22f;
            DestroyCollider(highlight);
            ApplyGemColor(highlight.GetComponent<Renderer>(), new Color(1f, 1f, 1f, 0.55f), 1f, 0.6f);

            var shadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shadow.name = "MarkerShadow";
            shadow.transform.SetParent(root.transform, false);
            shadow.transform.localPosition = new Vector3(0f, 0.001f, 0f);
            shadow.transform.localScale = new Vector3(ringSize * 1.1f, 0.003f, ringSize * 1.1f);
            DestroyCollider(shadow);
            ApplyEmissiveColor(shadow.GetComponent<Renderer>(), new Color(0.04f, 0.03f, 0.05f, 0.28f), 0.1f);

            var animator = root.AddComponent<PaiSho.Game.OverlayAnimator>();
            animator.Configure(PaiSho.Game.OverlayAnimator.Style.GemPulse, dot.transform, 2.6f + coordinate * 0.0003f, 0.1f);

            var reveal = root.AddComponent<PaiSho.Game.SpawnRevealAnimator>();
            reveal.Configure(root.transform, (coordinate % 7) * 0.018f, 0.3f);

            return root;
        }

        /// <summary>
        /// Distinct from move/capture gems — a diamond drop marker for Boat unload spaces
        /// (GameInputController lists these separately from ordinary legal moves).
        /// </summary>
        public static GameObject CreateUnloadMarker(BoardLayout layout, int coordinate)
        {
            var root = new GameObject("UnloadMarker");
            if (layout == null)
                return root;

            float ringSize = layout.CellSpacing * 0.24f;
            float diamondSize = layout.CellSpacing * 0.15f;
            Vector3 world = layout.GetSurfaceWorldPosition(coordinate, 0.04f);
            root.transform.position = world;

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "UnloadRing";
            ring.transform.SetParent(root.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.002f, 0f);
            ring.transform.localScale = new Vector3(ringSize, 0.004f, ringSize);
            DestroyCollider(ring);
            ApplyGemColor(ring.GetComponent<Renderer>(), JapaneseTheme.SumiInk, 0.7f, 0.2f);

            var diamond = GameObject.CreatePrimitive(PrimitiveType.Cube);
            diamond.name = "UnloadDiamond";
            diamond.transform.SetParent(root.transform, false);
            diamond.transform.localPosition = new Vector3(0f, diamondSize * 0.5f, 0f);
            diamond.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
            diamond.transform.localScale = Vector3.one * diamondSize;
            DestroyCollider(diamond);
            ApplyGemColor(diamond.GetComponent<Renderer>(), JapaneseTheme.UnloadMarker, 0.92f, 0.85f);

            var animator = root.AddComponent<PaiSho.Game.OverlayAnimator>();
            animator.Configure(PaiSho.Game.OverlayAnimator.Style.GemPulse, diamond.transform, 2.4f + coordinate * 0.0003f, 0.1f);

            var reveal = root.AddComponent<PaiSho.Game.SpawnRevealAnimator>();
            reveal.Configure(root.transform, (coordinate % 7) * 0.018f, 0.3f);

            return root;
        }

        public static GameObject CreateBlockedMoveMarker(BoardLayout layout, int coordinate, bool isDisharmony)
        {
            var root = new GameObject(isDisharmony ? "DisharmonyBlocked" : "GardenBlocked");
            if (layout == null)
                return root;

            float discDiameter = layout.CellSpacing * 0.28f;
            Vector3 world = layout.GetSurfaceWorldPosition(coordinate, 0.035f);
            root.transform.position = world;

            Color fill = isDisharmony
                ? JapaneseTheme.DisharmonyMarker
                : JapaneseTheme.GardenBlockedMarker;

            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "BlockedDisc";
            disc.transform.SetParent(root.transform, false);
            disc.transform.localPosition = Vector3.zero;
            disc.transform.localScale = new Vector3(discDiameter, 0.005f, discDiameter);
            DestroyCollider(disc);
            ApplyEmissiveColor(disc.GetComponent<Renderer>(), fill, isDisharmony ? 1.2f : 0.9f);

            float barLength = discDiameter * 0.72f;
            float barWidth = discDiameter * 0.1f;
            Color barColor = isDisharmony
                ? new Color(0.95f, 0.55f, 0.65f, 0.9f)
                : new Color(0.95f, 0.82f, 0.42f, 0.88f);

            // Colorblind Assist: garden-blocked reads as a single strike, disharmony keeps the full X.
            int barCount = !isDisharmony && GameSession.ColorblindAssist ? 1 : 2;
            for (int i = 0; i < barCount; i++)
            {
                var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bar.name = i == 0 ? "BlockedBarA" : "BlockedBarB";
                bar.transform.SetParent(root.transform, false);
                bar.transform.localPosition = new Vector3(0f, 0.008f, 0f);
                bar.transform.localRotation = Quaternion.Euler(0f, i * 90f, 45f);
                bar.transform.localScale = new Vector3(barWidth, 0.006f, barLength);
                DestroyCollider(bar);
                ApplyEmissiveColor(bar.GetComponent<Renderer>(), barColor, 1.1f);
            }

            return root;
        }

        public static GameObject CreatePortMarker(BoardLayout layout, int coordinate, bool isLegalEntry)
        {
            var root = new GameObject(isLegalEntry ? "PortEntry" : "PortInactive");
            if (layout == null)
                return root;

            float size = layout.CellSpacing * 0.38f;
            Vector3 world = layout.GetSurfaceWorldPosition(coordinate, 0.03f);
            root.transform.position = world;

            Color glow = isLegalEntry ? JapaneseTheme.PortGlow : new Color(0.5f, 0.45f, 0.38f, 0.35f);
            var disc = CreateGlowDisc(world, size, glow);
            disc.transform.SetParent(root.transform, false);
            disc.transform.localPosition = Vector3.zero;

            if (isLegalEntry)
            {
                float postW = layout.CellSpacing * 0.04f;
                float postH = layout.CellSpacing * 0.14f;
                for (int i = -1; i <= 1; i += 2)
                {
                    var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    post.name = "ToriiPost";
                    post.transform.SetParent(root.transform, false);
                    post.transform.localPosition = new Vector3(i * size * 0.32f, postH * 0.5f, 0f);
                    post.transform.localScale = new Vector3(postW, postH, postW);
                    DestroyCollider(post);
                    ApplyEmissiveColor(post.GetComponent<Renderer>(), JapaneseTheme.Vermillion, 0.9f);
                }

                var lintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lintel.name = "ToriiLintel";
                lintel.transform.SetParent(root.transform, false);
                lintel.transform.localPosition = new Vector3(0f, postH * 0.92f, 0f);
                lintel.transform.localScale = new Vector3(size * 0.72f, postW * 0.8f, postW);
                DestroyCollider(lintel);
                ApplyEmissiveColor(lintel.GetComponent<Renderer>(), JapaneseTheme.Vermillion, 0.85f);
            }

            var animator = root.AddComponent<PaiSho.Game.OverlayAnimator>();
            animator.Configure(
                PaiSho.Game.OverlayAnimator.Style.DiscFade,
                disc.transform,
                isLegalEntry ? 2.4f : 1.6f,
                0.25f);

            return root;
        }

        public static GameObject CreatePathBeam(Vector3 from, Vector3 to, Color color, float width)
        {
            var beam = CreateHarmonyBeam(from, to, color, width);
            if (beam != null)
                beam.name = "PathBeam";
            return beam;
        }

        public static GameObject CreateWheelArrow(BoardLayout layout, int coordinate, float yawDegrees)
        {
            var root = new GameObject("WheelArrow");
            if (layout == null)
                return root;

            float size = layout.CellSpacing * 0.14f;
            root.transform.position = layout.GetSurfaceWorldPosition(coordinate, 0.06f);
            root.transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);

            var shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shaft.name = "ArrowShaft";
            shaft.transform.SetParent(root.transform, false);
            shaft.transform.localPosition = new Vector3(0f, 0f, size * 0.15f);
            shaft.transform.localScale = new Vector3(size * 0.18f, 0.006f, size * 0.55f);
            DestroyCollider(shaft);
            ApplyEmissiveColor(shaft.GetComponent<Renderer>(), JapaneseTheme.WheelArrow, 1f);

            var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "ArrowHead";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 0f, size * 0.48f);
            head.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            head.transform.localScale = new Vector3(size * 0.28f, 0.008f, size * 0.28f);
            DestroyCollider(head);
            ApplyEmissiveColor(head.GetComponent<Renderer>(), JapaneseTheme.GoldLeaf, 1.1f);

            var animator = root.AddComponent<PaiSho.Game.OverlayAnimator>();
            animator.Configure(PaiSho.Game.OverlayAnimator.Style.ArrowBob, root.transform, 2.2f, 0.12f);

            return root;
        }

        public static GameObject CreateDisharmonyRaySegment(Vector3 from, Vector3 to, Color color, float width)
        {
            var beam = CreateHarmonyBeam(from, to, color, width);
            if (beam != null)
                beam.name = "DisharmonyRay";
            return beam;
        }

        private static void ApplyGemColor(Renderer renderer, Color color, float smoothness, float emission)
        {
            if (renderer == null)
                return;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                         ?? Shader.Find("Standard");

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else
                material.color = color;

            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);

            Color emissive = color * emission;
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emissive);
            }

            renderer.material = material;
            SetRenderQueue(renderer, 3100);
        }

        private static void CreateGardenPatch(Transform parent, Vector3 position, float size, Color color)
        {
            var patch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            patch.name = "GardenPatch";
            patch.transform.SetParent(parent, false);
            patch.transform.position = position;
            patch.transform.localScale = new Vector3(size, 0.008f, size);
            DestroyCollider(patch);
            ApplyWood(patch.GetComponent<Renderer>(), color, 0.4f);
        }

        private static void CreateCardinalGrid(Transform parent, BoardLayout layout)
        {
            var gridRoot = new GameObject("GridLines").transform;
            gridRoot.SetParent(parent, false);

            float lineWidth = layout.CellSpacing * 0.03f;
            float lineY = layout.TileHeight - 0.006f;
            int[] cardinalOffsets = { -BoardUtils.GridSize, BoardUtils.GridSize, -1, 1 };

            for (int i = 0; i < BoardUtils.NumPoints; i++)
            {
                if (!BoardUtils.IsValidPointCoordinate(i))
                    continue;

                Vector3 from = layout.CoordinateToWorld(i);
                from.y = lineY;

                foreach (int offset in cardinalOffsets)
                {
                    int neighbor = i + offset;
                    if (neighbor <= i || !BoardUtils.IsValidPointCoordinate(neighbor))
                        continue;

                    Vector3 to = layout.CoordinateToWorld(neighbor);
                    to.y = lineY;
                    CreateGridLine(gridRoot, from, to, lineWidth);
                }
            }
        }

        private static void SetRenderQueue(Renderer renderer, int queue)
        {
            if (renderer != null && renderer.material != null)
                renderer.material.renderQueue = queue;
        }

        public static GameObject CreateSelectionRing(BoardLayout layout, int coordinate, Color color)
        {
            var root = new GameObject("SelectionRing");
            if (layout == null)
                return root;

            float size = layout.CellSpacing * 0.56f;
            float arm = size * 0.22f;
            float thick = layout.CellSpacing * 0.018f;
            float lift = 0.022f;
            Vector3 center = layout.GetSurfaceWorldPosition(coordinate, lift);
            root.transform.position = center;

            Color accent = Color.Lerp(color, JapaneseTheme.GoldLeaf, 0.35f);
            float half = size * 0.5f;
            Vector3[] corners =
            {
                new Vector3(-half, 0f, -half),
                new Vector3(half, 0f, -half),
                new Vector3(-half, 0f, half),
                new Vector3(half, 0f, half)
            };

            for (int i = 0; i < 4; i++)
            {
                Vector3 corner = corners[i];
                float sx = corner.x > 0f ? 1f : -1f;
                float sz = corner.z > 0f ? 1f : -1f;

                var barX = GameObject.CreatePrimitive(PrimitiveType.Cube);
                barX.name = $"RingBarX{i}";
                barX.transform.SetParent(root.transform, false);
                barX.transform.localPosition = corner + new Vector3(sx * arm * 0.5f, 0f, 0f);
                barX.transform.localScale = new Vector3(arm, thick, thick * 0.7f);
                DestroyCollider(barX);
                ApplyEmissiveColor(barX.GetComponent<Renderer>(), accent, 1.05f);

                var barZ = GameObject.CreatePrimitive(PrimitiveType.Cube);
                barZ.name = $"RingBarZ{i}";
                barZ.transform.SetParent(root.transform, false);
                barZ.transform.localPosition = corner + new Vector3(0f, 0f, sz * arm * 0.5f);
                barZ.transform.localScale = new Vector3(thick * 0.7f, thick, arm);
                DestroyCollider(barZ);
                ApplyEmissiveColor(barZ.GetComponent<Renderer>(), accent, 1.05f);
            }

            var animator = root.AddComponent<PaiSho.Game.OverlayAnimator>();
            animator.Configure(PaiSho.Game.OverlayAnimator.Style.RingBreathe, root.transform, 2.2f, 0.08f);

            return root;
        }

        public static GameObject CreateGlowDisc(Vector3 worldPosition, float diameter, Color color)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "GlowDisc";
            disc.transform.position = worldPosition;
            disc.transform.localScale = new Vector3(diameter, 0.004f, diameter);
            DestroyCollider(disc);
            ApplyEmissiveColor(disc.GetComponent<Renderer>(), color, 0.85f);
            return disc;
        }

        public static GameObject CreateGlowDiscLocal(float diameter, Color color, float localY = 0.02f)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "GlowDisc";
            disc.transform.localPosition = new Vector3(0f, localY, 0f);
            disc.transform.localScale = new Vector3(diameter, 0.004f, diameter);
            DestroyCollider(disc);
            ApplyEmissiveColor(disc.GetComponent<Renderer>(), color, 0.85f);
            return disc;
        }

        public static GameObject CreateHarmonyBeam(Vector3 from, Vector3 to, Color color, float width)
        {
            Vector3 delta = to - from;
            delta.y = 0f;
            float length = delta.magnitude;
            if (length < 0.001f)
                return null;

            var beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.name = "HarmonyBeam";
            beam.transform.position = (from + to) * 0.5f;
            beam.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            beam.transform.localScale = new Vector3(width, 0.008f, length);
            DestroyCollider(beam);
            ApplyEmissiveColor(beam.GetComponent<Renderer>(), color, 1.1f);
            SetRenderQueue(beam.GetComponent<Renderer>(), 2990);
            return beam;
        }

        public static GameObject CreateHarmonyBeamWithBead(Vector3 from, Vector3 to, Color color, float width)
        {
            var root = new GameObject("HarmonyBeamGroup");
            var beam = CreateHarmonyBeam(from, to, color, width);
            if (beam != null)
                beam.transform.SetParent(root.transform, true);

            Vector3 mid = Vector3.Lerp(from, to, 0.5f);
            mid.y = Mathf.Lerp(from.y, to.y, 0.5f) + 0.012f;
            float beadSize = width * 1.6f;
            var bead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bead.name = "HarmonyBead";
            bead.transform.SetParent(root.transform, false);
            bead.transform.position = mid;
            bead.transform.localScale = Vector3.one * beadSize;
            DestroyCollider(bead);
            ApplyEmissiveColor(bead.GetComponent<Renderer>(), Color.Lerp(color, JapaneseTheme.GoldLeaf, 0.45f), 1.25f);
            SetRenderQueue(bead.GetComponent<Renderer>(), 2995);

            var pulse = root.AddComponent<PaiSho.Game.OverlayAnimator>();
            pulse.Configure(PaiSho.Game.OverlayAnimator.Style.GemPulse, bead.transform, 1.8f, 0.08f);

            return root;
        }

        public static Color GetOwnerHarmonyColor(Player owner)
        {
            return owner == Player.Host ? JapaneseTheme.HostHarmonyAura : JapaneseTheme.OpponentHarmonyAura;
        }

        public static Color GetOwnerHarmonyLineColor(Player owner)
        {
            return owner == Player.Host ? JapaneseTheme.HostHarmonyLine : JapaneseTheme.OpponentHarmonyLine;
        }

        public static Material CreateMarkerMaterial(Color color, bool gameplay)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                         ?? Shader.Find("Standard");

            var material = new Material(shader);
            ConfigureTransparentMaterial(material, color, gameplay ? 0.9f : 1.2f);
            material.renderQueue = 3100;
            return material;
        }

        public static void UpdateMarkerMaterial(Material material, Color color, float emission)
        {
            if (material == null)
                return;

            ConfigureTransparentMaterial(material, color, emission);
        }

        public static void EnableTransparentSurface(Renderer renderer)
        {
            if (renderer == null || renderer.material == null)
                return;

            Material material = renderer.material;
            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend"))
                material.SetFloat("_Blend", 0f);
            material.renderQueue = 3100;
        }

        private static void ConfigureTransparentMaterial(Material material, Color color, float emission)
        {
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else
                material.color = color;

            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend"))
                material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend"))
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend"))
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3100;

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emission);
            }
        }

        private static void ApplyEmissiveColor(Renderer renderer, Color color, float emission)
        {
            ApplyEmissiveColorPublic(renderer, color, emission);
        }

        public static void ApplyEmissiveColorPublic(Renderer renderer, Color color, float emission)
        {
            if (renderer == null)
                return;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                         ?? Shader.Find("Standard");

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else
                material.color = color;

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emission);
            }

            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);

            renderer.material = material;
            SetRenderQueue(renderer, 2985);
        }

        public static Color GetFlowerAccent(PieceType type)
        {
            return type switch
            {
                PieceType.Jasmine => Hex("#FFEE44"),
                PieceType.Rose => Hex("#FF2244"),
                PieceType.Lily => Hex("#FFF4D8"),
                PieceType.Jade => Hex("#2EE86A"),
                PieceType.Chrysanthemum => Hex("#FF8800"),
                PieceType.Rhododendron => Hex("#FF40C0"),
                PieceType.Boat => Hex("#E09040"),
                PieceType.Rock => Hex("#B8B0A8"),
                PieceType.Knotweed => Hex("#28C858"),
                PieceType.Wheel => Hex("#D8D8FF"),
                PieceType.Lotus => Hex("#FF66FF"),
                PieceType.Orchid => Hex("#AA66FF"),
                _ => Hex("#FFE8B0")
            };
        }

        public static Color GetTileBodyColor(PieceType type)
        {
            return type switch
            {
                PieceType.Rock => Hex("#9A9088"),
                PieceType.Wheel => Hex("#C9A574"),
                PieceType.Boat => Hex("#B88450"),
                PieceType.Knotweed => Hex("#8A9A72"),
                _ => TileWoodBase
            };
        }

        public static void ApplyTileBodyColor(Renderer renderer, Color color, float smoothness = 0.38f)
        {
            if (renderer == null)
                return;
            renderer.material = CreateCeramicMaterial(color, smoothness, 0.02f);
        }

        public static void ApplyCeramicBody(Renderer renderer, Player owner, PieceType type)
        {
            if (renderer == null)
                return;

            Color color = GetOwnerBodyColor(owner, type);
            float smoothness = GetOwnerBodySmoothness(owner);
            float metallic = owner == Player.Host ? 0.03f : 0.05f;
            renderer.material = CreateCeramicMaterial(color, smoothness, metallic);
        }

        public static void ApplyTexturedTileBody(
            Renderer renderer,
            Material sourceTemplate,
            Player owner,
            PieceType type,
            float smoothness = 0.38f,
            bool distinctTemplates = false)
        {
            if (renderer == null || sourceTemplate == null)
                return;

            var material = new Material(sourceTemplate);
            Color ownerTint = GetOwnerTextureTint(owner, type, distinctTemplates);

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", ownerTint);
            else
                material.color = ownerTint;

            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", owner == Player.Host ? 0.02f : 0.06f);

            renderer.material = material;
        }

        public static Color GetOwnerTextureTint(Player owner, PieceType type, bool distinctTemplates = false)
        {
            if (distinctTemplates)
            {
                if (owner == Player.Host)
                    return Color.Lerp(Color.white, HostWood, 0.08f);

                Color terracotta = Color.Lerp(OpponentWood, GetTileBodyColor(type), 0.1f);
                return Color.Lerp(Color.white, terracotta, 0.18f);
            }

            Color typeAccent = GetTileBodyColor(type);
            if (owner == Player.Host)
            {
                Color stone = Color.Lerp(HostWood, Color.white, 0.55f);
                return Color.Lerp(stone, typeAccent, 0.03f);
            }

            Color warm = Color.Lerp(OpponentWood, typeAccent, 0.15f);
            return Color.Lerp(Color.white, warm, 0.45f);
        }

        public static void ApplyFlowerColor(Renderer renderer, Color color)
        {
            if (renderer == null)
                return;

            Material source = renderer.sharedMaterial;
            if (source != null && PieceMaterialUtility.HasAlbedoTexture(source))
            {
                var material = new Material(source);
                Color tinted = Color.Lerp(Color.white, color, 0.55f);

                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", tinted);
                else
                    material.color = tinted;

                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", color * 0.18f);
                }

                material.renderQueue = 3001;
                renderer.material = material;
                return;
            }

            ApplyBrightColor(renderer, color, unlit: true);
        }

        private static void ApplyBrightColor(Renderer renderer, Color color, bool unlit)
        {
            if (renderer == null)
                return;

            Shader shader = unlit
                ? Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Unlit/Color")
                : Shader.Find("Universal Render Pipeline/Lit")
                  ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                  ?? Shader.Find("Standard");

            if (shader == null)
                shader = Shader.Find("Standard");

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else
                material.color = color;

            if (!unlit)
            {
                if (material.HasProperty("_Smoothness"))
                    material.SetFloat("_Smoothness", 0.15f);
                if (material.HasProperty("_Metallic"))
                    material.SetFloat("_Metallic", 0f);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * (unlit ? 0.85f : 0.6f));
            }

            material.renderQueue = 3000;
            renderer.material = material;
        }

        public static Color GetOwnerWood(Player owner)
        {
            return owner == Player.Host ? HostWood : OpponentWood;
        }

        public static Color GetOwnerBodyColor(Player owner, PieceType type)
        {
            Color baseBody = owner == Player.Host ? HostCeramic : OpponentTerracotta;
            Color typeAccent = GetTileBodyColor(type);
            float typeMix = owner == Player.Host ? 0.04f : 0.1f;
            return Color.Lerp(baseBody, typeAccent, typeMix);
        }

        public static float GetOwnerBodySmoothness(Player owner)
        {
            return owner == Player.Host ? 0.84f : 0.68f;
        }

        public static float GetTileDiameter(float cellSpacing) => cellSpacing * 0.95f;

        public struct PiecePrefabMetrics
        {
            public float Footprint;
            public float Height;
            public float SurfaceLift;
            public float CellSpacing;

            public static PiecePrefabMetrics Default => new()
            {
                Footprint = 0.4f,
                Height = 0.2f,
                SurfaceLift = 0f,
                CellSpacing = 0.42f
            };
        }

        public static PiecePrefabMetrics MeasurePiecePrefab(GameObject prefab)
        {
            if (prefab == null)
                return PiecePrefabMetrics.Default;

            var temp = Object.Instantiate(prefab);
            temp.hideFlags = HideFlags.HideAndDontSave;
            temp.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            temp.transform.localScale = Vector3.one;

            var renderers = temp.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Object.Destroy(temp);
                return PiecePrefabMetrics.Default;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float footprint = Mathf.Max(bounds.size.x, bounds.size.z);
            float surfaceLift = Mathf.Max(0f, -temp.transform.InverseTransformPoint(bounds.min).y);

            Object.Destroy(temp);

            return new PiecePrefabMetrics
            {
                Footprint = footprint,
                Height = bounds.size.y,
                SurfaceLift = surfaceLift,
                CellSpacing = footprint * 1.08f
            };
        }

        /// <summary>Exported tile body height in GLB space (meters). Jasmine exports thinner (0.3036).</summary>
        public const float StandardTileMeshHeight = 0.4f;
        public const float JasmineExportedMeshHeight = 0.3036f;
        public const float JasmineThicknessCorrection = StandardTileMeshHeight / JasmineExportedMeshHeight;

        private static Transform FindTileBodyTransform(Transform root)
        {
            foreach (Transform child in root)
            {
                if (child.name.Equals("Tile", System.StringComparison.OrdinalIgnoreCase))
                    return child;
            }

            return null;
        }

        public static void ApplyJasmineThicknessCorrection(GameObject root)
        {
            if (root == null)
                return;

            Transform body = FindTileBodyTransform(root.transform);
            if (body == null)
                return;

            MeshFilter bodyMesh = body.GetComponent<MeshFilter>();
            if (bodyMesh != null && bodyMesh.sharedMesh != null)
            {
                float bodyHeight = bodyMesh.sharedMesh.bounds.size.y * Mathf.Abs(body.localScale.y);
                if (bodyHeight >= StandardTileMeshHeight * 0.97f)
                    return;
            }
            else if (body.localScale.y >= JasmineThicknessCorrection * 0.97f)
            {
                return;
            }

            float factor = JasmineThicknessCorrection;
            Vector3 bodyScale = body.localScale;
            body.localScale = new Vector3(bodyScale.x, bodyScale.y * factor, bodyScale.z);

            foreach (Transform child in root.transform)
            {
                if (child == body)
                    continue;

                if (child.name.IndexOf("face", System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                Vector3 pos = child.localPosition;
                child.localPosition = new Vector3(pos.x, pos.y * factor, pos.z);
                Vector3 faceScale = child.localScale;
                child.localScale = new Vector3(faceScale.x, faceScale.y * factor, faceScale.z);
            }

            RecenterPrefabOrigin(root);
        }

        public static bool TryMeasureVisualBoundsLocal(GameObject root, out Bounds localBounds)
        {
            localBounds = default;
            if (root == null)
                return false;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return false;

            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            foreach (Renderer renderer in renderers)
            {
                Bounds world = renderer.bounds;
                Vector3 center = world.center;
                Vector3 extents = world.extents;

                for (int x = -1; x <= 1; x += 2)
                {
                    for (int y = -1; y <= 1; y += 2)
                    {
                        for (int z = -1; z <= 1; z += 2)
                        {
                            Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                            Vector3 local = root.transform.InverseTransformPoint(corner);
                            min = Vector3.Min(min, local);
                            max = Vector3.Max(max, local);
                        }
                    }
                }
            }

            localBounds = new Bounds((min + max) * 0.5f, max - min);
            return localBounds.size.sqrMagnitude > 0.0000001f;
        }

        public static bool TryGetVisualBoundsCenterLocal(GameObject root, out Vector3 centerLocal)
        {
            centerLocal = Vector3.zero;
            if (!TryMeasureVisualBoundsLocal(root, out Bounds localBounds))
                return false;

            centerLocal = localBounds.center;
            return true;
        }

        /// <summary>
        /// Shifts child geometry so combined renderer bounds are centered on the root origin.
        /// </summary>
        public static bool RecenterPrefabOrigin(GameObject root)
        {
            if (!TryGetVisualBoundsCenterLocal(root, out Vector3 offset))
                return false;

            if (offset.sqrMagnitude < 0.00000001f)
                return false;

            foreach (Transform child in root.transform)
                child.localPosition -= offset;

            return true;
        }

        public static float AlignPrefabToSurface(GameObject root, float extraLift = 0f)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return 0f;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            // Lift in world space so the mesh bottom sits on the current pivot Y.
            // World-space avoids losing the offset when transform.position is assigned after.
            float lift = root.transform.position.y - bounds.min.y + extraLift;
            if (Mathf.Abs(lift) > 0.0001f)
                root.transform.position += Vector3.up * lift;

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return Mathf.Max(bounds.size.x, bounds.size.z);
        }

        public static float SeatOnWoodSurface(GameObject root)
        {
            return AlignPrefabToSurface(root, BoardSurfaceRestClearance);
        }

        public static bool TryGetVisualBoundsWorldBottom(GameObject root, out float worldBottomY)
        {
            worldBottomY = 0f;
            if (root == null)
                return false;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return false;

            float minY = float.PositiveInfinity;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                minY = Mathf.Min(minY, renderer.bounds.min.y);
            }

            if (float.IsPositiveInfinity(minY))
                return false;

            worldBottomY = minY;
            return true;
        }

        /// <summary>Seat a tile so its mesh bottom matches a reference tile (e.g. baked SampleTile).</summary>
        public static void AlignPrefabBottomToReference(GameObject tile, GameObject reference)
        {
            if (tile == null || reference == null)
                return;

            if (!TryGetVisualBoundsWorldBottom(reference, out float referenceBottom) ||
                !TryGetVisualBoundsWorldBottom(tile, out float tileBottom))
            {
                return;
            }

            tile.transform.position += Vector3.up * (referenceBottom - tileBottom);
        }

        public static void EnsurePiecePickCollider(GameObject root, float cellSpacing)
        {
            if (root == null)
                return;

            // Strip child mesh colliders only. Never Destroy() the root BoxCollider in play mode —
            // deferred Destroy would remove the collider we configure on this same frame.
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                if (collider == null || collider.gameObject == root)
                    continue;

                if (Application.isPlaying)
                    Object.Destroy(collider);
                else
                    Object.DestroyImmediate(collider);
            }

            float footprint = Mathf.Max(cellSpacing * BoardPickUtility.PieceColliderFootprintScale, 0.42f);
            float height = Mathf.Max(cellSpacing * 1.35f, 0.48f);

            var box = root.GetComponent<BoxCollider>();
            if (box == null)
                box = root.AddComponent<BoxCollider>();

            // Tall, wide volume so a high camera still hits the tile easily.
            box.enabled = true;
            box.isTrigger = false;
            box.center = new Vector3(0f, height * 0.45f, 0f);
            box.size = new Vector3(footprint, height, footprint);
        }

        public static float MeasurePrefabFootprint(GameObject root)
        {
            if (root == null)
                return 0f;

            float maxDiameter = 0f;
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                    continue;

                Vector3 meshSize = filter.sharedMesh.bounds.size;
                Vector3 scale = filter.transform.lossyScale;
                float diameter = Mathf.Max(
                    meshSize.x * Mathf.Abs(scale.x),
                    meshSize.z * Mathf.Abs(scale.z));
                maxDiameter = Mathf.Max(maxDiameter, diameter);
            }

            if (maxDiameter > 0.0001f)
                return maxDiameter;

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return 0f;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return Mathf.Max(bounds.size.x, bounds.size.z);
        }

        public static void FitPrefabToCellSpacing(GameObject root, float cellSpacing, bool alignBottomToSurface = true)
        {
            if (root == null)
                return;

            FitPrefabScaleOnly(root, cellSpacing);

            if (alignBottomToSurface)
                AlignPrefabToSurface(root);
            else
                RecenterPrefabOrigin(root);
        }

        /// <summary>Scale a prefab tile to cell size without recentering or seating (hand tray).</summary>
        public static void FitPrefabScaleOnly(GameObject root, float cellSpacing)
        {
            if (root == null)
                return;

            float footprint = MeasurePrefabFootprint(root);
            float target = GetTileDiameter(cellSpacing);
            if (footprint > 0.0001f)
            {
                float factor = target / footprint;
                Vector3 scale = root.transform.localScale;
                root.transform.localScale = scale * factor;
            }

            EnsureMeshLighting(root);
        }

        /// <summary>Prefer real mesh shadows over fake disc overlays.</summary>
        public static void EnsureMeshLighting(GameObject root)
        {
            if (root == null)
                return;

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                string name = renderer.gameObject.name;
                if (name.Contains("TravelShadow") || name.Contains("Glow") || name.Contains("Aura") ||
                    name.Contains("HarmonyBeam") || name.Contains("Marker"))
                    continue;

                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }

        /// <summary>Scale a prefab tile to match a reference without recentering (hand-tray sample match).</summary>
        public static void MatchPrefabFootprintToReference(GameObject tile, GameObject reference)
        {
            if (tile == null || reference == null)
                return;

            float targetFootprint = MeasurePrefabFootprint(reference);
            if (targetFootprint < 0.0001f)
                return;

            tile.transform.localScale = Vector3.one;

            float footprint = MeasurePrefabFootprint(tile);
            if (footprint > 0.0001f)
            {
                float factor = targetFootprint / footprint;
                tile.transform.localScale *= factor;
            }
        }

        public static void PreparePlacedTile(GameObject tile, PieceType type, Player owner, float cellSpacing)
        {
            if (tile == null)
                return;

            tile.transform.localScale = Vector3.one;

            for (int i = tile.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(tile.transform.GetChild(i).gameObject);

            float footprint = GetTileDiameter(cellSpacing);
            BuildCompleteTileVisual(tile.transform, type, owner, footprint);
        }

        public static bool IsFlowerRenderer(string partName)
        {
            if (partName is "FlowerInlay" or "FlowerFace" or "FlowerPetal" or "FlowerStem" or "Face")
                return true;

            // Authored glTF prefabs use "Face" / "Face_Mat0" for engraved flower inlays.
            // Jasmine historically exported as "Jasmine Face" — NormalizeFlowerInlayNames renames those.
            if (partName.IndexOf("face", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return partName.IndexOf("inlay", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Rename authored flower layers to a stable "Face" / "Face_Mat*" convention so
        /// harmony motion and emission always find the inlay (esp. Jasmine).
        /// </summary>
        public static void NormalizeFlowerInlayNames(GameObject root)
        {
            if (root == null)
                return;

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child == null || child == root.transform)
                    continue;

                string name = child.name;
                if (name.Equals("Face", System.StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("Face_", System.StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("Flower", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int faceIndex = name.IndexOf("face", System.StringComparison.OrdinalIgnoreCase);
                if (faceIndex < 0)
                    continue;

                string suffix = name.Substring(faceIndex + 4); // after "face"
                child.name = string.IsNullOrEmpty(suffix) ? "Face" : "Face" + suffix;
            }
        }

        /// <summary>True for engraved flower/inlay renderers on prefab or procedural tiles.</summary>
        public static bool IsFlowerVisualRenderer(Renderer renderer)
        {
            if (renderer == null)
                return false;

            string partName = renderer.gameObject.name;
            if (IsFlowerRenderer(partName))
                return true;

            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null)
                    continue;

                string materialName = material.name.ToLowerInvariant();
                if (materialName.Contains("hostbody_ceramic") ||
                    materialName.Contains("terracotta") ||
                    materialName.Contains("opponentbody"))
                {
                    continue;
                }

                if (materialName.Contains("engrav") || materialName.Contains("hj"))
                    return true;
            }

            return false;
        }

        public static GameObject CreateWoodTileToken(PieceType type, Player owner, float diameter = -1f)
        {
            float footprint = diameter > 0.01f ? diameter : 0.42f;
            var root = new GameObject($"Tile_{type}");
            BuildCompleteTileVisual(root.transform, type, owner, footprint);
            return root;
        }

        /// <summary>
        /// Forces textures onto the GPU and converts glTF shaders to URP Lit so the board
        /// does not flash cyan while materials finish loading.
        /// </summary>
        public static void PreloadVisualMaterials(GameObject root, System.Func<Transform, bool> skipRenderer = null)
        {
            if (root == null)
                return;

            PieceMaterialUtility.EnsureMaterials(root, skipRenderer: skipRenderer);

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || (skipRenderer != null && skipRenderer(renderer.transform)))
                    continue;

                if (IsSquareBoardUnderlay(renderer))
                    continue;

                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (source == null)
                        continue;

                    ForceLoadMaterialTextures(source);
                    Material converted = PieceMaterialUtility.ToUrpMaterial(source);
                    if (!ReferenceEquals(converted, source))
                    {
                        materials[i] = converted;
                        changed = true;
                        ForceLoadMaterialTextures(converted);
                    }
                }

                if (changed)
                    renderer.sharedMaterials = materials;
            }

            HideSquareBoardUnderlays(root);
        }

        private static void ForceLoadMaterialTextures(Material material)
        {
            if (material == null)
                return;

            string[] textureProperties =
            {
                "_BaseMap", "_MainTex", "_BumpMap", "_MetallicGlossMap", "_OcclusionMap", "_EmissionMap"
            };

            foreach (string property in textureProperties)
            {
                if (!material.HasProperty(property))
                    continue;

                Texture texture = material.GetTexture(property);
                if (texture == null)
                    continue;

                // Touching width/height forces the texture upload before the first frame draws.
                _ = texture.width;
                _ = texture.height;
            }
        }

        private static void BuildCompleteTileVisual(Transform root, PieceType type, Player owner, float footprint)
        {
            Color flowerColor = GetFlowerAccent(type);
            Color ownerColor = GetOwnerWood(owner);

            float baseHeight = footprint * 0.18f;
            var woodBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            woodBase.name = "WoodBody";
            woodBase.transform.SetParent(root, false);
            woodBase.transform.localScale = new Vector3(footprint * 0.72f, baseHeight, footprint * 0.72f);
            woodBase.transform.localPosition = new Vector3(0f, baseHeight * 0.5f, 0f);
            DestroyCollider(woodBase);
            ApplyWood(woodBase.GetComponent<Renderer>(), GetOwnerBodyColor(owner, type), GetOwnerBodySmoothness(owner));
            SetRenderQueue(woodBase.GetComponent<Renderer>(), 2000);

            var ownerBand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ownerBand.name = "OwnerBand";
            ownerBand.transform.SetParent(root, false);
            ownerBand.transform.localScale = new Vector3(footprint * 0.12f, baseHeight * 1.4f, footprint * 0.12f);
            ownerBand.transform.localPosition = new Vector3(footprint * 0.34f, baseHeight * 0.55f, footprint * 0.34f);
            DestroyCollider(ownerBand);
            ApplyFlowerColor(ownerBand.GetComponent<Renderer>(), ownerColor);

            float capHeight = footprint * GetCapHeightScale(type);
            float capWidth = footprint * 0.92f;
            var flowerCap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flowerCap.name = "FlowerFace";
            flowerCap.transform.SetParent(root, false);
            flowerCap.transform.localScale = new Vector3(capWidth, capHeight, capWidth);
            flowerCap.transform.localPosition = new Vector3(0f, baseHeight + capHeight * 0.5f, 0f);
            DestroyCollider(flowerCap);
            ApplyFlowerColor(flowerCap.GetComponent<Renderer>(), flowerColor);

            if (IsLightFlowerColor(flowerColor))
            {
                var capRim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                capRim.name = "FlowerStem";
                capRim.transform.SetParent(root, false);
                capRim.transform.localScale = new Vector3(capWidth * 1.04f, capHeight * 0.08f, capWidth * 1.04f);
                capRim.transform.localPosition = new Vector3(0f, baseHeight + capHeight * 0.04f, 0f);
                DestroyCollider(capRim);
                ApplyFlowerColor(capRim.GetComponent<Renderer>(), Hex("#3A2A1E"));
            }

            var cosmetics = new GameObject("FlowerCosmetics");
            cosmetics.transform.SetParent(root, false);
            cosmetics.transform.localPosition = Vector3.zero;
            float topY = baseHeight + capHeight;
            AddTypeMarker(cosmetics.transform, type, footprint, topY, flowerColor);
        }

        private static bool IsLightFlowerColor(Color color)
        {
            float luminance = color.r * 0.299f + color.g * 0.587f + color.b * 0.114f;
            return luminance > 0.72f;
        }

        private static float GetCapHeightScale(PieceType type)
        {
            return type switch
            {
                PieceType.Rose or PieceType.Rhododendron => 0.62f,
                PieceType.Lily or PieceType.Orchid => 0.58f,
                PieceType.Chrysanthemum => 0.5f,
                PieceType.Rock or PieceType.Wheel => 0.38f,
                _ => 0.55f
            };
        }

        private static void AddTypeMarker(Transform parent, PieceType type, float footprint, float topY, Color flowerColor)
        {
            if (PieceRules.IsNonFlower(type) || type == PieceType.Rock || type == PieceType.Wheel)
            {
                var mark = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mark.name = "FlowerInlay";
                mark.transform.SetParent(parent, false);
                mark.transform.localScale = new Vector3(footprint * 0.35f, footprint * 0.12f, footprint * 0.35f);
                mark.transform.localPosition = new Vector3(0f, topY + footprint * 0.06f, 0f);
                DestroyCollider(mark);
                ApplyFlowerColor(mark.GetComponent<Renderer>(), Color.Lerp(flowerColor, Color.white, 0.35f));
                return;
            }

            int dots = type switch
            {
                PieceType.Rose or PieceType.Rhododendron or PieceType.Lily => 6,
                PieceType.Chrysanthemum => 8,
                PieceType.Jade => 4,
                _ => 5
            };

            float dot = footprint * 0.14f;
            float ring = footprint * 0.28f;
            for (int i = 0; i < dots; i++)
            {
                float angle = i * Mathf.PI * 2f / dots;
                var petal = GameObject.CreatePrimitive(PrimitiveType.Cube);
                petal.name = i == 0 ? "FlowerInlay" : "FlowerPetal";
                petal.transform.SetParent(parent, false);
                petal.transform.localPosition = new Vector3(
                    Mathf.Sin(angle) * ring,
                    topY + dot * 0.55f,
                    Mathf.Cos(angle) * ring);
                petal.transform.localScale = Vector3.one * dot;
                DestroyCollider(petal);
                ApplyFlowerColor(petal.GetComponent<Renderer>(), Color.Lerp(flowerColor, Color.white, 0.45f));
            }
        }

        private static bool IsFlowerPiece(PieceType type)
        {
            return type is PieceType.Jasmine or PieceType.Rose or PieceType.Lily
                or PieceType.Jade or PieceType.Chrysanthemum or PieceType.Rhododendron
                or PieceType.Lotus or PieceType.Orchid;
        }

        public static GameObject CreateBoardGrid(BoardLayout layout)
        {
            return CreateBoardSurface(layout);
        }

        public static GameObject CreateModelBoardSurface(
            BoardLayout layout,
            GameObject modelRoot,
            string sourceAssetPath,
            out float alignedGridSpan,
            out float boardSurfaceY)
        {
            alignedGridSpan = 0f;
            boardSurfaceY = 0f;
            var surfaceRoot = new GameObject("BoardModelSurface");
            if (layout == null || modelRoot == null)
                return surfaceRoot;

            GameObject instance = Object.Instantiate(modelRoot);
            instance.name = "BoardModel";
            instance.transform.SetParent(surfaceRoot.transform, false);
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 90f, 0f));
            instance.transform.localScale = Vector3.one;

            PieceMaterialUtility.EnsureMaterials(instance, sourceAssetPath);
            StripColliders(instance);

            float targetSpan = layout.GridSpan;
            float scale = 1f;
            if (TryMeasureNineteenByNineteenGridSpan(instance, out float unitSpan19))
            {
                scale = targetSpan / unitSpan19;
                alignedGridSpan = unitSpan19 * scale;
            }
            else if (TryMeasurePlayableGridSpan(instance, out float unitSpan) && unitSpan > 0.001f)
            {
                scale = targetSpan / unitSpan;
                alignedGridSpan = unitSpan * scale;
            }
            else
            {
                alignedGridSpan = targetSpan;
            }

            instance.transform.localScale = Vector3.one * scale;

            AlignBoardModelToFloor(instance, layout, recenterXZ: true);
            boardSurfaceY = TryGetBoardSurfaceY(instance, layout.Origin, out float topY) ? topY : layout.TileHeight;

            ApplyBoardModelMaterialTweaks(instance, layout);

            return surfaceRoot;
        }

        /// <summary>Hides the square Base/Board underlay meshes. Garden surfaces are the visible board.</summary>
        public static void HideSquareBoardBaseAndEmbeddedStand(GameObject boardModel)
        {
            if (boardModel == null)
                return;

            HideSquareBoardUnderlays(boardModel);
            HideEmbeddedPlayerStand(boardModel);
        }

        public static void HideSquareBoardUnderlays(GameObject boardModel)
        {
            if (boardModel == null)
                return;

            foreach (Renderer renderer in boardModel.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !IsSquareBoardUnderlay(renderer))
                    continue;

                renderer.enabled = false;
                renderer.gameObject.SetActive(false);
            }
        }

        private static bool IsSquareBoardUnderlay(Renderer renderer) =>
            renderer != null && IsSquareBoardUnderlayName(renderer.gameObject.name);

        public static bool IsSquareBoardUnderlayName(string objectName) =>
            objectName.Equals("Base", System.StringComparison.OrdinalIgnoreCase);

        public static void RefreshPrebuiltBoardVisuals(BoardLayout layout, GameObject boardModelRoot, bool applyMaterialTweaks = true)
        {
            if (boardModelRoot == null || layout == null)
                return;

            ApplySourceMaterials(boardModelRoot, null, BoardVisualLoader.ModelAssetPath, BoardVisualLoader.ModelResourcesPath);
            if (applyMaterialTweaks)
                ApplyBoardModelMaterialTweaks(boardModelRoot, layout);
            HideSquareBoardBaseAndEmbeddedStand(boardModelRoot);
        }

        public static void RefreshPrebuiltStandVisuals(GameObject standRoot, System.Func<Transform, bool> skipRenderer = null)
        {
            if (standRoot == null)
                return;

            ApplySourceMaterials(standRoot, skipRenderer, PlayerStandLoader.ModelAssetPath, PlayerStandLoader.ModelResourcesPath);
            EnsurePlayerStandVisible(standRoot);
        }

        private static void ApplySourceMaterials(GameObject root, System.Func<Transform, bool> skipRenderer, params string[] sourceAssetPaths)
        {
            if (root == null || sourceAssetPaths == null)
                return;

            foreach (string path in sourceAssetPaths)
            {
                if (string.IsNullOrEmpty(path))
                    continue;

                PieceMaterialUtility.EnsureMaterials(root, path, skipRenderer: skipRenderer);
                if (HasAssignedMaterials(root, skipRenderer))
                    return;
            }
        }

        private static bool HasAssignedMaterials(GameObject root, System.Func<Transform, bool> skipRenderer = null)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || (skipRenderer != null && skipRenderer(renderer.transform)))
                    continue;

                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null)
                        return true;
                }
            }

            return false;
        }

        public static void HideEmbeddedPlayerStand(GameObject boardModel)
        {
            if (boardModel == null)
                return;

            foreach (Renderer renderer in boardModel.GetComponentsInChildren<Renderer>(true))
            {
                if (IsPlayerStandRenderer(renderer))
                    renderer.gameObject.SetActive(false);
            }
        }

        public static Transform SpawnPlayerStand(BoardLayout layout, Transform parent, Player player)
        {
            if (layout == null || parent == null || !PlayerStandLoader.TryLoadModel(out GameObject prefab, out string source))
                return null;

            var instance = Object.Instantiate(prefab, parent);
            instance.name = player == Player.Host ? "PlayerStand" : "PlayerStandOpponent";
            PieceMaterialUtility.EnsureMaterials(instance, source);
            EnsurePlayerStandVisible(instance);
            StripColliders(instance);

            Renderer renderer = instance.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                Bounds localBounds = ComputeRendererLocalBounds(renderer);
                float longSpan = Mathf.Max(localBounds.size.x, localBounds.size.z);
                float targetSpan = layout.CellSpacing * 6f;
                if (longSpan > 0.001f)
                    instance.transform.localScale = Vector3.one * (targetSpan / longSpan);
            }

            AlignPlayerStandToGate(instance.transform, layout, player);
            return instance.transform;
        }

        public static void AlignPlayerStandToGate(Transform stand, BoardLayout layout, Player player)
        {
            AlignPlayerStandToGate(
                stand,
                layout,
                player,
                BoardAlignmentDefaults.PlayerStandSouthCells,
                BoardAlignmentDefaults.PlayerStandEastCells);
        }

        public static void AlignPlayerStandToGate(
            Transform stand,
            BoardLayout layout,
            Player player,
            float southCells,
            float eastCells)
        {
            if (stand == null || layout == null)
                return;

            int gateCoordinate = player == Player.Host ? BoardUtils.SouthGate : BoardUtils.NorthGate;
            Vector3 gatePos = layout.CoordinateToWorld(gateCoordinate);
            Vector3 middle = layout.CoordinateToWorld(BoardUtils.MiddleGate);
            Vector3 towardGate = gatePos - middle;
            towardGate.y = 0f;
            if (towardGate.sqrMagnitude < 0.0001f)
                towardGate = player == Player.Host ? Vector3.back : Vector3.forward;
            towardGate.Normalize();

            Vector3 east = Vector3.Cross(Vector3.up, towardGate).normalized;
            float spacing = layout.CellSpacing * layout.SpacingFineTune;

            Vector3 position = gatePos + towardGate * (spacing * southCells) + east * (spacing * eastCells);
            stand.SetPositionAndRotation(position, Quaternion.LookRotation(-towardGate, Vector3.up));

            Renderer renderer = stand.GetComponentInChildren<Renderer>();
            if (renderer == null)
                return;

            Bounds bounds = renderer.bounds;
            float lift = layout.TileHeight - bounds.min.y;
            stand.position += Vector3.up * lift;
        }

        public static void ApplyPlayerStandTuning(
            Transform stand,
            BoardLayout layout,
            Player player,
            HandTrayTunerSettings tuner)
        {
            if (stand == null || layout == null)
                return;

            float southCells = tuner != null
                ? tuner.standSouthCells
                : HandTrayAlignmentDefaults.StandSouthCells;
            float eastCells = tuner != null
                ? tuner.standEastCells
                : HandTrayAlignmentDefaults.StandEastCells;
            float scaleMultiplier = tuner != null
                ? tuner.standScaleMultiplier
                : HandTrayAlignmentDefaults.StandScaleMultiplier;
            float yawOffset = tuner != null
                ? tuner.standYawOffset
                : HandTrayAlignmentDefaults.StandYawOffset;
            float liftOffset = tuner != null
                ? tuner.standLiftOffset
                : HandTrayAlignmentDefaults.StandLiftOffset;
            Vector3 extraOffset = tuner != null
                ? tuner.standExtraOffset
                : HandTrayAlignmentDefaults.StandExtraOffset;

            AlignPlayerStandToGate(stand, layout, player, southCells, eastCells);

            Renderer renderer = stand.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                Bounds localBounds = ComputeRendererLocalBounds(renderer);
                float longSpan = Mathf.Max(localBounds.size.x, localBounds.size.z);
                float targetSpan = layout.CellSpacing * 6f * scaleMultiplier;
                if (longSpan > 0.001f)
                    stand.localScale = Vector3.one * (targetSpan / longSpan);
            }

            if (Mathf.Abs(yawOffset) > 0.001f)
                stand.Rotate(Vector3.up, yawOffset, Space.World);

            if (Mathf.Abs(liftOffset) > 0.0001f)
                stand.position += Vector3.up * liftOffset;

            if (extraOffset.sqrMagnitude > 0.000001f)
                stand.position += stand.right * extraOffset.x + stand.up * extraOffset.y + stand.forward * extraOffset.z;
        }

        public static void EnsurePlayerStandVisible(GameObject boardModel)
        {
            if (boardModel == null)
                return;

            foreach (Renderer renderer in boardModel.GetComponentsInChildren<Renderer>(true))
            {
                if (!IsPlayerStandRenderer(renderer))
                    continue;

                renderer.gameObject.SetActive(true);
                renderer.enabled = true;
                ApplyTexturedWood(renderer, "Scene/dark_oak", FrameWood, 0.45f, 2.2f);
            }
        }

        public readonly struct PlayerStandAnchor
        {
            public readonly Transform StandTransform;
            public readonly Bounds LocalBounds;
            public readonly Vector3 LocalTileAxis;
            public readonly float LocalShelfWidth;

            public PlayerStandAnchor(
                Transform standTransform,
                Bounds localBounds,
                Vector3 localTileAxis,
                float localShelfWidth)
            {
                StandTransform = standTransform;
                LocalBounds = localBounds;
                LocalTileAxis = localTileAxis;
                LocalShelfWidth = localShelfWidth;
            }
        }

        public static bool TryGetPlayerStandAnchor(
            GameObject standRoot,
            Player player,
            out PlayerStandAnchor anchor)
        {
            anchor = default;
            if (standRoot == null)
                return false;

            if (!TryGetPlayerStandMeshRenderer(standRoot, out Renderer renderer))
                return false;

            // Tray slots were tuned in mesh-local space (F9); parent to the mesh, not the stand root.
            Transform meshTransform = renderer.transform;
            Bounds localBounds = ComputeRendererLocalBounds(renderer);
            Vector3 localTileAxis = localBounds.size.x >= localBounds.size.z
                ? Vector3.right
                : Vector3.forward;
            float localShelfWidth = localTileAxis == Vector3.right
                ? localBounds.size.x
                : localBounds.size.z;

            anchor = new PlayerStandAnchor(meshTransform, localBounds, localTileAxis, localShelfWidth);
            return localShelfWidth > 0.001f;
        }

        public static Bounds ComputeRendererBoundsInParentSpace(Renderer renderer, Transform parent)
        {
            if (renderer == null || parent == null)
                return default;

            Bounds world = renderer.bounds;
            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            Vector3 center = world.center;
            Vector3 extents = world.extents;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                        Vector3 local = parent.InverseTransformPoint(corner);
                        min = Vector3.Min(min, local);
                        max = Vector3.Max(max, local);
                    }
                }
            }

            return new Bounds((min + max) * 0.5f, max - min);
        }

        public static bool TryGetPlayerStandMeshRenderer(GameObject standRoot, out Renderer renderer)
        {
            renderer = null;
            if (standRoot == null)
                return false;

            if (TryGetRendererByName(standRoot, "PlayerStand", out renderer))
                return true;

            renderer = standRoot.GetComponentInChildren<Renderer>(true);
            return renderer != null;
        }

        private static Bounds ComputeRendererLocalBounds(Renderer renderer)
        {
            if (renderer == null)
                return default;

            Transform transform = renderer.transform;
            Bounds world = renderer.bounds;
            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            Vector3 center = world.center;
            Vector3 extents = world.extents;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                        Vector3 local = transform.InverseTransformPoint(corner);
                        min = Vector3.Min(min, local);
                        max = Vector3.Max(max, local);
                    }
                }
            }

            return new Bounds((min + max) * 0.5f, max - min);
        }

        public static void TrimSquareBoardBase(GameObject boardModel) => HideSquareBoardUnderlays(boardModel);

        public static void HideEmbeddedBoardUnderlays(GameObject root)
        {
            if (root == null)
                return;

            Transform boardModel = FindBoardModelTransform(root);
            if (boardModel != null)
                TrimSquareBoardBase(boardModel.gameObject);
        }

        private static Transform FindBoardModelTransform(GameObject root)
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name.Equals("BoardModel", System.StringComparison.OrdinalIgnoreCase))
                    return transform;
            }

            return null;
        }

        public static void HideBoardPortMarkers(GameObject boardModel)
        {
            HideBoardDecorMeshes(boardModel);
        }

        private static void HideBoardDecorMeshes(GameObject boardModel)
        {
            if (boardModel == null)
                return;

            string[] decorNames = { "GateHome", "GateForeign", "GateEast", "GateWest" };
            foreach (string decorName in decorNames)
            {
                if (!TryGetRendererByName(boardModel, decorName, out Renderer renderer))
                    continue;

                renderer.gameObject.SetActive(false);
            }
        }

        public static bool TryGetBoardSurfaceY(GameObject boardModel, Transform origin, out float topY)
        {
            topY = 0f;
            if (!TryGetGridTopY(boardModel, out Bounds gridBounds))
                return false;

            Transform reference = origin != null ? origin : boardModel.transform;
            topY = reference.InverseTransformPoint(gridBounds.max).y;
            return true;
        }

        public static bool TryGetBoardSurfaceY(GameObject boardModel, out float topY)
        {
            return TryGetBoardSurfaceY(boardModel, null, out topY);
        }

        public static void FineAlignBoardModelIfPresent(BoardLayout layout)
        {
            if (layout == null || BoardManager.Instance == null)
                return;

            Transform boardModel = BoardManager.Instance.GetBoardModelTransform();
            if (boardModel == null)
                return;

            FineAlignBoardModel(boardModel.gameObject, layout);
            if (TryMeasureNineteenByNineteenGridSpan(boardModel.gameObject, out float span))
            {
                layout.RememberCalibratedGridSpan(span);
                layout.CalibrateFromBoardGridSpan(span);
            }

            if (TryGetBoardSurfaceY(boardModel.gameObject, layout.Origin, out float surfaceY))
                layout.SetBoardSurfaceHeight(surfaceY);

            HideSquareBoardBaseAndEmbeddedStand(boardModel.gameObject);
        }

        public static void FineAlignBoardModel(GameObject boardModel, BoardLayout layout)
        {
            if (boardModel == null || layout == null)
                return;

            if (TryAlignGates(boardModel, layout, "GateForeign", BoardUtils.SouthGate, "GateHome", BoardUtils.NorthGate))
            {
                AlignGridCenterToMiddleGate(boardModel, layout);
                AlignBoardModelToFloor(boardModel, layout, recenterXZ: false);
                return;
            }

            CenterBoardLinesXZ(boardModel);
        }

        public static bool TryMeasureGatePitch(GameObject boardModel, out float pitch)
        {
            pitch = 0f;
            if (!TryGetRendererByName(boardModel, "GateForeign", out Renderer southGate) ||
                !TryGetRendererByName(boardModel, "GateHome", out Renderer northGate))
            {
                return false;
            }

            int rowDelta = BoardUtils.GetRow(BoardUtils.NorthGate) - BoardUtils.GetRow(BoardUtils.SouthGate);
            if (rowDelta <= 0)
                return false;

            Vector3 south = southGate.bounds.center;
            Vector3 north = northGate.bounds.center;
            float distance = Vector2.Distance(new Vector2(south.x, south.z), new Vector2(north.x, north.z));
            pitch = distance / rowDelta;
            return pitch > 0.001f;
        }

        private static void AlignGridCenterToMiddleGate(GameObject boardModel, BoardLayout layout)
        {
            if (!TryGetGridInlayRenderer(boardModel, out Renderer gridRenderer))
                return;

            Vector3 target = layout.CoordinateToWorld(BoardUtils.MiddleGate);
            Vector3 actual = gridRenderer.bounds.center;
            float deltaX = target.x - actual.x;
            boardModel.transform.position += new Vector3(deltaX, 0f, 0f);
        }

        private static bool TryAlignGates(
            GameObject boardModel,
            BoardLayout layout,
            string southGateName,
            int southGateCoordinate,
            string northGateName,
            int northGateCoordinate)
        {
            if (!TryGetRendererByName(boardModel, southGateName, out Renderer southGate) ||
                !TryGetRendererByName(boardModel, northGateName, out Renderer northGate))
            {
                return false;
            }

            Vector3 southTarget = layout.CoordinateToWorld(southGateCoordinate);
            Vector3 northTarget = layout.CoordinateToWorld(northGateCoordinate);
            Vector3 southActual = southGate.bounds.center;
            Vector3 northActual = northGate.bounds.center;

            Vector3 delta = new Vector3(
                (southTarget.x - southActual.x + northTarget.x - northActual.x) * 0.5f,
                0f,
                (southTarget.z - southActual.z + northTarget.z - northActual.z) * 0.5f);
            delta += layout.BoardModelOffset;
            boardModel.transform.position += delta;
            return true;
        }

        private static void AlignBoardModelToFloor(GameObject boardModel, BoardLayout layout, bool recenterXZ)
        {
            float bottomY = GetBoardBottomY(boardModel);
            if (float.IsPositiveInfinity(bottomY))
            {
                if (recenterXZ)
                    CenterBoardLinesXZ(boardModel);
                return;
            }

            float deltaY = layout.BoardFloorY - bottomY;
            boardModel.transform.position += Vector3.up * deltaY;
            if (recenterXZ)
                CenterBoardLinesXZ(boardModel);
        }

        private static float GetBoardBottomY(GameObject boardModel)
        {
            if (TryGetRendererByName(boardModel, "Base", out Renderer baseRenderer))
                return baseRenderer.bounds.min.y;

            float minY = float.PositiveInfinity;
            foreach (Renderer renderer in boardModel.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || IsPlayerStandRenderer(renderer))
                    continue;

                minY = Mathf.Min(minY, renderer.bounds.min.y);
            }

            return minY;
        }

        private static bool IsPlayerStandRenderer(Renderer renderer)
        {
            return renderer != null &&
                   renderer.gameObject.name.IndexOf("PlayerStand", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void CenterBoardLinesXZ(GameObject boardModel)
        {
            if (!TryGetGridInlayRenderer(boardModel, out Renderer gridRenderer))
                return;

            Bounds gridBounds = gridRenderer.bounds;
            Vector3 delta = new Vector3(-gridBounds.center.x, 0f, -gridBounds.center.z);
            boardModel.transform.position += delta;
        }

        private static bool TryGetRendererByName(GameObject root, string objectName, out Renderer renderer)
        {
            renderer = null;
            if (root == null || string.IsNullOrEmpty(objectName))
                return false;

            foreach (Renderer candidate in root.GetComponentsInChildren<Renderer>(true))
            {
                if (candidate == null)
                    continue;

                if (candidate.gameObject.name.Equals(objectName, System.StringComparison.OrdinalIgnoreCase))
                {
                    renderer = candidate;
                    return true;
                }
            }

            return false;
        }

        public static GameObject CreateModelBoardSurface(BoardLayout layout, GameObject modelRoot, string sourceAssetPath = null)
        {
            return CreateModelBoardSurface(layout, modelRoot, sourceAssetPath, out _, out _);
        }

        private static bool TryGetGridTopY(GameObject boardModel, out Bounds gridBounds)
        {
            gridBounds = default;
            if (!TryGetGridInlayRenderer(boardModel, out Renderer gridRenderer))
                return false;

            gridBounds = gridRenderer.bounds;
            return true;
        }

        private static bool TryGetGridTopY(GameObject boardModel, out float topY)
        {
            topY = 0f;
            if (!TryGetGridTopY(boardModel, out Bounds gridBounds))
                return false;

            topY = gridBounds.max.y;
            return true;
        }

        /// <summary>World span from outermost to outermost intersection on the 19×19 point grid (18 intervals).</summary>
        public static bool TryMeasureNineteenByNineteenGridSpan(GameObject boardModel, out float span)
        {
            span = 0f;
            if (TryMeasureGatePitch(boardModel, out float pitch))
            {
                span = pitch * BoardUtils.GridIntervals;
                return span > 0.001f;
            }

            // Fallback: BoardLines bounds are slightly larger than the playable grid.
            if (TryMeasurePlayableGridSpan(boardModel, out float linesSpan))
            {
                span = linesSpan * 0.92f;
                return span > 0.001f;
            }

            return false;
        }

        /// <summary>
        /// Fits BoardLayout spacing, yaw, and surface height to a scene-authored board mesh
        /// without moving the mesh (preserve-authored mode).
        /// </summary>
        public static bool CalibrateLayoutToSceneBoard(BoardLayout layout, GameObject boardModel)
        {
            if (layout == null || boardModel == null)
                return false;

            bool calibrated = false;

            // Keep axis-aligned grid (yaw 0) — tune in F8 if the mesh is rotated.
            layout.SetGridYawDegrees(0f);

            if (TryMeasureNineteenByNineteenGridSpan(boardModel, out float span))
            {
                layout.RememberCalibratedGridSpan(span);
                layout.CalibrateFromBoardGridSpan(span);
                calibrated = true;
            }

            if (TryGetPlayableSurfaceWorldY(boardModel, out float worldSurfaceY))
            {
                Transform origin = layout.Origin;
                float localSurfaceY = origin != null
                    ? origin.InverseTransformPoint(new Vector3(origin.position.x, worldSurfaceY, origin.position.z)).y
                    : worldSurfaceY;
                layout.SetBoardSurfaceHeight(localSurfaceY);
                calibrated = true;
            }

            return calibrated;
        }

        public static bool TryGetPlayableSurfaceWorldY(GameObject boardModel, out float worldY)
        {
            worldY = 0f;
            if (boardModel == null)
                return false;

            bool foundPreferred = false;
            bool foundFallback = false;
            float preferredMaxY = float.NegativeInfinity;
            float fallbackMaxY = float.NegativeInfinity;

            foreach (Renderer renderer in boardModel.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;

                string name = renderer.gameObject.name;
                if (IsExcludedBoardSurfaceRenderer(name))
                    continue;

                float topY = renderer.bounds.max.y;
                fallbackMaxY = Mathf.Max(fallbackMaxY, topY);
                foundFallback = true;

                if (!IsPreferredBoardSurfaceRenderer(name))
                    continue;

                preferredMaxY = Mathf.Max(preferredMaxY, topY);
                foundPreferred = true;
            }

            if (!foundFallback)
                return false;

            worldY = foundPreferred ? preferredMaxY : fallbackMaxY;
            return true;
        }

        private static bool IsExcludedBoardSurfaceRenderer(string name)
        {
            return name.IndexOf("Gate", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("PlayerStand", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Base", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Leg", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Tray", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Slot", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("SampleTile", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Tile_", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsPreferredBoardSurfaceRenderer(string name)
        {
            return name.IndexOf("Garden", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("BoardLines", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("BoardSurface", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Playing", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Grid", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Hinoki", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Table", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>BoardLines mesh bounds — fallback only; may exceed the playable 19-line circle.</summary>
        public static bool TryMeasurePlayableGridSpan(GameObject boardModel, out float horizontalSpan)
        {
            horizontalSpan = 0f;
            if (!TryGetGridInlayRenderer(boardModel, out Renderer gridRenderer))
                return false;

            Bounds bounds = gridRenderer.bounds;
            horizontalSpan = Mathf.Max(bounds.size.x, bounds.size.z);
            return horizontalSpan > 0.001f;
        }

        private static bool TryGetGridInlayRenderer(GameObject boardModel, out Renderer gridRenderer)
        {
            gridRenderer = null;
            if (boardModel == null)
                return false;

            foreach (Renderer renderer in boardModel.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                if (renderer.gameObject.name.IndexOf("BoardLines", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    gridRenderer = renderer;
                    return true;
                }
            }

            return false;
        }

        private static void ApplyBoardModelMaterialTweaks(GameObject boardModel, BoardLayout layout)
        {
            if (boardModel == null || layout == null)
                return;

            float neutralTiling = layout.NeutralGardenTextureTiling;
            float neutralLighten = layout.NeutralGardenLighten;

            Material lightGardenMaterial = null;
            Material darkGardenMaterial = null;
            var lightRenderers = new System.Collections.Generic.List<Renderer>();
            var darkRenderers = new System.Collections.Generic.List<Renderer>();

            foreach (Renderer renderer in boardModel.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                if (IsGardenLightRenderer(renderer))
                {
                    lightRenderers.Add(renderer);
                    lightGardenMaterial ??= renderer.sharedMaterial;
                    continue;
                }

                if (IsGardenDarkRenderer(renderer))
                {
                    darkRenderers.Add(renderer);
                    darkGardenMaterial ??= renderer.sharedMaterial;
                    continue;
                }

                if (!IsNeutralGardenRenderer(renderer))
                    continue;

                Material material = CreateMaterialInstance(renderer.sharedMaterial);
                SetMaterialTiling(material, neutralTiling);
                LightenMaterial(material, neutralLighten);
                AssignRendererMaterial(renderer, material);
            }

            // GardenLight/GardenDark mesh names are reversed vs gameplay garden coordinates.
            if (lightGardenMaterial != null && darkGardenMaterial != null)
            {
                foreach (Renderer renderer in lightRenderers)
                    AssignRendererMaterial(renderer, CreateMaterialInstance(darkGardenMaterial));

                foreach (Renderer renderer in darkRenderers)
                    AssignRendererMaterial(renderer, CreateMaterialInstance(lightGardenMaterial));
            }
        }

        private static Material CreateMaterialInstance(Material source)
        {
            return source != null ? new Material(source) : null;
        }

        private static void AssignRendererMaterial(Renderer renderer, Material material)
        {
            if (renderer == null || material == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                renderer.sharedMaterial = material;
            else
                renderer.material = material;
#else
            renderer.material = material;
#endif
        }

        private static bool IsGardenLightRenderer(Renderer renderer)
        {
            string objectName = renderer.gameObject.name;
            return !string.IsNullOrEmpty(objectName) &&
                   objectName.IndexOf("gardenlight", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsGardenDarkRenderer(Renderer renderer)
        {
            string objectName = renderer.gameObject.name;
            return !string.IsNullOrEmpty(objectName) &&
                   objectName.IndexOf("gardendark", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void LightenMaterial(Material material, float amount)
        {
            if (material == null || amount <= 0f)
                return;

            if (material.HasProperty("_BaseColor"))
            {
                Color color = material.GetColor("_BaseColor");
                material.SetColor("_BaseColor", Color.Lerp(color, Color.white, amount));
            }
            else
            {
                material.color = Color.Lerp(material.color, Color.white, amount);
            }

            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", Mathf.Min(material.GetFloat("_Smoothness") + 0.08f, 0.55f));
        }

        private static bool IsNeutralGardenRenderer(Renderer renderer)
        {
            string objectName = renderer.gameObject.name;
            if (string.IsNullOrEmpty(objectName))
                return false;

            if (objectName.IndexOf("gardenneutral", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return objectName.IndexOf("neutral", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                   objectName.IndexOf("garden", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static void SetMaterialTiling(Material material, float tiling)
        {
            if (material == null)
                return;

            Vector2 scale = new Vector2(tiling, tiling);
            material.mainTextureScale = scale;

            if (material.HasProperty("_BaseMap"))
                material.SetTextureScale("_BaseMap", scale);
        }

        private static Bounds CalculateRendererBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(root.transform.position, Vector3.one);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }

        private static void StripColliders(GameObject root)
        {
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
                Object.Destroy(collider);
        }

        public static GameObject CreatePhotoBoardSurface(BoardLayout layout, Texture2D texture)
        {
            var surfaceRoot = new GameObject("BoardPhotoSurface");
            if (layout == null || texture == null)
                return surfaceRoot;

            float worldSize = layout.GetPhotoBoardWorldSize();
            float planeScale = worldSize / 10f;
            float surfaceY = layout.TileHeight - 0.018f;

            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "BoardPhotoPlane";
            plane.transform.SetParent(surfaceRoot.transform, false);
            plane.transform.localScale = new Vector3(planeScale, 1f, planeScale);
            plane.transform.localPosition = new Vector3(0f, surfaceY, 0f);
            DestroyCollider(plane);

            Material material = CreateBoardPhotoMaterial(texture, layout.PhotoUvScale, layout.PhotoUvOffset);
            plane.GetComponent<Renderer>().material = material;

            return surfaceRoot;
        }

        public static Material CreateBoardPhotoMaterial(Texture2D texture, float uvScale, Vector2 uvOffset)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                         ?? Shader.Find("Standard");

            var material = new Material(shader);
            material.mainTexture = texture;

            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);

            Vector2 scale = new Vector2(uvScale, uvScale);
            material.mainTextureScale = scale;
            material.mainTextureOffset = uvOffset;

            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.34f);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 0.04f);
            if (material.HasProperty("_EnvironmentReflections"))
                material.SetFloat("_EnvironmentReflections", 1f);

            return material;
        }

        public static float GetPhotoRimDiameter(BoardLayout layout)
        {
            return layout != null ? layout.GetPhotoBoardWorldSize() : 7.56f;
        }

        private static void CreateGridLine(Transform parent, Vector3 from, Vector3 to, float width)
        {
            Vector3 delta = to - from;
            delta.y = 0f;
            float length = delta.magnitude;
            if (length < 0.001f)
                return;

            var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "GridLine";
            line.transform.SetParent(parent, false);
            line.transform.position = (from + to) * 0.5f;
            line.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            line.transform.localScale = new Vector3(width, 0.006f, length);
            DestroyCollider(line);
            ApplyWood(line.GetComponent<Renderer>(), GridLineWood, 0.2f);
        }

        private static void RefreshFlowerInlayColors(Transform root, PieceType type)
        {
            Color flowerColor = GetFlowerAccent(type);
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (!IsFlowerPart(renderer.gameObject))
                    continue;

                Color tint = renderer.gameObject.name == "FlowerFace"
                    ? Color.Lerp(flowerColor, Color.white, 0.6f)
                    : Color.Lerp(flowerColor, Color.white, 0.25f);
                ApplyFlowerColor(renderer, tint);
            }
        }

        private static void RaiseProceduralInlays(Transform root)
        {
            // Legacy no-op; cosmetics are rebuilt with correct height in PreparePlacedTile.
        }

        private static void NormalizeTileScale(Transform root, float targetDiameter)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;

            Bounds bounds = default;
            bool hasBounds = false;

            foreach (var renderer in renderers)
            {
                if (IsFlowerPart(renderer.gameObject))
                    continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (!hasBounds)
                return;

            float diameter = Mathf.Max(bounds.size.x, bounds.size.z);
            if (diameter < 0.001f)
                return;

            if (diameter > targetDiameter * 1.08f || diameter < targetDiameter * 0.82f)
            {
                float factor = targetDiameter / diameter;
                root.localScale *= factor;
            }
        }

        private static float GetMeshTopLocalY(Transform root)
        {
            float maxY = 0f;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (IsFlowerPart(renderer.gameObject))
                    continue;

                float localTop = root.InverseTransformPoint(renderer.bounds.max).y;
                if (localTop > maxY)
                    maxY = localTop;
            }

            return maxY;
        }

        private static void TintPrefabBody(Transform root, Player owner)
        {
            Color bodyColor = Color.Lerp(GetOwnerWood(owner), TileWoodBase, 0.35f);

            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (IsFlowerPart(renderer.gameObject))
                    continue;

                ApplyWood(renderer, bodyColor, 0.42f);
            }
        }

        private static bool IsFlowerPart(GameObject obj)
        {
            if (IsFlowerRenderer(obj.name))
                return true;

            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                if (parent.name == "FlowerCosmetics")
                    return true;
                parent = parent.parent;
            }

            return false;
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>())
            {
                if (child.name == childName)
                    return child;
            }

            return null;
        }

        private static void DestroyCollider(GameObject obj)
        {
            var collider = obj.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);
        }

        private static Color Hex(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color color))
                return color;
            return Color.white;
        }
    }

    public static class BoardTextureLoader
    {
        public const string ResourceName = "BoardPhoto";
        public const string AssetPath = "Assets/Textures/BoardPhoto.png";

        public static Texture2D Load()
        {
            Texture2D texture = Resources.Load<Texture2D>(ResourceName);
#if UNITY_EDITOR
            if (texture == null)
                texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(AssetPath);
#endif
            return texture;
        }

        public static bool IsAvailable()
        {
            return Load() != null;
        }
    }

    public static class BoardVisualLoader
    {
        public const string ModelResourceName = "Board/Board";
        public const string ModelAssetPath = "Assets/Models/Board/Board.glb";
        public const string ModelResourcesPath = "Assets/Resources/Board/Board.glb";
        public const string BlendAssetPath = "Assets/BlenderFiles/Board/Board.blend";

        public static bool TryLoadModel(out GameObject modelRoot, out string sourcePath)
        {
            sourcePath = null;
            modelRoot = null;

            string[] paths =
            {
                ModelAssetPath,
                ModelResourcesPath,
                BlendAssetPath
            };

            foreach (string path in paths)
            {
                PieceVisualLoader.ClearCacheEntry(path);
                if (!TryLoadFullBoardVisual(path, out GameObject visual))
                    continue;

                sourcePath = path;
                modelRoot = visual;
                return true;
            }

            return false;
        }

        private static bool TryLoadFullBoardVisual(string assetPath, out GameObject visual)
        {
            visual = null;
            if (string.IsNullOrEmpty(assetPath))
                return false;

#if UNITY_EDITOR
            if (!System.IO.File.Exists(assetPath))
                return false;

            UnityEditor.AssetDatabase.ImportAsset(assetPath, UnityEditor.ImportAssetOptions.ForceUpdate);
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
                return false;

            return ValidateBoardPrefab(prefab, out visual);
#else
            string resourceKey = ToResourcesKey(assetPath);
            if (string.IsNullOrEmpty(resourceKey))
                return false;

            GameObject prefab = Resources.Load<GameObject>(resourceKey);
            if (prefab == null)
                return false;

            return ValidateBoardPrefab(prefab, out visual);
#endif
        }

        private static string ToResourcesKey(string assetPath)
        {
            string normalized = assetPath.Replace('\\', '/');
            const string resourcesPrefix = "Assets/Resources/";
            if (!normalized.StartsWith(resourcesPrefix))
                return null;

            string key = normalized.Substring(resourcesPrefix.Length);
            int extension = key.LastIndexOf('.');
            if (extension > 0)
                key = key.Substring(0, extension);

            return key;
        }

        private static bool ValidateBoardPrefab(GameObject prefab, out GameObject visual)
        {
            visual = null;
            GameObject probe = Object.Instantiate(prefab);
            try
            {
                if (!ContainsBoardPart(probe, "BoardLines"))
                    return false;

                visual = prefab;
                return true;
            }
            finally
            {
#if UNITY_EDITOR
                Object.DestroyImmediate(probe);
#else
                Object.Destroy(probe);
#endif
            }
        }

        private static bool ContainsBoardPart(GameObject root, string partName)
        {
            if (root == null || string.IsNullOrEmpty(partName))
                return false;

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.Equals(partName, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static bool IsModelAvailable()
        {
            return TryLoadModel(out _, out _);
        }
    }
}
