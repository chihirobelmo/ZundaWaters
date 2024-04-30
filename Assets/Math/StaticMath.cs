using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public static class StaticMath
{
    public const float KTS_TO_MPS = 1.94384f;
    public const float gravity = 9.8f;
    public const float waterDrag = 5000000f; // placeholder
    public const float airDrag = 1000000f; // placeholder
    public const float circleAreaRatioToSquare = (1.0f * 1.0f) / (0.5f * 0.5f * Mathf.PI);

    public static float TruePitch(this Transform transform)
    {
        return TruePitch(transform.eulerAngles.x);
    }
    public static float TruePitch(this float eulerAngles)
    {
        if (eulerAngles >= 0 && eulerAngles < 90)
        {
            return -eulerAngles;
        }
        if (eulerAngles >= 90 && eulerAngles < 180)
        {
            return - 180 + eulerAngles;
        }
        if (eulerAngles >= 180 && eulerAngles < 270)
        {
            return eulerAngles - 180;
        }
        if (eulerAngles >= 270 && eulerAngles < 360)
        {
            return 360 - eulerAngles;
        }
        return 0;
    }

    /// <summary>
    /// Calculate velocity increase per time.
    /// </summary>
    /// <param name="k"></param>
    /// <param name="m"></param>
    /// <param name="a"></param>
    /// <param name="v"></param>
    /// <returns></returns>
    public static float VtDt(float k, float m, float a, float v) => (-k / m) * (v - m * a / k);

    /// <summary>
    /// Calculate velocity increase per time.
    /// </summary>
    /// <param name="k"></param>
    /// <param name="m"></param>
    /// <param name="a"></param>
    /// <param name="v"></param>
    /// <returns></returns>
    public static Vector3 VtDt(float k, float m, Vector3 a, Vector3 v) => (-k / m) * (v - m * a / k);

    public static Vector3 VRotation(Vector3 omegaRad, float radius) => omegaRad * radius;
    public static float VRotation(float omegaRad, float radius) => omegaRad * radius;

    public static Vector3 OmegaRad(Vector3 v, float radius) => v / (0.0001f + radius);
    public static float OmegaRad(float v, float radius) => v / (0.0001f + radius);

    /// <summary>
    /// Assume submarine body as a ellipsoid. Estimate projected area to each side/top/forward axis of ship in m^2.
    /// </summary>
    /// <param name="length"></param>
    /// <param name="radius"></param>
    /// <returns>Projected Area in meter * meter for each axis</returns>
    public static Vector3 EllipsoidProjectedAreaM2(float length, float radius) => new Vector3(
        length * radius * 2.0f * circleAreaRatioToSquare,
        length * radius * 2.0f * circleAreaRatioToSquare,
        radius * radius * Mathf.PI);

    /// <summary>
    /// Drag Factor
    /// </summary>
    /// <param name="cd">coefficient Drag</param>
    /// <param name="rho">medium density, medium means water or air</param>
    /// <param name="v">velocity m/s</param>
    /// <param name="s">projected area to the velocity</param>
    /// <returns>Drag in N</returns>
    public static float Drag(float cd, float rho, float v, float s) => (1.0f / 2.0f) * cd * rho * v * v * s;

    /// <summary>
    /// Apply Drag Factor
    /// </summary>
    /// <param name="cd">coefficient Drag</param>
    /// <param name="rho">medium density, medium means water or air</param>
    /// <param name="v">velocity m/s</param>
    /// <param name="s">projected area to the velocity</param>
    /// <returns>Drag Applide Acceralation</returns>
    public static float ApplyDrag(this float a, float cd, float rho, float v, float s) => a + Drag(cd, rho, v, s);

    /// <summary>
    /// this is the time since last frame. so can be considered delta time.
    /// </summary>
    public static float dt => Time.deltaTime * Main.timeScale;

    /// <summary>
    /// just to indicate its velocity change per time.
    /// </summary>
    /// <param name="dv">velocity change per time</param>
    /// <returns>return value does not change</returns>
    public static float Dv(this float dv) => dv;

    /// <summary>
    /// just to indicate its angular(deg).
    /// </summary>
    /// <param name="deg">angular(deg)</param>
    /// <returns>return value does not change</returns>
    public static float Deg(this float deg) => deg;

    /// <summary>
    /// just to indicate its angular(deg) change per time.
    /// </summary>
    /// <param name="degt">angular(deg) change per time</param>
    /// <returns>return value does not change</returns>
    public static float Degt(this float degt) => degt;

    /// <summary>
    /// just to indicate its angular(deg) change per time.
    /// </summary>
    /// <param name="degt">angular(deg) change per time</param>
    /// <returns>return value does not change</returns>
    public static Vector3 Degt(this Vector3 degt) => degt;

    /// <summary>
    /// just to indicate its angular(rad) change per time.
    /// </summary>
    /// <param name="radt">angular(rad) change per time</param>
    /// <returns>return value does not change</returns>
    public static float Radt(this float radt) => radt;

    /// <summary>
    /// just to indicate its angular(rad) change per time.
    /// </summary>
    /// <param name="radt">angular(rad) change per time</param>
    /// <returns>return value does not change</returns>
    public static Vector3 Radt(this Vector3 radt) => radt;
}
