using UnityEngine;

/// <summary>
/// 关卡开始时从 Resources 路径实例化相机与玩家，并绑定 <see cref="LevelPhaseManager"/>。
/// 预制体须放在可加载路径下，例如 Assets/Resources/Prefabs/xxx（代码中路径为 "Prefabs/xxx"，不含扩展名）。
/// </summary>
[DefaultExecutionOrder(-500)]
public class GameplayBootstrap : MonoBehaviour
{
    [SerializeField] private string playerResourcesPath = "Prefabs/Player";
    [SerializeField] private string cameraResourcesPath = "Prefabs/GameCamera";

    [Tooltip("动态生成的主相机世界坐标（与场景中 former GameCamera 一致）")]
    [SerializeField] private Vector3 cameraWorldPosition = new Vector3(4.3f, 5f, -10f);

    private void Awake()
    {
        var camPrefab = Resources.Load<GameObject>(cameraResourcesPath);
        if (camPrefab == null)
        {
            Debug.LogError($"GameplayBootstrap: 无法加载 Resources路径 \"{cameraResourcesPath}\"。");
            return;
        }

        var camGo = Instantiate(camPrefab, cameraWorldPosition, Quaternion.identity);
        camGo.name = "GameCamera";

        var topDown = camGo.GetComponent<TopDownCamera>();
        var phaseManager = FindObjectOfType<LevelPhaseManager>();
        if (phaseManager != null && topDown != null)
            phaseManager.AssignTopDownCamera(topDown);

        var playerPrefab = Resources.Load<GameObject>(playerResourcesPath);
        if (playerPrefab == null)
        {
            Debug.LogError($"GameplayBootstrap: 无法加载 Resources 路径 \"{playerResourcesPath}\"。");
            return;
        }

        var spawn = FindObjectOfType<SpawnPoint>();
        Vector3 p = spawn != null ? spawn.transform.position : Vector3.zero;
        p.z = 0f;
        Instantiate(playerPrefab, p, Quaternion.identity);
    }
}
