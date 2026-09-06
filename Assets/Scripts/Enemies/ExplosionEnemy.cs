using UnityEngine;

public class ExplosionEnemy : Enemy
{
    [SerializeField] private GameObject explosionObject;

    // Chạm player thì nổ ngay và chết, thay vì gây stayDmg liên tục như enemy thường
    protected override void OnPlayerStay(Collider2D collision)
    {
        Die();
    }

    private void Explode()
    {
        if (explosionObject != null)
        {
            if (ObjectPoolManager.Instance != null)
                ObjectPoolManager.Instance.SpawnObject(explosionObject, transform.position, Quaternion.identity);
            else
                Instantiate(explosionObject, transform.position, Quaternion.identity);
        }
    }

    protected override void DropItems()
    {
        if (bigXpObject != null)
        {
            int bigCount = UnityEngine.Random.Range(1, 3); // 1-2 viên to
            for (int i = 0; i < bigCount; i++) SpawnItem(bigXpObject);
        }
        
        if (xpObject != null)
        {
            int smallCount = UnityEngine.Random.Range(0, 3); // 0-2 viên nhỏ
            for (int i = 0; i < smallCount; i++) SpawnItem(xpObject);
        }
    }

    protected override void Die()
    {
        if (isDead) return;
        Explode();
        base.Die();
    }
}
