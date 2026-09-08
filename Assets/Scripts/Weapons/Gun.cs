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

    [Header("Potion (Pháp sư)")]
    [SerializeField] private GameObject potionPrefab;      // Kéo Prefab viên thuốc vào đây
    private bool hasPotion = false;
    [SerializeField] private float potionCooldown = 6f;
    private bool isPotionOnCooldown = false;
    [SerializeField] private GameObject potionButtonObj;   // Gán PotionButton vào đây
    [SerializeField] private Image potionCooldownBar;      // Gán Overlay Cooldown của PotionButton

    [Header("Character Visual/Audio Override")]
    [Tooltip("SpriteRenderer của vũ khí (súng/gậy phép) - đổi hình theo nhân vật được chọn")]
    [SerializeField] private SpriteRenderer weaponRenderer;
    [Tooltip("Image của nút Bắn - đổi icon theo nhân vật được chọn")]
    [SerializeField] private Image shootButtonImage;
    [Tooltip("Image của nút Nạp đạn - đổi icon theo nhân vật được chọn")]
    [SerializeField] private Image reloadButtonImage;
    [Tooltip("GameObject chứa nút Bắn - tự ẩn khi chọn nhân vật cận chiến (Knight)")]
    [SerializeField] private GameObject shootButtonObj;
    [Tooltip("GameObject chứa nút Nạp đạn - tự ẩn khi chọn nhân vật cận chiến (Knight)")]
    [SerializeField] private GameObject reloadButtonObj;
    private AudioClip characterShootClip;  // null = dùng âm thanh mặc định của AudioManager (Gunner)
    private AudioClip characterReloadClip; // null = dùng âm thanh mặc định của AudioManager (Gunner)

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
        if (potionButtonObj != null) potionButtonObj.SetActive(false);
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

        // Ném bình thuốc bằng phím T (PC)
        if (!isMobile && CanThrowPotion() && Input.GetKeyDown(KeyCode.T))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            ThrowPotion(mouseWorldPos);
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
        if (characterShootClip != null) audioManager.PlaySFX(characterShootClip);
        else audioManager.PlayShootSound();
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

    public bool CanThrowPotion()
    {
        return hasPotion && !isPotionOnCooldown && currentAmmo >= 5;
    }

    public void ThrowPotion(Vector3 targetPosition)
    {
        if (potionPrefab == null) return;

        currentAmmo -= 10;
        UpdateAmmoText();

        GameObject potion;
        if (ObjectPoolManager.Instance != null)
        {
            potion = ObjectPoolManager.Instance.SpawnObject(potionPrefab, transform.position, Quaternion.identity);
        }
        else
        {
            potion = Instantiate(potionPrefab, transform.position, Quaternion.identity);
        }

        Potion potionScript = potion.GetComponent<Potion>();
        if (potionScript != null)
        {
            targetPosition.z = 0f;
            potionScript.SetTarget(targetPosition);
        }

        StartCoroutine(PotionCooldownCoroutine());
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

        if (characterReloadClip != null) audioManager.PlaySFX(characterReloadClip);
        else audioManager.PlayReloadSound();

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

    public void EnablePotion()
    {
        hasPotion = true;
        isPotionOnCooldown = false;
        if (potionButtonObj != null) potionButtonObj.SetActive(true); // Hiển thị nút Potion
        Debug.Log("Đã mở khóa Ném Bình Thuốc!");
    }

    /// <summary>
    /// Coroutine hồi chiêu Potion giống cơ chế Bomb
    /// </summary>
    private IEnumerator PotionCooldownCoroutine()
    {
        isPotionOnCooldown = true;

        float timer = 0f;
        if (potionCooldownBar != null) potionCooldownBar.fillAmount = 1f;

        while (timer < potionCooldown)
        {
            timer += Time.deltaTime;
            if (potionCooldownBar != null)
            {
                potionCooldownBar.fillAmount = 1f - (timer / potionCooldown);
            }
            yield return null;
        }

        // Hồi chiêu xong
        if (potionCooldownBar != null) potionCooldownBar.fillAmount = 0f;
        isPotionOnCooldown = false;
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

    // Gọi từ GameManager.Start() - bật/tắt toàn bộ cơ chế bắn súng + nút Bắn/Nạp đạn tùy theo
    // nhân vật đang chọn có phải Ranged (Gunner/Mage) hay không (Knight dùng KnightCombat thay thế)
    public void SetActive(bool active)
    {
        enabled = active;
        // Tắt Update() không tự ẩn sprite vũ khí (nó vẫn đứng yên và hiển thị) - phải ẩn riêng SpriteRenderer
        if (weaponRenderer != null) weaponRenderer.enabled = active;
        if (shootButtonObj != null) shootButtonObj.SetActive(active);
        if (reloadButtonObj != null) reloadButtonObj.SetActive(active);

        // Nếu Gun bị tắt NGAY từ đầu (VD chọn Knight), Start() của Gun sẽ không bao giờ tự chạy
        // (Unity chỉ gọi Start() khi component đang bật) - nên không thể trông cậy vào dòng
        // "bombButtonObj.SetActive(false)" trong Start() để ẩn 2 nút này, phải tự ẩn ở đây luôn.
        if (!active)
        {
            if (bombButtonObj != null) bombButtonObj.SetActive(false);
            if (potionButtonObj != null) potionButtonObj.SetActive(false);
        }
    }

    // Gọi từ GameManager.Start() theo nhân vật đã chọn ở Character Select
    public void ApplyCharacterData(CharacterData character)
    {
        if (character == null) return;

        if (character.bulletPrefab != null) bulletPrefabs = character.bulletPrefab;
        if (character.weaponSprite != null && weaponRenderer != null) weaponRenderer.sprite = character.weaponSprite;
        if (character.shootButtonIcon != null && shootButtonImage != null) shootButtonImage.sprite = character.shootButtonIcon;
        if (character.reloadButtonIcon != null && reloadButtonImage != null) reloadButtonImage.sprite = character.reloadButtonIcon;

        characterShootClip = character.shootSound;
        characterReloadClip = character.reloadSound;
    }

    private bool isManaRegenActive = false;
    private float manaRegenPerSecond = 0f;
    private float manaRegenAccumulator = 0f; // Gom phần lẻ (VD 0.25/s) qua nhiều giây tới khi đủ 1 đơn vị nguyên mới cộng vào currentAmmo (số nguyên)

    // Hồi dần Ammo/Mana theo thời gian - cơ chế mới, dùng cho augment "Mana Regen" của Pháp sư
    public void StartManaRegen(float amountPerSecond)
    {
        if (!isManaRegenActive)
        {
            manaRegenPerSecond = amountPerSecond;
            StartCoroutine(ManaRegenCoroutine());
        }
        else
        {
            manaRegenPerSecond += amountPerSecond; // Cộng dồn nếu chọn augment này nhiều lần
        }
    }

    public float GetManaRegenPerSecond() => manaRegenPerSecond;

    private IEnumerator ManaRegenCoroutine()
    {
        isManaRegenActive = true;
        while (true)
        {
            yield return new WaitForSeconds(1f);

            if (currentAmmo < maxAmmo && !isReloading)
            {
                // manaRegenPerSecond có thể là số lẻ (VD 0.25) - gom dần qua nhiều giây, đủ 1 đơn vị nguyên mới cộng vào currentAmmo
                manaRegenAccumulator += manaRegenPerSecond;
                int wholeAmount = Mathf.FloorToInt(manaRegenAccumulator);
                if (wholeAmount > 0)
                {
                    manaRegenAccumulator -= wholeAmount;
                    currentAmmo = Mathf.Min(currentAmmo + wholeAmount, maxAmmo);
                    UpdateAmmoText();
                }
            }
        }
    }
}
