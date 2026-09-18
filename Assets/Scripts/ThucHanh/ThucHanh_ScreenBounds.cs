using UnityEngine;

// Tính biên trái/phải/trên/dưới của màn hình chơi game (theo Camera Main, giả định Orthographic nhìn thẳng vào
// mặt phẳng z = 0) - dùng chung cho ThucHanh_ObjectA/B/C để xác định vị trí xuất hiện và kiểm tra chạm biên.
public static class ThucHanh_ScreenBounds
{
    public static float Left { get; private set; }
    public static float Right { get; private set; }
    public static float Top { get; private set; }
    public static float Bottom { get; private set; }

    // Gọi lại mỗi khi cần dùng (VD lúc Start của từng đối tượng) - không cache vĩnh viễn vì kích thước màn hình
    // có thể khác nhau giữa lúc test trong Editor và lúc build thật.
    public static void Refresh()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float distanceToPlane = Mathf.Abs(cam.transform.position.z);
        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0f, 0f, distanceToPlane));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1f, 1f, distanceToPlane));

        Left = bottomLeft.x;
        Bottom = bottomLeft.y;
        Right = topRight.x;
        Top = topRight.y;
    }
}
