using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class BgmManager : MonoBehaviour
{
    public static BgmManager Instance { get; private set; }

    [Header("BGM")]
    [SerializeField] private AudioClip mainMenuBgm;
    [SerializeField] private AudioClip lightPhaseBgm;
    [SerializeField] private AudioClip darkPhaseBgm;

    [Header("Settings")]
    [SerializeField] private float crossfadeDuration = 1.5f;
    [SerializeField] [Range(0f, 1f)] private float volume = 0.5f;

    private AudioSource sourceA;
    private AudioSource sourceB;
    private AudioSource activeSource;
    private Coroutine crossfadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        sourceA = gameObject.AddComponent<AudioSource>();
        sourceB = gameObject.AddComponent<AudioSource>();
        ConfigureSource(sourceA);
        ConfigureSource(sourceB);
        activeSource = sourceA;
    }

    private void ConfigureSource(AudioSource src)
    {
        src.loop = true;
        src.playOnAwake = false;
        src.volume = 0f;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        PlayMainMenuBgm();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var lm = LevelManager.Instance;
        bool isMainMenu = lm != null && scene.name == GetMainMenuSceneName();
        if (isMainMenu)
        {
            PlayMainMenuBgm();
            return;
        }

        StartCoroutine(SubscribeToPhaseManagerWhenReady());
    }

    private string GetMainMenuSceneName()
    {
        return "MainMenu";
    }

    private IEnumerator SubscribeToPhaseManagerWhenReady()
    {
        float elapsed = 0f;
        while (LevelPhaseManager.Instance == null && elapsed < 5f)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        var pm = LevelPhaseManager.Instance;
        if (pm == null) yield break;

        pm.OnPhaseChanged += OnPhaseChanged;
        OnPhaseChanged(pm.CurrentPhase);
    }

    private void OnPhaseChanged(LevelPhase phase)
    {
        if (phase == LevelPhase.Light)
            CrossfadeTo(lightPhaseBgm);
        else
            CrossfadeTo(darkPhaseBgm);
    }

    private void PlayMainMenuBgm()
    {
        CrossfadeTo(mainMenuBgm);
    }

    public void CrossfadeTo(AudioClip clip)
    {
        if (clip == null) return;

        if (activeSource.clip == clip && activeSource.isPlaying)
            return;

        if (crossfadeCoroutine != null)
            StopCoroutine(crossfadeCoroutine);

        var incoming = activeSource == sourceA ? sourceB : sourceA;
        crossfadeCoroutine = StartCoroutine(CrossfadeRoutine(activeSource, incoming, clip));
        activeSource = incoming;
    }

    private IEnumerator CrossfadeRoutine(AudioSource outgoing, AudioSource incoming, AudioClip clip)
    {
        incoming.clip = clip;
        incoming.volume = 0f;
        incoming.Play();

        float t = 0f;
        float outStartVol = outgoing.volume;

        while (t < crossfadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(t / crossfadeDuration);
            outgoing.volume = Mathf.Lerp(outStartVol, 0f, progress);
            incoming.volume = Mathf.Lerp(0f, volume, progress);
            yield return null;
        }

        outgoing.Stop();
        outgoing.volume = 0f;
        incoming.volume = volume;
        crossfadeCoroutine = null;
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (activeSource != null && activeSource.isPlaying)
            activeSource.volume = volume;
    }
}
