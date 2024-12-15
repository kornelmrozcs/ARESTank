using System;
using System.Linq;
using UnityEngine;

public class A_AttackStateFSM : A_TankStateFSM
{
    private GameObject target;
    private GameObject enemyBase;
    private float fireDuration = 1f; // Fire for 1 second before moving
    private float fireTimer = 1f;

    public A_AttackStateFSM(A_SmartFSM tank, GameObject target = null) : base(tank)
    {
        this.target = target;
    }

    public override Type Enter()
    {
        Debug.Log("[AttackState] Entered.");
        fireTimer = 0f; // Reset the fire timer
        return null;
    }

    public override Type Execute()
    {
        // Check for enemy base
        enemyBase = tank.enemyBasesFound.FirstOrDefault().Key;

        if (enemyBase != null)
        {
            Debug.Log("[AttackState] Enemy base detected: " + enemyBase.name);
            float baseDistance = Vector3.Distance(tank.transform.position, enemyBase.transform.position);

            if (baseDistance < 40f)
            {
                Debug.Log("[AttackState] Firing at enemy base: " + enemyBase.name);
                tank.TurretFaceWorldPoint(enemyBase);
                tank.FireAtPoint(enemyBase);
                return null; // Prioritize attacking the base
            }
            else
            {
                Debug.Log("[AttackState] Moving closer to enemy base: " + enemyBase.name);
                tank.FollowPathToPoint(enemyBase, 1f, tank.heuristicMode);
                return null; // Prioritize moving toward the base
            }
            //return null;
        }

        // Continue with enemy tank logic
        if (target == null || !tank.enemyTanksFound.ContainsKey(target))
        {
            Debug.Log("[SaerchState] No valid target. Switching to SearchState.");
           
            return typeof(A_SearchStateFSM);
        }

        // Lock the turret onto the target
        tank.TurretFaceWorldPoint(target);

        // Calculate the distance to the target
        float distance = Vector3.Distance(tank.transform.position, target.transform.position);

        if (distance < 50f)
        {
            // Fire at the target
            Debug.Log("[AttackState] Firing at target: " + target.name);
            tank.FireAtPoint(target);

            // Increment the fire timer
            fireTimer += Time.deltaTime;

            if (fireTimer >= fireDuration)
            {
                Debug.Log("[AttackState] Fired for 1 second. Switching to MovingPhaseState.");
                
                return typeof(A_MovingPhaseStateFSM);
            }
        }
        else
        {
            // Move closer to the target if out of range
            Debug.Log("[AttackState] Target out of range. Moving closer to: " + target.name);
            tank.FollowPathToPoint(target, 1f, tank.heuristicMode);

            // Fire while moving closer
            Debug.Log("[AttackState] Firing at target while moving: " + target.name);
            tank.FireAtPoint(target);
            return null;
        }

        System.Random randomRetreat = new System.Random();

        // Rarely retreat if health is critically low
        if (tank.GetHealthLevel() < 12 && randomRetreat.Next(20) < 5)
        {
            Debug.Log("[AttackState] Low health detected. Retreating to SearchState.");
            
            return typeof(A_RetreatStateFSM);
        }
        return null;
    }

    public override Type Exit()
    {
        Debug.Log("[AttackState] Exiting.");
        return null;
    }
}
