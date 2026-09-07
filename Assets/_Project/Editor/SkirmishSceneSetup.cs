using System.IO;
using GothicTactics.Skirmish;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace GothicTactics.Editor
{
    public static class SkirmishSceneSetup
    {
        [MenuItem("Gothic Tactics/Play Complete Skirmish")]
        public static void Create()
        {
            if (EditorApplication.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera = new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));
            camera.tag = "MainCamera";
            var light = new GameObject("Moonlight",typeof(Light)).GetComponent<Light>();
            light.type = LightType.Directional; light.intensity = .85f;
            light.color = new Color(.72f,.70f,.66f);
            light.transform.rotation = Quaternion.Euler(50,-30,0);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.24f,.21f,.19f);
            RenderSettings.skybox = null;
            var game = new GameObject("Ashen Bell Skirmish",typeof(SkirmishController));
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/AshenBell.unity");
            Selection.activeGameObject = game;
            EditorApplication.isPlaying = true;
        }
    }
}
