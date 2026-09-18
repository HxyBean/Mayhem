using UnityEngine;
using UnityEngine.UI;

public abstract class Enemy : MonoBehaviour
{
    [SerializeField] protected float enemyMoveSpeed = 1f;
    protected Player player;
    [SerializeField] protected float maxHP = 50f;
    [SerializeField] protected float enterDmg = 10f;
    [SerializeField] protected float stayDmg = 1f;
    [Tooltip("Khoảng cách giữa 2 lần gây stayDmg khi Player đứng chạm (giây). BẮT BUỘC có field này vì OnTriggerStay2D chạy theo nhịp vật lý (~50 lần/giây), không giới hạn lại thì stayDmg sẽ bị áp ~50 lần/giây thay vì đúng nghĩa 'mỗi giây'.")]
    [SerializeField] protected float stayDmgInterval = 1f;
    private float stayDmgTimer = 0f;
    protected float currentHP;
    [SerializeField] private Image hpBar;
    [SerializeField] protected GameObject xpObject;
    [SerializeField] protected GameObject bigXpObject;
    [Tooltip("Prefab Coin rơi ra khi chết (mỗi con rơi đúng 1 coin). Để trống nếu muốn loại quái này KHÔNG rơi coin.")]
    [SerializeField] protected GameObject coinObject;
    protected bool isDead = false;

    // Hiệu ứng làm chậm (VD PotionZone). speedMultiplier = 1 nghĩa là tốc độ bình thường.
    private float speedMultiplier = 1f;
    private int slowStackCount = 0;

    // Giá trị GỐC lấy từ Inspector lúc khởi tạo lần đầu (chưa nhân hệ số độ khó theo Stage).
    // Lưu 1 lần trong Awake() để không bị nhân dồn mỗi khi Enemy được tái sử dụng từ Pool.
    // protected để BossEnemy có thể tự điều chỉnh baseMaxHP khi tính máu hồi sinh.
    protected float baseMoveSpeed;
    protected float baseMaxHP;
    protected float baseEnterDmg;
    protected float baseStayDmg;
    private bool statsCaptured = false;

    protected virtual void Awake()
    {
        // Rigidbody2D tự "ngủ" khi đứng yên đủ lâu (VD Enemy đã đuổi kịp Player rồi dừng lại), lúc đó
        // OnTriggerStay2D ngừng bắn dù vẫn đang chạm nhau. Ép không bao giờ ngủ để stayDmg luôn hoạt động đúng.
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

        if (statsCaptured) return;

        baseMoveSpeed = enemyMoveSpeed;
        baseMaxHP = maxHP;
        baseEnterDmg = enterDmg;
        baseStayDmg = stayDmg;
        statsCaptured = true;
    }

    protected virtual void OnEnable()
    {
        ApplyStageDifficulty();

        player = Player.Instance;
        currentHP = maxHP;
        isDead = false;
        speedMultiplier = 1f;
        slowStackCount = 0;
        stayDmgTimer = 0f;
        UpdateHPBar();
    }

    // Scale máu/damage/tốc độ theo hệ số độ khó của Stage đang chơi (dùng chung 1 prefab cho mọi Level).
    private void ApplyStageDifficulty()
    {
        // Ưu tiên Stage hiệu lực của GameManager (đã tự xử lý fallback debugStage khi Play thẳng Scene trong Editor)
        StageData stage = (GameManager.Instance != null) ? GameManager.Instance.CurrentStage : GameProgress.SelectedStage;
        float hpMul = (stage != null) ? stage.enemyHpMultiplier : 1f;
        float dmgMul = (stage != null) ? stage.enemyDamageMultiplier : 1f;
        float speedMul = (stage != null) ? stage.enemySpeedMultiplier : 1f;

        maxHP = baseMaxHP * hpMul;
        enterDmg = baseEnterDmg * dmgMul;
        stayDmg = baseStayDmg * dmgMul;
        enemyMoveSpeed = baseMoveSpeed * speedMul;
    }

    protected virtual void Update()
    {
        MoveToPlayer();
    }

