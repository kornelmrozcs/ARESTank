using UnityEngine;
using System.Linq;

/// <summary>
/// DodgingState handles evasive maneuvers and tactical positioning.
/// Key Features:
/// - Multiple dodge patterns
/// - Fuel-efficient movement
/// - Predictive enemy shot avoidance
/// - Tactical positioning
/// </summary>
public class DodgingState : TankState
{
    //=============================
    // DODGE STATE PROPERTIES
    //=============================
    private GameObject target;
    private float dodgeTimer = 0f;
    private const float DODGE_DURATION = 1.5f;
    private const float DODGE_DISTANCE = 6f;
    private Vector3 currentDodgeDirection;
    private bool isDodging = false;
    private int currentPattern = 0;
    private const int PATTERN_COUNT = 4;

    //=============================
    // MOVEMENT PARAMETERS
    //=============================
    private const float MAX_DISTANCE = 35f;
    private const float MIN_DISTANCE = 25f;
    private const float SCAN_INTERVAL = 2f;
    private float scanTimer = 0f;
    private Vector3 lastEnemyPosition;
    private Vector3 lastEnemyVelocity;

    //=============================
    // DODGE PATTERNS
    //=============================
    private enum DodgePattern
    {
        Sidestep,
        ZigZag,
        Retreat,
        Circle
    }
    private DodgePattern currentDodgePattern;

    //=============================
    // CONSTRUCTOR
    //=============================
    public DodgingState(A_Smart tank, GameObject target) : base(tank)
    {
        this.target = target;
        this.lastEnemyPosition = target.transform.position;
        this.lastEnemyVelocity = target.GetComponent<Rigidbody>().velocity;
    }

    //=============================
    // STATE METHODS
    //=============================
    public override void Enter()
    {
        Debug.Log("[DodgingState] Entered. Initiating evasive maneuvers.");
        InitiateDodge();
    }

    public override void Execute()
    {
        // Regular scanning if not actively dodging
        if (!isDodging)
        {
            scanTimer += Time.deltaTime;
            if (scanTimer >= SCAN_INTERVAL)
            {
                if (tank.PerformScan())
                {
                    return;
                }
                scanTimer = 0f;
            }
        }

        // Check if we should continue dodging
        if (!ShouldContinueDodging())
        {
            HandleDodgeExit();
            return;
        }

        // Execute dodge pattern
        if (isDodging)
        {
            ExecuteDodgePattern();
        }
        else
        {
            EvaluateNextAction();
        }

        // Always track enemy
        UpdateEnemyTracking();
    }

    //=============================
    // DODGE METHODS
    //=============================
    private void InitiateDodge()
    {
        isDodging = true;
        dodgeTimer = DODGE_DURATION;
        currentDodgePattern = (DodgePattern)(currentPattern % PATTERN_COUNT);
        currentPattern++;

        // Calculate best dodge direction based on current pattern
        currentDodgeDirection = CalculateBestDodgeDirection();
        Debug.Log($"[DodgingState] New dodge initiated with pattern: {currentDodgePattern}");
    }

    private void ExecuteDodgePattern()
    {
        dodgeTimer -= Time.deltaTime;
        if (dodgeTimer <= 0)
        {
            isDodging = false;
            return;
        }

        Vector3 targetPosition = CalculateDodgePosition();
        float speed = CalculateOptimalSpeed();

        // Execute the selected dodge pattern
        switch (currentDodgePattern)
        {
            case DodgePattern.Sidestep:
                ExecuteSidestepDodge(targetPosition, speed);
                break;
            case DodgePattern.ZigZag:
                ExecuteZigZagDodge(targetPosition, speed);
                break;
            case DodgePattern.Retreat:
                ExecuteRetreatDodge(targetPosition, speed);
                break;
            case DodgePattern.Circle:
                ExecuteCircleDodge(targetPosition, speed);
                break;
        }

        // Always face turret towards enemy while dodging
        tank.TurretFaceWorldPoint(target);

        // Check for resources during dodge
        CheckForCriticalResources();
    }

    //=============================
    // DODGE PATTERNS
    //=============================
    private void ExecuteSidestepDodge(Vector3 targetPosition, float speed)
    {
        Vector3 sideStep = Vector3.Cross(tank.transform.forward, Vector3.up).normalized * DODGE_DISTANCE;
        tank.FollowPathToPoint(tank.transform.position + sideStep * (dodgeTimer < DODGE_DURATION / 2 ? -1 : 1),
            speed, tank.heuristicMode);
    }

    private void ExecuteZigZagDodge(Vector3 targetPosition, float speed)
    {
        float zigZagAmplitude = 5f;
        float zigZagFrequency = 2f;
        Vector3 zigZagOffset = Vector3.right * Mathf.Sin(Time.time * zigZagFrequency) * zigZagAmplitude;
        tank.FollowPathToPoint(targetPosition + zigZagOffset, speed, tank.heuristicMode);
    }

