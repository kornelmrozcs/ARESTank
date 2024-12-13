using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using static AStar;

/// <summary>
/// Smart AI Tank Implementation with advanced resource management and tactical combat.
/// Key Features:
/// - Efficient fuel conservation strategy
/// - Advanced resource tracking and collection
/// - Predictive combat system
/// - Multi-state behavioral system
/// - Optimized scanning and threat detection
/// 
/// Version: 4.0
/// Last Updated: 13.12.2024
/// </summary>
public class A_Smart : AITank
{
    //=============================
    // SHARED STATE THRESHOLDS
    //=============================
    public static class StateThresholds
    {
        // Resource Critical Levels
        public const float CRITICAL_FUEL = 15f;
        public const float CRITICAL_HEALTH = 25f;
        public const float LOW_FUEL = 30f;
        public const float LOW_HEALTH = 35f;

        // Combat Ranges
        public const float OPTIMAL_COMBAT_RANGE = 35f;
        public const float MIN_COMBAT_RANGE = 25f;
        public const float MAX_CHASE_RANGE = 40f;

        // Combat Thresholds
        public const float WEAK_ENEMY_HEALTH = 25f;
        public const float HEALTH_ADVANTAGE = 10f;

        // Resource Collection
        public const float RESOURCE_COLLECTION_RANGE = 15f;
        public const float CRITICAL_RESOURCE_RANGE = 10f;
    }

    //=============================
    // CORE TANK PROPERTIES
    //=============================
    private TankState currentState;
    private float fireCooldown = 4f;
    private bool isFiring = false;
    private float reducedSpeed = 0.5f;
    public GameObject strafeTarget;

    //=============================
    // VISIBLE ENTITIES & PATHFINDING
    //=============================
    public Dictionary<GameObject, float> enemyTanksFound => a_TanksFound;
    public Dictionary<GameObject, float> consumablesFound => a_ConsumablesFound;
    public Dictionary<GameObject, float> enemyBasesFound => a_BasesFound;
    public HeuristicMode heuristicMode = HeuristicMode.DiagonalShort; // Default to most efficient

    //=============================
    // SCANNING SYSTEM PROPERTIES
    //=============================
    private float scanRotationSpeed = 180f; // degrees per second
    private float timeBetweenScans = 5f;
    private float currentScanTime = 0f;
    private bool isScanning = false;
    private float scanAngle = 0f;
    private Queue<Vector3> recentScans = new Queue<Vector3>();
    private const int MAX_RECENT_SCANS = 5;

    //=============================
    // TARGET TRACKING SYSTEM
    //=============================
    private Vector3 lastKnownTargetVelocity;
    private Vector3 lastKnownTargetPosition;
    private Dictionary<GameObject, TargetInfo> targetMemory = new Dictionary<GameObject, TargetInfo>();

    //=============================
    // RESOURCE MANAGEMENT
    //=============================
    private ResourceManager resourceManager;
    private List<Vector3> lastConsumablePositions = new List<Vector3>();
    private Dictionary<string, int> consumableTypeCount = new Dictionary<string, int>();
    private const int MAX_TRACKED_POSITIONS = 5;

    //=============================
    // COMBAT PROPERTIES
    //=============================
    private const float PROJECTILE_SPEED = 60f;
    private const float SHOOT_DELAY = 2f;
    private float previousAmmoLevel = 0f;
    public float TankCurrentAmmo => a_GetAmmoLevel;

    //=============================
    // INITIALIZATION METHODS
    //=============================
    public override void AITankStart()
    {
        Debug.Log("[A_Smart] Tank AI Initialized.");
        InitializeComponents();
        InitializeTracking();
        // Start with WaitState to implement our fuel conservation strategy
        ChangeState(new WaitState(this));
    }

    private void InitializeComponents()
    {
        strafeTarget = new GameObject("StrafeTarget");
        strafeTarget.transform.SetParent(transform);
        resourceManager = new ResourceManager(this);
    }

