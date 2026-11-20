using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace MagicBlock.EditorTools
{
    /// <summary>Menu MagicBlock/Build Game Scene: tạo scene Game.unity, gắn GameManager + GameView và asset.</summary>
    public static class MagicBlockSceneBuilder
    {
        const string ScenePath = "Assets/MagicBlock/Scenes/Game.unity";
        const string Art = "Assets/MagicBlock/Art/";
        const string Audio = "Assets/MagicBlock/Audio/";
        const string Fonts = "Assets/MagicBlock/Fonts/";

        static readonly string[] TileNames = { "red", "orange", "yellow_0", "green_0", "cyan", "blue_0", "purple" };

        [MenuItem("MagicBlock/Build Game Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.32f, 0.75f);
            camGo.transform.position = new Vector3(0, 0, -10);

            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var root = new GameObject("MagicBlock");
            var game = root.AddComponent<GameManager>();
            var view = root.AddComponent<GameView>();

            var so = new SerializedObject(view);
            so.FindProperty("game").objectReferenceValue = game;

            var tiles = so.FindProperty("tileSprites");
            var loaded = new List<Sprite>();
            foreach (var n in TileNames)
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{Art}Tiles/{n}.png");
                if (s != null) loaded.Add(s); else Debug.LogWarning($"[MagicBlock] Missing tile sprite {n}");
            }
            tiles.arraySize = loaded.Count;
            for (int i = 0; i < loaded.Count; i++) tiles.GetArrayElementAtIndex(i).objectReferenceValue = loaded[i];

            so.FindProperty("cellSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"{Art}grid_01.png");
            so.FindProperty("backgroundSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"{Art}BG.png");
            so.FindProperty("font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>($"{Fonts}Nunito-Black.ttf");

            so.FindProperty("sfxBeginDrag").objectReferenceValue = Clip("BeginDrag");
            so.FindProperty("sfxPut").objectReferenceValue = Clip("Put");
            so.FindProperty("sfxGameOver").objectReferenceValue = Clip("GameOver");
            so.FindProperty("sfxNewHighScore").objectReferenceValue = Clip("NewHighScore");
            so.FindProperty("sfxClick").objectReferenceValue = Clip("Button_UIClick");
            var clears = so.FindProperty("sfxClear");
            clears.arraySize = 5;
            for (int i = 0; i < 5; i++) clears.GetArrayElementAtIndex(i).objectReferenceValue = Clip($"Combo{i}");
            so.ApplyModifiedPropertiesWithoutUndo();

            System.IO.Directory.CreateDirectory("Assets/MagicBlock/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            var list = new List<EditorBuildSettingsScene> { new(ScenePath, true) };
            foreach (var s in EditorBuildSettings.scenes)
                if (s.path != ScenePath) list.Add(s);
            EditorBuildSettings.scenes = list.ToArray();

            Debug.Log($"[MagicBlock] Built {ScenePath} ({loaded.Count} tile sprites)");
        }

        static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>($"{Audio}{name}.ogg");
    }
}
