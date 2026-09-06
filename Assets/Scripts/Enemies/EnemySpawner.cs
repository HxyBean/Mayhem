using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] enemies;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float timeBetweenSpawns = 3f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(SpawnEnemyCoroutine());
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
