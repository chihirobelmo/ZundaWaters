using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public static class StaticMath
{
    public const float ESP = 0.0001f; // avoid 0 divide

    public const float KTS_TO_MPS = 1.94384f;
    public const float gravity = 9.8f;
    public const float waterDrag = 5000000f; // placeholder
    public const float airDrag = 1000000f; // placeholder
    public const float circleAreaRatioToSquare = (1.0f * 1.0f) / (0.5f * 0.5f * Mathf.PI);

    //// Raynolds Number ////
    static public Vector3 Reynolds(float rho, Vector3 v, Vector3 l, float mu) => rho * new Vector3(v.x * l.x, v.y * l.y, v.z * l.z) / (ESP + mu);
    static public Vector3 Reynolds(float rho, Vector3 v, float l, float mu) => rho * v * l / (ESP + mu);
    static public float Reynolds(float rho, float v, float l, float mu) => rho * v * l / (ESP + mu);

    public const float waterRho = 997f; /* kg/m^3 */
    public const float airRho = 1.293f; /* kg/m^3 */

    public const float waterMu = 0.001792f; /* Pa.s at 0C */
    public const float airMu = 0.001724f; /* Pa.s at 0C */

    // https://www2t.biglobe.ne.jp/~bono/study/memo/hydro_sheet.htm
    public static readonly Dictionary<int, float> waterMuList = new Dictionary<int, float>{ 
        // celcius vs Pa.s
        {0  , 0.001792f},
        {1  , 0.001731f},
        {2  , 0.001673f},
        {3  , 0.001619f},
        {4  , 0.001567f},
        {5  , 0.001519f},
        {6  , 0.001473f},
        {7  , 0.001428f},
        {8  , 0.001386f},
        {9  , 0.001346f},
        {10 , 0.001308f},
        {11 , 0.001271f},
        {12 , 0.001236f},
        {13 , 0.001203f},
        {14 , 0.001171f},
        {15 , 0.001140f},
        {16 , 0.001111f},
        {17 , 0.001083f},
        {18 , 0.001056f},
        {19 , 0.001030f},
        {20 , 0.001005f},
        {21 , 0.000981f},
        {22 , 0.000958f},
        {23 , 0.000936f},
        {24 , 0.000914f},
        {25 , 0.000894f},
        {26 , 0.000874f},
        {27 , 0.000855f},
        {28 , 0.000836f},
        {29 , 0.000818f},
        {30 , 0.000801f},
        {31 , 0.000784f},
        {32 , 0.000768f},
        {33 , 0.000752f},
        {34 , 0.000737f},
        {35 , 0.000723f},
        {36 , 0.000709f},
        {37 , 0.000685f},
        {38 , 0.000681f},
        {39 , 0.000668f},
        {40 , 0.000656f},
        {41 , 0.000644f},
        {42 , 0.000632f},
        {43 , 0.000621f},
        {44 , 0.000610f},
        {45 , 0.000599f},
        {46 , 0.000588f},
        {47 , 0.000578f},
        {48 , 0.000568f},
        {49 , 0.000559f},
        {50 , 0.000549f},
        {52 , 0.000532f},
        {54 , 0.000515f},
        {56 , 0.000499f},
        {58 , 0.000483f},
        {60 , 0.000469f},
        {62 , 0.000455f},
        {64 , 0.000442f},
        {66 , 0.000429f},
        {68 , 0.000417f},
        {70 , 0.000406f},
        {72 , 0.000395f},
        {74 , 0.000385f},
        {76 , 0.000375f},
        {78 , 0.000366f},
        {80 , 0.000357f},
        {82 , 0.000348f},
        {84 , 0.000339f},
        {86 , 0.000331f},
        {88 , 0.000324f},
        {90 , 0.000317f},
        {92 , 0.000310f},
        {94 , 0.000303f},
        {96 , 0.000296f},
        {98 , 0.000290f},
        {100, 0.000284f},
    };

    public static float TruePitchDeg(this Transform transform)
    {
        return TruePitchDeg(transform.eulerAngles.x);
    }
    public static float TruePitchDeg(this float eulerAngles)
    {
        if (eulerAngles >= 0 && eulerAngles < 90)
        {
            return -eulerAngles;
        }
        if (eulerAngles >= 90 && eulerAngles < 180)
        {
            return -180 + eulerAngles;
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

    static public List<float> FloatRange(float min, float max, int num)
    {
        return Enumerable
            .Range(0, num)
            .Select(x => min + x * (float)((max - min) / (num - 1)))
            .ToList();
    }

    public static float Cos(this float theta) => Mathf.Cos(theta);
    public static float Sin(this float theta) => Mathf.Sin(theta);
    public static float Abs(this float theta) => Mathf.Abs(theta);

    /// <summary>
    /// Calculate velocity increase per time.
    /// </summary>
    /// <param name="k"></param>
    /// <param name="m"></param>
    /// <param name="a"></param>
    /// <param name="v"></param>
    /// <returns></returns>
    public static float VtDt(float k, float m, float a, float v) => (-k / m) * (v - m * a / (ESP + k));

    /// <summary>
    /// Calculate velocity increase per time.
    /// </summary>
    /// <param name="k"></param>
    /// <param name="m"></param>
    /// <param name="a"></param>
    /// <param name="v"></param>
    /// <returns></returns>
    public static Vector3 VtDt(float k, float m, Vector3 a, Vector3 v) => (-k / m) * (v - m * a / (ESP + k));
    public static Vector3 VtDt(Vector3 k, float m, Vector3 a, Vector3 v) {
        return new Vector3(
                       (-k.x / m) * (v.x - m * a.x / (ESP + k.x)),
                       (-k.y / m) * (v.y - m * a.y / (ESP + k.y)),
                       (-k.z / m) * (v.z - m * a.z / (ESP + k.z)) );
    }

    public static Vector3 VRotation(Vector3 omegaRad, float radius) => omegaRad * radius;
    public static float VRotation(float omegaRad, float radius) => omegaRad * radius;

    public static Vector3 OmegaRad(Vector3 v, float radius) => v / (ESP + radius);
    public static float OmegaRad(float v, float radius) => v / (ESP + radius);

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

    /// <summary>
    /// Simple feedback controller, returns delta value to reach target value.
    /// </summary>
    /// <param name="targetvalue">targetvalue</param>
    /// <param name="currentValue">currentValue</param>
    /// <param name="startSpeedDecentRange">value range before target value decrease return value</param>
    /// <param name="magnitude">return value max magnitude</param>
    /// <returns>delta value</returns>
    static public float TargetValueVector(float targetvalue, float currentValue, float startSpeedDecentRange, float magnitude)
        => magnitude
        * (targetvalue - currentValue > 0 ? 1.0f : -1.0f)
        * Mathf.Clamp(Mathf.Abs(targetvalue - currentValue) / startSpeedDecentRange, 0.0f, 1.0f);
}
