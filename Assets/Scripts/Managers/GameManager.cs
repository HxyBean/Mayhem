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
    [SerializeField] private float xpToLevelUp = 20f;
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
    [Tooltip("Modal cảnh báo khi bấm Thoát HOẶC Chơi lại ở màn Pause (bỏ dở ván sẽ mất sạch coin/kim cương). " +
             "Dùng CHUNG cho cả 2 nút, nội dung cảnh báo đổi theo nút vừa bấm")]
    [SerializeField] private GameObject exitConfirmPanel;
    [Tooltip("Text nội dung trong modal trên. Để trống nếu muốn giữ nguyên câu chữ đã gõ sẵn trong Editor")]
    [SerializeField] private TextMeshProUGUI confirmMessageText;
    [SerializeField, TextArea(2, 4)]
    private string exitWarningMessage = "Thoát ra giữa chừng sẽ mất toàn bộ Coin và Kim cương nhặt được trong ván này. Vẫn thoát?";
    [SerializeField, TextArea(2, 4)]
    private string restartWarningMessage = "Chơi lại sẽ mất toàn bộ Coin và Kim cương nhặt được trong ván này. Vẫn chơi lại?";

    // Modal xác nhận dùng chung cho 2 nút nên phải nhớ nút nào vừa bấm
    private enum PendingConfirmAction { ExitToMainMenu, RestartLevel }
    private PendingConfirmAction pendingConfirmAction = PendingConfirmAction.ExitToMainMenu;

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
    // Số phase Boss ĐÃ HẠ - đây chính là tiến trình của thanh "USB" trên HUD. ĐỪNG nhầm với collectedUsb bên
    // dưới (vật phẩm USB thật sự nhặt được): 2 thứ hoàn toàn khác nhau, chỉ trùng chữ "USB" vì các field
    // [SerializeField] usbThreshold/usbBar đã trót đặt tên vậy và đổi tên sẽ mất tham chiếu trong Inspector.
    private int bossPhaseCount = 0;
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

    // Vật phẩm USB nhặt được trong ván (do USBEnemy rơi ra). Là tài nguyên TIÊU HAO dùng ngay trong màn: giao
    // nhiệm vụ cho NPC, sau này còn để trao đổi. KHÔNG lưu qua ván như Coin/Kim cương, cũng KHÔNG liên quan gì
    // tới bossPhaseCount ở trên.
    private int collectedUsb = 0;
    public int CollectedUsb => collectedUsb;

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
        bossPhaseCount = 0;
        collectedUsb = 0;
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

        // Chỉ số nội tại mua ở Shop - áp SAU ApplyCharacterData() (hàm đó ghi đè chỉ số gốc theo nhân vật) và
        // để ngoài khối if để vẫn hoạt động cả khi Play thẳng Scene trong Editor mà không chọn nhân vật nào.
        if (Player.Instance != null) Player.Instance.ApplyShopUpgrades();

        // HUD có thể đã tự vẽ số liệu từ Start() của chính nó TRƯỚC khi chỗ này chạy (Unity không đảm bảo thứ
        // tự Start giữa các MonoBehaviour), nên phải vẽ lại cho khớp chỉ số cuối cùng.
        GameUI gameUI = FindFirstObjectByType<GameUI>();
        if (gameUI != null) gameUI.RefreshAllStats();

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

    // ==============================================
    // ĐIỀU HƯỚNG TỪ MÀN PAUSE / THUA / THẮNG
    // ==============================================
    // Chơi lại chính Level đang chơi. Từ Pause thì đi qua modal cảnh báo (mất tiền của ván), từ Thua/Thắng thì
    // gọi thẳng vì CommitRunCurrency() đã chạy rồi - cờ runCurrencyCommitted chặn cộng 2 lần.
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        GameProgress.SaveNow();

        string sceneName = (currentStage != null && !string.IsNullOrEmpty(currentStage.sceneName))
            ? currentStage.sceneName
            : SceneManager.GetActiveScene().name; // Play thẳng Scene trong Editor không có currentStage

        SceneManager.LoadScene(sceneName);
    }

    // Quay về màn CHỌN LEVEL
    public void BackToStageSelect()
    {
        GameProgress.PendingPanel = GameProgress.MenuPanel.StageSelect;
        BackToMainMenu();
    }

    // Quay về màn CHỌN NHÂN VẬT của chính Level đang chơi (đổi tướng rồi chơi lại màn này)
    public void BackToCharacterSelect()
    {
        GameProgress.PendingPanel = GameProgress.MenuPanel.CharacterSelect;
        GameProgress.AdvanceToNextStage = false;
        BackToMainMenu();
    }

    // Nút "Màn tiếp theo" ở màn hình Thắng: sang Level kế tiếp và chọn tướng luôn cho màn đó.
    // Việc tra ra Level kế tiếp do MainMenuUI làm, vì danh sách toàn bộ Level chỉ tồn tại ở Scene MainMenu.
    public void NextLevel()
    {
        GameProgress.PendingPanel = GameProgress.MenuPanel.CharacterSelect;
        GameProgress.AdvanceToNextStage = true;
        BackToMainMenu();
    }

    // Nút "Thoát" trên Pause panel - cảnh báo trước thay vì thoát ngay, vì bỏ dở ván sẽ mất sạch tiền đã nhặt
    public void ShowExitConfirm()
    {
        ShowConfirm(PendingConfirmAction.ExitToMainMenu, exitWarningMessage);
    }

    // Nút "Chơi lại" trên Pause panel - CŨNG phải cảnh báo: chơi lại giữa chừng cũng là bỏ dở ván, mất sạch
    // coin/kim cương y hệt như thoát ra. Chơi lại mà mất tiền không báo trước là một bất ngờ khó chịu.
    public void ShowRestartConfirm()
    {
        ShowConfirm(PendingConfirmAction.RestartLevel, restartWarningMessage);
    }

    private void ShowConfirm(PendingConfirmAction action, string message)
    {
        pendingConfirmAction = action;

        if (confirmMessageText != null && !string.IsNullOrEmpty(message)) confirmMessageText.text = message;
        if (exitConfirmPanel != null) exitConfirmPanel.SetActive(true);
    }

    public void CancelExitConfirm()
    {
        if (exitConfirmPanel != null) exitConfirmPanel.SetActive(false);
    }

    // Nút "Yes" trên modal cảnh báo. GIỮ NGUYÊN TÊN HÀM vì nó đã được nối sẵn vào nút trong Editor - đổi tên là
    // nút mất tham chiếu mà Unity không báo lỗi gì.
    // Cố tình KHÔNG gọi CommitRunCurrency() ở cả 2 nhánh, nên toàn bộ coin/kim cương nhặt được trong ván này bị
    // bỏ đi, đúng như lời cảnh báo.
    public void ConfirmExitToMainMenu()
    {
        if (pendingConfirmAction == PendingConfirmAction.RestartLevel)
        {
            RestartLevel();
            return;
        }

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

        // PHẢI gom trước CommitRunCurrency(). Thắng xảy ra ngay lúc hạ Boss cuối nên Time.timeScale về 0 đúng
        // vào lúc phần thưởng vừa rơi ra - người chơi không còn cơ hội chạy tới nhặt, đặc biệt là viên kim cương
        // phá đảo lần đầu (bỏ lỡ là mất vĩnh viễn vì Stage đã được đánh dấu hoàn thành).
        CollectDroppedCurrency();

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
        xpToLevelUp *= 1.075f; // Tăng yêu cầu XP cho cấp sau

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
    // Boss vừa bị hạ có phải phase ĐẦU TIÊN của ván không.
    // PHỤ THUỘC THỨ TỰ: BossEnemy.Die() gọi DropItems() (nơi dùng hàm này) TRƯỚC OnBossDefeated() (nơi tăng
    // bossPhaseCount), nên ngay tại phase đầu tiên bossPhaseCount vẫn đang là 0.
    private bool IsFirstBossKill()
    {
        return bossPhaseCount == 0;
    }

    // Kim cương CHỈ rơi ở phase Boss ĐẦU TIÊN, và chỉ trong lần đầu chinh phục Stage này.
    // Rơi ở phase đầu (thay vì phase cuối) vì hạ Boss phase cuối là thắng luôn -> Time.timeScale về 0 ngay lúc
    // kim cương vừa rơi ra, người chơi không kịp chạy tới nhặt. Rơi sớm thì có cả ván để thong thả nhặt.
    public bool ShouldDropDiamond()
    {
        if (currentStage == null) return false;
        if (!IsFirstBossKill()) return false;
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

    // Gọi từ PlayerCollision khi nhặt vật phẩm USB (do USBEnemy rơi ra)
    public void AddUsb(int amount)
    {
        if (amount <= 0) return;

        collectedUsb += amount;
        OnRunCurrencyChanged?.Invoke();
    }

    // Tiêu USB (giao nhiệm vụ cho NPC). Kẹp ở 0 để không bao giờ âm.
    public void SpendUsb(int amount)
    {
        if (amount <= 0) return;

        collectedUsb = Mathf.Max(0, collectedUsb - amount);
        OnRunCurrencyChanged?.Invoke();
    }

    // ==============================================
    // TIÊU TÀI NGUYÊN TRONG VÁN (mua mã độc ở NPCComputer)
    // ==============================================
    // Khác SpendUsb ở trên: trả về false và KHÔNG trừ gì cả khi không đủ, thay vì kẹp về 0. Giao dịch mua bán
    // bắt buộc phải dùng kiểu này - kẹp về 0 nghĩa là người chơi vẫn nhận được hàng dù trả thiếu.
    public bool TrySpendUsb(int amount)
    {
        if (amount <= 0) return false;
        if (collectedUsb < amount) return false;

        collectedUsb -= amount;
        OnRunCurrencyChanged?.Invoke();
        return true;
    }

    // Tiêu Coin của VÁN NÀY (runCoin), không phải tổng Coin đã lưu. Tiêu ở đây thì cuối ván CommitRunCurrency()
    // cộng vào tổng ít đi bấy nhiêu - đó chính là cái giá phải cân nhắc: mạnh ngay trong ván, hay để dành mua
    // chỉ số vĩnh viễn ở Shop.
    public bool TrySpendRunCoin(int amount)
    {
        if (amount <= 0) return false;
        if (runCoin < amount) return false;

        runCoin -= amount;
        OnRunCurrencyChanged?.Invoke();
        return true;
    }

    // Gom mọi Coin/Kim cương còn nằm trên bản đồ vào ví của ván, coi như người chơi đã nhặt hết.
    // FindGameObjectsWithTag chỉ trả về object ĐANG BẬT nên không đụng tới các vật phẩm đang nằm sẵn trong Pool.
    private void CollectDroppedCurrency()
    {
        CollectDroppedCurrencyWithTag("Coin");
        CollectDroppedCurrencyWithTag("Diamond");
    }

    private void CollectDroppedCurrencyWithTag(string tag)
    {
        GameObject[] droppedItems = GameObject.FindGameObjectsWithTag(tag);

        foreach (GameObject item in droppedItems)
        {
            CurrencyPickup pickup = item.GetComponent<CurrencyPickup>();
            if (pickup != null) pickup.Collect();

            if (ObjectPoolManager.Instance != null) ObjectPoolManager.Instance.ReturnObjectToPool(item);
            else Destroy(item);
        }
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

    // Gọi từ BossEnemy.Die(). Tiến trình phá đảo tính theo SỐ PHASE BOSS ĐÃ HẠ, cộng ngay tại đây - không còn
    // phụ thuộc việc người chơi có nhặt vật phẩm USB hay không (cách cũ tạo lỗ hổng: cứ bỏ USB dưới đất là kẹt
    // phase vĩnh viễn mà vẫn farm coin/kinh nghiệm từ quái thường vô hạn).
    public void OnBossDefeated()
    {
        bossPhaseCount += 1;
        UpdateUsbBar();

        if (bossPhaseCount >= usbThreshold)
        {
            WinGame();
            return;
        }

        SpawnBossWarningThenCallBoss();
    }

    // Hiện hiệu ứng cảnh báo Boss sắp xuất hiện, rồi mới thật sự gọi Boss
    private void SpawnBossWarningThenCallBoss()
    {
        if (bossRevive != null)
        {
            GameObject reviveObj = ObjectPoolManager.Instance != null
                ? ObjectPoolManager.Instance.SpawnObject(bossRevive, boss.transform.position, Quaternion.identity)
                : Instantiate(bossRevive, boss.transform.position, Quaternion.identity);

            StartCoroutine(DestroyAfterDelay(reviveObj, 3f)); // Hiệu ứng cảnh báo tồn tại 3 giây
        }

        StartCoroutine(DelayedBossSpawn(2.0f)); // Boss xuất hiện sau khi hiệu ứng cảnh báo đã chạy được 2 giây
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
            usbBar.fillAmount = Mathf.Clamp01((float)bossPhaseCount / usbThreshold);
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

