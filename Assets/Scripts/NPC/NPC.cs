using UnityEngine;
using UnityEngine.UI;

public enum NPCQuestState
{
    Idle,
    QuestAccepted,
    QuestReadyToComplete,
    Companion
}

// interactionRadius / interactionButtonObj + phần dò khoảng cách và nối nút nằm ở InteractableNPC (base).
public class NPC : InteractableNPC
{
    public static NPC Instance { get; private set; }

    [Header("References")]
    [SerializeField] private NPCDialogueUI dialogueUI;
    [SerializeField] private QuestNotificationUI notificationUI;
    
    [Header("Quest Data")]
    [SerializeField] private string npcName = "Mysterious Ally";
    [SerializeField] private int usbRequired = 3;
    [TextArea(2, 5)]
    [SerializeField] private string[] questDialoguePages = new string[] {
        "Hey you! Yes, you! It looks like you're trying to survive this mayhem.",
        "I can help you fight them off, but I need some resources first.",
        "Bring me 3 USBs dropped from enemies, and I'll join your side."
    };
    [TextArea(2, 5)]
    [SerializeField] private string[] completeDialoguePages = new string[] {
        "Amazing! You actually found them all.",
        "Alright, a deal is a deal. Let's show them what we've got!"
    };

    [Header("Companion Settings")]
    [SerializeField] private float followDistance = 2f;
    [Tooltip("Khoảng đệm chống rung: đã bắt đầu đi rồi thì phải vào gần hơn (followDistance - giá trị này) mới " +
             "dừng. Thiếu nó thì lúc Player đi chậm, NPC sẽ đi-dừng liên tục mỗi frame và animation nháy idle/run")]
    [SerializeField] private float followStopBuffer = 0.3f;
    [SerializeField] private float followSpeed = 4f;
    [SerializeField] private float shootCooldown = 2f;
    [SerializeField] private float damageMultiplier = 0.8f;
    [SerializeField] private float shootRange = 10f;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;

    private NPCQuestState currentState = NPCQuestState.Idle;
    
    // Companion logic
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private float nextShootTime = 0f;
    private bool isFollowing = false; // Đang trong trạng thái chạy theo Player (dùng cho cả animation lẫn hysteresis)

    protected override void Awake()
    {
        // Kiểm tra singleton TRƯỚC base.Awake(): bản thừa sắp bị Destroy thì không cần nối listener làm gì
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        base.Awake(); // Nối nút tương tác + ẩn nút đi (xem InteractableNPC)
    }

    private void Update()
    {
        if (Player.Instance == null) return;

        switch (currentState)
        {
            case NPCQuestState.Idle:
            case NPCQuestState.QuestReadyToComplete:
            case NPCQuestState.QuestAccepted:
                // QuestAccepted vẫn hiện nút để người chơi hỏi lại còn thiếu bao nhiêu viên
                UpdateInteractionButton();
                break;
            case NPCQuestState.Companion:
                UpdateInteractionButton(false); // Đã đồng hành thì không tương tác được nữa
                FollowPlayer();
                AutoShoot();
                break;
        }
    }

    public override void OnInteract()
    {
        if (dialogueUI == null) return;

        // Đồng bộ lại trạng thái với số USB ĐANG THỰC SỰ CÓ trước khi quyết định hiện hội thoại nào. USB là tài
        // nguyên tiêu hao dùng chung (sau này còn để trao đổi trong màn), nên số lượng có thể thay đổi giữa 2 lần
        // nói chuyện mà không qua sự kiện nhặt đồ nào. Trạng thái lưu sẵn luôn có nguy cơ lỗi thời.
        CheckQuestProgress();
        if (currentState == NPCQuestState.QuestReadyToComplete && GetCollectedUsb() < usbRequired)
        {
            currentState = NPCQuestState.QuestAccepted; // Đã tiêu USB vào việc khác sau khi gom đủ -> quay lại đòi
        }

        switch (currentState)
        {
            case NPCQuestState.Idle:
                dialogueUI.ShowDialogue(npcName, questDialoguePages, "Accept", AcceptQuest, portrait);
                break;

            case NPCQuestState.QuestAccepted:
                // Lời nhắc nhiệm vụ ngắn (không đổi trạng thái)
                int missing = usbRequired - GetCollectedUsb();
                string[] reminder = new string[] { $"I still need {Mathf.Max(0, missing)} more USBs." };
                dialogueUI.ShowDialogue(npcName, reminder, "Got it", () => {}, portrait);
                break;

            case NPCQuestState.QuestReadyToComplete:
                dialogueUI.ShowDialogue(npcName, completeDialoguePages, "Complete", StartCompanion, portrait);
                break;
        }
    }

    private void AcceptQuest()
    {
        currentState = NPCQuestState.QuestAccepted;

        // PHẢI kiểm tra lại NGAY tại đây. USB là tài nguyên của ván, người chơi hoàn toàn có thể đã gom đủ TRƯỚC
        // khi gặp NPC lần đầu. Nếu chỉ dựa vào sự kiện nhặt USB thì lúc đó không có sự kiện nào bắn ra nữa ->
        // nhiệm vụ kẹt ở QuestAccepted, NPC đòi "thêm 0 viên" và phải nhặt dư 1 viên mới thoát ra được.
        CheckQuestProgress();
    }

