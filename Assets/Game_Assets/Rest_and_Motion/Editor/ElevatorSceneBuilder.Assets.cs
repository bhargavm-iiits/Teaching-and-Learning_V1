using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TeachingAndLearning.RestAndMotion.Editor
{
    public static partial class ElevatorSceneBuilder
    {
        const string CabinSource = "Assets/Rest_and_Motion/sci-fi-elevator/source/winda.fbx";
        const string PanelSource = "Assets/Rest_and_Motion/elevator---elevator-key-5-mb/source/Elevator _ elevator key.fbx";
        const string CitySource = "Assets/Rest_and_Motion/residential_building_set.glb";
        const string PanelTextures = "Assets/Rest_and_Motion/elevator---elevator-key-5-mb/textures/Untitled_1_DefaultMaterial_";

        static void PrepareSources()
        {
            foreach (var path in new[] { CabinSource, PanelSource, CitySource })
                if (!File.Exists(path)) throw new FileNotFoundException("The supplied model is required; no placeholder will be generated.", path);
            foreach (var folder in new[] { ImportedPath, ImportedPath + "/Materials", ImportedPath + "/Textures", ImportedPath + "/ResidentialCity", ImportedPath + "/Previews" })
            {
                if (AssetDatabase.IsValidFolder(folder)) continue;
                AssetDatabase.CreateFolder(Path.GetDirectoryName(folder).Replace('\\', '/'), Path.GetFileName(folder));
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CabinSource) == null || AssetDatabase.LoadAssetAtPath<GameObject>(PanelSource) == null)
                throw new InvalidOperationException("Unity must finish importing the supplied FBX models before rebuilding this lesson.");
        }

        static Transform BuildImportedWorld(Transform parent, out Bounds cabinBounds)
        {
            var cabin = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CabinSource), parent);
            cabin.name = "Supplied Cabin - winda.fbx";
            DisableEmbeddedDevices(cabin);
            FitModel(cabin, 3.5f, Vector3.zero, 0f);
            PreserveMaterials(cabin, "Cabin", "Assets/Rest_and_Motion/sci-fi-elevator/textures/metal_texture.jpeg");
            cabinBounds = RenderedBounds(cabin);
            AdaptObservationWindow(cabin, cabinBounds);

            var panel = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PanelSource), parent);
            panel.name = "Supplied Controls - Elevator _ elevator key.fbx";
            DisableEmbeddedDevices(panel);
            FitModel(panel, 2.05f, new Vector3(cabinBounds.max.x - 0.18f, 0.55f, 0.35f), -90f);
            PreserveMaterials(panel, "ControlPanel", PanelTextures + "BaseColor.png", PanelTextures + "Normal.png", PackMetallicRoughness());

            var exterior = new GameObject("ExteriorCity - moves down as the lift rises").transform;
            exterior.SetParent(parent, false);
            GameObject city;
            var cityPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ImportedPath + "/ResidentialCity.prefab");
            if (cityPrefab != null && Environment.GetCommandLineArgs().All(arg => arg != "-ramReimportCity"))
            {
                city = (GameObject)PrefabUtility.InstantiatePrefab(cityPrefab, exterior);
                Debug.Log("[Rest and Motion] Reused the converted residential city prefab.");
            }
            else
            {
                int sourcePart = 0;
                var result = RamGlbImporter.Build(new RamGlbImporter.Spec
                {
                    name = "SuppliedResidentialCity",
                    source = CitySource,
                    length = 60f,
                    pivotAtNose = false,
                    meshLods = 4,
                    part = primitive => SafeName(primitive.node) + "_Source_" + (sourcePart++).ToString("D2")
                }, ImportedPath + "/ResidentialCity", null);
                result.root.name = "Supplied City - residential_building_set.glb";
                result.root.transform.SetParent(exterior, false);
                result.root.transform.localPosition = new Vector3(-result.bounds.center.x, -16f, 36f);
                PrefabUtility.SaveAsPrefabAssetAndConnect(result.root, ImportedPath + "/ResidentialCity.prefab", InteractionMode.AutomatedAction);
                city = result.root;
                Debug.Log("[Rest and Motion] Imported " + result.triangles + " triangles directly from " + CitySource);
            }
            TintCity(city);
            return exterior;
        }

        /// <summary>Warm pastel tints (material copies) so the supplied towers read as separate buildings.</summary>
        static void TintCity(GameObject city)
        {
            var tints = new[]
            {
                new Color(1f, 0.93f, 0.8f), new Color(1f, 0.82f, 0.7f), new Color(0.86f, 0.92f, 1f),
                new Color(0.95f, 0.88f, 0.98f), new Color(0.88f, 0.97f, 0.88f), new Color(1f, 0.97f, 0.86f)
            };
            int index = 0;
            foreach (var renderer in city.GetComponentsInChildren<MeshRenderer>())
            {
                var slots = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i] == null || !slots[i].name.EndsWith("_ground") || renderer.bounds.size.y < 4f) continue;
                    var tint = tints[index % tints.Length];
                    string path = ImportedPath + "/Materials/CityTint_" + (index % tints.Length) + ".mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material == null)
                    {
                        material = new Material(slots[i]);
                        AssetDatabase.CreateAsset(material, path);
                    }
                    else material.CopyPropertiesFromMaterial(slots[i]);
                    var baseColor = slots[i].HasProperty("_BaseColor") ? slots[i].GetColor("_BaseColor") : Color.white;
                    material.SetColor("_BaseColor", baseColor * tint);
                    EditorUtility.SetDirty(material);
                    slots[i] = material;
                    changed = true;
                }
                if (changed) { renderer.sharedMaterials = slots; index++; }
            }
        }

        static void DisableEmbeddedDevices(GameObject model)
        {
            foreach (var camera in model.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
            foreach (var listener in model.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
            foreach (var light in model.GetComponentsInChildren<Light>(true)) light.enabled = false;
            foreach (var animator in model.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
        }

        static Bounds RenderedBounds(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new InvalidOperationException("Supplied model has no renderers: " + model.name);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        static void FitModel(GameObject model, float height, Vector3 floorCenter, float yaw)
        {
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var bounds = RenderedBounds(model);
            model.transform.localScale *= height / Mathf.Max(bounds.size.y, 0.0001f);
            bounds = RenderedBounds(model);
            model.transform.position += floorCenter - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        static void PreserveMaterials(GameObject model, string prefix, string baseMapPath, string normalMapPath = null, Texture2D packedMap = null)
        {
            Texture2D normalMap = null;
            if (normalMapPath != null)
            {
                string normalCopy = ImportedPath + "/Textures/" + prefix + "_Normal.png";
                File.Copy(normalMapPath, normalCopy, true);
                AssetDatabase.ImportAsset(normalCopy, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(normalCopy);
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
                normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>(normalCopy);
            }
            var converted = new Dictionary<Material, Material>();
            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                var slots = renderer.sharedMaterials;
                for (int index = 0; index < slots.Length; index++)
                {
                    var source = slots[index];
                    if (source == null) throw new InvalidOperationException("A supplied model is missing its material: " + model.name);
                    if (!converted.TryGetValue(source, out var material))
                    {
                        string materialPath = ImportedPath + "/Materials/" + prefix + "_" + SafeName(source.name) + "_URP.mat";
                        material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                        if (material == null)
                        {
                            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                            AssetDatabase.CreateAsset(material, materialPath);
                        }
                        var originalMap = source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : source.mainTexture;
                        material.SetTexture("_BaseMap", originalMap != null ? originalMap : AssetDatabase.LoadAssetAtPath<Texture2D>(baseMapPath));
                        material.SetColor("_BaseColor", source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white);
                        material.SetFloat("_Smoothness", packedMap != null ? 1f : 0.3f);
                        if (normalMap != null)
                        {
                            material.SetTexture("_BumpMap", normalMap);
                            material.EnableKeyword("_NORMALMAP");
                        }
                        if (packedMap != null)
                        {
                            material.SetTexture("_MetallicGlossMap", packedMap);
                            material.EnableKeyword("_METALLICSPECGLOSSMAP");
                        }
                        material.SetFloat("_Cull", (float)CullMode.Off);
                        EditorUtility.SetDirty(material);
                        converted.Add(source, material);
                    }
                    slots[index] = material;
                }
                renderer.sharedMaterials = slots;
            }
        }

        static Texture2D PackMetallicRoughness()
        {
            var metallic = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            var roughness = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            metallic.LoadImage(File.ReadAllBytes(PanelTextures + "Metallic.png"));
            roughness.LoadImage(File.ReadAllBytes(PanelTextures + "Roughness.png"));
            var pixels = metallic.GetPixels32();
            for (int index = 0; index < pixels.Length; index++)
            {
                int column = index % metallic.width;
                int row = index / metallic.width;
                byte smoothness = (byte)Mathf.RoundToInt(255f * (1f - roughness.GetPixelBilinear((column + 0.5f) / metallic.width, (row + 0.5f) / metallic.height).r));
                pixels[index] = new Color32(pixels[index].r, 0, 0, smoothness);
            }
            metallic.SetPixels32(pixels);
            metallic.Apply();
            string path = ImportedPath + "/Textures/ControlPanel_MetallicSmoothness.png";
            File.WriteAllBytes(path, metallic.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(metallic);
            UnityEngine.Object.DestroyImmediate(roughness);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.sRGBTexture = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void AdaptObservationWindow(GameObject cabin, Bounds bounds)
        {
            var glass = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            glass.SetColor("_BaseColor", new Color(0.75f, 0.9f, 1f, 0.06f));
            glass.SetFloat("_Surface", 1f);
            glass.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            glass.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            glass.SetFloat("_ZWrite", 0f);
            glass.SetFloat("_Cull", (float)CullMode.Off);
            glass.SetOverrideTag("RenderType", "Transparent");
            glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            glass.renderQueue = (int)RenderQueue.Transparent;
            glass = SaveAsset(glass, ImportedPath + "/Materials/ObservationGlass.mat");
            int windowTriangles = 0;
            foreach (var filter in cabin.GetComponentsInChildren<MeshFilter>())
            {
                var source = filter.sharedMesh;
                var mesh = UnityEngine.Object.Instantiate(source);
                var vertices = source.vertices;
                var transparent = new List<int>();
                var opaqueSlots = new List<int[]>();
                for (int slot = 0; slot < source.subMeshCount; slot++)
                {
                    var opaque = new List<int>();
                    var indices = source.GetTriangles(slot);
                    for (int index = 0; index < indices.Length; index += 3)
                    {
                        var first = filter.transform.TransformPoint(vertices[indices[index]]);
                        var second = filter.transform.TransformPoint(vertices[indices[index + 1]]);
                        var third = filter.transform.TransformPoint(vertices[indices[index + 2]]);
                        var center = (first + second + third) / 3f;
                        var normal = Vector3.Cross(second - first, third - first).normalized;
                        bool isWindow = center.z > bounds.center.z + bounds.size.z * 0.18f && center.y > bounds.min.y + 0.35f && center.y < bounds.max.y - 0.35f && Mathf.Abs(normal.z) > 0.55f;
                        var destination = isWindow ? transparent : opaque;
                        destination.Add(indices[index]);
                        destination.Add(indices[index + 1]);
                        destination.Add(indices[index + 2]);
                    }
                    opaqueSlots.Add(opaque.ToArray());
                }
                mesh.subMeshCount = source.subMeshCount + 1;
                for (int slot = 0; slot < opaqueSlots.Count; slot++) mesh.SetTriangles(opaqueSlots[slot], slot);
                mesh.SetTriangles(transparent, source.subMeshCount);
                filter.sharedMesh = SaveAsset(mesh, ImportedPath + "/" + SafeName(source.name) + "_Observation.asset");
                var renderer = filter.GetComponent<Renderer>();
                var slots = new List<Material>(renderer.sharedMaterials) { glass };
                renderer.sharedMaterials = slots.ToArray();
                windowTriangles += transparent.Count / 3;
            }
            if (windowTriangles == 0) throw new InvalidOperationException("Could not locate the supplied cabin's front faces for the observation window.");
            Debug.Log("[Rest and Motion] Retained source cabin geometry; assigned glass to " + windowTriangles + " front triangles.");
        }

        static AssetType SaveAsset<AssetType>(AssetType asset, string path) where AssetType : UnityEngine.Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<AssetType>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(asset, path);
                return asset;
            }
            EditorUtility.CopySerialized(asset, existing);
            UnityEngine.Object.DestroyImmediate(asset);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        static string SafeName(string name)
        {
            foreach (char character in Path.GetInvalidFileNameChars()) name = name.Replace(character, '_');
            return name;
        }

        static TMP_Text WorldText(Transform parent, string name, string value, Vector3 position, float fontSize, Color color)
        {
            var text = new GameObject(name, typeof(TextMeshPro)).GetComponent<TextMeshPro>();
            text.transform.SetParent(parent, false);
            text.transform.localPosition = position;
            text.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.rectTransform.sizeDelta = new Vector2(0.42f, 0.2f);
            return text;
        }

        static Sprite[] CapturePreviews(Camera camera, Transform exterior)
        {
            var sprites = new Sprite[3];
            var position = camera.transform.position;
            var rect = camera.rect;
            var cityPosition = exterior.position;
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var target = new RenderTexture(760, 292, 24);
            try
            {
                camera.rect = new Rect(0f, 0f, 1f, 1f);
                camera.targetTexture = target;
                for (int index = 0; index < 3; index++)
                {
                    camera.transform.position = position + (index == 0 ? Vector3.zero : Vector3.forward * 1.1f);
                    exterior.position = cityPosition + (index == 2 ? Vector3.down * 5f : Vector3.zero);
                    camera.Render();
                    RenderTexture.active = target;
                    var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                    image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                    image.Apply();
                    string path = ImportedPath + "/Previews/Step" + (index + 1) + ".png";
                    File.WriteAllBytes(path, image.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(image);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();
                    sprites[index] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
            }
            finally
            {
                camera.transform.position = position;
                exterior.position = cityPosition;
                camera.rect = rect;
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
            return sprites;
        }

        static void WriteAssetReport()
        {
            Directory.CreateDirectory("Docs");
            File.WriteAllText("Docs/rest_and_motion_assets.md", "# Rest and Motion asset sources\n\n- Cabin: `" + CabinSource + "`\n- Control panel: `" + PanelSource + "`\n- City, buildings and ground: `" + CitySource + "`\n\nThe FBX instances remain linked to their source models. URP material copies preserve the supplied color, normal, metallic and roughness textures. The residential GLB is converted with the existing project importer, retaining its vertex data, UVs and materials.\n\nThe observation cabin uses a copy of the supplied mesh with front triangles assigned a transparent glass material. Its vertices and source FBX are unchanged. Derived Unity meshes, material conversions, packed texture channels and scene previews are stored in `Assets/Rest_and_Motion/Imported`. No primitive substitute cabin, controls, buildings, trees, mountains or hands are generated.\n");
        }
    }
}

