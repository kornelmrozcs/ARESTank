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
    private float rotationProgress = 0f;
    private Quaternion targetRotation;
    private float turretRotationSpeed = 30f; // Speed of turret rotation

    public EmergencyState(A_Smart tank) : base(tank)
    {
    }

    public override void Enter()
    {
        Debug.Log("[EmergencyState] Entered.");
    }

    public override void Execute()
    {
        if (tank.GetHealthLevel() < lowHealthThreshold || tank.GetFuelLevel() < lowFuelThreshold || tank.GetAmmoLevel() < lowAmmoThreshold)
        {
            if (tank.GetFuelLevel() <= criticalFuelThreshold)
            {
                Debug.Log("[EmergencyState] Fuel is critically low. Stopping and searching for fuel.");
                tank.FollowPathToPoint(tank.transform.position, 0f, tank.heuristicMode); // Stop the tank
                RotateTurretToFindFuel(); // Rotate the turret to find fuel
                return;
            }

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

            Debug.Log("[EmergencyState] No consumables found. Searching...");
            tank.FollowPathToRandomPoint(1f, tank.heuristicMode);
        }
        else
        {
            Debug.Log("[EmergencyState] All resources sufficient. Transitioning to another state.");
            tank.ChangeState(new ExploreState(tank)); // Or another appropriate state
        }
    }

    private void RotateTurretToFindFuel()
    {
        if (rotationProgress < 1f)
        {
            GameObject rightPoint = new GameObject("RightPoint");
            rightPoint.transform.position = tank.transform.position + tank.transform.right;

            GameObject leftPoint = new GameObject("LeftPoint");
            leftPoint.transform.position = tank.transform.position - tank.transform.right;

            targetRotation = Quaternion.LookRotation(rightPoint.transform.position - tank.turret.position);  // Use the turret's position for rotation
            tank.turret.rotation = Quaternion.Slerp(tank.turret.rotation, targetRotation, turretRotationSpeed * Time.deltaTime);

            rotationProgress += Time.deltaTime * turretRotationSpeed;
            if (rotationProgress >= 1f)
            {
                targetRotation = Quaternion.LookRotation(leftPoint.transform.position - tank.turret.position);
                rotationProgress = 0f;
            }

            GameObject.Destroy(rightPoint);
            GameObject.Destroy(leftPoint);
        }
    }

    public override void Exit()
    {
        Debug.Log("[EmergencyState] Exiting.");
    }
}
