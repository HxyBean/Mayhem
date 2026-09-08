using UnityEngine;

// Gắn vào MỌI vật phẩm có thể bị hút (EXP nhỏ/to/boss, Energy, Trái tim).
// Tự động di chuyển về phía Player khi ở trong bán kính Hút Item hiện tại (lõi Magnet, Player.GetMagnetRadius()),
// hoặc bị hút cưỡng bức bất kể khoảng cách (ForceMagnetPull - dùng cho viên EXP Boss hút hết EXP trên bản đồ).
public class Pickup : MonoBehaviour
{
    [SerializeField] private float magnetSpeed = 8f;
    private bool isForcePulled = false;

    private void OnEnable()
    {
        // Reset lại vì object được tái sử dụng từ Pool, không được giữ trạng thái hút cưỡng bức của lần trước
        isForcePulled = false;
    }

    private void Update()
    {
        if (Player.Instance == null) return;

        Vector3 playerPos = Player.Instance.transform.position;

        if (isForcePulled)
        {
            transform.position = Vector3.MoveTowards(transform.position, playerPos, magnetSpeed * Time.deltaTime);
            return;
        }

        float magnetRadius = Player.Instance.GetMagnetRadius();
        if (magnetRadius <= 0f) return;

        float sqrDist = (playerPos - transform.position).sqrMagnitude;
        if (sqrDist <= magnetRadius * magnetRadius)
        {
            transform.position = Vector3.MoveTowards(transform.position, playerPos, magnetSpeed * Time.deltaTime);
        }
    }

    // Gọi khi nhặt viên EXP Boss - hút về phía Player bất kể có đang trong bán kính Magnet augment hay không
    public void ForceMagnetPull()
    {
        isForcePulled = true;
    }
}
