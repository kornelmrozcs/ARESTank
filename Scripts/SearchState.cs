using System.Linq;
using UnityEngine;

public class SearchState : TankState
{
    public SearchState(A_Smart tank) : base(tank) { }

    public override void Enter()
    {
        Debug.Log("[SearchState] Entered.");
    }

    public override void Execute()
    {
        Debug.Log("[SearchState] Searching for consumables...");

        // Check if there are consumables visible
        if (tank.consumablesFound.Count > 0)
        {
            GameObject closestConsumable = tank.consumablesFound
                .OrderBy(c => c.Value) // Sort by distance
                .First().Key;

            Debug.Log("[SearchState] Moving to consumable: " + closestConsumable.name);
            tank.FollowPathToPoint(closestConsumable, 1f, tank.heuristicMode);
        }

        // Check if health is above threshold and an enemy is visible
        if (tank.GetHealthLevel() >= 25f && tank.enemyTanksFound.Count > 0)
        {
            GameObject target = tank.enemyTanksFound.First().Key;
            Debug.Log("[SearchState] Health restored. Switching back to AttackState targeting: " + target.name);
            tank.ChangeState(new AttackState(tank, target));
            return;
        }

        // If no consumables are found, explore randomly
        if (tank.consumablesFound.Count == 0)
        {
            Debug.Log("[SearchState] No consumables found. Exploring...");
            tank.FollowPathToRandomPoint(1f, tank.heuristicMode);
        }
    }

    public override void Exit()
    {
        Debug.Log("[SearchState] Exiting.");
    }
}
