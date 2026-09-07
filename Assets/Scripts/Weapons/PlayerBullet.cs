using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 25f;
    [SerializeField] private float timeDestroy = 0.5f;
    public float dmg = 10f;

    // Bán kính bay tối đa (0 = không giới hạn, dùng timeDestroy như bình thường). Gun set giá trị này khi spawn đạn Burst Shot.
    public float maxRange = 0f;
    private Vector3 spawnPosition;

    [Header("Splash (đạn Pháp sư)")]
    [Tooltip("0 = không nổ lan (đạn Gunner mặc định). > 0: bán kính gây thêm sát thương lan quanh mục tiêu trúng trực tiếp")]
    public float splashRadius = 0f;
    [Tooltip("Tỉ lệ % sát thương gốc gây cho các Enemy khác trong bán kính nổ lan")]
    public float splashDamagePercent = 0.5f;
    [Tooltip("Hiệu ứng animation hiện lên khi đạn nổ lan (để trống nếu không cần hiệu ứng)")]
    [SerializeField] private GameObject splashEffectPrefab;

    [SerializeField] private GameObject bloodPrefabs;

    private void Awake()
    {
        // Đạn tự di chuyển bằng code (transform.Translate), không cần mô phỏng vật lý.
        // Ép Kinematic để tránh trường hợp Rigidbody2D lỡ để Dynamic (VD trên prefab đạn Pháp sư)
        // khiến đạn spawn đè lên Collider của Player rồi bị engine vật lý đẩy giật lùi Player ra.
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    // Sử dụng OnEnable thay vì Start để có thể reset thời gian mỗi khi tái phát hành từ Pool
    private void OnEnable()
    {
        spawnPosition = transform.position;
        Invoke(nameof(DisableBullet), timeDestroy);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(DisableBullet));
    }

    private void DisableBullet()
    {
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnObjectToPool(gameObject);
        }
        else
        {
            Destroy(gameObject); // Fallback
        }
    }

    // Update is called once per frame
    void Update()
    {
        MoveBullet();

        if (maxRange > 0f && Vector3.Distance(spawnPosition, transform.position) >= maxRange)
        {
            DisableBullet();
        }
    }
    void MoveBullet()
    {
        transform.Translate(Vector2.right * moveSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDmg(dmg);

                // Gọi hàm hút máu từ Player
                if (Player.Instance != null)
                {
                    Player.Instance.OnEnemyHit(dmg);
                }

                if (splashRadius > 0f)
                {
                    ApplySplashDamage(enemy);
                }
            }

            // Xử lý hiệu ứng máu qua Pool nếu có
            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.SpawnObject(bloodPrefabs, transform.position, Quaternion.identity);
            }
            else
            {
                Instantiate(bloodPrefabs, transform.position, Quaternion.identity);
            }

            DisableBullet(); // Dọn dẹp đạn
        }
    }

    // Gây thêm sát thương lan cho các Enemy khác quanh mục tiêu trúng trực tiếp (đạn Pháp sư)
    private void ApplySplashDamage(Enemy directHitEnemy)
    {
        float splashDmg = dmg * splashDamagePercent;
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, splashRadius);

        foreach (Collider2D hit in hitColliders)
        {
            if (!hit.CompareTag("Enemy")) continue;

            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy == null || enemy == directHitEnemy) continue; // Mục tiêu trúng trực tiếp đã nhận đủ sát thương gốc rồi

            enemy.TakeDmg(splashDmg);
        }

        SpawnSplashEffect();
    }

    private void SpawnSplashEffect()
    {
        if (splashEffectPrefab == null) return;

        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.SpawnObject(splashEffectPrefab, transform.position, Quaternion.identity);
        }
        else
        {
            Instantiate(splashEffectPrefab, transform.position, Quaternion.identity);
        }
    }
}
