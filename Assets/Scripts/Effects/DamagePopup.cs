using TMPro;
using UnityEngine;

// Con số sát thương bay lên rồi mờ dần. Gắn vào prefab có sẵn 1 TextMeshPro (loại 3D/world space, KHÔNG phải
// TextMeshProUGUI trong Canvas) - số phải nằm trong world space thì mới bám đúng vị trí con quái.
//
// Dùng TMP_Text (lớp cha) chứ không phải TextMeshPro cụ thể, để prefab dùng kiểu nào cũng chạy.
public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [Tooltip("Thời gian tồn tại (giây)")]
    [SerializeField] private float lifetime = 0.7f;
    [Tooltip("Tốc độ bay lên (đơn vị world/giây)")]
    [SerializeField] private float floatSpeed = 1.5f;
    [Tooltip("Bắt đầu mờ dần khi còn lại bao nhiêu phần thời gian. 0.5 = nửa sau mới mờ")]
    [SerializeField, Range(0f, 1f)] private float fadeStartPercent = 0.5f;

    private float timer;
    private Color baseColor;

    private void Awake()
    {
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);
        if (label == null) return;

        // ÉP 2 thứ này ngay trong code thay vì trông chờ prefab set đúng - cả 2 đều là mặc định SAI của
        // TextMeshPro world-space và đều gây lỗi nhìn rất khó đoán nguyên nhân:
        //
        // 1) Căn lề: object TextMeshPro 3D mặc định có RectTransform rộng 20 đơn vị và căn Top-Left, nên chữ
        //    bị vẽ ở MÉP TRÁI khung -> con số hiện lệch cả chục đơn vị sang trái so với chỗ spawn, trông như
        //    tính sai vị trí trong khi vị trí hoàn toàn đúng.
        // 2) Color Gradient: TMP.color là màu NHÂN với gradient đỉnh. Prefab bật gradient (nhất là gradient
        //    tối) sẽ nuốt sạch màu do code set -> con số luôn ra đen.
        label.alignment = TextAlignmentOptions.Center;
        label.enableVertexGradient = false;

        // Pivot ở giữa thì khung chữ mới nằm cân quanh vị trí spawn (cùng lý do với neo của MinimapUI)
        RectTransform rect = label.rectTransform;
        if (rect != null) rect.pivot = new Vector2(0.5f, 0.5f);

        // 3) Nếu TextMeshPro nằm ở object CON, kéo nó về đúng tâm object gốc. Con lệch sẵn trong prefab là
        //    con số hiện lệch một khoảng CỐ ĐỊNH so với chỗ spawn - dấu hiệu nhận biết: các số lệch đều nhau
        //    về cùng một phía thay vì tản đều quanh mục tiêu.
        if (label.transform != transform) label.transform.localPosition = Vector3.zero;
    }

    // Gọi NGAY SAU khi spawn. Không đặt trong OnEnable vì lúc đó chưa biết số/màu cần hiện.
    public void Setup(float amount, Color color)
    {
        if (label == null) return;

        // Sát thương lẻ (VD tick DOT 20% của 10 dmg = 2, hoặc 0.4) làm tròn về 0 nhìn như đánh hụt.
        // Luôn hiện tối thiểu 1 khi thực sự có gây damage.
        int shown = Mathf.Max(1, Mathf.RoundToInt(amount));
        label.text = shown.ToString();

        baseColor = color;
        label.color = color;
    }

    private void OnEnable()
    {
        // Object tái sử dụng qua Pool: reset lại bộ đếm, nếu không con số mới sẽ biến mất ngay lập tức
        timer = 0f;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        if (label != null && lifetime > 0f)
        {
            float progress = timer / lifetime;
            float alpha = (progress < fadeStartPercent)
                ? 1f
                : 1f - Mathf.InverseLerp(fadeStartPercent, 1f, progress);

            label.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }

        if (timer >= lifetime) ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (ObjectPoolManager.Instance != null) ObjectPoolManager.Instance.ReturnObjectToPool(gameObject);
        else Destroy(gameObject);
    }
}
