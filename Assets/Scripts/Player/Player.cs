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
    [SerializeField] private float maxBlinkRange = 5f;
    [SerializeField] private float blinkCooldown = 3f;
    [Tooltip("Bán kính kiểm tra vật cản tại điểm đến, nên khớp kích thước Collider của Player")]
    [SerializeField] private float blinkCheckRadius = 0.3f;
    [Tooltip("Layer chứa vật cản (VD Rock) - Blink sẽ không bao giờ đưa Player vào bên trong các Layer này")]
    [SerializeField] private LayerMask blinkObstacleMask;
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

    // Private Components
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private TrailRenderer trailRenderer;
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
        
        if (trailRenderer != null) trailRenderer.emitting = false;
    }

    private void Start()
    {
        currentHP = maxHP;
        regenAmount = 0f; // Đảm bảo khởi đầu là 0
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

    // Tìm vị trí gần "target" nhất mà không đè lên vật cản, bằng cách lùi dần về phía "origin"
    private Vector3 FindSafeBlinkPosition(Vector3 origin, Vector3 target)
    {
        if (!Physics2D.OverlapCircle(target, blinkCheckRadius, blinkObstacleMask))
        {
            return target;
        }

        const int steps = 10;
        for (int i = 1; i <= steps; i++)
        {
            Vector3 candidate = Vector3.Lerp(origin, target, 1f - (float)i / steps);
            if (!Physics2D.OverlapCircle(candidate, blinkCheckRadius, blinkObstacleMask))
            {
                return candidate;
            }
        }

        // Không tìm được chỗ trống nào trên đường đi - đứng yên tại chỗ thay vì kẹt vào vật cản
        return origin;
    }

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
    // COMBAT & HEALTH
    // ==============================================
    public void TakeDmg(float dmg)
    {
        if (isInvulnerable) return;

        float actualDmg = dmg * (1f - damageResistance);
        currentHP -= actualDmg;
        currentHP = Mathf.Max(currentHP, 0);
        UpdateHPBar();

        if (currentHP <= 0)
        {
            Die();
        }
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
        }
    }

    public void RestoreFullHP()
    {
        currentHP = maxHP; 
        UpdateHPBar();     
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

    // GETTERS
    public float GetCurrentDamage() => bulletDamage;
    public float GetRegenAmount() => regenAmount;
    public float GetLifeStealPercent() => lifeStealPercent;
    public float GetMagnetRadius() => magnetRadius;


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
