using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;



public class A_StateMachineFSM : MonoBehaviour

{

    private Dictionary<Type, A_TankStateFSM> states;



    public A_TankStateFSM currentState;

    public A_TankStateFSM CurrentState

    {

        get

        {

            return currentState;

        }

        private set

        {

            currentState = value;

        }

    }



    public void SetStates(Dictionary<Type, A_TankStateFSM> states)

    {

        this.states = states;

    }



    void Update()

    {

        if (CurrentState == null)

        {

            CurrentState = states.Values.First();

        }

        else

        {




            if (CurrentState.Execute() != null && CurrentState.Execute() != CurrentState.GetType())

            {

                SwitchToState(CurrentState.Execute());

            }

        }

    }



    void SwitchToState(Type nextState)

    {

        CurrentState.Exit();

        CurrentState = states[nextState];

        CurrentState.Enter();



    }

}