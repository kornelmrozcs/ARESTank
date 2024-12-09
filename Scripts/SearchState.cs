using UnityEngine;

public class SearchState : TankState
{
    private float searchTimer = 0f;
    private const float searchDuration = 2f;

    public SearchState(A_Smart tank) : base(tank) { }

    public override void Enter()
    {
        Debug.Log("[SearchState] Entered.");
        searchTimer = 0f;
    }

    public override void Execute()
    {
        searchTimer += Time.deltaTime;

        if (searchTimer >= searchDuration)
        {
            Debug.Log("[SearchState] Search complete. Switching to AttackState.");
            tank.ChangeState(new AttackState(tank));
        }
    }

    public override void Exit()
    {
        Debug.Log("[SearchState] Exiting.");
    }
}
