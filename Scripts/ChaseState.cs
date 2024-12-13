using UnityEngine;
using System.Linq;

/// <summary>
/// ChaseState handles tactical pursuit of enemies.
/// Key Features:
/// - Fuel-efficient pursuit
/// - Tactical positioning for combat
/// - Resource awareness during chase
/// - Safe distance maintenance
/// </summary>
public class ChaseState : TankState
{
    //=============================
    // CHASE STATE PROPERTIES
    //=============================
    private GameObject target;
    private float chaseTimer = 0f;
    private const float CHASE_TIMEOUT = 8f; // Reduced from 10f for faster state transitions
    private Vector3 lastKnownPosition;
    private bool isRepositioning = false;

    //=============================
    // CHASE PARAMETERS
    //=============================
    private const float OPTIMAL_CHASE_DISTANCE = 30f;
    private const float MIN_CHASE_DISTANCE = 20f;
    private const float MAX_CHASE_DISTANCE = 40f;
    private const float SCAN_INTERVAL = 2f;
    private float scanTimer = 0f;

    //=============================
    // TACTICAL POSITIONING
    //=============================
    private Vector3[] tacticalPositions = new Vector3[4];
    private int currentTacticalPosition = 0;
    private float positionUpdateTimer = 0f;
    private const float POSITION_UPDATE_INTERVAL = 2f;

    //=============================
    // CONSTRUCTOR
    //=============================
    public ChaseState(A_Smart tank, GameObject target) : base(tank)
    {
        this.target = target;
        this.lastKnownPosition = target.transform.position;
    }

    //=============================
    // STATE METHODS
    //=============================
    public override void Enter()
    {
        Debug.Log("[ChaseState] Entered. Initiating tactical pursuit.");
        InitializeChase();
    }

    public override void Execute()
    {
        // Perform regular scanning
        scanTimer += Time.deltaTime;
        if (scanTimer >= SCAN_INTERVAL)
        {
            if (tank.PerformScan())
                return;
            scanTimer = 0f;
        }

        // Check if we should continue chase
        if (!ShouldContinueChase())
        {
            HandleChaseExit();
            return;
        }

        // Main chase logic
        HandleChase();
    }

    //=============================
    // CHASE METHODS
    //=============================
    private void InitializeChase()
    {
        chaseTimer = 0f;
        UpdateTacticalPositions();
    }

    private void HandleChase()
    {
        float distanceToTarget = Vector3.Distance(tank.transform.position, target.transform.position);

        // Check for critical resources first
        if (CheckForPriorityResources())
            return;

        // Adjust chase behavior based on distance
        if (distanceToTarget > MAX_CHASE_DISTANCE)
        {
            // Too far, catch up efficiently
            PursueTarget(0.5f); // Using reduced speed for fuel efficiency
        }
        else if (distanceToTarget < MIN_CHASE_DISTANCE)
        {
            // Too close, maintain safe distance
            MaintainDistance();
        }
        else if (distanceToTarget <= OPTIMAL_CHASE_DISTANCE)
        {
            // In optimal range, transition to combat
            DecideCombatTransition();
        }
        else
        {
            // Use tactical positioning
            UseTacticalPosition();
        }

        // Update target tracking
        lastKnownPosition = target.transform.position;
    }

    private void DecideCombatTransition()
    {
        if (tank.GetFuelLevel() < A_Smart.StateThresholds.LOW_FUEL)
        {
            Debug.Log("[ChaseState] Low fuel, switching to dodging for defense.");
            tank.ChangeState(new DodgingState(tank, target));
        }
        else if (target.GetComponent<DumbTank>().TankCurrentHealth <= A_Smart.StateThresholds.WEAK_ENEMY_HEALTH)
        {
            Debug.Log("[ChaseState] Enemy weak, switching to snipe state.");
            tank.ChangeState(new SnipeState(tank, target));
        }
        else
        {
            Debug.Log("[ChaseState] In range, switching to dodge state.");
            tank.ChangeState(new DodgingState(tank, target));
        }
    }

