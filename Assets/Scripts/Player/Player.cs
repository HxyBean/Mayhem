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

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashTime = 0.2f;
    [SerializeField] private float dashCooldown = 1f;


    [Header("UI & References")]
    [SerializeField] private Image hpBar;
    [SerializeField] private Image dashBar;
    [SerializeField] private GameManager gameManager;

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
        rb.linearVelocity = playerInput.normalized * moveSpeed;

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
    // COMBAT & HEALTH
    // ==============================================
    public void TakeDmg(float dmg)
    {
        currentHP -= dmg;
        currentHP = Mathf.Max(currentHP, 0);
        UpdateHPBar();
        
        if (currentHP <= 0)
        {
            Die();
        }
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

    // GETTERS
    public float GetCurrentDamage() => bulletDamage;
    public float GetRegenAmount() => regenAmount;
    public float GetLifeStealPercent() => lifeStealPercent;

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
