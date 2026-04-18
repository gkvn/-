using UnityEngine;
using System;

public class DeathFlashEffect : MonoBehaviour
{
    private SpriteRenderer sr;
    private Color originalColor;
    private float elapsed;
    private float duration;
    private Action onComplete;
    private int flashCount;
    private float flashInterval;

    public void Play(SpriteRenderer sprite, float totalDuration, int flashes, Action callback)
    {
        sr = sprite;
        originalColor = sr != null ? sr.color : Color.white;
        duration = totalDuration;
        flashCount = flashes;
        flashInterval = totalDuration / (flashes * 2f);
        onComplete = callback;

        if (sr != null)
            sr.color = Color.red;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        if (sr != null)
        {
            int step = Mathf.FloorToInt(elapsed / flashInterval);
            bool visible = step % 2 == 0;
            sr.enabled = visible;
        }

        if (elapsed >= duration)
        {
            if (sr != null)
            {
                sr.enabled = true;
                sr.color = originalColor;
            }
            onComplete?.Invoke();
            Destroy(this);
        }
    }
}
