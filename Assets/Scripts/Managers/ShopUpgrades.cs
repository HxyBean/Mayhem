// Các chỉ số nội tại mua được ở Shop bằng Coin. Tên enum được dùng LÀM KHÓA LƯU trong PlayerPrefs
// (xem GameProgress.GetShopLevel) - đổi tên các giá trị này sẽ làm mất cấp đã mua của người chơi.
public enum ShopStatType
{
    MaxHP,
    Damage,
    MoveSpeed,
    LifeSteal,
    Regen
}

// Bảng số liệu + logic mua của Shop Power Up. Để static/hard-code (không dùng ScriptableObject như
// CharacterData/StageData) vì đây là 1 bảng DUY NHẤT dùng chung cho cả game, không thay đổi theo nhân vật hay
// Stage - làm asset sẽ phải kéo tham chiếu vào từng Scene Level mà chẳng được thêm gì. Chỉnh số ngay tại đây.
public static class ShopUpgrades
{
    public const int MaxLevel = 5;

    // Tỉ giá đổi Coin sang Kim cương
    public const int ExchangeCoinCost = 500;
    public const int ExchangeDiamondGain = 1;

    // Giá Coin của từng mức (mức 1 → 5) - tịnh tiến, mức sau đắt hơn mức trước
    private static readonly int[] levelCosts = { 100, 200, 300, 400, 500 };

    // Giá trị CỘNG THÊM tại từng mức (KHÔNG phải tổng tích lũy). VD Máu: mua mức 1 được +20, mua tiếp mức 2
    // được +40 nữa (tổng +60)... mua hết 5 mức là tổng +300.
    private static readonly float[] maxHpBonus = { 20f, 40f, 60f, 80f, 100f };
    private static readonly float[] damageBonus = { 5f, 10f, 10f, 15f, 20f };
    private static readonly float[] moveSpeedBonus = { 0.2f, 0.2f, 0.2f, 0.4f, 1f };
    private static readonly float[] lifeStealBonus = { 0.01f, 0.01f, 0.02f, 0.02f, 0.03f }; // 1%/1%/2%/2%/3%
    private static readonly float[] regenBonus = { 1f, 1f, 1f, 2f, 2f };                    // HP mỗi giây

    // Giá của mức sắp mua (level tính từ 1 tới MaxLevel)
    public static int GetCost(int level)
    {
        if (level < 1 || level > MaxLevel) return 0;
        return levelCosts[level - 1];
    }

    // Giá trị cộng thêm riêng của MỘT mức
    public static float GetBonusAtLevel(ShopStatType stat, int level)
    {
        if (level < 1 || level > MaxLevel) return 0f;
        return GetBonusTable(stat)[level - 1];
    }

    // Tổng giá trị cộng thêm từ mức 1 tới mức người chơi đang có - đây là con số thực sự áp vào Player
    public static float GetTotalBonus(ShopStatType stat)
    {
        float[] table = GetBonusTable(stat);
        int level = GameProgress.GetShopLevel(stat);

        float total = 0f;
        for (int i = 0; i < level && i < table.Length; i++)
        {
            total += table[i];
        }
        return total;
    }

    // Trừ tiền rồi nâng cấp. Trả về false nếu đã full cấp hoặc không đủ Coin (không trừ gì cả).
    public static bool TryBuyUpgrade(ShopStatType stat)
    {
        int currentLevel = GameProgress.GetShopLevel(stat);
        if (currentLevel >= MaxLevel) return false;

        int cost = GetCost(currentLevel + 1);
        if (!GameProgress.TrySpendCoin(cost)) return false;

        GameProgress.SetShopLevel(stat, currentLevel + 1);
        return true;
    }

    public static string GetDisplayName(ShopStatType stat)
    {
        switch (stat)
        {
            case ShopStatType.MaxHP: return "MAX HEALTH";
            case ShopStatType.Damage: return "DAMAGE";
            case ShopStatType.MoveSpeed: return "MOVE SPEED";
            case ShopStatType.LifeSteal: return "LIFE STEAL";
            case ShopStatType.Regen: return "HP REGEN";
        }
        return stat.ToString();
    }

    // Chuỗi hiển thị cho 1 giá trị cộng thêm (Life Steal lưu dạng 0.01 nhưng hiển thị là 1%)
    public static string FormatBonus(ShopStatType stat, float value)
    {
        switch (stat)
        {
            case ShopStatType.MaxHP: return "+" + value.ToString("0") + " HP";
            case ShopStatType.Damage: return "+" + value.ToString("0.##") + " DMG";
            case ShopStatType.MoveSpeed: return "+" + value.ToString("0.##") + " SPD";
            case ShopStatType.LifeSteal: return "+" + (value * 100f).ToString("0.##") + "% LS";
            case ShopStatType.Regen: return "+" + value.ToString("0.##") + " HP/s";
        }
        return value.ToString("0.##");
    }

    private static float[] GetBonusTable(ShopStatType stat)
    {
        switch (stat)
        {
            case ShopStatType.MaxHP: return maxHpBonus;
            case ShopStatType.Damage: return damageBonus;
            case ShopStatType.MoveSpeed: return moveSpeedBonus;
            case ShopStatType.LifeSteal: return lifeStealBonus;
            case ShopStatType.Regen: return regenBonus;
        }
        return maxHpBonus;
    }
}
