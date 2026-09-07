using GothicTactics.CameraSystem;
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
            gridSerialized.ApplyModifiedPropertiesWithoutUndo();

            var unitObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            unitObject.name = "Prototype Hunter";
            unitObject.transform.localScale = new Vector3(0.55f, 0.7f, 0.55f);
            var unit = unitObject.AddComponent<HexUnit>();
            var unitSerialized = new SerializedObject(unit);
            unitSerialized.FindProperty("grid").objectReferenceValue = grid;
            unitSerialized.FindProperty("startingQ").intValue = 1;
            unitSerialized.FindProperty("startingR").intValue = 1;
            unitSerialized.ApplyModifiedPropertiesWithoutUndo();

            var unitRenderer = unitObject.GetComponent<MeshRenderer>();
            unitRenderer.sharedMaterial = GetOrCreateUnitMaterial();

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

        private static Material GetOrCreateUnitMaterial()
        {
            const string unitMaterialPath = "Assets/_Project/PrototypeUnitMaterial.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(unitMaterialPath);
            if (material != null) return material;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            material = new Material(shader)
            {
                name = "Prototype Unit Material",
                color = new Color(0.62f, 0.16f, 0.12f)
            };
            material.SetFloat("_Smoothness", 0.1f);
            AssetDatabase.CreateAsset(material, unitMaterialPath);
            return material;
        }
    }
}
