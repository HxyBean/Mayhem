using System.Collections.Generic;
using UnityEngine;

public enum AbilityType
{
    Dash,
    Blink
}

// Cấu hình riêng cho từng nhân vật chọn ở màn hình Character Select.
// Player.cs/Gun.cs dùng chung 1 script cho mọi nhân vật; sự khác biệt hoàn toàn nằm ở dữ liệu trong asset này.
[CreateAssetMenu(fileName = "CharacterData", menuName = "Mayhem/Character Data")]
public class CharacterData : ScriptableObject
{
    public string characterName = "Gunner";

    [Tooltip("Sát thương gốc mặc định của nhân vật này")]
    public float baseBulletDamage = 10f;

    [Tooltip("Prefab đạn riêng của nhân vật này (dùng chung script PlayerBullet)")]
    public GameObject bulletPrefab;

    [Tooltip("Khả năng di chuyển đặc biệt: Dash (Gunner) hoặc Blink (Mage)")]
    public AbilityType abilityType = AbilityType.Dash;

    [Header("Tạo hình")]
    [Tooltip("Animator Controller riêng của nhân vật này (khuyên dùng Animator Override Controller trỏ về cùng 1 State Machine gốc, chỉ đổi clip). Để trống = giữ nguyên Animator Controller đang gắn sẵn trên Player.")]
    public RuntimeAnimatorController animatorController;

    [Tooltip("Sprite mặc định gán ngay lúc bắt đầu, tránh lóe hình nhân vật cũ 1 frame trước khi Animator kịp chạy. Có thể để trống nếu Animator Controller đã tự có state Idle mặc định.")]
    public Sprite idleSprite;

    [Header("Vũ khí")]
    [Tooltip("Hình ảnh vũ khí riêng (VD gậy phép thay vì súng). Để trống = giữ nguyên sprite súng đang gắn sẵn.")]
    public Sprite weaponSprite;

    [Header("Âm thanh (để trống = dùng âm thanh mặc định của Gunner)")]
    public AudioClip shootSound;
    public AudioClip reloadSound;

    [Header("Giao diện nút bắn/nạp đạn (để trống = giữ nguyên icon mặc định)")]
    public Sprite shootButtonIcon;
    public Sprite reloadButtonIcon;

    [Tooltip("Các augment CHỈ xuất hiện khi chơi nhân vật này, được cộng thêm vào pool augment chung lúc bắt đầu")]
    public List<Augment> exclusiveAugments = new List<Augment>();
}
