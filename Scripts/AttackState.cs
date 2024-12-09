using System.Linq;
using UnityEngine;

public class AttackState : TankState
{
    private GameObject target; // The target to attack

    public AttackState(A_Smart tank, GameObject target) : base(tank)
    {
        this.target = target;
    }

    public override void Enter()
    {
        Debug.Log("[AttackState] Entered. Target: " + target.name);
    }

    public override void Execute()
    {
        // Check if the target is still valid
        if (target == null || !tank.enemyTanksFound.ContainsKey(target))
        {
            Debug.Log("[AttackState] Lost target. Switching to ExploreState.");
            tank.ChangeState(new ExploreState(tank));
            return;
        }

        // Move towards the target and attack
        float distance = Vector3.Distance(tank.transform.position, target.transform.position);
        if (distance < 25f)
        {
            Debug.Log("[AttackState] In range. Attacking target: " + target.name);
            tank.TurretFireAtPoint(target);
        }
        else
        {
            Debug.Log("[AttackState] Target out of range. Moving closer to: " + target.name);
            tank.FollowPathToPoint(target, 1f, tank.heuristicMode);
        }
    }

    public override void Exit()
    {
        Debug.Log("[AttackState] Exiting. Ceasing attack on target: " + target?.name);
    }
}

