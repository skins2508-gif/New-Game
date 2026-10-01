using UnityEngine;

public class Mushroom : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        SpriteRenderer sr = other.GetComponent<SpriteRenderer>();

        if (sr != null)
        {
            sr.color = Color.red;
        }

        Destroy(gameObject);
    }
}