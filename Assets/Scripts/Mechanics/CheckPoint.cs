using Platformer.Core;
using Platformer.Model;
using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    private PlatformerModel model;
    private bool activated = false;

    private void Awake()
    {
        model = Simulation.GetModel<PlatformerModel>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activated)
            return;

        if (!other.CompareTag("Player"))
            return;

        activated = true;

        model.spawnPoint.transform.position = transform.position;

        Debug.Log("체크포인트 저장 완료!");
    }
}