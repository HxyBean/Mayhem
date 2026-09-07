using System.Collections;
using UnityEngine;

public class BossEnemy : Enemy
{

    [SerializeField] private Transform firePos;// vị trí viên đạn bắn ra
    [SerializeField] private float speedNormalBullet = 20f;// tốc độ đạn thường
    [SerializeField] private float speedCircleBullet = 10f;// tốc độ đạn vòng tròn
    [SerializeField] private float healValue = 100f;// giá trị hồi máu
    [SerializeField] private float skillCoolDown = 2f;// thời gian hồi chiêu
    [SerializeField] private int miniSpawnAmount = 3;// số lượng mini khi dưới 50% máu
    private float nextSKillTime = 0f;// thời gian tung chiêu tiếp theo
    [SerializeField] private GameObject miniEnemy;
    [SerializeField] private GameObject usbPrefabs;
    [SerializeField] private GameObject bulletPrefabs;

    [Header("Teleport Skill")]
    [SerializeField] private float teleportTelegraphTime = 0.25f; // Thời gian đứng im vận chiêu trước khi dịch chuyển
    [SerializeField] private float teleportLandingRadius = 1.5f;  // Bán kính gây damage quanh điểm đáp xuống
    [SerializeField] private GameObject teleportTelegraphPrefab;  // Hiệu ứng cảnh báo vị trí sắp đáp xuống (kéo prefab vào)
    private bool isCastingSkill = false; // Boss đứng im khi đang vận chiêu (không di chuyển, không dùng chiêu khác)
    private GameObject activeTelegraph;

    protected override void OnEnable()
    {
        isCastingSkill = false;
        // base.OnEnable() tự tính maxHP = baseMaxHP * hệ số độ khó Stage (baseMaxHP được Die() cập nhật dần qua mỗi lần hồi sinh)
        base.OnEnable();

        if (GameManager.Instance != null && GameManager.Instance.currentLevel >= 15)
        {
            maxHP += 200f;
            currentHP = maxHP;
            UpdateHPBar();
        }
    }

    protected override void Update()
    {
        if (isCastingSkill) return; // Đứng im hoàn toàn trong lúc vận chiêu Dịch Chuyển

        if (Time.time >= nextSKillTime)// thời gian thực lớn hơn thời gian chiêu kế thì dùng skill
        {
            UseSkill();
        }
        base.Update();
    }

    private void NormalAtk()
    {
        if (player != null)
        {
            Vector3 directionToPlayer = player.transform.position - firePos.position; 
            directionToPlayer.Normalize();
            
            GameObject bullet;
            if (ObjectPoolManager.Instance != null)
                bullet = ObjectPoolManager.Instance.SpawnObject(bulletPrefabs, firePos.position, Quaternion.identity);
            else
                bullet = Instantiate(bulletPrefabs, firePos.position, Quaternion.identity);

            EnemyBullet enemyBullet = bullet.GetComponent<EnemyBullet>();
            if (enemyBullet == null) enemyBullet = bullet.AddComponent<EnemyBullet>(); // Dự phòng nếu user quên gắn component vào Prefab

            enemyBullet.SetMovementDirection(directionToPlayer * speedNormalBullet);
        }
    }

    private void CircleAtk()
    {
        const int bulletCount = 12;
        float angleStep = 360f / bulletCount;
        for (int i = 0; i < bulletCount; i++)
        {
            float angle = i * angleStep;
            Vector3 bulletDirection = new Vector3(Mathf.Cos(Mathf.Deg2Rad * angle), Mathf.Sin(Mathf.Deg2Rad * angle), 0);
            
            GameObject bullet;
            if (ObjectPoolManager.Instance != null)
                bullet = ObjectPoolManager.Instance.SpawnObject(bulletPrefabs, firePos.position, Quaternion.identity);
            else
                bullet = Instantiate(bulletPrefabs, firePos.position, Quaternion.identity);

            EnemyBullet enemyBullet = bullet.GetComponent<EnemyBullet>();
            if (enemyBullet == null) enemyBullet = bullet.AddComponent<EnemyBullet>(); 

            enemyBullet.SetMovementDirection(bulletDirection * speedCircleBullet);
        }
    }