    private void ExecuteRetreatDodge(Vector3 targetPosition, float speed)
    {
        Vector3 retreatDirection = (tank.transform.position - target.transform.position).normalized;
        tank.FollowPathToPoint(tank.transform.position + retreatDirection * DODGE_DISTANCE,
            speed, tank.heuristicMode);
    }

    private void ExecuteCircleDodge(Vector3 targetPosition, float speed)
    {
        float circleRadius = 8f;
        float angleSpeed = 2f;
        Vector3 centerPoint = target.transform.position;
        Vector3 circlePoint = centerPoint + new Vector3(
            Mathf.Cos(Time.time * angleSpeed) * circleRadius,
            0,
            Mathf.Sin(Time.time * angleSpeed) * circleRadius
        );
        tank.FollowPathToPoint(circlePoint, speed, tank.heuristicMode);
    }

    //=============================
    // CALCULATION METHODS
    //=============================
    private Vector3 CalculateBestDodgeDirection()
    {
        Vector3 directionToTarget = (tank.transform.position - target.transform.position).normalized;
        Vector3 rightDirection = Vector3.Cross(directionToTarget, Vector3.up);

        // Choose direction based on pattern and situation
        switch (currentDodgePattern)
        {
            case DodgePattern.Sidestep:
                return rightDirection;
            case DodgePattern.ZigZag:
                return (directionToTarget + rightDirection * 0.5f).normalized;
            case DodgePattern.Retreat:
                return directionToTarget;
            case DodgePattern.Circle:
                return Vector3.Cross(directionToTarget, Vector3.up);
            default:
                return rightDirection;
        }
    }

    private float CalculateOptimalSpeed()
    {
        // Adjust speed based on fuel level and distance to target
        float distanceToTarget = Vector3.Distance(tank.transform.position, target.transform.position);
        float baseSpeed = tank.GetFuelLevel() < A_Smart.StateThresholds.LOW_FUEL ? 0.5f : 0.7f;

        // Reduce speed when getting too close to target
        if (distanceToTarget < MIN_DISTANCE)
            return baseSpeed * 0.8f;

        return baseSpeed;
    }

    //=============================
    // UTILITY METHODS
    //=============================
    private void UpdateEnemyTracking()
    {
        if (target != null)
        {
            lastEnemyPosition = target.transform.position;
            lastEnemyVelocity = target.GetComponent<Rigidbody>().velocity;
        }
    }

    private bool ShouldContinueDodging()
    {
        if (target == null || !target.activeSelf)
            return false;

        if (tank.GetFuelLevel() < A_Smart.StateThresholds.CRITICAL_FUEL)
            return false;

        float distance = Vector3.Distance(tank.transform.position, target.transform.position);
        return distance <= MAX_DISTANCE;
    }

    private void CheckForCriticalResources()
    {
        if (tank.GetFuelLevel() < A_Smart.StateThresholds.CRITICAL_FUEL ||
            tank.GetHealthLevel() < A_Smart.StateThresholds.CRITICAL_HEALTH)
        {
            var criticalResource = GetMostNeededResource();
            if (criticalResource != null &&
                Vector3.Distance(tank.transform.position, criticalResource.transform.position) < 10f)
            {
                isDodging = false;
                tank.ChangeState(new ResourceCollectionState(tank));
            }
        }
    }

    private GameObject GetMostNeededResource()
    {
        if (tank.GetFuelLevel() < A_Smart.StateThresholds.CRITICAL_FUEL)
            return tank.GetNearestConsumable("Fuel");
        if (tank.GetHealthLevel() < A_Smart.StateThresholds.CRITICAL_HEALTH)
            return tank.GetNearestConsumable("Health");
        return null;
    }

    private void EvaluateNextAction()
    {
        if (target != null && target.GetComponent<DumbTank>().TankCurrentHealth <= A_Smart.StateThresholds.WEAK_ENEMY_HEALTH)
        {
            tank.ChangeState(new SnipeState(tank, target));
            return;
        }

        InitiateDodge();
    }

    private void HandleDodgeExit()
    {
        if (tank.GetFuelLevel() < A_Smart.StateThresholds.CRITICAL_FUEL)
        {
            Debug.Log("[DodgingState] Critical fuel level, switching to wait state.");
            tank.ChangeState(new WaitState(tank));
        }
        else
        {
            Debug.Log("[DodgingState] Switching to resource collection.");
            tank.ChangeState(new ResourceCollectionState(tank));
        }
    }

    public override void Exit()
    {
        Debug.Log("[DodgingState] Exiting dodge state.");
    }
}