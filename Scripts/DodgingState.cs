using System.Linq;
using UnityEngine;

public class DodgingState : TankState
{
    private GameObject target;
    private float dodgeDuration = 2.5f; // Dodge for 2 seconds
    private float dodgeTimer = 0f;
    private float dodgeDistance = 4f; // Distance to move while dodging
    private float maxDistanceFromTarget = 35f; // Max distance before we consider losing sight of the target
    private int previousAmmoLevel;  // To track the ammo level change

    public DodgingState(A_Smart tank, GameObject target) : base(tank)
    {
        this.target = target;
    }

    public override void Enter()
    {
        Debug.Log("[DodgingState] Entered.");
        dodgeTimer = dodgeDuration; // Set timer for dodging
        previousAmmoLevel = (int)tank.TankCurrentAmmo; // Initialize ammo level tracking with the current ammo
    }

    public override void Execute()
    {
        // Check if the enemy tank has fired (ammo level decreased)
        if ((int)tank.TankCurrentAmmo < previousAmmoLevel)
        {
            Debug.Log("[DodgingState] Ammo has decreased, transitioning to SnipeState.");
            tank.ChangeState(new SnipeState(tank, target));  // Transition to SnipeState immediately
            return;
        }

        // Get distance to the target
        float distance = Vector3.Distance(tank.transform.position, target.transform.position);

        // Check if we are far enough to stop dodging and continue chasing
        if (distance > maxDistanceFromTarget)
        {
            Debug.Log("[DodgingState] Lost sight of target, transitioning to ChaseState.");
            tank.ChangeState(new ChaseState(tank, target));  // Transition to ChaseState
            return;
        }

        // Perform dodging movement if the dodge timer is active
        if (dodgeTimer > 0f)
        {
            PerformDodgingMovement();
            dodgeTimer -= Time.deltaTime; // Countdown the dodge timer
            Debug.Log($"[DodgingState] Dodge Timer: {dodgeTimer}"); // For debugging
        }
        else
        {
            // After dodging, transition to SnipeState to continue fighting
            Debug.Log("[DodgingState] Dodge complete, transitioning to SnipeState.");
            tank.ChangeState(new SnipeState(tank, target)); // Transition to SnipeState
        }

        // Check for consumables while dodging, prioritize them
        if (tank.consumablesFound.Count > 0)
        {
            GameObject consumable = tank.consumablesFound.First().Key;
            if (consumable != null)
            {
                Debug.Log("[DodgingState] Collecting consumable: " + consumable.name);
                tank.FollowPathToPoint(consumable.transform.position, 1f, tank.heuristicMode);
                return; // After collecting consumable, transition back to ChaseState
            }
        }

        // If health/fuel/ammo are low, go back to ExploreState
        if (tank.GetHealthLevel() < 20f || tank.GetFuelLevel() < 15f || tank.GetAmmoLevel() < 2)
        {
            Debug.Log("[DodgingState] Health/Fuel/Ammo low. Transitioning to ExploreState.");
            tank.ChangeState(new ExploreState(tank));
        }
    }

    private void PerformDodgingMovement()
    {
        // Dodge up or down by a random direction
        Vector3 dodgeDirection = (Random.value > 0.5f) ? tank.transform.up : -tank.transform.up;

        // Move away from the target for dodgeDistance, but avoid crossing enemy fire line
        tank.FollowPathToPoint(tank.transform.position + dodgeDirection * dodgeDistance, 1f, tank.heuristicMode);

        // Ensure the tank is always facing the target after dodging
        tank.TurretFaceWorldPoint(target);
    }

    public override void Exit()
    {
        Debug.Log("[DodgingState] Exiting.");
    }
}
