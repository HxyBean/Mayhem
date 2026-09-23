using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }

    [Header("Stats")]
    public float maxHP = 100f;
    public float currentHP;
    public float regenAmount = 0f;
    public float lifeStealPercent = 0f;
    public float bulletDamage = 10f;

    // Chống chịu/bất tử tạm thời - dùng cho Khiên và Xoay Kiếm của Knight, nhưng áp dụng chung cho MỌI nguồn damage
    private float damageResistance = 0f; // 0-1 (0.5 = giảm 50% damage nhận vào)
    private bool isInvulnerable = false; // true = miễn nhiễm hoàn toàn mọi damage

    // Bán kính hút vật phẩm (lõi Magnet, chung cho mọi nhân vật) - 0 = chưa có, tối đa 4
    private float magnetRadius = 0f;

    // % sát thương lan CỘNG THÊM qua augment (Pháp sư) - lưu ở đây (không phải trên PlayerBullet) vì đạn
    // là object tái sử dụng qua Pool, mutate trực tiếp lên 1 instance đạn sẽ không áp dụng cho các viên khác.
    private float splashDamageBonus = 0f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    private float baseMoveSpeedSnapshot; // Mốc tốc độ gốc của nhân vật (set trong ApplyCharacterData), dùng để tính % Speed đã tăng thêm qua augment
    // Hệ số tốc độ TẠM THỜI (VD tăng khi giữ Khiên) - khác với moveSpeed (vĩnh viễn, tăng qua augment).
    // Nhân thêm lúc di chuyển, không ghi đè vào moveSpeed nên không ảnh hưởng tới GetSpeedBonusPercent().
    private float speedBoostMultiplier = 1f;
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashTime = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    [Header("Blink (Pháp sư)")]
    [SerializeField] private float maxBlinkRange = 6f;
    [SerializeField] private float blinkCooldown = 3f;
    [Tooltip("Bán kính kiểm tra vật cản tại điểm đến, nên khớp kích thước Collider của Player")]
    [SerializeField] private float blinkCheckRadius = 0.3f;
    [Tooltip("Layer chứa vật cản (VD Rock) - Blink sẽ không bao giờ đưa Player vào bên trong các Layer này")]
    [SerializeField] private LayerMask blinkObstacleMask;
    [Tooltip("Lùi lại thêm bấy nhiêu khi dừng trước vật cản, để nhân vật không dính sát mép collider. " +
             "0.05-0.15 là đủ")]
    [SerializeField] private float blinkStopMargin = 0.08f;
    [Tooltip("Overlay cooldown Blink, gán Image nằm trên chính nút BlinkButton (khác với Dash Bar vì 2 nút không hiện cùng lúc)")]
    [SerializeField] private Image blinkCooldownBar;
    [Tooltip("Hiệu ứng animation tại vị trí biến mất lúc bắt đầu Blink (để trống nếu không cần)")]
    [SerializeField] private GameObject blinkStartEffectPrefab;
    [Tooltip("Hiệu ứng animation tại vị trí xuất hiện sau khi Blink xong (để trống nếu không cần)")]
    [SerializeField] private GameObject blinkEndEffectPrefab;

    [Header("UI & References")]
    [SerializeField] private Image hpBar;
    [SerializeField] private Image dashBar;
    [SerializeField] private GameManager gameManager;
    [Tooltip("Nút Dash (Gunner) - sẽ tự ẩn/hiện theo nhân vật được chọn")]
    [SerializeField] private GameObject dashButtonObj;
    [Tooltip("Nút Blink kéo-thả (Pháp sư) - sẽ tự ẩn/hiện theo nhân vật được chọn")]
    [SerializeField] private GameObject blinkButtonObj;

    [Header("Phản hồi khi ăn đòn (game juice)")]
    [Tooltip("Tiếng nhân vật ăn đòn. Phát qua PlaySFXThrottled nên không bị rè khi dính nhiều nguồn damage " +
             "cùng lúc. Để trống = không kêu gì")]
    [SerializeField] private AudioClip hurtSound;
    [Tooltip("Hiệu ứng animation khi được hồi máu (hút máu, nhặt Heart, nhặt USB). Được gắn làm CON của Player " +
             "nên bám theo nhân vật. Để trống = không có hiệu ứng")]
    [SerializeField] private GameObject healEffectPrefab;
    [Tooltip("Khoảng cách tối thiểu giữa 2 lần hiện hiệu ứng hồi máu (giây). BẮT BUỘC > 0: hút máu kích hoạt " +
             "MỖI viên đạn trúng quái, không chặn lại thì hiệu ứng spawn hàng chục lần mỗi giây")]
    [SerializeField] private float healEffectMinInterval = 0.4f;
    [SerializeField] private AudioClip healSound;

    private float nextHealEffectTime = 0f;

    // Private Components
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private TrailRenderer trailRenderer;
    // Hiệu ứng nháy khi ăn đòn (tùy chọn) - chỉ cần gắn component DamageFlash lên GameObject Player
    private DamageFlash damageFlash;
    public Joystick joystick;

    // State Variables
    private bool isDashing = false;
    private bool canDash = true;
    private bool isRegenActive = false;
    private bool canBlink = true;
    private AbilityType abilityType = AbilityType.Dash;

    // ==============================================
    // UNITY CALLBACKS
    // ==============================================
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        trailRenderer = GetComponent<TrailRenderer>();
        damageFlash = GetComponent<DamageFlash>();

        // Rigidbody2D tự "ngủ" khi đứng yên đủ lâu, lúc đó OnTriggerStay2D (VD stayDmg của Enemy) ngừng bắn
        // dù vẫn đang chạm nhau. Ép không bao giờ ngủ để Player luôn nhận đủ sát thương stay kể cả đứng yên.
        if (rb != null) rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

        if (trailRenderer != null) trailRenderer.emitting = false;

        // Đảm bảo khởi đầu là 0 (phòng khi Inspector lỡ để giá trị khác). PHẢI đặt ở Awake chứ không phải
        // Start: GameManager.Start() có thể chạy TRƯỚC Start() của Player (Unity không đảm bảo thứ tự giữa 2
        // MonoBehaviour khác nhau), nếu reset ở Start thì lượng hồi máu mua từ Shop vừa áp xong sẽ bị xóa mất.
        regenAmount = 0f;
    }

    private void Start()
    {
        currentHP = maxHP;
        UpdateHPBar();
    }

    private void Update()
    {
        if (isDashing) return;
        
        MovePlayer();
        
        // Nút lướt bằng phím Space để test trên máy tính
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            OnDashButtonPressed();
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (gameManager != null) gameManager.PauseMenu();
        } 
    }



    // ==============================================
    // MOVEMENT & ACTIONS
    // ==============================================
    private void MovePlayer()
    {
        Vector2 playerInput = Vector2.zero;
        if (joystick != null)
        {
            playerInput = new Vector2(joystick.Horizontal, joystick.Vertical);
        }

        // Bổ sung WASD để test trên máy tính (chỉ lấy khi không chạm vào joystick)
        if (playerInput.sqrMagnitude < 0.001f)
        {
            float h = 0f;
            float v = 0f;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h = -1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h = 1f;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v = 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v = -1f;
            }
            playerInput = new Vector2(h, v);
        }

        // Dùng normalized để đảm bảo tốc độ di chuyển chéo không bị nhanh hơn
        rb.linearVelocity = playerInput.normalized * moveSpeed * speedBoostMultiplier;

        if (playerInput.x < 0)
        {
            spriteRenderer.flipX = true;
        }
        else if (playerInput.x > 0)
        {
            spriteRenderer.flipX = false;
        }

        if (animator != null)
        {
            animator.SetBool("isRun", playerInput != Vector2.zero);
        }
    }

    public void OnDashButtonPressed()
    {
        if (canDash && !isDashing)
        {
            AudioManager.Instance.PlayGunnerDashSound();
            StartCoroutine(Dash());
            
        }

    }

    private IEnumerator Dash()
    {
        isDashing = true;
        canDash = false;

        Vector2 dashInput = Vector2.zero;
        if (joystick != null)
        {
            dashInput = new Vector2(joystick.Horizontal, joystick.Vertical);
        }
        
        // Hướng Dash bằng bàn phím nếu test trên PC
        if (dashInput.sqrMagnitude < 0.001f)
        {
            float h = 0f;
            float v = 0f;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h = -1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h = 1f;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v = 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v = -1f;
            }
            dashInput = new Vector2(h, v);
        }

        if (dashInput == Vector2.zero) dashInput = spriteRenderer.flipX ? Vector2.left : Vector2.right;

        rb.linearVelocity = dashInput.normalized * dashSpeed;
        if (trailRenderer != null) trailRenderer.emitting = true;

        yield return new WaitForSeconds(dashTime);

        if (trailRenderer != null) trailRenderer.emitting = false;
        isDashing = false;

        // BẮT ĐẦU COOLDOWN
        float timer = 0;
        if (dashBar != null) dashBar.fillAmount = 1f;

        while (timer < dashCooldown)
        {
            timer += Time.deltaTime;
            UpdateDashBar(timer, dashCooldown);
            yield return null;
        }

        // HỒI CHIÊU XONG
        if (dashBar != null) dashBar.fillAmount = 0f;
        canDash = true;
    }

    // ==============================================
    // BLINK (Pháp sư) — dịch chuyển tức thời, vị trí đến từ BlinkButton kéo-thả
    // ==============================================
    public bool CanBlink()
    {
        return canBlink;
    }

    public void Blink(Vector3 targetPosition)
    {
        if (!canBlink) return;
        StartCoroutine(BlinkRoutine(targetPosition));
    }

    private IEnumerator BlinkRoutine(Vector3 targetPosition)
    {
        canBlink = false;

        Vector3 startPosition = transform.position;

        // Phòng hờ UI gửi vị trí ngoài tầm cho phép: luôn giới hạn lại đúng bán kính tối đa
        Vector3 offset = Vector3.ClampMagnitude(targetPosition - startPosition, maxBlinkRange);
        Vector3 rawTarget = startPosition + offset;

        // Không bao giờ đưa Player vào bên trong vật cản (VD Rock) - lùi dần về phía vị trí gốc tới khi tìm được chỗ trống
        Vector3 safeTarget = FindSafeBlinkPosition(startPosition, rawTarget);

        PlayAnimTrigger("Blink");
        SpawnBlinkEffect(blinkStartEffectPrefab, startPosition);
        transform.position = safeTarget;
        SpawnBlinkEffect(blinkEndEffectPrefab, safeTarget);

        float timer = 0f;
        if (blinkCooldownBar != null) blinkCooldownBar.fillAmount = 1f;

        while (timer < blinkCooldown)
        {
            timer += Time.deltaTime;
            if (blinkCooldownBar != null) blinkCooldownBar.fillAmount = 1f - (timer / blinkCooldown);
            yield return null;
        }

        if (blinkCooldownBar != null) blinkCooldownBar.fillAmount = 0f;
        canBlink = true;
    }

    private void SpawnBlinkEffect(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return;

        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.SpawnObject(prefab, position, Quaternion.identity);
        }
        else
        {
            Instantiate(prefab, position, Quaternion.identity);
        }
    }

    // Tìm vị trí xa nhất trên đường Blink mà không đè vào vật cản.
    //
    // QUÉT CẢ ĐƯỜNG ĐI (CircleCast) chứ KHÔNG chỉ kiểm tra điểm đích. Cách cũ chỉ OverlapCircle tại đích rồi
    // lùi dần, nên blinkCheckRadius phải gánh 2 việc mâu thuẫn nhau: vừa là "bề ngang nhân vật" (phải NHỎ để
    // lách được khe giữa các chướng ngại vật), vừa là "độ dày tường chặn" (phải LỚN để không nhảy xuyên tường).
    // Chỉnh to thì kẹt ở mọi khe hẹp, chỉnh nhỏ thì lọt hẳn vào trong đá - không có giá trị nào đúng cả.
    //
    // Quét đường đi thì 2 việc tách hẳn ra: blinkCheckRadius chỉ còn là bề ngang nhân vật (để nhỏ, khớp
    // Collider của Player), còn việc chặn xuyên tường do chính phép quét lo - vật cản DÀY hay MỎNG đều chặn
    // như nhau vì tia quét đụng vào là dừng.
    private Vector3 FindSafeBlinkPosition(Vector3 origin, Vector3 target)
    {
        Vector2 delta = target - origin;
        float distance = delta.magnitude;
        if (distance < 0.001f) return origin;

        Vector2 direction = delta / distance;

        RaycastHit2D hit = Physics2D.CircleCast(origin, blinkCheckRadius, direction, distance, blinkObstacleMask);

        // Đường thông suốt -> tới thẳng đích
        if (hit.collider == null) return target;

        // Có vật cản -> dừng NGAY TRƯỚC nó thay vì hủy chiêu. Blink được bao xa hay bấy nhiêu vẫn hữu ích hơn
        // là đứng yên tại chỗ mà vẫn mất lượt hồi chiêu.
        // hit.distance là quãng đường TÂM đường tròn đi được tới lúc chạm, nên trừ thêm margin cho khỏi dính mép.
        float safeDistance = hit.distance - blinkStopMargin;
        if (safeDistance <= 0f) return origin; // Đã đứng sát/trong vật cản ngay từ đầu

        return origin + (Vector3)(direction * safeDistance);
    }

    // Dùng lại làm mặc định cho việc chọn chỗ đáp của vật phẩm rơi ra (Enemy.GetRandomDropPosition), để không
    // phải khai báo LẠI cùng một danh sách Layer vật cản ở chỗ thứ hai trong mỗi Scene.
    public LayerMask GetObstacleMask() => blinkObstacleMask;

    public void ReduceBlinkCooldown(float amount)
    {
        blinkCooldown = Mathf.Max(0.5f, blinkCooldown*amount);
    }

    // ==============================================
    // CHARACTER SELECT
    // ==============================================
    // Gọi từ GameManager.Start() theo nhân vật đã chọn ở Character Select
    public void ApplyCharacterData(CharacterData character)
    {
        if (character == null) return;

        Debug.Log($"[ApplyCharacterData] Nhân vật: {character.characterName} - AbilityType: {character.abilityType}");

        bulletDamage = character.baseBulletDamage;
        abilityType = character.abilityType;

        // Override máu/tốc độ riêng nếu nhân vật này có set (0 = giữ nguyên mặc định trên Player, không phá vỡ Gunner/Mage đã cấu hình sẵn)
        if (character.baseMaxHP > 0f)
        {
            maxHP = character.baseMaxHP;
            RestoreFullHP();
        }
        if (character.baseMoveSpeed > 0f)
        {
            moveSpeed = character.baseMoveSpeed;
        }
        // Lưu lại mốc tốc độ GỐC của nhân vật này (sau khi đã áp override) để tính % Speed tăng thêm qua augment
        baseMoveSpeedSnapshot = moveSpeed;

        // Hồi máu riêng của nhân vật. StartHealthRegen CỘNG DỒN nên lượng hồi mua ở Shop (áp sau hàm này) vẫn
        // được cộng thêm chứ không ghi đè. Bỏ qua khi = 0 để không chạy coroutine vô ích.
        if (character.baseRegen > 0f) StartHealthRegen(character.baseRegen);

        bool isDash = abilityType == AbilityType.Dash;
        bool isBlink = abilityType == AbilityType.Blink;
        if (dashButtonObj != null) dashButtonObj.SetActive(isDash);
        if (blinkButtonObj != null) blinkButtonObj.SetActive(isBlink);

        // Đổi tạo hình/animation theo nhân vật được chọn
        if (character.animatorController != null && animator != null)
        {
            animator.runtimeAnimatorController = character.animatorController;
        }
        if (character.idleSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = character.idleSprite;
        }
    }

    // ==============================================
    // SHOP POWER UP (chỉ số nội tại mua bằng Coin, áp mỗi lần vào màn)
    // ==============================================
    // PHẢI gọi SAU ApplyCharacterData() vì hàm đó ghi đè maxHP/bulletDamage/moveSpeed bằng chỉ số gốc của
    // nhân vật - gọi trước thì toàn bộ bonus mua ở Shop sẽ bị xóa sạch.
    public void ApplyShopUpgrades()
    {
        maxHP += ShopUpgrades.GetTotalBonus(ShopStatType.MaxHP);
        bulletDamage += ShopUpgrades.GetTotalBonus(ShopStatType.Damage);
        moveSpeed += ShopUpgrades.GetTotalBonus(ShopStatType.MoveSpeed);
        lifeStealPercent += ShopUpgrades.GetTotalBonus(ShopStatType.LifeSteal);

        // Chốt LẠI mốc tốc độ gốc sau khi đã cộng bonus Shop. Nếu vẫn dùng mốc cũ thì GetSpeedBonusPercent()
        // sẽ tưởng người chơi đã tự tăng sẵn x% và cắt trần augment Speed sớm hơn thực tế.
        baseMoveSpeedSnapshot = moveSpeed;

        RestoreFullHP();

        float regenPerSecond = ShopUpgrades.GetTotalBonus(ShopStatType.Regen);
        if (regenPerSecond > 0f) StartHealthRegen(regenPerSecond);
    }

    // ==============================================
    // COMBAT & HEALTH
    // ==============================================
    public void TakeDmg(float dmg)
    {
        if (isInvulnerable) return;

        float actualDmg = dmg * (1f - damageResistance);
        currentHP -= actualDmg;
        currentHP = Mathf.Max(currentHP, 0);
        UpdateHPBar();

        // Juice: nháy + kêu NGAY, trước khi kiểm tra chết - đặt sau Die() thì đòn chí mạng sẽ im re, mà đó
        // lại đúng là lúc cần phản hồi rõ nhất. Đặt sau isInvulnerable ở trên nên lúc Xoay Kiếm bất tử sẽ
        // không nháy, đúng ý đồ: không nháy = không mất máu.
        if (damageFlash != null) damageFlash.Flash();
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFXThrottled(hurtSound);
        if (DamagePopupSpawner.Instance != null) DamagePopupSpawner.Instance.SpawnPlayerDamage(transform.position, actualDmg);

        if (currentHP <= 0)
        {
            Die();
        }
    }

    // ==============================================
    // HIỆU ỨNG HỒI MÁU
    // ==============================================
    // Gọi từ MỌI nguồn hồi máu (hút máu, Heart, USB) nên phải tự chặn spam: hút máu kích hoạt mỗi viên đạn
    // trúng quái, mà Mage bắn 1 phát nổ lan trúng cả đàn thì có thể gọi hàng chục lần trong 1 frame.
    private void PlayHealFeedback()
    {
        if (Time.time < nextHealEffectTime) return;
        nextHealEffectTime = Time.time + Mathf.Max(0f, healEffectMinInterval);

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFXThrottled(healSound);

        if (healEffectPrefab == null) return;

        GameObject effect = ObjectPoolManager.Instance != null
            ? ObjectPoolManager.Instance.SpawnObject(healEffectPrefab, transform.position, Quaternion.identity)
            : Instantiate(healEffectPrefab, transform.position, Quaternion.identity);

        // Gắn làm con của Player để bám theo lúc chạy, giống hiệu ứng chém của Knight. An toàn vì Player
        // không bao giờ vào Pool (xem quy tắc ở mục 9 của CLAUDE.md).
        effect.transform.SetParent(transform);
        effect.transform.localPosition = Vector3.zero;
    }

    // Gọi từ KnightCombat khi giữ Khiên (resistance tạm thời) - resistance = 0 khi thả tay
    public void SetDamageResistance(float resistance)
    {
        damageResistance = Mathf.Clamp01(resistance);
        
    }

    // Gọi từ KnightCombat lúc bắt đầu/kết thúc Xoay Kiếm (bất tử tạm thời)
    public void SetInvulnerable(bool invulnerable)
    {
        isInvulnerable = invulnerable;
    }

    // Gọi từ KnightCombat khi giữ/thả Khiên - tăng tốc tạm thời trong lúc dùng, trả về 1f khi thả tay
    public void SetSpeedBoostMultiplier(float multiplier)
    {
        speedBoostMultiplier = multiplier;
    }

    public void Heal(float healValue)
    {
        if(currentHP < maxHP)
        {
            currentHP += healValue;
            currentHP = Mathf.Min(currentHP, maxHP);
            UpdateHPBar();

            // Nằm TRONG khối if nên máu đã đầy thì không hiện gì - đúng ý đồ: không hồi được thì đừng báo là có
            PlayHealFeedback();
        }
    }

    public void RestoreFullHP()
    {
        // ApplyCharacterData/ApplyShopUpgrades cũng gọi hàm này lúc khởi tạo màn chơi, khi đó currentHP còn = 0
        // và chưa có gì để "hồi" cả - chỉ báo hiệu ứng khi thực sự đang thiếu máu giữa ván.
        bool wasInjured = currentHP > 0f && currentHP < maxHP;

        currentHP = maxHP;
        UpdateHPBar();

        if (wasInjured) PlayHealFeedback();
    }

    private void Die()
    {
        if (gameManager != null) gameManager.GameOverMenu();
    }

    public void StartHealthRegen(float amount)
    {
        if (!isRegenActive)
        {
            regenAmount = amount;
            StartCoroutine(RegenHealthCoroutine());
        }
        else
        {
            regenAmount += amount; // Cộng dồn lượng hồi máu
        }
    }

    private IEnumerator RegenHealthCoroutine()
    {
        isRegenActive = true;
        while (true)
        {
            yield return new WaitForSeconds(1f);

            if (currentHP < maxHP)
            {
                currentHP += regenAmount;
                currentHP = Mathf.Min(currentHP, maxHP);
                UpdateHPBar();
            }
        }
    }

    public void OnEnemyHit(float damageDealt)
    {
        if (lifeStealPercent > 0)
        {
            float healAmount = damageDealt * lifeStealPercent;
            Heal(healAmount);
        }
    }

    // ==============================================
    // STATS MODIFIERS (AUGMENTS)
    // ==============================================
    public void ApplyMoveSpeedBoost(float multiplier)
    {
        moveSpeed *= multiplier;
    }

    public void ApplyMaxHPBoost(int bonus)
    {
        maxHP += bonus;
        RestoreFullHP();
    }

    public void IncreaseDamage(float amount)
    {
        bulletDamage += amount;
        Debug.Log("Sát thương hiện tại: " + bulletDamage);
    }

    public void IncreaseDamagePercent(float amount)
    {
        bulletDamage *= amount;
        Debug.Log("Sát thương hiện tại: " + bulletDamage +"%");
    }

    public void AddLifeSteal(float amount)
    {
        lifeStealPercent += amount;
        Debug.Log("Tỉ lệ hút máu hiện tại: " + (lifeStealPercent * 100) + "%");
    }

    // Lõi Magnet: tăng dần bán kính hút vật phẩm, tối đa 4
    public void IncreaseMagnetRadius(float amount)
    {
        magnetRadius = Mathf.Min(magnetRadius + amount, 4f);
        Debug.Log("Bán kính hút vật phẩm hiện tại: " + magnetRadius);
    }

    // Lõi riêng của Pháp sư: tăng % sát thương lan CỘNG THÊM vào splashDamagePercent gốc của đạn
    public void IncreaseSplashDamagePercent(float amount)
    {
        splashDamageBonus += amount;
        Debug.Log("% sát thương lan cộng thêm hiện tại: " + (splashDamageBonus * 100) + "%");
    }

    // GETTERS
    public float GetCurrentDamage() => bulletDamage;
    public float GetRegenAmount() => regenAmount;
    public float GetLifeStealPercent() => lifeStealPercent;
    public float GetMagnetRadius() => magnetRadius;
    public float GetSplashDamageBonus() => splashDamageBonus;


    // % tốc độ đã tăng thêm so với mốc gốc của nhân vật (1f = +100%). Dùng để giới hạn trần augment Speed.
    public float GetSpeedBonusPercent()
    {
        if (baseMoveSpeedSnapshot <= 0f) return 0f;
        return (moveSpeed - baseMoveSpeedSnapshot) / baseMoveSpeedSnapshot;
    }

    // ==============================================
    // ANIMATION HOOKS
    // Gun.cs/KnightCombat.cs gọi qua Player.Instance để kích hoạt animation mà không cần
    // tự giữ tham chiếu Animator riêng - Player là nơi duy nhất sở hữu Animator.
    // ==============================================
    public void PlayAnimTrigger(string triggerName)
    {
        if (animator != null) animator.SetTrigger(triggerName);
    }

    public void SetAnimBool(string boolName, bool value)
    {
        if (animator != null) animator.SetBool(boolName, value);
    }

    // ==============================================
    // UI UPDATES
    // ==============================================
    private void UpdateHPBar()
    {
        if (hpBar != null)
        {
            hpBar.fillAmount = currentHP / maxHP;
        }
    }

    private void UpdateDashBar(float currentTime, float maxCooldown)
    {
        if (dashBar != null)
        {
            dashBar.fillAmount = 1f - (currentTime / maxCooldown);
        }
    }
}
