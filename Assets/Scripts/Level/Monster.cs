using UnityEngine;
using System.Collections.Generic;

public class Monster : MonoBehaviour
{
    [Header("Combo Sequence")]
    [Tooltip("击败此怪物所需的子弹组合（Dot=点击, Line=长按）")]
    [SerializeField] private BulletType[] requiredCombo = new BulletType[]
    {
        BulletType.Dot, BulletType.Dot, BulletType.Line
    };

    private int comboIndex;
    private List<GameObject> comboIndicators = new List<GameObject>();
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        CreateComboDisplay();
        UpdateComboDisplay();
    }

    public void OnBulletHit(BulletType type)
    {
        if (comboIndex >= requiredCombo.Length) return;

        if (requiredCombo[comboIndex] == type)
        {
            comboIndex++;
            Debug.Log($"[Monster] Correct! {comboIndex}/{requiredCombo.Length}");

            if (comboIndex >= requiredCombo.Length)
            {
                Defeat();
                return;
            }
        }
        else
        {
            comboIndex = 0;
            Debug.Log("[Monster] Wrong input! Combo reset.");
            FlashRed();
        }

        UpdateComboDisplay();
    }

    private void Defeat()
    {
        Debug.Log("[Monster] Defeated!");
        gameObject.SetActive(false);
    }

    private void FlashRed()
    {
        if (spriteRenderer == null) return;
        spriteRenderer.color = Color.red;
        Invoke(nameof(ResetColor), 0.15f);
    }

    private void ResetColor()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = Color.magenta;
    }

    // ── Combo display: small icons above monster head ──

    private void CreateComboDisplay()
    {
        var parentSR = GetComponent<SpriteRenderer>();
        Sprite baseSprite = parentSR != null ? parentSR.sprite : null;
        if (baseSprite == null) baseSprite = RuntimeSprite.Get();

        float spacing = 0.5f;
        float startX = -(requiredCombo.Length - 1) * spacing / 2f;

        for (int i = 0; i < requiredCombo.Length; i++)
        {
            var go = new GameObject($"Combo_{i}");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(startX + i * spacing, 1.8f, 0);

            bool isDot = requiredCombo[i] == BulletType.Dot;
            go.transform.localScale = isDot
                ? Vector3.one * 0.35f
                : new Vector3(0.7f, 0.25f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = baseSprite;
            sr.sortingOrder = 15;
            sr.color = Color.gray;

            comboIndicators.Add(go);
        }
    }

    private void UpdateComboDisplay()
    {
        for (int i = 0; i < comboIndicators.Count; i++)
        {
            var sr = comboIndicators[i].GetComponent<SpriteRenderer>();
            if (sr == null) continue;

            if (i < comboIndex)
                sr.color = Color.green;
            else if (i == comboIndex)
                sr.color = Color.white;
            else
                sr.color = Color.gray;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
