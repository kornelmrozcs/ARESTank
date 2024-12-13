using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static AStar;

/// <summary>
/// FuelMaster Tank AI - focuses on fuel conservation and resource control
/// Uses waiting strategy and forces enemies to waste fuel
/// </summary>
public class FuelMaster_Tank : AITank
{
    // Sensor data collections
    private Dictionary<GameObject, float> enemyTanksFound = new Dictionary<GameObject, float>();
    private Dictionary<GameObject, float> consumablesFound = new Dictionary<GameObject, float>();
    private Dictionary<GameObject, float> enemyBasesFound = new Dictionary<GameObject, float>();

    // Target references
    private GameObject enemyTank;
    private GameObject currentConsumable;
    private GameObject enemyBase;

    // Strategy parameters
    private readonly float initialWaitTime = 4f;    // Initial waiting time
    private readonly float minFuelToMove = 20f;     // Minimum fuel to allow movement
    private readonly float scanRotationSpeed = 30f;  // Degrees per second for scanning
    private readonly float optimalDistance = 35f;    // Optimal distance from enemy
    private float waitTimer = 0f;
    private float scanTimer = 0f;
    private float resourceCheckTimer = 0f;

    // Resource tracking
    private List<Vector3> resourceSpawnPoints = new List<Vector3>();
    private Vector3 lastEnemyPosition;
    private bool isInitialWait = true;
    private bool isScanning = false;

    public HeuristicMode heuristicMode;

    /// <summary>
    /// Initialize the tank
    /// </summary>
    public override void AITankStart()
    {
        Debug.Log("[FuelMaster] Tank initialized - Starting waiting phase");
        heuristicMode = HeuristicMode.Manhattan; // Most fuel efficient
    }

    /// <summary>
    /// Main update loop
    /// </summary>
    public override void AITankUpdate()
    {
        // Update visible objects
        UpdateVisibleObjects();

        // Initial waiting phase
        if (isInitialWait)
        {
            HandleInitialWait();
            return;
        }

        // Main strategy execution
        if (TankCurrentFuel <= minFuelToMove)
        {
            // Conservation mode when low on fuel
            HandleLowFuel();
        }
        else if (IsEnemyThreatening())
        {
            // Escape from enemy while making them waste fuel
            HandleEnemyThreat();
        }
        else if (ShouldCollectResource())
        {
            // Collect valuable resources
            HandleResourceCollection();
        }
        else
        {
            // Default scanning behavior
            HandleScanning();
        }

        // Track resource spawns
        TrackResourceSpawns();
    }

    private void UpdateVisibleObjects()
    {
        enemyTanksFound = VisibleEnemyTanks;
        consumablesFound = VisibleConsumables;
        enemyBasesFound = VisibleEnemyBases;

        // Update enemy tracking
        if (enemyTanksFound.Count > 0)
        {
            enemyTank = enemyTanksFound.First().Key;
            lastEnemyPosition = enemyTank.transform.position;
        }
    }

    private void HandleInitialWait()
    {
        waitTimer += Time.deltaTime;
        TankStop();

        // Rotate to scan during wait
        transform.Rotate(0, scanRotationSpeed * Time.deltaTime, 0);

        if (waitTimer >= initialWaitTime)
        {
            isInitialWait = false;
            Debug.Log("[FuelMaster] Initial wait complete - Starting normal operation");
        }
    }

    private void HandleLowFuel()
    {
        Debug.Log("[FuelMaster] Low fuel mode - Minimizing movement");
        TankStop();

        // Only move if fuel pickup is very close
        var fuelPickup = consumablesFound.FirstOrDefault(x => x.Key.CompareTag("Fuel"));
        if (fuelPickup.Key != null && Vector3.Distance(transform.position, fuelPickup.Key.transform.position) < 10f)
        {
            FollowPathToWorldPoint(fuelPickup.Key, 0.5f, heuristicMode);
        }
    }

    private void HandleEnemyThreat()
    {
        if (enemyTank != null)
        {
            // Calculate escape direction (opposite of enemy)
            Vector3 escapeDirection = transform.position - enemyTank.transform.position;
            Vector3 escapePoint = transform.position + escapeDirection.normalized * optimalDistance;

            // Move away while conserving fuel
            FollowPathToWorldPoint(escapePoint, 0.7f, heuristicMode);

            // Keep turret facing enemy
            TurretFaceWorldPoint(enemyTank);
        }
    }

    private void HandleResourceCollection()
    {
        if (consumablesFound.Count > 0)
        {
            GameObject bestResource = GetBestResource();
            if (bestResource != null)
            {
                FollowPathToWorldPoint(bestResource, 0.8f, heuristicMode);
            }
        }
    }

    private void HandleScanning()
    {
        scanTimer += Time.deltaTime;
        if (scanTimer >= 2f)
        {
            isScanning = !isScanning;
            scanTimer = 0f;
        }

        if (isScanning)
        {
            TankStop();
            transform.Rotate(0, scanRotationSpeed * Time.deltaTime, 0);
        }
        else if (resourceSpawnPoints.Count > 0)
        {
            // Move to nearest known resource spawn point
            Vector3 nearestSpawn = GetNearestResourceSpawn();
            FollowPathToWorldPoint(nearestSpawn, 0.6f, heuristicMode);
        }
    }

    private void TrackResourceSpawns()
    {
        foreach (var resource in consumablesFound)
        {
            Vector3 spawnPoint = resource.Key.transform.position;
            if (!resourceSpawnPoints.Contains(spawnPoint))
            {
                resourceSpawnPoints.Add(spawnPoint);
                if (resourceSpawnPoints.Count > 5) // Keep only recent spawn points
                {
                    resourceSpawnPoints.RemoveAt(0);
                }
            }
        }
    }

    private bool IsEnemyThreatening()
    {
        return enemyTank != null &&
               Vector3.Distance(transform.position, enemyTank.transform.position) < optimalDistance;
    }

    private bool ShouldCollectResource()
    {
        return consumablesFound.Any(x =>
            x.Key.CompareTag("Fuel") ||
            (x.Key.CompareTag("Health") && TankCurrentHealth < 30f));
    }

    private GameObject GetBestResource()
    {
        // Prioritize fuel
        var fuelPickup = consumablesFound.FirstOrDefault(x => x.Key.CompareTag("Fuel"));
        if (fuelPickup.Key != null) return fuelPickup.Key;

        // Then health if low
        if (TankCurrentHealth < 30f)
        {
            var healthPickup = consumablesFound.FirstOrDefault(x => x.Key.CompareTag("Health"));
            if (healthPickup.Key != null) return healthPickup.Key;
        }

        return null;
    }

    private Vector3 GetNearestResourceSpawn()
    {
        return resourceSpawnPoints
            .OrderBy(point => Vector3.Distance(transform.position, point))
            .First();
    }

    public override void AIOnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            // On collision, generate new point and rotate
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