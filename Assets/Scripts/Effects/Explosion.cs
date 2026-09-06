using UnityEngine;

public class Explosion : MonoBehaviour
{
    [SerializeField] private float dmg = 25f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.GetComponent<Player>();
        Enemy enemy = collision.GetComponent<Enemy>();

        if(collision.CompareTag("Player"))
        {   
            player.TakeDmg(dmg);
        }
        if (collision.CompareTag("Enemy"))
        {
            enemy.TakeDmg(dmg);
        }

    }

    public void DestroyExplosion()
    {
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnObjectToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
