using UnityEngine;
using UnityEngine.UI;

// Game juice cho thanh máu: vệt "máu vừa mất" trôi chậm phía sau + nháy màu khi ăn đòn.
// Gắn vào object chứa thanh máu (cả thanh máu Player trên HUD lẫn thanh máu trên đầu Enemy đều dùng chung).
//
// TỰ QUAN SÁT fillAmount của mainFill mỗi frame thay vì bắt Player/Enemy gọi vào một hàm nào đó. Nhờ vậy
// KHÔNG phải sửa Player.UpdateHPBar()/Enemy.UpdateHPBar(), và gắn được lên BẤT KỲ thanh Filled nào trong
// project (máu, sau này là khiên/stamina...) mà bên kia không cần biết component này tồn tại.
public class HealthBarJuice : MonoBehaviour
{
    [Tooltip("Thanh máu THẬT - cái đã được Player/Enemy set fillAmount. Image Type phải là Filled")]
    [SerializeField] private Image mainFill;
    [Tooltip("Thanh vệt trôi chậm. Đặt NGAY PHÍA SAU mainFill trong Hierarchy (đứng TRÊN trong danh sách con " +
             "= vẽ trước = nằm dưới), màu trắng/đỏ nhạt. Để trống nếu chỉ muốn nháy màu")]
    [SerializeField] private Image trailFill;

    [Header("Vệt trôi")]
    [Tooltip("Đứng yên bấy nhiêu giây rồi vệt mới bắt đầu trôi - chính khoảng đứng yên này làm mắt kịp thấy " +
             "vừa mất bao nhiêu máu")]
    [SerializeField] private float trailDelay = 0.2f;
    [Tooltip("Tốc độ trôi (phần thanh trên mỗi giây). 1 = trôi hết cả thanh trong 1 giây")]
    [SerializeField] private float trailSpeed = 0.3f;

    [Header("Nháy màu")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.05f;

    private Color originalColor;      // Chụp 1 LẦN ở Awake - xem ghi chú cùng vấn đề ở DamageFlash
    private float flashTimer = 0f;
    private float trailDelayTimer = 0f;
    private float lastFill = -1f;     // -1 = chưa có mốc nào, để lần đọc đầu tiên không bị coi là "vừa mất máu"

    private void Awake()
    {
        if (mainFill != null) originalColor = mainFill.color;
    }

    private void OnEnable()
    {
        // Thanh máu Enemy đi theo object được Pool tái sử dụng: phải đồng bộ lại từ đầu, nếu không con quái
        // mới spawn sẽ thừa hưởng vệt máu dở dang của con trước.
        flashTimer = 0f;
        trailDelayTimer = 0f;

        if (mainFill != null)
        {
            mainFill.color = originalColor;
            lastFill = mainFill.fillAmount;
            if (trailFill != null) trailFill.fillAmount = lastFill;
        }
        else
        {
            lastFill = -1f;
        }
    }

    private void Update()
    {
        if (mainFill == null) return;

        float current = mainFill.fillAmount;

        // Chỉ phản ứng khi GIẢM. Hồi máu (tăng) thì kéo vệt lên theo ngay, không có gì để "juice" cả.
        if (lastFill >= 0f && current < lastFill - 0.0001f)
        {
            OnDamaged();
        }
        else if (current > lastFill)
        {
            if (trailFill != null) trailFill.fillAmount = current;
        }

        lastFill = current;

        UpdateFlash();
        UpdateTrail(current);
    }

    private void OnDamaged()
    {
        trailDelayTimer = trailDelay;

        flashTimer = flashDuration;
        mainFill.color = flashColor;
    }

    private void UpdateFlash()
    {
        if (flashTimer <= 0f) return;

        flashTimer -= Time.deltaTime;
        if (flashTimer <= 0f) mainFill.color = originalColor;
    }

    private void UpdateTrail(float current)
    {
        if (trailFill == null) return;

        // Vệt luôn phải >= thanh thật; nhỏ hơn thì kéo lên ngay (VD vừa hồi máu)
        if (trailFill.fillAmount < current)
        {
            trailFill.fillAmount = current;
            return;
        }

        if (trailDelayTimer > 0f)
        {
            trailDelayTimer -= Time.deltaTime;
            return;
        }

        trailFill.fillAmount = Mathf.MoveTowards(trailFill.fillAmount, current, trailSpeed * Time.deltaTime);
    }
}
