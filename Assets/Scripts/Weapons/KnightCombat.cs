using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Toàn bộ cơ chế chiến đấu của nhân vật cận chiến (Knight): tự động chém quanh nhân vật theo chu kỳ,
// tài nguyên Stamina (tự hồi theo thời gian), Khiên (giữ để tăng chống chịu, tốn Stamina theo thời gian),
// và lõi đặc biệt Xoay Kiếm (bất tử tạm thời + damage theo tick quanh nhân vật).
// Đặt cùng GameObject với Player.cs. Được GameManager bật/tắt (SetActive) tùy nhân vật đang chọn có phải Melee hay không.
public class KnightCombat : MonoBehaviour
{
    [Header("Chém tự động")]
    [SerializeField] private float attackRadius = 2f;
    [SerializeField] private float attackInterval = 1f;
    private float attackTimer = 0f;
    [Tooltip("Hiệu ứng animation (sprite riêng) khi chém - spawn 1 lần mỗi đòn, tự dọn qua AutoDestroyOrPool giống Bomb")]
    [SerializeField] private GameObject attackEffectPrefab;
    [Tooltip("Hiệu ứng máu khi trúng đòn (có thể dùng chung prefab máu đang gắn trên PlayerBullet) - để trống nếu không cần")]
    [SerializeField] private GameObject bloodPrefab;

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 50f;
    public float currentStamina;
    [SerializeField] private float staminaRegenPerSecond = 3f;
    [SerializeField] private TextMeshProUGUI staminaText;

    [Header("Khiên (giữ nút để tăng chống chịu)")]
    [SerializeField] private float shieldResistance = 0.5f; // Tăng chống chịu tối đa 50%
    [SerializeField] private float shieldSpeedMultiplier = 1.3f; // Tăng 30% tốc độ chạy trong lúc giữ Khiên
    [SerializeField] private float shieldStaminaCostPerSecond = 10f;
    [Tooltip("GameObject nút Khiên - tự ẩn/hiện theo nhân vật được chọn")]
    [SerializeField] private GameObject shieldButtonObj;
    [Tooltip("Hiệu ứng animation (sprite riêng) bám theo nhân vật suốt lúc giữ Khiên - tự dọn khi thả tay")]
    [SerializeField] private GameObject shieldEffectPrefab;
    private GameObject activeShieldEffect;
    private bool isShielding = false;

    [Header("Xoay Kiếm (lõi đặc biệt)")]
    [SerializeField] private float swordSpinDuration = 5f;
    [SerializeField] private float swordSpinCooldown = 15f;
    [SerializeField] private float swordSpinStaminaCost = 30f;
    [SerializeField] private float swordSpinTickInterval = 0.15f;
    [SerializeField] private float swordSpinRadius = 2.5f;
    private bool hasSwordSpin = false;
    private bool isSwordSpinOnCooldown = false;
    [Tooltip("Gán GameObject nút Xoay Kiếm vào đây - tự ẩn cho tới khi lõi được chọn")]
    [SerializeField] private GameObject swordSpinButtonObj;
    [SerializeField] private Image swordSpinCooldownBar;
    [Tooltip("Hiệu ứng animation (sprite riêng) bám theo nhân vật suốt lúc Xoay Kiếm - tự dọn khi hết 5s")]
    [SerializeField] private GameObject swordSpinEffectPrefab;
    private GameObject activeSwordSpinEffect;

    private void Start()
    {
        currentStamina = maxStamina;
        UpdateStaminaText();
        if (swordSpinButtonObj != null) swordSpinButtonObj.SetActive(false);
    }

    private void Update()
    {
        RegenStamina();
        HandleAutoAttack();

        if (isShielding) DrainShieldStamina();
    }

    // ==============================================
    // CHÉM TỰ ĐỘNG
    // ==============================================
    private void HandleAutoAttack()
    {
        attackTimer += Time.deltaTime;
        if (attackTimer >= attackInterval)
        {
            attackTimer = 0f;
            PerformMeleeAttack();
        }
    }

