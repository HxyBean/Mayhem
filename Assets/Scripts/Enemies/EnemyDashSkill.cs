using System.Collections;
using UnityEngine;

// Chiêu LƯỚT dùng chung: đứng vận sức (có cảnh báo hướng) rồi lướt thẳng theo hướng đã khóa.
// Gắn component này vào BẤT KỲ Enemy nào muốn có chiêu đó - USBEnemy, Boss, và các loại thêm sau này.
//
// Tách thành component RỜI thay vì viết vào từng loại quái (cùng kiểu với DamageFlash/MinimapMarker): thêm
// chiêu cho 1 loại quái mới chỉ là Add Component, không phải sửa dòng code nào.
//
// 2 kiểu kích hoạt:
//  - autoTriggerByProximity = true  -> tự dùng khi Player vào trong triggerRadius (USBEnemy)
//  - autoTriggerByProximity = false -> chờ ai đó gọi TryDash() (Boss, gọi từ bộ chọn chiêu ngẫu nhiên)
public class EnemyDashSkill : MonoBehaviour
{
    [Header("Kích hoạt")]
    [Tooltip("Bật = tự lướt khi Player vào trong bán kính. Tắt = chờ script khác gọi TryDash() (Boss dùng kiểu này)")]
    [SerializeField] private bool autoTriggerByProximity = true;
    [Tooltip("Chỉ dùng khi bật auto: Player vào trong bán kính này thì bắt đầu vận sức")]
    [SerializeField] private float triggerRadius = 8f;

    [Header("Thông số chiêu")]
    [Tooltip("Thời gian đứng im vận sức trước khi lướt")]
    [SerializeField] private float chargeTime = 0.75f;
    [Tooltip("Quãng đường lướt được")]
    [SerializeField] private float dashDistance = 8f;
    [SerializeField] private float dashSpeed = 25f;
    [Tooltip("Tính từ lúc BẮT ĐẦU vận sức, không phải lúc lướt xong")]
    [SerializeField] private float cooldown = 3f;
    [Tooltip("Hiệu ứng cảnh báo hướng lướt (mũi tên/vệt sáng), sprite phải chĩa sang PHẢI ở góc 0 độ")]
    [SerializeField] private GameObject warningPrefab;

    private float nextDashTime = 0f;
    private bool isCharging = false;
    private bool isDashing = false;
    private GameObject activeWarning;
    private Quaternion warningRotation = Quaternion.identity;

    // Enemy.MoveToPlayer() đọc cờ này để đứng im nhường chỗ cho chiêu; Boss đọc thêm để không tung chiêu khác đè lên
    public bool IsBusy => isCharging || isDashing;
    public bool IsReady => !IsBusy && Time.time >= nextDashTime;

    private void OnEnable()
    {
        // Reset vì object được tái sử dụng qua Pool - còn sót cờ của lần trước là con mới sẽ đứng đơ
        isCharging = false;
        isDashing = false;
        nextDashTime = Time.time + cooldown;
    }

    // Quái có thể bị hạ ngay giữa lúc vận sức/lướt (bị SetActive(false)), coroutine chết theo mà hiệu ứng cảnh
    // báo thì vẫn còn nằm lại trên bản đồ -> phải tự dọn.
    private void OnDisable()
    {
        isCharging = false;
        isDashing = false;
        RemoveWarning();
    }

    private void Update()
    {
        if (!autoTriggerByProximity || IsBusy) return;
        if (Player.Instance == null) return;

        float distance = Vector2.Distance(transform.position, Player.Instance.transform.position);
        if (distance <= triggerRadius) TryDash();
    }

    // Trả về false nếu chưa hồi chiêu / đang bận. Boss dựa vào đó để biết lượt chiêu này coi như bỏ lỡ.
    public bool TryDash()
    {
        if (!IsReady || Player.Instance == null) return false;
        if (!gameObject.activeInHierarchy) return false; // Coroutine không chạy được trên object đang tắt

        StartCoroutine(DashRoutine());
        return true;
    }

    private IEnumerator DashRoutine()
    {
        isCharging = true;
        nextDashTime = Time.time + cooldown;

        // KHÓA hướng ngay từ đầu lúc vận sức (giống chiêu Teleport của Boss): nhờ vậy Player có cả chargeTime
        // để né sang bên, thay vì bị chiêu bám dính không thể tránh.
        Vector2 dashDirection = ((Vector2)Player.Instance.transform.position - (Vector2)transform.position).normalized;
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
    // CỐ Ý KHÔNG gắn làm con của quái: quái có thể bị hạ ngay giữa lúc vận sức, lúc đó nó bị trả về Pool ->
    // SetActive(false) -> OnDisable() -> RemoveWarning() -> ReturnObjectToPool() gọi SetParent(null) ngay giữa
    // lúc chính parent đang bị tắt, và Unity CẤM điều đó (xem mục 9 của CLAUDE.md). Không parent thì không có
    // gì phải gỡ. Gắn làm con còn làm sprite bị soi gương theo localScale.x âm của quái khi nó quay trái.
    private void SpawnWarning(Vector2 direction)
    {
        if (warningPrefab == null) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        warningRotation = Quaternion.Euler(0f, 0f, angle);

        activeWarning = ObjectPoolManager.Instance != null
            ? ObjectPoolManager.Instance.SpawnObject(warningPrefab, transform.position, warningRotation)
            : Instantiate(warningPrefab, transform.position, warningRotation);
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemyDashSound();
        }
    }

    private void RemoveWarning()
    {
        if (activeWarning == null) return;

        if (ObjectPoolManager.Instance != null) ObjectPoolManager.Instance.ReturnObjectToPool(activeWarning);
        else Destroy(activeWarning);

        activeWarning = null;
    }

    // Cảnh báo không phải con của quái nên phải tự kéo theo.
    //
    // ĐẶT Ở LateUpdate CHỨ KHÔNG PHẢI Update: nếu prefab cảnh báo có Animator mà clip lỡ có key Rotation
    // (rất hay gặp khi record animation hiệu ứng), Animator chạy SAU Update và sẽ ghi đè sạch góc xoay vừa set
    // -> nhìn như mũi tên không bao giờ xoay theo hướng lướt. LateUpdate chạy sau Animator nên luôn thắng.
    private void LateUpdate()
    {
        if (activeWarning == null) return;

        activeWarning.transform.SetPositionAndRotation(transform.position, warningRotation);
    }

    private void OnDrawGizmosSelected()
    {
        if (!autoTriggerByProximity) return;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}
