using UnityEngine;

// Lưu tiến trình mở khóa Level, tiền tệ (Coin/Kim cương) và danh sách nhân vật đã mua. Sống xuyên suốt qua các
// lần LoadScene (và cả khi tắt/mở lại game nhờ PlayerPrefs), không gắn vào GameObject/Scene nào nên không cần
// DontDestroyOnLoad.
public static class GameProgress
{
    private const string UnlockedStageCountKey = "Mayhem_UnlockedStageCount";
    private const string CoinKey = "Mayhem_Coin";
    private const string DiamondKey = "Mayhem_Diamond";

    // Danh sách nhân vật đã mở khóa gom vào ĐÚNG 1 key dạng CSV ("Mage,Knight") thay vì mỗi nhân vật 1 key
    // riêng: PlayerPrefs không liệt kê được key đang có, nên nhiều key riêng sẽ không thể xóa sạch khi bấm
    // New Game. Một key duy nhất thì chỉ cần DeleteKey là reset trọn vẹn.
    private const string UnlockedCharactersKey = "Mayhem_UnlockedCharacters";

    // Tương tự, danh sách stageIndex đã nhận kim cương ("1,2") - chặn farm lại kim cương ở Stage đã lấy rồi.
    private const string DiamondClaimedStagesKey = "Mayhem_DiamondClaimedStages";

    // Bắn ra mỗi khi Coin/Kim cương thay đổi để UI (CurrencyUI) tự cập nhật, không phải kiểm tra lại mỗi frame.
    public static event System.Action OnCurrencyChanged;

    // StageData vừa được chọn ở Stage Select, dùng để Scene Level mới load lên biết cộng augment nào.
    // Chỉ tồn tại trong phiên chơi hiện tại, không cần lưu PlayerPrefs.
    public static StageData SelectedStage { get; set; }

    // CharacterData vừa được chọn ở Character Select (chọn lại mỗi lần vào 1 Level). Cũng chỉ tồn tại trong phiên hiện tại.
    public static CharacterData SelectedCharacter { get; set; }

    // ==============================================
    // TIẾN TRÌNH MỞ KHÓA LEVEL
    // ==============================================
    // Số lượng Stage đã mở khóa, tính từ Stage 1. Mặc định luôn mở sẵn Stage 1.
    public static int UnlockedStageCount
    {
        get => Mathf.Max(1, PlayerPrefs.GetInt(UnlockedStageCountKey, 1));
        private set
        {
            PlayerPrefs.SetInt(UnlockedStageCountKey, value);
            PlayerPrefs.Save();
        }
    }

    public static bool IsStageUnlocked(int stageIndex)
    {
        return stageIndex <= UnlockedStageCount;
    }

    // Stage đã từng phá đảo hay chưa (mở khóa được Stage kế tiếp nghĩa là Stage này đã thắng ít nhất 1 lần)
    public static bool IsStageCompleted(int stageIndex)
    {
        return UnlockedStageCount > stageIndex;
    }

    // Gọi khi người chơi thắng 1 Stage (đủ điều kiện WinGame) để mở khóa Stage kế tiếp.
    public static void CompleteStage(int stageIndex)
    {
        if (stageIndex + 1 > UnlockedStageCount)
        {
            UnlockedStageCount = stageIndex + 1;
        }
    }

    // ==============================================
    // TIỀN TỆ (COIN / KIM CƯƠNG)
    // ==============================================
    // LƯU Ý: setter chỉ SetInt (ghi vào bộ nhớ), KHÔNG gọi PlayerPrefs.Save() - Coin được cộng liên tục mỗi lần
    // giết quái, mà Save() là thao tác ghi đĩa nên gọi mỗi lần nhặt coin sẽ gây giật trên điện thoại. Việc ghi
    // xuống đĩa thật được gom lại ở các mốc an toàn qua SaveNow() (thắng/thua/về menu/mua nhân vật), cộng thêm
    // Unity tự flush PlayerPrefs khi thoát game hoặc app chuyển xuống nền.
    public static int Coin
    {
        get => PlayerPrefs.GetInt(CoinKey, 0);
        private set
        {
            PlayerPrefs.SetInt(CoinKey, Mathf.Max(0, value));
            OnCurrencyChanged?.Invoke();
        }
    }

