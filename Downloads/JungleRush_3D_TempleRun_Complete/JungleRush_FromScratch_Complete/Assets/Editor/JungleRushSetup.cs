using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public static class JungleRushSetup
{
    [MenuItem("Jungle Rush/Build New Playable Scene")]
    public static void BuildScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5;
        cameraObject.transform.position = new Vector3(0, 0, -10);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.52f, .78f, .92f);

        GameObject game = new GameObject("Jungle Rush Game");
        game.AddComponent<JungleRushGame>();

        string folder = "Assets/Scenes";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        string path = folder + "/JungleRush.unity";
        EditorSceneManager.SaveScene(scene, path);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Jungle Rush Ready",
            "A new playable scene was created at Assets/Scenes/JungleRush.unity\\n\\nPress Play to run the game.",
            "Let's Play");
        EditorSceneManager.OpenScene(path);
    }
}
