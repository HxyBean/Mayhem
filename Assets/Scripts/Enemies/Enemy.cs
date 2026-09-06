using UnityEngine;
using UnityEngine.UI;

public abstract class Enemy : MonoBehaviour
{
    [SerializeField] protected float enemyMoveSpeed = 1f;
    protected Player player;
    [SerializeField] protected float maxHP = 50f;
    [SerializeField] protected float enterDmg = 10f;
    [SerializeField] protected float stayDmg = 1f;
    protected float currentHP;
    [SerializeField] private Image hpBar;
    [SerializeField] protected GameObject xpObject;
    [SerializeField] protected GameObject bigXpObject;
    protected bool isDead = false;


    protected virtual void OnEnable()
    {
        player = Player.Instance;
        currentHP = maxHP;
        isDead = false;
        UpdateHPBar();
    }

    protected virtual void Update()
    {
        MoveToPlayer();
    }

    // ==============================================
    // VA CHẠM VỚI PLAYER (dùng chung cho mọi loại Enemy)
    // ==============================================
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            OnPlayerEnter(collision);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            OnPlayerStay(collision);
        }
    }

    protected virtual void OnPlayerEnter(Collider2D collision)
    {
        player.TakeDmg(enterDmg);
    }

    protected virtual void OnPlayerStay(Collider2D collision)
    {
        player.TakeDmg(stayDmg);
    }
    protected void MoveToPlayer()
    {
        if (player != null)
        {
            transform.position = Vector2.MoveTowards(transform.position, player.transform.position, enemyMoveSpeed * Time.deltaTime);
            FlipEnemy();
        }
    }

    protected void FlipEnemy()
    {
        if (player != null)
        {
            transform.localScale = new Vector3(player.transform.position.x < transform.position.x ? -1 : 1, 1, 1);
        }
    }
    public virtual void TakeDmg(float dmg)
    {
        if (isDead) return;

        currentHP -= dmg;
        currentHP = Mathf.Max(currentHP, 0);
        UpdateHPBar();
        if (currentHP <= 0)
        {
            Die();
        }
    }

    protected Vector3 GetRandomDropPosition(float radius = 2f)
    {
        Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * radius;
        return transform.position + new Vector3(randomOffset.x, randomOffset.y, 0f);
    }

    protected GameObject SpawnItem(GameObject prefab)
    {
        if (prefab == null) return null;
        Vector3 dropPos = GetRandomDropPosition(2f);
        GameObject obj;
        if (ObjectPoolManager.Instance != null)
        {
            obj = ObjectPoolManager.Instance.SpawnObject(prefab, dropPos, Quaternion.identity);
        }
        else
        {
            obj = Instantiate(prefab, dropPos, Quaternion.identity);
            Destroy(obj, 5f);
        }
        return obj;
    }

    protected virtual void DropItems()
    {
        if (xpObject != null)
        {
            int dropCount = UnityEngine.Random.Range(1, 4); // Basic: 1-3
            for (int i = 0; i < dropCount; i++) SpawnItem(xpObject);
        }
    }

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        DropItems();
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnObjectToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    protected void UpdateHPBar()
    {
        if(hpBar != null)
        {
            hpBar.fillAmount = currentHP / maxHP;
        }
    }
}
