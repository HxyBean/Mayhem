using UnityEngine;
using UnityEngine.EventSystems;

// Lớp cơ sở dùng chung cho các nút kéo-thả chọn vị trí trên bản đồ (Bomb, Blink...).
// Aim luôn xuất phát từ vị trí nhân vật, di chuyển theo đúng độ lệch ngón tay đã kéo so với lúc mới nhấn
// (không bám theo vị trí tuyệt đối của ngón tay, tránh bị kẹt ngay tại chỗ nút bấm).
public abstract class DragAimButton : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] protected Transform aimReticle;
    [SerializeField] protected RectTransform cancelZone;
    [Tooltip("0 = không giới hạn tầm kéo. > 0: giới hạn khoảng cách tối đa từ vị trí nhân vật")]
    [SerializeField] protected float maxRange = 0f;

    private bool isDragging = false;
    private bool isHoveringCancel = false;
    private Vector2 pressScreenPos;

    protected virtual void Awake()
    {
        if (aimReticle != null) aimReticle.gameObject.SetActive(false);
        if (cancelZone != null) cancelZone.gameObject.SetActive(false);
    }

    // Điều kiện cho phép bắt đầu kéo (VD: còn ammo để ném bom, Blink đã hết cooldown...)
    protected abstract bool CanStartDrag();

    // Gọi khi thả tay ngoài vùng hủy, với vị trí world cuối cùng của aim reticle
    protected abstract void OnConfirm(Vector3 targetPosition);

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanStartDrag()) return;

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
        if (aimReticle != null && Camera.main != null && Player.Instance != null)
        {
            Vector3 pressWorldPos = Camera.main.ScreenToWorldPoint(pressScreenPos);
            Vector3 currentWorldPos = Camera.main.ScreenToWorldPoint(eventData.position);
            Vector3 worldDelta = currentWorldPos - pressWorldPos;
            worldDelta.z = 0f;

            if (maxRange > 0f)
            {
                worldDelta = Vector3.ClampMagnitude(worldDelta, maxRange);
            }

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

        // Xác nhận nếu nhả ngón tay ngoài vùng hủy
        if (!isHoveringCancel && aimReticle != null)
        {
            OnConfirm(aimReticle.position);
        }

        isHoveringCancel = false;
    }
}
