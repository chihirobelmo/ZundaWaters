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

    public static float VtPerDt(float k, float m, float a, float v) =>  (-k / m) * (v - m * a / k);

    public static Vector3 VtPerDt(float k, float m, Vector3 a, Vector3 v) =>  (-k / m) * (v - m * a / k);

    const float KTS_TO_MPS = 1.94384f;
    const float gravity = 9.8f;
    const float mass = 7000/*ton displacement*/ * 1000/*kg*/ * 2.0f/*assume actual mass has twice displacement*/;
    const float waterDrag = 500000f;
    const float airDrag = 100000f;
    [SerializeField] float ballastAir = 9.8f;
    [SerializeField] float thrust = 1.0f;
    [SerializeField] Vector3 velocityMPS;
    [SerializeField] Vector3 gravityVelocityMPS;
    [SerializeField] Vector3 angularSpeedDeg;
    Vector3 lastPosition;

    void updateInfo()
    {
        lastPosition = transform.position;
    }

    void UpdateVelocity()
    {
        // propeller under the  water
        if ((transform.position + transform.forward * -50.0f).y <= 0)
        {
            velocityMPS += VtPerDt(waterDrag, mass, transform.forward * thrust, velocityMPS) * Time.deltaTime;
        }
        velocityMPS -= Vector3.ProjectOnPlane(velocityMPS, transform.right) * Time.deltaTime;
        velocityMPS -= Vector3.ProjectOnPlane(velocityMPS, transform.up) * Time.deltaTime;
    }

    void UpdatePosition() => transform.position += (velocityMPS + gravityVelocityMPS) * Time.deltaTime;

    void CalcGravityAndFloat() {

       // gravity for each cell

       IEnumerable < Vector3 > eachCellOfShip =
           from z in Enumerable.Range(-55,55)
           select transform.position + 
           transform.forward * z;

        int cellCount = eachCellOfShip.Count();

        eachCellOfShip.ToList().ForEach(p =>
        {
            float offset = (p.z - transform.position.z);
            if (p.y > 0)
            { // in air
                velocityMPS += VtPerDt(airDrag, mass, -Vector3.up * gravity, velocityMPS) * Time.deltaTime * (1.0f / cellCount);
                angularSpeedDeg.x -= Mathf.Rad2Deg * (2.0f * Mathf.PI / 60.0f) * gravity * offset * Time.deltaTime * (1.0f / cellCount);
            }
            if (p.y <= 0)
            { // under water
                velocityMPS += VtPerDt(airDrag, mass, -Vector3.up * (gravity - ballastAir), velocityMPS) * Time.deltaTime * (1.0f / cellCount);
                angularSpeedDeg.x -= Mathf.Rad2Deg * (2.0f * Mathf.PI / 60.0f) * (gravity - ballastAir) * offset * Time.deltaTime * (1.0f / cellCount);
            }
        });
    }

    void UpdateOrientaion()
    {
        // yaw
        transform.rotation *= Quaternion.Euler((angularSpeedDeg * Mathf.Deg2Rad) * Time.deltaTime);
        // roll fixed 0
        transform.rotation *= Quaternion.Euler((new Vector3(0, 0, -transform.eulerAngles.z) * Mathf.Deg2Rad) * Time.deltaTime);
        //transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, 0);
        //transform.GetChild(0).rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, -(vector - lastVector).magnitude * Mathf.Rad2Deg);
    }

    void ChangeAndLimitThrust(float add) 
        => thrust = Mathf.Clamp(thrust + add, -10.0f, 80.0f);

    void ChangeAndLimitYawAngularSpeed(float add) 
        => angularSpeedDeg.y = Mathf.Clamp(angularSpeedDeg.y + add * velocityMPS.magnitude / (1.0f * KTS_TO_MPS), -360.0f, 360.0f);

    void ChangeAndLimitPitchAngularSpeed(float add) 
        => angularSpeedDeg.x = Mathf.Clamp(angularSpeedDeg.x + add * velocityMPS.magnitude / (1.0f * KTS_TO_MPS), -30.0f, 30.0f);

    void StabilizeRoll() => transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, 0.0f);

    // Update is called once per frame
    void Update()
    {
        updateInfo();

        // rotate propeller
        transform.GetChild(0).GetChild(0).Rotate(transform.forward, -thrust, Space.World);
        transform.GetChild(0).GetChild(8).Rotate(transform.forward, -thrust, Space.World);

        UpdateVelocity();
        CalcGravityAndFloat();
        UpdatePosition();
        UpdateOrientaion();
        StabilizeRoll();

        new Dictionary<KeyCode, System.Action> {
            { KeyCode.Q, () => { ChangeAndLimitThrust(+10.0f * Time.deltaTime); } },
            { KeyCode.Z, () => { ChangeAndLimitThrust(-10.0f * Time.deltaTime); } },
            { KeyCode.A, () => { ChangeAndLimitYawAngularSpeed(-30.0f * Time.deltaTime); } },
            { KeyCode.D, () => { ChangeAndLimitYawAngularSpeed(+30.0f * Time.deltaTime); } },
            { KeyCode.W, () => { ChangeAndLimitPitchAngularSpeed(+30.0f * Time.deltaTime); } },
            { KeyCode.S, () => { ChangeAndLimitPitchAngularSpeed(-30.0f * Time.deltaTime); } },
            { KeyCode.E, () => { ballastAir = Mathf.Clamp(ballastAir + 0.1f * Time.deltaTime, 9.6f, 10.0f); } },
            { KeyCode.C, () => { ballastAir = Mathf.Clamp(ballastAir - 0.1f * Time.deltaTime, 9.6f, 10.0f); } },
            { KeyCode.X, () => { angularSpeedDeg *= 0.0f; ballastAir = +9.8f; } },
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