using UnityEngine;

public class FrameAnimator : MonoBehaviour
{
    [Tooltip("动画帧序列（按顺序拖入多张 Sprite）")]
    [SerializeField] private Sprite[] frames;
    [Tooltip("每秒播放帧数")]
    [SerializeField] private float fps = 8f;
    [Tooltip("是否循环")]
    [SerializeField] private bool loop = true;

    private SpriteRenderer spriteRenderer;
    private float timer;
    private int currentFrame;
    private bool playing = true;

    public Sprite[] Frames
    {
        get => frames;
        set { frames = value; currentFrame = 0; timer = 0f; }
    }

    public float FPS { get => fps; set => fps = value; }

    public Sprite CurrentSprite =>
        frames != null && frames.Length > 0 ? frames[currentFrame % frames.Length] : null;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (!playing || frames == null || frames.Length <= 1 || fps <= 0f) return;

        timer += Time.deltaTime;
        float interval = 1f / fps;

        if (timer >= interval)
        {
            timer -= interval;
            currentFrame++;

            if (currentFrame >= frames.Length)
            {
                if (loop) currentFrame = 0;
                else { currentFrame = frames.Length - 1; playing = false; }
            }

            ApplyFrame();
        }
    }

    public void Play()
    {
        playing = true;
        currentFrame = 0;
        timer = 0f;
        ApplyFrame();
    }

    public void Stop()
    {
        playing = false;
    }

    public void Pause()
    {
        playing = false;
        currentFrame = 0;
        timer = 0f;
        ApplyFrame();
    }

    public void Resume()
    {
        if (!playing)
            playing = true;
    }

    public void SetFramesAndPlay(Sprite[] newFrames)
    {
        frames = newFrames;
        Play();
    }

    private void ApplyFrame()
    {
        if (spriteRenderer != null && frames != null && currentFrame < frames.Length && frames[currentFrame] != null)
            spriteRenderer.sprite = frames[currentFrame];
    }
}
