using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class FontReplacer
{
    const string FONT_PATH = "Fonts/NotoSansSC-Regular";

    [MenuItem("Tools/替换所有字体 - Prefab和场景")]
    public static void ReplaceAllFonts()
    {
        var font = Resources.Load<Font>(FONT_PATH);
        if (font == null)
        {
            Debug.LogError($"找不到字体资源: Resources/{FONT_PATH}");
            return;
        }

        int prefabCount = ReplaceFontsInPrefabs(font);
        int sceneCount = ReplaceFontsInAllScenes(font);

        AssetDatabase.SaveAssets();
        Debug.Log($"字体替换完成！共修改 {prefabCount} 个Prefab, {sceneCount} 个场景中的Text/TextMesh组件");
    }

    static int ReplaceFontsInPrefabs(Font font)
    {
        int total = 0;
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            bool modified = false;
            foreach (var text in prefab.GetComponentsInChildren<Text>(true))
            {
                if (text.font != font)
                {
                    text.font = font;
                    modified = true;
                    total++;
                }
            }
            foreach (var mesh in prefab.GetComponentsInChildren<TextMesh>(true))
            {
                if (mesh.font != font)
                {
                    mesh.font = font;
                    var mr = mesh.GetComponent<MeshRenderer>();
                    if (mr != null && font.material != null)
                        mr.sharedMaterial = font.material;
                    modified = true;
                    total++;
                }
            }

            if (modified)
            {
                EditorUtility.SetDirty(prefab);
                Debug.Log($"已替换 Prefab: {path}");
            }
        }
        return total;
    }

    static int ReplaceFontsInAllScenes(Font font)
    {
        int total = 0;
        var currentScene = SceneManager.GetActiveScene().path;

        foreach (var scenePath in EditorBuildSettings.scenes)
        {
            if (!scenePath.enabled) continue;
            var scene = EditorSceneManager.OpenScene(scenePath.path, OpenSceneMode.Single);

            bool modified = false;
            foreach (var go in scene.GetRootGameObjects())
            {
                foreach (var text in go.GetComponentsInChildren<Text>(true))
                {
                    if (text.font != font)
                    {
                        text.font = font;
                        modified = true;
                        total++;
                    }
                }
                foreach (var mesh in go.GetComponentsInChildren<TextMesh>(true))
                {
                    if (mesh.font != font)
                    {
                        mesh.font = font;
                        var mr = mesh.GetComponent<MeshRenderer>();
                        if (mr != null && font.material != null)
                            mr.sharedMaterial = font.material;
                        modified = true;
                        total++;
                    }
                }
            }

            if (modified)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"已替换场景: {scenePath.path}");
            }
        }

        if (!string.IsNullOrEmpty(currentScene))
            EditorSceneManager.OpenScene(currentScene);

        return total;
    }
}
