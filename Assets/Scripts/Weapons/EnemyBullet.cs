using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    private Vector3 movementDirection;
    [SerializeField] private float damage = 10f;
    [Tooltip("Hiệu ứng nổ/va chạm khi đạn chạm Player hoặc vật cản")]
    [SerializeField] private GameObject hitEffectPrefab;
    private void OnEnable()
    {
        Invoke(nameof(DisableBullet), 5f);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(DisableBullet));
        movementDirection = Vector3.zero; // Reset vector tránh bay tiếp khi Tái sinh
    }

    private void DisableBullet()
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
    void Update()
    {
        if (movementDirection == Vector3.zero) return;
        transform.position += movementDirection * Time.deltaTime;
    }

    public void SetMovementDirection(Vector3 direction)
    {
        movementDirection = direction;
    }
    // Thiết lập riêng sát thương cho từng loại quái bắn (RangedEnemy/Boss)
    public void SetDamage(float dmg)
    {
        damage = dmg;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();
            if (player != null)
            {
                player.TakeDmg(damage);
            }

            SpawnHitEffect();
            DisableBullet();
        }
    }

    private void SpawnHitEffect()
    {
        if (hitEffectPrefab == null) return;

        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.SpawnObject(hitEffectPrefab, transform.position, Quaternion.identity);
        }
        else
        {
            GameObject effect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 1.5f);
        }
    }
}