    private void Heal(float HPAmount)
    {
        if (currentHP < maxHP)
        {
            currentHP += HPAmount;
            currentHP = Mathf.Min(currentHP, maxHP);
            UpdateHPBar();
        }
        else if (currentHP < maxHP / 2)
        {
            currentHP += maxHP / 4;
            currentHP = Mathf.Min(currentHP, maxHP);
            UpdateHPBar();
        }

    }

    private void SpawnMini()
    {
        float spawnRadius = 1.5f;
        if (currentHP <= maxHP / 2)
        {
            for (int i = 0; i < miniSpawnAmount; i++)
            {
                Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
                Vector3 spawnPosition = transform.position + (Vector3)randomOffset;
                
                if (ObjectPoolManager.Instance != null) ObjectPoolManager.Instance.SpawnObject(miniEnemy, spawnPosition, Quaternion.identity);
                else Instantiate(miniEnemy, spawnPosition, Quaternion.identity);
            }
        }
        else
        {
            if (ObjectPoolManager.Instance != null) ObjectPoolManager.Instance.SpawnObject(miniEnemy, transform.position, Quaternion.identity);
            else Instantiate(miniEnemy, transform.position, Quaternion.identity);
        }
    }

    private void Teleport()
    {
        if (player == null || currentHP > maxHP / 2) return;
        StartCoroutine(TeleportRoutine());
    }

    private IEnumerator TeleportRoutine()
    {
        // Khóa vị trí đích NGAY khi bắt đầu vận chiêu (trước khi delay 0.25s),
        // để Player có thời gian dash né trong lúc vận chiêu mà Boss không "đuổi theo" đích mới.
        Vector3 targetPosition = player.transform.position;
        isCastingSkill = true;

        if (teleportTelegraphPrefab != null)
        {
            activeTelegraph = ObjectPoolManager.Instance != null
                ? ObjectPoolManager.Instance.SpawnObject(teleportTelegraphPrefab, targetPosition, Quaternion.identity)
                : Instantiate(teleportTelegraphPrefab, targetPosition, Quaternion.identity);
        }

        yield return new WaitForSeconds(teleportTelegraphTime);

        ReturnTelegraph();

        transform.position = targetPosition;
        isCastingSkill = false;

        // Gây damage cơ bản của Boss nếu Player vẫn còn đứng gần điểm đáp xuống (chưa né kịp)
        if (player != null && Vector3.Distance(player.transform.position, targetPosition) <= teleportLandingRadius)
        {
            player.TakeDmg(enterDmg);
        }
    }

    private void ReturnTelegraph()
    {
        if (activeTelegraph == null) return;

        if (ObjectPoolManager.Instance != null) ObjectPoolManager.Instance.ReturnObjectToPool(activeTelegraph);
        else Destroy(activeTelegraph);

        activeTelegraph = null;
    }

    // Boss có thể bị hạ gục ngay trong lúc đang vận chiêu (bị SetActive(false) giữa chừng) —
    // dọn dẹp hiệu ứng cảnh báo để nó không bị kẹt lại trên màn hình.
    private void OnDisable()
    {
        isCastingSkill = false;
        ReturnTelegraph();
    }

    private void PickRandomSkill()
    {
        int randomSkill = Random.Range(0, 5);

        switch (randomSkill)
        {
            case 0:
                NormalAtk();
                break;
            case 1:
                CircleAtk();
                break;
            case 2:
                Heal(healValue);
                break;
            case 3:
                SpawnMini();
                break;
            case 4:
                Teleport();
                break;
        }
    }

    private void UseSkill()
    {
        nextSKillTime = Time.time + skillCoolDown;
        PickRandomSkill();
    }

    protected override void DropItems()
    {
        if (usbPrefabs != null) SpawnItem(usbPrefabs);
        if (xpObject != null) SpawnItem(xpObject); // 1 Boss Exp
    }

    protected override void Die()
    {
        if (isDead) return;
        isDead = true;

        DropItems();

        // Chuẩn bị máu cho lần revive tiếp theo - nhân vào giá trị GỐC (trước hệ số độ khó Stage)
        // để lần OnEnable sau tính lại đúng: maxHP = baseMaxHP (đã x1.5) * hệ số độ khó Stage
        baseMaxHP *= 1.5f;

        // Tắt boss - GameManager sẽ bật lại khi Player nhặt USB
        gameObject.SetActive(false);
    }
}

