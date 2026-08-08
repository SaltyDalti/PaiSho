using System.Collections.Generic;
using PaiSho.Game;
using PaiSho.Pieces;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PaiSho
{
    public static class PieceMaterialUtility
    {
        private static Shader urpLitShader;

        private static Shader UrpLitShader
        {
            get
            {
                if (urpLitShader == null)
                {
                    urpLitShader = Shader.Find("Universal Render Pipeline/Lit")
                                ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                                ?? Shader.Find("Standard");
                }

                return urpLitShader;
            }
        }

        public static void EnsureMaterials(
            GameObject root,
            string sourceAssetPath = null,
            bool liftFlowerFaces = true,
            System.Func<Transform, bool> skipRenderer = null)
        {
            if (root == null)
                return;

            Material[] fallbackMaterials = LoadSourceMaterials(sourceAssetPath);
            int fallbackIndex = 0;

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || (skipRenderer != null && skipRenderer(renderer.transform)))
                    continue;

                if (WoodTheme.IsSquareBoardUnderlayName(renderer.gameObject.name))
                    continue;

                Material[] materials = renderer.sharedMaterials;
                bool changed = false;

                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (source == null && fallbackMaterials != null && fallbackMaterials.Length > 0)
                    {
                        source = PickFallbackMaterial(renderer, fallbackMaterials, ref fallbackIndex);
                    }

                    Material converted = ToUrpMaterial(source);
                    if (!ReferenceEquals(converted, source))
                        changed = true;

                    materials[i] = converted;
                }

                if (changed)
                    renderer.sharedMaterials = materials;
            }

            if (liftFlowerFaces)
                EnsureFlowerFacesVisible(root, skipRenderer);
        }

        public static void ApplyPieceTheme(GameObject root, PieceType type, Player owner)
        {
            if (root == null)
                return;

            Material hostBodyTemplate = FindFaceBodyTemplate(root) ?? FindThemedMaterial(root, "ceramic", "stone", "marble", "hostbody");
            Material opponentBodyTemplate = FindTileBodyTemplate(root) ?? FindThemedMaterial(root, "terracotta", "terracota", "opponentbody");
            Color bodyColor = WoodTheme.GetOwnerBodyColor(owner, type);
            float smoothness = WoodTheme.GetOwnerBodySmoothness(owner);

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                if (IsInlayRenderer(renderer))
                    continue; // keep authored face/inlay colors from Blender export

                Material bodyTemplate = owner == Player.Host ? hostBodyTemplate : opponentBodyTemplate;
                if (bodyTemplate == null)
                    bodyTemplate = opponentBodyTemplate ?? hostBodyTemplate;

                bool distinctTemplates = hostBodyTemplate != null &&
                                         opponentBodyTemplate != null &&
                                         hostBodyTemplate != opponentBodyTemplate;

                if (bodyTemplate != null && HasAlbedoTexture(bodyTemplate))
                {
                    WoodTheme.ApplyTexturedTileBody(
                        renderer, bodyTemplate, owner, type, smoothness, distinctTemplates);
                }
                else if (HasAlbedoTexture(renderer))
                {
                    WoodTheme.ApplyTexturedTileBody(
                        renderer, renderer.sharedMaterial, owner, type, smoothness, distinctTemplates: false);
                }
                else
                    WoodTheme.ApplyTileBodyColor(renderer, bodyColor, smoothness);
            }
        }

        private static Material FindTileBodyTemplate(GameObject root)
        {
            if (root == null)
                return null;

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || IsInlayRenderer(renderer))
                    continue;

                foreach (Material material in renderer.sharedMaterials)
                {
                    if (HasAlbedoTexture(material))
                        return material;
                }
            }

            return null;
        }

        private static Material FindFaceBodyTemplate(GameObject root)
        {
            if (root == null)
                return null;

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !IsInlayRenderer(renderer))
                    continue;

                foreach (Material material in renderer.sharedMaterials)
                {
                    if (HasAlbedoTexture(material))
                        return material;
                }
            }

            return null;
        }

        public static bool HasAlbedoTexture(Renderer renderer)
        {
            if (renderer == null)
                return false;

            foreach (Material material in renderer.sharedMaterials)
            {
                if (HasAlbedoTexture(material))
                    return true;
            }

            return false;
        }

        public static bool HasAlbedoTexture(Material material)
        {
            if (material == null)
                return false;

            if (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null)
                return true;

            return material.mainTexture != null;
        }

        private static Material FindThemedMaterial(GameObject root, params string[] keywords)
        {
            if (root == null || keywords == null || keywords.Length == 0)
                return null;

            Material best = null;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null || !HasAlbedoTexture(material))
                        continue;

                    string name = material.name.ToLowerInvariant();
                    foreach (string keyword in keywords)
                    {
                        if (name.Contains(keyword))
                            return material;
                    }

                    best ??= material;
                }
            }

            return best;
        }

        public static void EnsureFlowerFacesVisible(GameObject root, System.Func<Transform, bool> skipRenderer = null)
        {
            if (root == null)
                return;

            Transform rootTransform = root.transform;
            float bodyTopLocalY = GetBodyTopLocalY(root, skipRenderer);

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || (skipRenderer != null && skipRenderer(renderer.transform)))
                    continue;

                if (!IsInlayRenderer(renderer))
                    continue;

                Transform faceTransform = renderer.transform;
                float faceBottomLocalY = GetRendererExtremeYInRootLocal(renderer, rootTransform, findMax: false);
                const float desiredGap = 0.004f;
                float correction = bodyTopLocalY - faceBottomLocalY + desiredGap;
                if (Mathf.Abs(correction) < 0.0005f)
                    continue;

                faceTransform.localPosition += Vector3.up * correction;

                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    if (material == null)
                        continue;

                    material.renderQueue = 2001;
                    if (material.HasProperty("_Cull"))
                        material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                    if (material.HasProperty("_OffsetFactor"))
                        material.SetFloat("_OffsetFactor", -2f);
                    if (material.HasProperty("_OffsetUnits"))
                        material.SetFloat("_OffsetUnits", -2f);
                }
            }
        }

        private static float GetBodyTopLocalY(GameObject root, System.Func<Transform, bool> skipRenderer = null)
        {
            Transform rootTransform = root.transform;
            float top = float.NegativeInfinity;

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || (skipRenderer != null && skipRenderer(renderer.transform)))
                    continue;

                if (IsInlayRenderer(renderer))
                    continue;

                float localTop = GetRendererExtremeYInRootLocal(renderer, rootTransform, findMax: true);
                if (localTop > top)
                    top = localTop;
            }

            return top;
        }

        /// <summary>
        /// Min or max Y of mesh corners in the tile root's local space.
        /// Uses oriented bounds so parent stand/slot rotation cannot skew face lift.
        /// </summary>
        private static float GetRendererExtremeYInRootLocal(Renderer renderer, Transform root, bool findMax)
        {
            Bounds bounds = renderer.localBounds;
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;
            float extreme = findMax ? float.NegativeInfinity : float.PositiveInfinity;
            Transform rendererTransform = renderer.transform;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 rendererLocal = center + Vector3.Scale(extents, new Vector3(x, y, z));
                        Vector3 world = rendererTransform.TransformPoint(rendererLocal);
                        float rootLocalY = root.InverseTransformPoint(world).y;
                        extreme = findMax
                            ? Mathf.Max(extreme, rootLocalY)
                            : Mathf.Min(extreme, rootLocalY);
                    }
                }
            }

            return extreme;
        }

        public static bool IsInlayRenderer(Renderer renderer)
        {
            if (renderer == null)
                return false;

            string objectName = renderer.gameObject.name;
            if (WoodTheme.IsFlowerRenderer(objectName))
                return true;

            if (objectName.IndexOf("inlay", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (objectName.IndexOf("face", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null)
                    continue;

                string materialName = material.name.ToLowerInvariant();
                if (materialName.Contains("hostbody_ceramic") ||
                    materialName.Contains("engrav") ||
                    materialName.Contains("hj"))
                {
                    return true;
                }
            }

            return false;
        }

        private static Material PickFallbackMaterial(Renderer renderer, Material[] fallbacks, ref int roundRobin)
        {
            string objectName = renderer.gameObject.name.ToLowerInvariant();
            bool wantsFace = objectName.Contains("face");

            if (objectName == "board")
            {
                foreach (Material candidate in fallbacks)
                {
                    if (candidate == null)
                        continue;

                    if (candidate.name.IndexOf("Board_Baked", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return candidate;
                }
            }

            Material bodyMaterial = null;
            Material faceMaterial = null;

            foreach (Material candidate in fallbacks)
            {
                if (candidate == null)
                    continue;

                string materialName = candidate.name.ToLowerInvariant();
                if (materialName.Contains("face") || materialName.Contains("hj"))
                    faceMaterial = candidate;
                else if (bodyMaterial == null)
                    bodyMaterial = candidate;
            }

            if (wantsFace && faceMaterial != null)
                return faceMaterial;

            if (!wantsFace && bodyMaterial != null)
                return bodyMaterial;

            Material picked = fallbacks[roundRobin % fallbacks.Length];
            roundRobin++;
            return picked;
        }

        public static Material ToUrpMaterial(Material source)
        {
            if (source != null && !NeedsConversion(source))
            {
                EnsureEmissionCapable(source);
                return source;
            }

            var material = new Material(UrpLitShader);

            if (source == null)
            {
                SetBaseColor(material, new Color(0.82f, 0.72f, 0.55f));
                SetSmoothness(material, 0.42f);
                SetMetallic(material, 0.05f);
                return material;
            }

            Texture mainTexture = GetTexture(source, "_BaseMap", "_MainTex", "baseColorTexture");
            Color baseColor = GetColor(source, "_BaseColor", "_Color", "baseColorFactor");

            if (mainTexture != null)
            {
                SetTexture(material, mainTexture);
                SetBaseColor(material, Color.white);
            }
            else
            {
                SetBaseColor(material, baseColor == default ? Color.white : baseColor);
            }

            Texture normalMap = GetTexture(source, "_BumpMap", "_NormalMap", "normalTexture");
            if (normalMap != null)
                SetNormalMap(material, normalMap);

            SetMetallic(material, GetFloat(source, 0.05f, "_Metallic", "metallicFactor"));
            SetSmoothness(material, GetFloat(source, 0.42f, "_Smoothness", "_Glossiness", "roughnessFactor"));
            EnsureEmissionCapable(material);

            return material;
        }

        public static void EnsureEmissionCapable(Material material)
        {
            if (material == null || !material.HasProperty("_EmissionColor"))
                return;

            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        public static bool NeedsConversion(Material material)
        {
            if (material == null || material.shader == null)
                return true;

            string shaderName = material.shader.name;
            if (shaderName.Contains("Error") || shaderName.Contains("InternalError"))
                return true;

            if (!material.shader.isSupported)
                return true;

            if (shaderName.StartsWith("Universal Render Pipeline/"))
                return false;

            return shaderName.StartsWith("glTF/")
                || shaderName.StartsWith("Shader Graphs/glTF")
                || shaderName == "Standard"
                || shaderName.Contains("Autodesk")
                || shaderName.StartsWith("Hidden/");
        }

        private static Material[] LoadSourceMaterials(string sourceAssetPath)
        {
            if (string.IsNullOrEmpty(sourceAssetPath))
                return null;

            // Loading .blend sub-assets from editor scripts triggers a Blender import.
            if (sourceAssetPath.EndsWith(".blend", System.StringComparison.OrdinalIgnoreCase))
                return null;

#if UNITY_EDITOR
            Material[] editorMaterials = LoadMaterialsAtAssetPath(sourceAssetPath);
            if (editorMaterials != null && editorMaterials.Length > 0)
                return editorMaterials;
#endif

            return LoadMaterialsFromResources(sourceAssetPath);
        }

        public static Material[] LoadSourceMaterialsForPath(string sourceAssetPath) =>
            LoadSourceMaterials(sourceAssetPath);

        private static Material[] LoadMaterialsFromResources(string sourceAssetPath)
        {
            string resourceKey = ToResourcesKey(sourceAssetPath);
            if (string.IsNullOrEmpty(resourceKey))
                return null;

            Object[] assets = Resources.LoadAll(resourceKey);
            return ExtractMaterials(assets);
        }

#if UNITY_EDITOR
        private static Material[] LoadMaterialsAtAssetPath(string sourceAssetPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(sourceAssetPath);
            return ExtractMaterials(assets);
        }
#endif

        private static Material[] ExtractMaterials(Object[] assets)
        {
            if (assets == null || assets.Length == 0)
                return null;

            var materials = new List<Material>();
            foreach (Object asset in assets)
            {
                if (asset is Material material && material != null)
                    materials.Add(material);
            }

            return materials.Count > 0 ? materials.ToArray() : null;
        }

        private static string ToResourcesKey(string assetPath)
        {
            const string resourcesPrefix = "Assets/Resources/";
            string normalized = assetPath.Replace('\\', '/');
            if (!normalized.StartsWith(resourcesPrefix))
                return null;

            string key = normalized.Substring(resourcesPrefix.Length);
            int extension = key.LastIndexOf('.');
            if (extension > 0)
                key = key.Substring(0, extension);

            return key;
        }

        private static Texture GetTexture(Material material, params string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                if (!material.HasProperty(propertyName))
                    continue;

                Texture texture = material.GetTexture(propertyName);
                if (texture != null)
                    return texture;
            }

            return material.mainTexture;
        }

        private static Color GetColor(Material material, params string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                if (!material.HasProperty(propertyName))
                    continue;

                return material.GetColor(propertyName);
            }

            return material.color;
        }

        private static float GetFloat(Material material, float fallback, params string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                if (!material.HasProperty(propertyName))
                    continue;

                float value = material.GetFloat(propertyName);
                if (propertyName == "roughnessFactor")
                    return Mathf.Clamp01(1f - value);

                return value;
            }

            return fallback;
        }

        private static void SetTexture(Material material, Texture texture)
        {
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);

            material.mainTexture = texture;
        }

        private static void SetBaseColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else
                material.color = color;
        }

        private static void SetNormalMap(Material material, Texture texture)
        {
            if (material.HasProperty("_BumpMap"))
                material.SetTexture("_BumpMap", texture);
        }

        private static void SetSmoothness(Material material, float smoothness)
        {
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);
        }

        private static void SetMetallic(Material material, float metallic)
        {
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);
        }
    }
}
