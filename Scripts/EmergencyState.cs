using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EmergencyState : TankState
{
    private float lowHealthThreshold = 15f;
    private float lowFuelThreshold = 20f;
    private float lowAmmoThreshold = 0f;
    private float criticalFuelThreshold = 10f;  // New threshold for critically low fuel

    public EmergencyState(A_Smart tank) : base(tank)
    {
    }

    public override void Enter()
    {
        Debug.Log("[EmergencyState] Entered.");
    }

    public override void Execute()
    {
        // Check if health, fuel, or ammo are critically low
        if (tank.GetHealthLevel() < lowHealthThreshold || tank.GetFuelLevel() < lowFuelThreshold || tank.GetAmmoLevel() < lowAmmoThreshold)
        {
            // Log which resource is low
            if (tank.GetHealthLevel() < lowHealthThreshold)
                Debug.Log("[EmergencyState] Health is low.");
            if (tank.GetFuelLevel() < lowFuelThreshold)
                Debug.Log("[EmergencyState] Fuel is low.");
            if (tank.GetAmmoLevel() < lowAmmoThreshold)
                Debug.Log("[EmergencyState] Ammo is low.");

            // Handle critical fuel scenario
            if (tank.GetFuelLevel() <= criticalFuelThreshold)
            {
                Debug.Log("[EmergencyState] Fuel is critically low. Stopping and searching for fuel.");

                // Stop the tank in place
                tank.FollowPathToPoint(tank.transform.position, 0f, tank.heuristicMode);  // Stops the tank

                // Rotate the turret to look for fuel
                RotateTurretToFindFuel();

                // If consumables (fuel) are found, move towards the closest one
                if (tank.consumablesFound.Count > 0)
                {
                    GameObject closestFuel = tank.consumablesFound
                        .Where(c => c.Key != null && c.Key.CompareTag("Fuel") && c.Value > 0f)  // Filter for fuel consumables
                        .OrderBy(c => Vector3.Distance(tank.transform.position, c.Key.transform.position))  // Order by proximity
                        .FirstOrDefault().Key;  // Get the closest fuel

                    if (closestFuel != null)
                    {
                        Debug.Log("[EmergencyState] Moving to collect fuel: " + closestFuel.name);
                        tank.FollowPathToPoint(closestFuel, 1f, tank.heuristicMode);
                        return; // After collecting fuel, return to keep processing
                    }
                }

                // If no fuel is found, continue rotating and searching
                Debug.Log("[EmergencyState] No fuel found. Continuing to search...");
                return;
            }

            // If health, ammo, or fuel are not critically low, continue searching for other consumables
            if (tank.consumablesFound.Count > 0)
            {
                GameObject closestConsumable = tank.consumablesFound
                    .Where(c => c.Key != null && c.Value > 0f)
                    .OrderBy(c => Vector3.Distance(tank.transform.position, c.Key.transform.position))
                    .FirstOrDefault().Key;

                if (closestConsumable != null)
                {
                    Debug.Log("[EmergencyState] Moving to collect consumable: " + closestConsumable.name);
                    tank.FollowPathToPoint(closestConsumable, 1f, tank.heuristicMode);
                    return;
                }
            }

            // If no consumables are found, continue searching
            Debug.Log("[EmergencyState] No consumables found. Searching...");
            tank.FollowPathToRandomPoint(1f, tank.heuristicMode);
        }
        else
        {
            // If all resources are sufficient, exit EmergencyState
            Debug.Log("[EmergencyState] All resources sufficient. Transitioning to another state.");
            tank.ChangeState(new ExploreState(tank)); // Or another appropriate state
        }
    }

    private void RotateTurretToFindFuel()
    {
        // Rotate the turret in place to search for fuel
        float rotationSpeed = 30f;  // Adjust speed of rotation

        // Create temporary GameObjects to represent the points where the turret will face
        GameObject rightPoint = new GameObject("RightPoint");
        rightPoint.transform.position = tank.transform.position + tank.transform.right;  // 90 degrees to the right

        GameObject leftPoint = new GameObject("LeftPoint");
        leftPoint.transform.position = tank.transform.position - tank.transform.right;  // 90 degrees to the left

        // Rotate the turret to face these temporary points
        tank.TurretFaceWorldPoint(rightPoint);  // Face right
        tank.TurretFaceWorldPoint(leftPoint);   // Face left

        // Cleanup the temporary GameObjects after usage
        GameObject.Destroy(rightPoint);
        GameObject.Destroy(leftPoint);
    }

    public override void Exit()
    {
        Debug.Log("[EmergencyState] Exiting.");
    }
}
