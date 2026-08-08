using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using PaiSho.Board;

namespace PaiSho
{
    public static class SceneEnvironmentBuilder
    {
        public static void Build(Transform boardRoot, BoardLayout layout)
        {
            if (GameObject.Find("SceneEnvironment") != null)
                return;

            DisableLegacySceneObjects();
            float boardSpan = layout != null ? layout.GridSpan : 8f;
            float roomSize = boardSpan * 3.2f;

            var environment = new GameObject("SceneEnvironment");

            ApplySkyAndAmbient();
            SetupPostProcessing(environment.transform);
            SetupLighting(environment.transform, boardSpan);
            BuildTeaRoom(environment.transform, boardRoot, roomSize, boardSpan);
            SetupReflectionProbe(environment.transform, boardSpan);
            EnableCameraEffects();
        }

        private static void DisableLegacySceneObjects()
        {
            GameObject legacySun = GameObject.Find("Directional Light");
            if (legacySun != null)
                legacySun.SetActive(false);

            GameObject legacyPlane = GameObject.Find("Plane");
            if (legacyPlane != null)
                legacyPlane.SetActive(false);
        }

        private static void ApplySkyAndAmbient()
        {
            Material skybox = Resources.Load<Material>("Scene/CuteSkybox");
            if (skybox != null)
                RenderSettings.skybox = skybox;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.52f, 0.48f, 0.55f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.34f, 0.28f);
            RenderSettings.ambientGroundColor = new Color(0.14f, 0.10f, 0.08f);
            RenderSettings.ambientIntensity = 1.2f;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.005f;
            RenderSettings.fogColor = new Color(0.32f, 0.28f, 0.34f);
        }

