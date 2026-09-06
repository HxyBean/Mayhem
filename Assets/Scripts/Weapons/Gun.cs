using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public enum ShootMode
{
    Normal,  // Bắn 1 viên
    Burst,   // Bắn 3 viên song song
    Split    // Bắn 3 viên tỏa ra (lệch 15 độ)
}

public class Gun : MonoBehaviour
{
    [Header("Gun Settings")]
    private float rotateOffset = 180f;
    [SerializeField] private Transform firePos;
    [SerializeField] private GameObject bulletPrefabs;
    [SerializeField] private float shotDelay = 0.15f;
    private float nextShot;
    private bool isReloading = false;
    [SerializeField] private int maxAmmo = 24;
    [SerializeField] private float defaultReloadTime = 1.2f;
    private float currentReloadTime;
    private int reloadAugmentCount = 0; // Đếm số lần đã nâng cấp nạp đạn
    public int currentAmmo;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private AudioManager audioManager;

    [Header("Mobile Settings")]
    public bool isMobile = true;
    [SerializeField] private float autoAimRadius = 15f;
    [SerializeField] private Image reloadCooldownBar;

    [Header("Shoot Mode")]
    public ShootMode currentShootMode = ShootMode.Normal;
    [SerializeField] private float burstSpacing = 0.3f; // Khoảng cách giữa các viên đạn Burst (đơn vị Unity)
    [SerializeField] private float burstMaxRange = 5f;   // Đạn Burst Shot chỉ bay tối đa bán kính này rồi biến mất
    [SerializeField] private float splitAngle = 15f;     // Góc lệch mỗi viên trong Split (độ)

    [Header("Bomb")]
    [SerializeField] private GameObject bombPrefab;       // Kéo Prefab bom vào đây
    private bool hasBomb = false;
    [SerializeField] private float bombCooldown = 5f;
    private bool isBombOnCooldown = false;
    [SerializeField] private GameObject bombButtonObj;    // Gán BombButton vào đây
    [SerializeField] private Image bombCooldownBar;       // Gán Overlay Cooldown của BombButton

    private ContactFilter2D contactFilter;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        contactFilter.useTriggers = true;
        contactFilter.SetLayerMask(Physics2D.AllLayers);
        contactFilter.useLayerMask = true;
        currentAmmo = maxAmmo;
        UpdateAmmoText();
        currentReloadTime = defaultReloadTime;
        if (bombButtonObj != null) bombButtonObj.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        // Kiểm tra nạp đạn bằng phím (dành cho PC)
        if (!isMobile && Input.GetMouseButtonDown(1) && currentAmmo < maxAmmo && !isReloading)
        {
            StartCoroutine(ExecuteReload());
        }
        
        RotateGun();
        
        if (!isMobile) Shoot();

