using UnityEngine;

public enum CurrencyType
{
    Coin,
    Diamond
}

// Gắn vào prefab Coin / Kim cương. PlayerCollision gọi Collect() khi Player chạm vào vật phẩm.
// Gắn kèm Pickup.cs nếu muốn vật phẩm này cũng bị hút theo lõi Magnet giống EXP/Energy/Trái tim.
public class CurrencyPickup : MonoBehaviour
{
    [SerializeField] private CurrencyType currencyType = CurrencyType.Coin;
    [Tooltip("Số tiền cộng vào khi nhặt (mỗi quái thường rơi 1 coin, Boss rơi kim cương)")]
    [SerializeField] private int amount = 1;

    // Cộng vào "ví tạm" của ván đang chơi (GameManager), KHÔNG cộng thẳng vào tổng đã lưu: người chơi phải
    // chơi hết ván (thắng hoặc chết) mới được cộng vào tổng, thoát giữa chừng là mất sạch.
    public void Collect()
    {
        if (GameManager.Instance == null) return;

        if (currencyType == CurrencyType.Coin)
        {
            GameManager.Instance.AddRunCoin(amount);
            return;
        }

        GameManager.Instance.AddRunDiamond(amount);
    }
}
