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
    float balast = -9.8f;
    float thrust = 100.0f; // m/s2
    float fallspeed = 0.0f;
    readonly float mass = 7000/*ton displacement*/ * 1000/*kg*/ * 2.0f/*assume actual mass has twice displacement*/;
    Vector3 velocityMPS = new Vector3(0, 0, 0);
    Vector3 gravityMoment = new Vector3(0, 0, 0);
    Vector3 angularSpeed_rad = new Vector3(0, 0, 0);

    void CalcVelocity() { velocityMPS += (
            transform.forward * VtPerDt(mass, mass, thrust, velocityMPS.magnitude)
            - transform.right * SpeedToVector(velocityMPS, transform.right) // remove
            - transform.up * SpeedToVector(velocityMPS, transform.up) // remove
            ) * Time.deltaTime; }

    void OnUpdatePosition() {
        transform.position += velocityMPS * Time.deltaTime;
    }

    void OnUpdateOrientation()
    {
        transform.rotation *= Quaternion.Euler(angularSpeed_rad * Time.deltaTime);
        // yaw
        //transform.rotation *= Quaternion.Euler(angularSpeed_rad * Time.deltaTime);
        // roll fixed 0
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, 0);
        //if (transform.eulerAngles.x > 180.0f && transform.eulerAngles.x < 330.0f)
        //{
        //    if (angularSpeed_rad.x < 0)
        //        angularSpeed_rad.x += 30.0f * Time.deltaTime;
        //}
        //if (transform.eulerAngles.x < 180.0f && transform.eulerAngles.x > 30.0f)
        //{
        //    if (angularSpeed_rad.x > 0)
        //        angularSpeed_rad.x -= 30.0f * Time.deltaTime;
        //}
    }

    float CalcPitch(Transform transform)
    {
        Matrix4x4 M = transform.worldToLocalMatrix;
        return Mathf.Atan2(-M.m10, Mathf.Sqrt(M.m01 * M.m01 + M.m00 * M.m00));
    }

    /*
    roll = atan2(M[1][2], M[1][1])
    pitch = atan2(-M[1][0], sqrt(M[0][1] * M[0][1] + M[0][0] * M[0][0]))
    yaw = atan2(M[2][0], M[0][0])
    */

    void ChangeAndLimitThrust(float add)
    {
        thrust += add;
        thrust = thrust > 30.0f ? 30.0f : thrust < -5.0f ? -5.0f : thrust;
    }

    void ChangeAndLimitYawAngularSpeed(float add)
    {
        angularSpeed_rad.y += add;
        angularSpeed_rad.y = angularSpeed_rad.y > 30.0f ? 30.0f : angularSpeed_rad.y < -30.0f ? -30.0f : angularSpeed_rad.y;
    }

    void ChangeAndLimitPitch(float add)
    {
        angularSpeed_rad.x += add;
        angularSpeed_rad.x = angularSpeed_rad.x > 10.0f ? 10.0f : angularSpeed_rad.x < -10.0f ? -10.0f : angularSpeed_rad.x;
    }

    const float k = 100f;
    const float m = 15000f;
    Vector3 lastPosition;
    float speed_mps = 0; // only for update.

    float[] floatArray(float start, float end, float step) {
        List<float> r = new List<float>();
        for (float f = start; f <= end; f += step) {
            r.Add(f);
        }
        return r.ToArray();
    }

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
        OnUpdatePosition();
        OnUpdateOrientation();

        new Dictionary<KeyCode, System.Action> {
            { KeyCode.Q, () => { ChangeAndLimitThrust(+0.1f * Time.deltaTime); } },
            { KeyCode.Z, () => { ChangeAndLimitThrust(-0.1f * Time.deltaTime); } },
            { KeyCode.A, () => { ChangeAndLimitYawAngularSpeed(-30.0f * Time.deltaTime); } },
            { KeyCode.D, () => { ChangeAndLimitYawAngularSpeed(+30.0f * Time.deltaTime); } },
            { KeyCode.W, () => { ChangeAndLimitPitch(+30.0f * Time.deltaTime); } },
            { KeyCode.S, () => { ChangeAndLimitPitch(-30.0f * Time.deltaTime); } },
            { KeyCode.E, () => { balast += 1.0f * Time.deltaTime; } },
            { KeyCode.C, () => { balast -= 1.0f * Time.deltaTime; } },
        }
        .ToList()
        .Select(x => { if (Input.GetKey(x.Key)) x.Value(); return 0; })
        .Sum();

    }

    private void OnCollisionEnter(Collision collision)
    {
        //speed_kts = 0.0f;
    }

    private void OnCollisionStay(Collision collision)
    {
    }

    private void OnCollisionExit(Collision collision)
    {
    }
}


// gravity for each cell
//IEnumerable<Vector3> eachCellOfShip =
//    from z in new int[] { -50, 50 }
//    from y in floatArray(-10, 10, 0.01f)
//    from x in new int[] { -1, 1 }
//    select transform.GetChild(0).position + transform.GetChild(0).forward * z + transform.GetChild(0).up * y + transform.GetChild(0).right * x;

//int cellCount = eachCellOfShip.Count();

//eachCellOfShip.ToList().ForEach(p => {
//    float offset = (p.z - transform.GetChild(0).position.z);
//    if (p.y > 0)
//    { // under water
//        fallspeed += VtPerDt(100f, weight * (1.0f / cellCount), 9.8f, fallspeed) * Time.deltaTime;
//        gravityMoment += Mathf.Deg2Rad * Quaternion.Euler(transform.GetChild(0).forward * (9.8f - balast) * offset * Time.deltaTime * -1.0f).eulerAngles * Time.deltaTime * (1.0f / cellCount);
//    }
//    if (p.y <= 0)
//    { // under water
//        fallspeed += VtPerDt(500000f, weight * (1.0f / cellCount), 9.8f, fallspeed) * Time.deltaTime;
//        fallspeed += VtPerDt(500000f, weight * (1.0f / cellCount), - (9.8f - balast), fallspeed) * Time.deltaTime;
//        gravityMoment += Mathf.Deg2Rad * Quaternion.Euler(transform.GetChild(0).forward * (9.8f - balast) * offset * Time.deltaTime * -1.0f).eulerAngles * Time.deltaTime * (1.0f / cellCount);
//    }
//});