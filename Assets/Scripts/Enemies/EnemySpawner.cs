using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [SerializeField] private GameObject[] enemies;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float timeBetweenSpawns = 3f;

    [Header("USB Enemy (KHÔNG spawn ngẫu nhiên)")]
    [Tooltip("Quái rơi vật phẩm USB. ĐỪNG cho vào mảng Enemies ở trên - nó chỉ xuất hiện theo mốc số mạng, " +
             "không nằm trong vòng spawn ngẫu nhiên")]
    [SerializeField] private GameObject usbEnemyPrefab;
    [Tooltip("Cứ hạ đủ bấy nhiêu con quái (mọi loại) thì spawn 1 con USB Enemy")]
    [SerializeField] private int killsPerUsbEnemy = 50;

    private int killCount = 0;

    private void Awake()
    {
        Instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(SpawnEnemyCoroutine());
    }

    // Gọi từ Enemy.Die(). Boss override Die() mà không gọi base nên KHÔNG tính vào bộ đếm này - cố ý, vì mốc
    // 50 mạng là để thưởng cho việc dọn quái thường.
    public void OnEnemyKilled()
    {
        if (usbEnemyPrefab == null) return;

        killCount++;
        if (killCount < killsPerUsbEnemy) return;

        killCount = 0;
        SpawnUsbEnemy();
    }

    private void SpawnUsbEnemy()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.SpawnObject(usbEnemyPrefab, spawnPoint.position, Quaternion.identity);
        }
        else
        {
            Instantiate(usbEnemyPrefab, spawnPoint.position, Quaternion.identity);
        }
    }

    private IEnumerator SpawnEnemyCoroutine()
    {
        GameManager gameManager = GameManager.Instance;
        while (true) {
            if(gameManager.currentLevel == 5)
            {
                timeBetweenSpawns = 2f;
            }
            if (gameManager.currentLevel == 10)
            {
                timeBetweenSpawns = 1.5f;
            }
            yield return new WaitForSeconds(timeBetweenSpawns);
            GameObject enemy = enemies[Random.Range(0, enemies.Length)];
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            
            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.SpawnObject(enemy, spawnPoint.position, Quaternion.identity);
            }
            else
            {
                Instantiate(enemy, spawnPoint.position, Quaternion.identity);
            }
        }

    }
}
