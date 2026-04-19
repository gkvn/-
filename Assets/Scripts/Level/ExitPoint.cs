using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ExitPoint : MonoBehaviour
{
    [Header("通关特效（仅黑灯阶段）")]
    [Tooltip("旋转贴图 A")]
    [SerializeField] private Sprite victorySpriteA;
    [Tooltip("旋转贴图 B")]
    [SerializeField] private Sprite victorySpriteB;
    [Tooltip("旋转轨道半径")]
    [SerializeField] private float victoryOrbitRadius = 0.8f;
    [Tooltip("旋转一圈的时长(秒)")]
    [SerializeField] private float victoryDuration = 1f;
    [Tooltip("发光颜色")]
    [SerializeField] private Color victoryGlowColor = new Color(1f, 0.9f, 0.5f, 0.9f);

    private bool triggered;

    private void Start()
    {
        var col = GetComponent<Collider2D>();
        Debug.Log($"[ExitPoint] Start — isTrigger={col.isTrigger}, bounds={col.bounds}, scale={transform.lossyScale}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[ExitPoint] OnTriggerEnter2D — other={other.name}, tag={other.tag}");
        if (!other.CompareTag("Player")) return;
        if (triggered) return;

        if (LevelConfig.IsAvgFlowBlocking)
            return;

        triggered = true;

        var player = other.GetComponent<PlayerController>();
        var playerTransform = other.transform;

        var pm = LevelPhaseManager.Instance;
        if (pm == null) { Debug.LogWarning("[ExitPoint] LevelPhaseManager.Instance is null"); return; }

        var cfg = FindObjectOfType<LevelConfig>();

        if (pm.CurrentPhase == LevelPhase.Light)
        {
            if (cfg != null)
                cfg.RunAvgChapterIfConfigured(cfg.AvgChapterBeforeNight, () =>
                {
                    pm.TransitionToDark();
                    triggered = false;
                });
            else
            {
                pm.TransitionToDark();
                triggered = false;
            }
        }
        else
        {
            System.Action onDone = () =>
            {
                if (BgmManager.Instance != null)
                    BgmManager.Instance.PlayLevelEndBgm();

                if (cfg != null)
                    cfg.RunAvgChapterIfConfigured(cfg.AvgChapterOnLevelComplete, () => pm.OnLevelComplete());
                else
                    pm.OnLevelComplete();
            };
            PlayVictoryEffect(playerTransform, onDone);
        }
    }

    private void PlayVictoryEffect(Transform playerTransform, System.Action onComplete)
    {
        var fxGo = new GameObject("VictoryEffect");
        var fx = fxGo.AddComponent<VictoryEffect>();
        fx.Play(playerTransform, victorySpriteA, victorySpriteB,
                victoryOrbitRadius, victoryDuration, victoryGlowColor, () =>
        {
            onComplete?.Invoke();
            triggered = false;
        });
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
