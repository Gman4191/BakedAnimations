using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

public class AnimationBaker : MonoBehaviour
{
    public ComputeShader infoTexGen;
    public AnimationClip[] clips;

    public struct VertInfo
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector3 tangent;
    }

    private void Reset()
    {
        var animation = GetComponent<Animation>();
        var animator = GetComponent<Animator>();

        if (animation != null)
        {
            clips = new AnimationClip[animation.GetClipCount()];
            var i = 0;
            foreach (AnimationState state in animation)
                clips[i++] = state.clip;
        }
        else if (animator != null)
            clips = animator.runtimeAnimatorController.animationClips;
    }

    // Use this for initialization
    [ContextMenu("bake texture")]
    void Bake()
    {
        var skin = GetComponentInChildren<SkinnedMeshRenderer>();
        var vCount = skin.sharedMesh.vertexCount;
        var texWidth = Mathf.NextPowerOfTwo(vCount);
        var mesh = new Mesh();

        foreach (var clip in clips)
        {
            var frames = Mathf.NextPowerOfTwo((int)(clip.length / 0.05f));
            var dt = clip.length / frames;
            var infoList = new List<VertInfo>();

            var pRt = new RenderTexture(texWidth, frames, 0, RenderTextureFormat.ARGBHalf);
            pRt.name = string.Format("{0}.{1}.posTex", name, clip.name);
            var nRt = new RenderTexture(texWidth, frames, 0, RenderTextureFormat.ARGBHalf);
            nRt.name = string.Format("{0}.{1}.normTex", name, clip.name);
            var tRt = new RenderTexture(texWidth, frames, 0, RenderTextureFormat.ARGBHalf);
            tRt.name = string.Format("{0}.{1}.tanTex", name, clip.name);
            foreach (var rt in new[] { pRt, nRt, tRt })
            {
                rt.enableRandomWrite = true;
                rt.Create();
                RenderTexture.active = rt;
                GL.Clear(true, true, Color.clear);
            }
            for (var i = 0; i < frames; i++)
            {
                
                clip.SampleAnimation(gameObject, dt * i);
                skin.BakeMesh(mesh);

                var verexArry = mesh.vertices;
                var normalArry = mesh.normals;
                var tangentArray = mesh.tangents;
                infoList.AddRange(Enumerable.Range(0, vCount)
                    .Select(idx => new VertInfo()
                    {
                        position = verexArry[idx],
                        normal = normalArry[idx],
                        tangent = tangentArray[idx],
                    })
                );
            }
            var buffer = new ComputeBuffer(infoList.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(VertInfo)));
            buffer.SetData(infoList.ToArray());

            var kernel = infoTexGen.FindKernel("CSMain");
            uint x, y, z;
            infoTexGen.GetKernelThreadGroupSizes(kernel, out x, out y, out z);

            infoTexGen.SetInt("VertCount", vCount);
            infoTexGen.SetBuffer(kernel, "Info", buffer);
            infoTexGen.SetTexture(kernel, "OutPosition", pRt);
            infoTexGen.SetTexture(kernel, "OutNormal", nRt);
            infoTexGen.SetTexture(kernel, "OutTangent", tRt);
            infoTexGen.Dispatch(kernel, vCount / (int)x + 1, frames / (int)y + 1, 1);

            buffer.Release();

#if UNITY_EDITOR
            // Create a folder to contain the baked animation information
            var folderName = "BakedAnimations";
            var folderPath = Path.Combine("Assets", folderName);
            if (!AssetDatabase.IsValidFolder(folderPath))
                AssetDatabase.CreateFolder("Assets", folderName);

            // Create a folder to contain a specific animation's textures
            var subFolder = name;
            var subFolderPath = Path.Combine(folderPath, subFolder);
            if (!AssetDatabase.IsValidFolder(subFolderPath))
                AssetDatabase.CreateFolder(folderPath, subFolder);

            // Create a folder to contain animation objects
            var animFolderName = "Animations";
            var animFolderPath = Path.Combine(folderPath, animFolderName);
            if(!AssetDatabase.IsValidFolder(animFolderPath))
                AssetDatabase.CreateFolder(folderPath, animFolderName);

            // Convert the textures to texture2D 
            var posTex = RenderTextureToTexture2D.Convert(pRt);
            var normTex = RenderTextureToTexture2D.Convert(nRt);
            Graphics.CopyTexture(pRt, posTex);
            Graphics.CopyTexture(nRt, normTex);

            // Create an animation object to store a specific animation's data
            AnimationObject animation = ScriptableObject.CreateInstance<AnimationObject>();
            animation.positionTexture = posTex;
            animation.normalTexture = normTex;
            animation.animationLength = clip.length;
            if (clip.wrapMode == WrapMode.Loop)
            {
                animation.isLooping = 1;
            }

            byte[] bytes = posTex.EncodeToPNG();
            File.WriteAllBytes(subFolderPath + "Image" + ".png", bytes);

            // Save the textures and animation object to their designated folders
            AssetDatabase.CreateAsset(posTex, Path.Combine(subFolderPath, pRt.name + ".asset"));
            AssetDatabase.CreateAsset(normTex, Path.Combine(subFolderPath, nRt.name + ".asset"));
            AssetDatabase.CreateAsset(animation, Path.Combine(animFolderPath, string.Format("{0}_{1}.asset", name, clip.name)));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }
    }
}