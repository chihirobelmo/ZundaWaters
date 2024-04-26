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
        GetComponent<Rigidbody>().drag = 1.0f;
    }

    public static float VtPerDt(float k, float m, float a, float v)
    {
        return (-k / m) * (v * v - m * a / k);
    }

    public static float SpeedToVector(Vector3 source, Vector3 vector) {
        return source.x * vector.normalized.x + source.y * vector.normalized.y + source.z * vector.normalized.z;
    }

    const float KTS_TO_MPS = 1.94384f;
    float gravity = 9.8f;
    float ballast = -9.8f * 2.0f;
    float thrust = 100.0f; // m/s2
    float fallspeed = 0.0f;
    readonly float mass = 7000/*ton displacement*/ * 1000/*kg*/ * 2.0f/*assume actual mass has twice displacement*/;
    Vector3 velocityMPS = new Vector3(0, 0, 0);
    Vector3 gravityMoment = new Vector3(0, 0, 0);
    Vector3 angularSpeed_rad = new Vector3(0, 0, 0);

    void CalcVelocity() { velocityMPS += (
            transform.forward * VtPerDt(mass, mass, thrust, velocityMPS.magnitude)
            - transform.right * SpeedToVector(velocityMPS, transform.right) // cancel inertia
            - transform.up * SpeedToVector(velocityMPS, transform.up) // remove inertia
            // help me for good calc method to cancel inertia.
            ) * Time.deltaTime; }

    void CalcGravityAndFloat() {

       // gravity for each cell

       IEnumerable < Vector3 > eachCellOfShip =
           from z in new int[] { -50 -25, 0, 25, 50 }
           from y in new int[] { -2, 2, 5 }
           from x in new int[] { -5, 5 }
           select transform.GetChild(0).position + 
           transform.GetChild(0).forward * z + 
           transform.GetChild(0).up * y + 
           transform.GetChild(0).right * x;

       int cellCount = eachCellOfShip.Count();

        eachCellOfShip.ToList().ForEach(p =>
        {
            float offset = (p.z - transform.GetChild(0).position.z);
            if (p.y > 0)
            { // in air
                velocityMPS.y -= gravity * Time.deltaTime * (1.0f / cellCount); // gravity
                transform.rotation *= Quaternion.Euler(transform.GetChild(0).forward * gravity * offset * Time.deltaTime * +1.0f * Time.deltaTime * (1.0f / cellCount) * 6.0f);
            }
            if (p.y <= 0)
            { // under water
                velocityMPS.y -= gravity * Time.deltaTime * (1.0f / cellCount); // gravity
                velocityMPS.y -= ballast * Time.deltaTime * (1.0f / cellCount); // float
                transform.rotation *= Quaternion.Euler(transform.GetChild(0).forward * gravity * offset * Time.deltaTime * +1.0f * Time.deltaTime * (1.0f / cellCount) * 6.0f);
                transform.rotation *= Quaternion.Euler(transform.GetChild(0).forward * ballast * offset * Time.deltaTime * -1.0f * Time.deltaTime * (1.0f / cellCount) * 6.0f);
            }
        });
    }

    void OnUpdatePosition() {
        transform.position += velocityMPS * Time.deltaTime;
    }

    void OnUpdateOrientation()
    {
        // yaw
        transform.rotation *= Quaternion.Euler(angularSpeed_rad * Time.deltaTime);
        // roll fixed 0
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, 0);
        transform.GetChild(0).rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, -angularSpeed_rad.y);
    }

    void ChangeAndLimitThrust(float add)
    {
        thrust += add;
        thrust = thrust > 400.0f ? 400.0f : thrust < -50.0f ? -50.0f : thrust;
    }

    void ChangeAndLimitYawAngularSpeed(float add)
    {
        angularSpeed_rad.y += add;
        angularSpeed_rad.y = angularSpeed_rad.y > 30.0f ? 30.0f : angularSpeed_rad.y < -30.0f ? -30.0f : angularSpeed_rad.y;
    }

    void ChangeAndLimitPitchAngularSpeed(float add)
    {
        angularSpeed_rad.x += add;
        angularSpeed_rad.x = angularSpeed_rad.x > 10.0f ? 10.0f : angularSpeed_rad.x < -10.0f ? -10.0f : angularSpeed_rad.x;

        // limit pitch angle
        if (transform.eulerAngles.x > 180.0f && transform.eulerAngles.x < 330.0f)
        {
            if (angularSpeed_rad.x < 0)
                angularSpeed_rad.x += 30.0f * Time.deltaTime;
        }
        if (transform.eulerAngles.x < 180.0f && transform.eulerAngles.x > 30.0f)
        {
            if (angularSpeed_rad.x > 0)
                angularSpeed_rad.x -= 30.0f * Time.deltaTime;
        }
    }

    Vector3 lastPosition;
    float speed_mps = 0; // only for update.

    void updateInfo()
    {
        speed_mps = (lastPosition - transform.position).magnitude;
        gravityMoment = new Vector3(0, 0, 0);
        lastPosition = transform.GetChild(0).position;
    }

    // Update is called once per frame
    void Update()
    {
        updateInfo();

        // rotate propeller
        transform.GetChild(0).GetChild(0).Rotate(transform.forward, -1 * thrust * 40.0f, Space.World);
        transform.GetChild(0).GetChild(8).Rotate(transform.forward, -1 * thrust * 40.0f, Space.World);

        CalcVelocity();
        CalcGravityAndFloat();
        OnUpdatePosition();
        OnUpdateOrientation();

        new Dictionary<KeyCode, System.Action> {
            { KeyCode.Q, () => { ChangeAndLimitThrust(+100.0f * Time.deltaTime); } },
            { KeyCode.Z, () => { ChangeAndLimitThrust(-100.0f * Time.deltaTime); } },
            { KeyCode.A, () => { ChangeAndLimitYawAngularSpeed(-30.0f * Time.deltaTime); } },
            { KeyCode.D, () => { ChangeAndLimitYawAngularSpeed(+30.0f * Time.deltaTime); } },
            { KeyCode.W, () => { ChangeAndLimitPitchAngularSpeed(+30.0f * Time.deltaTime); } },
            { KeyCode.S, () => { ChangeAndLimitPitchAngularSpeed(-30.0f * Time.deltaTime); } },
            { KeyCode.E, () => { ballast += ballast <= -19.6f ? -ballast-19.6f : -6.0f * Time.deltaTime; } },
            { KeyCode.C, () => { ballast += ballast >= +9.80f ? -ballast+9.80f : +3.0f * Time.deltaTime; } },
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