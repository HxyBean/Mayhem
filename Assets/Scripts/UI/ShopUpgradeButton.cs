using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Gắn vào MỖI dòng chỉ số trong panel Shop (1 dòng = 1 ShopStatType). Bố cục kiểu Subway Surfers: icon + tên
// chỉ số + thanh vạch cấp chia ô + nút mua hiển thị giá (hoặc "Full" khi đã nâng hết).
// Mọi dòng đều nghe GameProgress.OnCurrencyChanged nên mua 1 dòng là CẢ BẢNG tự cập nhật lại khả năng mua.
public class ShopUpgradeButton : MonoBehaviour
{
    [SerializeField] private ShopStatType statType = ShopStatType.MaxHP;

    [Header("Hiển thị (để trống cái nào không cần)")]
    [Tooltip("Tên chỉ số - tự điền theo statType, để trống nếu muốn tự viết tay trong Editor")]
    [SerializeField] private TMP_Text nameText;
    [Tooltip("Cấp hiện tại dạng chữ, VD \"3/5\"")]
    [SerializeField] private TMP_Text levelText;
    [Tooltip("Phần thưởng của mức SẮP mua, VD \"+60 HP\"")]
    [SerializeField] private TMP_Text bonusText;
    [Tooltip("Giá Coin của mức sắp mua; khi đã nâng hết sẽ tự đổi thành \"Full\"")]
    [SerializeField] private TMP_Text costText;
    [Tooltip("Lớp phủ/nhãn phụ hiện khi đã nâng hết 5/5")]
    [SerializeField] private GameObject maxedOverlay;

    [Header("Thanh vạch cấp")]
    [Tooltip("Các ô VÀNG (phần đã nâng) của thanh vạch, xếp theo thứ tự trái→phải, nên có đúng 5 ô = số mức tối đa. " +
             "Khung/nền ô trống cứ để hiện sẵn phía sau, script chỉ bật/tắt mấy ô vàng này.")]
    [SerializeField] private GameObject[] levelSegments;

    [Header("Nút mua")]
    [SerializeField] private Button buyButton;

    private void Awake()
    {
        if (buyButton != null) buyButton.onClick.AddListener(OnBuyClicked);
    }

    private void OnEnable()
    {
        GameProgress.OnCurrencyChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        GameProgress.OnCurrencyChanged -= Refresh;
    }

    private void OnBuyClicked()
    {
        ShopUpgrades.TryBuyUpgrade(statType);

        // BẮT BUỘC tự Refresh ở đây, KHÔNG được trông chờ vào OnCurrencyChanged:
        // 1. Event đó bắn ra ngay LÚC trừ Coin, tức là TRƯỚC khi TryBuyUpgrade kịp ghi cấp mới xuống
        //    PlayerPrefs -> dòng này vẽ lại bằng cấp CŨ, nhìn như bấm mua mà không lên cấp (lệch 1 nhịp).
        // 2. Khi đã full 5/5, TryBuyUpgrade thoát sớm và KHÔNG trừ Coin -> event không hề bắn -> nút không bao
        //    giờ tự đổi sang "Full" cho tới khi thoát ra vào lại Shop.
        // Các dòng KHÁC thì vẫn cập nhật đúng qua event, vì cấp của chúng không đổi, chỉ cần biết Coin mới.
        Refresh();
    }

    public void Refresh()
    {
        int level = GameProgress.GetShopLevel(statType);
        bool isMaxed = level >= ShopUpgrades.MaxLevel;

        if (nameText != null) nameText.text = ShopUpgrades.GetDisplayName(statType);
        if (levelText != null) levelText.text = level + "/" + ShopUpgrades.MaxLevel;
        if (maxedOverlay != null) maxedOverlay.SetActive(isMaxed);

        RefreshLevelSegments(level);

        if (isMaxed)
        {
            if (bonusText != null) bonusText.text = "MAX";
            if (costText != null) costText.text = "Full";
            if (buyButton != null) buyButton.interactable = false;
            return;
        }

        int nextLevel = level + 1;
        int cost = ShopUpgrades.GetCost(nextLevel);

        if (bonusText != null) bonusText.text = ShopUpgrades.FormatBonus(statType, ShopUpgrades.GetBonusAtLevel(statType, nextLevel));
        if (costText != null) costText.text = cost.ToString();
        if (buyButton != null) buyButton.interactable = GameProgress.Coin >= cost;
    }

    // Bật đúng "level" ô đầu tiên, tắt phần còn lại. Duyệt theo độ dài mảng thật (không theo MaxLevel) để
    // không vỡ nếu số ô kéo vào Inspector lỡ khác 5.
    private void RefreshLevelSegments(int level)
    {
        if (levelSegments == null) return;

        for (int i = 0; i < levelSegments.Length; i++)
        {
            if (levelSegments[i] != null) levelSegments[i].SetActive(i < level);
        }
    }
}
