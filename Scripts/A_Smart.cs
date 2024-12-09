using System;
using System.Collections;
using System.Linq;
using static AStar;
using UnityEngine;
using System.Collections.Generic;

public class A_Smart : AITank
{
    private TankState currentState;

    // Visible entities
    public Dictionary<GameObject, float> enemyTanksFound => a_TanksFound;
    public Dictionary<GameObject, float> consumablesFound => a_ConsumablesFound;
    public Dictionary<GameObject, float> enemyBasesFound => a_BasesFound;

    // Pathfinding heuristic
    public HeuristicMode heuristicMode;

    public override void AITankStart()
    {
        Debug.Log("[A_Smart] Tank AI Initialized.");
        ChangeState(new ExploreState(this)); // Start with ExploreState
    }

    public override void AITankUpdate()
    {
        // Check if critical needs (low health/ammo) exist
        if (a_GetHealthLevel < 4 || a_GetAmmoLevel < 1)
        {
            Debug.Log("[A_Smart] Critical needs detected! Switching to CollectState.");
            ChangeState(new CollectState(this));
            return;
        }

        // If enemies are found, enter AttackState
        if (enemyTanksFound.Count > 0)
        {
            GameObject target = enemyTanksFound.First().Key; // Get the first visible enemy
            Debug.Log("[A_Smart] Enemies detected! Switching to AttackState.");
            ChangeState(new AttackState(this, target)); // Pass the target GameObject to AttackState
            return;
        }

        // Default to exploring if no immediate needs or threats
        if (!(currentState is ExploreState))
        {
            Debug.Log("[A_Smart] No critical needs or threats. Exploring...");
            ChangeState(new ExploreState(this));
        }
    }

    public void ChangeState(TankState newState)
    {
        Debug.Log($"[A_Smart] Changing state from {currentState?.GetType().Name} to {newState.GetType().Name}.");
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }

    public override void AIOnCollisionEnter(Collision collision)
    {
        Debug.Log($"[A_Smart] Collided with {collision.gameObject.name}.");
    }

    // Public wrapper for a_FollowPathToRandomPoint
    public void FollowPathToRandomPoint(float normalizedSpeed, HeuristicMode heuristic)
    {
        a_FollowPathToRandomPoint(normalizedSpeed, heuristic);
    }

    public void FireAtPoint(GameObject target)
    {
        a_FireAtPoint(target);
    }

    public void FollowPathToPoint(GameObject target, float normalizedSpeed, HeuristicMode heuristic)
    {
        a_FollowPathToPoint(target, normalizedSpeed, heuristic);
    }

    public void TurretFireAtPoint(GameObject target)
    {
        a_FireAtPoint(target); // Calls the protected method in the base class
    }


}




