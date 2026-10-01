using UnityEngine;
using Platformer.Mechanics;

public class TeleportZone : MonoBehaviour
{
    public Transform target;
    private static bool globalTeleporting;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (globalTeleporting) return;

        PlayerController player = other.GetComponent<PlayerController>();

        if (player != null)
        {
            globalTeleporting = true;
            player.Teleport(target.position);
            Invoke(nameof(ResetTeleport), 0.7f);
        }
    }

    private void ResetTeleport()
    {
        globalTeleporting = false;
    }
}