using System.Linq;
using UnityEngine;

public class AttackState : TankState
{
    public AttackState(A_Smart tank) : base(tank) { }

    public override void Enter()
    {
        Debug.Log("[AttackState] Entered.");
    }

    public override void Execute()
    {
        if (tank.enemyTanksFound.Count > 0)
        {
            GameObject target = tank.enemyTanksFound.First().Key;

            if (target != null)
            {
                if (Vector3.Distance(tank.transform.position, target.transform.position) < 25f)
                {
                    Debug.Log("[AttackState] Firing at enemy tank.");
                    tank.FireAtPoint(target); // Use the public wrapper method
                    tank.ChangeState(new SearchState(tank)); // Transition to SearchState
                }
                else
                {
                    Debug.Log("[AttackState] Moving towards enemy tank.");
                    tank.FollowPathToPoint(target, 1f, tank.heuristicMode); // Already fixed with a wrapper if needed
                }
            }
        }
        else
        {
            Debug.Log("[AttackState] No enemies found. Exploring...");
            tank.ChangeState(new ExploreState(tank)); // Transition to ExploreState
        }
    }

    public override void Exit()
    {
        Debug.Log("[AttackState] Exiting.");
    }
}

