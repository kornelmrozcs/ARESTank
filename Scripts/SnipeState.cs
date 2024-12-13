using UnityEngine;

/// <summary>
/// SnipeState handles tactical combat engagement with predictive shooting.
/// Key Features:
/// - Advanced target prediction
/// - Optimal positioning
/// - Fuel-efficient movement
/// - Resource awareness during combat
/// </summary>
public class SnipeState : TankState
{
    //=============================
    // SNIPE STATE PROPERTIES
    //=============================
    private GameObject target;
    private float snipeTimer = 0f;
    private const float MAX_SNIPE_DURATION = 4f;
    private bool hasFired = false;
    private Vector3 lastKnownPosition;
    private Vector3 lastKnownVelocity;

    //=============================
    // COMBAT RANGES
    //=============================
    private const float OPTIMAL_RANGE_MIN = 30f;
    private const float OPTIMAL_RANGE_MAX = 40f;
    private const float CRITICAL_RANGE = 15f;

    //=============================
    // SHOT PREDICTION
    //=============================
    private float lastShotAccuracy = 0f;
    private Vector3 lastPredictedPosition;
    private float predictionQuality = 1f;

    //=============================
    // CONSTRUCTOR
    //=============================
    public SnipeState(A_Smart tank, GameObject target) : base(tank)
    {
        this.target = target;
        this.lastKnownPosition = target.transform.position;
        this.lastKnownVelocity = target.GetComponent<Rigidbody>().velocity;
    }

    //=============================
    // STATE METHODS
    //=============================
    public override void Enter()
    {
        Debug.Log("[SnipeState] Entered. Engaging target with predictive firing.");
        InitializeCombat();
    }

    public override void Execute()
    {
        // Update target tracking
        UpdateTargetTracking();

        // Check if we should stay in combat
        if (!ShouldStayInCombat())
        {
            HandleCombatExit();
            return;
        }

        // Handle combat positioning and firing
        HandleCombat();
    }

    //=============================
    // COMBAT METHODS
    //=============================
    private void InitializeCombat()
    {
        snipeTimer = MAX_SNIPE_DURATION;
        hasFired = false;
        predictionQuality = 1f;
        tank.PerformScan(); // Initial scan
    }

    private void HandleCombat()
    {
        float distanceToTarget = Vector3.Distance(tank.transform.position, target.transform.position);

        // Maintain optimal range
        if (!IsInOptimalRange(distanceToTarget))
        {
            AdjustPosition(distanceToTarget);
            return;
        }

        // Aim and fire if we have a clear shot
        if (IsTargetInFiringPosition())
        {
            PredictAndShoot();
        }
        else
        {
            FindBetterFiringPosition();
        }
    }

    //=============================
    // SHOOTING METHODS
    //=============================
    private void PredictAndShoot()
    {
        if (tank.IsTankFiring() || hasFired) return;

        // Calculate predicted position with improved accuracy
        Vector3 predictedPosition = CalculatePredictedPosition();
        lastPredictedPosition = predictedPosition;

        // Create temporary aim target
        GameObject aimTarget = new GameObject("AimTarget");
        aimTarget.transform.position = predictedPosition;

        // Fire and track shot accuracy
        tank.FireAtPoint(aimTarget);
        hasFired = true;

        // Cleanup
        GameObject.Destroy(aimTarget);
    }

    private Vector3 CalculatePredictedPosition()
    {
        // Get base prediction from A_Smart
        Vector3 basePrediction = tank.PredictTargetPosition(target);

        // Apply additional prediction based on past accuracy
        Vector3 adjustedPrediction = basePrediction + (lastKnownVelocity * lastShotAccuracy);

        // Apply prediction quality modifier
        return Vector3.Lerp(basePrediction, adjustedPrediction, predictionQuality);
    }

    //=============================
    // POSITIONING METHODS
    //=============================
    private void AdjustPosition(float distanceToTarget)
    {
        Vector3 directionToTarget = (target.transform.position - tank.transform.position).normalized;
        Vector3 desiredPosition;

        if (distanceToTarget < OPTIMAL_RANGE_MIN)
        {
            // Move away from target
            desiredPosition = tank.transform.position - directionToTarget *
                (OPTIMAL_RANGE_MIN - distanceToTarget);
        }
        else
        {
            // Move closer to target
            desiredPosition = tank.transform.position + directionToTarget *
                (distanceToTarget - OPTIMAL_RANGE_MAX);
        }

        // Move with fuel efficiency in mind
        float speed = CalculateOptimalSpeed(distanceToTarget);
        tank.FollowPathToPoint(desiredPosition, speed, tank.heuristicMode);
    }

