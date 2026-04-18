using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("背景")]
    [SerializeField] private Image background;

    public void SetBackgroundSprite(Sprite sprite)
    {
        if (background != null)
            background.sprite = sprite;
    }

    public void OnStartGame()
    {
        LevelManager.Instance.StartGame();
    }
}
