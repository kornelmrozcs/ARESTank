using System;
using System.Linq;
using UnityEngine;

public class A_AmbushStateFSM : A_TankStateFSM
{
    private GameObject target;
    private float fireDuration = 1f; // Fire for 1 second before moving
    private float fireTimer = 1f;

    public float ambushTimeout = 0f;

    public A_AmbushStateFSM(A_SmartFSM tank, GameObject target = null) : base(tank)
    {
        this.target = target;
    }

    public override Type Enter()
    {
        Debug.Log("[AmbushState] Entered.");
        fireTimer = 0f; // Reset the fire timer
        return null;
    }

    public override Type Execute()
    {
        // Continue with enemy tank logic
        if (target == null || !tank.enemyTanksFound.ContainsKey(target))
        {
            Debug.Log("[SearchState] No valid target. Switching to SaerchState.");

            return typeof(A_SearchStateFSM);
        }

        // Lock the turret onto the target
        tank.TurretFaceWorldPoint(target);

        // Calculate the distance to the target
        float distance = Vector3.Distance(tank.transform.position, target.transform.position);

        // Increment the ambush timer (when time out start moving)
        ambushTimeout += Time.deltaTime;

        if (distance < 50f)
        {
            // Fire at the target
            Debug.Log("[AmbushState] Firing at target: " + target.name);
            tank.FireAtPoint(target);

            // Increment the fire timer
            fireTimer += Time.deltaTime;
            
            System.Random randomMove = new System.Random();
             
            if ((fireTimer >= fireDuration && randomMove.Next(20) < 5) || (ambushTimeout > 30f))
            {
                Debug.Log("[AmbushState] Fired for 1 second. Switching to MovingPhaseState.");

                return typeof(A_MovingPhaseStateFSM);
            }
        }
        else
        {
            if (ambushTimeout > 50f)
            {
                Debug.Log("[AmbushState] Timeout. Switching to SearchPhaseState.");

                return typeof(A_SearchStateFSM);
            }
            else 
            {
                // Waiting to be closer to the target if out of range
                Debug.Log("[AmbushState] Target out of range. Waiting to be closer to: " + target.name);

                // Fire while moving closer
                Debug.Log("[AmbushState] Firing at target while moving: " + target.name);
                tank.FireAtPoint(target);
            }

            return null;
        }

        System.Random randomRetreat = new System.Random();

        // Rarely retreat if health is critically low
        if (tank.GetHealthLevel() < 20 && randomRetreat.Next(20) < 10)
        {
            Debug.Log("[AmbushState] Low health detected. Retreating to SearchState.");

            return typeof(A_SearchStateFSM);
        }
        return null;
    }

    public override Type Exit()
    {
        Debug.Log("[AmbushState] Exiting.");
        return null;
    }
}
