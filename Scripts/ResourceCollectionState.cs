using UnityEngine;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// ResourceCollectionState handles efficient resource gathering while maintaining tactical awareness.
/// Key Features:
/// - Prioritizes resources based on current needs
/// - Maintains memory of resource spawn locations
/// - Efficient path planning between resources
/// - Monitors threats while collecting
/// </summary>
public class ResourceCollectionState : TankState
{
    //=============================
    // COLLECTION PROPERTIES
    //=============================
    private int currentPositionIndex = 0;
    private float stateTimer = 0f;
    private Vector3 currentTarget;
    private bool isMovingToResource = false;
    private float lastCollectionTime = 0f;

    //=============================
    // THRESHOLDS & DISTANCES
    //=============================
    private const float POSITION_REACHED_DISTANCE = 5f;
    private const float COLLECTION_TIMEOUT = 15f;
    private const float SAFE_DISTANCE = 20f;

    //=============================
    // CONSTRUCTOR
    //=============================
    public ResourceCollectionState(A_Smart tank) : base(tank)
    {
        lastCollectionTime = Time.time;
    }

    //=============================
    // STATE METHODS
    //=============================
    public override void Enter()
    {
        Debug.Log("[ResourceCollectionState] Entered. Beginning resource collection pattern.");
        SelectNextTarget();
    }

    public override void Execute()
    {
        // Regular scanning while collecting
        if (tank.PerformScan())
        {
            return;
        }

        // Check for immediate threats or opportunities
        if (HandleThreatsAndOpportunities())
        {
            return;
        }

        // Update collection process
        stateTimer += Time.deltaTime;

        // Handle collection logic
        if (isMovingToResource)
        {
            HandleResourceCollection();
        }
        else
        {
            PatrolKnownPositions();
        }
    }

    //=============================
    // COLLECTION METHODS
    //=============================
    private void HandleResourceCollection()
    {
        float distanceToTarget = Vector3.Distance(tank.transform.position, currentTarget);

        // Check if we've reached the current resource
        if (distanceToTarget < POSITION_REACHED_DISTANCE)
        {
            Debug.Log("[ResourceCollectionState] Reached resource position.");
            isMovingToResource = false;
            lastCollectionTime = Time.time;
            SelectNextTarget();
            return;
        }

        // Move towards resource with fuel-efficient speed
        float speed = CalculateOptimalSpeed(distanceToTarget);
        tank.FollowPathToPoint(currentTarget, speed, tank.heuristicMode);
    }

    private float CalculateOptimalSpeed(float distance)
    {
        // Use lower speed when fuel is low or target is close
        if (tank.GetFuelLevel() < A_Smart.StateThresholds.LOW_FUEL)
            return 0.6f;
        if (distance < 10f)
            return 0.7f;
        return 0.8f;
    }

    private void PatrolKnownPositions()
    {
        // Check for any visible high-priority resources
        var nearbyResource = FindBestNearbyResource();
        if (nearbyResource != null)
        {
            currentTarget = nearbyResource.transform.position;
            isMovingToResource = true;
            tank.FollowPathToPoint(nearbyResource, 0.7f, tank.heuristicMode);
            return;
        }

        // Move between known spawn points
        if (tank.lastConsumablePositions.Count > 0)
        {
            MoveToNextKnownPosition();
        }
        else
        {
            Debug.Log("[ResourceCollectionState] No known positions, switching to explore.");
            tank.ChangeState(new ExploreState(tank));
        }
    }

    //=============================
    // THREAT HANDLING
    //=============================
    private bool HandleThreatsAndOpportunities()
    {
        // Check for enemies with low health - opportunity to attack
        if (tank.enemyTanksFound.Count > 0)
        {
            var enemy = tank.enemyTanksFound.First().Key;
            var enemyHealth = enemy.GetComponent<DumbTank>().TankCurrentHealth;

            if (enemyHealth <= A_Smart.StateThresholds.WEAK_ENEMY_HEALTH &&
                tank.GetFuelLevel() > A_Smart.StateThresholds.LOW_FUEL)
            {
                Debug.Log("[ResourceCollectionState] Found weak enemy, switching to chase.");
                tank.ChangeState(new ChaseState(tank, enemy));
                return true;
            }
            else if (Vector3.Distance(tank.transform.position, enemy.transform.position) < SAFE_DISTANCE)
            {
                Debug.Log("[ResourceCollectionState] Enemy too close, switching to dodge.");
                tank.ChangeState(new DodgingState(tank, enemy));
                return true;
            }
        }

        // Check if collection is taking too long
        if (Time.time - lastCollectionTime > COLLECTION_TIMEOUT)
        {
            Debug.Log("[ResourceCollectionState] Collection timeout, switching to explore.");
            tank.ChangeState(new ExploreState(tank));
            return true;
        }

        return false;
    }

    //=============================
    // RESOURCE EVALUATION
    //=============================
    private GameObject FindBestNearbyResource()
    {
        float bestScore = float.MinValue;
        GameObject bestResource = null;

        foreach (var resource in tank.consumablesFound)
        {
            float score = EvaluateResourceValue(resource.Key, resource.Value);
            if (score > bestScore)
            {
                bestScore = score;
                bestResource = resource.Key;
            }
        }

        return bestResource;
    }

    private float EvaluateResourceValue(GameObject resource, float distance)
    {
        if (resource == null) return float.MinValue;

        float score = 100f - (distance * 2f); // Base score on distance

        // Adjust based on resource type and current needs
        switch (resource.tag)
        {
            case "Fuel":
                score *= (100f / tank.GetFuelLevel()) * 1.5f;
                if (tank.GetFuelLevel() < A_Smart.StateThresholds.CRITICAL_FUEL)
                    score *= 2f;
                break;
            case "Health":
                score *= (100f / tank.GetHealthLevel()) * 1.2f;
                if (tank.GetHealthLevel() < A_Smart.StateThresholds.CRITICAL_HEALTH)
                    score *= 2f;
                break;
            case "Ammo":
                score *= (20f / tank.GetAmmoLevel()) * 0.8f;
                break;
        }

        return score;
    }

    //=============================
    // POSITION MANAGEMENT
    //=============================
    private void SelectNextTarget()
    {
        if (tank.lastConsumablePositions.Count > 0)
        {
            currentPositionIndex = (currentPositionIndex + 1) % tank.lastConsumablePositions.Count;
            Debug.Log($"[ResourceCollectionState] Moving to position {currentPositionIndex}");
        }
    }

    private void MoveToNextKnownPosition()
    {
        if (tank.lastConsumablePositions.Count == 0) return;

        currentTarget = tank.lastConsumablePositions[currentPositionIndex];
        tank.FollowPathToPoint(currentTarget, 0.7f, tank.heuristicMode);

        if (Vector3.Distance(tank.transform.position, currentTarget) < POSITION_REACHED_DISTANCE)
        {
            SelectNextTarget();
        }
    }

    public override void Exit()
    {
        Debug.Log("[ResourceCollectionState] Exiting resource collection state.");
    }
}