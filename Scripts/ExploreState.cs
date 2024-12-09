using UnityEngine;

public class ExploreState : TankState
{
    public ExploreState(A_Smart tank) : base(tank) { }

    public override void Enter()
    {
        Debug.Log("[ExploreState] Entered.");
    }

    public override void Execute()
    {
        Debug.Log("[ExploreState] Exploring randomly.");
        tank.FollowPathToRandomPoint (1f, tank.heuristicMode); // Use AITank's random exploration method

        if (tank.enemyTanksFound.Count > 0)
        {
            Debug.Log("[ExploreState] Enemies found. Switching to AttackState.");
            tank.ChangeState(new AttackState(tank)); // Transition to AttackState
        }
    }

    public override void Exit()
    {
        Debug.Log("[ExploreState] Exiting.");
    }
}
