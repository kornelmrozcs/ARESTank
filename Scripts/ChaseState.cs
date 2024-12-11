using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChaseState : TankState
{
    private GameObject target;
    private const float chaseRange = 35f; // Maximum range to start chasing
    private const float snipeRange = 25f; // Range within which we transition to Sniping

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
        // Check if the DumbTank is within sniping range
        float distance = Vector3.Distance(tank.transform.position, target.transform.position);
        if (distance <= snipeRange)
        {
            Debug.Log("[ChaseState] Target within sniping range. Transitioning to SnipeState.");
            tank.ChangeState(new SnipeState(tank, target)); // Transition to SnipeState
            return;
        }

        // Chase the DumbTank if it's outside sniping range
        Debug.Log("[ChaseState] Pursuing target.");
        tank.FollowPathToPoint(target, 1f, tank.heuristicMode);

        // If the DumbTank moves out of range, keep chasing until it's in range for sniping
        if (distance > chaseRange)
        {
            Debug.Log("[ChaseState] Target moved too far. Keeping pursuit.");
        }
    }

    public override void Exit()
    {
        Debug.Log("[ChaseState] Exiting.");
    }
}