    public static int Diamond
    {
        get => PlayerPrefs.GetInt(DiamondKey, 0);
        private set
        {
            PlayerPrefs.SetInt(DiamondKey, Mathf.Max(0, value));
            OnCurrencyChanged?.Invoke();
        }
    }

    public static void AddCoin(int amount)
    {
        if (amount <= 0) return;
        Coin += amount;
    }

    public static void AddDiamond(int amount)
    {
        if (amount <= 0) return;
        Diamond += amount;
    }

    // Trả về false nếu không đủ tiền (không trừ gì cả) - nơi gọi tự quyết định hiển thị thông báo.
    public static bool TrySpendCoin(int amount)
    {
        if (amount <= 0 || Coin < amount) return false;

        Coin -= amount;
        SaveNow();
        return true;
    }

    public static bool TrySpendDiamond(int amount)
    {
        if (amount <= 0 || Diamond < amount) return false;

        Diamond -= amount;
        SaveNow();
        return true;
    }

    // Ghi thật xuống đĩa. Gọi ở các mốc an toàn (kết thúc ván, về menu, giao dịch mua bán).
    public static void SaveNow()
    {
        PlayerPrefs.Save();
    }

    // ==============================================
    // MỞ KHÓA NHÂN VẬT
    // ==============================================
    public static bool IsCharacterUnlocked(CharacterData character)
    {
        if (character == null) return false;
        if (character.unlockedByDefault) return true;

        return CsvContains(UnlockedCharactersKey, character.characterName);
    }

    public static void UnlockCharacter(CharacterData character)
    {
        if (character == null || character.unlockedByDefault) return;

        CsvAdd(UnlockedCharactersKey, character.characterName);
        SaveNow();
    }

    // ==============================================
    // KIM CƯƠNG THEO STAGE (mỗi Stage chỉ cho nhận 1 lần duy nhất)
    // ==============================================
    public static bool IsStageDiamondClaimed(int stageIndex)
    {
        return CsvContains(DiamondClaimedStagesKey, stageIndex.ToString());
    }

    public static void MarkStageDiamondClaimed(int stageIndex)
    {
        CsvAdd(DiamondClaimedStagesKey, stageIndex.ToString());
        SaveNow();
    }

    // ==============================================
    // NEW GAME
    // ==============================================
    // Gọi khi người chơi xác nhận "New Game": khóa lại toàn bộ Stage (chỉ giữ Stage 1), xóa sạch tiền tệ,
    // khóa lại các nhân vật đã mua và cho phép nhận lại kim cương từ đầu.
    public static void ResetProgress()
    {
        UnlockedStageCount = 1;
        SelectedStage = null;
        SelectedCharacter = null;

        Coin = 0;
        Diamond = 0;
        PlayerPrefs.DeleteKey(UnlockedCharactersKey);
        PlayerPrefs.DeleteKey(DiamondClaimedStagesKey);
        SaveNow();
    }

    // ==============================================
    // HELPER: danh sách CSV lưu trong 1 key PlayerPrefs
    // ==============================================
    private static bool CsvContains(string key, string value)
    {
        if (string.IsNullOrEmpty(value)) return false;

        string raw = PlayerPrefs.GetString(key, "");
        if (string.IsNullOrEmpty(raw)) return false;

        foreach (string entry in raw.Split(','))
        {
            if (entry == value) return true;
        }
        return false;
    }

    private static void CsvAdd(string key, string value)
    {
        if (string.IsNullOrEmpty(value) || CsvContains(key, value)) return;

        string raw = PlayerPrefs.GetString(key, "");
        PlayerPrefs.SetString(key, string.IsNullOrEmpty(raw) ? value : raw + "," + value);
    }

#if UNITY_EDITOR
    // Tiện ích debug: mở khóa hết tất cả Stage khi test trong Editor.
    [UnityEditor.MenuItem("Mayhem/Debug/Unlock All Stages")]
    private static void UnlockAllStages_Editor()
    {
        UnlockedStageCount = 99;
    }

    [UnityEditor.MenuItem("Mayhem/Debug/Reset Stage Progress")]
    private static void ResetProgress_Editor()
    {
        ResetProgress();
    }

    // Tiện ích debug: nạp sẵn tiền để test mua nhân vật mà không phải cày.
    [UnityEditor.MenuItem("Mayhem/Debug/Add 1000 Coin + 50 Diamond")]
    private static void AddCurrency_Editor()
    {
        AddCoin(1000);
        AddDiamond(50);
        SaveNow();
    }
#endif
}
