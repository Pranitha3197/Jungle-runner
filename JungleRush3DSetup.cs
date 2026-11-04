using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class JungleRush3DSetup
{
    [MenuItem("Jungle Rush 3D/Create Playable 3D Scene")]
    public static void CreateScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject camera = new GameObject("Main Camera");
        camera.tag = "MainCamera";
        Camera cam = camera.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 68;
        camera.transform.position = new Vector3(0, 5, -8.5f);
        camera.transform.rotation = Quaternion.Euler(18, 0, 0);
        GameObject light = new GameObject("Jungle Sun");
        Light sun = light.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.15f;
        light.transform.rotation = Quaternion.Euler(48, -28, 0);
        GameObject game = new GameObject("Jungle Rush 3D Game");
        game.AddComponent<JungleRush3DGame>();
        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets","Scenes");
        string path = "Assets/Scenes/JungleRush3D.unity";
        EditorSceneManager.SaveScene(scene, path);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(path);
        EditorUtility.DisplayDialog("Jungle Rush 3D", "3D runner scene created. Press Play to run!", "Play");
    }
}
