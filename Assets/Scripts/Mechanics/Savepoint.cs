using UnityEngine;

public class Savepoint : MonoBehaviour
{
    public static Savepoint Instance;

    private Vector3 spawnPoint;

    private void Awake()
    {
        Instance = this;
    }

    public void SetSpawnPoint(Vector3 pos)
    {
        spawnPoint = pos;
    }

    public Vector3 GetSpawnPoint()
    {
        return spawnPoint;
    }
}