using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace AVG {
  public static class AvgUtil {
    public const float DEFAULT_TYPING_SPEED = 0.05f;
    public const float DEFAULT_FAST_WAIT_TIME = 0.4f;
    public const float DEFAULT_AUTO_WAIT_TIME = 0.5f;
    public const int LOG_LIMIT_CNT = 50;
    public const float DEFAULT_FADE_TIME = 0.2f;
    
    private static readonly Dictionary<string, Sprite> s_spriteCache = new Dictionary<string, Sprite>();

    #region Load Sprite
    public enum ResourceType {
      CharFace,
      CharBody,
      CG,
      Avatar,
    }

    public static Sprite LoadSprite(string spriteName, ResourceType type) {
      if (string.IsNullOrEmpty(spriteName)) {
        return null;
      }
      
      string cacheKey = $"{type}_{spriteName}";
      if (s_spriteCache.TryGetValue(cacheKey, out Sprite cachedSprite)) {
        return cachedSprite;
      }

      if (TryLoadSpriteFromResources(spriteName, type, out Sprite sprite)) {
        s_spriteCache[cacheKey] = sprite;
        return sprite;
      }
      return null;
    }
    
    public static IEnumerator LoadSpriteAsync(string spriteName, ResourceType type, Action<Sprite> onComplete) {
      if (string.IsNullOrEmpty(spriteName)) {
        onComplete?.Invoke(null);
        yield break;
      }
      
      string cacheKey = $"{type}_{spriteName}";
      
      if (s_spriteCache.TryGetValue(cacheKey, out Sprite cachedSprite)) {
        onComplete?.Invoke(cachedSprite);
        yield break;
      }

      if (TryLoadSpriteFromResources(spriteName, type, out Sprite resSprite)) {
        s_spriteCache[cacheKey] = resSprite;
        onComplete?.Invoke(resSprite);
        yield break;
      }
      
      string filePath = GetFilePath(spriteName, type);

#if UNITY_EDITOR || (!UNITY_ANDROID && !UNITY_WEBGL)
      if (!File.Exists(filePath)) {
        Debug.LogWarning($"无法加载图片: {spriteName}（请在 Assets/.../Resources/{GetFolder(type)}/ 放置 png，或放入 StreamingAssets/{GetFolder(type)}/）");
        onComplete?.Invoke(null);
        yield break;
      }
#endif
      
      string url = filePath;
#if !UNITY_ANDROID || UNITY_EDITOR
      url = "file://" + filePath;
#endif
      
      using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url)) {
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success) {
          Texture2D texture = DownloadHandlerTexture.GetContent(request);
          Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f
          );
          
          s_spriteCache[cacheKey] = sprite;
          onComplete?.Invoke(sprite);
        } else {
          Debug.LogWarning($"无法加载图片: {spriteName} (路径: {filePath}, 错误: {request.error})");
          onComplete?.Invoke(null);
        }
      }
    }
    
    /// <summary>
    /// 优先从任意 Resources 目录加载（推荐放在 ImportedAVG/Resources/Arts/...），避免依赖 StreamingAssets。
    /// </summary>
    private static bool TryLoadSpriteFromResources(string spriteName, ResourceType type, out Sprite sprite) {
      sprite = null;
      string folder = GetFolder(type);
      string baseName = spriteName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
        ? spriteName.Substring(0, spriteName.Length - 4)
        : spriteName;
      string path = $"{folder}/{baseName}";
      
      sprite = Resources.Load<Sprite>(path);
      if (sprite != null) {
        return true;
      }

      Texture2D tex = Resources.Load<Texture2D>(path);
      if (tex != null) {
        sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        return true;
      }

      return false;
    }
    
    public static IEnumerator PreloadSprite(string spriteName, ResourceType type) {
      yield return LoadSpriteAsync(spriteName, type, null);
    }
    
    public static void ClearSpriteCache() {
      foreach (var sprite in s_spriteCache.Values) {
        if (sprite != null && sprite.texture != null) {
          UnityEngine.Object.Destroy(sprite.texture);
          UnityEngine.Object.Destroy(sprite);
        }
      }
      s_spriteCache.Clear();
    }
    
    public static void RemoveSpriteFromCache(string spriteName, ResourceType type) {
      string cacheKey = $"{type}_{spriteName}";
      if (s_spriteCache.TryGetValue(cacheKey, out Sprite sprite)) {
        if (sprite != null && sprite.texture != null) {
          UnityEngine.Object.Destroy(sprite.texture);
          UnityEngine.Object.Destroy(sprite);
        }
        s_spriteCache.Remove(cacheKey);
      }
    }
    
    private static string GetFolder(ResourceType type) {
      return type switch {
        ResourceType.CharFace => "Arts/charFace",
        ResourceType.CharBody => "Arts/charBody",
        ResourceType.CG => "Arts/cg",
        ResourceType.Avatar => "Arts/avatar",
        _ => "Arts/charBody"
      };
    }
    
    private static string GetFilePath(string spriteName, ResourceType type) {
      string folder = GetFolder(type);
      string fileName = spriteName.EndsWith(".png") ? spriteName : $"{spriteName}.png";
      return Path.Combine(Application.streamingAssetsPath, folder, fileName);
    }
    #endregion

    public static void OnAvgEvent(EventData eventData) {
      if (eventData == null) {
        return;
      }
      var avgController = AvgController.Instance;
      if (avgController == null) {
        return;
      }
      switch (eventData.type) {
        case EventData.Type.PLAY_BGM:
          avgController.soundManager?.PlayBgm(eventData.param1);
          break;
        case EventData.Type.PLAY_ENV_BGM:
          avgController.soundManager?.PlayEnvBgm(eventData.param1);
          break;
        case EventData.Type.STOP_BGM:
          avgController.soundManager.StopBgm();
          break;
        case EventData.Type.STOP_ENV_BGM:
          avgController.soundManager.StopEnvBgm();
          break;
        case EventData.Type.PLAY_SF:
          avgController.soundManager.PlaySf(eventData.param1);
          break;
      }
    }
  }
}
