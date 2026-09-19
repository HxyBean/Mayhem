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
        RefreshAllStats();
    }

    // Vẽ lại toàn bộ chỉ số. GameManager gọi lại hàm này sau khi áp chỉ số Shop, phòng khi Start() ở đây đã
    // chạy trước và vẽ số liệu cũ.
    public void RefreshAllStats()
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

    // Nút "Back to Menu" trên GameOver/Win panel — lúc này tiền của ván đã được cộng vào tổng rồi nên thoát thẳng
    public void MainMenu()
    {
        gameManager.BackToMainMenu();
    }

    // Nút "Thoát" trên PAUSE panel — hiện modal cảnh báo mất tiền thay vì thoát ngay
    public void ExitFromPause()
    {
        gameManager.ShowExitConfirm();
    }

    // Nút "No/Cancel" trên modal cảnh báo
    public void CancelExit()
    {
        gameManager.CancelExitConfirm();
    }

    // Nút "Yes" trên modal cảnh báo — chấp nhận mất toàn bộ coin/kim cương nhặt được trong ván này
    public void ConfirmExit()
    {
        gameManager.ConfirmExitToMainMenu();
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
