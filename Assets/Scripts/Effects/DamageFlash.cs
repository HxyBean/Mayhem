using UnityEngine;

public enum DamageFlashMode
{
    // Đổi SpriteRenderer.color. KHÔNG cần setup gì, nhưng vì color là màu NHÂN nên chỉ làm TỐI đi hoặc ngả màu
    // được (đỏ/cam), KHÔNG BAO GIỜ làm sáng lên - để trắng là không thấy gì.
    Tint,
    // Nháy SÁNG thật (trắng xóa). Bắt buộc SpriteRenderer phải dùng material có shader "Mayhem/SpriteFlash".
    Brighten
}

// Nháy màu sprite khi ăn sát thương. DÙNG CHUNG cho cả Player lẫn mọi loại Enemy - gắn component này vào
// object rồi gọi Flash(), không viết riêng cho từng bên.
//
// KHÔNG dùng Coroutine mà đếm giờ trong Update: quái chết giữa lúc đang nháy sẽ bị trả về Pool ->
// SetActive(false) -> coroutine bị giết ngang, màu không kịp trả lại, và con quái đó lần sau spawn ra vẫn
// đang đỏ lòm. Đếm giờ + khôi phục màu trong OnEnable/OnDisable thì không có kẽ hở đó.
public class DamageFlash : MonoBehaviour
{
    [Tooltip("Tint = đổi SpriteRenderer.color, không cần setup nhưng CHỈ ngả màu được (đỏ/cam), không sáng lên " +
             "được. Brighten = nháy sáng thật, nhưng SpriteRenderer phải dùng material shader Mayhem/SpriteFlash")]
    [SerializeField] private DamageFlashMode mode = DamageFlashMode.Tint;
    [Tooltip("Màu nháy. Ở chế độ Tint để trắng (1,1,1) sẽ KHÔNG thấy gì - dùng đỏ/cam. Ở chế độ Brighten thì " +
             "trắng mới là lựa chọn đúng")]
    [SerializeField] private Color flashColor = new Color(1f, 0.3f, 0.3f, 1f);
    [Tooltip("Độ mạnh lúc nháy ở chế độ Brighten. 1 = trắng xóa hoàn toàn, 0.7 còn thấy lờ mờ hình gốc")]
    [SerializeField, Range(0f, 1f)] private float flashStrength = 1f;
    [Tooltip("Thời gian nháy (giây). 0.08-0.15 là khoảng cho cảm giác 'đánh trúng' rõ mà không rối mắt")]
    [SerializeField] private float flashDuration = 0.1f;
    [Tooltip("Để trống = tự lấy SpriteRenderer trên chính object này. Điền tay nếu muốn nháy thêm các sprite con")]
    [SerializeField] private SpriteRenderer[] targets;

    // Màu GỐC, chụp đúng 1 LẦN lúc Awake (chỉ dùng cho chế độ Tint).
    // ĐỪNG chụp lại ở đầu mỗi lần Flash(): trúng 2 phát đạn liền nhau thì lần thứ 2 sẽ chụp nhầm màu ĐANG
    // nháy làm "màu gốc", và sprite kẹt màu đỏ vĩnh viễn.
    private Color[] originalColors;
    private float flashTimer = 0f;

    // MaterialPropertyBlock cho chế độ Brighten. BẮT BUỘC dùng cái này thay vì đụng vào renderer.material:
    // đọc .material sẽ khiến Unity NHÂN BẢN material cho riêng renderer đó - mỗi con quái một bản sao, vừa rò rỉ
    // bộ nhớ theo số lần spawn từ Pool, vừa phá batching. MaterialPropertyBlock đổi được giá trị cho từng
    // renderer mà vẫn dùng chung đúng 1 material.
    private MaterialPropertyBlock propertyBlock;
    private static readonly int FlashColorID = Shader.PropertyToID("_FlashColor");
    private static readonly int FlashAmountID = Shader.PropertyToID("_FlashAmount");

    private void Awake()
    {
        if (targets == null || targets.Length == 0)
        {
            SpriteRenderer self = GetComponent<SpriteRenderer>();
            targets = (self != null) ? new SpriteRenderer[] { self } : new SpriteRenderer[0];
        }

        originalColors = new Color[targets.Length];
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null) originalColors[i] = targets[i].color;
        }

        if (mode == DamageFlashMode.Brighten) propertyBlock = new MaterialPropertyBlock();
    }

    // Object tái sử dụng qua Pool: con quái trước có thể đã bị tắt giữa lúc đang nháy
    private void OnEnable()
    {
        flashTimer = 0f;
        StopFlash();
    }

    private void OnDisable()
    {
        flashTimer = 0f;
        StopFlash();
    }

    public void Flash()
    {
        if (targets == null || targets.Length == 0) return;

        flashTimer = flashDuration;

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null) continue;

            if (mode == DamageFlashMode.Tint)
            {
                targets[i].color = flashColor;
            }
            else
            {
                SetFlashAmount(targets[i], flashStrength);
            }
        }
    }

    private void Update()
    {
        if (flashTimer <= 0f) return;

        flashTimer -= Time.deltaTime;
        if (flashTimer <= 0f) StopFlash();
    }

    private void StopFlash()
    {
        if (targets == null) return;

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null) continue;

            if (mode == DamageFlashMode.Tint)
            {
                if (originalColors != null && i < originalColors.Length) targets[i].color = originalColors[i];
            }
            else
            {
                SetFlashAmount(targets[i], 0f);
            }
        }
    }

    private void SetFlashAmount(SpriteRenderer renderer, float amount)
    {
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

        // Phải GetPropertyBlock trước khi sửa, nếu không sẽ xóa sạch các property khác ai đó đã set trên
        // renderer này (VD Sprite Skin, hiệu ứng khác).
        renderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(FlashColorID, flashColor);
        propertyBlock.SetFloat(FlashAmountID, amount);
        renderer.SetPropertyBlock(propertyBlock);
    }
}
