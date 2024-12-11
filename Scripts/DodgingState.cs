using UnityEngine;
using System.Collections;

public class DodgingState : TankState
{
    private GameObject target;
    private float dodgeDuration = 1.7f; // Adjusted dodge duration for more dodging time
    private float dodgeTimer = 0f;
    private float dodgeDistance = 3f; // Distance the tank moves left or right while dodging
    private float maxDistanceFromTarget = 35f; // Max distance before we consider losing sight of the DumbTank

    public DodgingState(A_Smart tank, GameObject target) : base(tank)
    {
        this.target = target;
    }

    public override void Enter()
    {
        Debug.Log("[DodgingState] Entered.");
        dodgeTimer = dodgeDuration; // Set timer for dodging
    }

    public override void Execute()
    {
        if (dodgeTimer > 0f)
        {
            // Perform dodging movement
            PerformDodgingMovement();
            dodgeTimer -= Time.deltaTime; // Countdown dodge duration
            Debug.Log($"[DodgingState] Dodge Timer: {dodgeTimer}"); // For debugging
        }
        else
        {
            // After dodging duration ends, transition back to AttackState
            Debug.Log("[DodgingState] Dodge complete. Returning to AttackState.");
            tank.ChangeState(new AttackState(tank, target));  // Ensure transition
        }

        // If the target is too far away, transition to ExploreState
        if (Vector3.Distance(tank.transform.position, target.transform.position) > maxDistanceFromTarget)
        {
            Debug.Log("[DodgingState] Lost sight of target. Transitioning to ExploreState.");
            tank.ChangeState(new ExploreState(tank));
        }
    }

    private void PerformDodgingMovement()
    {
        // Perform lateral dodging (left or right) while keeping an eye on the target
        float randomDirection = Random.Range(0f, 1f);

        if (randomDirection > 0.5f)
        {
            // Move left
            tank.FollowPathToPoint(tank.transform.position + tank.transform.right * dodgeDistance, 1f, tank.heuristicMode);
        }
        else
        {
            // Move right
            tank.FollowPathToPoint(tank.transform.position - tank.transform.right * dodgeDistance, 1f, tank.heuristicMode);
        }

        // Ensure the tank is always facing the target after dodging
        tank.TurretFaceWorldPoint(target);

        // After dodging, return to the target
        tank.FollowPathToPoint(target, 1f, tank.heuristicMode);
    }

    public override void Exit()
    {
        Debug.Log("[DodgingState] Exiting.");
    }
}
