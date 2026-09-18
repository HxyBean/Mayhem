using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Stats")]
    [SerializeField] private int energyThreshold = 10;
    [SerializeField] private int usbThreshold = 3;
    [SerializeField] private float xpToLevelUp = 10f;
    public int currentLevel = 1;

    [Header("Enemy & Boss Management")]
    [SerializeField] private GameObject boss;
    [SerializeField] private GameObject bossRevive;
    [SerializeField] private GameObject enemySpawner;

    [Header("In-Game UI")]
    [SerializeField] private Image energyBar;
    [SerializeField] private Image usbBar;
    [Tooltip("GameObject bao ngoài thanh USB (cả khung/icon) - tự ẩn khi Boss chưa xuất hiện. Để trống = ẩn/hiện luôn GameObject của Usb Bar")]
    [SerializeField] private GameObject usbBarGroup;
    [SerializeField] private Image xpBar;
    [SerializeField] private TextMeshProUGUI levelText;

    [Header("Menu UI")]
    [SerializeField] private GameObject gameOverMenu;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject winMenu;
    [Tooltip("Modal cảnh báo khi bấm Thoát ở màn Pause (thoát giữa chừng sẽ mất sạch coin/kim cương của ván này)")]
    [SerializeField] private GameObject exitConfirmPanel;

    [Header("Camera & Audio")]
    [SerializeField] private CinemachineCamera cam;
    [SerializeField] private AudioManager audioManager;

    [Header("Stage")]
    [Tooltip("Tên Scene Main Menu, dùng khi bấm nút thoát về menu")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [Tooltip("Chỉ dùng khi Play trực tiếp Scene này trong Editor (không qua Main Menu/Stage Select)")]
    [SerializeField] private StageData debugStage;
    [Tooltip("Chỉ dùng khi Play trực tiếp Scene này trong Editor (không qua Character Select)")]
    [SerializeField] private CharacterData debugCharacter;

    // Private Fields
    private int currentEnergy = 0;
    private int currentUSB = 0;
    private float currentXP = 0f;
    private float expMultiplier = 1f; // 100% kinh nghiệm
    private StageData currentStage;
    public StageData CurrentStage => currentStage;
    public bool IsBossCalled { get; private set; } = false;

    // Tiền tệ nhặt được TRONG VÁN NÀY - chưa cộng vào tổng đã lưu cho tới khi ván kết thúc hợp lệ (thắng hoặc
    // chết). Thoát giữa chừng ở màn Pause thì mất trắng, nên mới cần modal cảnh báo exitConfirmPanel.
    private int runCoin = 0;
    private int runDiamond = 0;
    private bool runCurrencyCommitted = false;
    public int RunCoin => runCoin;
    public int RunDiamond => runDiamond;

    // Bắn ra mỗi khi nhặt được coin/kim cương trong ván, để CurrencyUI (chế độ This Run) tự cập nhật
    public static event System.Action OnRunCurrencyChanged;

    // ==============================================
    // UNITY CALLBACKS
    // ==============================================
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
        Time.timeScale = 1f; // Phòng trường hợp Scene trước đó (Pause/GameOver) rời đi khi timeScale đang = 0
        currentEnergy = 0;
        currentUSB = 0;
        currentXP = 0f;
        IsBossCalled = false;
        runCoin = 0;
        runDiamond = 0;
        runCurrencyCommitted = false;

        boss.SetActive(false);
        enemySpawner.SetActive(true);
        SetUsbBarVisible(false); // Chưa gọi được Boss thì thanh USB chưa có ý nghĩa gì, ẩn đi cho gọn HUD

        UpdateEnergyBar();
        UpdateUsbBar();
        UpdateXPBar();
        UpdateLevelText();

        cam.Lens.OrthographicSize = 5f;

        // Stage được chọn ở màn Stage Select (Scene MainMenu); nếu Play thẳng Scene này trong Editor thì dùng debugStage
        currentStage = GameProgress.SelectedStage != null ? GameProgress.SelectedStage : debugStage;
        if (AugmentManager.Instance != null)
        {
            AugmentManager.Instance.ApplyStageData(currentStage);
        }

        // Nhân vật được chọn ở màn Character Select; nếu Play thẳng Scene này trong Editor thì dùng debugCharacter
        CharacterData currentCharacter = GameProgress.SelectedCharacter != null ? GameProgress.SelectedCharacter : debugCharacter;
        if (currentCharacter != null)
        {
            if (Player.Instance != null) Player.Instance.ApplyCharacterData(currentCharacter);

            bool isMelee = currentCharacter.combatType == CombatType.Melee;

            Gun gun = FindFirstObjectByType<Gun>();
            if (gun != null)
            {
                gun.SetActive(!isMelee);
                if (!isMelee) gun.ApplyCharacterData(currentCharacter);
            }

            KnightCombat knight = FindFirstObjectByType<KnightCombat>();
            if (knight != null)
            {
                knight.SetActive(isMelee);
                if (isMelee) knight.ApplyCharacterData(currentCharacter);
            }

            if (AugmentManager.Instance != null) AugmentManager.Instance.ApplyCharacterData(currentCharacter);
        }

        SetActiveMenu(null);
        audioManager.PlayDefaultAudio();
    }

    // ==============================================
    // GAME STATE & MENU MANAGEMENT
    // ==============================================
    // Nút "Back to Menu" trên Win/GameOver panel - lúc này tiền của ván đã được cộng vào tổng rồi (CommitRunCurrency)
    public void BackToMainMenu()
    {
        Time.timeScale = 1f;
        GameProgress.SaveNow();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // Nút "Thoát" trên Pause panel - cảnh báo trước thay vì thoát ngay, vì bỏ dở ván sẽ mất sạch tiền đã nhặt
    public void ShowExitConfirm()
    {
        if (exitConfirmPanel != null) exitConfirmPanel.SetActive(true);
    }

    public void CancelExitConfirm()
    {
        if (exitConfirmPanel != null) exitConfirmPanel.SetActive(false);
    }

    // Nút "Yes" trên modal cảnh báo - cố tình KHÔNG gọi CommitRunCurrency() nên toàn bộ coin/kim cương nhặt
    // được trong ván này bị bỏ đi, đúng như lời cảnh báo.
    public void ConfirmExitToMainMenu()
    {
        BackToMainMenu();
    }

    public void PauseMenu()
    {
        SetActiveMenu(pauseMenu);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        SetActiveMenu(null);
        Time.timeScale = 1f;
    }

    public void GameOverMenu()
    {
        CommitRunCurrency(); // Chết vẫn được giữ tiền đã nhặt, chỉ thoát giữa chừng mới mất
        SetActiveMenu(gameOverMenu);
        Time.timeScale = 0f;
    }

    public void WinGame()
    {
        // Phá đảo Stage hiện tại thì mở khóa Stage kế tiếp
        if (currentStage != null)
        {
            GameProgress.CompleteStage(currentStage.stageIndex);
        }

        CommitRunCurrency();

        SetActiveMenu(winMenu);
        Time.timeScale = 0f;
    }

    private void SetActiveMenu(GameObject activeMenu)
    {
        if (gameOverMenu != null) gameOverMenu.SetActive(gameOverMenu == activeMenu);
        if (pauseMenu != null) pauseMenu.SetActive(pauseMenu == activeMenu);
        if (winMenu != null) winMenu.SetActive(winMenu == activeMenu);

        // Modal cảnh báo thoát luôn đóng khi chuyển menu, để lần Pause sau không thấy nó hiện sẵn
        if (exitConfirmPanel != null) exitConfirmPanel.SetActive(false);
    }

    // ==============================================
    // RESOURCES & PROGRESSION
    // ==============================================
    public void AddEnergy()
    {
        if (IsBossCalled) return;

        currentEnergy += 1;
        UpdateEnergyBar();

        if (currentEnergy >= energyThreshold)
        {
            CallBoss();
        }
    }

    public void AddUSB()
    {
        currentUSB += 1;
        UpdateUsbBar();

        if (Player.Instance != null)
        {
            Player.Instance.RestoreFullHP();
        }

        if (currentUSB >= usbThreshold)
        {
            WinGame();
        }
        else
        {
            // Hiệu ứng revive boss (tồn tại 3 giây)
            if (bossRevive != null)
            {
                GameObject reviveObj;
                if (ObjectPoolManager.Instance != null)
                {
                    reviveObj = ObjectPoolManager.Instance.SpawnObject(bossRevive, boss.transform.position, Quaternion.identity);
                }
                else
                {
                    reviveObj = Instantiate(bossRevive, boss.transform.position, Quaternion.identity);
                }
                StartCoroutine(DestroyAfterDelay(reviveObj, 3f));
            }
            StartCoroutine(DelayedBossSpawn(2.0f));
        }
    }

    public void AddXP(int amount)
    {
        currentXP += amount * GetExpMultiplier();
        UpdateXPBar();

        if (currentXP >= xpToLevelUp)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentLevel++;
        currentXP -= xpToLevelUp;
        xpToLevelUp *= 1.15f; // Tăng yêu cầu XP cho cấp sau

        UpdateXPBar();
        UpdateLevelText();

        if (AugmentManager.Instance != null)
        {
            AugmentManager.Instance.ShowAugmentMenu();
        }
    }

    public void AddExpBoost(float amount)
    {
        expMultiplier += amount;
        Debug.Log("Hệ số kinh nghiệm hiện tại: " + (expMultiplier * 100) + "%");
    }

    public float GetExpMultiplier()
    {
        return expMultiplier;
    }

    // ==============================================
    // BOSS MANAGEMENT
    // ==============================================
    private void CallBoss()
    {
        IsBossCalled = true;
        SetUsbBarVisible(true); // Boss xuất hiện thì thanh USB mới có ý nghĩa, hiện lại cho người chơi theo dõi
        boss.SetActive(true);
        cam.Lens.OrthographicSize = 8f;
        audioManager.PlayBossAudio();
    }

    // ==============================================
    // KIM CƯƠNG (phần thưởng phá đảo lần đầu)
    // ==============================================
    // Boss sắp bị hạ có phải lần CUỐI trong ván không: viên USB rơi ra lần này nhặt vào sẽ đủ ngưỡng thắng.
    // VD usbThreshold = 3: lần chết 1 (currentUSB=0) và 2 (=1) đều false, chỉ lần 3 (=2) mới true.
    private bool IsFinalBossKill()
    {
        return currentUSB + 1 >= usbThreshold;
    }

    // Kim cương CHỈ rơi khi: đây là lần hạ Boss cuối cùng của ván VÀ đây là lần đầu phá đảo Stage này
    // (chưa từng thắng, và cũng chưa từng nhặt kim cương của Stage này ở lần chơi trước đó).
    public bool ShouldDropDiamond()
    {
        if (currentStage == null) return false;
        if (!IsFinalBossKill()) return false;
        if (GameProgress.IsStageCompleted(currentStage.stageIndex)) return false;

        return !GameProgress.IsStageDiamondClaimed(currentStage.stageIndex);
    }

    // ==============================================
    // TIỀN TỆ NHẶT TRONG VÁN
    // ==============================================
    // Gọi từ CurrencyPickup khi Player nhặt được vật phẩm tiền tệ
    public void AddRunCoin(int amount)
    {
        if (amount <= 0) return;

        runCoin += amount;
        OnRunCurrencyChanged?.Invoke();
    }

    public void AddRunDiamond(int amount)
    {
        if (amount <= 0) return;

        runDiamond += amount;
        OnRunCurrencyChanged?.Invoke();
    }

    // Cộng tiền nhặt trong ván vào tổng đã lưu. CHỈ gọi khi ván kết thúc hợp lệ (thắng hoặc chết) - thoát giữa
    // chừng thì cố tình KHÔNG gọi, người chơi mất trắng số tiền của ván đó.
    private void CommitRunCurrency()
    {
        if (runCurrencyCommitted) return;
        runCurrencyCommitted = true;

        GameProgress.AddCoin(runCoin);
        GameProgress.AddDiamond(runDiamond);

        // Đánh dấu đã nhận kim cương của Stage tại ĐÂY (lúc cộng vào tổng) chứ không phải lúc nhặt: nếu đánh
        // dấu ngay lúc nhặt mà người chơi thoát giữa chừng thì kim cương vừa bị mất, vừa không bao giờ rơi lại.
        if (runDiamond > 0 && currentStage != null)
        {
            GameProgress.MarkStageDiamondClaimed(currentStage.stageIndex);
        }

        GameProgress.SaveNow();
    }

    public void OnBossDefeated()
    {
        IsBossCalled = false;
        boss.SetActive(false);
    }

    private IEnumerator DelayedBossSpawn(float delay)
    {
        yield return new WaitForSeconds(delay);
        CallBoss();
    }

    private IEnumerator DestroyAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null)
        {
            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.ReturnObjectToPool(obj);
            }
            else
            {
                Destroy(obj);
            }
        }
    }

    // ==============================================
    // UI UPDATER HELPERS
    // ==============================================
    // Ưu tiên ẩn/hiện GameObject bao ngoài (khung + icon); nếu không gán thì ẩn/hiện chính Image thanh USB.
    // fillAmount vẫn cập nhật bình thường kể cả lúc đang ẩn, nên khi hiện lại là đã đúng số liệu.
    private void SetUsbBarVisible(bool visible)
    {
        GameObject target = usbBarGroup != null ? usbBarGroup : (usbBar != null ? usbBar.gameObject : null);
        if (target != null) target.SetActive(visible);
    }

    private void UpdateUsbBar()
    {
        if (usbBar != null)
        {
            usbBar.fillAmount = Mathf.Clamp01((float)currentUSB / usbThreshold);
        }
    }

    private void UpdateEnergyBar()
    {
        if (energyBar != null)
        {
            energyBar.fillAmount = Mathf.Clamp01((float)currentEnergy / energyThreshold);
        }
    }

    private void UpdateXPBar()
    {
        if (xpBar != null)
        {
            xpBar.fillAmount = Mathf.Clamp01(currentXP / xpToLevelUp);
        }
    }

    private void UpdateLevelText()
    {
        if (levelText != null)
        {
            levelText.text = "LEVEL: " + Mathf.Max(currentLevel, 1).ToString();
        }
    }
}

