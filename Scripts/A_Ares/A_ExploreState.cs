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

        // Check if the enemy tank can see the current tank but the current tank cannot see it
        if (tank.enemyTanksFound.Count == 0 && tank.enemyTanksFound.Any(x => !x.Key.activeSelf))
        {
            Debug.Log("[ExploreState] Enemy tank detected us. Returning to AttackState.");
            // Switch to attack state if an enemy is detected and we don't see them
            tank.ChangeState(new ChaseState(tank, tank.enemyTanksFound.FirstOrDefault().Key));
            return;
        }

        // Prioritize enemies over bases and consumables
        if (tank.enemyTanksFound.Count > 0)
        {
            GameObject target = tank.enemyTanksFound.First().Key; // Get the first visible enemy
            if (target != null)
            {
                // Check DumbTank's health for transition to ChaseState or SnipeState
                if (target.GetComponent<DumbTank>().TankCurrentHealth <= 30f)
                {
                    Debug.Log("[ExploreState] DumbTank's health is low. Transitioning to ChaseState.");
                    tank.ChangeState(new ChaseState(tank, target));
                    return;
                }

                Debug.Log("[ExploreState] Enemy detected. Switching to SnipeState: " + target.name);
                tank.ChangeState(new DodgingState(tank, target)); // Engage the enemy
                return;
            }
        }

        // Prioritize consumables over enemy bases
        if (tank.consumablesFound.Count > 0)
        {
            GameObject consumable = tank.consumablesFound.First().Key; // Get the first visible consumable
            if (consumable != null)
            {
                Debug.Log("[ExploreState] Collecting visible consumable: " + consumable.name);
                tank.FollowPathToPoint(consumable, 1f, tank.heuristicMode);
                return;
            }
        }

        // If no consumables, check for enemy bases
        if (tank.enemyBasesFound.Count > 0)
        {
            GameObject enemyBase = tank.enemyBasesFound.First().Key; // Get the first visible enemy base
            if (enemyBase != null)
            {
                Debug.Log("[ExploreState] Enemy base detected. Moving to attack base: " + enemyBase.name);

                // Move towards the base and attack if in range
                if (Vector3.Distance(tank.transform.position, enemyBase.transform.position) < 25f)
                {
                    Debug.Log("[ExploreState] Attacking enemy base: " + enemyBase.name);
                    tank.FireAtPoint(enemyBase);
                }
                else
                {
                    Debug.Log("[ExploreState] Moving closer to enemy base: " + enemyBase.name);
                    tank.FollowPathToPoint(enemyBase.transform.position, 1f, tank.heuristicMode);
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
    }

    public override void Exit()
    {
        Debug.Log("[ExploreState] Exiting.");
    }
}
