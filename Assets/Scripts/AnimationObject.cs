using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Animation", menuName = "ScriptableObjects/AnimationObject", order = 1)]
public class AnimationObject : ScriptableObject
{
    public Texture2D positionTexture;
    public Texture2D normalTexture;
    public float animationLength;
    public uint isLooping;
}
