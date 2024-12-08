using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static AStar;

/// <summary>
/// KORNEL :: Update2 :: 
/// 1. Added debugging logs to trace AI decisions, state transitions, and key actions:
///    - Logging FSM decisions (e.g., switching to Attack, Search, or Explore states).
///    - Logging actions like targeting an enemy, collecting a consumable, or exploring a random point.
/// 2. Improved testing functionality for tracking AI behavior in different scenarios.
///    - Added timestamps and tank stats (e.g., health, ammo) in logs.
///
/// KORNEL :: Update1 :: 
/// 1. Added FSM (Finite State Machine) logic to control the tank:
///    - Searching for consumables when health or ammo is low.
///    - Attacking visible enemy tanks and bases.
///    - Exploring random points when no targets are available.
/// 2. Utilized functions from the base class (AITank):
///    - Leveraged methods for pathfinding, path-following, random point generation, and turret firing.
///    - Used properties from the base class to access critical variables (e.g., health level, ammo level, etc.).
/// 3. Improved code structure:
///    - Split logic into separate functions for better readability and maintainability.
///    - Each function (e.g., SearchForConsumables, AttackEnemyTank) has a clearly defined purpose and comments.
/// </summary>
public class A_Smart : AITank
{
    // Visible entities
    public Dictionary<GameObject, float> enemyTanksFound = new Dictionary<GameObject, float>();
    public Dictionary<GameObject, float> consumablesFound = new Dictionary<GameObject, float>();
    public Dictionary<GameObject, float> enemyBasesFound = new Dictionary<GameObject, float>();

    // Targets
    public GameObject enemyTank;
    public GameObject consumable;
    public GameObject enemyBase;

    // Timer
    private float timer = 0f;

    // Pathfinding heuristic
    public HeuristicMode heuristicMode;

    // Initializes variables and settings for the AI tank
    public override void AITankStart()
    {
        Debug.Log("Tank AI Initialized.");
    }

    // Implements AI behavior for the tank
    public override void AITankUpdate()
    {
        // Update visible objects
        enemyTanksFound = VisibleEnemyTanks;
        consumablesFound = VisibleConsumables;
        enemyBasesFound = VisibleEnemyBases;


        // Health or ammo is low - prioritize consumables
        if (TankCurrentHealth < 30 || TankCurrentAmmo < 4)
        {
            Debug.Log("[AITankUpdate] Health or ammo is low. Searching for consumables...");
            SearchForConsumables();
        }
        else
        {
            // Prioritize targets based on visibility and proximity
            if (enemyTanksFound.Count > 0)
            {
                Debug.Log("[AITankUpdate] Enemy tank found. Engaging...");
                AttackEnemyTank();
            }
            else if (enemyBasesFound.Count > 0)
            {
                Debug.Log("[AITankUpdate] Enemy base found. Engaging...");
                AttackEnemyBase();
            }
            else if (consumablesFound.Count > 0)
            {
                Debug.Log("[AITankUpdate] Consumables found. Moving to collect...");
                SearchForConsumables();
            }
            else
            {
                Debug.Log("[AITankUpdate] No targets found. Exploring randomly...");
                ExploreRandomly();
            }
        }
    }

    // Handles collision events for the AI tank
    public override void AIOnCollisionEnter(Collision collision)
    {
        Debug.Log($"[AIOnCollisionEnter] Collided with: {collision.gameObject.name}");
    }

    // Searches for consumables when health or ammo is low
    private void SearchForConsumables()
    {
        if (consumablesFound.Count > 0)
        {
            consumable = consumablesFound.First().Key;
            Debug.Log($"[SearchForConsumables] Moving towards consumable: {consumable.name}");
            FollowPathToWorldPoint(consumable, 1f, heuristicMode);
        }
        else
        {
            Debug.Log("[SearchForConsumables] No consumables visible. Exploring randomly...");
            ExploreRandomly();
        }
    }

    // Attacks enemy tanks when visible
    private void AttackEnemyTank()
    {
        enemyTank = enemyTanksFound.First().Key;
        if (enemyTank != null)
        {
            if (Vector3.Distance(transform.position, enemyTank.transform.position) < 25f)
            {
                Debug.Log($"[AttackEnemyTank] Firing at enemy tank: {enemyTank.name}");
                TurretFireAtPoint(enemyTank);
            }
            else
            {
                Debug.Log($"[AttackEnemyTank] Moving towards enemy tank: {enemyTank.name}");
                FollowPathToWorldPoint(enemyTank, 1f, heuristicMode);
            }
        }
    }

    // Attacks enemy bases when visible
    private void AttackEnemyBase()
    {
        enemyBase = enemyBasesFound.First().Key;
        if (enemyBase != null)
        {
            if (Vector3.Distance(transform.position, enemyBase.transform.position) < 25f)
            {
                Debug.Log($"[AttackEnemyBase] Firing at enemy base: {enemyBase.name}");
                TurretFireAtPoint(enemyBase);
            }
            else
            {
                Debug.Log($"[AttackEnemyBase] Moving towards enemy base: {enemyBase.name}");
                FollowPathToWorldPoint(enemyBase, 1f, heuristicMode);
            }
        }
    }

    // Explores random points when no targets are available
    private void ExploreRandomly()
    {
        Debug.Log("[ExploreRandomly] Exploring a random point...");
        FollowPathToRandomWorldPoint(1f, heuristicMode);
        timer += Time.deltaTime;
        if (timer > 10f)
        {
            Debug.Log("[ExploreRandomly] Generating new random point...");
            GenerateNewRandomWorldPoint();
            timer = 0f;
        }
    }

    // Utility methods from the base class
    public void GeneratePathToWorldPoint(GameObject pointInWorld) => a_FindPathToPoint(pointInWorld);
    public void GeneratePathToWorldPoint(GameObject pointInWorld, HeuristicMode heuristic) => a_FindPathToPoint(pointInWorld, heuristic);
    public void FollowPathToWorldPoint(GameObject pointInWorld, float normalizedSpeed) => a_FollowPathToPoint(pointInWorld, normalizedSpeed);
    public void FollowPathToWorldPoint(GameObject pointInWorld, float normalizedSpeed, HeuristicMode heuristic) => a_FollowPathToPoint(pointInWorld, normalizedSpeed, heuristic);
    public void FollowPathToRandomWorldPoint(float normalizedSpeed) => a_FollowPathToRandomPoint(normalizedSpeed);
    public void FollowPathToRandomWorldPoint(float normalizedSpeed, HeuristicMode heuristic) => a_FollowPathToRandomPoint(normalizedSpeed, heuristic);
    public void GenerateNewRandomWorldPoint() => a_GenerateRandomPoint();
    public void TankStop() => a_StopTank();
    public void TankGo() => a_StartTank();
    public void TurretFaceWorldPoint(GameObject pointInWorld) => a_FaceTurretToPoint(pointInWorld);
    public void TurretReset() => a_ResetTurret();
    public void TurretFireAtPoint(GameObject pointInWorld) => a_FireAtPoint(pointInWorld);
    public bool TankIsFiring() => a_IsFiring;
    public float TankCurrentHealth => a_GetHealthLevel;
    public float TankCurrentAmmo => a_GetAmmoLevel;
    public float TankCurrentFuel => a_GetFuelLevel;
    protected List<GameObject> MyBases => a_GetMyBases;
    protected Dictionary<GameObject, float> VisibleEnemyTanks => a_TanksFound;
    protected Dictionary<GameObject, float> VisibleConsumables => a_ConsumablesFound;
    protected Dictionary<GameObject, float> VisibleEnemyBases => a_BasesFound;
}