    //=============================
    // MOVEMENT METHODS
    //=============================
    private void PursueTarget(float speedModifier)
    {
        tank.FollowPathToPoint(target, speedModifier, AStar.HeuristicMode.DiagonalShort);
    }

    private void MaintainDistance()
    {
        Vector3 awayFromTarget = tank.transform.position - target.transform.position;
        Vector3 targetPosition = tank.transform.position + awayFromTarget.normalized * 10f;
        tank.FollowPathToPoint(targetPosition, 0.4f, tank.heuristicMode);
    }

    private void UseTacticalPosition()
    {
        UpdateTacticalPositions();
        Vector3 bestPosition = FindBestTacticalPosition();
        tank.FollowPathToPoint(bestPosition, 0.5f, tank.heuristicMode);
    }

    //=============================
    // TACTICAL METHODS
    //=============================
    private void UpdateTacticalPositions()
    {
        if (target == null) return;

        Vector3 targetPos = target.transform.position;
        float radius = OPTIMAL_CHASE_DISTANCE;

        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );
            tacticalPositions[i] = targetPos + offset;
        }
    }

    private Vector3 FindBestTacticalPosition()
    {
        float bestScore = float.MinValue;
        Vector3 bestPosition = tacticalPositions[0];

        foreach (var position in tacticalPositions)
        {
            float score = EvaluatePosition(position);
            if (score > bestScore)
            {
                bestScore = score;
                bestPosition = position;
            }
        }

        return bestPosition;
    }

    //=============================
    // UTILITY METHODS
    //=============================
    private float EvaluatePosition(Vector3 position)
    {
        float score = 0f;

        // Distance to target
        float distanceToTarget = Vector3.Distance(position, target.transform.position);
        score += 100f - Mathf.Abs(distanceToTarget - OPTIMAL_CHASE_DISTANCE);

        // Fuel efficiency consideration
        float distanceFromCurrent = Vector3.Distance(position, tank.transform.position);
        score -= distanceFromCurrent * 0.5f;

        // Line of sight check
        RaycastHit hit;
        Vector3 directionToTarget = (target.transform.position - position).normalized;
        if (!Physics.Raycast(position, directionToTarget, out hit))
        {
            score += 50f; // Bonus for clear line of sight
        }

        return score;
    }

    private bool CheckForPriorityResources()
    {
        if (tank.GetFuelLevel() < A_Smart.StateThresholds.CRITICAL_FUEL ||
            tank.GetHealthLevel() < A_Smart.StateThresholds.CRITICAL_HEALTH)
        {
            var resource = GetMostNeededResource();
            if (resource != null)
            {
                Debug.Log("[ChaseState] Critical resource found, collecting.");
                tank.ChangeState(new ResourceCollectionState(tank));
                return true;
            }
        }
        return false;
    }

    private GameObject GetMostNeededResource()
    {
        if (tank.GetFuelLevel() < A_Smart.StateThresholds.CRITICAL_FUEL)
            return tank.GetNearestConsumable("Fuel");
        if (tank.GetHealthLevel() < A_Smart.StateThresholds.CRITICAL_HEALTH)
            return tank.GetNearestConsumable("Health");
        return null;
    }

    private bool ShouldContinueChase()
    {
        if (target == null || !target.activeSelf)
            return false;

        if (tank.GetFuelLevel() < A_Smart.StateThresholds.CRITICAL_FUEL ||
            tank.GetHealthLevel() < A_Smart.StateThresholds.CRITICAL_HEALTH)
            return false;

        chaseTimer += Time.deltaTime;
        return chaseTimer < CHASE_TIMEOUT;
    }

    private void HandleChaseExit()
    {
        if (tank.GetFuelLevel() < A_Smart.StateThresholds.CRITICAL_FUEL)
        {
            Debug.Log("[ChaseState] Critical fuel, switching to wait state.");
            tank.ChangeState(new WaitState(tank));
        }
        else
        {
            Debug.Log("[ChaseState] Chase timeout, switching to explore state.");
            tank.ChangeState(new ExploreState(tank));
        }
    }

    public override void Exit()
    {
        Debug.Log("[ChaseState] Exiting chase state.");
    }
}