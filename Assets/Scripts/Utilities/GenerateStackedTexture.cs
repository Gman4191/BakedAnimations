using UnityEngine;

public class GenerateStackedTexture
{
    public static Texture2D GenerateStackedPositionTexture(AnimationObject[] animations)
    {
        Texture2D stackedTexture;
        int height  = 0;
        int yOffset = 0;

        // Get the height of the stacked texture
        for(int i = 0; i < animations.Length; i++)
        {
            height += animations[i].positionTexture.height;
        }
        
        // Initialize the stacked texture
        stackedTexture = new Texture2D(animations[0].positionTexture.width, height, TextureFormat.RGBAHalf, false);

        // Copy the pixels of each texture to the stacked texture
        for(int i = 0; i < animations.Length; i++)
        {
            Color[] colors = animations[i].positionTexture.GetPixels();
            stackedTexture.SetPixels(0, yOffset, animations[i].positionTexture.width, animations[i].positionTexture.height, colors);
            yOffset += animations[i].positionTexture.height;
        }

        stackedTexture.Apply();

        return stackedTexture;
    }

    public static Texture2D GenerateStackedNormalTexture(AnimationObject[] animations)
    {
        Texture2D stackedTexture;
        int height  = 0;
        int yOffset = 0;

        // Get the height of the stacked texture
        for(int i = 0; i < animations.Length; i++)
        {
            height += animations[i].normalTexture.height;
        }
        
        // Initialize the stacked texture
        stackedTexture = new Texture2D(animations[0].normalTexture.width, height, TextureFormat.RGBAHalf, false);

        // Copy the pixels of each texture to the stacked texture
        for(int i = 0; i < animations.Length; i++)
        {
            Color[] colors = animations[i].normalTexture.GetPixels();
            stackedTexture.SetPixels(0, yOffset, animations[i].normalTexture.width, animations[i].normalTexture.height, colors);
            yOffset += animations[i].normalTexture.height;
        }

        stackedTexture.Apply();

        return stackedTexture;
    }
}
