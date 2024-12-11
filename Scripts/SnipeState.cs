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

            // If the shot has been fired and the health difference is small, we transition to DodgingState
            if (hasFired && tank.GetHealthLevel() > target.GetComponent<DumbTank>().TankCurrentHealth)
            {
                Debug.Log("[SnipeState] Health is greater, continuing to snipe.");
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
        // Predict the position of the DumbTank based on its velocity and direction
        Vector3 predictedPosition = target.transform.position + target.GetComponent<Rigidbody>().velocity * 4f; // Predictive aim (you can adjust the multiplier)

        // Face the turret towards the predicted position
        tank.TurretFaceWorldPoint(new GameObject { transform = { position = predictedPosition } });

        // Fire at the predicted target
        if (!tank.IsTankFiring() && !hasFired)
        {
            tank.FireAtPoint(target); // Call the FireAtPoint method to handle firing
            hasFired = true; // Mark that the shot has been fired
        }
    }

    public override void Exit()
    {
        Debug.Log("[SnipeState] Exiting.");
    }
}
