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
    [Tooltip("Tiếng trúng đòn riêng của loại quái này. Để trống = không kêu gì. Luôn phát qua PlaySFXThrottled " +
             "nên không sợ rè khi cả đàn cùng ăn damage lan/DOT trong 1 frame")]
    [SerializeField] protected AudioClip hitSound;

    [Header("Đẩy lùi khi trúng đòn")]
    [Tooltip("Quãng đường bị đẩy lùi (đơn vị world). Để 0 = MIỄN NHIỄM đẩy lùi - dùng cho Boss")]
    [SerializeField] private float knockbackDistance = 0.15f;
    [Tooltip("Đẩy lùi xong trong bấy nhiêu giây. Càng ngắn càng 'nảy', 0.06-0.1 là khoảng gọn gàng")]
    [SerializeField] private float knockbackDuration = 0.08f;

    protected bool isDead = false;

    // Hiệu ứng nháy khi ăn đòn (tùy chọn) - chỉ cần gắn component DamageFlash lên prefab là tự hoạt động
    private DamageFlash damageFlash;

    // Chiêu Lướt (tùy chọn) - gắn component EnemyDashSkill lên prefab là có. Để protected cho subclass đọc
    // (VD Boss cần biết để không tung chiêu khác đè lên lúc đang lướt).
    protected EnemyDashSkill dashSkill;
    protected bool IsDashing => dashSkill != null && dashSkill.IsBusy;

    // Trạng thái đẩy lùi đang diễn ra
    private Vector2 knockbackVelocity;
    private float knockbackTimer = 0f;

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

        damageFlash = GetComponent<DamageFlash>();
        dashSkill = GetComponent<EnemyDashSkill>();

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
        knockbackTimer = 0f; // Pool tái sử dụng: con trước có thể chết ngay giữa lúc đang bị đẩy lùi
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

    // Đẩy lùi xử lý ở LateUpdate CHỨ KHÔNG PHẢI Update - đây là điểm mấu chốt khiến nó chạy cho MỌI loại quái
    // mà không phải sửa subclass nào: RangedEnemy/USBEnemy/BossEnemy đều override Update() và phần lớn KHÔNG
    // gọi base.Update(), nên nhét vào Update() của base là mất tác dụng với đúng những con thú vị nhất.
    // LateUpdate chạy SAU mọi Update, nên nó ghi đè lên bất kỳ kiểu di chuyển nào con đó vừa thực hiện.
    protected virtual void LateUpdate()
    {
        if (knockbackTimer <= 0f) return;

        knockbackTimer -= Time.deltaTime;
        transform.position += (Vector3)(knockbackVelocity * Time.deltaTime);
    }

    // Đẩy lùi theo hướng TỪ nguồn sát thương RA. Gọi kèm trong TakeDmg(dmg, sourcePosition).
    public void ApplyKnockback(Vector2 sourcePosition)
    {
        if (isDead || knockbackDistance <= 0f || knockbackDuration <= 0f) return;

        Vector2 direction = (Vector2)transform.position - sourcePosition;

        // Nguồn nằm trùng khít vị trí quái (VD Mini Robot nổ ngay khi chạm) thì không có hướng nào để đẩy -
        // đẩy đại 1 hướng còn hơn nhân 0 rồi đứng im, vì đứng im trông như đòn đánh không ăn.
        if (direction.sqrMagnitude < 0.0001f) direction = Vector2.right;

        knockbackVelocity = direction.normalized * (knockbackDistance / knockbackDuration);
        knockbackTimer = knockbackDuration;
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
        // Đang vận sức/lướt thì EnemyDashSkill tự lo phần di chuyển. Đặt chốt chặn NGAY TẠI ĐÂY thay vì trong
        // Update() của base: mọi subclass (RangedEnemy/USBEnemy/Boss) đều đi qua MoveToPlayer() dù có override
        // Update() hay không, nên chỉ 1 dòng này là cả 8 loại quái đều xử lý đúng mà không phải sửa gì.
        if (IsDashing) return;

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
    // Sát thương CÓ hướng: gây damage rồi đẩy lùi khỏi nguồn. Dùng cho đòn đánh trúng trực tiếp (đạn, laser,
    // chém, nổ). CỐ Ý tách riêng khỏi TakeDmg(dmg) thường thay vì tự suy ra hướng từ vị trí Player: sát thương
    // theo tick (PotionZone, DOT aura của mã độc) phải gọi bản KHÔNG hướng, vì 1 phút DOT là 120 tick - đẩy
    // lùi mỗi tick sẽ hất con quái ra khỏi bản đồ và biến mọi vùng DOT thành tường chắn.
    public void TakeDmg(float dmg, Vector2 sourcePosition)
    {
        ApplyKnockback(sourcePosition);
        TakeDmg(dmg);
    }

    public virtual void TakeDmg(float dmg)
    {
        if (isDead) return;

        currentHP -= dmg;
        currentHP = Mathf.Max(currentHP, 0);
        UpdateHPBar();

        // Juice: nháy + kêu + hiện số NGAY, trước khi kiểm tra chết. Đặt sau Die() thì đòn kết liễu sẽ im re
        // và không nháy gì cả, vì lúc đó object đã bị trả về Pool.
        if (damageFlash != null) damageFlash.Flash();
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFXThrottled(hitSound);
        if (DamagePopupSpawner.Instance != null) DamagePopupSpawner.Instance.SpawnEnemyDamage(transform.position, dmg);

        if (currentHP <= 0)
        {
            Die();
        }
    }

    // Chọn chỗ đáp NGẪU NHIÊN nhưng phải TRÁNH VẬT CẢN. Rơi vào trong đá là vật phẩm coi như mất trắng:
    // Player không đi tới được, và chỉ nhặt được nếu tình cờ đã có lõi Magnet đủ xa.
    //
    // Thử nhiều lần rồi mới bỏ cuộc, vì 1 lần bốc trúng chỗ kẹt là chuyện bình thường; bỏ cuộc thì rơi ngay
    // dưới chân quái - chỗ đó chắc chắn đi tới được vì con quái vừa đứng ở đấy.
    protected Vector3 GetRandomDropPosition(float radius = 2f)
    {
        LayerMask obstacleMask = GetDropObstacleMask();

        const int maxAttempts = 10;
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * radius;
            Vector3 candidate = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0f);

            // Chưa cấu hình Layer vật cản thì giữ nguyên hành vi cũ thay vì chặn hết mọi thứ
            if (obstacleMask == 0) return candidate;

            if (!Physics2D.OverlapCircle(candidate, GetDropClearance(), obstacleMask)) return candidate;
        }

        return transform.position;
    }

    // Ưu tiên cấu hình riêng trên GameManager; chưa set thì mượn lại Blink Obstacle Mask của Player để không
    // phải khai cùng một danh sách Layer ở 2 chỗ trong mỗi Scene.
    private LayerMask GetDropObstacleMask()
    {
        if (GameManager.Instance != null && GameManager.Instance.ItemDropObstacleMask != 0)
        {
            return GameManager.Instance.ItemDropObstacleMask;
        }

        return (Player.Instance != null) ? Player.Instance.GetObstacleMask() : (LayerMask)0;
    }

    private float GetDropClearance()
    {
        return (GameManager.Instance != null) ? GameManager.Instance.ItemDropClearance : 0.3f;
    }

    protected GameObject SpawnItem(GameObject prefab)
    {
        if (prefab == null) return null;

        Vector3 dropPos = GetRandomDropPosition(2f);

        // Spawn NGAY TẠI XÁC QUÁI rồi mới bay tới chỗ đáp - đó là cái làm nên hiệu ứng "văng ra". Prefab không
        // có ItemDropMotion thì đặt thẳng vào chỗ đáp như cũ.
        GameObject obj;
        if (ObjectPoolManager.Instance != null)
        {
            obj = ObjectPoolManager.Instance.SpawnObject(prefab, transform.position, Quaternion.identity);
        }
        else
        {
            obj = Instantiate(prefab, transform.position, Quaternion.identity);
            Destroy(obj, 5f);
        }

        ItemDropMotion motion = obj.GetComponent<ItemDropMotion>();
        if (motion != null) motion.Launch(dropPos);
        else obj.transform.position = dropPos;

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

        // Đếm mạng để tới mốc thì spawn USBEnemy. Đặt ở Die() của base nên Boss (override Die() không gọi base)
        // không tính vào - đúng ý đồ: mốc này thưởng cho việc dọn quái thường.
        if (EnemySpawner.Instance != null) EnemySpawner.Instance.OnEnemyKilled();
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
