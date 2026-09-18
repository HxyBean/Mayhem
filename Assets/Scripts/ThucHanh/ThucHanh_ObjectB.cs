using UnityEngine;

// ĐỐI TƯỢNG B (Enemy) - CHỈ di chuyển theo 1 trục duy nhất (mặc định: NGANG trái/phải) để dễ kiểm tra
// đúng yêu cầu "đối tượng B chạm biên xa thì xuất hiện lại ở biên đối diện, vị trí ngẫu nhiên" - không bị
// trộn lẫn với chuyển động lên/xuống như trước.
// - Axis = Horizontal: B chỉ di chuyển sang trái/phải (Y giữ nguyên, trừ lúc wrap thì random Y dọc biên).
// - Axis = Vertical:   B chỉ di chuyển lên/xuống    (X giữ nguyên, trừ lúc wrap thì random X dọc biên).
// - Xuất hiện chính giữa biên PHẢI (Horizontal) / biên DƯỚI (Vertical) - đối diện Đối tượng A.
// - Kích thước LUÔN bằng Đối tượng A (copy trực tiếp localScale từ A lúc Start).
public class ThucHanh_ObjectB : MonoBehaviour
{
    [Header("Bố trí trên màn hình")]
    [Tooltip("PHẢI đặt giống Axis trên ThucHanh_ObjectA để 2 đối tượng xuất hiện đối diện nhau. Horizontal = B chỉ di chuyển ngang trái/phải.")]
    [SerializeField] private ThucHanh_ScreenAxis axis = ThucHanh_ScreenAxis.Horizontal;
    [Tooltip("Khoảng cách từ mép sprite tới biên màn hình lúc xuất hiện/bật lại/wrap - nên set khoảng bằng nửa kích thước sprite để không bị cắt hình")]
    [SerializeField] private float edgeMargin = 0.5f;

    [Header("Kích thước (đồng bộ với Đối tượng A)")]
    [Tooltip("Kéo GameObject của ThucHanh_ObjectA vào đây - B sẽ tự lấy đúng localScale của A lúc bắt đầu")]
    [SerializeField] private Transform objectA;

    [Header("Di chuyển")]
    [SerializeField] private float moveSpeed = 3f;

    // Tốc độ CÓ DẤU dọc theo trục di chuyển (dương = sang phải/lên trên, âm = sang trái/xuống dưới) -
    // chỉ 1 giá trị vì B giờ chỉ di chuyển đúng 1 trục, không còn Vector2 velocity 2 chiều như trước.
    private float signedSpeed;

    private void Start()
    {
        ThucHanh_ScreenBounds.Refresh();
        MatchSizeWithObjectA();
        PlaceAtSpawnPoint();
        PickInitialDirection();
    }

    private void MatchSizeWithObjectA()
    {
        if (objectA != null) transform.localScale = objectA.localScale;
    }

    private void PlaceAtSpawnPoint()
    {
        Vector3 pos = transform.position;

        if (axis == ThucHanh_ScreenAxis.Horizontal)
        {
            pos.x = ThucHanh_ScreenBounds.Right - edgeMargin;
            pos.y = (ThucHanh_ScreenBounds.Top + ThucHanh_ScreenBounds.Bottom) / 2f;
        }
        else
        {
            pos.x = (ThucHanh_ScreenBounds.Left + ThucHanh_ScreenBounds.Right) / 2f;
            pos.y = ThucHanh_ScreenBounds.Bottom + edgeMargin;
        }

        transform.position = pos;
    }

    // Random chiều ban đầu (trái hoặc phải / lên hoặc xuống) cho có yếu tố ngẫu nhiên - sau đó cứ bật lại ở
    // biên gần và wrap ở biên xa theo đúng chu kỳ, không tự đổi hướng giữa chừng để dễ quan sát/kiểm tra.
    private void PickInitialDirection()
    {
        signedSpeed = (Random.value < 0.5f ? 1f : -1f) * moveSpeed;
    }

    private void Update()
    {
        Vector3 pos = transform.position;

        if (axis == ThucHanh_ScreenAxis.Horizontal)
        {
            pos.x += signedSpeed * Time.deltaTime;

            if (pos.x <= ThucHanh_ScreenBounds.Left)
            {
                WrapToOppositeEdge(ref pos);
            }
            else if (pos.x >= ThucHanh_ScreenBounds.Right)
            {
                pos.x = ThucHanh_ScreenBounds.Right;
                signedSpeed = -Mathf.Abs(signedSpeed);
            }
        }
        else
        {
            pos.y += signedSpeed * Time.deltaTime;

            if (pos.y >= ThucHanh_ScreenBounds.Top)
            {
                WrapToOppositeEdge(ref pos);
            }
            else if (pos.y <= ThucHanh_ScreenBounds.Bottom)
            {
                pos.y = ThucHanh_ScreenBounds.Bottom;
                signedSpeed = Mathf.Abs(signedSpeed);
            }
        }

        transform.position = pos;
    }

    // Yêu cầu đề bài: chạm biên trái/trên (xa nhất so với điểm xuất hiện) -> xuất hiện lại ở biên đối diện
    // (phải/dưới), vị trí NGẪU NHIÊN dọc theo biên đó, rồi tiếp tục đi ngược lại về phía biên xa (lặp vòng
    // qua lại 2 biên liên tục - dễ quan sát để kiểm tra đúng yêu cầu).
    private void WrapToOppositeEdge(ref Vector3 pos)
    {
        if (axis == ThucHanh_ScreenAxis.Horizontal)
        {
            pos.x = ThucHanh_ScreenBounds.Right - edgeMargin;
            pos.y = Random.Range(ThucHanh_ScreenBounds.Bottom + edgeMargin, ThucHanh_ScreenBounds.Top - edgeMargin);
        }
        else
        {
            pos.y = ThucHanh_ScreenBounds.Bottom + edgeMargin;
            pos.x = Random.Range(ThucHanh_ScreenBounds.Left + edgeMargin, ThucHanh_ScreenBounds.Right - edgeMargin);
        }

        signedSpeed = -Mathf.Abs(signedSpeed); // Lại hướng về phía biên xa (trái/trên)
    }
}
