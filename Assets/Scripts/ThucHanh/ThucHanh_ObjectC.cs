using UnityEngine;

// ĐỐI TƯỢNG C (đạn/bom/tên lửa...) - lập trình theo đúng cơ chế PlayerBullet.cs của project gốc:
// bay thẳng bằng transform.Translate(Vector2.right * moveSpeed) dựa theo ĐÚNG HƯỚNG (rotation) đã được
// ThucHanh_ObjectA set sẵn lúc Instantiate, KHÔNG lưu vector hướng riêng bên trong viên đạn.
public class ThucHanh_ObjectC : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 8f;
    [Tooltip("Tự hủy sau khoảng thời gian này nếu chưa trúng Đối tượng B (giống timeDestroy của PlayerBullet.cs)")]
    [SerializeField] private float timeDestroy = 3f;

    private void Awake()
    {
        // Đạn tự di chuyển bằng code (Translate), không cần mô phỏng vật lý - ép Kinematic để không bị
        // vật lý tác động ngược lại (giống PlayerBullet.Awake() của project gốc).
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    // Dùng OnEnable thay vì Start để hoạt động đúng cả khi đạn được tái sử dụng qua Object Pool sau này
    private void OnEnable()
    {
        Invoke(nameof(DisableBullet), timeDestroy);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(DisableBullet));
    }

    private void Update()
    {
        MoveBullet();
    }

    private void MoveBullet()
    {
        transform.Translate(Vector2.right * moveSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Kiểm tra thẳng component thay vì Tag - không cần khai báo thêm Tag mới trong Project Settings
        if (collision.GetComponent<ThucHanh_ObjectB>() != null)
        {
            DisableBullet();
        }
    }

    private void DisableBullet()
    {
        Destroy(gameObject);
    }

    // Gọi từ ThucHanh_ObjectA ngay sau khi Instantiate, nếu muốn tốc độ bắn khác giá trị mặc định trên Prefab
    public void SetSpeed(float speed)
    {
        moveSpeed = speed;
    }
}
