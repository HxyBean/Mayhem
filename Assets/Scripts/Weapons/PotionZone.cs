using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Vùng tròn tồn tại 1 khoảng thời gian, gây sát thương theo tick + làm chậm Enemy đứng trong vùng.
// Spawn ra bởi Potion.cs khi viên thuốc đáp xuống đích.
// Cần 1 CircleCollider2D (Is Trigger) trên chính GameObject này để bắt sự kiện Enter/Exit cho hiệu ứng làm chậm.
public class PotionZone : MonoBehaviour
{
    [Header("Potion Zone Settings")]
    [Tooltip("Thời gian tồn tại của vùng (giây)")]
    [SerializeField] private float duration = 5f;
    [Tooltip("Khoảng cách giữa 2 lần gây damage (giây) - 0.25s = 1 tick")]
    [SerializeField] private float tickInterval = 0.25f;
    [Tooltip("Damage mỗi tick = sát thương gốc hiện tại của Player * hệ số này")]
    [SerializeField] private float damagePerTickMultiplier = 0.3f;
    [Tooltip("Bán kính vùng gây damage")]
    [SerializeField] private float radius = 2f;

    [Header("Làm chậm")]
    [Tooltip("% giảm tốc độ di chuyển của Enemy khi đứng trong vùng (0.3 = giảm 30%)")]
    [SerializeField, Range(0f, 1f)] private float slowPercent = 0.3f;

    private readonly List<Enemy> slowedEnemies = new List<Enemy>();

    private void Awake()
    {
        // Đồng bộ Collider2D theo đúng radius đã set, phòng trường hợp quên chỉnh tay trong Inspector
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
            col.radius = radius;
        }

        // Unity 2D cần ít nhất 1 bên có Rigidbody2D thì OnTrigger mới bắn ra - tự thêm cho chắc nếu quên gắn
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
    }

    private void OnEnable()
    {
        StartCoroutine(ZoneRoutine());
    }

    private void OnDisable()
    {
        // Đảm bảo không Enemy nào bị kẹt hiệu ứng chậm mãi mãi nếu vùng bị tắt đột ngột (hết giờ, trả về Pool...)
        foreach (Enemy enemy in slowedEnemies)
        {
            if (enemy != null) enemy.RemoveSlow();
        }
        slowedEnemies.Clear();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Enemy")) return;

        Enemy enemy = collision.GetComponent<Enemy>();
        if (enemy == null) return;

        enemy.ApplySlow(slowPercent);
        slowedEnemies.Add(enemy);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Enemy")) return;

        Enemy enemy = collision.GetComponent<Enemy>();
        if (enemy == null) return;

        enemy.RemoveSlow();
        slowedEnemies.Remove(enemy);
    }

    private IEnumerator ZoneRoutine()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;
            DealTickDamage();
        }

        ReturnToPool();
    }

    private void DealTickDamage()
    {
        float tickDamage = (Player.Instance != null) ? Player.Instance.bulletDamage * damagePerTickMultiplier : 0f;
        if (tickDamage <= 0f) return;

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (Collider2D hit in hitColliders)
        {
            if (!hit.CompareTag("Enemy")) continue;

            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null) enemy.TakeDmg(tickDamage);
        }
    }

    private void ReturnToPool()
    {
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnObjectToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Vẽ bán kính vùng trong Editor để debug
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 1f, 0.4f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
