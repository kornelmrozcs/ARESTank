using System;
using System.Linq;
using UnityEngine;

public class A_SearchStateFSM : A_TankStateFSM
{
    private float explorationTimer = 0f;
    private const float maxExplorationTime = 6f; // Time before generating a new random point

    public A_SearchStateFSM(A_SmartFSM tank) : base(tank) { }

    public override Type Enter()
    {
        Debug.Log("[SearchState] Entered.");
        explorationTimer = 0f;

        return null;
    }

    public override Type Execute()
    {
        Debug.Log("[SearchState] Scanning and exploring...");

        // Prioritize collecting consumables if any are visible
        if (tank.consumablesFound.Count > 0)
        {
            GameObject consumable = tank.consumablesFound.First().Key; // Get the first visible consumable
            if (consumable != null)
            {
                Debug.Log("[SearchState] Collecting visible consumable: " + consumable.name);
                tank.FollowPathToPoint(consumable, 1f, tank.heuristicMode);
                return null; // Skip exploration and attacking to collect the consumable
            }
        }

        // Check if an enemy base is found
        if (tank.enemyBasesFound.Count > 0)
        {
            GameObject enemyBase = tank.enemyBasesFound.First().Key; // Get the first visible enemy base
            if (enemyBase != null)
            {
                Debug.Log("[SearchState] Enemy base found. Moving to attack base: " + enemyBase.name);

                // Move towards the base and attack if in range
                if (Vector3.Distance(tank.transform.position, enemyBase.transform.position) < 25f)
                {
                    Debug.Log("[SearchState] Attacking enemy base: " + enemyBase.name);
                    tank.FireAtPoint(enemyBase);
                }
                else
                {
                    Debug.Log("[SearchState] Moving closer to enemy base: " + enemyBase.name);
                    tank.FollowPathToPoint(enemyBase, 1f, tank.heuristicMode);
                }

                return null; // Focus on the base and stop other behaviors
            }
        }

        // Continue exploration behavior
        explorationTimer += Time.deltaTime;
        if (explorationTimer >= maxExplorationTime)
        {
            Debug.Log("[SearchState] Generating new random exploration point...");
            tank.FollowPathToRandomPoint(1f, tank.heuristicMode); // Corrected method call
            explorationTimer = 0f; // Reset exploration timer
        }

        tank.FollowPathToRandomPoint(1f, tank.heuristicMode);

        // Transition to AttackState if an enemy is found
        if (tank.enemyTanksFound.Count > 0)
        {
            GameObject target = tank.enemyTanksFound.First().Key; // Get the first visible enemy
            if (target != null)
            {
                Debug.Log("[SearchState] Enemies found. Switching to ChaseState targeting: " + target.name);
                
                return typeof(A_ChaseStateFSM);
            }
        }
        return null;
    }

    public override Type Exit()
    {
        Debug.Log("[SearchState] Exiting.");
        return null;
    }
}