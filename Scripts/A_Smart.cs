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
        currentState?.Execute();
    }

    public void ChangeState(TankState newState)
    {
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

}




/// <summary>
/// KORNEL :: Update2 :: 
/// 1. Added debugging logs to trace AI decisions, state transitions, and key actions:
///    - Logging FSM decisions (e.g., switching to Attack, Search, or Explore states).
///    - Logging actions like targeting an enemy, collecting a consumable, or exploring a random point.
/// 2. Improved testing functionality for tracking AI behavior in different scenarios.
///    - Added timestamps and tank stats (e.g., health, ammo) in logs.
/// 
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
/// 

// Utility methods from the base class