using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LosAngelsClassFlightII : MonoBehaviour
{
    // Use this for initialization
    void Start()
    {
        velocityMPS = transform.forward * 5.0f * KTS_TO_MPS;
        lastPosition = transform.position;
    }

    public static float VtPerDt(float k, float m, float a, float v)
    {
        return (-k / m) * (v * v - m * a / k);
    }

    /// <summary>
    /// The matrix of a proper rotation "r" by angle θ around the axis "n"
    /// 
    /// usage : RotationAroundAxis(n).(theta).(r);
    /// </summary>
    /// <param name="n">target vector</param>
    /// <returns>effect for vector "r"</returns>
    public static Func<float, Func<Vector3, Vector3>> RotationAroundAxis(Vector3 n) {
        return (float theta) => {
            return (Vector3 r) => {
                return r * (float)Math.Cos(theta) + n * Vector3.Dot(n, r) * (1 - (float)Math.Cos(theta)) + Vector3.Cross(n, r) * (float)Math.Sin(theta);
            };
        };
    }

    const float KTS_TO_MPS = 1.94384f;
    float gravity = 9.8f;
    float ballast = -9.8f * 2.0f;
    float thrust = 5.0f; // m/s2
    float fallspeed = 0.0f;
    readonly float mass = 7000/*ton displacement*/ * 1000/*kg*/ * 2.0f/*assume actual mass has twice displacement*/;
    readonly float waterDrag = 50000f;
    Vector3 velocityMPS;
    Vector3 gravityVelocityMPS;
    Vector3 gravityAngularRad;
    Vector3 gravityMoment;
    Vector3 angularSpeedRad;
    Vector3 lastPosition;
    Vector3 vector;

    void updateInfo()
    {
        vector = lastPosition - transform.position;
        gravityMoment = new Vector3(0, 0, 0);
        lastPosition = transform.GetChild(0).position;
    }

    void UpdateVelocity()
    {
        if ((transform.position + transform.forward * -50.0f).y <= 0)
        {
            velocityMPS += transform.forward * VtPerDt(waterDrag, mass, thrust, velocityMPS.magnitude) * Time.deltaTime;
        }
        velocityMPS += (transform.forward * velocityMPS.magnitude - velocityMPS) * Time.deltaTime;
    }

    void UpdatePosition()
    {
        transform.position += (velocityMPS + gravityVelocityMPS) * Time.deltaTime;
    }

    void CalcGravityAndFloat() {

       // gravity for each cell

       IEnumerable < Vector3 > eachCellOfShip =
           from z in Enumerable.Range(-55,55)
           from y in Enumerable.Range(-2, 10)
           select transform.position + 
           transform.forward * z + transform.up * y;

        int cellCount = eachCellOfShip.Count();
        gravityAngularRad.x = 0;

        float kpm = 50000/mass;

        eachCellOfShip.ToList().ForEach(p =>
        {
            float offset = (p.z - transform.position.z);
            if (p.y > 0)
            { // in air
                gravityVelocityMPS.y += VtPerDt(waterDrag, mass, -gravity, gravityVelocityMPS.y) * Time.deltaTime * (1.0f / cellCount); // gravity
                gravityAngularRad.x += gravity * offset * Time.deltaTime * (1.0f / cellCount);
            }
            if (p.y <= 0)
            { // under water
                gravityVelocityMPS.y += VtPerDt(waterDrag, mass, - gravity - ballast, gravityVelocityMPS.y) * Time.deltaTime * (1.0f / cellCount); // gravity
                gravityAngularRad.x += (gravity + ballast) * offset * Time.deltaTime * (1.0f / cellCount);
            }
        });
    }

    void UpdateOrientaion()
    {
        // yaw
        transform.rotation *= Quaternion.Euler((angularSpeedRad + gravityAngularRad) * Time.deltaTime);
        // roll fixed 0
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, 0);
        //transform.GetChild(0).rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, -angularSpeed_rad.y);
    }

    void ChangeAndLimitThrust(float add)
    {
        thrust += add;
        thrust = thrust > 5.0f ? 5.0f : thrust < -1.0f ? -1.0f : thrust;
    }

    void ChangeAndLimitYawAngularSpeed(float add)
    {
        angularSpeedRad.y += add;
        angularSpeedRad.y = angularSpeedRad.y > 30.0f ? 30.0f : angularSpeedRad.y < -30.0f ? -30.0f : angularSpeedRad.y;
    }

    void ChangeAndLimitPitchAngularSpeed(float add)
    {
        angularSpeedRad.x += add * velocityMPS.magnitude;
        angularSpeedRad.x = angularSpeedRad.x > 10.0f ? 10.0f : angularSpeedRad.x < -10.0f ? -10.0f : angularSpeedRad.x;

        // limit pitch angle
        if (transform.eulerAngles.x > 180.0f && transform.eulerAngles.x < 330.0f)
        {
            if (angularSpeedRad.x < 0)
                angularSpeedRad.x += 30.0f * Time.deltaTime;
        }
        if (transform.eulerAngles.x < 180.0f && transform.eulerAngles.x > 30.0f)
        {
            if (angularSpeedRad.x > 0)
                angularSpeedRad.x -= 30.0f * Time.deltaTime;
        }
    }

    // Update is called once per frame
    void Update()
    {
        updateInfo();

        // rotate propeller
        transform.GetChild(0).GetChild(0).Rotate(transform.forward, -1 * thrust * 40.0f, Space.World);
        transform.GetChild(0).GetChild(8).Rotate(transform.forward, -1 * thrust * 40.0f, Space.World);

        UpdateVelocity();
        CalcGravityAndFloat();
        UpdatePosition();
        UpdateOrientaion();

        new Dictionary<KeyCode, System.Action> {
            { KeyCode.Q, () => { ChangeAndLimitThrust(+0.5f * Time.deltaTime); } },
            { KeyCode.Z, () => { ChangeAndLimitThrust(-0.5f * Time.deltaTime); } },
            { KeyCode.A, () => { ChangeAndLimitYawAngularSpeed(-30.0f * Time.deltaTime); } },
            { KeyCode.D, () => { ChangeAndLimitYawAngularSpeed(+30.0f * Time.deltaTime); } },
            { KeyCode.W, () => { ChangeAndLimitPitchAngularSpeed(+30.0f * Time.deltaTime); } },
            { KeyCode.S, () => { ChangeAndLimitPitchAngularSpeed(-30.0f * Time.deltaTime); } },
            { KeyCode.E, () => { ballast += ballast <= -19.6f ? -ballast-19.6f : -6.0f * Time.deltaTime; } },
            { KeyCode.C, () => { ballast += ballast >= +9.80f ? -ballast+9.80f : +3.0f * Time.deltaTime; } },
            { KeyCode.X, () => { angularSpeedRad *= 0.0f; } },
        }
        .ToList()
        .Select(x => { if (Input.GetKey(x.Key)) x.Value(); return 0; })
        .Sum();

    }

    private void OnCollisionEnter(Collision collision)
    {
    }

    private void OnCollisionStay(Collision collision)
    {
    }

    private void OnCollisionExit(Collision collision)
    {
    }
}