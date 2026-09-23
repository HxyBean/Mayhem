using UnityEngine;

// Viên đạn bay theo vòng cung tới 1 ĐIỂM đã khóa sẵn, rơi xuống thì để lại vùng độc.
// Dùng bởi PoisonEnemy, nhưng không phụ thuộc gì vào nó nên loại quái khác muốn bắn kiểu này cũng dùng lại được.
//
// KHÔNG có Collider và KHÔNG gây sát thương lúc va chạm: đây là đạn bắn cầu vồng, nó BAY QUA đầu mọi thứ và chỉ
// có ý nghĩa tại chỗ rơi. Toàn bộ sát thương nằm ở vùng độc để lại.
public class PoisonProjectile : MonoBehaviour
{
    [Header("Đường bay")]
    [Tooltip("Thời gian bay tới đích. Đây CHÍNH LÀ khoảng thời gian người chơi có để chạy khỏi vệt cảnh báo - " +
             "để quá ngắn là không né được, quá dài thì né quá dễ")]
    [SerializeField] private float flightDuration = 1.2f;
    [Tooltip("Độ cao vòng cung. 0 = bay thẳng")]
    [SerializeField] private float arcHeight = 2.5f;
    [SerializeField] private float spinSpeed = 180f;

    [Header("Cảnh báo điểm rơi")]
    [Tooltip("Vệt cảnh báo hiện tại ĐIỂM RƠI suốt thời gian đạn bay, giống telegraph của chiêu Teleport Boss")]
    [SerializeField] private GameObject landingWarningPrefab;

    [Header("Sát thương lúc chạm đất")]
    [Tooltip("Sát thương gây NGAY khi đạn rơi xuống, cho Player còn đứng trong tầm nổ. Để 0 = không gây gì, " +
             "chỉ còn vùng độc")]
    [SerializeField] private float impactDamage = 10f;
    [Tooltip("Bán kính vụ nổ lúc chạm đất. NÊN để bằng bán kính vùng độc và khớp sprite vệt cảnh báo - người " +
             "chơi coi vệt cảnh báo là vùng nguy hiểm, lệch nhau là ăn đòn ở chỗ trông như an toàn")]
    [SerializeField] private float impactRadius = 1.5f;

    [Header("Vùng độc để lại")]
    [SerializeField] private GameObject poisonZonePrefab;
    [Tooltip("Sát thương mỗi tick của vùng độc. <= 0 = giữ nguyên giá trị đang set trên prefab vùng độc")]
    [SerializeField] private float zoneDamagePerTick = 5f;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float timer;
    private bool isFlying;
    private GameObject activeWarning;

    // Gọi ngay sau khi spawn. Điểm đến được KHÓA tại đây và không đổi nữa - đó là lý do vệt cảnh báo đáng tin.
    public void Launch(Vector3 target)
    {
        startPosition = transform.position;
        targetPosition = target;
        timer = 0f;
        isFlying = true;

        SpawnWarning();
    }

    private void OnEnable()
    {
        // Pool tái sử dụng: viên trước có thể bị tắt giữa chừng, còn sót cờ là viên mới đứng lơ lửng giữa trời
        isFlying = false;
        timer = 0f;
    }

    // Đạn có thể bị tắt giữa lúc đang bay (Scene dỡ, Pool dọn...). Không dọn thì vệt cảnh báo nằm lại vĩnh viễn
    // trên bản đồ và người chơi cứ né mãi một chỗ chẳng bao giờ có gì rơi xuống.
    private void OnDisable()
    {
        isFlying = false;
        RemoveWarning();
    }

    private void Update()
    {
        if (!isFlying) return;

        timer += Time.deltaTime;
        float t = (flightDuration > 0f) ? Mathf.Clamp01(timer / flightDuration) : 1f;

        Vector3 position = Vector3.Lerp(startPosition, targetPosition, t);
        position.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
        transform.position = position;

        if (spinSpeed != 0f) transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);

        if (t < 1f) return;

        Land();
    }

    private void Land()
    {
        isFlying = false;
        RemoveWarning();

        // Gây sát thương TRƯỚC khi spawn vùng độc và trước khi tự trả về Pool: đứng lì trong vệt cảnh báo phải
        // ăn đòn ngay, không phải chờ tick đầu tiên của vùng độc.
        DealImpactDamage();
        SpawnPoisonZone();

        if (ObjectPoolManager.Instance != null) ObjectPoolManager.Instance.ReturnObjectToPool(gameObject);
        else Destroy(gameObject);
    }

    // Đo từ targetPosition chứ KHÔNG phải transform.position: đó mới là chỗ vệt cảnh báo đứng. Hai giá trị này
    // trùng nhau ở khoảnh khắc chạm đất, nhưng lấy đúng điểm đã cảnh báo thì sát thương luôn khớp với thứ người
    // chơi nhìn thấy, kể cả sau này có sửa cách tính đường bay.
    private void DealImpactDamage()
    {
        if (impactDamage <= 0f || Player.Instance == null) return;

        float distance = Vector2.Distance(targetPosition, Player.Instance.transform.position);
        if (distance <= impactRadius) Player.Instance.TakeDmg(impactDamage);
    }

    // Vệt cảnh báo đứng YÊN tại điểm rơi, KHÔNG gắn làm con của viên đạn: nó phải nằm im ở đích trong khi viên
    // đạn còn đang bay, và gắn làm con của một object sắp bị trả về Pool là dính đúng lỗi "Cannot set the parent
    // ... while deactivating" ở mục 9 của CLAUDE.md.
    private void SpawnWarning()
    {
        if (landingWarningPrefab == null) return;

        activeWarning = ObjectPoolManager.Instance != null
            ? ObjectPoolManager.Instance.SpawnObject(landingWarningPrefab, targetPosition, Quaternion.identity)
            : Instantiate(landingWarningPrefab, targetPosition, Quaternion.identity);
    }

    private void RemoveWarning()
    {
        if (activeWarning == null) return;

        if (ObjectPoolManager.Instance != null) ObjectPoolManager.Instance.ReturnObjectToPool(activeWarning);
        else Destroy(activeWarning);

        activeWarning = null;
    }

    private void SpawnPoisonZone()
    {
        if (poisonZonePrefab == null) return;

        GameObject zone = ObjectPoolManager.Instance != null
            ? ObjectPoolManager.Instance.SpawnObject(poisonZonePrefab, targetPosition, Quaternion.identity)
            : Instantiate(poisonZonePrefab, targetPosition, Quaternion.identity);

        PoisonZone poisonZone = zone.GetComponent<PoisonZone>();
        if (poisonZone != null) poisonZone.SetDamagePerTick(zoneDamagePerTick);
    }
}
