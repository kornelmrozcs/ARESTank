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
    private float reducedSpeed = 0.3f; // Reduced speed for faster stopping

    public override void AITankStart()
    {
        Debug.Log("[A_Smart] Tank AI Initialized.");

        // Create a temporary target GameObject for strafing
        strafeTarget = new GameObject("StrafeTarget");
        strafeTarget.transform.SetParent(transform); // Attach to the tank

        // Start in ExploreState if no enemies are found initially
        GameObject initialTarget = enemyTanksFound.Count > 0 ? enemyTanksFound.First().Key : null;
        if (initialTarget != null)
        {
            ChangeState(new AttackState(this, initialTarget));
        }
        else
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

    // You will also need to handle the transition into the ChaseState when the DumbTank's HP is low
    public void OnEnemyDetected(GameObject enemy)
    {
        if (enemyTanksFound.Count > 0)
        {
            GameObject detectedEnemy = enemyTanksFound.First().Key;

            // Check if the detected enemy's health is 25 or less (using the GetTankHealthLevel method)
            if (detectedEnemy != null && detectedEnemy.GetComponent<DumbTank>().TankCurrentHealth <= 25f)
            {
                // Start the chase + sniping behavior if health is low
                ChangeState(new ChaseState(this, detectedEnemy));
            }
            else
            {
                // Proceed with standard behavior if health is higher
                ChangeState(new AttackState(this, detectedEnemy));
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
