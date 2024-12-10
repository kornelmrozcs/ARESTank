using UnityEngine;

public class MovingPhaseState : TankState
{
    private GameObject target;
    private float circularRadius = 15f; // Radius for circular motion
    private float circularSpeed = 2f; // Speed of circular motion
    private float angle = 0f; // Angle for circular motion
    private bool waitingForEnemyFire = true; // Whether the tank is dodging and waiting for the enemy to fire
    private float responseCooldown = 0.5f; // Cooldown between detecting a shot and shooting back
    private float responseTimer = 0f;

    private float moveDuration = 3f; // Total time for moving around
    private float moveTimer = 3f; // Timer for movement phase

    public MovingPhaseState(A_Smart tank, GameObject target = null) : base(tank)
    {
        this.target = target;
    }

    public override void Enter()
    {
        Debug.Log("[MovingPhaseState] Entered.");
        angle = 0f; // Reset angle
        responseTimer = 0f;
        moveTimer = 0f; // Reset move timer
        waitingForEnemyFire = true; // Initially waiting for the enemy to fire
    }

    public override void Execute()
    {
        if (target == null || !tank.enemyTanksFound.ContainsKey(target))
        {
            Debug.Log("[MovingPhaseState] Lost target. Switching to ExploreState.");
            tank.ChangeState(new ExploreState(tank));
            return;
        }

        // Lock the turret onto the target to maintain aggression
        tank.TurretFaceWorldPoint(target);

        // Calculate the circular position around the target
        angle += circularSpeed * Time.deltaTime; // Increment the angle
        if (angle >= 360f) angle -= 360f; // Keep the angle within bounds

        Vector3 directionToTarget = (target.transform.position - tank.transform.position).normalized;
        Vector3 perpendicularDirection = Vector3.Cross(directionToTarget, Vector3.up).normalized; // Perpendicular to the target
        Vector3 circularPosition = target.transform.position + (Mathf.Cos(angle) * directionToTarget + Mathf.Sin(angle) * perpendicularDirection) * circularRadius;

        // Move to the circular position
        Debug.Log("[MovingPhaseState] Moving in circular motion around target: " + target.name);
        tank.FollowPathToPoint(new GameObject { transform = { position = circularPosition } }, 1f, tank.heuristicMode);

        // Increment the move timer
        moveTimer += Time.deltaTime;

        // If the moving phase is complete, switch back to AttackState
        if (moveTimer >= moveDuration)
        {
            Debug.Log("[MovingPhaseState] Completed movement phase. Switching back to AttackState.");
            tank.ChangeState(new AttackState(tank, target));
            return;
        }

        // Check if enemy fired a bullet
        if (EnemyTankFired()) // Replace this with actual detection logic
        {
            Debug.Log("[MovingPhaseState] Enemy fired! Preparing to counterattack.");
            waitingForEnemyFire = false;
            responseTimer = responseCooldown; // Start the cooldown before counterattacking
        }

        // Counterattack logic
        if (!waitingForEnemyFire)
        {
            responseTimer -= Time.deltaTime;
            if (responseTimer <= 0f)
            {
                Debug.Log("[MovingPhaseState] Counterattacking target: " + target.name);
                tank.FireAtPoint(target);
                waitingForEnemyFire = true; // Go back to dodging
            }
        }
    }

    public override void Exit()
    {
        Debug.Log("[MovingPhaseState] Exiting.");
    }

    private bool EnemyTankFired()
    {
        // Placeholder for actual logic to detect if the enemy fired
        // This could be implemented using collision detection, projectile tracking, or game event hooks
        return Random.value < 0.01f; // Simulate random enemy fire for testing
    }
}
