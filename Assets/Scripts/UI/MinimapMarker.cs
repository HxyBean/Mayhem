using System.Collections.Generic;
using UnityEngine;

// Gắn vào BẤT KỲ object nào muốn hiện lên minimap (NPC, sau này có thể là Boss, rương đồ, USBEnemy...).
// Tự ghi danh vào danh sách tĩnh lúc bật, gỡ ra lúc tắt - MinimapUI chỉ việc đọc danh sách này.
//
// Dùng danh sách TĨNH thay vì gọi MinimapUI.Instance.Register(): Unity KHÔNG đảm bảo thứ tự Awake/OnEnable
// giữa các MonoBehaviour, nên marker rất dễ bật TRƯỚC khi MinimapUI kịp tồn tại. Danh sách tĩnh thì không phụ
// thuộc thứ tự, và cũng tự đúng với object được tái sử dụng qua Pool (OnEnable/OnDisable chạy mỗi lần).
public class MinimapMarker : MonoBehaviour
{
    private static readonly List<MinimapMarker> active = new List<MinimapMarker>();
    public static IReadOnlyList<MinimapMarker> Active => active;

    [Tooltip("Màu riêng của mốc này trên minimap - mỗi NPC một màu để phân biệt")]
    [SerializeField] private Color markerColor = Color.white;
    [Tooltip("Icon riêng. Để trống = dùng icon mặc định gán trên MinimapUI")]
    [SerializeField] private Sprite iconOverride;

    public Color MarkerColor => markerColor;
    public Sprite IconOverride => iconOverride;

    // Biến static KHÔNG tự reset khi bấm Play nếu project bật "Enter Play Mode Options" (tắt Domain Reload).
    // Thiếu dòng này thì lần Play thứ 2 trở đi danh sách còn sót marker của lần chạy trước -> minimap hiện
    // mốc ma ở vị trí lung tung.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        active.Clear();
    }

    private void OnEnable()
    {
        if (!active.Contains(this)) active.Add(this);
    }

    private void OnDisable()
    {
        active.Remove(this);
    }
}
