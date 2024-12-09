using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CollectState : TankState
{
    private GameObject targetConsumable; // The target collectible item
    private float searchTimer = 0f;
    private const float maxSearchTime = 10f;

    public CollectState(A_Smart tank) : base(tank) { }

    public override void Enter()
    {
        Debug.Log("[CollectState] Entered.");
        searchTimer = 0f;
        // Check if there is already a consumable in sight
        SetTargetConsumable();
    }

    public override void Execute()
    {
        searchTimer += Time.deltaTime;

        // If a target consumable exists, move toward it
        if (targetConsumable != null)
        {
            Debug.Log($"[CollectState] Moving toward consumable: {targetConsumable.name}");
            tank.FollowPathToPoint(targetConsumable, 1f, tank.heuristicMode);

            // If the consumable is no longer visible, reset the target
            if (!tank.consumablesFound.ContainsKey(targetConsumable))
            {
                Debug.Log("[CollectState] Lost sight of consumable. Searching again...");
                targetConsumable = null;
            }
        }
        else
        {
            // If no consumable is found within the max search time, explore
            if (searchTimer >= maxSearchTime)
            {
                Debug.Log("[CollectState] No consumables found. Switching to ExploreState.");
                tank.ChangeState(new ExploreState(tank));
                return;
            }

            // Re-check for visible consumables
            SetTargetConsumable();
        }

        // Transition to AttackState if an enemy is found
        if (tank.enemyTanksFound.Count > 0)
        {
            var enemyTarget = tank.enemyTanksFound.First().Key;
            Debug.Log($"[CollectState] Enemy detected. Switching to AttackState: {enemyTarget.name}");
            tank.ChangeState(new AttackState(tank, enemyTarget));
            return;
        }
    }

    public override void Exit()
    {
        Debug.Log("[CollectState] Exiting.");
    }

    private void SetTargetConsumable()
    {
        if (tank.consumablesFound.Count > 0)
        {
            targetConsumable = tank.consumablesFound.First().Key;
            Debug.Log($"[CollectState] Targeting consumable: {targetConsumable.name}");
        }
    }
}
