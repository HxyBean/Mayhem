using UnityEngine;
using UnityEngine.EventSystems;

public class BombButton : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private Gun gunScript;
    [SerializeField] private Transform aimReticle;
    [SerializeField] private RectTransform cancelZone;

    private bool isDragging = false;
    private bool isHoveringCancel = false;
    private Vector2 pressScreenPos; // Vị trí ngón tay lúc bắt đầu nhấn nút bom, dùng để tính độ lệch khi kéo

    private void Awake()
    {
        if (aimReticle != null) aimReticle.gameObject.SetActive(false);
        if (cancelZone != null) cancelZone.gameObject.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (gunScript == null || !gunScript.CanThrowBomb()) return;

        isDragging = true;
        pressScreenPos = eventData.position;

        if (aimReticle != null) aimReticle.gameObject.SetActive(true);
        if (cancelZone != null) cancelZone.gameObject.SetActive(true);

        UpdateAim(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        UpdateAim(eventData);
    }

    private void UpdateAim(PointerEventData eventData)
    {
        // Aim luôn xuất phát từ vị trí nhân vật, rồi lệch đi đúng bằng khoảng ngón tay đã kéo
        // so với lúc mới nhấn nút bom (thay vì bám theo vị trí tuyệt đối của ngón tay trên màn hình
        // như trước, khiến aim luôn bị kẹt ngay tại chỗ nút bom).
        if (aimReticle != null && Camera.main != null && Player.Instance != null)
        {
            Vector3 pressWorldPos = Camera.main.ScreenToWorldPoint(pressScreenPos);
            Vector3 currentWorldPos = Camera.main.ScreenToWorldPoint(eventData.position);
            Vector3 worldDelta = currentWorldPos - pressWorldPos;
            worldDelta.z = 0f;

            Vector3 targetPos = Player.Instance.transform.position + worldDelta;
            targetPos.z = 0f;
            aimReticle.position = targetPos;
        }

        // Kiểm tra xem ngón tay có nằm trong vùng Hủy (Cancel Zone) không
        if (cancelZone != null)
        {
            isHoveringCancel = RectTransformUtility.RectangleContainsScreenPoint(cancelZone, eventData.position, eventData.pressEventCamera);

            // Ẩn hồng tâm đi nếu đang kéo vào vùng hủy
            if (aimReticle != null)
            {
                aimReticle.gameObject.SetActive(!isHoveringCancel);
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        isDragging = false;
        
        if (cancelZone != null) cancelZone.gameObject.SetActive(false);
        if (aimReticle != null) aimReticle.gameObject.SetActive(false);

        // Gọi hàm ném bom nếu nhả ngón tay ngoài vùng hủy
        if (!isHoveringCancel && gunScript != null && aimReticle != null)
        {
            gunScript.ThrowBomb(aimReticle.position);
        }
        
        isHoveringCancel = false;
    }
}