    private void InitializeTracking()
    {
        consumableTypeCount["Fuel"] = 0;
        consumableTypeCount["Health"] = 0;
        consumableTypeCount["Ammo"] = 0;
    }

    //=============================
    // CORE UPDATE METHODS
    //=============================
    public override void AITankUpdate()
    {
        if (currentState == null)
        {
            Debug.LogError("[A_Smart] Current state is null. Switching to WaitState as fallback.");
            ChangeState(new WaitState(this));
            return;
        }
        currentState.Execute();
    }

    //=============================
    // SCANNING SYSTEM METHODS
    //=============================
    /// <summary>
    /// Performs a 360-degree scan of the environment, tracking enemies and resources.
    /// </summary>
    /// <returns>True if currently scanning, false otherwise.</returns>
    public bool PerformScan()
    {
        if (!isScanning && currentScanTime < timeBetweenScans)
        {
            currentScanTime += Time.deltaTime;
            return false;
        }

        float rotationThisFrame = scanRotationSpeed * Time.deltaTime;
        transform.Rotate(0, rotationThisFrame, 0);
        scanAngle += rotationThisFrame;

        ProcessScanResults();

        if (scanAngle >= 360f)
        {
            ResetScan();
            return false;
        }

        return true;
    }

    private void ProcessScanResults()
    {
        // Process enemies
        foreach (var enemy in enemyTanksFound)
        {
            UpdateTargetInfo(enemy.Key);
        }

        // Process resources
        foreach (var consumable in consumablesFound)
        {
            TrackConsumablePosition(consumable.Key);
            AddToRecentScans(consumable.Key.transform.position);
        }
    }

    private void ResetScan()
    {
        isScanning = false;
        scanAngle = 0f;
        currentScanTime = 0f;
    }

    //=============================
    // TARGET TRACKING METHODS
    //=============================
    private void UpdateTargetInfo(GameObject target)
    {
        if (!targetMemory.ContainsKey(target))
        {
            targetMemory[target] = new TargetInfo();
        }

        var targetRb = target.GetComponent<Rigidbody>();
        targetMemory[target].UpdateInfo(
            target.transform.position,
            targetRb.velocity,
            target.GetComponent<DumbTank>().TankCurrentHealth
        );
    }

    /// <summary>
    /// Predicts target's future position based on current movement and acceleration.
    /// </summary>
    public Vector3 PredictTargetPosition(GameObject target, float additionalTimeOffset = 0f)
    {
        if (target == null || !targetMemory.ContainsKey(target))
            return Vector3.zero;

        var info = targetMemory[target];
        float distance = Vector3.Distance(transform.position, target.transform.position);
        float timeToTarget = (distance / PROJECTILE_SPEED) + SHOOT_DELAY + additionalTimeOffset;

        return info.PredictPosition(timeToTarget);
    }

    //=============================
    // RESOURCE MANAGEMENT METHODS
    //=============================
    private void TrackConsumablePosition(GameObject consumable)
    {
        if (consumable == null) return;

        string consumableType = consumable.tag;
        Vector3 position = consumable.transform.position;

        if (!lastConsumablePositions.Contains(position))
        {
            UpdateConsumableTracking(position, consumableType);
        }
    }

    private void UpdateConsumableTracking(Vector3 position, string consumableType)
    {
        lastConsumablePositions.Add(position);
        if (lastConsumablePositions.Count > MAX_TRACKED_POSITIONS)
        {
            lastConsumablePositions.RemoveAt(0);
        }

        consumableTypeCount[consumableType]++;
        Debug.Log($"[A_Smart] Tracked new {consumableType} at position {position}");
    }

    //=============================
    // COMBAT METHODS
    //=============================
    public void FireAtPoint(GameObject target)
    {
        if (!isFiring && target != null)
        {
            StartCoroutine(FireAndMove(target));
        }
    }

