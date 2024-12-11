using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class A_TankStateFSM
{
    protected A_SmartFSM tank; // Reference to the main tank class

    public A_TankStateFSM(A_SmartFSM tank)
    {
        this.tank = tank;
    }

    public abstract Type Enter(); // Called when the state is entered
    public abstract Type Execute(); // Called every frame
    //public override void Wait();
    public abstract Type Exit(); // Called when the state is exited
}
