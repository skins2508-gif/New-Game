using UnityEngine;
using UnityEngine.SceneManagement;

namespace Platformer.Mechanics
{
    public class VictoryZone : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D collider)
        {
            var player = collider.GetComponent<PlayerController>();

            if (player != null)
            {
                int nextScene = SceneManager.GetActiveScene().buildIndex + 1;

                if (nextScene < SceneManager.sceneCountInBuildSettings)
                {
                    SceneManager.LoadScene(nextScene);
                }
                else
                {
                    Debug.Log("마지막 스테이지입니다.");
                }
            }
        }
    }
}