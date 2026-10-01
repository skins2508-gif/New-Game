using Platformer.Gameplay;
using UnityEngine;
using static Platformer.Core.Simulation;

namespace Platformer.Mechanics
{
    public class Health : MonoBehaviour
    {
        public int maxHP = 1;
        public int HP = 1;

        public bool IsAlive => HP > 0;

        public void Increment()
        {
            HP = Mathf.Clamp(HP + 1, 0, maxHP);
        }

        public void Decrement()
        {
            HP = Mathf.Clamp(HP - 1, 0, maxHP);

            if (HP <= 0)
            {
                Die();
            }
        }

        public void Die()
        {
            HP = 0;

            var ev = Schedule<HealthIsZero>();
            ev.health = this;
        }
    }
}