using TMPro;
using UnityEngine;

public class GameUI : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    [SerializeField] private TextMeshProUGUI dmgText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI regenText;
    [SerializeField] private TextMeshProUGUI lifeStealText;

    void Start()
    {
        UpdateDmgText();
        UpdateHpText();
        UpdateRegenText();
        UpdateLifeStealText();
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void ResumeGame()
    {
        gameManager.ResumeGame();
    }

    // Nút "Back to Menu" trên Pause/GameOver/Win panel — quay về Scene MainMenu (Stage vừa chơi đã unlock/lock đúng)
    public void MainMenu()
    {
        gameManager.BackToMainMenu();
    }
    public void UpdateDmgText()
    {
        Player player = Player.Instance;
        if (dmgText != null)
        {
            if (player.bulletDamage > 0)
            {
                dmgText.text = "DAMAGE: " + player.bulletDamage.ToString();
            }
            else
            {
                dmgText.text = "LEVEL: 1";
            }
        }
    }
    public void UpdateHpText()
    {
        Player player = Player.Instance;
        if (healthText != null)
        {
            if (player.maxHP > 0)
            {
                healthText.text = "HP: " + player.maxHP.ToString();
            }
            else
            {
                healthText.text = "HP: 0";
            }
        }


    }
    public void UpdateRegenText()
    {
        Player player = Player.Instance;
        if (regenText != null && player != null)
        {
            regenText.text = player.GetRegenAmount().ToString() + " HP/s";
        }
    }

    public void UpdateLifeStealText()
    {
        Player player = Player.Instance;
        if (lifeStealText != null && player != null)
        {
            // Nhân 100 để ra con số phần trăm (VD: 0.05 -> 5%)
            float displayPercent = player.GetLifeStealPercent() * 100f;
            lifeStealText.text = "LIFE STEAL: " + displayPercent.ToString("F0") + "%";
        }
    }
}
