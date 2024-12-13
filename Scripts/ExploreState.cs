using System.Linq;
using UnityEngine;

/// <summary>
/// ExploreState handles efficient map exploration and resource discovery.
/// Key Features:
/// - Intelligent exploration patterns
/// - Resource spawn point tracking
/// - Threat awareness and response
/// - Fuel-efficient movement
/// </summary>
public class ExploreState : TankState
{
    //=============================
    // EXPLORATION PROPERTIES
    //=============================
    private float explorationTimer = 0f;
    private const float MAX_EXPLORATION_TIME = 8f;
    private const float SCAN_INTERVAL = 3f;
    private float scanTimer = 0f;
    private Vector3 lastExplorationPoint;
    private bool hasFoundConsumable = false;
    private int explorationPattern = 0;
    private const int PATTERN_COUNT = 4;

    //=============================
    // EXPLORATION THRESHOLDS
    //=============================
    private const float EXPLORE_RADIUS = 40f;
    private const float MIN_DISTANCE_BETWEEN_POINTS = 15f;
    private const float RESOURCE_COLLECTION_RANGE = 15f;

    //=============================
    // CONSTRUCTOR
    //=============================
    public ExploreState(A_Smart tank) : base(tank)
    {
        lastExplorationPoint = tank.transform.position;
    }

    //=============================
    // STATE METHODS
    //=============================
    public override void Enter()
    {
        Debug.Log("[ExploreState] Entered. Beginning efficient exploration pattern.");
        InitializeExploration();
    }

    public override void Execute()
    {
        // Regular scanning during exploration
        UpdateTimers();

        if (scanTimer >= SCAN_INTERVAL)
        {
            if (tank.PerformScan())
            {
                return;
            }
            scanTimer = 0f;
        }

        // Check for threats and opportunities
        if (HandleThreatsAndOpportunities())
        {
            return;
        }

        // Main exploration logic
        HandleExploration();
    }

    //=============================
    // EXPLORATION METHODS
    //=============================
    private void InitializeExploration()
    {
        explorationTimer = 0f;
        scanTimer = 0f;
        explorationPattern = Random.Range(0, PATTERN_COUNT);
    }

    private void HandleExploration()
    {
        // Generate new exploration point if needed
        if (explorationTimer >= MAX_EXPLORATION_TIME || hasFoundConsumable)
        {
            // Prefer exploring near known resource positions
            if (tank.lastConsumablePositions.Count > 0 && Random.value > 0.3f)
            {
                ExploreKnownAreas();
            }
            else
            {
                GenerateNewExplorationPoint();
            }

            explorationTimer = 0f;
            hasFoundConsumable = false;
        }

        // Move with fuel efficiency in mind
        float speed = CalculateOptimalSpeed();
        tank.FollowPathToRandomPoint(speed, tank.heuristicMode);
    }

    private void ExploreKnownAreas()
    {
        if (tank.lastConsumablePositions.Count == 0) return;

        // Choose a random known position with some variation
        int randomIndex = Random.Range(0, tank.lastConsumablePositions.Count);
        Vector3 knownPosition = tank.lastConsumablePositions[randomIndex];

        // Add some randomness to the position
        Vector3 offset = Random.insideUnitSphere * 10f;
        offset.y = 0;

        lastExplorationPoint = knownPosition + offset;
        tank.FollowPathToPoint(lastExplorationPoint, CalculateOptimalSpeed(), tank.heuristicMode);
    }

    //=============================
    // THREAT HANDLING
    //=============================
    private bool HandleThreatsAndOpportunities()
    {
        // Check critical resources first
        if (CheckCriticalResources())
        {
            return true;
        }

        // Check for enemies
        if (CheckForEnemies())
        {
            return true;
        }

        // Check for valuable resources
        if (CheckForResources())
        {
            return true;
        }

        return false;
    }

