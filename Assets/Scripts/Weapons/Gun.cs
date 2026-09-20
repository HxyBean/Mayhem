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

    [Header("Laser (Robot) - tích charge từ đòn bắn thường")]
    [Tooltip("Số đòn bắn thường cần tích để dùng được 1 phát Laser")]
    [SerializeField] private int laserChargeRequired = 10;
    [Tooltip("Tầm bắn của Laser tính từ nòng súng")]
    [SerializeField] private float laserRange = 12f;
    [Tooltip("Bề rộng vùng trúng đòn của Laser - nên khớp với độ dày sprite hiệu ứng")]
    [SerializeField] private float laserWidth = 1f;
    [Tooltip("Hệ số sát thương so với sát thương gốc của Player")]
    [SerializeField] private float laserDamageMultiplier = 1.5f;
    [Tooltip("Prefab hiệu ứng/animation tia Laser - tự dọn qua AutoDestroyOrPool gắn sẵn trên prefab")]
    [SerializeField] private GameObject laserEffectPrefab;
    [Tooltip("GameObject nút bắn Laser - chỉ hiện với nhân vật không dùng đạn (Robot)")]
    [SerializeField] private GameObject laserButtonObj;
    [Tooltip("Nút bắn Laser - tự làm mờ khi chưa tích đủ charge. Có thể để trống")]
    [SerializeField] private Button laserButton;
    private int currentCharge = 0;

    [Header("Mini Robot (lõi riêng của Robot)")]
    [SerializeField] private GameObject miniRobotPrefab;
    [SerializeField] private int miniRobotCount = 3;
    [Tooltip("Khoảng cách lệch nhau giữa các con, tính theo trục vuông góc nòng súng (để không chồng lên nhau)")]
    [SerializeField] private float miniRobotSpacing = 0.4f;
    [Tooltip("Hệ số sát thương lúc nổ so với sát thương gốc của Player")]
    [SerializeField] private float miniRobotDamageMultiplier = 2f;
    [SerializeField] private float miniRobotCooldown = 20f;
    [SerializeField] private GameObject miniRobotButtonObj;
    [SerializeField] private Image miniRobotCooldownBar;
    private bool hasMiniRobot = false;
    private bool isMiniRobotOnCooldown = false;

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

    // Nhân vật hiện tại có dùng đạn/mana không (lấy từ CharacterData.usesAmmo). Robot = false: bắn miễn phí,
    // không nạp đạn, ô text đạn chuyển thành bộ đếm charge Laser.
    private bool usesAmmo = true;

    private ContactFilter2D contactFilter;

    // Vị trí gốc của vũ khí đặt sẵn trong Scene. Chốt ở Awake() để weaponOffset của từng nhân vật luôn cộng vào
    // MỘT mốc cố định, không bị dồn thêm nếu ApplyCharacterData() chẳng may chạy nhiều lần.
    // Dùng Awake chứ không phải Start vì Unity vẫn gọi Awake kể cả khi component đang bị disable (VD chọn Knight).
    private Vector3 baseLocalPosition;

    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
    }

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
        // Nút Mini Robot ẩn tới khi chọn được lõi (giống Bomb/Potion). KHÔNG đụng tới laserButtonObj ở đây:
        // ApplyCharacterData() mới là nơi quyết định nút Laser hiện hay ẩn, mà Start() có thể chạy SAU hàm đó
        // (Unity không đảm bảo thứ tự Start giữa các MonoBehaviour) nên ẩn ở đây sẽ ẩn nhầm nút của Robot.
        if (miniRobotButtonObj != null) miniRobotButtonObj.SetActive(false);
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

    // Nhân vật không dùng đạn (Robot) thì luôn đủ điều kiện bắn - chỉ bị giới hạn bởi shotDelay
    private bool HasAmmo(int amount)
    {
        return !usesAmmo || currentAmmo >= amount;
    }

    private void ConsumeAmmo(int amount)
    {
        if (!usesAmmo) return;
        currentAmmo -= amount;
    }

    void Shoot()
    {
        if (Input.GetMouseButtonDown(0) && HasAmmo(1) && Time.time > nextShot && !isReloading)
        {
            PerformShoot();
        }
    }

    public void OnAttackButtonPressed()
    {
        if (HasAmmo(1) && Time.time > nextShot && !isReloading)
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
                ConsumeAmmo(1);
                break;

            case ShootMode.Burst:
                if (!HasAmmo(3)) break; // Cần ít nhất 3 viên
                ShootBurst(currentDamage * 0.4f); // Giảm 60% sát thương mỗi viên
                ConsumeAmmo(3);
                break;

            case ShootMode.Split:
                if (!HasAmmo(3)) break; // Cần ít nhất 3 viên
                ShootSplit(currentDamage * 0.4f); // Giảm 60% sát thương mỗi viên
                ConsumeAmmo(3);
                break;
        }

        AddLaserCharge();
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
        // Nhân vật không dùng đạn (Robot): tận dụng luôn ô text này để hiện số charge đã tích cho Laser
        if (!usesAmmo)
        {
            if (ammoText != null) ammoText.text = currentCharge + "/" + laserChargeRequired;
            if (laserButton != null) laserButton.interactable = CanFireLaser();
            return;
        }

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

    // ====== LASER (Robot) ======
    // Mỗi đòn bắn thường tích 1 charge, đủ laserChargeRequired thì mở khóa 1 phát Laser
    private void AddLaserCharge()
    {
        if (usesAmmo) return; // Chỉ nhân vật không dùng đạn mới có cơ chế charge

        if (currentCharge < laserChargeRequired) currentCharge++;
    }

    public bool CanFireLaser()
    {
        return !usesAmmo && currentCharge >= laserChargeRequired;
    }

    // Laser là chiêu TỨC THÌ, không phải viên đạn bay: quét ngay 1 vùng chữ nhật theo hướng được chọn và gây
    // sát thương cho MỌI Enemy nằm trong đó (xuyên thấu - không bị chặn lại ở con đầu tiên).
    // Hướng do LaserButton truyền vào (kéo thả chọn hướng), KHÔNG lấy theo nòng súng nữa.
    public void FireLaser(Vector2 direction)
    {
        if (!CanFireLaser()) return;
        if (direction.sqrMagnitude < 0.0001f) return;

        direction = direction.normalized;
        currentCharge = 0;
        UpdateAmmoText();

        float baseDamage = (Player.Instance != null) ? Player.Instance.bulletDamage : 10f;
        float laserDamage = baseDamage * laserDamageMultiplier;

        // Bắn từ tâm NHÂN VẬT chứ không phải nòng súng: súng vẫn tự auto-aim vào con gần nhất nên có thể đang
        // chĩa hẳn hướng khác với hướng vừa kéo - lấy nòng súng làm gốc sẽ thấy tia mọc ra từ phía sau lưng.
        Vector2 origin = (Player.Instance != null) ? (Vector2)Player.Instance.transform.position : (Vector2)firePos.position;
        Vector2 center = origin + direction * (laserRange * 0.5f);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, new Vector2(laserRange, laserWidth), angle);
        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;

            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy == null) continue;

            enemy.TakeDmg(laserDamage);
            if (Player.Instance != null) Player.Instance.OnEnemyHit(laserDamage);
        }

        SpawnLaserEffect(origin, angle);
    }

    private void SpawnLaserEffect(Vector3 position, float angle)
    {
        if (laserEffectPrefab == null) return;

        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
        GameObject effect = ObjectPoolManager.Instance != null
            ? ObjectPoolManager.Instance.SpawnObject(laserEffectPrefab, position, rotation)
            : Instantiate(laserEffectPrefab, position, rotation);

        // Gắn làm con của Player để tia bám theo nhân vật lúc đang chạy, thay vì đứng lại chỗ vừa bắn - cùng
        // cách với hiệu ứng chém tự động của Knight (KnightCombat.SpawnEffect với attachToPlayer = true).
        // Gắn vào PLAYER chứ không phải Gun: Gun tự xoay auto-aim liên tục, gắn vào đó thì tia sẽ quay theo nòng.
        // Player chỉ lật sprite bằng flipX chứ không xoay transform nên hướng tia giữ nguyên.
        // ObjectPoolManager.ReturnObjectToPool() tự gỡ parent trước khi trả về Pool nên không cần dọn thủ công.
        if (effect != null && Player.Instance != null)
        {
            effect.transform.SetParent(Player.Instance.transform);
            effect.transform.localPosition = Vector3.zero;
        }
    }

    // Vẽ vùng trúng đòn của Laser trong Editor để căn cho khớp sprite hiệu ứng.
    // Vẽ từ tâm nhân vật vì Laser bắn từ đó (xem FireLaser); hướng lấy tạm theo nòng súng để xem tỉ lệ dài/rộng.
    private void OnDrawGizmosSelected()
    {
        if (firePos == null) return;

        Vector3 origin = (transform.parent != null) ? transform.parent.position : transform.position;

        Gizmos.color = Color.cyan;
        Gizmos.matrix = Matrix4x4.TRS(origin + firePos.right * (laserRange * 0.5f), firePos.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(laserRange, laserWidth, 0f));
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

    public void EnableMiniRobot()
    {
        hasMiniRobot = true;
        isMiniRobotOnCooldown = false;
        if (miniRobotButtonObj != null) miniRobotButtonObj.SetActive(true);
        Debug.Log("Đã mở khóa Mini Robot!");
    }

    public bool CanReleaseMiniRobot()
    {
        return hasMiniRobot && !isMiniRobotOnCooldown;
    }

    // Gán vào OnClick của nút Mini Robot
    public void OnMiniRobotButtonPressed()
    {
        if (!CanReleaseMiniRobot()) return;

        ReleaseMiniRobots();
        StartCoroutine(MiniRobotCooldownCoroutine());
    }

    // Thả 3 con robot nhỏ cùng chạy theo hướng nòng súng, xếp lệch nhau theo trục vuông góc để không chồng lên
    // nhau (cùng kỹ thuật với ShootBurst).
    private void ReleaseMiniRobots()
    {
        if (miniRobotPrefab == null) return;

        float baseDamage = (Player.Instance != null) ? Player.Instance.bulletDamage : 10f;
        float explodeDamage = baseDamage * miniRobotDamageMultiplier;
        Vector3 perpendicular = firePos.up;

        for (int i = 0; i < miniRobotCount; i++)
        {
            float offset = (i - (miniRobotCount - 1) * 0.5f) * miniRobotSpacing;
            Vector3 spawnPos = firePos.position + perpendicular * offset;

            GameObject robotObj = ObjectPoolManager.Instance != null
                ? ObjectPoolManager.Instance.SpawnObject(miniRobotPrefab, spawnPos, firePos.rotation)
                : Instantiate(miniRobotPrefab, spawnPos, firePos.rotation);

            MiniRobot miniRobot = robotObj.GetComponent<MiniRobot>();
            if (miniRobot != null) miniRobot.SetDamage(explodeDamage);
        }
    }

    private IEnumerator MiniRobotCooldownCoroutine()
    {
        isMiniRobotOnCooldown = true;

        float timer = 0f;
        if (miniRobotCooldownBar != null) miniRobotCooldownBar.fillAmount = 1f;

        while (timer < miniRobotCooldown)
        {
            timer += Time.deltaTime;
            if (miniRobotCooldownBar != null)
            {
                miniRobotCooldownBar.fillAmount = 1f - (timer / miniRobotCooldown);
            }
            yield return null;
        }

        if (miniRobotCooldownBar != null) miniRobotCooldownBar.fillAmount = 0f;
        isMiniRobotOnCooldown = false;
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
            if (laserButtonObj != null) laserButtonObj.SetActive(false);
            if (miniRobotButtonObj != null) miniRobotButtonObj.SetActive(false);
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

        // Nhịp bắn riêng từng nhân vật (0 = giữ nguyên mặc định trên Gun trong Scene), cùng quy ước với
        // baseMaxHP/baseMoveSpeed bên Player. Gán thẳng chứ không cộng dồn nên gọi lại nhiều lần vẫn an toàn.
        if (character.shotDelay > 0f) shotDelay = character.shotDelay;

        // Nhân vật cao/thấp khác nhau nên vị trí cầm vũ khí cũng khác. Luôn tính từ baseLocalPosition (mốc gốc
        // trong Scene) thay vì cộng vào vị trí hiện tại, để không bị dồn offset.
        transform.localPosition = baseLocalPosition + (Vector3)character.weaponOffset;

        // Robot (usesAmmo = false): đổi nút Nạp đạn thành nút Laser, ô text đạn thành bộ đếm charge.
        // SetActive() ở trên đã bật sẵn nút Nạp đạn nên phải ẩn lại tại đây (SetActive luôn chạy trước hàm này).
        usesAmmo = character.usesAmmo;
        currentCharge = 0;
        if (reloadButtonObj != null) reloadButtonObj.SetActive(usesAmmo);
        if (laserButtonObj != null) laserButtonObj.SetActive(!usesAmmo);
        UpdateAmmoText();
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
