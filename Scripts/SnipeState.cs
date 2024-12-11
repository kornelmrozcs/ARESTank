using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnipeState : TankState
{
    private GameObject target;
    private float snipeTimer = 0f;
    private const float maxSnipeDuration = 4f; // Maximum time for sniping

    public SnipeState(A_Smart tank, GameObject target) : base(tank)
    {
        this.target = target;
    }

    public override void Enter()
    {
        Debug.Log("[SnipeState] Entered.");
        snipeTimer = maxSnipeDuration; // Initialize snipe timer
    }

    public override void Execute()
    {
        // If the DumbTank is still within range
        if (Vector3.Distance(tank.transform.position, target.transform.position) <= 25f)
        {
            // Aim and shoot with predictive targeting
            PredictAndShootAtTarget();
        }
        else
        {
            // If out of range, transition to ChaseState to pursue
            Debug.Log("[SnipeState] Target out of range. Transitioning to ChaseState.");
            tank.ChangeState(new ChaseState(tank, target));
        }
    }

    private void PredictAndShootAtTarget()
    {
        // Predict the position of the DumbTank based on its velocity and direction
        Vector3 predictedPosition = target.transform.position + target.GetComponent<Rigidbody>().velocity * 2f; // Predictive aim (you can adjust the multiplier)

        // Face the turret towards the predicted position
        tank.TurretFaceWorldPoint(new GameObject { transform = { position = predictedPosition } });

        // Fire at the predicted target
        if (!tank.IsTankFiring())
        {
            tank.FireAtPoint(target); // Call the FireAtPoint method to handle firing
        }

        // Continue sniping
        snipeTimer -= Time.deltaTime;
        if (snipeTimer <= 0f)
        {
            // Transition back to ChaseState if sniping duration is over
            Debug.Log("[SnipeState] Snipe duration ended. Transitioning back to ChaseState.");
            tank.ChangeState(new ChaseState(tank, target));
        }
    }

    public override void Exit()
    {
        Debug.Log("[SnipeState] Exiting.");
    }
}
