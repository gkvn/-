using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    public void OnStartGame()
    {
        LevelManager.Instance.StartGame();
    }
}