    // ==============================================
    // VA CHẠM VỚI PLAYER (dùng chung cho mọi loại Enemy)
    // ==============================================
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            OnPlayerEnter(collision);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            OnPlayerStay(collision);
        }
    }

    protected virtual void OnPlayerEnter(Collider2D collision)
    {
        player.TakeDmg(enterDmg);
    }

    protected virtual void OnPlayerStay(Collider2D collision)
    {
        // OnTriggerStay2D (nơi gọi hàm này) chạy theo nhịp vật lý (~50 lần/giây) - phải tự giới hạn
        // lại đúng khoảng stayDmgInterval, không thì stayDmg sẽ bị cộng dồn ~50 lần mỗi giây.
        stayDmgTimer += Time.deltaTime;
        if (stayDmgTimer < stayDmgInterval) return;

        stayDmgTimer = 0f;
        player.TakeDmg(stayDmg);
    }
    protected void MoveToPlayer()
    {
        if (player != null)
        {
            float actualSpeed = enemyMoveSpeed * speedMultiplier;
            transform.position = Vector2.MoveTowards(transform.position, player.transform.position, actualSpeed * Time.deltaTime);
            FlipEnemy();
        }
    }

    // Gọi khi Enemy bước vào vùng làm chậm (VD PotionZone). Dùng đếm stack để an toàn khi đứng chồng nhiều vùng cùng lúc.
    public void ApplySlow(float slowPercent)
    {
        slowStackCount++;
        speedMultiplier = Mathf.Max(0.1f, 1f - Mathf.Clamp01(slowPercent));
    }

    // Gọi khi Enemy rời khỏi vùng làm chậm. Chỉ khi hết TẤT CẢ vùng đang chồng lên mới trả lại tốc độ bình thường.
    public void RemoveSlow()
    {
        slowStackCount = Mathf.Max(0, slowStackCount - 1);
        if (slowStackCount == 0)
        {
            speedMultiplier = 1f;
        }
    }

    protected void FlipEnemy()
    {
        if (player != null)
        {
            transform.localScale = new Vector3(player.transform.position.x < transform.position.x ? -1 : 1, 1, 1);
        }
    }
    public virtual void TakeDmg(float dmg)
    {
        if (isDead) return;

        currentHP -= dmg;
        currentHP = Mathf.Max(currentHP, 0);
        UpdateHPBar();
        if (currentHP <= 0)
        {
            Die();
        }
    }

    protected Vector3 GetRandomDropPosition(float radius = 2f)
    {
        Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * radius;
        return transform.position + new Vector3(randomOffset.x, randomOffset.y, 0f);
    }

    protected GameObject SpawnItem(GameObject prefab)
    {
        if (prefab == null) return null;
        Vector3 dropPos = GetRandomDropPosition(2f);
        GameObject obj;
        if (ObjectPoolManager.Instance != null)
        {
            obj = ObjectPoolManager.Instance.SpawnObject(prefab, dropPos, Quaternion.identity);
        }
        else
        {
            obj = Instantiate(prefab, dropPos, Quaternion.identity);
            Destroy(obj, 5f);
        }
        return obj;
    }

    protected virtual void DropItems()
    {
        if (xpObject != null)
        {
            int dropCount = UnityEngine.Random.Range(1, 4); // Basic: 1-3
            for (int i = 0; i < dropCount; i++) SpawnItem(xpObject);
        }
    }

    // Mọi Enemy thường đều rơi đúng 1 coin. Đặt ở Die() chứ KHÔNG gộp vào DropItems() vì các subclass override
    // trọn vẹn DropItems() (MiniEnemy/EnergyEnemy/ExplosionEnemy...) sẽ làm mất coin nếu quên gọi base.
    // BossEnemy override luôn cả Die() nên tự động không rơi coin - Boss rơi kim cương theo cơ chế riêng.
    protected void DropCoin()
    {
        if (coinObject != null) SpawnItem(coinObject);
    }

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        DropItems();
        DropCoin();
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnObjectToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    protected void UpdateHPBar()
    {
        if(hpBar != null)
        {
            hpBar.fillAmount = currentHP / maxHP;
        }
    }
}
