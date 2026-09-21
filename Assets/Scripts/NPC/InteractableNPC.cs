using UnityEngine;
using UnityEngine.UI;

// Phần DÙNG CHUNG cho mọi NPC đứng trong màn chơi: đo khoảng cách tới Player để hiện/ẩn nút tương tác, và nối
// sự kiện bấm nút. Kế thừa class này rồi chỉ override OnInteract() - đừng chép lại đoạn dò khoảng cách/nối nút,
// vì riêng việc nối nút đã có sẵn 1 cái bẫy của Unity (xem WireInteractionButton).
//
// Cùng tinh thần với Enemy.cs: base lo phần chung, subclass chỉ viết phần khác biệt.
public abstract class InteractableNPC : MonoBehaviour
{
    [Header("Danh tính")]
    [Tooltip("Ảnh chân dung hiện trong cửa sổ hội thoại. Cửa sổ hội thoại DÙNG CHUNG cho mọi NPC, nên ảnh phải " +
             "do từng NPC truyền vào - để trống thì ô ảnh tự ẩn")]
    [SerializeField] protected Sprite portrait;

    [Header("Tương tác")]
    [SerializeField] protected float interactionRadius = 3f;
    [Tooltip("Nút Interact hiện lên khi Player lại gần. NÊN đặt trong Canvas Screen Space của GameUI rồi gắn " +
             "UIFollowWorldTarget để bám theo NPC - Canvas World Space luôn bị Overlay ăn mất raycast")]
    [SerializeField] protected GameObject interactionButtonObj;

    protected virtual void Awake()
    {
        WireInteractionButton();
    }

    protected void WireInteractionButton()
    {
        if (interactionButtonObj == null) return;

        // PHẢI truyền includeInactive = true. GetComponentInChildren<T>() mặc định BỎ QUA object đang tắt, kể cả
        // chính object gốc - mà nút này bị SetActive(false) ngay bên dưới (và thường cũng đã tắt sẵn trong
        // prefab). Thiếu tham số này thì btn luôn = null, listener không bao giờ được gắn, nên lúc lại gần nút
        // vẫn hiện ra nhưng bấm KHÔNG có phản ứng gì.
        Button btn = interactionButtonObj.GetComponentInChildren<Button>(true);
        if (btn != null)
        {
            btn.onClick.AddListener(OnInteract);
        }

        interactionButtonObj.SetActive(false);
    }

    // Hiện nút khi Player đứng trong bán kính. Truyền allowed = false để ép ẩn (VD NPC đã chuyển sang trạng thái
    // không còn tương tác được nữa).
    protected void UpdateInteractionButton(bool allowed = true)
    {
        if (interactionButtonObj == null) return;

        bool shouldShow = allowed
                          && Player.Instance != null
                          && Vector2.Distance(transform.position, Player.Instance.transform.position) <= interactionRadius;

        if (interactionButtonObj.activeSelf != shouldShow) interactionButtonObj.SetActive(shouldShow);
    }

    // public vì được gắn thẳng vào onClick của Button
    public abstract void OnInteract();

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
