using UnityEngine;

public class MathUtilities
{
    public static float FloatModulo(float dividend, float divisor)
    {
        if (divisor == 0)
        {
            Debug.LogError("Division by zero is not allowed.");
            return 0;
        }

        return dividend - divisor * Mathf.Floor(dividend / divisor);
    }
}

