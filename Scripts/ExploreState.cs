using System.Linq;
using UnityEngine;

public class ExploreState : TankState
{
    private float explorationTimer = 0f; // Timer for regenerating random points
    private const float explorationDuration = 5f; // Time before generating a new point

    public ExploreState(A_Smart tank) : base(tank) { }

    public override void Enter()
    {
        Debug.Log("[ExploreState] Entered.");
    }

    public override void Execute()
    {
        Debug.Log("[ExploreState] Scanning and exploring...");

        // Prioritize consumables if health, ammo, or fuel is low
        if (tank.GetHealthLevel() < 50 || tank.GetAmmoLevel() < 3 || tank.GetFuelLevel() < 20)
        {
            Debug.Log("[ExploreState] Searching for consumables...");
            GameObject closestConsumable = tank.consumablesFound
                .OrderBy(c => c.Value) // Sort by distance
                .Select(c => c.Key)
                .FirstOrDefault();

            if (closestConsumable != null)
            {
                Debug.Log("[ExploreState] Moving towards consumable: " + closestConsumable.name);
                tank.FollowPathToPoint(closestConsumable, 1f, tank.heuristicMode);
                return;
            }
        }

        // Perform exploration behavior (random point generation)
        explorationTimer += Time.deltaTime;

        if (explorationTimer >= explorationDuration)
        {
            Debug.Log("[ExploreState] Generating new random exploration point...");
            tank.FollowPathToRandomPoint(1f, tank.heuristicMode);
            explorationTimer = 0f; // Reset the timer
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

        // Debug if no consumables or enemies are found
        if (tank.enemyTanksFound.Count == 0 && tank.consumablesFound.Count == 0)
        {
            Debug.Log("[ExploreState] No visible threats or resources. Continuing exploration...");
        }
    }

    public override void Exit()
    {
        Debug.Log("[ExploreState] Exiting.");
    }
}
