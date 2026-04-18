using UnityEngine;
using System.Collections.Generic;

public class LevelConfig : MonoBehaviour
{
    [Header("画布图标配置")]
    [Tooltip("本关卡可用的标记图标")]
    [SerializeField] private List<Sprite> availableIcons = new List<Sprite>();

    public List<Sprite> AvailableIcons => availableIcons;
}