    // USB là tài nguyên dùng chung của ván (GameManager), KHÔNG phải bộ đếm riêng của NPC - vì sau này còn dùng
    // để trao đổi thứ khác trong màn, và khi giao nhiệm vụ thì bị TIÊU HAO đi.
    private int GetCollectedUsb()
    {
        return (GameManager.Instance != null) ? GameManager.Instance.CollectedUsb : 0;
    }

    public void OnUSBCollected()
    {
        CheckQuestProgress();
    }

    // Gọi từ CẢ 2 chỗ: lúc nhặt USB và lúc vừa nhận nhiệm vụ. Đừng gộp lại vào riêng OnUSBCollected() - xem
    // ghi chú ở AcceptQuest().
    private void CheckQuestProgress()
    {
        if (currentState != NPCQuestState.QuestAccepted) return;
        if (GetCollectedUsb() < usbRequired) return;

        currentState = NPCQuestState.QuestReadyToComplete;

        // Thử dùng QuestNotificationUI tĩnh nếu field notificationUI chưa được gán
        var notifyUI = notificationUI != null ? notificationUI : QuestNotificationUI.Instance;

        if (notifyUI != null)
        {
            notifyUI.ShowNotification($"Quest Completed! Return to {npcName}.");
        }
    }

    private void StartCompanion()
    {
        // Giao nhiệm vụ = ĐƯA USB cho NPC, nên phải trừ đi. Chỉ tới được đây khi đã đủ số lượng (state machine
        // lo việc đó), nên không cần xử lý nhánh thiếu tiền.
        if (GameManager.Instance != null) GameManager.Instance.SpendUsb(usbRequired);

        currentState = NPCQuestState.Companion;
        
        // Remove collision so NPC doesn't block player
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }
    }

    private void FollowPlayer()
    {
        Vector2 targetPos = Player.Instance.transform.position;
        float dist = Vector2.Distance(transform.position, targetPos);

        // Hysteresis (2 ngưỡng khác nhau cho lúc bắt đầu đi và lúc dừng): chỉ dùng 1 ngưỡng thì khi Player đi
        // chậm hơn NPC, khoảng cách sẽ liên tục nhảy qua lại quanh followDistance -> NPC đi-dừng mỗi frame và
        // animation nháy idle/run.
        if (!isFollowing && dist > followDistance)
        {
            isFollowing = true;
        }
        else if (isFollowing && dist <= Mathf.Max(0f, followDistance - followStopBuffer))
        {
            isFollowing = false;
        }

        if (isFollowing)
        {
            transform.position = Vector2.MoveTowards(transform.position, targetPos, followSpeed * Time.deltaTime);
        }

        // Dùng chung tên tham số "isRun" với Animator của Player cho nhất quán
        if (animator != null) animator.SetBool("isRun", isFollowing);

        if (spriteRenderer != null)
        {
            if (targetPos.x > transform.position.x)
                spriteRenderer.flipX = false;
            else if (targetPos.x < transform.position.x)
                spriteRenderer.flipX = true;
        }
    }

    private void AutoShoot()
    {
        if (Time.time < nextShootTime) return;

        Enemy target = FindClosestEnemy();
        if (target != null)
        {
            nextShootTime = Time.time + shootCooldown;
            ShootAt(target.transform.position);
        }
    }

    private Enemy FindClosestEnemy()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, shootRange);
        Enemy closest = null;
        float minDistance = float.MaxValue;

        foreach (var col in colliders)
        {
            if (col.CompareTag("Enemy"))
            {
                Enemy enemy = col.GetComponent<Enemy>();
                // PHẢI thoát ngay khi không lấy được component Enemy (VD collider phụ gắn tag Enemy). Nếu để lọt
                // xuống dưới thì closest bị gán = null nhưng minDistance vẫn bị chiếm chỗ, làm các con Enemy thật
                // ở xa hơn bị loại oan -> NPC đứng im không bắn dù quanh đó đầy quái.
                if (enemy == null) continue;

                float dist = Vector2.Distance(transform.position, col.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = enemy;
                }
            }
        }

        return closest;
    }

    private void ShootAt(Vector3 targetPos)
    {
        if (bulletPrefab == null) return;
        
        Transform spawnPoint = firePoint != null ? firePoint : transform;
        Vector3 direction = (targetPos - spawnPoint.position).normalized;

        GameObject bullet;
        if (ObjectPoolManager.Instance != null)
        {
            bullet = ObjectPoolManager.Instance.SpawnObject(bulletPrefab, spawnPoint.position, Quaternion.identity);
        }
        else
        {
            bullet = Instantiate(bulletPrefab, spawnPoint.position, Quaternion.identity);
        }

        PlayerBullet pBullet = bullet.GetComponent<PlayerBullet>();
        if (pBullet != null)
        {
            // Set damage to 0.8x of player's current damage
            float dmg = Player.Instance != null ? Player.Instance.GetCurrentDamage() : 10f;
            pBullet.dmg = dmg * damageMultiplier;

            // Đạn lấy từ Pool có thể còn giữ maxRange của lần bắn Burst Shot trước đó (Gun.SpawnBullet cũng
            // phải reset y hệt) - không set lại thì đạn NPC tự biến mất sau 5f nếu dùng chung prefab với Player.
            pBullet.maxRange = 0f;

            // Xoay đạn về phía mục tiêu
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            bullet.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }
}

