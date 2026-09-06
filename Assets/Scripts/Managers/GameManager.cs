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
    [SerializeField] private Image xpBar;
    [SerializeField] private TextMeshProUGUI levelText;

    [Header("Menu UI")]
    [SerializeField] private GameObject gameOverMenu;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject winMenu;

    [Header("Camera & Audio")]
    [SerializeField] private CinemachineCamera cam;
    [SerializeField] private AudioManager audioManager;

    [Header("Stage")]
    [Tooltip("Tên Scene Main Menu, dùng khi bấm nút thoát về menu")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [Tooltip("Chỉ dùng khi Play trực tiếp Scene này trong Editor (không qua Main Menu/Stage Select)")]
    [SerializeField] private StageData debugStage;

    // Private Fields
    private int currentEnergy = 0;
    private int currentUSB = 0;
    private float currentXP = 0f;
    private float expMultiplier = 1f; // 100% kinh nghiệm
    private StageData currentStage;
    public bool IsBossCalled { get; private set; } = false;

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

        boss.SetActive(false);
        enemySpawner.SetActive(true);

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

        SetActiveMenu(null);
        audioManager.PlayDefaultAudio();
    }

    // ==============================================
    // GAME STATE & MENU MANAGEMENT
    // ==============================================
    public void BackToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
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

        SetActiveMenu(winMenu);
        Time.timeScale = 0f;
    }

    private void SetActiveMenu(GameObject activeMenu)
    {
        if (gameOverMenu != null) gameOverMenu.SetActive(gameOverMenu == activeMenu);
        if (pauseMenu != null) pauseMenu.SetActive(pauseMenu == activeMenu);
        if (winMenu != null) winMenu.SetActive(winMenu == activeMenu);
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
        xpToLevelUp *= 1.25f; // Tăng yêu cầu XP cho cấp sau

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
        boss.SetActive(true);
        cam.Lens.OrthographicSize = 8f;
        audioManager.PlayBossAudio();
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