    private bool CheckCriticalResources()
    {
        if (tank.GetFuelLevel() < A_Smart.StateThresholds.CRITICAL_FUEL)
        {
            var nearestFuel = tank.GetNearestConsumable("Fuel");
            if (nearestFuel != null)
            {
                Debug.Log("[ExploreState] Found critical fuel resource, collecting.");
                tank.ChangeState(new ResourceCollectionState(tank));
                return true;
            }
            else
            {
                Debug.Log("[ExploreState] Critical fuel, switching to wait state.");
                tank.ChangeState(new WaitState(tank));
                return true;
            }
        }
        return false;
    }

    private bool CheckForEnemies()
    {
        if (tank.enemyTanksFound.Count > 0)
        {
            var enemy = tank.enemyTanksFound.First();
            if (enemy.Key != null)
            {
                float enemyHealth = enemy.Key.GetComponent<DumbTank>().TankCurrentHealth;
                float distance = enemy.Value;

                if (enemyHealth <= A_Smart.StateThresholds.WEAK_ENEMY_HEALTH)
                {
                    Debug.Log("[ExploreState] Found weak enemy, switching to chase.");
                    tank.ChangeState(new ChaseState(tank, enemy.Key));
                    return true;
                }
                else if (tank.GetFuelLevel() > A_Smart.StateThresholds.LOW_FUEL)
                {
                    Debug.Log("[ExploreState] Enemy detected, engaging with dodge pattern.");
                    tank.ChangeState(new DodgingState(tank, enemy.Key));
                    return true;
                }
            }
        }
        return false;
    }

    //=============================
    // UTILITY METHODS
    //=============================
    private void UpdateTimers()
    {
        explorationTimer += Time.deltaTime;
        scanTimer += Time.deltaTime;
    }

    private float CalculateOptimalSpeed()
    {
        // Adjust speed based on fuel level and situation
        if (tank.GetFuelLevel() < A_Smart.StateThresholds.LOW_FUEL)
            return 0.5f;
        if (tank.GetFuelLevel() < A_Smart.StateThresholds.CRITICAL_FUEL)
            return 0.3f;
        return 0.7f;
    }

    private void GenerateNewExplorationPoint()
    {
        // Generate point based on current pattern
        switch (explorationPattern)
        {
            case 0: // Spiral pattern
                GenerateSpiralPoint();
                break;
            case 1: // Grid pattern
                GenerateGridPoint();
                break;
            case 2: // Concentric circles
                GenerateConcentricCirclePoint();
                break;
            default: // Random with constraints
                tank.FollowPathToRandomPoint(CalculateOptimalSpeed(), tank.heuristicMode);
                break;
        }

        explorationPattern = (explorationPattern + 1) % PATTERN_COUNT;
    }

    private void GenerateSpiralPoint()
    {
        float angle = explorationTimer * 0.5f;
        float radius = 10f + explorationTimer * 2f;
        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * radius,
            0,
            Mathf.Sin(angle) * radius
        );
        Vector3 newPoint = tank.transform.position + offset;
        tank.FollowPathToPoint(newPoint, CalculateOptimalSpeed(), tank.heuristicMode);
    }

    private void GenerateGridPoint()
    {
        // Simple grid pattern
        int gridSize = 20;
        Vector3 newPoint = new Vector3(
            Mathf.Round(tank.transform.position.x / gridSize) * gridSize,
            0,
            Mathf.Round(tank.transform.position.z / gridSize) * gridSize
        );
        tank.FollowPathToPoint(newPoint, CalculateOptimalSpeed(), tank.heuristicMode);
    }

    private void GenerateConcentricCirclePoint()
    {
        float radius = 15f + (explorationTimer % 3) * 10f;
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 newPoint = tank.transform.position + new Vector3(
            Mathf.Cos(angle) * radius,
            0,
            Mathf.Sin(angle) * radius
        );
        tank.FollowPathToPoint(newPoint, CalculateOptimalSpeed(), tank.heuristicMode);
    }

    public override void Exit()
    {
        Debug.Log("[ExploreState] Exiting explore state.");
    }
}