using System.Linq;
using UnityEngine;

public class ChaseState : TankState
{
    private GameObject target;

    public ChaseState(A_Smart tank, GameObject target) : base(tank)
    {
        this.target = target;
    }

    public override void Enter()
    {
        Debug.Log("[ChaseState] Entered.");
    }

    public override void Execute()
    {
        // Chase the target, move towards the enemy tank
        float distance = Vector3.Distance(tank.transform.position, target.transform.position);
        if (distance > 20f)
        {
            tank.FollowPathToPoint(target, 1f, tank.heuristicMode);
        }
        else
        {
            // Switch to SnipeState to start shooting from close range
            Debug.Log("[ChaseState] Close enough, transitioning to SnipeState.");
            tank.ChangeState(new SnipeState(tank, target));
        }

        // Check for consumables while chasing, prioritize them
        if (tank.consumablesFound.Count > 0)
        {
            GameObject consumable = tank.consumablesFound.First().Key;
            if (consumable != null)
            {
                Debug.Log("[ChaseState] Collecting consumable: " + consumable.name);
                tank.FollowPathToPoint(consumable, 1f, tank.heuristicMode);
                return;
            }
        }

        // If health/fuel/ammo are low, go back to ExploreState
        if (tank.GetHealthLevel() < 20f || tank.GetFuelLevel() < 15f || tank.GetAmmoLevel() < 2)
        {
            Debug.Log("[ChaseState] Health/Fuel/Ammo low. Transitioning to ExploreState.");
            tank.ChangeState(new ExploreState(tank));
        }
    }

    public override void Exit()
    {
        Debug.Log("[ChaseState] Exiting.");
    }
}
