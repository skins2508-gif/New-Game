using System.Collections;
using System.Linq;
using Platformer.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;
using static Platformer.Core.Simulation;

namespace Platformer.Mechanics
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class PetController : MonoBehaviour
    {
        [Header("Follow")]
        public PlayerController player;
        [Min(0f)] public float delay = 0.1f;
        public Vector3 playerPositionOffset = new Vector3(-1f, 0f, 0f);

        [Header("Flying Attack")]
        [SerializeField, Min(0.1f)] private float detectionRange = 12f;
        [SerializeField, Min(0f)] private float hitDistance = 0.15f;
        [SerializeField, Min(0f)] private float attackCooldown = 0.25f;
        [SerializeField] private float spinSpeed = 1080f;
        [SerializeField, Min(1)] private int damage = 1;

        private Animator animator;
        private Collider2D attackCollider;
        private Coroutine attackCoroutine;
        private bool isAttacking;
        private bool dealtDamage;
        private float nextAttackTime;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            attackCollider = GetComponent<Collider2D>();
            attackCollider.isTrigger = true;
            attackCollider.enabled = false;

            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
            }

            if (player == null)
            {
                Debug.LogError("[PetController] PlayerController를 찾을 수 없습니다.");
                enabled = false;
            }
        }

        private void Update()
        {
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                Attack();
            }
        }

        private void LateUpdate()
        {
            if (!enabled || player == null || isAttacking)
                return;

            FollowDelayedPlayer();
        }

        private void FollowDelayedPlayer()
        {
            if (player.stateHistory == null || player.stateHistory.Count == 0)
            {
                transform.position = player.transform.position + playerPositionOffset;
                return;
            }

            float targetTime = Time.time - delay;
            PlayerState past = player.stateHistory.LastOrDefault(state => state.Timestamp <= targetTime);
            ApplyState(past.Timestamp > 0f ? past : player.stateHistory[0]);
        }

        private void ApplyState(PlayerState playerState)
        {
            transform.position = playerState.Position + playerPositionOffset;

            if (animator != null)
            {
                animator.SetFloat("velocityX", playerState.VelocityX);
                animator.SetBool("grounded", playerState.IsGrounded);
            }
        }

        public void Attack()
        {
            if (player == null || isAttacking || Time.time < nextAttackTime)
                return;

            if (!player.controlEnabled || (player.health != null && !player.health.IsAlive))
                return;

            Transform target = FindNearestTarget();
            if (target == null)
            {
                Debug.Log("펫의 공격 범위 안에 적이 없습니다.");
                return;
            }

            attackCoroutine = StartCoroutine(AttackRoutine(target));
        }

        private Transform FindNearestTarget()
        {
            Transform nearest = null;
            float nearestSqrDistance = detectionRange * detectionRange;
            Vector3 origin = transform.position;

            EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
            foreach (EnemyController enemy in enemies)
            {
                Health health = enemy.GetComponent<Health>();
                if (!enemy.isActiveAndEnabled || (health != null && !health.IsAlive))
                    continue;

                float sqrDistance = (enemy.transform.position - origin).sqrMagnitude;
                if (sqrDistance <= nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    nearest = enemy.transform;
                }
            }

            BossHealth[] bosses = FindObjectsByType<BossHealth>(FindObjectsSortMode.None);
            foreach (BossHealth boss in bosses)
            {
                if (!boss.isActiveAndEnabled)
                    continue;

                float sqrDistance = (boss.transform.position - origin).sqrMagnitude;
                if (sqrDistance <= nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    nearest = boss.transform;
                }
            }

            return nearest;
        }

        private IEnumerator AttackRoutine(Transform target)
        {
            isAttacking = true;
            dealtDamage = false;
            attackCollider.enabled = true;

            while (target != null && !dealtDamage)
            {
                Vector3 targetPosition = GetTargetPoint(target);
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    GetFlightSpeed() * Time.deltaTime);
                transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);

                if (Vector2.Distance(transform.position, targetPosition) <= hitDistance)
                {
                    DealDamage(target);
                }

                yield return null;
            }

            attackCollider.enabled = false;

            while (player != null)
            {
                Vector3 homePosition = player.transform.position + playerPositionOffset;
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    homePosition,
                    GetFlightSpeed() * Time.deltaTime);
                transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);

                if (Vector2.Distance(transform.position, homePosition) <= 0.05f)
                    break;

                yield return null;
            }

            transform.rotation = Quaternion.identity;
            isAttacking = false;
            attackCoroutine = null;
            nextAttackTime = Time.time + attackCooldown;
        }

        private float GetFlightSpeed()
        {
            return player != null ? Mathf.Max(0.1f, player.maxSpeed) : 0.1f;
        }

        private static Vector3 GetTargetPoint(Transform target)
        {
            Collider2D targetCollider = target.GetComponentInChildren<Collider2D>();
            return targetCollider != null ? targetCollider.bounds.center : target.position;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isAttacking || dealtDamage)
                return;

            DealDamage(other.transform);
        }

        private void DealDamage(Transform hitTransform)
        {
            if (dealtDamage || hitTransform == null)
                return;

            BossHealth boss = hitTransform.GetComponentInParent<BossHealth>();
            if (boss != null)
            {
                boss.TakeDamage(damage);
                dealtDamage = true;
                return;
            }

            EnemyController enemy = hitTransform.GetComponentInParent<EnemyController>();
            if (enemy == null)
                return;

            Health health = enemy.GetComponent<Health>();
            if (health == null || !health.IsAlive)
                return;

            for (int i = 0; i < damage && health.IsAlive; i++)
            {
                health.Decrement();
            }

            if (!health.IsAlive)
            {
                Schedule<EnemyDeath>().enemy = enemy;
            }

            dealtDamage = true;
        }

        private void OnDisable()
        {
            if (attackCoroutine != null)
            {
                StopCoroutine(attackCoroutine);
                attackCoroutine = null;
            }

            if (attackCollider != null)
                attackCollider.enabled = false;

            isAttacking = false;
            transform.rotation = Quaternion.identity;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.75f, 0.2f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
    }
}