        private static void SetupPostProcessing(Transform parent)
        {
            var volumeObject = new GameObject("GlobalVolume");
            volumeObject.transform.SetParent(parent, false);

            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.weight = 1f;

            VolumeProfile profile = Resources.Load<VolumeProfile>("Scene/GamePlayVolumeProfile");
            if (profile != null)
            {
                volume.profile = profile;
                return;
            }

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.profile = profile;

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.42f);
            bloom.threshold.Override(0.78f);
            bloom.scatter.Override(0.72f);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.42f);

            var colorAdjust = profile.Add<ColorAdjustments>(true);
            colorAdjust.postExposure.Override(0.15f);
            colorAdjust.contrast.Override(10f);
            colorAdjust.saturation.Override(10f);
        }

        private static void SetupLighting(Transform parent, float boardSpan)
        {
            var sunObject = new GameObject("Sun");
            sunObject.transform.SetParent(parent, false);
            sunObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.94f, 0.82f);
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.72f;
            sun.shadowBias = 0.05f;

            var fillObject = new GameObject("FillLight");
            fillObject.transform.SetParent(parent, false);
            fillObject.transform.rotation = Quaternion.Euler(18f, 128f, 0f);

            var fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.72f, 0.82f, 1f);
            fill.intensity = 0.28f;
            fill.shadows = LightShadows.None;

            var rimObject = new GameObject("RimLight");
            rimObject.transform.SetParent(parent, false);
            rimObject.transform.rotation = Quaternion.Euler(12f, 210f, 0f);

            var rim = rimObject.AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = new Color(1f, 0.78f, 0.55f);
            rim.intensity = 0.18f;
            rim.shadows = LightShadows.None;

            CreateCandle(parent, new Vector3(-boardSpan * 0.95f, 0.55f, boardSpan * 0.75f));
            CreateCandle(parent, new Vector3(boardSpan * 0.95f, 0.55f, -boardSpan * 0.75f));

            var boardGlow = new GameObject("BoardAccentLight");
            boardGlow.transform.SetParent(parent, false);
            boardGlow.transform.localPosition = new Vector3(0f, boardSpan * 0.55f, 0f);

            var accent = boardGlow.AddComponent<Light>();
            accent.type = LightType.Point;
            accent.color = new Color(1f, 0.92f, 0.75f);
            accent.intensity = 0.65f;
            accent.range = boardSpan * 2.2f;
            accent.shadows = LightShadows.None;

            var life = parent.gameObject.AddComponent<SceneLifeAnimator>();
            life.Configure(accent);
        }

        private static void CreateCandle(Transform parent, Vector3 localPosition)
        {
            var candleRoot = new GameObject("Candle");
            candleRoot.transform.SetParent(parent, false);
            candleRoot.transform.localPosition = localPosition;

            var baseMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseMesh.name = "CandleBase";
            baseMesh.transform.SetParent(candleRoot.transform, false);
            baseMesh.transform.localScale = new Vector3(0.08f, 0.12f, 0.08f);
            baseMesh.transform.localPosition = Vector3.zero;
            Object.Destroy(baseMesh.GetComponent<Collider>());
            WoodTheme.ApplyTexturedWood(baseMesh.GetComponent<Renderer>(), "Scene/dark_oak", WoodTheme.FrameWood, 0.4f, 2f);

            var flame = new GameObject("CandleLight");
            flame.transform.SetParent(candleRoot.transform, false);
            flame.transform.localPosition = new Vector3(0f, 0.18f, 0f);

            var light = flame.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.72f, 0.35f);
            light.intensity = 0.9f;
            light.range = 4.5f;
            light.shadows = LightShadows.Soft;

            var flicker = flame.AddComponent<CandleLightFlicker>();
            flicker.minIntensity = 0.55f;
            flicker.maxIntensity = 1.15f;
            flicker.flickerSpeed = 0.14f;
            flicker.flickerAmount = 0.35f;
        }

        private static void BuildTeaRoom(Transform parent, Transform boardRoot, float roomSize, float boardSpan)
        {
            var room = new GameObject("TeaRoom");
            room.transform.SetParent(parent, false);
            if (boardRoot != null)
                room.transform.position = boardRoot.position;

            float floorY = -0.12f;
            CreateTiledFloor(room.transform, roomSize, floorY, "Scene/Black_Tatami", 6f);
            CreateTiledFloor(room.transform, boardSpan * 1.55f, floorY + 0.002f, "Scene/Light_Blue_Tatami", 3.5f);

            float wallHeight = roomSize * 0.42f;
            float wallThickness = 0.18f;
            float half = roomSize * 0.5f;

            CreateWall(room.transform, new Vector3(0f, wallHeight * 0.5f + floorY, half), new Vector3(roomSize, wallHeight, wallThickness));
            CreateWall(room.transform, new Vector3(0f, wallHeight * 0.5f + floorY, -half), new Vector3(roomSize, wallHeight, wallThickness));
            CreateWall(room.transform, new Vector3(half, wallHeight * 0.5f + floorY, 0f), new Vector3(wallThickness, wallHeight, roomSize));
            CreateWall(room.transform, new Vector3(-half, wallHeight * 0.5f + floorY, 0f), new Vector3(wallThickness, wallHeight, roomSize));

            CreateBeam(room.transform, new Vector3(0f, wallHeight + floorY - 0.08f, half * 0.35f), new Vector3(roomSize * 0.9f, 0.12f, 0.14f));
            CreateBeam(room.transform, new Vector3(0f, wallHeight + floorY - 0.08f, -half * 0.35f), new Vector3(roomSize * 0.9f, 0.12f, 0.14f));

            EnhanceTable(boardRoot, boardSpan);
        }

        private static void EnhanceTable(Transform boardRoot, float boardSpan)
        {
            if (boardRoot == null)
                return;

            Transform table = boardRoot.Find("TableWood");
            if (table != null)
            {
                var renderer = table.GetComponent<Renderer>();
                WoodTheme.ApplyTexturedWood(renderer, "Scene/dark_wood", WoodTheme.TableWood, 0.42f, 3f);
            }

            float tableDiameter = boardSpan * 1.35f;
            var tableLeg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tableLeg.name = "TablePedestal";
            tableLeg.transform.SetParent(boardRoot, false);
            float pedestalHeight = 0.35f;
            tableLeg.transform.localScale = new Vector3(tableDiameter * 0.22f, pedestalHeight, tableDiameter * 0.22f);
            // Keep the pedestal fully under the board — a centered cylinder poking through reads as a black disc.
            float pedestalTop = -0.08f;
            tableLeg.transform.localPosition = new Vector3(0f, pedestalTop - pedestalHeight, 0f);
            Object.Destroy(tableLeg.GetComponent<Collider>());
            WoodTheme.ApplyTexturedWood(tableLeg.GetComponent<Renderer>(), "Scene/dark_oak", WoodTheme.FrameWood, 0.38f, 2.5f);
        }

        private static void CreateTiledFloor(Transform parent, float size, float y, string texturePath, float tileWorldSize)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = $"Floor_{texturePath}";
            floor.transform.SetParent(parent, false);
            float scale = size / 10f;
            floor.transform.localScale = new Vector3(scale, 1f, scale);
            floor.transform.localPosition = new Vector3(0f, y, 0f);
            Object.Destroy(floor.GetComponent<Collider>());

            var renderer = floor.GetComponent<Renderer>();
            WoodTheme.ApplyTiledSurface(renderer, texturePath, 0.28f, size / tileWorldSize);
        }

        private static void CreateWall(Transform parent, Vector3 position, Vector3 size)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = position;
            wall.transform.localScale = size;
            Object.Destroy(wall.GetComponent<Collider>());
            WoodTheme.ApplyTexturedWood(wall.GetComponent<Renderer>(), "Scene/dark_wood", WoodTheme.FrameWood, 0.32f, 1.8f);
        }

        private static void CreateBeam(Transform parent, Vector3 position, Vector3 size)
        {
            var beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.name = "CeilingBeam";
            beam.transform.SetParent(parent, false);
            beam.transform.localPosition = position;
            beam.transform.localScale = size;
            Object.Destroy(beam.GetComponent<Collider>());
            WoodTheme.ApplyTexturedWood(beam.GetComponent<Renderer>(), "Scene/dark_oak", WoodTheme.FrameWood, 0.45f, 2f);
        }

        private static void SetupReflectionProbe(Transform parent, float boardSpan)
        {
            var probeObject = new GameObject("BoardReflectionProbe");
            probeObject.transform.SetParent(parent, false);
            probeObject.transform.localPosition = new Vector3(0f, boardSpan * 0.35f, 0f);

            var probe = probeObject.AddComponent<ReflectionProbe>();
            probe.size = new Vector3(boardSpan * 2.4f, boardSpan * 1.4f, boardSpan * 2.4f);
            probe.resolution = 128;
            probe.shadowDistance = boardSpan;
            probe.intensity = 0.85f;
            probe.boxProjection = true;
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.RenderProbe();
        }

        private static void EnableCameraEffects()
        {
            Camera camera = Camera.main;
            if (camera == null)
                return;

            var data = camera.GetComponent<UniversalAdditionalCameraData>();
            if (data == null)
                data = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();

            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            data.antialiasingQuality = AntialiasingQuality.High;

            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 52f;
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 120f;
        }
    }
}