        // Ném bom bằng phím R (PC)
        if (!isMobile && CanThrowBomb() && Input.GetKeyDown(KeyCode.R))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            ThrowBomb(mouseWorldPos);
        }
    }

    private Collider2D[] enemyColliders = new Collider2D[100];
    private Transform currentTarget;
    private float targetSearchTimer = 0f;
    private const float TARGET_SEARCH_INTERVAL = 0.1f;

    void RotateGun()
    {
        if (isMobile)
        {
            targetSearchTimer -= Time.deltaTime;
            if (targetSearchTimer <= 0f || currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
            {
                currentTarget = FindClosestEnemy();
                targetSearchTimer = TARGET_SEARCH_INTERVAL;
            }

            if (currentTarget != null)
            {
                Vector3 displacement = transform.position - currentTarget.position;
                float angle = Mathf.Atan2(displacement.y, displacement.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle + rotateOffset);

                if (angle < -90 || angle > 90)
                    transform.localScale = new Vector3(1, 1, 1);
                else
                    transform.localScale = new Vector3(1, -1, 1);
            }
        }
        else
        {
            if (Input.mousePosition.x < 0 || Input.mousePosition.x > Screen.width || Input.mousePosition.y < 0 || Input.mousePosition.y > Screen.height) return;

            Vector3 displacement = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
            float angle = Mathf.Atan2(displacement.y, displacement.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle + rotateOffset);

            if (angle < -90 || angle > 90)
            {
                transform.localScale = new Vector3(1, 1, 1);
            }
            else
            {
                transform.localScale = new Vector3(1, -1, 1);
            }
        }
    }

    private Transform FindClosestEnemy()
    {
        int count = Physics2D.OverlapCircle(transform.position, autoAimRadius, contactFilter, enemyColliders);
        Transform closest = null;
        float minSqrDistance = float.MaxValue;
        
        for (int i = 0; i < count; i++)
        {
            Collider2D col = enemyColliders[i];
            if (col.CompareTag("Enemy"))
            {
                float sqrDist = (transform.position - col.transform.position).sqrMagnitude;
                if (sqrDist < minSqrDistance)
                {
                    minSqrDistance = sqrDist;
                    closest = col.transform;
                }
            }
        }
        return closest;
    }

    void Shoot()
    {
        if (Input.GetMouseButtonDown(0) && currentAmmo > 0 && Time.time > nextShot && !isReloading)
        {
            PerformShoot();
        }
    }

    public void OnAttackButtonPressed()
    {
        if (currentAmmo > 0 && Time.time > nextShot && !isReloading)
        {
            PerformShoot();
        }
    }

    private void PerformShoot()
    {
        nextShot = Time.time + shotDelay;
        float currentDamage = (Player.Instance != null) ? Player.Instance.bulletDamage : 10f;

        switch (currentShootMode)
        {
            case ShootMode.Normal:
                SpawnBullet(firePos.position, firePos.rotation, currentDamage);
                currentAmmo--;
                break;

            case ShootMode.Burst:
                if (currentAmmo < 3) break; // Cần ít nhất 3 viên
                ShootBurst(currentDamage * 0.8f); // Giảm 20% sát thương mỗi viên
                currentAmmo -= 3;
                break;

            case ShootMode.Split:
                if (currentAmmo < 3) break; // Cần ít nhất 3 viên
                ShootSplit(currentDamage * 0.8f); // Giảm 20% sát thương mỗi viên
                currentAmmo -= 3;
                break;
        }

        UpdateAmmoText();
        audioManager.PlayShootSound();
    }

    /// <summary>
    /// Bắn 3 viên đạn song song (lệch vị trí theo trục vuông góc với nòng súng)
    /// </summary>
    private void ShootBurst(float damage)
    {
        // Viên giữa (gốc)
        SpawnBullet(firePos.position, firePos.rotation, damage, burstMaxRange);

        // Tính hướng vuông góc với nòng súng để lệch vị trí
        Vector3 perpendicular = firePos.up; // Trục vuông góc với hướng bắn

        // Viên trên
        Vector3 posUp = firePos.position + perpendicular * burstSpacing;
        SpawnBullet(posUp, firePos.rotation, damage, burstMaxRange);

        // Viên dưới
        Vector3 posDown = firePos.position - perpendicular * burstSpacing;
        SpawnBullet(posDown, firePos.rotation, damage, burstMaxRange);
    }

    /// <summary>
    /// Bắn 3 viên đạn tỏa ra hình rẻ quạt (chênh 15 độ mỗi viên)
    /// </summary>
    private void ShootSplit(float damage)
    {
        // Viên giữa (thẳng)
        SpawnBullet(firePos.position, firePos.rotation, damage);

        // Viên lệch trái (-15 độ)
        Quaternion leftRotation = firePos.rotation * Quaternion.Euler(0, 0, splitAngle);
        SpawnBullet(firePos.position, leftRotation, damage);

        // Viên lệch phải (+15 độ)
        Quaternion rightRotation = firePos.rotation * Quaternion.Euler(0, 0, -splitAngle);
        SpawnBullet(firePos.position, rightRotation, damage);
    }

    /// <summary>
    /// Hàm dùng chung để spawn 1 viên đạn tại vị trí & góc cho trước.
    /// maxRange = 0 nghĩa là không giới hạn tầm bay (dùng timeDestroy như bình thường).
    /// </summary>
    private void SpawnBullet(Vector3 position, Quaternion rotation, float damage, float maxRange = 0f)
    {
        GameObject bullet;
        if (ObjectPoolManager.Instance != null)
        {
            bullet = ObjectPoolManager.Instance.SpawnObject(bulletPrefabs, position, rotation);
        }
        else
        {
            bullet = Instantiate(bulletPrefabs, position, rotation);
        }

        PlayerBullet bulletScript = bullet.GetComponent<PlayerBullet>();
        bulletScript.dmg = damage;
        // Luôn set lại maxRange vì đạn được tái sử dụng từ Pool có thể còn giữ giá trị của lần bắn Burst trước đó
        bulletScript.maxRange = maxRange;
    }

    public bool CanThrowBomb()
    {
        return hasBomb && !isBombOnCooldown && currentAmmo >= 5;
    }

    public void ThrowBomb(Vector3 targetPosition)
    {
        if (bombPrefab == null) return;
        
        currentAmmo -= 10;
        UpdateAmmoText();

        GameObject bomb;
        if (ObjectPoolManager.Instance != null)
        {
            bomb = ObjectPoolManager.Instance.SpawnObject(bombPrefab, transform.position, Quaternion.identity);
        }
        else
        {
            bomb = Instantiate(bombPrefab, transform.position, Quaternion.identity);
        }

        Bomb bombScript = bomb.GetComponent<Bomb>();
        if (bombScript != null)
        {
            targetPosition.z = 0f;
            bombScript.SetTarget(targetPosition);
        }

        StartCoroutine(BombCooldownCoroutine());
    }

    public void OnReloadButtonPressed()
    {
        if (currentAmmo < maxAmmo && !isReloading)
        {
            StartCoroutine(ExecuteReload());
        }
    }

    private IEnumerator ExecuteReload()
    {
        isReloading = true;
        Debug.Log("Đang nạp đạn...");

        audioManager.PlayReloadSound();

        float timer = 0f;
        if (reloadCooldownBar != null) reloadCooldownBar.fillAmount = 1f;

        while (timer < currentReloadTime)
        {
            timer += Time.deltaTime;
            if (reloadCooldownBar != null)
            {
                reloadCooldownBar.fillAmount = 1f - (timer / currentReloadTime);
            }
            yield return null;
        }

        if (reloadCooldownBar != null) reloadCooldownBar.fillAmount = 0f;

        currentAmmo = maxAmmo;
        UpdateAmmoText();
        isReloading = false;
        Debug.Log("Nạp đạn xong!");
    }

    private void UpdateAmmoText()
    {
        if (ammoText != null)
        {
            if (currentAmmo > 0)
            {
                ammoText.text = currentAmmo.ToString();
            }
            else
            {
                ammoText.text = "EMPTY";
            }
        }
    }

    // ====== PUBLIC METHODS (Gọi từ AugmentManager) ======
    public void SetShootMode(ShootMode mode)
    {
        currentShootMode = mode;
        Debug.Log("Chế độ bắn: " + mode.ToString());
    }

    public void EnableBomb()
    {
        hasBomb = true;
        isBombOnCooldown = false;
        if (bombButtonObj != null) bombButtonObj.SetActive(true); // Hiển thị nút bom
        Debug.Log("Đã mở khóa Ném Bom!");
    }

    /// <summary>
    /// Coroutine hồi chiêu bom giống cơ chế Dash
    /// </summary>
    private IEnumerator BombCooldownCoroutine()
    {
        isBombOnCooldown = true;

        float timer = 0f;
        if (bombCooldownBar != null) bombCooldownBar.fillAmount = 1f;

        while (timer < bombCooldown)
        {
            timer += Time.deltaTime;
            if (bombCooldownBar != null)
            {
                bombCooldownBar.fillAmount = 1f - (timer / bombCooldown);
            }
            yield return null;
        }

        // Hồi chiêu xong
        if (bombCooldownBar != null) bombCooldownBar.fillAmount = 0f;
        isBombOnCooldown = false;
    }

    public void AddAmmo(int amount)
    {
        maxAmmo += amount;
    }

    public void ReduceReloadTime(float amount)
    {
        if (reloadAugmentCount < 3) // Giới hạn tối đa 2 lần
        {
            currentReloadTime -= amount;
            reloadAugmentCount++;
            Debug.Log("Thời gian nạp đạn còn: " + currentReloadTime);
        }
    }

    public int GetReloadAugmentCount()
    {
        return reloadAugmentCount;
    }
}
