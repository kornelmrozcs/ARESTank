﻿using System;
using System.Collections;
using System.Linq;
using static AStar;
using UnityEngine;
using System.Collections.Generic;

public class A_SmartFSM : AITank
{

    // Visible entities
    public Dictionary<GameObject, float> enemyTanksFound => a_TanksFound;
    public Dictionary<GameObject, float> consumablesFound => a_ConsumablesFound;
    public Dictionary<GameObject, float> enemyBasesFound => a_BasesFound;

    // Pathfinding heuristic
    public HeuristicMode heuristicMode;

    ///public GameObject strafeTarget;

    public override void AITankStart()
    {
        InitializeStateMachine();

        Debug.Log("[A_Smart] Tank AI Initialized.");

        // Create a temporary target GameObject for strafing
        //strafeTarget = new GameObject("StrafeTarget");
        //strafeTarget.transform.SetParent(transform); // Attach to the tank

        // Start in AttackState with no target initially
        GameObject initialTarget = null;

        if (enemyTanksFound.Count > 0)
        {
            initialTarget = enemyTanksFound.First().Key; // Attempt to get an initial target
        }

    }

    private void InitializeStateMachine()
    {
        Dictionary<Type, A_TankStateFSM> states = new Dictionary<Type, A_TankStateFSM>();
        
        
        states.Add(typeof(A_SearchStateFSM),new A_SearchStateFSM(this));
        states.Add(typeof(A_ChaseStateFSM), new A_ChaseStateFSM(this));
        states.Add(typeof(A_AttackStateFSM), new A_AttackStateFSM(this));
        states.Add(typeof(A_AmbushStateFSM), new A_AmbushStateFSM(this));
        //states.Add(typeof(A_RetreatStateFSM), new A_RetreatStateFSM(this));
        //states.Add(typeof(A_WaitStateFSM), new A_WaitStateFSM(this));*/

        GetComponent<A_StateMachineFSM>().SetStates(states);
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

    public void TurretFaceWorldPoint(GameObject target)
    {
        if (target != null)
        {
            a_FaceTurretToPoint(target); // Calls the protected method from AITank
        }
        else
        {
            Debug.LogWarning("[A_Smart] Target is null. Cannot rotate turret.");
        }
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

    public void LockVisionOnTarget(GameObject target)
    {
        if (target != null)
        {
            // Rotate the tank's turret or vision cone to face the target
            a_FaceTurretToPoint(target); // Ensure the turret is always aiming at the enemy
            Debug.Log("[A_Smart] Locking vision on target: " + target.name);
        }
        else
        {
            Debug.LogWarning("[A_Smart] Cannot lock vision. Target is null.");
        }
    }

    internal void FollowPathToPoint(Vector3 movePosition, float v, HeuristicMode heuristicMode)
    {
        throw new NotImplementedException();
    }

    public override void AITankUpdate()
    {
        
    }
}

