using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static AStar;

/// <summary>
/// KORNEL :: Update1 :: 
///     1.Added FSM(Finite State Machine) logic to control the tank:
///         -Searching for consumables when health or ammo is low.
///         -Attacking visible enemy tanks and bases.
///         -Exploring random points when no targets are available.
///     2. Utilized functions from the base class (AITank):
///         -Leveraged methods for pathfinding, path-following, random point generation, and turret firing.
///         -Used properties from the base class to access critical variables (e.g., health level, ammo level, etc.).
///     3. Improved code structure:
///         -Split logic into separate functions for better readability and maintainability.
///          -Each function(e.g., SearchForConsumables, AttackEnemyTank) has a clearly defined purpose and comments.
///
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

    /// <summary>
    /// WARNING: Use <c>AITankStart()</c> instead of <c>Start()</c>.
    /// Initializes variables and settings for the AI tank.
    /// </summary>
    public override void AITankStart()
    {
        // Initialization logic if required
    }

    /// <summary>
    /// WARNING: Use <c>AITankUpdate()</c> instead of <c>Update()</c>.
    /// Implements AI behavior for the tank.
    /// </summary>
    public override void AITankUpdate()
    {
        // Update visible objects
        enemyTanksFound = VisibleEnemyTanks;
        consumablesFound = VisibleConsumables;
        enemyBasesFound = VisibleEnemyBases;

        // Health or ammo is low - prioritize consumables
        if (TankCurrentHealth < 30 || TankCurrentAmmo < 4)
        {
            SearchForConsumables();
        }
        else
        {
            // Prioritize targets based on visibility and proximity
            if (enemyTanksFound.Count > 0)
            {
                AttackEnemyTank();
            }
            else if (enemyBasesFound.Count > 0)
            {
                AttackEnemyBase();
            }
            else if (consumablesFound.Count > 0)
            {
                SearchForConsumables();
            }
            else
            {
                // Random exploration
                ExploreRandomly();
            }
        }
    }

    /// <summary>
    /// WARNING: Use <c>AIOnCollisionEnter()</c> instead of <c>OnCollisionEnter()</c>.
    /// Handles collision events for the AI tank.
    /// </summary>
    /// <param name="collision">Collision data.</param>
    public override void AIOnCollisionEnter(Collision collision)
    {
        // Handle collisions if necessary
    }

    /// <summary>
    /// Handles behavior when searching for consumables.
    /// </summary>
    private void SearchForConsumables()
    {
        if (consumablesFound.Count > 0)
        {
            consumable = consumablesFound.First().Key;
            FollowPathToWorldPoint(consumable, 1f, heuristicMode);
        }
        else
        {
            ExploreRandomly();
        }
    }

    /// <summary>
    /// Handles behavior when attacking enemy tanks.
    /// </summary>
    private void AttackEnemyTank()
    {
        enemyTank = enemyTanksFound.First().Key;
        if (enemyTank != null)
        {
            if (Vector3.Distance(transform.position, enemyTank.transform.position) < 25f)
            {
                TurretFireAtPoint(enemyTank);
            }
            else
            {
                FollowPathToWorldPoint(enemyTank, 1f, heuristicMode);
            }
        }
    }

    /// <summary>
    /// Handles behavior when attacking enemy bases.
    /// </summary>
    private void AttackEnemyBase()
    {
        enemyBase = enemyBasesFound.First().Key;
        if (enemyBase != null)
        {
            if (Vector3.Distance(transform.position, enemyBase.transform.position) < 25f)
            {
                TurretFireAtPoint(enemyBase);
            }
            else
            {
                FollowPathToWorldPoint(enemyBase, 1f, heuristicMode);
            }
        }
    }

    /// <summary>
    /// Handles random exploration when no targets are visible.
    /// </summary>
    private void ExploreRandomly()
    {
        FollowPathToRandomWorldPoint(1f, heuristicMode);
        timer += Time.deltaTime;
        if (timer > 10f)
        {
            GenerateNewRandomWorldPoint();
            timer = 0f;
        }
    }



    /*******************************************************************************************************       
    Below are a set of functions you can use. These reference the functions in the AITank Abstract class
    and are protected. These are simply to make access easier if you an not familiar with inheritance and modifiers
    when dealing with reference to this class. This does mean you will have two similar function names, one in this
    class and one from the AIClass. 
    *******************************************************************************************************/


    /// <summary>
    /// Generate a path from current position to pointInWorld (GameObject). If no heuristic mode is set, default is Euclidean,
    /// </summary>
    /// <param name="pointInWorld">This is a gameobject that is in the scene.</param>
    public void GeneratePathToWorldPoint(GameObject pointInWorld)
    {
        a_FindPathToPoint(pointInWorld);
    }

    /// <summary>
    /// Generate a path from current position to pointInWorld (GameObject)
    /// </summary>
    /// <param name="pointInWorld">This is a gameobject that is in the scene.</param>
    /// <param name="heuristic">Chosen heuristic for path finding</param>
    public void GeneratePathToWorldPoint(GameObject pointInWorld, HeuristicMode heuristic)
    {
        a_FindPathToPoint(pointInWorld, heuristic);
    }

    /// <summary>
    ///Generate and Follow path to pointInWorld (GameObject) at normalizedSpeed (0-1). If no heuristic mode is set, default is Euclidean,
    /// </summary>
    /// <param name="pointInWorld">This is a gameobject that is in the scene.</param>
    /// <param name="normalizedSpeed">This is speed the tank should go at. Normalised speed between 0f,1f.</param>
    public void FollowPathToWorldPoint(GameObject pointInWorld, float normalizedSpeed)
    {
        a_FollowPathToPoint(pointInWorld, normalizedSpeed);
    }

    /// <summary>
    ///Generate and Follow path to pointInWorld (GameObject) at normalizedSpeed (0-1). 
    /// </summary>
    /// <param name="pointInWorld">This is a gameobject that is in the scene.</param>
    /// <param name="normalizedSpeed">This is speed the tank should go at. Normalised speed between 0f,1f.</param>
    /// <param name="heuristic">Chosen heuristic for path finding</param>
    public void FollowPathToWorldPoint(GameObject pointInWorld, float normalizedSpeed, HeuristicMode heuristic)
    {
        a_FollowPathToPoint(pointInWorld, normalizedSpeed, heuristic);
    }

    /// <summary>
    ///Generate and Follow path to a randome point at normalizedSpeed (0-1). Go to a randon spot in the playfield. 
    ///If no heuristic mode is set, default is Euclidean,
    /// </summary>
    /// <param name="normalizedSpeed">This is speed the tank should go at. Normalised speed between 0f,1f.</param>
    public void FollowPathToRandomWorldPoint(float normalizedSpeed)
    {
        a_FollowPathToRandomPoint(normalizedSpeed);
    }

    /// <summary>
    ///Generate and Follow path to a randome point at normalizedSpeed (0-1). Go to a randon spot in the playfield
    /// </summary>
    /// <param name="normalizedSpeed">This is speed the tank should go at. Normalised speed between 0f,1f.</param>
    /// <param name="heuristic">Chosen heuristic for path finding</param>
    public void FollowPathToRandomWorldPoint(float normalizedSpeed, HeuristicMode heuristic)
    {
        a_FollowPathToRandomPoint(normalizedSpeed, heuristic);
    }

    /// <summary>
    ///Generate new random point
    /// </summary>
    public void GenerateNewRandomWorldPoint()
    {
        a_GenerateRandomPoint();
    }

    /// <summary>
    /// Stop Tank at current position.
    /// </summary>
    public void TankStop()
    {
        a_StopTank();
    }

    /// <summary>
    /// Continue Tank movement at last know speed and pointInWorld path.
    /// </summary>
    public void TankGo()
    {
        a_StartTank();
    }

    /// <summary>
    /// Face turret to pointInWorld (GameObject)
    /// </summary>
    /// <param name="pointInWorld">This is a gameobject that is in the scene.</param>
    public void TurretFaceWorldPoint(GameObject pointInWorld)
    {
        a_FaceTurretToPoint(pointInWorld);
    }

    /// <summary>
    /// Reset turret to forward facing position
    /// </summary>
    public void TurretReset()
    {
        a_ResetTurret();
    }

    /// <summary>
    /// Face turret to pointInWorld (GameObject) and fire (has delay).
    /// </summary>
    /// <param name="pointInWorld">This is a gameobject that is in the scene.</param>
    public void TurretFireAtPoint(GameObject pointInWorld)
    {
        a_FireAtPoint(pointInWorld);
    }

    /// <summary>
    /// Returns true if the tank is currently in the process of firing.
    /// </summary>
    public bool TankIsFiring()
    {
        return a_IsFiring;
    }

    /// <summary>
    /// Returns float value of remaining health.
    /// </summary>
    /// <returns>Current health.</returns>
    public float TankCurrentHealth
    {
        get
        {
            return a_GetHealthLevel;
        }
    }

    /// <summary>
    /// Returns float value of remaining ammo.
    /// </summary>
    /// <returns>Current ammo.</returns>
    public float TankCurrentAmmo
    {
        get
        {
            return a_GetAmmoLevel;
        }
    }

    /// <summary>
    /// Returns float value of remaining fuel.
    /// </summary>
    /// <returns>Current fuel level.</returns>
    public float TankCurrentFuel
    {
        get
        {
            return a_GetFuelLevel;
        }
    }

    /// <summary>
    /// Returns list of friendly bases. Does not include bases which have been destroyed.
    /// </summary>
    /// <returns>List of your own bases which are. </returns>
    protected List<GameObject> MyBases
    {
        get
        {
            return a_GetMyBases;
        }
    }

    /// <summary>
    /// Returns Dictionary(GameObject target, float distance) of visible targets (tanks in TankMain LayerMask).
    /// </summary>
    /// <returns>All enemy tanks currently visible.</returns>
    protected Dictionary<GameObject, float> VisibleEnemyTanks
    {
        get
        {
            return a_TanksFound;
        }
    }

    /// <summary>
    /// Returns Dictionary(GameObject consumable, float distance) of visible consumables (consumables in Consumable LayerMask).
    /// </summary>
    /// <returns>All consumables currently visible.</returns>
    protected Dictionary<GameObject, float> VisibleConsumables
    {
        get
        {
            return a_ConsumablesFound;
        }
    }

    /// <summary>
    /// Returns Dictionary(GameObject base, float distance) of visible enemy bases (bases in Base LayerMask).
    /// </summary>
    /// <returns>All enemy bases currently visible.</returns>
    protected Dictionary<GameObject, float> VisibleEnemyBases
    {
        get
        {
            return a_BasesFound;
        }
    }

}
