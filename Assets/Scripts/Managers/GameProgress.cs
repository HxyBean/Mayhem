using UnityEngine;

// Lưu tiến trình mở khóa Level, sống xuyên suốt qua các lần LoadScene (và cả khi tắt/mở lại game
// nhờ PlayerPrefs), không gắn vào GameObject/Scene nào nên không cần DontDestroyOnLoad.
public static class GameProgress
{
    private const string UnlockedStageCountKey = "Mayhem_UnlockedStageCount";

    // StageData vừa được chọn ở Stage Select, dùng để Scene Level mới load lên biết cộng augment nào.
    // Chỉ tồn tại trong phiên chơi hiện tại, không cần lưu PlayerPrefs.
    public static StageData SelectedStage { get; set; }

    // CharacterData vừa được chọn ở Character Select (chọn lại mỗi lần vào 1 Level). Cũng chỉ tồn tại trong phiên hiện tại.
    public static CharacterData SelectedCharacter { get; set; }

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

    // Gọi khi người chơi thắng 1 Stage (đủ điều kiện WinGame) để mở khóa Stage kế tiếp.
    public static void CompleteStage(int stageIndex)
    {
        if (stageIndex + 1 > UnlockedStageCount)
        {
            UnlockedStageCount = stageIndex + 1;
        }
    }

    // Gọi khi người chơi xác nhận "New Game": khóa lại toàn bộ Stage, chỉ giữ Stage 1 mở sẵn.
    public static void ResetProgress()
    {
        UnlockedStageCount = 1;
        SelectedStage = null;
        SelectedCharacter = null;
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
#endif
}
