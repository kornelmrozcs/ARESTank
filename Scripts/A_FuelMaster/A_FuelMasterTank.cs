using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static AStar;

/// <summary>
/// FuelMaster Tank AI - Advanced Resource Control and Survival Strategy
/// 
/// Core Strategy:
/// 1. Fuel Conservation
///    - Minimal movement in early game
///    - Strategic positioning to force enemy movement
///    - Efficient path planning using Manhattan heuristic
///    - Fuel-efficient escape routes
///
/// 2. Resource Control
///    - Immediate collection of all visible resources
///    - Priority: Fuel > Health (when low) > Ammo
///    - Tracking of resource spawn points
///    - Strategic positioning near resource spawns
///
/// 3. Enemy Management
///    - Smart escape system with 8-directional checking
///    - Maintains optimal distance (35 units) to stay out of enemy range
///    - Obstacle-aware evasion tactics
///    - Forces enemy to waste fuel by chasing
///    - Always moves away from enemy in safe directions
///
/// 4. State Management
///    - InitialWait: Conserves fuel while enemy moves
///    - LowFuel: Minimal movement, survival mode
///    - EnemyThreat: Advanced evasion tactics
///    - ResourceCollection: Aggressive resource gathering
///    - Scanning: Efficient area monitoring
///
/// 5. Survival Tactics
///    - Obstacle avoidance during escape
///    - Multi-directional safety checking
///    - Strategic positioning to avoid getting cornered
///    - Continuous environment assessment
/// </summary>
public class FuelMaster_Tank : AITank
{
    // FSM States
    private enum TankState { InitialWait, LowFuel, EnemyThreat, ResourceCollection, Scanning }
    private TankState currentState;

    // Sensor data collections
    private Dictionary<GameObject, float> enemyTanksFound = new Dictionary<GameObject, float>();
    private Dictionary<GameObject, float> consumablesFound = new Dictionary<GameObject, float>();
    private Dictionary<GameObject, float> enemyBasesFound = new Dictionary<GameObject, float>();

    // Target references
    private GameObject enemyTank;
    private GameObject currentConsumable;

    // Strategy parameters
    private readonly float initialWaitTime = 4f;    // Initial waiting time
    private readonly float minFuelToMove = 20f;     // Minimum fuel to allow movement
    private readonly float scanRotationSpeed = 50f; // Degrees per second for scanning
    private readonly float optimalDistance = 45f;   // Optimal distance from enemy
    private readonly float resourceCollectionSpeed = 1f; // Full speed for resource collection

    // State tracking
    private float stateTimer = 0f;
    private float scanTimer = 0f;

    // Resource management
    private List<Vector3> resourceSpawnPoints = new List<Vector3>();
    private Vector3 lastEnemyPosition;

    // Pathfinding settings
    public HeuristicMode heuristicMode;

    /// <summary>
    /// Initialize the tank with Manhattan heuristic for optimal pathfinding
    /// </summary>
    public override void AITankStart()
    {
        Debug.Log("[FuelMaster] Tank initialized with resource-focused strategy");
        heuristicMode = HeuristicMode.Diagonal;
        TransitionToState(TankState.InitialWait);
    }

    /// <summary>
    /// Main update cycle - Updates sensor data and executes current state behavior
    /// </summary>
    public override void AITankUpdate()
    {
        UpdateVisibleObjects();
        ExecuteCurrentState();
    }

    /// <summary>
    /// State transition handler with logging
    /// </summary>
    private void TransitionToState(TankState newState)
    {
        currentState = newState;
        stateTimer = 0f;
        Debug.Log($"[FuelMaster] State Transition: {newState}");
    }

    /// <summary>
    /// Central state execution handler
    /// </summary>
    private void ExecuteCurrentState()
    {
        stateTimer += Time.deltaTime;

        // Always check for resources first
        if (consumablesFound.Count > 0 && currentState != TankState.ResourceCollection)
        {
            TransitionToState(TankState.ResourceCollection);
            return;
        }

        switch (currentState)
        {
            case TankState.InitialWait:
                HandleInitialWait();
                break;
            case TankState.LowFuel:
                HandleLowFuel();
                break;
            case TankState.EnemyThreat:
                HandleEnemyThreat();
                break;
            case TankState.ResourceCollection:
                HandleResourceCollection();
                break;
            case TankState.Scanning:
                HandleScanning();
                break;
        }

        TrackResourceSpawns();
    }

