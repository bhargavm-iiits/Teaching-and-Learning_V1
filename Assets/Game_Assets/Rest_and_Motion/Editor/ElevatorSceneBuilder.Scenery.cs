using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TeachingAndLearning.RestAndMotion.Editor
{
    /// <summary>
    /// Outside world around the supplied residential city: grass, roads, a river and bridge, street trees,
    /// a mountain ring and clouds (read-only instances of the project's Synty and SimpleNaturePack prefabs),
    /// plus the landing tower the lift serves. Everything is a child of the exterior, which moves down as the lift rises.
    /// </summary>
    public static partial class ElevatorSceneBuilder
    {
        public const float GroundY = -6.4f;
        public const float FloorHeight = 3.2f;
        const string Synty = "Assets/Synty/PolygonGeneric/Prefabs/Environment/";
        const string Nature = "Assets/SimpleNaturePack/Prefabs/";

        static void BuildScenery(Transform exterior)
        {
            var root = new GameObject("Scenery - project art packs (read-only instances)").transform;
            root.SetParent(exterior, false);
            var rng = new System.Random(20261009);

            var grass = GameObject.CreatePrimitive(PrimitiveType.Plane);
            grass.name = "Grass ground";
            grass.transform.SetParent(root, false);
            grass.transform.localPosition = new Vector3(0f, GroundY - 0.03f, 120f);
            grass.transform.localScale = new Vector3(120f, 1f, 120f);
            grass.GetComponent<Renderer>().sharedMaterial = LitMaterial("RamGrass", new Color(0.37f, 0.6f, 0.28f), 0.05f, 0f);
            UnityEngine.Object.DestroyImmediate(grass.GetComponent<Collider>());

            // Main road across the front of the city, and a north-south road over the river bridge.
            var roads = new GameObject("Roads (Synty)").transform;
            roads.SetParent(root, false);
            for (float x = -250f; x <= 260f; x += 10f)
                Place(Synty + "SM_Gen_Env_Road_01.prefab", roads, new Vector3(x, GroundY + 0.03f, 7f), 0f, 1f);
            for (float z = 17f; z < 330f; z += 10f)
            {
                if (z > 78f && z < 130f) continue;
                Place(Synty + "SM_Gen_Env_Road_01.prefab", roads, new Vector3(35f, GroundY + 0.035f, z), 90f, 1f);
            }

            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "River";
            water.transform.SetParent(root, false);
            water.transform.localPosition = new Vector3(0f, GroundY - 0.01f, 105f);
            water.transform.localScale = new Vector3(120f, 1f, 2.2f);
            water.GetComponent<Renderer>().sharedMaterial = LitMaterial("RamRiver", new Color(0.2f, 0.47f, 0.78f), 0.92f, 0f);
            UnityEngine.Object.DestroyImmediate(water.GetComponent<Collider>());
            BuildBridge(root);

            // Trees: street trees along the main road, clusters everywhere else, kept off roads, river and the city block.
            var trees = new GameObject("Trees, bushes and rocks (SimpleNaturePack, Synty)").transform;
            trees.SetParent(root, false);
            string[] treePrefabs = { Nature + "Tree_01.prefab", Nature + "Tree_02.prefab", Nature + "Tree_03.prefab", Nature + "Tree_04.prefab", Nature + "Tree_05.prefab", Synty + "SM_Gen_Env_Tree_01.prefab" };
            for (float x = -150f; x <= 150f; x += 12f)
            {
                if (Mathf.Abs(x) < 8f) continue;
                Place(treePrefabs[rng.Next(5)], trees, new Vector3(x, GroundY, 4.2f), (float)rng.NextDouble() * 360f, 1.5f + (float)rng.NextDouble() * 0.5f);
            }
            int placed = 0, attempts = 0;
            while (placed < 230 && attempts++ < 5000)
            {
                var p = new Vector3(Mathf.Lerp(-260f, 260f, (float)rng.NextDouble()), GroundY, Mathf.Lerp(-12f, 330f, (float)rng.NextDouble()));
                if (!FreeForPlanting(p)) continue;
                Place(treePrefabs[rng.Next(treePrefabs.Length)], trees, p, (float)rng.NextDouble() * 360f, 1.4f + (float)rng.NextDouble() * 1.2f);
                placed++;
            }
            string[] small = { Nature + "Bush_01.prefab", Nature + "Bush_02.prefab", Nature + "Bush_03.prefab", Nature + "Rock_01.prefab", Nature + "Rock_03.prefab" };
            for (int i = 0, n = 0; n < 90 && i < 3000; i++)
            {
                var p = new Vector3(Mathf.Lerp(-120f, 120f, (float)rng.NextDouble()), GroundY, Mathf.Lerp(-8f, 160f, (float)rng.NextDouble()));
                if (!FreeForPlanting(p)) continue;
                Place(small[rng.Next(small.Length)], trees, p, (float)rng.NextDouble() * 360f, 1.6f + (float)rng.NextDouble());
                n++;
            }

            var mountains = new GameObject("Mountain ring (Synty)").transform;
            mountains.SetParent(root, false);
            int index = 0;
            for (float angle = -110f; angle <= 110f; angle += 13f, index++)
            {
                float radians = angle * Mathf.Deg2Rad;
                float radius = 620f + 40f * (index % 3);
                var position = new Vector3(Mathf.Sin(radians) * radius, GroundY - 8f, 120f + Mathf.Cos(radians) * radius);
                Place(Synty + "SM_Gen_Env_Background_Mountain_0" + (1 + index % 3) + ".prefab", mountains, position, angle + 180f, 3.6f + 0.6f * (index % 2));
            }
            for (float angle = -75f; angle <= 75f; angle += 25f, index++)
            {
                float radians = angle * Mathf.Deg2Rad;
                var position = new Vector3(Mathf.Sin(radians) * 400f, GroundY - 2f, 120f + Mathf.Cos(radians) * 400f);
                Place(Synty + "SM_Gen_Env_Mountain_0" + (1 + index % 3) + ".prefab", mountains, position, angle, 1.8f);
            }

            var clouds = new GameObject("Clouds (Synty)").transform;
            clouds.SetParent(root, false);
            for (int i = 0; i < 16; i++)
            {
                float angle = Mathf.Lerp(-80f, 80f, i / 15f) + (float)rng.NextDouble() * 8f;
                float radius = 280f + (float)rng.NextDouble() * 260f;
                var position = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * radius, GroundY + 110f + (float)rng.NextDouble() * 90f, 60f + Mathf.Cos(angle * Mathf.Deg2Rad) * radius);
                Place(Synty + "SM_Gen_Env_Cloud_0" + (1 + i % 3) + ".prefab", clouds, position, (float)rng.NextDouble() * 360f, 2.2f + (float)rng.NextDouble() * 1.2f);
            }
        }

        static bool FreeForPlanting(Vector3 p)
        {
            if (p.x > -34f && p.x < 34f && p.z > 3f && p.z < 70f) return false;   // supplied city block
            if (p.z > 2f && p.z < 22f) return false;                              // main road verge
            if (p.x > 28f && p.x < 52f && p.z > 10f) return false;                // north-south road and bridge
            if (p.z > 88f && p.z < 122f) return false;                            // river
            if (Mathf.Abs(p.x) < 9f && p.z < 6f) return false;                    // lift tower
            if (p.x > -95f && p.x < -45f && p.z > 14f && p.z < 66f) return false; // Level 1 twin-lift tower and plaza
            return true;
        }

        static void BuildBridge(Transform parent)
        {
            var bridge = new GameObject("River bridge").transform;
            bridge.SetParent(parent, false);
            var concrete = LitMaterial("RamConcrete", new Color(0.72f, 0.7f, 0.66f), 0.15f, 0f);
            var asphalt = LitMaterial("RamAsphalt", new Color(0.2f, 0.21f, 0.23f), 0.1f, 0f);
            float deckTop = GroundY + 1.6f;
            Box(bridge, "Deck", new Vector3(40f, deckTop - 0.3f, 105f), new Vector3(14f, 0.6f, 32f), concrete, Quaternion.identity);
            Box(bridge, "Deck road", new Vector3(40f, deckTop + 0.01f, 105f), new Vector3(10f, 0.02f, 32f), asphalt, Quaternion.identity);
            foreach (float side in new[] { -1f, 1f })
                Box(bridge, "Parapet", new Vector3(40f + side * 6.7f, deckTop + 0.45f, 105f), new Vector3(0.5f, 0.9f, 32f), concrete, Quaternion.identity);
            foreach (float z in new[] { 98f, 112f })
                Box(bridge, "Pier", new Vector3(40f, GroundY + 0.3f, z), new Vector3(10f, 1.4f, 1.6f), concrete, Quaternion.identity);
            float slope = Mathf.Atan2(1.6f, 12f) * Mathf.Rad2Deg;
            Box(bridge, "Ramp south", new Vector3(40f, GroundY + 0.55f, 83f), new Vector3(10f, 0.5f, 12.2f), asphalt, Quaternion.Euler(-slope, 0f, 0f));
            Box(bridge, "Ramp north", new Vector3(40f, GroundY + 0.55f, 127f), new Vector3(10f, 0.5f, 12.2f), asphalt, Quaternion.Euler(slope, 0f, 0f));
        }

        static void BuildLandingTower(Transform exterior)
        {
            var tower = new GameObject("Landing tower - one landing per floor").transform;
            tower.SetParent(exterior, false);
            var concrete = LitMaterial("RamConcrete", new Color(0.72f, 0.7f, 0.66f), 0.15f, 0f);
            var steel = LitMaterial("RamSteel", new Color(0.3f, 0.32f, 0.36f), 0.55f, 0.7f);
            var signPlate = LitMaterial("RamSignPlate", new Color(0.055f, 0.1f, 0.2f), 0.3f, 0f);
            var glass = AssetDatabase.LoadAssetAtPath<Material>(ImportedPath + "/Materials/ObservationGlass.mat");
            float topOfTower = GroundY + FloorHeight * LiftLogic.MaxFloor + 3.6f;
            foreach (float x in new[] { -1.62f, 1.62f })
                foreach (float z in new[] { 1.42f, 2.58f })
                    Box(tower, "Column", new Vector3(x, (GroundY + topOfTower) / 2f, z), new Vector3(0.22f, topOfTower - GroundY, 0.22f), steel, Quaternion.identity);
            Box(tower, "Roof", new Vector3(0f, topOfTower, 1.97f), new Vector3(3.6f, 0.3f, 1.6f), concrete, Quaternion.identity);
            for (int floor = 0; floor <= LiftLogic.MaxFloor; floor++)
            {
                float top = GroundY + FloorHeight * floor + 0.05f;
                var landing = new GameObject("Landing " + LiftCar.FloorName(floor)).transform;
                landing.SetParent(tower, false);
                Box(landing, "Slab", new Vector3(0f, top - 0.1f, 1.97f), new Vector3(3.0f, 0.2f, 1.3f), concrete, Quaternion.identity);
                Box(landing, "Glass rail", new Vector3(0f, top + 0.52f, 2.6f), new Vector3(2.9f, 1.0f, 0.02f), glass, Quaternion.identity);
                Box(landing, "Top rail", new Vector3(0f, top + 1.04f, 2.6f), new Vector3(2.95f, 0.05f, 0.06f), steel, Quaternion.identity);
                foreach (float side in new[] { -1f, 1f })
                {
                    Box(landing, "Side glass", new Vector3(side * 1.47f, top + 0.52f, 1.97f), new Vector3(0.02f, 1.0f, 1.25f), glass, Quaternion.identity);
                    Box(landing, "Side rail", new Vector3(side * 1.47f, top + 1.04f, 1.97f), new Vector3(0.06f, 0.05f, 1.3f), steel, Quaternion.identity);
                }
                Box(landing, "Floor sign plate", new Vector3(-1.2f, top + 2.1f, 1.33f), new Vector3(0.5f, 0.32f, 0.03f), signPlate, Quaternion.identity);
                var number = new GameObject("Floor sign " + LiftCar.FloorName(floor), typeof(TextMeshPro)).GetComponent<TextMeshPro>();
                number.transform.SetParent(landing, false);
                number.transform.localPosition = new Vector3(-1.2f, top + 2.1f, 1.31f);
                number.font = font;
                number.text = floor == 0 ? "P" : floor.ToString();
                number.fontSize = 2.2f;
                number.fontStyle = FontStyles.Bold;
                number.alignment = TextAlignmentOptions.Center;
                number.color = Color.white;
                number.rectTransform.sizeDelta = new Vector2(0.48f, 0.3f);
            }
        }

        static GameObject Place(string path, Transform parent, Vector3 localPosition, float yaw, float scale)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new System.IO.FileNotFoundException("Scenery prefab is missing from the project art packs.", path);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            instance.transform.localScale = prefab.transform.localScale * scale;
            foreach (var collider in instance.GetComponentsInChildren<Collider>()) collider.enabled = false;
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                if (path.Contains("Cloud") || path.Contains("Background_Mountain")) renderer.shadowCastingMode = ShadowCastingMode.Off;
            return instance;
        }

        static GameObject Box(Transform parent, string name, Vector3 localPosition, Vector3 size, Material material, Quaternion rotation)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localRotation = rotation;
            box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
            return box;
        }

        static Material LitMaterial(string name, Color color, float smoothness, float metallic)
        {
            string path = ImportedPath + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
