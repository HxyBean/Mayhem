using System.Collections.Generic;
using UnityEngine;

// Cấu hình riêng cho từng level chọn ở màn hình Stage Select.
// Cơ chế gameplay dùng chung cho mọi level; chỉ có augmentPool được cộng thêm augment riêng theo từng Stage.
[CreateAssetMenu(fileName = "StageData", menuName = "Mayhem/Stage Data")]
public class StageData : ScriptableObject
{
    [Tooltip("Số thứ tự Stage (bắt đầu từ 1), dùng để kiểm tra mở khóa/hoàn thành. Stage 1 luôn mở sẵn.")]
    public int stageIndex = 1;

    public string stageName = "Level 1";

    [Tooltip("Tên Scene tương ứng, phải trùng chính xác tên đã thêm vào File > Build Settings > Scenes In Build")]
    public string sceneName;

    [Header("Độ khó Enemy")]
    [Tooltip("Hệ số nhân máu tối đa của MỌI Enemy (kể cả Boss) khi chơi Stage này. 1 = giữ nguyên như Inspector gốc trên prefab.")]
    public float enemyHpMultiplier = 1f;
    [Tooltip("Hệ số nhân sát thương chạm (enterDmg/stayDmg) của MỌI Enemy khi chơi Stage này.")]
    public float enemyDamageMultiplier = 1f;
    [Tooltip("Hệ số nhân tốc độ di chuyển của MỌI Enemy khi chơi Stage này.")]
    public float enemySpeedMultiplier = 1f;

    [Tooltip("Các augment CHỈ xuất hiện khi chơi Stage này, được cộng thêm vào pool augment chung lúc bắt đầu")]
    public List<Augment> extraAugments = new List<Augment>();
}
