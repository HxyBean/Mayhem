using UnityEngine;

// Nút bắn Laser của Robot: giữ nút và kéo để chọn HƯỚNG bắn, thả tay là bắn.
// Khác Bomb/Potion/Blink ở chỗ chúng chọn 1 ĐIỂM trên bản đồ, còn Laser là tia bắn thẳng nên chỉ cần hướng.
// Vì vậy mũi tên aim đứng yên ngay tại nhân vật và chỉ XOAY theo hướng kéo, thay vì chạy ra xa theo ngón tay.
public class LaserButton : DragAimButton
{
    [SerializeField] private Gun gunScript;

    // Kéo quá ngắn thì không xác định nổi hướng - coi như chạm nhầm, không bắn (và không mất charge)
    private const float MinDragToFire = 0.2f;

    protected override bool CanStartDrag()
    {
        return gunScript != null && gunScript.CanFireLaser();
    }

    protected override void ApplyAimVisual(Vector3 targetPosition, Vector3 worldDelta)
    {
        if (Player.Instance != null) aimReticle.position = Player.Instance.transform.position;

        if (worldDelta.sqrMagnitude < 0.0001f) return;

        float angle = Mathf.Atan2(worldDelta.y, worldDelta.x) * Mathf.Rad2Deg;
        aimReticle.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    protected override void OnConfirm(Vector3 targetPosition)
    {
        if (gunScript == null || Player.Instance == null) return;

        Vector3 direction = targetPosition - Player.Instance.transform.position;
        direction.z = 0f;
        if (direction.magnitude < MinDragToFire) return;

        gunScript.FireLaser(direction);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLaserSound();
        }
    }
}
