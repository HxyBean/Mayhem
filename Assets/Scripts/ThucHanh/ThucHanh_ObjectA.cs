using UnityEngine;

// ĐỐI TƯỢNG A (Player).
// - Xuất hiện chính giữa biên TRÁI (Axis = Horizontal) hoặc biên TRÊN (Axis = Vertical) của màn hình.
// - Di chuyển ngẫu nhiên (hướng đổi định kỳ + bật lại khi chạm biên), tốc độ chỉnh được trong Inspector.
// - Chạm/click màn hình ở đâu thì bắn 1 viên Đối tượng C (đạn) từ vị trí hiện tại của A bay về phía đó.
public class ThucHanh_ObjectA : MonoBehaviour
{
    [Header("Bố trí trên màn hình")]
    [Tooltip("PHẢI đặt giống Axis trên ThucHanh_ObjectB để 2 đối tượng xuất hiện đối diện nhau")]
    [SerializeField] private ThucHanh_ScreenAxis axis = ThucHanh_ScreenAxis.Horizontal;
    [Tooltip("Khoảng cách từ mép sprite tới biên màn hình lúc xuất hiện/bật lại - nên set khoảng bằng nửa kích thước sprite để không bị cắt hình")]
    [SerializeField] private float edgeMargin = 0.5f;

    [Header("Di chuyển")]
    [SerializeField] private float moveSpeed = 3f;
    [Tooltip("Sau mỗi khoảng thời gian này (giây) tự đổi sang 1 hướng ngẫu nhiên mới, kể cả khi chưa chạm biên. 0 = chỉ đổi hướng khi chạm biên")]
    [SerializeField] private float randomDirectionInterval = 2.5f;

    [Header("Bắn Đối tượng C (đạn)")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 8f;

    private Vector2 velocity;
    private float directionTimer;

    private void Start()
    {
        ThucHanh_ScreenBounds.Refresh();
        PlaceAtSpawnPoint();
        PickRandomDirection();
    }

    private void PlaceAtSpawnPoint()
    {
        Vector3 pos = transform.position;

        if (axis == ThucHanh_ScreenAxis.Horizontal)
        {
            pos.x = ThucHanh_ScreenBounds.Left + edgeMargin;
            pos.y = (ThucHanh_ScreenBounds.Top + ThucHanh_ScreenBounds.Bottom) / 2f;
        }
        else
        {
            pos.x = (ThucHanh_ScreenBounds.Left + ThucHanh_ScreenBounds.Right) / 2f;
            pos.y = ThucHanh_ScreenBounds.Top - edgeMargin;
        }

        transform.position = pos;
    }

    // Hướng di chuyển ngẫu nhiên linh hoạt (có thể ra lên/xuống/trái/phải hoặc bất kỳ góc nào ở giữa)
    private void PickRandomDirection()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * moveSpeed;
        directionTimer = 0f;
    }

    private void Update()
    {
        Move();
        HandleRandomDirectionTimer();
        HandleClickToShoot();
    }

    private void Move()
    {
        transform.position += (Vector3)(velocity * Time.deltaTime);
        BounceOffScreenEdges();
    }

    // Chạm biên nào thì bật ngược lại đúng trục đó (kiểu bóng nảy tường) - giữ A luôn ở trong màn hình
    private void BounceOffScreenEdges()
    {
        Vector3 pos = transform.position;

        if (pos.x <= ThucHanh_ScreenBounds.Left) { pos.x = ThucHanh_ScreenBounds.Left; velocity.x = Mathf.Abs(velocity.x); }
        else if (pos.x >= ThucHanh_ScreenBounds.Right) { pos.x = ThucHanh_ScreenBounds.Right; velocity.x = -Mathf.Abs(velocity.x); }

        if (pos.y <= ThucHanh_ScreenBounds.Bottom) { pos.y = ThucHanh_ScreenBounds.Bottom; velocity.y = Mathf.Abs(velocity.y); }
        else if (pos.y >= ThucHanh_ScreenBounds.Top) { pos.y = ThucHanh_ScreenBounds.Top; velocity.y = -Mathf.Abs(velocity.y); }

        transform.position = pos;
    }

    private void HandleRandomDirectionTimer()
    {
        if (randomDirectionInterval <= 0f) return;

        directionTimer += Time.deltaTime;
        if (directionTimer >= randomDirectionInterval)
        {
            PickRandomDirection();
        }
    }

    // Bắn đạn khi chạm/click bất kỳ đâu trên màn hình (chuột trên PC/AVD, chạm tay trên điện thoại thật)
    private void HandleClickToShoot()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (Camera.main == null) return;

        Vector3 targetWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        targetWorldPos.z = transform.position.z;

        Shoot(targetWorldPos);
    }

    // Tính rotation quay đúng hướng bắn rồi Instantiate với rotation đó (giống Gun.SpawnBullet() của project gốc:
    // firePos.rotation set trước, PlayerBullet chỉ Translate theo Vector2.right của chính nó, không cần biết hướng).
    private void Shoot(Vector3 targetPos)
    {
        if (bulletPrefab == null) return;

        Vector2 direction = ((Vector2)(targetPos - transform.position)).normalized;
        if (direction.sqrMagnitude < 0.0001f) direction = Vector2.right;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion bulletRotation = Quaternion.Euler(0f, 0f, angle);

        GameObject bulletObj = Instantiate(bulletPrefab, transform.position, bulletRotation);
        ThucHanh_ObjectC bullet = bulletObj.GetComponent<ThucHanh_ObjectC>();
        if (bullet != null) bullet.SetSpeed(bulletSpeed);
    }
}
