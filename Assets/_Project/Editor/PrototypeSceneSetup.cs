using GothicTactics.CameraSystem;
using GothicTactics.Combat;
using GothicTactics.Grid;
using GothicTactics.Units;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace GothicTactics.Editor
{
    public static class PrototypeSceneSetup
    {
        private const string MaterialPath = "Assets/_Project/PrototypeHexMaterial.mat";
        private const string ScenePath = "Assets/Scenes/HexPrototype.unity";

        [MenuItem("Gothic Tactics/Setup Hex Prototype Scene")]
        public static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Isometric Camera", typeof(Camera), typeof(AudioListener), typeof(IsometricCameraController));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetPositionAndRotation(new Vector3(6f, 14f, -10f), Quaternion.Euler(50f, 30f, 0f));
            cameraObject.GetComponent<Camera>().orthographic = true;
            cameraObject.GetComponent<Camera>().orthographicSize = 8f;

            var lightObject = new GameObject("Moonlight", typeof(Light));
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(0.68f, 0.75f, 0.9f);

            var gridObject = new GameObject("Hex Grid", typeof(HexGrid));
            var grid = gridObject.GetComponent<HexGrid>();
            var gridSerialized = new SerializedObject(grid);
            gridSerialized.FindProperty("interactionCamera").objectReferenceValue = cameraObject.GetComponent<Camera>();
            gridSerialized.FindProperty("tileMaterial").objectReferenceValue = GetOrCreateMaterial();
            var blockedCoordinates = gridSerialized.FindProperty("blockedCoordinates");
            var blockers = new[]
            {
                new Vector2Int(3, 2), new Vector2Int(3, 3), new Vector2Int(3, 4),
                new Vector2Int(4, 4), new Vector2Int(5, 4)
            };
            blockedCoordinates.arraySize = blockers.Length;
            for (var i = 0; i < blockers.Length; i++)
            {
                blockedCoordinates.GetArrayElementAtIndex(i).vector2IntValue = blockers[i];
            }
            gridSerialized.ApplyModifiedPropertiesWithoutUndo();

            CreateUnit("Hunter", grid, new Vector2Int(1, 1), UnitTeam.Player, 20,
                "Assets/_Project/PrototypeHunterMaterial.mat", new Color(0.62f, 0.16f, 0.12f));
            CreateUnit("Penitent", grid, new Vector2Int(1, 3), UnitTeam.Player, 15,
                "Assets/_Project/PrototypePenitentMaterial.mat", new Color(0.72f, 0.58f, 0.20f));
            CreateUnit("Ghoul", grid, new Vector2Int(6, 6), UnitTeam.Enemy, 10,
                "Assets/_Project/PrototypeEnemyMaterial.mat", new Color(0.20f, 0.42f, 0.20f));

            var turnObject = new GameObject("Turn System", typeof(TurnManager), typeof(PrototypeTurnHUD));
            var turnSerialized = new SerializedObject(turnObject.GetComponent<TurnManager>());
            turnSerialized.FindProperty("grid").objectReferenceValue = grid;
            turnSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = gridObject;
            Debug.Log($"Created prototype scene at {ScenePath}. Press Play to generate and interact with the grid.");
        }

        private static Material GetOrCreateMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null) return material;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            material = new Material(shader)
            {
                name = "Prototype Hex Material",
                color = new Color(0.16f, 0.17f, 0.19f)
            };
            material.SetFloat("_Smoothness", 0.1f);
            material.SetFloat("_Metallic", 0f);
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static void CreateUnit(
            string unitName,
            HexGrid grid,
            Vector2Int startingCoordinates,
            UnitTeam team,
            int initiative,
            string materialPath,
            Color colour)
        {
            var unitObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            unitObject.name = unitName;
            unitObject.transform.localScale = new Vector3(0.55f, 0.7f, 0.55f);
            var unit = unitObject.AddComponent<HexUnit>();
            var unitSerialized = new SerializedObject(unit);
            unitSerialized.FindProperty("grid").objectReferenceValue = grid;
            unitSerialized.FindProperty("startingQ").intValue = startingCoordinates.x;
            unitSerialized.FindProperty("startingR").intValue = startingCoordinates.y;
            unitSerialized.FindProperty("team").enumValueIndex = (int)team;
            unitSerialized.FindProperty("initiative").intValue = initiative;
            unitSerialized.ApplyModifiedPropertiesWithoutUndo();

            unitObject.GetComponent<MeshRenderer>().sharedMaterial = GetOrCreateUnitMaterial(materialPath, unitName, colour);
        }

        private static Material GetOrCreateUnitMaterial(string materialPath, string unitName, Color colour)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material != null) return material;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            material = new Material(shader)
            {
                name = $"Prototype {unitName} Material",
                color = colour
            };
            material.SetFloat("_Smoothness", 0.1f);
            AssetDatabase.CreateAsset(material, materialPath);
            return material;
        }
    }
}
