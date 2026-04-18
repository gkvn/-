using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class SceneSetup
{
    [MenuItem("Tools/生成游戏场景")]
    public static void SetupScenes()
    {
        if (!EditorUtility.DisplayDialog(
            "生成游戏场景",
            "将创建 MainMenu、Level_01、Level_02 三个场景，并配置 Build Settings。\n继续？",
            "确定", "取消"))
            return;

        CreateMainMenuScene();
        CreateLevelScene("Level_01");
        CreateLevelScene("Level_02");
        SyncBuildSettings();

        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        Debug.Log("游戏场景生成完毕！请在 MainMenu 场景的 LevelManager Inspector 中配置关卡顺序。");
    }

    static Font GetFont()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }

    static void CreateMainMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        var canvasGO = CreateCanvas("Canvas");

        // Title
        var titleGO = CreateText("Title", canvasGO.transform, "西格纳路",
            fontSize: 60, position: new Vector2(0, 100), size: new Vector2(600, 100));

        // Start Button
        var startBtnGO = CreateButton("StartButton", canvasGO.transform, "开始游戏",
            position: new Vector2(0, -50), size: new Vector2(240, 64));

        // MainMenuUI
        var menuUI = canvasGO.AddComponent<MainMenuUI>();
        var startBtn = startBtnGO.GetComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(startBtn.onClick, menuUI.OnStartGame);

        // LevelManager
        var managerGO = new GameObject("LevelManager");
        var lm = managerGO.AddComponent<LevelManager>();
        var so = new SerializedObject(lm);
        var levelsProp = so.FindProperty("levelScenes");
        levelsProp.arraySize = 2;
        levelsProp.GetArrayElementAtIndex(0).stringValue = "Level_01";
        levelsProp.GetArrayElementAtIndex(1).stringValue = "Level_02";
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
    }

    static void CreateLevelScene(string sceneName)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        var canvasGO = CreateCanvas("Canvas");

        // Level title
        CreateText("LevelTitle", canvasGO.transform, sceneName,
            fontSize: 48, position: new Vector2(0, 200), size: new Vector2(400, 80));

        // LevelUI container
        var levelUIGO = new GameObject("LevelUI");
        levelUIGO.transform.SetParent(canvasGO.transform, false);
        var levelUIRT = levelUIGO.AddComponent<RectTransform>();
        levelUIRT.anchoredPosition = new Vector2(0, -100);
        levelUIRT.sizeDelta = new Vector2(400, 200);
        var levelUI = levelUIGO.AddComponent<LevelUI>();

        // Next Level button
        var nextBtnGO = CreateButton("NextLevelButton", levelUIGO.transform, "下一关",
            position: Vector2.zero, size: new Vector2(200, 60));
        var nextBtn = nextBtnGO.GetComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(nextBtn.onClick, levelUI.OnNextLevel);

        // Finish button
        var finishBtnGO = CreateButton("FinishButton", levelUIGO.transform, "完成",
            position: Vector2.zero, size: new Vector2(200, 60));
        var finishBtn = finishBtnGO.GetComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(finishBtn.onClick, levelUI.OnFinish);

        // Wire serialized references
        var so = new SerializedObject(levelUI);
        so.FindProperty("nextLevelButton").objectReferenceValue = nextBtn;
        so.FindProperty("finishButton").objectReferenceValue = finishBtn;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, $"Assets/Scenes/{sceneName}.unity");
    }

    static GameObject CreateCanvas(string name)
    {
        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    static GameObject CreateText(string name, Transform parent, string content,
        int fontSize, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.font = GetFont();
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        return go;
    }

    static GameObject CreateButton(string name, Transform parent, string label,
        Vector2 position, Vector2 size)
    {
        var go = DefaultControls.CreateButton(new DefaultControls.Resources());
        go.name = name;
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        var text = go.GetComponentInChildren<Text>();
        text.text = label;
        text.fontSize = 28;
        text.font = GetFont();
        return go;
    }

    [MenuItem("Tools/同步关卡到 Build Settings")]
    public static void SyncBuildSettings()
    {
        var guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();

        // MainMenu first
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("MainMenu"))
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
                break;
            }
        }

        // Then all level scenes
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.Contains("MainMenu"))
                scenes.Add(new EditorBuildSettingsScene(path, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log($"Build Settings 已更新：共 {scenes.Count} 个场景");
    }
}
