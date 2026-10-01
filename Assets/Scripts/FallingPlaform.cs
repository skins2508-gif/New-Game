using System.Collections;
using UnityEngine;

public class FallingPlatform : MonoBehaviour
{
    public float fallDelay = 0.3f;
    public float fallGravity = 8f;

    // 이 Y값보다 아래로 내려가면 다시 생성
    public float resetY = -10f;

    // 떨어진 뒤 다시 생기기까지 시간
    public float respawnDelay = 1f;

    private Rigidbody2D rb;
    private bool isFalling = false;
    private bool isResetting = false;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // 처음 있던 위치 저장
        startPosition = transform.position;
        startRotation = transform.rotation;

        ResetPlatform();
    }

    private void Update()
    {
        // 박스가 아래로 떨어졌으면 다시 원래 자리로 복귀
        if (!isResetting && transform.position.y < resetY)
        {
            StartCoroutine(RespawnPlatform());
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isFalling) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            StartCoroutine(Fall());
        }
    }

    private IEnumerator Fall()
    {
        isFalling = true;

        yield return new WaitForSeconds(fallDelay);

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = fallGravity;
    }

    private IEnumerator RespawnPlatform()
    {
        isResetting = true;

        yield return new WaitForSeconds(respawnDelay);

        ResetPlatform();

        isResetting = false;
    }

    public void ResetPlatform()
    {
        isFalling = false;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        transform.position = startPosition;
        transform.rotation = startRotation;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
    }
}