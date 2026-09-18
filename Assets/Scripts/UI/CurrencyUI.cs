using TMPro;
using UnityEngine;

public enum CurrencyDisplaySource
{
    Total,   // Tổng đang có (đã lưu) - dùng ở Main Menu / Character Select
    ThisRun  // Số nhặt được trong ván đang chơi - dùng ở HUD trong game / Win / Game Over
}

// Hiển thị số Coin / Kim cương. Tự cập nhật qua event (GameProgress.OnCurrencyChanged cho tổng đã lưu,
// GameManager.OnRunCurrencyChanged cho số nhặt trong ván) nên đặt được ở bất kỳ panel/HUD nào mà không cần
// ai gọi Refresh thủ công.
public class CurrencyUI : MonoBehaviour
{
    [Tooltip("Total = tổng đang có (menu). This Run = số nhặt được trong ván đang chơi (HUD/Win/Game Over)")]
    [SerializeField] private CurrencyDisplaySource source = CurrencyDisplaySource.Total;
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text diamondText;

    private void OnEnable()
    {
        if (source == CurrencyDisplaySource.Total) GameProgress.OnCurrencyChanged += Refresh;
        else GameManager.OnRunCurrencyChanged += Refresh;

        // Panel Win/Game Over chỉ bật lên lúc kết thúc ván, OnEnable lúc đó mới đọc đúng số liệu cuối cùng
        Refresh();
    }

    private void OnDisable()
    {
        if (source == CurrencyDisplaySource.Total) GameProgress.OnCurrencyChanged -= Refresh;
        else GameManager.OnRunCurrencyChanged -= Refresh;
    }

    public void Refresh()
    {
        int coin;
        int diamond;

        if (source == CurrencyDisplaySource.Total)
        {
            coin = GameProgress.Coin;
            diamond = GameProgress.Diamond;
        }
        else
        {
            coin = (GameManager.Instance != null) ? GameManager.Instance.RunCoin : 0;
            diamond = (GameManager.Instance != null) ? GameManager.Instance.RunDiamond : 0;
        }

        if (coinText != null) coinText.text = ":" + coin.ToString();
        if (diamondText != null) diamondText.text = ":" + diamond.ToString();
    }
}
