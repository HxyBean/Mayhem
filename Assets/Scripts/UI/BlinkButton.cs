using UnityEngine;

// Nút kéo-thả chọn điểm Blink cho Pháp sư. Đặt maxRange (kế thừa từ DragAimButton) = 5 trong Inspector.
public class BlinkButton : DragAimButton
{
    protected override bool CanStartDrag()
    {
        return Player.Instance != null && Player.Instance.CanBlink();
    }

    protected override void OnConfirm(Vector3 targetPosition)
    {
        if (Player.Instance != null) Player.Instance.Blink(targetPosition);
    }
}
