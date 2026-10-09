using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TeachingAndLearning.RestAndMotion.Editor
{
    /// <summary>Level 1 set: a twin-lift tower with glass shafts (Lift A and Lift B, copies of the supplied cabin) and a plaza to watch from.</summary>
    public static partial class ElevatorSceneBuilder
    {
        const float TwinX = -70f;
        const float TwinFacadeZ = 45f;
        const float PlazaZ = 28f;
        const float TwinShaftOffset = 4.2f;
        public const float TwinTravel = 16f;

        static void BuildTwinLiftTower(Transform exterior, ElevatorVRLesson lesson, GameObject mainCabin)
        {
            var tower = new GameObject("Level 1 - twin lift tower").transform;
            tower.SetParent(exterior, false);
            var wall = LitMaterial("RamTowerWall", new Color(0.86f, 0.8f, 0.7f), 0.2f, 0f);
            var band = LitMaterial("RamTowerBand", new Color(0.62f, 0.58f, 0.52f), 0.2f, 0f);
            var window = LitMaterial("RamTowerWindow", new Color(0.25f, 0.38f, 0.55f), 0.85f, 0.1f);
            var steel = LitMaterial("RamSteel", new Color(0.3f, 0.32f, 0.36f), 0.55f, 0.7f);
            var concrete = LitMaterial("RamConcrete", new Color(0.72f, 0.7f, 0.66f), 0.15f, 0f);
            var glass = AssetDatabase.LoadAssetAtPath<Material>(ImportedPath + "/Materials/ObservationGlass.mat");
            float height = FloorHeight * 13f;
            Box(tower, "Tower body", new Vector3(TwinX, GroundY + height / 2f, TwinFacadeZ + 6f), new Vector3(20f, height, 12f), wall, Quaternion.identity);
            for (int floor = 1; floor <= 13; floor++)
                Box(tower, "Floor band", new Vector3(TwinX, GroundY + floor * FloorHeight, TwinFacadeZ - 0.05f), new Vector3(20.1f, 0.3f, 0.2f), band, Quaternion.identity);
            foreach (float side in new[] { -1f, 1f })
                for (int floor = 0; floor < 13; floor++)
                    Box(tower, "Window", new Vector3(TwinX + side * 8.2f, GroundY + floor * FloorHeight + 1.7f, TwinFacadeZ - 0.06f), new Vector3(2.6f, 1.6f, 0.1f), window, Quaternion.identity);
            Box(tower, "Roof", new Vector3(TwinX, GroundY + height + 0.2f, TwinFacadeZ + 6f), new Vector3(20.6f, 0.4f, 12.6f), band, Quaternion.identity);

            var people = new[] { "Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Street_Male_01.prefab", "Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Street_Female_01.prefab" };
            for (int i = 0; i < 2; i++)
            {
                string letter = i == 0 ? "A" : "B";
                float x = TwinX + (i == 0 ? -TwinShaftOffset : TwinShaftOffset);
                var shaft = new GameObject("Shaft " + letter).transform;
                shaft.SetParent(tower, false);
                foreach (float sx in new[] { -1.75f, 1.75f })
                    foreach (float z in new[] { TwinFacadeZ - 3.4f, TwinFacadeZ - 0.1f })
                        Box(shaft, "Shaft column", new Vector3(x + sx, GroundY + height / 2f, z), new Vector3(0.18f, height, 0.18f), steel, Quaternion.identity);
                Box(shaft, "Shaft glass", new Vector3(x, GroundY + height / 2f, TwinFacadeZ - 3.45f), new Vector3(3.5f, height, 0.03f), glass, Quaternion.identity);
                Box(shaft, "Shaft top", new Vector3(x, GroundY + height + 0.3f, TwinFacadeZ - 1.75f), new Vector3(3.8f, 0.6f, 3.6f), band, Quaternion.identity);
                Box(shaft, "Sign plate", new Vector3(x, GroundY + 0.9f, TwinFacadeZ - 3.6f), new Vector3(2.2f, 0.8f, 0.06f), LitMaterial("RamSignPlate", new Color(0.055f, 0.1f, 0.2f), 0.3f, 0f), Quaternion.identity);
                var sign = new GameObject("Label LIFT " + letter, typeof(TextMeshPro)).GetComponent<TextMeshPro>();
                sign.transform.SetParent(shaft, false);
                sign.transform.localPosition = new Vector3(x, GroundY + 0.9f, TwinFacadeZ - 3.64f);
                sign.font = font; sign.text = "LIFT " + letter; sign.fontSize = 6f; sign.fontStyle = FontStyles.Bold;
                sign.alignment = TextAlignmentOptions.Center; sign.color = Color.white; sign.rectTransform.sizeDelta = new Vector2(2.1f, 0.7f);
                var topSign = Object.Instantiate(sign.gameObject, shaft).GetComponent<TextMeshPro>();
                topSign.name = "Top label LIFT " + letter;
                topSign.transform.localPosition = new Vector3(x, GroundY + height + 1.6f, TwinFacadeZ - 3.2f);
                topSign.fontSize = 14f; topSign.rectTransform.sizeDelta = new Vector2(5f, 1.8f); topSign.color = new Color(0.35f, 0.65f, 1f);

                var car = new GameObject("LIFT " + letter + " (moves in Level 1)").transform;
                car.SetParent(tower, false);
                car.localPosition = new Vector3(x, GroundY + 0.05f, TwinFacadeZ - 1.75f);
                var body = Object.Instantiate(mainCabin, car);
                body.name = "Cabin (copy of supplied winda.fbx)";
                body.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                body.transform.localScale = mainCabin.transform.localScale * (3.0f / 3.5f);
                body.transform.localPosition = Vector3.zero;
                var cabinBounds = RenderedBounds(body);
                body.transform.position += car.position - new Vector3(cabinBounds.center.x, cabinBounds.min.y, cabinBounds.center.z);                var person = Place(people[i], car, new Vector3(0.2f, 0.02f, 0.3f), 180f, 1f);
                person.name = "Passenger";
                foreach (var animator in person.GetComponentsInChildren<Animator>()) animator.enabled = false;
                if (i == 0) lesson.liftA = car; else lesson.liftB = car;
            }
            lesson.twinLiftTravel = TwinTravel;

            var plaza = new GameObject("Level 1 - viewing plaza").transform;
            plaza.SetParent(tower, false);
            Box(plaza, "Plaza paving", new Vector3(TwinX, GroundY + 0.02f, PlazaZ + 1f), new Vector3(22f, 0.06f, 12f), concrete, Quaternion.identity);
            Box(plaza, "Path to tower", new Vector3(TwinX, GroundY + 0.015f, (PlazaZ + TwinFacadeZ) / 2f), new Vector3(5f, 0.04f, TwinFacadeZ - PlazaZ), concrete, Quaternion.identity);
            var anchor = new GameObject("Player spot (Level 1)").transform;
            anchor.SetParent(plaza, false);
            anchor.localPosition = new Vector3(TwinX, GroundY + 0.05f, PlazaZ);
            lesson.plazaAnchor = anchor;
            var popupAnchor = new GameObject("Pop-up anchor (plaza)").transform;
            popupAnchor.SetParent(anchor, false);
            var popupLocal = new Vector3(-2.1f, 1.75f, 2.3f);
            popupAnchor.localPosition = popupLocal;
            popupAnchor.localRotation = Quaternion.LookRotation(popupLocal - new Vector3(0f, 1.65f, 0f), Vector3.up);
            popupAnchor.localScale = Vector3.one * 1.45f;
            lesson.popupPlazaAnchor = popupAnchor;
        }
    }
}
