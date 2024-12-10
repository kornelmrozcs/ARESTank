using System.Linq;
using UnityEngine;
using System.Collections.Generic;

public class ExploreState : TankState
{
    private float explorationTimer = 0f;
    private const float maxExplorationTime = 10f; // Time before generating a new random point
    private Queue<Vector3> waypoints = new Queue<Vector3>();
    private const float waypointRadius = 5f; // Radius for avoidance detection

    public ExploreState(A_Smart tank) : base(tank) { }

    public override void Enter()
    {
        Debug.Log("[ExploreState] Entered.");
        explorationTimer = 0f;
    }

    public override void Execute()
    {
        Debug.Log("[ExploreState] Scanning and exploring...");

        // Prioritize collecting consumables if any are visible
        if (tank.consumablesFound.Count > 0)
        {
            GameObject consumable = tank.consumablesFound.First().Key; // Get the first visible consumable
            if (consumable != null)
            {
                Debug.Log("[ExploreState] Collecting visible consumable: " + consumable.name);
                FollowPathToPoint(consumable.transform.position, 1f);
                return; // Skip exploration and attacking to collect the consumable
            }
        }

        // Check if an enemy base is found
        if (tank.enemyBasesFound.Count > 0)
        {
            GameObject enemyBase = tank.enemyBasesFound.First().Key; // Get the first visible enemy base
            if (enemyBase != null)
            {
                Debug.Log("[ExploreState] Enemy base found. Moving to attack base: " + enemyBase.name);

                // Move towards the base and attack if in range
                if (Vector3.Distance(tank.transform.position, enemyBase.transform.position) < 25f)
                {
                    Debug.Log("[ExploreState] Attacking enemy base: " + enemyBase.name);
                    tank.FireAtPoint(enemyBase);
                }
                else
                {
                    Debug.Log("[ExploreState] Moving closer to enemy base: " + enemyBase.name);
                    FollowPathToPoint(enemyBase.transform.position, 1f);
                }

                return; // Focus on the base and stop other behaviors
            }
        }

        // Continue exploration behavior
        explorationTimer += Time.deltaTime;
        if (explorationTimer >= maxExplorationTime || waypoints.Count == 0)
        {
            Debug.Log("[ExploreState] Generating new random exploration point...");
            GenerateWaypoints(GetRandomPoint(), 5); // Create waypoints to a random point
            explorationTimer = 0f; // Reset exploration timer
        }

        FollowWaypoints(1f);

        // Transition to AttackState if an enemy is found
        if (tank.enemyTanksFound.Count > 0)
        {
            GameObject target = tank.enemyTanksFound.First().Key; // Get the first visible enemy
            if (target != null)
            {
                Debug.Log("[ExploreState] Enemies found. Switching to AttackState targeting: " + target.name);
                tank.ChangeState(new AttackState(tank, target)); // Pass the target GameObject to AttackState
                return;
            }
        }
    }

    public override void Exit()
    {
        Debug.Log("[ExploreState] Exiting.");
    }

    private void FollowPathToPoint(Vector3 targetPosition, float speed)
    {
        MoveWithAvoidance(targetPosition, speed, waypointRadius);
    }

    private void MoveWithAvoidance(Vector3 targetPosition, float speed, float avoidanceRadius)
    {
        Vector3 direction = (targetPosition - tank.transform.position).normalized;
        Vector3 avoidance = Vector3.zero;

        Collider[] obstacles = Physics.OverlapSphere(tank.transform.position, avoidanceRadius);
        foreach (var obstacle in obstacles)
        {
            if (obstacle.CompareTag("Obstacle")) // Use proper tag for obstacles
            {
                Vector3 awayFromObstacle = tank.transform.position - obstacle.transform.position;
                avoidance += awayFromObstacle.normalized / awayFromObstacle.sqrMagnitude;
            }
        }

        Vector3 finalDirection = (direction + avoidance).normalized;

        // Move in the final direction
        tank.transform.position += finalDirection * speed * Time.deltaTime;
        tank.transform.forward = Vector3.Lerp(tank.transform.forward, finalDirection, Time.deltaTime * 5f); // Smooth rotation
    }

    private void GenerateWaypoints(Vector3 targetPosition, int numWaypoints)
    {
        waypoints.Clear();
        Vector3 startPosition = tank.transform.position;

        for (int i = 1; i <= numWaypoints; i++)
        {
            Vector3 waypoint = Vector3.Lerp(startPosition, targetPosition, i / (float)numWaypoints);
            waypoints.Enqueue(waypoint);
        }
    }

    private void FollowWaypoints(float speed)
    {
        if (waypoints.Count > 0)
        {
            Vector3 currentWaypoint = waypoints.Peek();
            MoveWithAvoidance(currentWaypoint, speed, waypointRadius);

            if (Vector3.Distance(tank.transform.position, currentWaypoint) < 1f)
            {
                waypoints.Dequeue(); // Move to the next waypoint
            }
        }
    }

    private Vector3 GetRandomPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * 20f; // 20 units radius
        randomDirection.y = 0f; // Keep on the same plane
        return tank.transform.position + randomDirection;
    }
}