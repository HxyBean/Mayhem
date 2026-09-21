using UnityEngine;

public class RangedEnemy : Enemy
{
    [Header("Ranged Attack Settings")]
    [SerializeField] private float stopRadius = 8f;         // Dừng lại khi khoảng cách <= 8
    [SerializeField] private float attackCoolDown = 2f;     // Thời gian chờ giữa các lần bắn
    [SerializeField] private float bulletSpeed = 12f;       // Tốc độ bay của viên đạn
    [SerializeField] private float bulletDamage = 10f;      // Sát thương viên đạn
    [SerializeField] private Transform firePos;             // Điểm bắn đạn (đầu nòng/tay quái)
    [SerializeField] private GameObject bulletPrefab;       // Prefab đạn dùng chung EnemyBullet

    private float nextAttackTime = 0f;

    protected override void OnEnable()
    {
        base.OnEnable();
        nextAttackTime = Time.time + attackCoolDown;
    }

    protected override void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);

        if (distanceToPlayer > stopRadius)
        {
            // Khoảng cách còn xa: tiếp tục di chuyển lại gần Player
            MoveToPlayer();
        }
        else
        {
            // Trong bán kính 8: đứng yên và xoay mặt nhìn theo Player
            FlipEnemy();

            // Đủ thời gian hồi chiêu thì bắn
            if (Time.time >= nextAttackTime)
            {
                ShootAtPlayer();
                nextAttackTime = Time.time + attackCoolDown;
            }
        }
    }
    protected override void DropItems()
    {
        if (bigXpObject != null)
        {
            int bigCount = UnityEngine.Random.Range(0, 2); // 0-1 viên to
            for (int i = 0; i < bigCount; i++) SpawnItem(bigXpObject);
        }

        if (xpObject != null)
        {
            int smallCount = UnityEngine.Random.Range(1, 3); // 1-2 viên nhỏ
            for (int i = 0; i < smallCount; i++) SpawnItem(xpObject);
        }
    }

    private void ShootAtPlayer()
    {
        if (bulletPrefab == null || player == null) return;

        Vector3 spawnPoint = (firePos != null) ? firePos.position : transform.position;
        Vector3 directionToPlayer = (player.transform.position - spawnPoint).normalized;

        GameObject bulletObj;
        if (ObjectPoolManager.Instance != null)
        {
            bulletObj = ObjectPoolManager.Instance.SpawnObject(bulletPrefab, spawnPoint, Quaternion.identity);
        }
        else
        {
            bulletObj = Instantiate(bulletPrefab, spawnPoint, Quaternion.identity);
        }

        EnemyBullet enemyBullet = bulletObj.GetComponent<EnemyBullet>();
        if (enemyBullet == null) enemyBullet = bulletObj.AddComponent<EnemyBullet>();

        enemyBullet.SetDamage(bulletDamage);
        enemyBullet.SetMovementDirection(directionToPlayer * bulletSpeed);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopRadius);
    }
}