    private void PerformMeleeAttack()
    {
        if (Player.Instance != null) Player.Instance.PlayAnimTrigger("Attack");
        // Bám theo nhân vật (không đứng yên tại chỗ đánh) - vẫn tự dọn qua AutoDestroyOrPool gắn sẵn trên prefab
        SpawnEffect(attackEffectPrefab, transform.position, true);

        float damage = (Player.Instance != null) ? Player.Instance.bulletDamage : 10f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRadius);

        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;

            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy == null) continue;

            enemy.TakeDmg(damage);
            if (Player.Instance != null) Player.Instance.OnEnemyHit(damage);
            SpawnEffect(bloodPrefab, hit.transform.position, false);
        }
    }

    // Spawn hiệu ứng animation (sprite riêng, giống explosionPrefab của Bomb).
    // attachToPlayer = true: hiệu ứng được gắn làm con của Player nên luôn bám theo nhân vật khi di chuyển.
    //   - Hiệu ứng tồn tại không xác định trước (Khiên, Xoay Kiếm): phải tự gọi RemoveEffect() khi kết thúc.
    //   - Hiệu ứng chỉ chạy 1 lần rồi biến mất (chém tự động): cứ để AutoDestroyOrPool gắn sẵn trên prefab tự dọn, không cần gọi RemoveEffect().
    // attachToPlayer = false: đứng yên tại vị trí spawn, không bám theo nhân vật.
    private GameObject SpawnEffect(GameObject prefab, Vector3 position, bool attachToPlayer)
    {
        if (prefab == null) return null;

        GameObject effect = ObjectPoolManager.Instance != null
            ? ObjectPoolManager.Instance.SpawnObject(prefab, position, Quaternion.identity)
            : Instantiate(prefab, position, Quaternion.identity);

        if (attachToPlayer)
        {
            effect.transform.SetParent(transform);
            effect.transform.localPosition = Vector3.zero;
        }

        return effect;
    }

    private void RemoveEffect(ref GameObject effect)
    {
        if (effect == null) return;

        // ObjectPoolManager.ReturnObjectToPool() tự gỡ parent trước khi trả về Pool
        if (ObjectPoolManager.Instance != null) ObjectPoolManager.Instance.ReturnObjectToPool(effect);
        else
        {
            effect.transform.SetParent(null);
            Destroy(effect);
        }

        effect = null;
    }

    // ==============================================
    // STAMINA
    // ==============================================
    private void RegenStamina()
    {
        if (currentStamina < maxStamina)
        {
            currentStamina = Mathf.Min(currentStamina + staminaRegenPerSecond * Time.deltaTime, maxStamina);
            UpdateStaminaText();
        }
    }

    private void UpdateStaminaText()
    {
        if (staminaText != null) staminaText.text = Mathf.CeilToInt(currentStamina).ToString();
    }

    // ==============================================
    // KHIÊN - giữ nút để tăng chống chịu, tốn Stamina theo thời gian
    // ==============================================
    public void OnShieldPressed()
    {
        if (currentStamina <= 0f) return;

        isShielding = true;
        if (Player.Instance != null)
        {
            Player.Instance.SetDamageResistance(shieldResistance);
            Player.Instance.SetSpeedBoostMultiplier(shieldSpeedMultiplier);
            Player.Instance.SetAnimBool("Shielding", true);
        }
        activeShieldEffect = SpawnEffect(shieldEffectPrefab, transform.position, true);
    }

    public void OnShieldReleased()
    {
        if (!isShielding) return;

        isShielding = false;
        if (Player.Instance != null)
        {
            Player.Instance.SetDamageResistance(0f);
            Player.Instance.SetSpeedBoostMultiplier(1f);
            Player.Instance.SetAnimBool("Shielding", false);
        }
        RemoveEffect(ref activeShieldEffect);
    }

    private void DrainShieldStamina()
    {
        currentStamina -= shieldStaminaCostPerSecond * Time.deltaTime;
        UpdateStaminaText();

        if (currentStamina <= 0f)
        {
            currentStamina = 0f;
            OnShieldReleased(); // Hết Stamina thì tự hạ khiên
        }
    }

    // ==============================================
    // XOAY KIẾM - lõi đặc biệt: bất tử tạm thời + damage theo tick quanh nhân vật
    // ==============================================
    public bool CanUseSwordSpin()
    {
        return hasSwordSpin && !isSwordSpinOnCooldown && currentStamina >= swordSpinStaminaCost;
    }

    public void OnSwordSpinButtonPressed()
    {
        if (!CanUseSwordSpin())
        {
            Debug.Log($"[SwordSpin] Không thể dùng - hasSwordSpin={hasSwordSpin}, isOnCooldown={isSwordSpinOnCooldown}, stamina={currentStamina:F1}/{swordSpinStaminaCost}");
            return;
        }

        currentStamina -= swordSpinStaminaCost;
        UpdateStaminaText();

        StartCoroutine(SwordSpinRoutine());
        StartCoroutine(SwordSpinCooldownRoutine());
    }

    private IEnumerator SwordSpinRoutine()
    {
        if (Player.Instance != null)
        {
            Player.Instance.SetInvulnerable(true);
            Player.Instance.SetAnimBool("SwordSpin", true);
        }
        activeSwordSpinEffect = SpawnEffect(swordSpinEffectPrefab, transform.position, true);

        float elapsed = 0f;
        while (elapsed < swordSpinDuration)
        {
            yield return new WaitForSeconds(swordSpinTickInterval);
            elapsed += swordSpinTickInterval;
            DealSwordSpinTickDamage();
        }

        if (Player.Instance != null)
        {
            Player.Instance.SetInvulnerable(false);
            Player.Instance.SetAnimBool("SwordSpin", false);
        }
        RemoveEffect(ref activeSwordSpinEffect);
    }

    private void DealSwordSpinTickDamage()
    {
        float damage = (Player.Instance != null) ? Player.Instance.bulletDamage * 0.5f : 5f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, swordSpinRadius);

        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;

            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy == null) continue;

            enemy.TakeDmg(damage);
            SpawnEffect(bloodPrefab, hit.transform.position, false);
        }
    }

    private IEnumerator SwordSpinCooldownRoutine()
    {
        isSwordSpinOnCooldown = true;

        float timer = 0f;
        if (swordSpinCooldownBar != null) swordSpinCooldownBar.fillAmount = 1f;

        while (timer < swordSpinCooldown)
        {
            timer += Time.deltaTime;
            if (swordSpinCooldownBar != null) swordSpinCooldownBar.fillAmount = 1f - (timer / swordSpinCooldown);
            yield return null;
        }

        if (swordSpinCooldownBar != null) swordSpinCooldownBar.fillAmount = 0f;
        isSwordSpinOnCooldown = false;
    }

    // ====== PUBLIC METHODS (Gọi từ AugmentManager) ======
    public void EnableSwordSpin()
    {
        hasSwordSpin = true;
        isSwordSpinOnCooldown = false;
        if (swordSpinButtonObj != null) swordSpinButtonObj.SetActive(true);
        Debug.Log("Đã mở khóa Xoay Kiếm!");
    }

    public void ReduceAttackInterval(float amount)
    {
        attackInterval = Mathf.Max(0.2f, attackInterval - amount);
    }

    public float GetAttackInterval() => attackInterval;

    public void IncreaseStaminaRegen(float amount)
    {
        staminaRegenPerSecond += amount;
    }

    public void ReduceShieldCost(float amount)
    {
        shieldStaminaCostPerSecond = Mathf.Max(1f, shieldStaminaCostPerSecond - amount);
    }

    public void ReduceSwordSpinCooldown(float amount)
    {
        swordSpinCooldown = Mathf.Max(3f, swordSpinCooldown - amount);
    }

    // ==============================================
    // CHARACTER SELECT
    // ==============================================
    // Gọi từ GameManager.Start() - bật/tắt toàn bộ cơ chế Knight + nút Khiên tùy nhân vật đang chọn
    public void SetActive(bool active)
    {
        enabled = active;
        if (shieldButtonObj != null) shieldButtonObj.SetActive(active);

        // Nút Xoay Kiếm chỉ hiện khi đã unlock (hasSwordSpin) - nếu tắt Knight thì luôn ẩn bất kể đã unlock hay chưa
        if (!active && swordSpinButtonObj != null) swordSpinButtonObj.SetActive(false);
    }

    public void ApplyCharacterData(CharacterData character)
    {
        // Hiện tại KnightCombat chưa cần đọc thêm gì riêng từ CharacterData ngoài việc được bật (SetActive).
        // Giữ hàm này để nhất quán với Gun.ApplyCharacterData() và dễ mở rộng sau này (VD skin vũ khí Knight).
    }
}
