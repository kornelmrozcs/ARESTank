using System.Linq;
using UnityEngine;

public class ExploreState : TankState
{
    public ExploreState(A_Smart tank) : base(tank) { }

    public override void Enter()
    {
        Debug.Log("[ExploreState] Entered.");
    }

    public override void Execute()
    {
        Debug.Log("[ExploreState] Scanning and exploring...");

        // Perform exploration behavior
        tank.FollowPathToRandomPoint(1f, tank.heuristicMode);

        // Transition to AttackState if an enemy is found
        if (tank.enemyTanksFound.Count > 0)
        {
            GameObject target = tank.enemyTanksFound.First().Key; // Get the first visible enemy
            Debug.Log("[ExploreState] Enemies detected! Switching to AttackState targeting: " + target.name);
            tank.ChangeState(new AttackState(tank, target)); // Pass the target GameObject to AttackState
            return;
        }
    }

    public override void Exit()
    {
        Debug.Log("[ExploreState] Exiting.");
    }
}
