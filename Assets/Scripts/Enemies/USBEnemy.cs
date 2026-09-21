using System.Collections;
using UnityEngine;

// Quái rơi vật phẩm USB. KHÔNG nằm trong danh sách spawn ngẫu nhiên của EnemySpawner - chỉ xuất hiện sau mỗi
// killsPerUsbEnemy con quái bị hạ (xem EnemySpawner.OnEnemyKilled).
// Vẫn giữ nguyên mọi đặc điểm quái thường (máu/va chạm/hệ số độ khó Stage), chỉ thêm chiêu LƯỚT: vào trong
// dashTriggerRadius thì đứng vận sức chargeTime giây (có cảnh báo hướng), rồi lướt thẳng theo hướng đã khóa.
public class USBEnemy : Enemy
{
    [Header("Vật phẩm rơi ra")]
    [SerializeField] private GameObject usbObject;

    [Header("Chiêu Lướt")]
    [Tooltip("Vào trong bán kính này thì bắt đầu vận sức để lướt")]
    [SerializeField] private float dashTriggerRadius = 8f;
    [Tooltip("Thời gian đứng im vận sức trước khi lướt")]
    [SerializeField] private float chargeTime = 0.75f;
    [Tooltip("Quãng đường lướt được")]
    [SerializeField] private float dashDistance = 8f;
    [SerializeField] private float dashSpeed = 25f;
    [Tooltip("Tính từ lúc BẮT ĐẦU vận sức, không phải lúc lướt xong")]
    [SerializeField] private float dashCooldown = 3f;
    [Tooltip("Hiệu ứng cảnh báo hướng lướt (mũi tên/vệt sáng), sprite phải chĩa sang PHẢI ở góc 0 độ")]
    [SerializeField] private GameObject dashWarningPrefab;

    private float nextDashTime = 0f;
    private bool isCharging = false;
    private bool isDashing = false;
    private GameObject activeWarning;
    private Quaternion warningRotation = Quaternion.identity;

    protected override void OnEnable()
    {
        base.OnEnable();

        // Reset vì object được tái sử dụng qua Pool - còn sót cờ của lần trước là con mới sẽ đứng đơ
        isCharging = false;
        isDashing = false;
        nextDashTime = Time.time + dashCooldown;
    }

    // Quái có thể bị hạ ngay giữa lúc vận sức/lướt (bị SetActive(false)), coroutine chết theo mà hiệu ứng cảnh
    // báo thì vẫn còn nằm lại trên bản đồ -> phải tự dọn.
    private void OnDisable()
    {
        isCharging = false;
        isDashing = false;
        RemoveWarning();
    }

    protected override void Update()
    {
        if (player == null) return;

        // Đang vận sức hoặc đang lướt thì coroutine lo hết, không đuổi theo Player nữa
        if (isCharging || isDashing) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
        if (Time.time >= nextDashTime && distanceToPlayer <= dashTriggerRadius)
        {
            StartCoroutine(DashRoutine());
            return;
        }

        base.Update(); // Ngoài lúc dùng chiêu thì đuổi theo Player y như quái thường
    }

    // Cảnh báo KHÔNG phải con của quái (xem SpawnWarning) nên phải tự kéo theo.
    //
    // ĐẶT Ở LateUpdate CHỨ KHÔNG PHẢI Update: nếu prefab cảnh báo có Animator mà clip lỡ có key Rotation
    // (rất hay gặp khi record animation hiệu ứng), Animator chạy SAU Update và sẽ ghi đè sạch góc xoay vừa set
    // -> nhìn như mũi tên không bao giờ xoay theo hướng lướt. LateUpdate chạy sau Animator nên luôn thắng.
    private void LateUpdate()
    {
        if (activeWarning == null) return;

        activeWarning.transform.SetPositionAndRotation(transform.position, warningRotation);
    }

    private IEnumerator DashRoutine()
    {
        isCharging = true;
        nextDashTime = Time.time + dashCooldown;

        // KHÓA hướng ngay từ đầu lúc vận sức (giống chiêu Teleport của Boss): nhờ vậy Player có cả chargeTime
        // để né sang bên, thay vì bị chiêu bám dính không thể tránh.
        Vector2 dashDirection = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
        if (dashDirection.sqrMagnitude < 0.0001f) dashDirection = Vector2.right;

        FaceDirection(dashDirection);
        SpawnWarning(dashDirection);

        yield return new WaitForSeconds(chargeTime);

        RemoveWarning();
        isCharging = false;

        isDashing = true;
        Vector2 dashTarget = (Vector2)transform.position + dashDirection * dashDistance;

        float traveled = 0f;
        while (traveled < dashDistance)
        {
            float step = dashSpeed * Time.deltaTime;
            transform.position = Vector2.MoveTowards(transform.position, dashTarget, step);
            traveled += step;
            yield return null;
        }

        isDashing = false;
    }

    private void FaceDirection(Vector2 direction)
    {
        // Cùng quy ước với Enemy.FlipEnemy(): lật bằng localScale.x
        transform.localScale = new Vector3(direction.x < 0f ? -1f : 1f, 1f, 1f);
    }

    // Spawn cảnh báo ở world space, xoay đúng hướng sắp lướt.
    //
    // CỐ Ý KHÔNG gắn làm con của quái (dù hiệu ứng chém của Knight có làm vậy): quái này có thể bị hạ ngay giữa
    // lúc vận sức, lúc đó Enemy.Die() trả nó về Pool -> SetActive(false) -> OnDisable() -> RemoveWarning() ->
    // ReturnObjectToPool() gọi SetParent(null) ngay giữa lúc chính parent đang bị tắt. Unity CẤM đổi parent
    // trong lúc parent đang activate/deactivate và ném lỗi "Cannot set the parent of the GameObject ... while
    // activating or deactivating the parent". Không parent thì không có gì phải gỡ.
    //
    // QUY TẮC CHUNG: hiệu ứng gắn làm con của một object CÓ THỂ BỊ TRẢ VỀ POOL thì đừng dọn hiệu ứng đó trong
    // OnDisable. Hiệu ứng của Knight an toàn vì parent là Player - Player không bao giờ vào Pool.
    private void SpawnWarning(Vector2 direction)
    {
        if (dashWarningPrefab == null) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        warningRotation = Quaternion.Euler(0f, 0f, angle);

        activeWarning = ObjectPoolManager.Instance != null
            ? ObjectPoolManager.Instance.SpawnObject(dashWarningPrefab, transform.position, warningRotation)
            : Instantiate(dashWarningPrefab, transform.position, warningRotation);
    }

    private void RemoveWarning()
    {
        if (activeWarning == null) return;

        if (ObjectPoolManager.Instance != null) ObjectPoolManager.Instance.ReturnObjectToPool(activeWarning);
        else Destroy(activeWarning);

        activeWarning = null;
    }

    protected override void DropItems()
    {
        if (usbObject != null) SpawnItem(usbObject);

        if (xpObject != null)
        {
            int smallCount = UnityEngine.Random.Range(1, 3);
            for (int i = 0; i < smallCount; i++) SpawnItem(xpObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, dashTriggerRadius);
    }
}
