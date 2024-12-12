using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using static AStar;

public class A_Smart : AITank
{
    private TankState currentState;

    // Visible entities
    public Dictionary<GameObject, float> enemyTanksFound => a_TanksFound;
    public Dictionary<GameObject, float> consumablesFound => a_ConsumablesFound;
    public Dictionary<GameObject, float> enemyBasesFound => a_BasesFound;

    // Pathfinding heuristic
    public HeuristicMode heuristicMode;

    private float fireCooldown = 4f; // Time between shots
    private bool isFiring = false;

    public GameObject strafeTarget;

    // Speed adjustments
    private float reducedSpeed = 0.5f; // Reduced speed for faster stopping

    // Property to access ammo level
    // Ammo tracking
    private float previousAmmoLevel = 0f;

    public float TankCurrentAmmo
    {
        get
        {
            return a_GetAmmoLevel; // Call the protected method
        }
    }

    // Firing event handler: listens for when DumbTank fires
    private void HandleFiringMessage(string logString, string stackTrace, LogType type)
    {
        if (logString.Contains("has Fired!"))
        {
            // Decrease ammo by 1 when firing
            if (TankCurrentAmmo > 0)
            {
                previousAmmoLevel = TankCurrentAmmo; // Save previous ammo level for comparison
                Debug.Log("[A_Smart] Ammo reduced. Current Ammo: " + TankCurrentAmmo);
            }
        }
    }

    public override void AITankStart()
    {
        Debug.Log("[A_Smart] Tank AI Initialized.");

        // Create a temporary target GameObject for strafing
        strafeTarget = new GameObject("StrafeTarget");
        strafeTarget.transform.SetParent(transform); // Attach to the tank

        // Start in ExploreState if no enemies are found initially
        GameObject initialTarget = enemyTanksFound.Count > 0 ? enemyTanksFound.First().Key : null;
        {
            ChangeState(new ExploreState(this));
        }
    }


    public override void AITankUpdate()
    {
        if (currentState == null)
        {
            Debug.LogError("[A_Smart] Current state is null. Switching to ExploreState as a fallback.");
            ChangeState(new ExploreState(this));
            return;
        }

        // Transition to EmergencyState if critical levels are detected
        if (GetHealthLevel() < 20f || GetFuelLevel() < 15f || GetAmmoLevel() < 2)
        {
            if (!(currentState is EmergencyState)) // Avoid re-entering EmergencyState
            {
                Debug.Log("[A_Smart] Critical levels detected. Transitioning to EmergencyState.");
                ChangeState(new EmergencyState(this));
                return;
            }
        }

        currentState.Execute();
    }

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

    // You will now directly handle transitioning to ChaseState if an enemy is detected
    public void OnEnemyDetected(GameObject enemy)
    {
        if (enemyTanksFound.Count > 0)
        {
            GameObject detectedEnemy = enemyTanksFound.First().Key;

            // Check if the detected enemy's health is low (using the GetTankHealthLevel method)
            if (detectedEnemy != null && detectedEnemy.GetComponent<DumbTank>().TankCurrentHealth <= 25f)
            {
                // Start chasing directly if health is low
                ChangeState(new ChaseState(this, detectedEnemy));
            }
            else
            {
                // Start chasing (no longer using AttackState)
                ChangeState(new ChaseState(this, detectedEnemy));
            }
        }
    }

    public override void AIOnCollisionEnter(Collision collision)
    {
        Debug.Log($"[A_Smart] Collided with {collision.gameObject.name}.");
    }

    // FireAtPoint now manages the cooldown, firing, and movement
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
        a_FireAtPoint(target); // Fire the shot using AITank's method
        yield return new WaitForSeconds(fireCooldown); // Wait for cooldown period (4 seconds)
        isFiring = false; // Ready to fire again after cooldown
    }

    public void FollowPathToRandomPoint(float normalizedSpeed, HeuristicMode heuristic)
    {
        Debug.Log("[A_Smart] Following path to random point.");
        a_FollowPathToRandomPoint(normalizedSpeed, heuristic);
    }

    public void FollowPathToPoint(GameObject target, float normalizedSpeed, HeuristicMode heuristic)
    {
        if (target != null)
        {
            Debug.Log("[A_Smart] Following path to point: " + target.name);
            a_FollowPathToPoint(target, normalizedSpeed, heuristic);
        }
        else
        {
            Debug.LogWarning("[A_Smart] Cannot follow path. Target is null.");
        }
    }

    public void FollowPathToPoint(Vector3 position, float normalizedSpeed, HeuristicMode heuristic)
    {
        Debug.Log("[A_Smart] Following path to position: " + position);
        GameObject tempTarget = new GameObject("TemporaryTarget");
        tempTarget.transform.position = position;
        a_FollowPathToPoint(tempTarget, normalizedSpeed, heuristic);
        GameObject.Destroy(tempTarget); // Clean up temporary target
    }

    public void TurretFaceWorldPoint(GameObject target)
    {
    }

    public float GetAmmoLevel()
    {
        return a_GetAmmoLevel;
    }

    public float GetHealthLevel()
    {
        return a_GetHealthLevel;
    }

    public float GetFuelLevel()
    {
        return a_GetFuelLevel;
    }

    public bool IsTankFiring()
    {
        return a_IsFiring;  // Accesses a_IsFiring in AITank which is protected
    }

    public float GetTankHealthLevel()
    {
        return GetHealthLevel();  // Use the protected method from AITank to get health
    }

    // Utility to detect bullet threats
    public bool IsBulletThreateningTank(Collider bullet)
    {
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb != null)
        {
            Vector3 bulletVelocity = bulletRb.velocity;
            Vector3 toTank = transform.position - bullet.transform.position;

            // Only dodge if the bullet is actively heading toward the tank
            return Vector3.Dot(bulletVelocity.normalized, toTank.normalized) > 0.9f;
        }
        return false;
    }

    // Adjusted FollowPathToPoint with reduced speed for quick stopping
    public void FollowPathToPointWithReducedSpeed(GameObject target, float normalizedSpeed, HeuristicMode heuristic)
    {
        // Reduce the speed temporarily to help the tank stop quickly
        FollowPathToPoint(target, reducedSpeed, heuristic);
    }
}
