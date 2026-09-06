using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 25f;
    [SerializeField] private float timeDestroy = 0.5f;
    public float dmg = 10f;

    // Bán kính bay tối đa (0 = không giới hạn, dùng timeDestroy như bình thường). Gun set giá trị này khi spawn đạn Burst Shot.
    public float maxRange = 0f;
    private Vector3 spawnPosition;

    [SerializeField] private GameObject bloodPrefabs;
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
}
