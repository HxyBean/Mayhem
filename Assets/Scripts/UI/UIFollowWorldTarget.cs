using UnityEngine;

// Giữ 1 phần tử UI bám theo vị trí của 1 object trong thế giới (VD nút tương tác nổi trên đầu NPC).
//
// LÝ DO TỒN TẠI: Canvas Screen Space - Overlay LUÔN ăn raycast trước Canvas World Space, nên nút đặt trong
// canvas world-space gắn trên NPC rất dễ bị 1 Image bất kỳ của GameUI (kể cả trong suốt) chắn mất click, dù
// canvas đó đã có đủ Graphic Raycaster + Event Camera. Cách chắc ăn: để nút NẰM LUÔN trong GameUI (bấm được
// chắc chắn) rồi dùng script này kéo nó về đúng vị trí NPC trên màn hình.
public class UIFollowWorldTarget : MonoBehaviour
{
    [Tooltip("Object trong thế giới cần bám theo (VD chính GameObject của NPC)")]
    [SerializeField] private Transform target;

    [Tooltip("Dịch thêm so với tâm target, tính bằng đơn vị thế giới. Y dương = nổi lên phía trên đầu")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);

    private RectTransform rectTransform;
    private RectTransform parentRectTransform;
    private Canvas parentCanvas;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null) parentRectTransform = parentCanvas.GetComponent<RectTransform>();
    }

    // LateUpdate để chạy SAU khi NPC và camera đã di chuyển xong trong frame này, tránh bị trễ 1 frame
    private void LateUpdate()
    {
        if (target == null || parentCanvas == null) return;

        Camera worldCamera = Camera.main;
        if (worldCamera == null) return;

        Vector3 screenPoint = worldCamera.WorldToScreenPoint(target.position + worldOffset);

        // Canvas Overlay dùng thẳng toạ độ màn hình làm toạ độ world của UI
        if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            rectTransform.position = screenPoint;
            return;
        }

        // Screen Space - Camera / World Space: phải đổi điểm màn hình về toạ độ cục bộ trong canvas
        if (parentRectTransform == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRectTransform, screenPoint, parentCanvas.worldCamera, out Vector2 localPoint))
        {
            rectTransform.localPosition = localPoint;
        }
    }
}