    private IEnumerator FireAndMove(GameObject target)
    {
        isFiring = true;
        a_FireAtPoint(target);
        yield return new WaitForSeconds(fireCooldown);
        isFiring = false;
    }

    //=============================
    // MOVEMENT METHODS
    //=============================
    public void FollowPathToRandomPoint(float normalizedSpeed, HeuristicMode heuristic)
    {
        a_FollowPathToRandomPoint(normalizedSpeed, heuristic);
    }

    public void FollowPathToPoint(GameObject target, float normalizedSpeed, HeuristicMode heuristic)
    {
        if (target != null)
        {
            a_FollowPathToPoint(target, normalizedSpeed, heuristic);
        }
    }

    public void FollowPathToPoint(Vector3 position, float normalizedSpeed, HeuristicMode heuristic)
    {
        GameObject tempTarget = new GameObject("TemporaryTarget");
        tempTarget.transform.position = position;
        a_FollowPathToPoint(tempTarget, normalizedSpeed, heuristic);
        GameObject.Destroy(tempTarget);
    }

    //=============================
    // STATE MANAGEMENT METHODS
    //=============================
    public void ChangeState(TankState newState)
    {
        if (newState == null)
        {
            Debug.LogError("[A_Smart] Attempted to change to a null state. Ignoring.");
            return;
        }

        Debug.Log($"[A_Smart] Transitioning from {currentState?.GetType().Name} to {newState.GetType().Name}.");
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    //=============================
    // UTILITY METHODS
    //=============================
    public float GetAmmoLevel() => a_GetAmmoLevel;
    public float GetHealthLevel() => a_GetHealthLevel;
    public float GetFuelLevel() => a_GetFuelLevel;
    public bool IsTankFiring() => a_IsFiring;

    public GameObject GetNearestConsumable(string type)
    {
        return consumablesFound
            .Where(x => x.Key.CompareTag(type))
            .OrderBy(x => x.Value)
            .Select(x => x.Key)
            .FirstOrDefault();
    }

    //=============================
    // HELPER CLASSES
    //=============================
    private class TargetInfo
    {
        public Vector3 Position { get; private set; }
        public Vector3 Velocity { get; private set; }
        public Vector3 Acceleration { get; private set; }
        public float Health { get; private set; }
        private Vector3 LastVelocity;

        public void UpdateInfo(Vector3 position, Vector3 velocity, float health)
        {
            Acceleration = (velocity - LastVelocity) / Time.deltaTime;
            Position = position;
            Velocity = velocity;
            LastVelocity = velocity;
            Health = health;
        }

        public Vector3 PredictPosition(float time)
        {
            return Position +
                   (Velocity * time) +
                   (0.5f * Acceleration * time * time);
        }
    }

    private class ResourceManager
    {
        private A_Smart tank;

        public ResourceManager(A_Smart tank)
        {
            this.tank = tank;
        }

        public float CalculateResourcePriority(GameObject resource)
        {
            if (resource == null) return 0f;

            float priority = 0f;
            float distance = Vector3.Distance(tank.transform.position, resource.transform.position);
            float currentFuel = tank.GetFuelLevel();
            float currentHealth = tank.GetHealthLevel();

            switch (resource.tag)
            {
                case "Fuel":
                    priority = (100f - currentFuel) * 1.5f;
                    if (currentFuel < StateThresholds.CRITICAL_FUEL)
                        priority *= 2f;
                    break;
                case "Health":
                    priority = (100f - currentHealth) * 1.2f;
                    if (currentHealth < StateThresholds.CRITICAL_HEALTH)
                        priority *= 2f;
                    break;
                case "Ammo":
                    priority = (20f - tank.GetAmmoLevel()) * 0.8f;
                    break;
            }

            return priority / (1f + distance * 0.1f);
        }

        public GameObject GetHighestPriorityResource()
        {
            return tank.consumablesFound
                .OrderByDescending(x => CalculateResourcePriority(x.Key))
                .Select(x => x.Key)
                .FirstOrDefault();
        }
    }
}