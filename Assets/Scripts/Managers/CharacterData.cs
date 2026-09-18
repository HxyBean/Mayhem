using System.Collections.Generic;
using UnityEngine;

public enum AbilityType
{
    Dash,
    Blink,
    None // Không có skill di chuyển đặc biệt thay thế (VD Knight - quá trâu nên không cần né)
}

public enum CombatType
{
    Ranged, // Dùng Gun.cs: bắn đạn, ammo/mana, nút Bắn + Nạp đạn
    Melee   // Dùng KnightCombat.cs: tự động chém quanh nhân vật, stamina, nút Khiên + lõi đặc biệt
}

// Cấu hình riêng cho từng nhân vật chọn ở màn hình Character Select.
// Player.cs/Gun.cs/KnightCombat.cs dùng chung 1 script cho mọi nhân vật cùng loại; sự khác biệt hoàn toàn nằm ở dữ liệu trong asset này.
[CreateAssetMenu(fileName = "CharacterData", menuName = "Mayhem/Character Data")]
public class CharacterData : ScriptableObject
{
    [Tooltip("Tên nhân vật. LƯU Ý: tên này còn được dùng làm khóa lưu trạng thái đã mở khóa - đổi tên sau khi " +
             "người chơi đã mua sẽ làm mất trạng thái đã mua của họ. Mỗi nhân vật phải có tên khác nhau.")]
    public string characterName = "Gunner";

    [Header("Mở khóa nhân vật")]
    [Tooltip("Nhân vật khởi đầu, luôn mở sẵn và không cần mua (chỉ tick cho Gunner)")]
    public bool unlockedByDefault = false;

    [Tooltip("Giá mở khóa bằng Coin. Để 0 (hoặc nhỏ hơn) = KHÔNG cho mua bằng Coin, nút Coin sẽ bị ẩn đi")]
    public int coinPrice = 100;

    [Tooltip("Giá mở khóa bằng Kim cương. Để 0 (hoặc nhỏ hơn) = KHÔNG cho mua bằng Kim cương, nút Kim cương sẽ bị ẩn đi")]
    public int diamondPrice = 5;

    [Header("Chỉ số cơ bản")]
    [Tooltip("Sát thương gốc mặc định của nhân vật này (đạn với Gunner/Mage, đòn chém với Knight)")]
    public float baseBulletDamage = 10f;

    [Tooltip("Máu tối đa riêng của nhân vật này. Để 0 = giữ nguyên giá trị mặc định đang set sẵn trên Player trong Scene.")]
    public float baseMaxHP = 0f;

    [Tooltip("Tốc độ di chuyển riêng của nhân vật này. Để 0 = giữ nguyên giá trị mặc định đang set sẵn trên Player trong Scene.")]
    public float baseMoveSpeed = 0f;

    [Header("Chiến đấu")]
    [Tooltip("Ranged = dùng Gun.cs (đạn, ammo/mana). Melee = dùng KnightCombat.cs (tự động chém quanh nhân vật, stamina).")]
    public CombatType combatType = CombatType.Ranged;

    [Tooltip("Prefab đạn riêng của nhân vật này (chỉ áp dụng cho Combat Type = Ranged)")]
    public GameObject bulletPrefab;

    [Tooltip("Khả năng di chuyển đặc biệt: Dash (Gunner), Blink (Mage), hoặc None (Knight - không có)")]
    public AbilityType abilityType = AbilityType.Dash;

    [Header("Tạo hình")]
    [Tooltip("Animator Controller riêng của nhân vật này (khuyên dùng Animator Override Controller trỏ về cùng 1 State Machine gốc, chỉ đổi clip). Để trống = giữ nguyên Animator Controller đang gắn sẵn trên Player.")]
    public RuntimeAnimatorController animatorController;

    [Tooltip("Sprite mặc định gán ngay lúc bắt đầu, tránh lóe hình nhân vật cũ 1 frame trước khi Animator kịp chạy. Có thể để trống nếu Animator Controller đã tự có state Idle mặc định.")]
    public Sprite idleSprite;

    [Header("Vũ khí (chỉ dùng cho Combat Type = Ranged)")]
    [Tooltip("Hình ảnh vũ khí riêng (VD gậy phép thay vì súng). Để trống = giữ nguyên sprite súng đang gắn sẵn.")]
    public Sprite weaponSprite;

    [Header("Âm thanh (để trống = dùng âm thanh mặc định của Gunner)")]
    public AudioClip shootSound;
    public AudioClip reloadSound;

    [Header("Giao diện nút bắn/nạp đạn (chỉ dùng cho Combat Type = Ranged, để trống = giữ nguyên icon mặc định)")]
    public Sprite shootButtonIcon;
    public Sprite reloadButtonIcon;

    [Tooltip("Các augment CHỈ xuất hiện khi chơi nhân vật này, được cộng thêm vào pool augment chung lúc bắt đầu")]
    public List<Augment> exclusiveAugments = new List<Augment>();
}
