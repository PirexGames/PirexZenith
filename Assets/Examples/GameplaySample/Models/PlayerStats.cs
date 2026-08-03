using UnityEngine;

namespace PirexZenith.Examples.GameplaySample
{
    /// <summary>
    /// Player statistics data model.
    /// Demonstrates Transient registration — every dependency resolution request produces a distinct new instance.
    /// </summary>
    public class PlayerStats
    {
        public int Health { get; private set; }
        public int Energy { get; private set; }
        public float MoveSpeed { get; private set; }

        public PlayerStats()
        {
            Health = 100;
            Energy = 50;
            MoveSpeed = 5.5f;
            Debug.Log("[PirexZenith Example] PlayerStats initialized (Transient Instance Created).");
        }

        public void TakeDamage(int damage)
        {
            Health = Mathf.Max(0, Health - damage);
            Debug.Log($"[PirexZenith Example - PlayerStats] Took {damage} damage. Remaining health: {Health}");
        }
    }
}
