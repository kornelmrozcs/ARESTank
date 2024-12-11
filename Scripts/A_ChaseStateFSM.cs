using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class A_ChaseStateFSM : A_TankStateFSM
{
    public A_ChaseStateFSM(A_SmartFSM tank) : base(tank)
    {
    }

    public override Type Enter()
    {
        return null;
    }

    public override Type Execute()
    {
        // Transition to AttackState if an enemy is found
        if (tank.enemyTanksFound.Count > 0)
        {
            GameObject target = tank.enemyTanksFound.First().Key; // Get the first visible enemy
            if (target != null)
            {
                // Move towards the base and attack if in range
                if (Vector3.Distance(tank.transform.position, target.transform.position) < 25f)
                {
                    Debug.Log("[Chase] Changing to attack enemy : " + target.name);
                    return typeof(A_AttackStateFSM);
                }
                else
                {
                    Debug.Log("[Attack] Moving closer to enemy: " + target.name);
                    tank.FollowPathToPoint(target, 1f, tank.heuristicMode);
                }
            }
        }
        else
        {
            return typeof(A_SearchStateFSM);
        }
        return null;
    }

    public override Type Exit()
    {
        return null;
    }
}
