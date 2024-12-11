using System.Linq;
using UnityEngine;

public class AttackState : TankState
{
    private GameObject target;
    private GameObject enemyBase;
    private float fireDuration = 0.5f; // Fire for 1 second before moving
    private float fireTimer = 0.5f;

    public AttackState(A_Smart tank, GameObject target = null) : base(tank)
    {
        this.target = target;
    }

    public override void Enter()
    {
        Debug.Log("[AttackState] Entered.");
        fireTimer = 0f; // Reset the fire timer
    }

    public override void Execute()
    {
        // Check for enemy base
        enemyBase = tank.enemyBasesFound.FirstOrDefault().Key;

        if (enemyBase != null)
        {
            Debug.Log("[AttackState] Enemy base detected: " + enemyBase.name);
            float baseDistance = Vector3.Distance(tank.transform.position, enemyBase.transform.position);

            if (baseDistance < 40f)
            {
                Debug.Log("[AttackState] Firing at enemy base: " + enemyBase.name);
                tank.TurretFaceWorldPoint(enemyBase);
                tank.FireAtPoint(enemyBase);
                return; // Prioritize attacking the base
            }
            else
            {
                Debug.Log("[AttackState] Moving closer to enemy base: " + enemyBase.name);
                tank.FollowPathToPoint(enemyBase, 1f, tank.heuristicMode);
                return; // Prioritize moving toward the base
            }
        }

        // Continue with enemy tank logic
        if (target == null || !tank.enemyTanksFound.ContainsKey(target))
        {
            Debug.Log("[AttackState] No valid target. Switching to ExploreState.");
            tank.ChangeState(new ExploreState(tank));
            return;
        }

        // Lock the turret onto the target
        tank.TurretFaceWorldPoint(target);

        // Calculate the distance to the target
        float distance = Vector3.Distance(tank.transform.position, target.transform.position);

        if (distance < 200f)
        {
            // Fire at the target
            Debug.Log("[AttackState] Firing at target: " + target.name);
            tank.FireAtPoint(target);

            // Increment the fire timer
            fireTimer += Time.deltaTime;

            if (fireTimer >= fireDuration)
            {
                Debug.Log("[AttackState] Fired for 1 second. Switching to MovingPhaseState.");
                tank.ChangeState(new MovingPhaseState(tank, target)); // Transition to MovingPhaseState
                return;
            }
        }
        else
        {
            // Move closer to the target if out of range
            Debug.Log("[AttackState] Target out of range. Moving closer to: " + target.name);
            tank.FollowPathToPoint(target, 1f, tank.heuristicMode);

            // Fire while moving closer
            Debug.Log("[AttackState] Firing at target while moving: " + target.name);
            tank.FireAtPoint(target);
        }

        // Retreat if health is critically low
        if (tank.GetHealthLevel() < 12)
        {
            Debug.Log("[AttackState] Low health detected. Retreating to SearchState.");
            tank.ChangeState(new ExploreState(tank));
            return;
        }
    }

    public override void Exit()
    {
        Debug.Log("[AttackState] Exiting.");
    }
}
