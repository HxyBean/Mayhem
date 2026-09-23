using UnityEngine;

// Hiệu ứng vật phẩm BẮN VỌT RA khi quái chết, thay vì hiện ra đứng im tại chỗ.
// Gắn vào prefab vật phẩm nào muốn có (EXP, Coin, Kim cương, Heart, Energy, USB...). Không gắn thì vật phẩm
// vẫn rơi bình thường như cũ - Enemy.SpawnItem() tự kiểm tra.
//
// Enemy.SpawnItem() spawn vật phẩm NGAY TẠI XÁC QUÁI rồi gọi Launch() với điểm đáp đã được kiểm tra là không
// nằm trong vật cản. Nhờ vậy vừa có hiệu ứng bắn ra, vừa không bao giờ đáp vào trong đá.
public class ItemDropMotion : MonoBehaviour
{
    [Tooltip("Thời gian bay từ xác quái tới chỗ đáp (giây)")]
    [SerializeField] private float flightDuration = 0.35f;
    [Tooltip("Độ cao vòng cung lúc bay. 0 = bay thẳng, không nảy")]
    [SerializeField] private float arcHeight = 0.7f;
    [Tooltip("Tốc độ xoay lúc bay (độ/giây). ĐỂ 0 nếu prefab có Animator: clip animation có thể keyed Rotation " +
             "và sẽ ghi đè, làm vật phẩm giật lung tung")]
    [SerializeField] private float spinSpeed = 0f;
    [Tooltip("Phóng to dần từ nhỏ lên kích thước thật trong lúc bay")]
    [SerializeField] private bool popScale = true;
    [SerializeField, Range(0.1f, 1f)] private float popStartScale = 0.4f;

    private Vector3 startPosition;
    private Vector3 endPosition;
    private Vector3 baseScale;
    private float timer;
    private bool isFlying;

    // Pickup.cs đọc cờ này để KHÔNG kéo vật phẩm về phía Player trong lúc nó còn đang bay ra.
    // Thiếu bước này thì 2 script cùng ghi transform.position mỗi frame và vật phẩm giật qua giật lại.
    public bool IsFlying => isFlying;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        // Object tái sử dụng qua Pool: con trước có thể bị tắt ngay giữa lúc đang bay (VD Player nhặt được
        // luôn trên đường bay), để sót cờ/scale là lần sau spawn ra bé tí hoặc đứng đơ.
        isFlying = false;
        timer = 0f;
        transform.localScale = baseScale;
    }

    public void Launch(Vector3 landingPosition)
    {
        startPosition = transform.position;
        endPosition = landingPosition;
        timer = 0f;
        isFlying = true;

        if (popScale) transform.localScale = baseScale * popStartScale;
    }

    private void Update()
    {
        if (!isFlying) return;

        timer += Time.deltaTime;
        float t = (flightDuration > 0f) ? Mathf.Clamp01(timer / flightDuration) : 1f;

        // Vòng cung: sin(0..PI) cho 0 ở 2 đầu và cao nhất ở giữa đường bay
        Vector3 position = Vector3.Lerp(startPosition, endPosition, t);
        position.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
        transform.position = position;

        if (spinSpeed != 0f) transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
        if (popScale) transform.localScale = Vector3.Lerp(baseScale * popStartScale, baseScale, t);

        if (t < 1f) return;

        // Đáp xuống: chốt lại đúng vị trí đích và trả tư thế về chuẩn, tránh vật phẩm nằm nghiêng ngả
        transform.position = endPosition;
        transform.localScale = baseScale;
        if (spinSpeed != 0f) transform.rotation = Quaternion.identity;
        isFlying = false;
    }
}
