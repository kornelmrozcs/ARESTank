using System.Linq;
using UnityEngine;

public class ExploreState : TankState
{
    private float explorationTimer = 0f;
    private const float maxExplorationTime = 10f; // Time before generating a new random point

    public ExploreState(A_Smart tank) : base(tank) { }

    public override void Enter()
    {
        Debug.Log("[ExploreState] Entered.");
        explorationTimer = 0f;
    }

    public override void Execute()
    {
        Debug.Log("[ExploreState] Scanning and exploring...");

        // Prioritize collecting consumables if any are visible
        if (tank.consumablesFound.Count > 0)
        {
            GameObject consumable = tank.consumablesFound.First().Key; // Get the first visible consumable
            if (consumable != null)
            {
                Debug.Log("[ExploreState] Collecting visible consumable: " + consumable.name);
                tank.FollowPathToPoint(consumable, 1f, tank.heuristicMode);
                return; // Skip exploration and attacking to collect the consumable
            }
        }

        // Check if an enemy base is found
        if (tank.enemyBasesFound.Count > 0)
        {
            GameObject enemyBase = tank.enemyBasesFound.First().Key; // Get the first visible enemy base
            if (enemyBase != null)
            {
                Debug.Log("[ExploreState] Enemy base found. Moving to attack base: " + enemyBase.name);

                // Move towards the base and attack if in range
                if (Vector3.Distance(tank.transform.position, enemyBase.transform.position) < 25f)
                {
                    Debug.Log("[ExploreState] Attacking enemy base: " + enemyBase.name);
                    tank.FireAtPoint(enemyBase);
                }
                else
                {
                    Debug.Log("[ExploreState] Moving closer to enemy base: " + enemyBase.name);
                    tank.FollowPathToPoint(enemyBase, 1f, tank.heuristicMode);
                }

                return; // Focus on the base and stop other behaviors
            }
        }

        // Continue exploration behavior
        explorationTimer += Time.deltaTime;
        if (explorationTimer >= maxExplorationTime)
        {
            Debug.Log("[ExploreState] Generating new random exploration point...");
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
                Debug.Log("[ExploreState] Enemies found. Switching to AttackState targeting: " + target.name);
                tank.ChangeState(new AttackState(tank, target)); // Pass the target GameObject to AttackState
                return;
            }
        }
    }

    public override void Exit()
    {
        Debug.Log("[ExploreState] Exiting.");
    }
}