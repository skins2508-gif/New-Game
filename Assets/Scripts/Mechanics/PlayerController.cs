using Platformer.Core;
using Platformer.Gameplay;
using Platformer.Model;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using static Platformer.Core.Simulation;

namespace Platformer.Mechanics
{
    /// <summary>
    /// This is the main class used to implement control of the player.
    /// </summary>
    public class PlayerController : KinematicObject
    {
        public Animator llamaAnimator;
        public AudioClip jumpAudio;
        public AudioClip respawnAudio;
        public AudioClip ouchAudio;

        public float maxSpeed = 7;
        public float jumpTakeOffSpeed = 7;

        [Header("Dash")]
        [SerializeField] private float dashSpeed = 35f;
        [SerializeField] private float dashDuration = 0.15f;
        [SerializeField] private float dashCooldown = 0.35f;

        public JumpState jumpState = JumpState.Grounded;
        private bool stopJump;
        public Collider2D collider2d;
        public AudioSource audioSource;
        public Health health;
        public bool controlEnabled = true;

        bool jump;
        Vector2 move;
        private bool isDashing;
        private Vector2 dashDirection = Vector2.right;
        private float dashEndTime;
        private float nextDashTime;
        SpriteRenderer spriteRenderer;
        internal Animator animator;
        readonly PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        private InputAction m_MoveAction;
        private InputAction m_JumpAction;

        public Bounds Bounds => collider2d.bounds;

        // === 펫 추적용 상태 히스토리 ===
        public List<PlayerState> stateHistory = new List<PlayerState>();
        [SerializeField] private float historyDuration = 0.5f;
        // =============================

        void Awake()
        {
            health = GetComponent<Health>();
            audioSource = GetComponent<AudioSource>();
            collider2d = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();

            if (model != null) model.player = this;

            m_MoveAction = InputSystem.actions.FindAction("Player/Move");
            m_JumpAction = InputSystem.actions.FindAction("Player/Jump");
            InputSystem.actions.FindAction("Player/Move");

            m_MoveAction?.Enable();
            m_JumpAction?.Enable();

            if (collider2d == null) Debug.LogWarning("[PlayerController] Collider2D is missing.");
            if (health == null) Debug.LogWarning("[PlayerController] Health is missing.");
            if (audioSource == null) Debug.LogWarning("[PlayerController] AudioSource is missing.");
        }

        protected override void Update()
        {
            if (controlEnabled)
            {
                move.x = m_MoveAction != null ? m_MoveAction.ReadValue<Vector2>().x : 0f;

                if (jumpState == JumpState.Grounded && m_JumpAction != null && m_JumpAction.WasPressedThisFrame())
                {
                    jumpState = JumpState.PrepareToJump;
                }
                else if (m_JumpAction != null && m_JumpAction.WasReleasedThisFrame())
                {
                    stopJump = true;
                    Schedule<PlayerStopJump>().player = this;
                }

                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    TryStartDash();
                }
            }
            else
            {
                move.x = 0;
                isDashing = false;
            }

            UpdateJumpState();
            base.Update();
        }

        private void TryStartDash()
        {
            if (isDashing || Time.time < nextDashTime)
                return;

            if (Mouse.current != null && Camera.main != null)
            {
                Vector3 mouseScreenPosition = Mouse.current.position.ReadValue();
                Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPosition);
                Vector2 mouseDirection = mouseWorldPosition - transform.position;

                if (mouseDirection.sqrMagnitude > 0.0001f)
                {
                    dashDirection = mouseDirection.normalized;
                }
            }
            else if (Mathf.Abs(move.x) > 0.01f)
            {
                dashDirection = Vector2.right * Mathf.Sign(move.x);
            }
            else if (spriteRenderer != null)
            {
                dashDirection = spriteRenderer.flipX ? Vector2.left : Vector2.right;
            }

            isDashing = true;

            dashEndTime = Time.time + dashDuration;
            nextDashTime = Time.time + dashCooldown;
        }

        void UpdateJumpState()
        {
            jump = false;
            switch (jumpState)
            {
                case JumpState.PrepareToJump:
                    jumpState = JumpState.Jumping;
                    jump = true;
                    stopJump = false;
                    break;
                case JumpState.Jumping:
                    if (!IsGrounded)
                    {
                        Schedule<PlayerJumped>().player = this;
                        jumpState = JumpState.InFlight;
                    }
                    break;
                case JumpState.InFlight:
                    if (IsGrounded)
                    {
                        Schedule<PlayerLanded>().player = this;
                        jumpState = JumpState.Landed;
                    }
                    break;
                case JumpState.Landed:
                    jumpState = JumpState.Grounded;
                    break;
            }
        }

        protected override void ComputeVelocity()
        {
            if (jump && IsGrounded)
            {
                velocity.y = jumpTakeOffSpeed * model.jumpModifier;
                jump = false;
            }
            else if (stopJump)
            {
                stopJump = false;
                if (velocity.y > 0)
                {
                    velocity.y = velocity.y * model.jumpDeceleration;
                }
            }

            if (move.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (move.x < -0.01f)
                spriteRenderer.flipX = true;

            if (animator != null)
            {
                animator.SetBool("grounded", IsGrounded);
                animator.SetFloat("velocityX", Mathf.Abs(velocity.x) / Mathf.Max(0.0001f, maxSpeed));
            }

            if (isDashing)
            {
                if (Time.time >= dashEndTime)
                {
                    isDashing = false;
                }
                else
                {
                    targetVelocity = Vector2.right * dashDirection.x * dashSpeed;
                    velocity.y = dashDirection.y * dashSpeed;
                    return;
                }
            }

            targetVelocity = move * maxSpeed;
        }

        void LateUpdate()
        {
            var state = new PlayerState
            {
                Timestamp = Time.time,
                Position = transform.position,
                IsFlipped = spriteRenderer != null && spriteRenderer.flipX,
                VelocityX = Mathf.Abs(velocity.x) / Mathf.Max(0.0001f, maxSpeed),
                IsGrounded = IsGrounded
            };
            stateHistory.Add(state);

            float cutoff = Time.time - historyDuration;
            stateHistory.RemoveAll(s => s.Timestamp < cutoff);
        }

        private void OnParticleCollision(GameObject other)
        {
            Debug.Log("플레이어가 파티클에 맞아서 사망 이벤트 실행");

            if (health != null && health.IsAlive)
            {
                Schedule<PlayerDeath>();
            }
        }




        public enum JumpState
        {
            Grounded,
            PrepareToJump,
            Jumping,
            InFlight,
            Landed
        }
    }
}
