using UnityEngine;

public class SnipeState : TankState
{
    private GameObject target;
    private float snipeTimer = 0f;
    private const float maxSnipeDuration = 4f; // Maximum time for sniping
    private bool hasFired = false; // Track if a shot has been fired

    public SnipeState(A_Smart tank, GameObject target) : base(tank)
    {
        this.target = target;
    }

    public override void Enter()
    {
        Debug.Log("[SnipeState] Entered.");
        snipeTimer = maxSnipeDuration; // Initialize snipe timer
        hasFired = false; // Reset firing state
    }

    public override void Execute()
    {
        // Check if the target is still within range
        if (Vector3.Distance(tank.transform.position, target.transform.position) <= 25f)
        {
            // Aim and shoot with predictive targeting
            PredictAndShootAtTarget();

            // If the shot has been fired, check health difference
            if (hasFired)
            {
                float healthDifference = tank.GetHealthLevel() - target.GetComponent<DumbTank>().TankCurrentHealth;

                if (healthDifference <= 0)
                {
                    // If health is equal or lower than DumbTank's, transition to DodgingState
                    Debug.Log("[SnipeState] Health is lower or the same, transitioning to DodgingState.");
                    tank.ChangeState(new DodgingState(tank, target)); // Transition to DodgingState
                    return;
                }
                else if (healthDifference >= 10)
                {
                    // If health is higher by 10 or more, continue firing without dodging
                    Debug.Log("[SnipeState] Health is greater by 10 or more, continuing to fire.");
                    return;
                }
            }
        }
        else
        {
            // If the target goes out of range, stop firing and transition to ChaseState
            Debug.Log("[SnipeState] Target out of range. Transitioning to ChaseState.");
            tank.ChangeState(new ChaseState(tank, target));
            return;
        }

        // Continue sniping if still within the sniping duration
        snipeTimer -= Time.deltaTime;
        if (snipeTimer <= 0f)
        {
            // Transition back to ChaseState if sniping duration is over
            Debug.Log("[SnipeState] Snipe duration ended. Transitioning back to ChaseState.");
            tank.ChangeState(new ChaseState(tank, target));
        }
    }

    private void PredictAndShootAtTarget()
    {
        // Speed of the bullet (projectile speed). Adjust based on your game's mechanics.
        float bulletSpeed = 40f;  // Example: Set this value to the speed of your bullet.

        // Calculate the distance to the target
        float distanceToTarget = Vector3.Distance(tank.transform.position, target.transform.position);

        // If the target is too close, fire directly at it instead of predicting
        if (distanceToTarget < 5f) // You can adjust this threshold for how close is "too close"
        {
            // Aim directly at the target
            tank.TurretFaceWorldPoint(target);
        }
        else
        {
            // Predict the time it will take for the bullet to reach the target
            float timeToTarget = distanceToTarget / bulletSpeed;

            // Predict the target's future position based on its velocity
            Vector3 predictedPosition = target.transform.position + target.GetComponent<Rigidbody>().velocity * timeToTarget;

            // Face the turret towards the predicted position
            tank.TurretFaceWorldPoint(new GameObject { transform = { position = predictedPosition } });
        }

        // Fire at the target or predicted position
        if (!tank.IsTankFiring() && !hasFired)
        {
            tank.FireAtPoint(target); // Fire at the predicted position or the target directly
            hasFired = true;  // Mark that the shot has been fired
        }
    }


    public override void Exit()
    {
        Debug.Log("[SnipeState] Exiting.");
    }
}
