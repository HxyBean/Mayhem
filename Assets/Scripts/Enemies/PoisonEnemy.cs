using System.Collections;
using UnityEngine;

// Quái bắn đạn cầu vồng để lại vùng độc. Cơ chế tiếp cận giống RangedEnemy (ngoài stopRadius thì đuổi, vào
// trong thì đứng bắn), khác ở 3 điểm:
//  1. Đạn bay theo vòng cung tới 1 ĐIỂM đã khóa, có vệt cảnh báo điểm rơi suốt thời gian bay (PoisonProjectile).
//  2. Rơi xuống để lại vùng độc gây sát thương theo thời gian (PoisonZone).
//  3. Animation bắn chạy TRƯỚC, hết animation mới thật sự nhả đạn.
public class PoisonEnemy : Enemy
{
    [Header("Tấn công")]
    [Tooltip("Vào trong bán kính này thì dừng lại và bắt đầu bắn")]
    [SerializeField] private float stopRadius = 8f;
    [SerializeField] private float attackCoolDown = 5f;
    [Tooltip("Điểm đạn bay ra. Để trống = bắn từ tâm con quái")]
    [SerializeField] private Transform firePos;
    [SerializeField] private GameObject poisonProjectilePrefab;

    [Header("Animation")]
    [Tooltip("Đợi bấy nhiêu giây SAU khi kích hoạt animation bắn rồi mới thật sự nhả đạn. Chỉnh cho khớp đúng " +
             "khung hình vung tay trong clip - đây là con số duy nhất quyết định animation có ăn khớp hay không")]
    [SerializeField] private float shootAnimationDelay = 0.4f;
    [Tooltip("Tên tham số bool chạy/đứng trong Animator. Quy ước chung của project là 'isRun' (trùng Player/NPC)")]
    [SerializeField] private string runBoolName = "isRun";
    [Tooltip("Tên trigger animation bắn trong Animator")]
    [SerializeField] private string attackTriggerName = "Attack";

    private Animator animator;
    private float nextAttackTime = 0f;
    private bool isAttacking = false;

    protected override void Awake()
    {
        base.Awake();

        // Quy ước của project là Animator nằm trên chính object gốc (giống RangedEnemy/BasicEnemy). Vẫn tìm
        // thêm ở con vì nếu không có Animator thì animation im lặng không chạy mà chẳng có lỗi nào báo ra.
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        // Pool tái sử dụng: con trước có thể chết ngay giữa lúc đang vận đòn, còn sót cờ là con mới đứng đơ
        // vĩnh viễn vì Update() luôn thoát sớm.
        isAttacking = false;

        // SẴN SÀNG BẮN NGAY từ lúc spawn, để phát đầu tiên nổ ra đúng lúc vừa vào tầm.
        // ĐỪNG đặt Time.time + attackCoolDown ở đây: làm vậy là đồng hồ hồi chiêu chạy từ lúc SPAWN chứ không
        // phải từ lúc bắn, nên con quái đi tới nơi rồi vẫn phải đứng chờ nốt phần thời gian còn lại - nhìn như
        // bị đơ. Với cooldown 5s thì quãng chờ vô duyên đó dài tới mức không thể không để ý.
        // Bắn ngay lúc vào tầm vẫn công bằng: người chơi còn cả animation vung tay + thời gian đạn bay + vệt
        // cảnh báo điểm rơi để né.
        nextAttackTime = 0f;

        SetRunAnimation(false);
    }

    private void OnDisable()
    {
        isAttacking = false;
    }

    protected override void Update()
    {
        if (player == null) return;

        // Đang vận đòn thì đứng im chờ coroutine nhả đạn, không đuổi cũng không bắn chồng lên
        if (isAttacking) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);

        if (distanceToPlayer > stopRadius)
        {
            MoveToPlayer();
            SetRunAnimation(true);
            return;
        }

        FlipEnemy();
        SetRunAnimation(false);

        if (Time.time >= nextAttackTime) StartCoroutine(ShootRoutine());
    }

    // Animation chạy TRƯỚC, hết mới nhả đạn. Thứ tự này là cả điểm mấu chốt: bắn ngay lúc kích hoạt trigger thì
    // đạn bay ra trước cả khi con quái kịp vung tay, nhìn như bị lỗi.
    private IEnumerator ShootRoutine()
    {
        isAttacking = true;
        nextAttackTime = Time.time + attackCoolDown;

        if (animator != null && !string.IsNullOrEmpty(attackTriggerName)) animator.SetTrigger(attackTriggerName);

        yield return new WaitForSeconds(shootAnimationDelay);

        // Khóa điểm rơi tại ĐÂY, tức ngay lúc nhả đạn. Từ giây phút này vệt cảnh báo hiện ra và không đổi chỗ
        // nữa, nên khoảng thời gian đạn bay (PoisonProjectile.flightDuration) chính là cơ hội né của người chơi.
        // Người chơi có thể đã di chuyển suốt lúc animation chạy - đó là chủ ý, đứng yên vung tay mà vẫn bắn
        // trúng chỗ cũ thì animation chỉ còn là trang trí.
        FireProjectile();

        isAttacking = false;
    }

    private void FireProjectile()
    {
        if (poisonProjectilePrefab == null || player == null) return;

        Vector3 spawnPoint = (firePos != null) ? firePos.position : transform.position;
        Vector3 target = player.transform.position;

        GameObject projectileObj = ObjectPoolManager.Instance != null
            ? ObjectPoolManager.Instance.SpawnObject(poisonProjectilePrefab, spawnPoint, Quaternion.identity)
            : Instantiate(poisonProjectilePrefab, spawnPoint, Quaternion.identity);

        PoisonProjectile projectile = projectileObj.GetComponent<PoisonProjectile>();
        if (projectile != null) projectile.Launch(target);
    }

    private void SetRunAnimation(bool running)
    {
        if (animator == null || string.IsNullOrEmpty(runBoolName)) return;
        animator.SetBool(runBoolName, running);
    }

    protected override void DropItems()
    {
        if (bigXpObject != null)
        {
            int bigCount = UnityEngine.Random.Range(0, 2); // 0-1 viên to
            for (int i = 0; i < bigCount; i++) SpawnItem(bigXpObject);
        }

        if (xpObject != null)
        {
            int smallCount = UnityEngine.Random.Range(1, 3); // 1-2 viên nhỏ
            for (int i = 0; i < smallCount; i++) SpawnItem(xpObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, stopRadius);
    }
}
