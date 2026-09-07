using UnityEngine;

// Gắn vào 1 GameObject bất kỳ trong Canvas Main Menu (VD chính GameObject chứa BG1).
// Cho 2 ảnh nền giống hệt nhau (đặt cạnh nhau, đủ rộng che kín màn hình) chạy cuộn ngang liên tục,
// hết ảnh này lại nối đuôi ảnh kia - tạo cảm giác nền chạy vô hạn không thấy điểm nối.
public class ScrollingBackground : MonoBehaviour
{
    [Tooltip("2 bản sao Ảnh nền giống hệt nhau, đặt cạnh nhau lúc bắt đầu")]
    [SerializeField] private RectTransform bg1;
    [SerializeField] private RectTransform bg2;

    [Tooltip("Tốc độ cuộn (pixel/giây). Số dương = chạy sang trái, số âm = chạy sang phải")]
    [SerializeField] private float scrollSpeed = 50f;

    private float imageWidth;

    private void Start()
    {
        // rect.width là kích thước GỐC chưa phóng to - nếu bạn dùng Scale để phóng ảnh lên (thay vì kéo Width/Height
        // trong RectTransform) thì phải nhân thêm localScale.x mới ra đúng kích thước hiển thị thật trên màn hình.
        imageWidth = bg1.rect.width * bg1.localScale.x;

        // Đặt bg2 ngay sát bên phải bg1 lúc bắt đầu
        bg2.anchoredPosition = new Vector2(bg1.anchoredPosition.x + imageWidth, bg1.anchoredPosition.y);
    }

    private void Update()
    {
        float delta = -scrollSpeed * Time.deltaTime;
        bg1.anchoredPosition += new Vector2(delta, 0f);
        bg2.anchoredPosition += new Vector2(delta, 0f);

        // Ảnh nào đã trôi hết ra khỏi màn hình bên trái thì nối đuôi ra sau ảnh còn lại
        if (bg1.anchoredPosition.x <= -imageWidth)
        {
            bg1.anchoredPosition = new Vector2(bg2.anchoredPosition.x + imageWidth, bg1.anchoredPosition.y);
        }
        if (bg2.anchoredPosition.x <= -imageWidth)
        {
            bg2.anchoredPosition = new Vector2(bg1.anchoredPosition.x + imageWidth, bg2.anchoredPosition.y);
        }
    }
}