    private void FindBetterFiringPosition()
    {
        // Calculate perpendicular position for better angle
        Vector3 directionToTarget = (target.transform.position - tank.transform.position).normalized;
        Vector3 perpendicularDirection = Vector3.Cross(directionToTarget, Vector3.up);

        // Try both left and right positions
        Vector3 leftPosition = tank.transform.position - perpendicularDirection * 8f;
        Vector3 rightPosition = tank.transform.position + perpendicularDirection * 8f;

        // Choose the better position based on line of sight and distance
        Vector3 betterPosition = EvaluatePosition(leftPosition) > EvaluatePosition(rightPosition)
            ? leftPosition : rightPosition;

        tank.FollowPathToPoint(betterPosition, 0.5f, tank.heuristicMode);
    }

    //=============================
    // UTILITY METHODS
    //=============================
    private void UpdateTargetTracking()
    {
        if (target != null)
        {
            // Update position and velocity tracking
            Vector3 newPosition = target.transform.position;
            Vector3 newVelocity = target.GetComponent<Rigidbody>().velocity;

            // Calculate acceleration for better prediction
            Vector3 acceleration = (newVelocity - lastKnownVelocity) / Time.deltaTime;

            lastKnownPosition = newPosition;
            lastKnownVelocity = newVelocity;

            // Update prediction quality based on target's movement
            UpdatePredictionQuality(acceleration.magnitude);
        }
    }

    private void UpdatePredictionQuality(float targetAcceleration)
    {
        // Reduce prediction quality when target is making sharp movements
        const float MAX_STABLE_ACCELERATION = 5f;
        predictionQuality = Mathf.Lerp(0.5f, 1f,
            Mathf.Clamp01(1f - (targetAcceleration / MAX_STABLE_ACCELERATION)));
    }

    private bool IsTargetInFiringPosition()
    {
        RaycastHit hit;
        Vector3 directionToTarget = (target.transform.position - tank.transform.position).normalized;
        if (Physics.Raycast(tank.transform.position, directionToTarget, out hit))
        {
            return hit.collider.gameObject == target;
        }
        return false;
    }

    private float EvaluatePosition(Vector3 position)
    {
        float score = 0f;

        // Check line of sight
        RaycastHit hit;
        Vector3 directionToTarget = (target.transform.position - position).normalized;
        if (!Physics.Raycast(position, directionToTarget, out hit))
            score += 50f;

        // Evaluate distance
        float distance = Vector3.Distance(position, target.transform.position);
        if (distance >= OPTIMAL_RANGE_MIN && distance <= OPTIMAL_RANGE_MAX)
            score += 30f;

        // Consider fuel efficiency
        float distanceFromCurrent = Vector3.Distance(position, tank.transform.position);
        score -= distanceFromCurrent * 0.5f;

        return score;
    }

    private bool IsInOptimalRange(float distance)
    {
        return distance >= OPTIMAL_RANGE_MIN && distance <= OPTIMAL_RANGE_MAX;
    }

    private float CalculateOptimalSpeed(float distanceToTarget)
    {
        if (tank.GetFuelLevel() < A_Smart.StateThresholds.LOW_FUEL)
            return 0.4f;
        if (distanceToTarget < CRITICAL_RANGE)
            return 0.8f; // Faster when too close
        return 0.6f;
    }

    private bool ShouldStayInCombat()
    {
        // Check if target still exists and is active
        if (target == null || !target.activeSelf)
            return false;

        // Check resources
        if (tank.GetFuelLevel() < A_Smart.StateThresholds.CRITICAL_FUEL ||
            tank.GetHealthLevel() < A_Smart.StateThresholds.CRITICAL_HEALTH)
            return false;

        // Check timer
        snipeTimer -= Time.deltaTime;
        return snipeTimer > 0;
    }

    private void HandleCombatExit()
    {
        if (tank.GetFuelLevel() < A_Smart.StateThresholds.CRITICAL_FUEL ||
            tank.GetHealthLevel() < A_Smart.StateThresholds.CRITICAL_HEALTH)
        {
            Debug.Log("[SnipeState] Critical resources low, switching to wait state.");
            tank.ChangeState(new WaitState(tank));
        }
        else
        {
            Debug.Log("[SnipeState] Combat timeout, switching to chase state.");
            tank.ChangeState(new ChaseState(tank, target));
        }
    }

    public override void Exit()
    {
        Debug.Log("[SnipeState] Exiting snipe state.");
    }
}