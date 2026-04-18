using UnityEngine;

public static class RuntimeSprite
{
    private static Sprite _fallback;

    public static Sprite Get()
    {
        if (_fallback != null) return _fallback;
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var px = tex.GetPixels();
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        _fallback = Sprite.Create(tex, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4);
        _fallback.name = "RuntimeFallback";
        return _fallback;
    }
}
