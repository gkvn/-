using UnityEngine;
using UnityEngine.UI;

public class LevelUI : MonoBehaviour
{
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button finishButton;

    private void Start()
    {
        bool isLast = LevelManager.Instance.IsLastLevel;
        nextLevelButton.gameObject.SetActive(!isLast);
        finishButton.gameObject.SetActive(isLast);
    }

    public void OnNextLevel()
    {
        LevelManager.Instance.LoadNextLevel();
    }

    public void OnFinish()
    {
        LevelManager.Instance.ReturnToMainMenu();
    }
}
