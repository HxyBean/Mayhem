using UnityEngine;

// Vùng độc do đạn của PoisonEnemy để lại khi rơi xuống. Gây sát thương cho PLAYER theo tick rồi tự dọn.
//
// CỐ Ý KHÔNG tái dùng PotionZone: vùng đó là đồ của người chơi nên nó quét tag "Enemy" và lấy damage từ
// Player.bulletDamage. Đây là hướng ngược lại hoàn toàn - gộp chung sẽ phải nhét cờ "bên nào" vào giữa và
// làm cả 2 khó đọc.
//
// Khác PotionZone ở 1 điểm nữa: đo khoảng cách thẳng tới Player.Instance thay vì dùng Collider + OverlapCircle.
// Cả màn chỉ có đúng 1 Player nên quét vật lý là thừa, và nhờ vậy prefab KHÔNG cần Collider2D/Rigidbody2D -
// bớt hẳn 2 thứ dễ quên khi dựng.
public class PoisonZone : MonoBehaviour
{
    [Tooltip("Thời gian vùng độc tồn tại (giây)")]
    [SerializeField] private float duration = 3f;
    [Tooltip("Bán kính gây sát thương. Nhớ chỉnh sprite cho khớp, người chơi đọc vùng nguy hiểm bằng mắt chứ " +
             "không thấy con số này")]
    [SerializeField] private float radius = 1.5f;
    [Tooltip("Khoảng cách giữa 2 lần gây sát thương (giây)")]
    [SerializeField] private float tickInterval = 0.5f;
    [Tooltip("Sát thương mỗi tick. PoisonProjectile có thể ghi đè giá trị này lúc spawn")]
    [SerializeField] private float damagePerTick = 5f;

    private float lifeTimer;
    private float nextTickTime;

    private void OnEnable()
    {
        lifeTimer = 0f;

        // Tick ĐẦU TIÊN lùi lại 1 nhịp thay vì gây sát thương ngay khoảnh khắc chạm đất: đứng đúng chỗ đạn rơi
        // thì đã lãnh đủ rồi, mất máu ngay lập tức nữa là không có cách nào phản ứng kịp.
        nextTickTime = Time.time + tickInterval;
    }

    // Gọi từ PoisonProjectile để con quái quyết định sát thương, thay vì phải làm nhiều prefab vùng độc
    public void SetDamagePerTick(float damage)
    {
        if (damage > 0f) damagePerTick = damage;
    }

    private void Update()
    {
        lifeTimer += Time.deltaTime;

        if (Time.time >= nextTickTime)
        {
            nextTickTime = Time.time + Mathf.Max(0.05f, tickInterval);
            DealTickDamage();
        }

        if (lifeTimer >= duration) ReturnToPool();
    }

    private void DealTickDamage()
    {
        if (Player.Instance == null) return;

        float distance = Vector2.Distance(transform.position, Player.Instance.transform.position);
        if (distance <= radius) Player.Instance.TakeDmg(damagePerTick);
    }

    private void ReturnToPool()
    {
        if (ObjectPoolManager.Instance != null) ObjectPoolManager.Instance.ReturnObjectToPool(gameObject);
        else Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 1f, 0.3f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
