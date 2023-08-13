using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationStateMachine : MonoBehaviour
{
    [SerializeField]
    private AnimationObject[] animationObjects;
    private State[] animationStates;
    private Texture2D stackedPositionTexture;
    private Texture2D stackedNormalTexture;
    private float[] yOffsets;

    public void Start()
    {
        stackedPositionTexture = GenerateStackedTexture.generateStackedPositionTexture(animationObjects);
        stackedNormalTexture   = GenerateStackedTexture.generateStackedNormalTexture(animationObjects);
        yOffsets               = new float[animationObjects.Length];

        for(int i = 0; i < yOffsets.Length; i++)
        {
            for(int j = 0; j < i; j++)
            {
                yOffsets[i] += animationObjects[j].animationLength;
            }
        }
    }

    public void UpdateState(int currentState, ref unitInfo unitInformation)
    {
        if(animationStates[currentState].hasNextState())
        {
            animationStates[currentState] = animationStates[currentState].getNextState();
            Play(animationObjects[animationStates[currentState].AnimationIndex], unitInformation);
        }

    }

    // Play an animation from the state machine's list of animations
    public bool Play(int animationIndex, ref unitInfo unitInformation)
    {
        unitInformation.currentAnimation = yOffsets[animationIndex];
        unitInformation.animationLength = animationObjects[animationIndex].animationLength;
        unitInformation.time = 0.0f;
        unitInformation.isLooping = animationObjects[animationIndex].isLooping;

        return true;
    }
}

public delegate bool TransitionFunction(params object[] parameters);
public class State
{
    private int animationIndex;
    private Dictionary<State, TransitionFunction> transitions;

    public int AnimationIndex
    {
        get{return animationIndex;} 
        set{animationIndex = value;}
    }

    public State(int _animationIndex)
    {
        animationIndex = _animationIndex;
    }

    public void addTransition(State newState, TransitionFunction transitionFunction)
    {
        transitions.Add(newState, transitionFunction);
    }

    public State getNextState()
    {
        State nextState = this;

        // If there is a valid transition from the current state, take the first one.
        foreach(var transition in transitions)
        {
            if(transition.Value())
            {
                nextState = transition.Key;
                break;
            }
        }

        return nextState;
    }

    // True if a valid transition exists
    public bool hasNextState()
    {
        foreach(var transition in transitions)
            if(transition.Value())
                return true;

        return false;
    }
}