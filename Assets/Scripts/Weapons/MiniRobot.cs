using UnityEngine;

// Robot nhỏ thả ra từ lõi riêng của nhân vật Robot: chạy thẳng theo hướng nòng súng lúc được thả, chạm Enemy
// thì phát nổ rồi tự dọn. Di chuyển bằng transform.Translate dựa theo rotation lúc spawn - giống PlayerBullet.
public class MiniRobot : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [Tooltip("Tự dọn sau khoảng thời gian này nếu chạy mãi không đụng con nào")]
    [SerializeField] private float timeDestroy = 5f;
    [Tooltip("Bán kính nổ lan sang các Enemy xung quanh. Để 0 = chỉ gây sát thương đúng con chạm vào")]
    [SerializeField] private float explosionRadius = 1f;
    [Tooltip("Hiệu ứng nổ - có thể dùng chung prefab nổ của Bomb")]
    [SerializeField] private GameObject explosionPrefab;

    // Sát thương do Gun tính sẵn (sát thương gốc của Player × hệ số) rồi truyền vào lúc thả
    private float dmg = 10f;
    private bool hasExploded = false;

    private void Awake()
    {
        // Tự di chuyển bằng code nên không cần mô phỏng vật lý; ép Kinematic để không bị va chạm đẩy lệch hướng
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void OnEnable()
    {
        // Reset vì object được tái sử dụng qua Pool
        hasExploded = false;
        Invoke(nameof(ReturnToPool), timeDestroy);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(ReturnToPool));
    }

    public void SetDamage(float damage)
    {
        dmg = damage;
    }

    private void Update()
    {
        transform.Translate(Vector2.right * moveSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasExploded) return;
        if (!collision.CompareTag("Enemy")) return;

        Enemy enemy = collision.GetComponent<Enemy>();
        if (enemy == null) return;

        Explode(enemy);
    }

    private void Explode(Enemy directHitEnemy)
    {
        hasExploded = true;

        DealDamage(directHitEnemy);

        if (explosionRadius > 0f)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
            foreach (Collider2D hit in hits)
            {
                if (!hit.CompareTag("Enemy")) continue;

                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy == null || enemy == directHitEnemy) continue; // Con chạm trực tiếp đã nhận damage rồi

                DealDamage(enemy);
            }
        }

        SpawnExplosionEffect();
        ReturnToPool();
    }

    private void DealDamage(Enemy enemy)
    {
        enemy.TakeDmg(dmg);
        if (Player.Instance != null) Player.Instance.OnEnemyHit(dmg);
    }

    private void SpawnExplosionEffect()
    {
        if (explosionPrefab == null) return;

        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.SpawnObject(explosionPrefab, transform.position, Quaternion.identity);
        }
        else
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }
    }

    private void ReturnToPool()
    {
        if (ObjectPoolManager.Instance != null) ObjectPoolManager.Instance.ReturnObjectToPool(gameObject);
        else Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        if (explosionRadius <= 0f) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
