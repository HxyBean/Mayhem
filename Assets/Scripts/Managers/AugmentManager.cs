using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[System.Serializable]
public class Augment
{
    public string name;
    public string description;
    public string type;
    public int requiredLevel = 1;
}

public class AugmentManager : MonoBehaviour
{
    public static AugmentManager Instance;

    [Header("UI References")]
    [SerializeField] private GameObject augmentPanel;
    [SerializeField] private TMP_Text[] augmentNames;
    [SerializeField] private TMP_Text[] augmentDescs;

    [Header("Augment Data")]
    public List<Augment> augmentPool = new List<Augment>();
    private List<Augment> currentOptions = new List<Augment>();
    private CanvasGroup panelCanvasGroup;

    private void Awake()
    {
        Instance = this;

        panelCanvasGroup = augmentPanel.GetComponent<CanvasGroup>();

        if (panelCanvasGroup == null)
        {
            Debug.LogWarning("AugmentPanel thiếu Component CanvasGroup! Hãy add nó vào.");
        }
    }

    private void Start()
    {
        augmentPanel.SetActive(false);
        InitializePool();
    }

    void InitializePool()
    {
        // Augment dùng chung cho MỌI nhân vật. Augment riêng theo súng/phép (Ammo, Regen, Reload, Bomb,
        // BurstShot, SplitShot, Mana...) nằm trong CharacterData.exclusiveAugments, cộng vào qua ApplyCharacterData().
        augmentPool.Add(new Augment { name = "DAMAGE", description = "+2.5 Damage", type = "Damage", requiredLevel = 1 });
        augmentPool.Add(new Augment { name = "SPEED", description = "+10% Speed", type = "Speed", requiredLevel = 1 });
        augmentPool.Add(new Augment { name = "HEALTH", description = "+20 Max Health", type = "Health", requiredLevel = 1 });
        augmentPool.Add(new Augment { name = "LIFE STEAL", description = "+5% Life Steal", type = "LifeSteal", requiredLevel = 5 });
        augmentPool.Add(new Augment { name = "EXP", description = "+20% XP Value", type = "Exp", requiredLevel = 1 });
    }

    // Cộng thêm augment riêng của Stage được chọn vào pool chung. Gọi 1 lần khi GameManager bắt đầu ván chơi.
    public void ApplyStageData(StageData stage)
    {
        if (stage == null) return;

        foreach (Augment extra in stage.extraAugments)
        {
            augmentPool.Add(extra);
        }
    }

    // Cộng thêm augment riêng của nhân vật được chọn vào pool chung. Gọi 1 lần khi GameManager bắt đầu ván chơi.
    public void ApplyCharacterData(CharacterData character)
    {
        if (character == null) return;

        foreach (Augment extra in character.exclusiveAugments)
        {
            augmentPool.Add(extra);
        }
    }

    public void ShowAugmentMenu()
    {

        StartCoroutine(ShowMenuRoutine());
    }

    private IEnumerator ShowMenuRoutine()
    {
        Time.timeScale = 0f;
        augmentPanel.SetActive(true);

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = true; // Chặn bấm xuyên qua menu ngay lập tức
            panelCanvasGroup.alpha = 0f; // Bắt đầu từ trong suốt
        }

        SetupOptions();

