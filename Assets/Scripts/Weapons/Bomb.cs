using UnityEngine;
using System.Collections;

public class Bomb : MonoBehaviour
{
    [Header("Bomb Settings")]
    [SerializeField] private float moveSpeed = 15f;
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float damageMultiplier = 3f; // Sát thương = bulletDamage * multiplier
    [SerializeField] private GameObject explosionPrefab; // Kéo thả hiệu ứng nổ vào đây

    private Vector3 targetPosition;
    private bool hasExploded = false;

    private void OnEnable()
    {
        hasExploded = false;
    }

    /// <summary>
    /// Gọi hàm này ngay sau khi Spawn bom để thiết lập đích đến.
    /// </summary>
    public void SetTarget(Vector3 target)
    {
        targetPosition = target;
        hasExploded = false;
    }

    void Update()
    {
        if (hasExploded) return;

        // Bay về phía đích
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        // Kiểm tra đã tới đích chưa
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        // Tính sát thương dựa trên chỉ số Player
        float bombDamage = 10f;
        if (Player.Instance != null)
        {
            bombDamage = Player.Instance.bulletDamage * damageMultiplier;
        }

        // Tìm tất cả Enemy trong bán kính nổ
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D hit in hitEnemies)
        {
            if (hit.CompareTag("Enemy"))
            {
                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDmg(bombDamage);
                }
            }
        }

        // Spawn hiệu ứng nổ
        if (explosionPrefab != null)
        {
            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.SpawnObject(explosionPrefab, transform.position, Quaternion.identity);
            }
            else
            {
                Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            }
        }

        // Trả bom về Pool
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnObjectToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Vẽ bán kính nổ trong Editor để debug
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
