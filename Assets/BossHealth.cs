using UnityEngine;
using UnityEngine.UI;

namespace Platformer.Mechanics
{
    public class BossHealth : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 20;
        [SerializeField] private Slider healthSlider;

        [Header("Death Sprite")]
        public Sprite deadSprite;

        [Header("Ending Images")]
        public GameObject[] endingImages;
        [SerializeField] private float endingImageDelay = 1f;

        [Header("Death Effect")]
        [SerializeField] private float blinkDuration = 1f;
        [SerializeField] private float blinkInterval = 0.08f;
        [SerializeField] private float fadeDuration = 1f;

        private int currentHealth;
        private bool isDead;
        private bool isSpriteVisible = true;
        private int endingImageIndex;

        private SpriteRenderer spriteRenderer;
        private Collider2D bossCollider;

        /// <summary>
        /// 컴포넌트 참조를 초기화하고 보스의 체력 및 엔딩 이미지를 초기 상태로 설정합니다.
        /// </summary>
        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            bossCollider = GetComponent<Collider2D>();

            SetEndingImages(false);

            currentHealth = maxHealth;

            if (healthSlider != null)
            {
                healthSlider.maxValue = maxHealth;
                healthSlider.value = currentHealth;
            }
        }

        /// <summary>
        /// 보스에게 대미지를 입히고, 체력이 0 이하가 되면 Die() 메소드를 호출합니다.
        /// </summary>
        /// <param name="damage">입힐 대미지 양</param>
        public void TakeDamage(int damage)
        {
            if (isDead)
                return;

            currentHealth -= damage;
            currentHealth = Mathf.Max(currentHealth, 0);

            if (healthSlider != null)
            {
                healthSlider.value = currentHealth;
            }

            Debug.Log($"보스 피격! 현재 HP: {currentHealth}/{maxHealth}");

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// 보스의 사망 처리를 수행합니다. 콜라이더를 비활성화하고 사망 스프라이트로 교체한 뒤 사망 연출을 시작합니다.
        /// </summary>
        private void Die()
        {
            if (isDead)
                return;

            isDead = true;

            Debug.Log("보스 사망");

            if (bossCollider != null)
            {
                bossCollider.enabled = false;
            }

            if (deadSprite != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = deadSprite;
            }

            StartDeathEffect();
        }

        /// <summary>
        /// 보스 사망 시 발생하는 깜빡임(Blink) 연출을 시작합니다.
        /// </summary>
        private void StartDeathEffect()
        {
            Debug.Log("Invoke 방식 사망 연출 시작");

            InvokeRepeating(nameof(ToggleBossSprite), 0f, blinkInterval);
            Invoke(nameof(StopBlink), blinkDuration);
        }

        /// <summary>
        /// 깜빡임 연출을 위해 보스 스프라이트의 활성화 상태를 토글(반전)합니다.
        /// </summary>
        private void ToggleBossSprite()
        {
            if (spriteRenderer == null)
                return;

            isSpriteVisible = !isSpriteVisible;
            spriteRenderer.enabled = isSpriteVisible;
        }

        /// <summary>
        /// 깜빡임 연출을 종료하고 스프라이트를 활성화한 뒤 페이드아웃 연출을 시작합니다.
        /// </summary>
        private void StopBlink()
        {
            Debug.Log("깜빡임 종료");

            CancelInvoke(nameof(ToggleBossSprite));

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }

            Invoke(nameof(FadeOutBoss), 0f);
        }

        /// <summary>
        /// 보스 이미지의 페이드아웃 연출을 시작합니다. 만약 SpriteRenderer가 없다면 바로 오브젝트 비활성화 단계로 넘어갑니다.
        /// </summary>
        private void FadeOutBoss()
        {
            if (spriteRenderer == null)
            {
                Invoke(nameof(DisableBossGameObject), 0f);
                return;
            }

            Debug.Log("페이드아웃 시작");

            float alphaStep = blinkInterval / fadeDuration;

            InvokeRepeating(nameof(ReduceBossAlpha), 0f, blinkInterval);
            Invoke(nameof(StopFadeOut), fadeDuration);
        }

        /// <summary>
        /// 주기적으로 보스 이미지의 알파(투명도) 값을 감소시킵니다.
        /// </summary>
        private void ReduceBossAlpha()
        {
            if (spriteRenderer == null)
                return;

            Color color = spriteRenderer.color;
            color.a -= blinkInterval / fadeDuration;
            color.a = Mathf.Clamp01(color.a);

            spriteRenderer.color = color;
        }

        /// <summary>
        /// 페이드아웃 연출을 중지하고 알파값을 0으로 고정시킨 후 보스 게임오브젝트를 비활성화합니다.
        /// </summary>
        private void StopFadeOut()
        {
            Debug.Log("페이드아웃 종료");

            CancelInvoke(nameof(ReduceBossAlpha));

            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = 0f;
                spriteRenderer.color = color;
            }

            DisableBossGameObject();
        }

        /// <summary>
        /// 보스 게임오브젝트를 비활성화하고, 엔딩 이미지 연출을 시작하도록 예약합니다.
        /// </summary>
        private void DisableBossGameObject()
        {
            Debug.Log("보스 GameObject SetActive(false) 실행 직전");

            Invoke(nameof(TestInvokeAfterBossDisabled), 1f);

            gameObject.SetActive(false);

            Debug.Log("보스 GameObject SetActive(false) 실행 완료");

            Invoke(nameof(StartEndingImages), 0.5f);
        }

        /// <summary>
        /// 보스가 비활성화된 후에도 Invoke가 계속 실행되는지 테스트하기 위한 메소드입니다.
        /// </summary>
        private void TestInvokeAfterBossDisabled()
        {
            Debug.Log("테스트 성공: 보스가 꺼진 뒤에도 Invoke가 실행됨");
        }

        /// <summary>
        /// 엔딩 이미지를 순차적으로 화면에 표시하는 연출을 시작합니다.
        /// </summary>
        private void StartEndingImages()
        {
            Debug.Log("엔딩 이미지 출력 시작");

            endingImageIndex = 0;
            ShowNextEndingImage();
        }

        /// <summary>
        /// 엔딩 이미지 배열의 현재 인덱스 이미지를 활성화하고, 다음 이미지 표시를 예약합니다.
        /// </summary>
        private void ShowNextEndingImage()
        {
            if (endingImages == null || endingImages.Length == 0)
                return;

            if (endingImageIndex >= endingImages.Length)
                return;

            if (endingImages[endingImageIndex] != null)
            {
                endingImages[endingImageIndex].SetActive(true);
                Debug.Log($"엔딩 이미지 {endingImageIndex + 1}번 표시");
            }

            endingImageIndex++;

            if (endingImageIndex < endingImages.Length)
            {
                Invoke(nameof(ShowNextEndingImage), endingImageDelay);
            }
        }

        /// <summary>
        /// 모든 엔딩 이미지들의 활성화 여부를 일괄적으로 설정합니다.
        /// </summary>
        /// <param name="active">활성화 여부</param>
        private void SetEndingImages(bool active)
        {
            if (endingImages == null)
                return;

            for (int i = 0; i < endingImages.Length; i++)
            {
                if (endingImages[i] != null)
                {
                    endingImages[i].SetActive(active);
                }
            }
        }
    }
}