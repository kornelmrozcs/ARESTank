using System.Linq;
using UnityEngine;

public class AttackState : TankState
{
    private GameObject target;
    private float actionTimer = 0f; // Timer for controlling firing and moving phases
    private const float fireDuration = 1f; // Time spent firing in each burst
    private const float moveDuration = 2f; // Time spent moving between bursts
    private bool isFiring = true; // Toggle between firing and moving

    public AttackState(A_Smart tank, GameObject target = null) : base(tank)
    {
        this.target = target;
    }

    public override void Enter()
    {
        Debug.Log("[AttackState] Entered.");
        actionTimer = 0f;
        isFiring = true; // Start with firing
    }

    public override void Execute()
    {
        if (target == null || !tank.enemyTanksFound.ContainsKey(target))
        {
            Debug.Log("[AttackState] No valid target. Switching to ExploreState.");
            tank.ChangeState(new ExploreState(tank));
            return;
        }

        // Always aim at the target
        tank.TurretFaceWorldPoint(target);

        // Lock the cone of vision on the enemy
        tank.LockVisionOnTarget(target);

        // Calculate the distance to the target
        float distance = Vector3.Distance(tank.transform.position, target.transform.position);

        // Alternate between firing and moving
        actionTimer += Time.deltaTime;

        if (isFiring && actionTimer < fireDuration)
        {
            if (distance < 25f)
            {
                Debug.Log("[AttackState] Firing burst at target: " + target.name);
                tank.FireAtPoint(target); // Shoot while strafing or stationary
            }
            else
            {
                Debug.Log("[AttackState] Target out of range. Moving closer: " + target.name);
                tank.FollowPathToPoint(target, 1f, tank.heuristicMode);
            }
        }
        else if (!isFiring && actionTimer < moveDuration + fireDuration)
        {
            Debug.Log("[AttackState] Moving to reposition.");

            // Generate a new random position to move around
            tank.FollowPathToRandomPoint(1f, tank.heuristicMode);
        }
        else
        {
            // Reset timer and toggle firing/moving
            actionTimer = 0f;
            isFiring = !isFiring;
        }

        // Retreat if health is critically low
        if (tank.GetHealthLevel() < 25)
        {
            Debug.Log("[AttackState] Low health detected. Retreating to SearchState.");
            tank.ChangeState(new SearchState(tank));
            return;
        }
    }

    public override void Exit()
    {
        Debug.Log("[AttackState] Exiting.");
    }
}
