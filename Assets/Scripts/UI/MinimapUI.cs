using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Minimap tròn kiểu LA BÀN CHỈ HƯỚNG: Player luôn ở tâm, mỗi object mang MinimapMarker hiện thành 1 mũi tên
// chỉ về phía nó. Ở trong tầm thì mũi tên đứng đúng vị trí tương đối, ra ngoài tầm thì DÍNH VÀO VIỀN tròn và
// vẫn chỉ đúng hướng - nên luôn biết đường quay lại NPC dù ở xa cỡ nào.
//
// Cố ý KHÔNG dùng camera phụ + Render Texture để vẽ bản đồ thật: mục đích ở đây chỉ là tìm lại NPC, mà thêm
// một camera render mỗi frame là cái giá quá đắt trên mobile so với vài phép tính vector.
public class MinimapUI : MonoBehaviour
{
    [Tooltip("Vùng tròn của minimap. Mũi tên được đặt làm con của object này và tính vị trí từ TÂM của nó")]
    [SerializeField] private RectTransform minimapArea;
    [Tooltip("Prefab 1 mũi tên (chỉ cần 1 GameObject + Image). Script tự nhân bản theo số mốc cần hiện")]
    [SerializeField] private RectTransform markerPrefab;

    [Header("Tầm nhìn")]
    [Tooltip("Bán kính THẾ GIỚI mà minimap bao quát (đơn vị Unity). Xa hơn khoảng này thì mũi tên dính viền")]
    [SerializeField] private float worldRange = 30f;
    [Tooltip("Chừa lại bấy nhiêu pixel ở mép để mũi tên không bị cắt mất nửa khi dính viền")]
    [SerializeField] private float edgePadding = 10f;

    [Header("Hướng icon")]
    [Tooltip("Tick nếu sprite mũi tên vẽ CHĨA LÊN ở góc 0 độ (kiểu thường gặp của icon UI). Bỏ tick nếu nó " +
             "chĩa sang PHẢI. Sai ô này là mọi mũi tên lệch đúng 90 độ")]
    [SerializeField] private bool iconPointsUp = true;

    // Các mũi tên đã nhân bản, dùng lại theo chỉ số - thừa thì ẩn đi chứ không Destroy (tránh rác GC mỗi khi
    // có NPC bật/tắt).
    private readonly List<RectTransform> icons = new List<RectTransform>();
    private readonly List<Image> iconImages = new List<Image>();

    private void Update()
    {
        if (Player.Instance == null || minimapArea == null || markerPrefab == null)
        {
            HideFrom(0);
            return;
        }

        Vector2 playerPos = Player.Instance.transform.position;

        // Lấy cạnh NGẮN hơn để vòng tròn luôn nằm gọn trong khung, kể cả khi ai đó kéo minimap thành hình chữ nhật
        float uiRadius = Mathf.Min(minimapArea.rect.width, minimapArea.rect.height) * 0.5f;
        float clampRadius = Mathf.Max(0f, uiRadius - edgePadding);
        float pixelsPerUnit = (worldRange > 0f) ? uiRadius / worldRange : 0f;

        IReadOnlyList<MinimapMarker> markers = MinimapMarker.Active;
        int used = 0;

        for (int i = 0; i < markers.Count; i++)
        {
            MinimapMarker marker = markers[i];
            if (marker == null) continue; // Object bị Destroy giữa chừng mà chưa kịp chạy OnDisable

            Vector2 delta = (Vector2)marker.transform.position - playerPos;

            RectTransform icon = GetIcon(used);
            Image image = iconImages[used];
            used++;

            // Vị trí: quy đổi khoảng cách thế giới ra pixel, vượt quá viền thì kẹp lại đúng trên viền nhưng
            // GIỮ NGUYÊN hướng - đó là thứ biến nó thành mũi tên chỉ đường thay vì mốc biến mất.
            Vector2 uiPos = delta * pixelsPerUnit;
            if (uiPos.sqrMagnitude > clampRadius * clampRadius)
            {
                uiPos = uiPos.normalized * clampRadius;
            }
            icon.anchoredPosition = uiPos;

            // Hướng: luôn chĩa về phía NPC, kể cả lúc đang ở trong tầm
            if (delta.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                icon.localRotation = Quaternion.Euler(0f, 0f, iconPointsUp ? angle - 90f : angle);
            }

            image.color = marker.MarkerColor;
            if (marker.IconOverride != null) image.sprite = marker.IconOverride;
        }

        HideFrom(used);
    }

    private RectTransform GetIcon(int index)
    {
        while (icons.Count <= index)
        {
            RectTransform created = Instantiate(markerPrefab, minimapArea);

            // Ép neo + pivot về GIỮA ngay trong code thay vì trông chờ prefab set đúng: anchoredPosition chỉ
            // mang nghĩa "lệch bao nhiêu so với tâm" khi neo nằm ở giữa. Prefab neo ở góc là toàn bộ mũi tên
            // lệch hẳn sang một bên, rất mất công mò.
            created.anchorMin = new Vector2(0.5f, 0.5f);
            created.anchorMax = new Vector2(0.5f, 0.5f);
            created.pivot = new Vector2(0.5f, 0.5f);

            icons.Add(created);
            iconImages.Add(created.GetComponent<Image>());
        }

        if (!icons[index].gameObject.activeSelf) icons[index].gameObject.SetActive(true);
        return icons[index];
    }

    private void HideFrom(int startIndex)
    {
        for (int i = startIndex; i < icons.Count; i++)
        {
            if (icons[i] != null && icons[i].gameObject.activeSelf) icons[i].gameObject.SetActive(false);
        }
    }
}