    /// <summary>
    /// Updates sensor information about visible objects
    /// </summary>
    private void UpdateVisibleObjects()
    {
        enemyTanksFound = VisibleEnemyTanks;
        consumablesFound = VisibleConsumables;
        enemyBasesFound = VisibleEnemyBases;

        if (enemyTanksFound.Count > 0)
        {
            enemyTank = enemyTanksFound.First().Key;
            lastEnemyPosition = enemyTank.transform.position;
        }
    }

    /// <summary>
    /// Initial waiting state - Conserves fuel while enemy moves
    /// </summary>
    private void HandleInitialWait()
    {
        TankStop();
        transform.Rotate(0, scanRotationSpeed * Time.deltaTime, 0);

        if (stateTimer >= initialWaitTime)
        {
            TransitionToState(TankState.Scanning);
        }
    }

    /// <summary>
    /// Low fuel state - Minimal movement, focuses on survival
    /// </summary>
    private void HandleLowFuel()
    {
        TankStop();

        var fuelPickup = consumablesFound.FirstOrDefault(x => x.Key.CompareTag("Fuel"));
        if (fuelPickup.Key != null)
        {
            FollowPathToWorldPoint(fuelPickup.Key, resourceCollectionSpeed, heuristicMode);
        }

        if (TankCurrentFuel > minFuelToMove)
        {
            TransitionToState(TankState.Scanning);
        }
    }

    /// <summary>
    /// Enemy threat handling - Strategic evasion and repositioning
    /// </summary>
    private void HandleEnemyThreat()
    {
        if (enemyTank != null)
        {
            Vector3 safePosition = CalculateSafeEscapePosition();
            FollowPathToWorldPoint(safePosition, 0.7f, heuristicMode);
            TurretFaceWorldPoint(enemyTank);

            if (!IsEnemyThreatening())
            {
                TransitionToState(TankState.Scanning);
            }
        }
    }

    /// <summary>
    /// Resource collection state - Aggressive resource gathering
    /// </summary>
    private void HandleResourceCollection()
    {
        if (consumablesFound.Count > 0)
        {
            GameObject nearestResource = GetNearestResource();
            if (nearestResource != null)
            {
                FollowPathToWorldPoint(nearestResource, resourceCollectionSpeed, heuristicMode);
                return;
            }
        }

        TransitionToState(TankState.Scanning);
    }

    /// <summary>
    /// Scanning state - Efficient area monitoring
    /// </summary>
    private void HandleScanning()
    {
        transform.Rotate(0, scanRotationSpeed * Time.deltaTime, 0);

        if (TankCurrentFuel <= minFuelToMove)
        {
            TransitionToState(TankState.LowFuel);
        }
        else if (IsEnemyThreatening())
        {
            TransitionToState(TankState.EnemyThreat);
        }
    }

    /// <summary>
    /// Calculates optimal escape position by checking multiple directions
    /// </summary>
    private Vector3 CalculateSafeEscapePosition()
    {
        Vector3 escapeDirection = (transform.position - enemyTank.transform.position).normalized;
        List<Vector3> potentialEscapePoints = new List<Vector3>();

        // Check 8 different directions
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector3 rotatedDirection = Quaternion.Euler(0, angle, 0) * escapeDirection;
            Vector3 potentialPoint = transform.position + rotatedDirection * optimalDistance;

            Node escapeNode = NodePositionInGrid(potentialPoint);

            if (escapeNode != null && escapeNode.traversable)
            {
                float enemyDistance = Vector3.Distance(potentialPoint, enemyTank.transform.position);

                bool hasDirectPath = !Physics.Raycast(
                    transform.position,
                    rotatedDirection,
                    optimalDistance,
                    LayerMask.GetMask("Obstacle")
                );

                if (hasDirectPath)
                {
                    potentialEscapePoints.Add(potentialPoint);
                }
            }
        }

        if (potentialEscapePoints.Count == 0)
        {
            return transform.position + escapeDirection * (optimalDistance / 2);
        }

