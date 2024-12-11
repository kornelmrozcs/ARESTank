using UnityEngine;

public class AttackState : TankState
{
    private GameObject target;
    private const float fireCooldown = 0f; // Cooldown between shots
    private const float attackRange = 25f; // Range for firing at the target
    private const float maxChaseDistance = 35f; // Maximum range to keep chasing the target
    private const float detectionRange = 52f; // The range within which the enemy is still detectable
    private const float minFireDistance = 8f; // Minimum distance to maintain when firing to avoid being too close
    private float fireTimer = 0f; // Timer to track cooldown after firing

    public AttackState(A_Smart tank, GameObject target = null) : base(tank)
    {
        this.target = target;
    }

    public override void Enter()
    {
        Debug.Log("[AttackState] Entered.");
        fireTimer = 0f; // Reset fire cooldown
    }

    public override void Execute()
    {
        // Check if the tank still has a target and if the target is within detection range
        if (target == null || !tank.enemyTanksFound.ContainsKey(target))
        {
            Debug.Log("[AttackState] Lost target. Switching to ExploreState.");
            tank.ChangeState(new ExploreState(tank));
            return;
        }

        // Check DumbTank's health for transition to ChaseState or SnipeState
        if (target.GetComponent<DumbTank>().TankCurrentHealth <= 30f)
        {
            Debug.Log("[AttackState] DumbTank's health is low. Transitioning to ChaseState.");
            tank.ChangeState(new ChaseState(tank, target));
            return;
        }

        // Ensure turret locks on to the target
        tank.TurretFaceWorldPoint(target);

        // Update fire cooldown
        if (fireTimer > 0f) fireTimer -= Time.deltaTime;

        // Calculate distance to the target
        float distance = Vector3.Distance(tank.transform.position, target.transform.position);

        // Ensure the tank stays within the detection range of the enemy
        if (distance > detectionRange)
        {
            Debug.Log("[AttackState] Target moved out of detection range. Switching to ExploreState.");
            tank.ChangeState(new ExploreState(tank));
            return;
        }

        // If the target moves too far, but we are in the firing cooldown period, don't transition
        if (distance > maxChaseDistance && fireTimer <= 0f)
        {
            Debug.Log("[AttackState] Target moved too far. Switching to ExploreState.");
            tank.ChangeState(new ExploreState(tank));
            return;
        }

        // Ensure the tank maintains a minimum distance for accurate firing
        if (distance < minFireDistance)
        {
            Debug.Log("[AttackState] Too close to target. Moving back slightly.");
            // Move the tank slightly away from the target to avoid being too close
            Vector3 directionToTarget = (tank.transform.position - target.transform.position).normalized;
            tank.FollowPathToPoint(tank.transform.position + directionToTarget * minFireDistance, 1f, tank.heuristicMode);
            return;
        }

        // If the target is out of firing range, move closer
        if (distance > attackRange)
        {
            Debug.Log("[AttackState] Target out of range. Moving closer to target.");
            // Move closer to the target if out of range but not too close
            tank.FollowPathToPoint(target, 1f, tank.heuristicMode);
            return;
        }

        // Fire at the target if in range and cooldown is complete
        if (distance <= attackRange && fireTimer <= 0f)
        {
            Debug.Log("[AttackState] Firing at target while maintaining movement: " + target.name);
            tank.FireAtPoint(target); // Fire the shot using AITank's method
            fireTimer = fireCooldown; // Start cooldown after firing

            // Transition to DodgingState after firing to avoid incoming fire
            tank.ChangeState(new DodgingState(tank, target));

            // Continue strafing or circling while firing
            tank.FollowPathToPoint(target, 1f, tank.heuristicMode);
        }

        // Retreat if health is critically low
        if (tank.GetHealthLevel() < 10f)
        {
            Debug.Log("[AttackState] Low health detected. Retreating to ExploreState.");
            tank.ChangeState(new ExploreState(tank));
            return;
        }
    }

    public override void Exit()
    {
        Debug.Log("[AttackState] Exiting.");
    }
}