        // Hiệu ứng Fade-in mượt mà trong 0.5s
        float timer = 0f;
        while (timer < 0.5f)
        {
            timer += Time.unscaledDeltaTime; // Dùng unscaled vì Time.timeScale đang là 0
            if (panelCanvasGroup != null)
                panelCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / 0.5f);
            yield return null;
        }

        // Sau khi nạp xong mới cho phép bấm
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.alpha = 1f;
        }
    }

    private void SetupOptions()
    {
        Player player = Player.Instance;
        GameManager gameManager = GameManager.Instance;
        int pLevel = (gameManager != null) ? gameManager.currentLevel : 1;

        currentOptions.Clear();

        // Các augment lõi đặc biệt này chỉ tồn tại trong pool nếu nhân vật hiện tại là Gunner
        // (CharacterData.exclusiveAugments của Pháp sư không có). Vì vậy phải kiểm tra augment
        // ép buộc có THỰC SỰ tồn tại trong pool không trước khi ép buộc - nếu không, các nhân vật
        // không có lõi đó (VD Pháp sư) sẽ bị màn chọn augment trống ở đúng level ép buộc.
        Augment bombAug = augmentPool.Find(a => a.type == "Bomb");
        Augment burstAug = augmentPool.Find(a => a.type == "BurstShot");
        Augment splitAug = augmentPool.Find(a => a.type == "SplitShot");

        // 1. Xử lý ép buộc (Forced) cho Level 2 và Level 10
        if (pLevel == 2 && bombAug != null)
        {
            // Bắt buộc chỉ xuất hiện lõi Bomb
            currentOptions.Add(bombAug);
        }
        else if (pLevel == 10 && (burstAug != null || splitAug != null))
        {
            // Bắt buộc chỉ xuất hiện BurstShot và SplitShot
            if (burstAug != null) currentOptions.Add(burstAug);
            if (splitAug != null) currentOptions.Add(splitAug);
        }
        else
        {
            // Các level khác: Chọn ngẫu nhiên từ validPool (loại trừ Bomb, Burst, Split để không bị trùng lặp ở level khác)
            List<Augment> validPool = new List<Augment>();
            foreach (Augment aug in augmentPool)
            {
                if (aug.type == "Bomb" || aug.type == "BurstShot" || aug.type == "SplitShot") continue;
                if (pLevel >= aug.requiredLevel)
                {
                    validPool.Add(aug);
                }
            }

            for (int i = 0; i < 3; i++)
            {
                if (validPool.Count == 0) break;
                int randomIndex = Random.Range(0, validPool.Count);
                currentOptions.Add(validPool[randomIndex]);
                validPool.RemoveAt(randomIndex); // Đảm bảo không trùng nhau
            }
        }

        // 2. Cập nhật UI hiển thị và Ẩn/Hiện nút tương ứng
        for (int i = 0; i < 3; i++)
        {
            // Sử dụng transform.parent.gameObject để bật/tắt an toàn (GetComponentInParent không tìm thấy object nếu nó đang bị tắt)
            GameObject btnObj = augmentNames[i].transform.parent.gameObject;

            if (i < currentOptions.Count)
            {
                btnObj.SetActive(true);
                augmentNames[i].text = currentOptions[i].name;
                augmentDescs[i].text = currentOptions[i].description;
            }
            else
            {
                btnObj.SetActive(false); // Ẩn hoàn toàn nút đi
                augmentNames[i].text = "";
                augmentDescs[i].text = "";
            }
        }
    }

    public void SelectAugment(int index)
    {
        if (index >= currentOptions.Count) return;

        string selectedType = currentOptions[index].type;
        ApplyEffect(selectedType);

        // KIỂM TRA VÀ XÓA THẺ SAU KHI CHỌN
        CheckAndRemoveAugment(selectedType);

        augmentPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    private void CheckAndRemoveAugment(string type)
    {
        Player player = Player.Instance;
        Gun bullet = Object.FindFirstObjectByType<Gun>();
        if (player == null) return;

        if (type == "Reload")
        {
            // Nếu đã nâng cấp đủ 2 lần
            if (bullet.GetReloadAugmentCount() >= 2)
            {
                augmentPool.RemoveAll(a => a.type == "Reload");
                Debug.Log("Đã xóa thẻ Thay đạn nhanh khỏi danh sách lựa chọn.");
            }
        }

        // Nếu chọn Bomb -> Xóa khỏi pool
        if (type == "Bomb")
        {
            augmentPool.RemoveAll(a => a.type == "Bomb");
            Debug.Log("Đã chọn Bomb, xóa khỏi danh sách lựa chọn.");
        }

        // Nếu chọn Potion -> Xóa khỏi pool
        if (type == "Potion")
        {
            augmentPool.RemoveAll(a => a.type == "Potion");
            Debug.Log("Đã chọn Potion, xóa khỏi danh sách lựa chọn.");
        }

        // Chọn 1 trong 2 loại đạn đặc biệt → xóa CẢ 2 khỏi pool
        if (type == "BurstShot" || type == "SplitShot")
        {
            augmentPool.RemoveAll(a => a.type == "BurstShot" || a.type == "SplitShot");
            Debug.Log("Đã chọn lõi đạn đặc biệt, xóa cả BurstShot và SplitShot khỏi pool.");
        }
    }

    private void ApplyEffect(string type)
    {
        Player player = Player.Instance;
        Gun bullet = Object.FindFirstObjectByType<Gun>();
        GameManager gameManager = GameManager.Instance;
        GameUI ui = Object.FindFirstObjectByType<GameUI>();
        if (player == null) return;

        switch (type)
        {
            case "Damage":
                player.IncreaseDamage(2.5f);
                ui.UpdateDmgText();
                break;
            case "Speed":
                player.ApplyMoveSpeedBoost(1.1f);
                break;
            case "Health":
                player.ApplyMaxHPBoost(20);
                ui.UpdateHpText();
                break;
            case "Bullet":
                bullet.AddAmmo(10);
                break;
            case "Regen":
                player.StartHealthRegen(2f); // Hồi 2 máu mỗi giây
                ui.UpdateRegenText();
                break;
            case "Reload":
                bullet.ReduceReloadTime(0.5f);
                break;
            case "LifeSteal":
                player.AddLifeSteal(0.05f); // Cộng thêm 5% mỗi lần chọn
                ui.UpdateLifeStealText();
                break;
            case "Exp":
                gameManager.AddExpBoost(0.2f); // Cộng thêm 20% mỗi lần chọn
                break;

            // === AUGMENT RIÊNG CỦA PHÁP SƯ ===
            case "ManaRegen":
                bullet.StartManaRegen(2f); // Hồi 2 mana mỗi giây
                break;
            case "BlinkCooldown":
                player.ReduceBlinkCooldown(0.9f);
                break;
            case "Potion":
                bullet.EnablePotion();
                break;

            // === LÕI NÂNG CẤP ĐẶC BIỆT (LEVEL 10) ===
            case "BurstShot":
                bullet.SetShootMode(ShootMode.Burst);
                break;
            case "SplitShot":
                bullet.SetShootMode(ShootMode.Split);
                break;
            case "Bomb":
                bullet.EnableBomb();
                break;
                
        }
    }
}