        return potentialEscapePoints
            .OrderByDescending(point =>
                Vector3.Distance(point, enemyTank.transform.position) +
                (IsNearResourceSpawn(point) ? 10f : 0f))
            .First();
    }

    /// <summary>
    /// Checks if position is near known resource spawns
    /// </summary>
    private bool IsNearResourceSpawn(Vector3 position)
    {
        return resourceSpawnPoints.Any(spawn =>
            Vector3.Distance(position, spawn) < 15f);
    }

    /// <summary>
    /// Gets the node at a specific world position
    /// </summary>
    private Node NodePositionInGrid(Vector3 worldPosition)
    {
        AStar aStarScript = GameObject.Find("AStarPlane").GetComponent<AStar>();
        return aStarScript.NodePositionInGrid(worldPosition);
    }

    /// <summary>
    /// Enhanced threat detection with multiple factors
    /// </summary>
    private bool IsEnemyThreatening()
    {
        if (enemyTank == null) return false;

        float distanceToEnemy = Vector3.Distance(transform.position, enemyTank.transform.position);

        bool isTooClose = distanceToEnemy < optimalDistance;
        bool isNearObstacle = Physics.CheckSphere(transform.position, 5f, LayerMask.GetMask("Obstacle"));
        bool isCollectingResource = currentState == TankState.ResourceCollection;

        return isTooClose ||
               (isNearObstacle && distanceToEnemy < optimalDistance * 1.5f) ||
               (isCollectingResource && distanceToEnemy < optimalDistance * 1.2f);
    }

    /// <summary>
    /// Gets nearest available resource
    /// </summary>
    private GameObject GetNearestResource()
    {
        if (consumablesFound.Count == 0) return null;

        return consumablesFound
            .OrderBy(x => Vector3.Distance(transform.position, x.Key.transform.position))
            .First()
            .Key;
    }

    /// <summary>
    /// Tracks resource spawn points for future reference
    /// </summary>
    private void TrackResourceSpawns()
    {
        foreach (var resource in consumablesFound)
        {
            Vector3 spawnPoint = resource.Key.transform.position;
            if (!resourceSpawnPoints.Contains(spawnPoint))
            {
                resourceSpawnPoints.Add(spawnPoint);
                if (resourceSpawnPoints.Count > 5)
                {
                    resourceSpawnPoints.RemoveAt(0);
                }
            }
        }
    }

    public override void AIOnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            GenerateNewRandomWorldPoint();
            transform.Rotate(0, 90, 0);
        }
    }
    #region Helper Methods from DumbTank
    public void GeneratePathToWorldPoint(GameObject pointInWorld)
    {
        a_FindPathToPoint(pointInWorld);
    }

    public void GeneratePathToWorldPoint(GameObject pointInWorld, HeuristicMode heuristic)
    {
        a_FindPathToPoint(pointInWorld, heuristic);
    }

    public void FollowPathToWorldPoint(GameObject pointInWorld, float normalizedSpeed)
    {
        a_FollowPathToPoint(pointInWorld, normalizedSpeed);
    }

    public void FollowPathToWorldPoint(GameObject pointInWorld, float normalizedSpeed, HeuristicMode heuristic)
    {
        a_FollowPathToPoint(pointInWorld, normalizedSpeed, heuristic);
    }

    public void FollowPathToWorldPoint(Vector3 position, float normalizedSpeed, HeuristicMode heuristic)
    {
        GameObject tempTarget = new GameObject("TempTarget");
        tempTarget.transform.position = position;
        a_FollowPathToPoint(tempTarget, normalizedSpeed, heuristic);
        GameObject.Destroy(tempTarget);
    }

    public void FollowPathToRandomWorldPoint(float normalizedSpeed)
    {
        a_FollowPathToRandomPoint(normalizedSpeed);
    }

    public void FollowPathToRandomWorldPoint(float normalizedSpeed, HeuristicMode heuristic)
    {
        a_FollowPathToRandomPoint(normalizedSpeed, heuristic);
    }

    public void GenerateNewRandomWorldPoint()
    {
        a_GenerateRandomPoint();
    }

    public void TankStop()
    {
        a_StopTank();
    }

    public void TankGo()
    {
        a_StartTank();
    }

    public void TurretFaceWorldPoint(GameObject pointInWorld)
    {
        a_FaceTurretToPoint(pointInWorld);
    }

    public void TurretReset()
    {
        a_ResetTurret();
    }

    public void TurretFireAtPoint(GameObject pointInWorld)
    {
        a_FireAtPoint(pointInWorld);
    }

    protected Dictionary<GameObject, float> VisibleEnemyTanks => a_TanksFound;
    protected Dictionary<GameObject, float> VisibleConsumables => a_ConsumablesFound;
    protected Dictionary<GameObject, float> VisibleEnemyBases => a_BasesFound;
    protected float TankCurrentHealth => a_GetHealthLevel;
    protected float TankCurrentAmmo => a_GetAmmoLevel;
    protected float TankCurrentFuel => a_GetFuelLevel;
    protected List<GameObject> MyBases => a_GetMyBases;
    #endregion
}