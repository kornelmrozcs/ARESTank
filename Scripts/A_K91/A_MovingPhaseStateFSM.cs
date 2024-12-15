using System;
using UnityEngine;

public class A_MovingPhaseStateFSM : A_TankStateFSM
{
    private GameObject target;
    private float moveDuration = 3f; // Total time for moving around
    private float moveTimer = 0f; // Timer for movement phase
    private float circularRadius = 20f; // Radius for circular motion
    private float circularSpeed = 2f; // Speed of circular motion
    private float angle = 0f; // Angle for circular motion
    private Vector3[] circularPathPoints; // Pre-calculated path points
    private int currentPointIndex = 0; // Current path point index

    public A_MovingPhaseStateFSM(A_SmartFSM tank, GameObject target = null) : base(tank)
    {
        this.target = target;
    }

    public override Type Enter()
    {
        Debug.Log("[MovingPhaseState] Entered.");
        moveTimer = 0f; // Reset move timer
        angle = 0f; // Reset circular angle
        currentPointIndex = 0; // Start from the first point

        // Pre-calculate the circular path
        circularPathPoints = CalculateCircularPath(target.transform.position, circularRadius, 12); // Adjust number of points for smoother path
        return null;
    }

    public override Type Execute()
    {
        if (target == null || !tank.enemyTanksFound.ContainsKey(target))
        {
            Debug.Log("[MovingPhaseState] Lost target. Switching to ExploreState.");

            return typeof(A_SearchStateFSM);
        }

        // Lock the turret onto the target
        tank.TurretFaceWorldPoint(target);

        // Move to the next circular path point
        Vector3 currentPoint = circularPathPoints[currentPointIndex];
        Debug.Log("[MovingPhaseState] Moving to path point: " + currentPoint);
        tank.FollowPathToPoint(new GameObject { transform = { position = currentPoint } }, 1f, tank.heuristicMode);

        // Check if close to the current point
        if (Vector3.Distance(tank.transform.position, currentPoint) < 2f) // Adjust tolerance as needed
        {
            Debug.Log("[MovingPhaseState] Reached path point. Moving to the next point.");
            currentPointIndex = (currentPointIndex + 1) % circularPathPoints.Length; // Loop through points
        }

        // Increment the move timer
        moveTimer += Time.deltaTime;

        // If the moving phase is complete, switch back to AttackState
        if (moveTimer >= moveDuration)
        {
            Debug.Log("[MovingPhaseState] Completed movement phase. Switching back to AttackState.");
            return typeof(A_AttackStateFSM);
        }
        return null;
    }

    public override Type Exit()
    {
        Debug.Log("[MovingPhaseState] Exiting.");
        return null;
    }

    private Vector3[] CalculateCircularPath(Vector3 center, float radius, int numPoints)
    {
        Vector3[] points = new Vector3[numPoints];
        float angleStep = 360f / numPoints;

        for (int i = 0; i < numPoints; i++)
        {
            float currentAngle = i * angleStep * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(currentAngle), 0, Mathf.Sin(currentAngle));
            points[i] = center + direction * radius;
        }

        return points;
    }
}
