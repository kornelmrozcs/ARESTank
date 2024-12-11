using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TankState
{
    protected A_Smart tank; // Reference to the main tank class

    public TankState(A_Smart tank)
    {
        this.tank = tank;
    }

    public abstract void Enter(); // Called when the state is entered
    public abstract void Execute(); // Called every frame
    //public override void Wait();
    public abstract void Exit(); // Called when the state is exited
}
