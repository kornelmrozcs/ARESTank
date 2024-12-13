using UnityEngine;
using System.Linq;

/// <summary>
/// WaitState implements the initial fuel conservation strategy.
/// Key Features:
/// - Conserves fuel at the start of the game
/// - Monitors enemy fuel consumption
/// - Performs regular environment scanning
/// - Responds to critical situations and opportunities
/// </summary>
public class WaitState : TankState
{
    //=============================
    // WAIT STATE PROPERTIES
    //=============================
    private float waitTimer = 0f;
    private const float INITIAL_WAIT_TIME = 20f;    // Increased from 15f for better fuel advantage
    private bool initialWaitCompleted = false;
    private Vector3 lastPosition;
    private float scanTimer = 0f;
    private const float SCAN_INTERVAL = 3f;

    //=============================
    // RESOURCE EVALUATION
    //=============================
    private const float RESOURCE_PROXIMITY_THRESHOLD = 10f;
    private const float ENEMY_PROXIMITY_THRESHOLD = 15f;
    private float lastKnownEnemyFuel = 100f;

    //=============================
    // CONSTRUCTOR
    //=============================
    public WaitState(A_Smart tank) : base(tank)
    {
        lastPosition = tank.transform.position;
    }

    //=============================
    // STATE METHODS
    //=============================
    public override void Enter()
    {
        Debug.Log("[WaitState] Entered. Implementing fuel conservation strategy.");
        lastPosition = tank.transform.position;
    }

    public override void Execute()
    {
        // Regular scanning while waiting
        if (tank.PerformScan())
        {
            return; // Don't perform other actions while scanning
        }

        // Handle initial waiting period
        if (!initialWaitCompleted)
        {
            HandleInitialWait();
            return;
        }

        // Evaluate situation after initial wait
        EvaluateSituation();
    }

    //=============================
    // WAIT STRATEGY METHODS
    //=============================
    private void HandleInitialWait()
    {
        waitTimer += Time.deltaTime;

        // Check if initial wait period is complete
        if (waitTimer >= INITIAL_WAIT_TIME)
        {
            Debug.Log("[WaitState] Initial wait completed, enemy should have consumed significant fuel.");
            initialWaitCompleted = true;
            return;
        }

        // Check for situations that would break wait
        if (ShouldBreakWait())
        {
            Debug.Log("[WaitState] Breaking wait due to critical situation.");
            initialWaitCompleted = true;
        }
    }

    private bool ShouldBreakWait()
    {
        // Break if enemy is very close
        if (tank.enemyTanksFound.Any(x => x.Value < ENEMY_PROXIMITY_THRESHOLD))
        {
            return true;
        }

        // Break for critical fuel and nearby fuel resource
        if (tank.GetFuelLevel() < A_Smart.StateThresholds.CRITICAL_FUEL &&
            tank.consumablesFound.Any(x => x.Key.CompareTag("Fuel") && x.Value < RESOURCE_PROXIMITY_THRESHOLD))
        {
            return true;
        }

        // Break if enemy is weak
        var enemy = tank.enemyTanksFound.FirstOrDefault();
        if (enemy.Key != null &&
            enemy.Key.GetComponent<DumbTank>().TankCurrentHealth <= A_Smart.StateThresholds.WEAK_ENEMY_HEALTH)
        {
            return true;
        }

        return false;
    }

    //=============================
    // SITUATION EVALUATION
    //=============================
    private void EvaluateSituation()
    {
        // Priority 1: Handle Critical Resources
        if (tank.GetFuelLevel() < A_Smart.StateThresholds.CRITICAL_FUEL)
        {
            HandleCriticalResources();
            return;
        }

        // Priority 2: Check for Weak Enemies
        if (CheckForWeakEnemies())
        {
            return;
        }

        // Priority 3: Collect Valuable Resources
        if (CheckForValuableConsumables())
        {
            return;
        }

        // Continue waiting and scanning if no action needed
        // This maintains our fuel conservation strategy
    }

    private void HandleCriticalResources()
    {
        var nearestFuel = tank.GetNearestConsumable("Fuel");
        if (nearestFuel != null)
        {
            float distance = Vector3.Distance(tank.transform.position, nearestFuel.transform.position);
            if (distance < RESOURCE_PROXIMITY_THRESHOLD)
            {
                Debug.Log("[WaitState] Critical fuel level - collecting nearby fuel.");
                tank.ChangeState(new ResourceCollectionState(tank));
                return;
            }
        }

        // Stay absolutely still if fuel is critical and no resources nearby
        Debug.Log("[WaitState] Critical fuel level - maintaining absolute position.");
    }

    private bool CheckForWeakEnemies()
    {
        foreach (var enemy in tank.enemyTanksFound)
        {
            if (enemy.Key != null)
            {
                var dumbTank = enemy.Key.GetComponent<DumbTank>();
                if (dumbTank && dumbTank.TankCurrentHealth <= A_Smart.StateThresholds.WEAK_ENEMY_HEALTH)
                {
                    // Only chase if we have enough fuel
                    if (tank.GetFuelLevel() > A_Smart.StateThresholds.LOW_FUEL)
                    {
                        Debug.Log("[WaitState] Found weak enemy - transitioning to Chase state.");
                        tank.ChangeState(new ChaseState(tank, enemy.Key));
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private bool CheckForValuableConsumables()
    {
        // Only collect resources if they're very close to conserve fuel
        if (tank.GetFuelLevel() < 50f)
        {
            var fuel = tank.GetNearestConsumable("Fuel");
            if (fuel != null && Vector3.Distance(tank.transform.position, fuel.transform.position) < RESOURCE_PROXIMITY_THRESHOLD)
            {
                Debug.Log("[WaitState] Found nearby fuel - collecting.");
                tank.ChangeState(new ResourceCollectionState(tank));
                return true;
            }
        }

        if (tank.GetHealthLevel() < 75f)
        {
            var health = tank.GetNearestConsumable("Health");
            if (health != null && Vector3.Distance(tank.transform.position, health.transform.position) < RESOURCE_PROXIMITY_THRESHOLD)
            {
                Debug.Log("[WaitState] Found nearby health - collecting.");
                tank.ChangeState(new ResourceCollectionState(tank));
                return true;
            }
        }

        return false;
    }

    public override void Exit()
    {
        Debug.Log("[WaitState] Exiting wait state.");
    }
}