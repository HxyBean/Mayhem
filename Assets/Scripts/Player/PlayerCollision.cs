using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private AudioManager audioManager;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = GetComponent<Player>();

        if (collision.CompareTag("EnemyBullet"))
        {
            player.TakeDmg(10f);
        }
        else if (collision.CompareTag("Energy"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddEnergy();
                if (GameManager.Instance.IsBossCalled)
                {
                    GameManager.Instance.AddXP(3); // Tương đương 1.5 viên nhỏ
                }
            }
            ReturnToPoolOrDestroy(collision.gameObject);
            if (audioManager != null) audioManager.PlayEnergySound();
        }
        else if (collision.CompareTag("Heart"))
        {
            HeartPickup heart = collision.GetComponent<HeartPickup>();
            float healAmount = (heart != null) ? heart.healValue : 10f;
            player.Heal(healAmount);
            ReturnToPoolOrDestroy(collision.gameObject);
            if (audioManager != null) audioManager.PlayEnergySound();
        }
        else if (collision.CompareTag("USB"))
        {
            // USB giờ chỉ còn là vật phẩm hồi đầy máu. Tiến trình phá đảo được tính ngay lúc hạ Boss
            // (GameManager.OnBossDefeated) chứ không còn phụ thuộc việc có nhặt viên này hay không.
            player.RestoreFullHP();
            ReturnToPoolOrDestroy(collision.gameObject);
        }
        else if (collision.CompareTag("ExpSmall"))
        {
            if (GameManager.Instance != null) GameManager.Instance.AddXP(2); // Viên nhỏ cộng 2 XP
            ReturnToPoolOrDestroy(collision.gameObject);
            if (audioManager != null) audioManager.PlayEnergySound();
        }
        else if (collision.CompareTag("ExpBig"))
        {
            if (GameManager.Instance != null) GameManager.Instance.AddXP(5); // Viên lớn cộng 5 XP
            ReturnToPoolOrDestroy(collision.gameObject);
            if (audioManager != null) audioManager.PlayEnergySound();
        }
        else if (collision.CompareTag("Coin") || collision.CompareTag("Diamond"))
        {
            CurrencyPickup currency = collision.GetComponent<CurrencyPickup>();
            if (currency != null) currency.Collect();
            ReturnToPoolOrDestroy(collision.gameObject);
            if (audioManager != null) audioManager.PlayEnergySound();
        }
        else if (collision.CompareTag("ExpBoss"))
        {
            if (GameManager.Instance != null) GameManager.Instance.AddXP(50); // Viên boss cộng 50 XP
            PullAllExpOrbsToPlayer(); // Hút toàn bộ EXP còn lại trên bản đồ về phía nhân vật
            ReturnToPoolOrDestroy(collision.gameObject);
            if (audioManager != null) audioManager.PlayEnergySound();
        }
    }

    // Gọi khi nhặt viên EXP Boss - hút bất kể các viên EXP khác đang ở đâu trên bản đồ, không phụ thuộc lõi Magnet
    private void PullAllExpOrbsToPlayer()
    {
        PullOrbsWithTag("ExpSmall");
        PullOrbsWithTag("ExpBig");
        PullOrbsWithTag("ExpBoss");
    }

    private void PullOrbsWithTag(string tag)
    {
        GameObject[] orbs = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject orb in orbs)
        {
            Pickup pickup = orb.GetComponent<Pickup>();
            if (pickup != null) pickup.ForceMagnetPull();
        }
    }

    private void ReturnToPoolOrDestroy(GameObject obj)
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
