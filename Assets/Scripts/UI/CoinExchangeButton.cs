using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ExchangeDirection
{
    CoinToDiamond, // Trả Coin, nhận Kim cương
    DiamondToCoin  // Trả Kim cương, nhận Coin
}

// Ô đổi tiền tệ trong panel Shop, dùng chung cho CẢ 2 CHIỀU - đặt 2 instance, mỗi cái set 1 Direction.
// Người chơi chọn SỐ LƯỢNG KIM CƯƠNG của giao dịch bằng 3 nút (+ / - / Max); kim cương luôn là đơn vị đếm dù
// đổi chiều nào, chỉ khác bên nào trả bên nào nhận. 2 dòng text cho biết giao dịch gồm bao nhiêu Coin và bao
// nhiêu Kim cương, rồi bấm nút Đổi để chốt.
public class CoinExchangeButton : MonoBehaviour
{
    [Tooltip("Chiều đổi: trả Coin lấy Kim cương, hay trả Kim cương lấy Coin")]
    [SerializeField] private ExchangeDirection direction = ExchangeDirection.CoinToDiamond;

    [Tooltip("Tỉ giá: bao nhiêu Coin ăn 1 Kim cương. Muốn chiều Kim cương → Coin thiệt hơn (chống đổi qua đổi " +
             "lại để kiếm lời) thì để số nhỏ hơn ở ô đổi chiều đó")]
    [SerializeField] private int coinPerDiamond = ShopUpgrades.ExchangeCoinCost;

    [Header("Hiển thị")]
    [Tooltip("Lượng Coin của giao dịch - bên TRẢ nếu đổi Coin→Kim cương, bên NHẬN nếu đổi ngược lại")]
    [SerializeField] private TMP_Text coinCostText;
    [Tooltip("Lượng Kim cương của giao dịch - bên NHẬN nếu đổi Coin→Kim cương, bên TRẢ nếu đổi ngược lại")]
    [SerializeField] private TMP_Text diamondAmountText;

    [Header("Nút")]
    [SerializeField] private Button plusButton;
    [SerializeField] private Button minusButton;
    [Tooltip("Chọn thẳng số lượng tối đa đổi được với số tiền đang có")]
    [SerializeField] private Button maxButton;
    [Tooltip("Nút chốt giao dịch")]
    [SerializeField] private Button exchangeButton;

    // Số kim cương đang định đổi. Luôn tối thiểu là 1 để 2 dòng text còn hiển thị được tỉ giá dù chưa đủ tiền.
    private int selectedDiamond = 1;

    private void Awake()
    {
        if (plusButton != null) plusButton.onClick.AddListener(OnPlusClicked);
        if (minusButton != null) minusButton.onClick.AddListener(OnMinusClicked);
        if (maxButton != null) maxButton.onClick.AddListener(OnMaxClicked);
        if (exchangeButton != null) exchangeButton.onClick.AddListener(OnExchangeClicked);
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

    // Số kim cương nhiều nhất giao dịch được với ví hiện tại
    private int GetMaxAffordableDiamond()
    {
        if (coinPerDiamond <= 0) return 0;

        // Đổi xuôi thì bị giới hạn bởi số Coin đang có; đổi ngược thì giới hạn bởi chính số Kim cương đang có
        return direction == ExchangeDirection.CoinToDiamond
            ? GameProgress.Coin / coinPerDiamond
            : GameProgress.Diamond;
    }

    private void OnPlusClicked()
    {
        selectedDiamond++;
        Refresh(); // Refresh tự kẹp lại trong khoảng hợp lệ
    }

    private void OnMinusClicked()
    {
        selectedDiamond--;
        Refresh();
    }

    private void OnMaxClicked()
    {
        selectedDiamond = GetMaxAffordableDiamond();
        Refresh();
    }

    private void OnExchangeClicked()
    {
        // Đổi xong ví thay đổi -> OnCurrencyChanged bắn ra -> Refresh tự kẹp lại số đang chọn theo ví mới
        int coinAmount = selectedDiamond * coinPerDiamond;

        if (direction == ExchangeDirection.CoinToDiamond)
        {
            GameProgress.TryExchangeCoinForDiamond(coinAmount, selectedDiamond);
            return;
        }

        GameProgress.TryExchangeDiamondForCoin(selectedDiamond, coinAmount);
    }

    public void Refresh()
    {
        int maxAffordable = GetMaxAffordableDiamond();
        selectedDiamond = Mathf.Clamp(selectedDiamond, 1, Mathf.Max(1, maxAffordable));

        int coinAmount = selectedDiamond * coinPerDiamond;

        if (coinCostText != null) coinCostText.text = coinAmount.ToString();
        if (diamondAmountText != null) diamondAmountText.text = selectedDiamond.ToString();

        // Hết chỗ tăng (hoặc chưa đủ tiền đổi nổi 1 viên) thì làm mờ cả nút + lẫn nút Max
        bool canIncrease = selectedDiamond < maxAffordable;
        if (plusButton != null) plusButton.interactable = canIncrease;
        if (maxButton != null) maxButton.interactable = canIncrease;
        if (minusButton != null) minusButton.interactable = selectedDiamond > 1;
        if (exchangeButton != null) exchangeButton.interactable = selectedDiamond <= maxAffordable;
    }
}
