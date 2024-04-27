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
    const float waterDrag = 5000000f;
    const float airDrag = 1f;

    [SerializeField] float angleAileron = 0.0f;
    [SerializeField] float angleRudder = 0.0f;

    [SerializeField] float ballastAir = 9.8f;
    [SerializeField] float thrust = 1.0f;
    [SerializeField] Vector3 velocityMPS;
    [SerializeField] Vector3 angularSpeedDeg;

    [SerializeField] float Knots { 
        get => (lastPosition - transform.position).magnitude;
        set => velocityMPS = transform.forward * value; }
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
        // cancel inartial velocity
        velocityMPS -= Vector3.ProjectOnPlane(velocityMPS, transform.right) * Time.deltaTime;
        velocityMPS -= Vector3.ProjectOnPlane(velocityMPS, transform.up) * Time.deltaTime;
    }

    void UpdatePosition() 
        => transform.position += velocityMPS * Time.deltaTime;

    void CalcGravityAndFloat() {

       // Divide ship to each cell
       IEnumerable < Vector3 > eachCellOfShip =
           from z in Enumerable.Range(-55,55)
           select transform.position + 
           transform.forward * z;

        int cellCount = eachCellOfShip.Count();

        // Calculate gravity and float makes velocity and rotation for each cell.
        eachCellOfShip.ToList().ForEach(p =>
        {
            // GC = Gravity Center
            float offsetFromGC = (p.z - transform.position.z);

            // 
            Vector3 n = p.y > 0 ? 
            VtPerDt(airDrag, mass, -Vector3.up * gravity, velocityMPS) * (1.0f / cellCount) : // in air
            VtPerDt(waterDrag, mass, -Vector3.up * (gravity - ballastAir), velocityMPS) * (1.0f / cellCount); // under water

            velocityMPS += n * Time.deltaTime;
            angularSpeedDeg.x += Mathf.Rad2Deg * (2.0f * Mathf.PI / 60.0f) * n.y * offsetFromGC * Time.deltaTime;
        });
    }

    void UpdateOrientaion()
    {
        // yaw and pitch
        angularSpeedDeg.y = Mathf.Clamp(angularSpeedDeg.y + (angleRudder - angularSpeedDeg.y) * Time.deltaTime, -360.0f, 360.0f);
        angularSpeedDeg.x = Mathf.Clamp(angularSpeedDeg.x + (angleAileron - angularSpeedDeg.x) * Time.deltaTime, -360.0f, 360.0f);
        angularSpeedDeg.y = Mathf.Clamp(angularSpeedDeg.y, -360.0f, 360.0f);
        angularSpeedDeg.x = Mathf.Clamp(angularSpeedDeg.x, -60.0f, 60.0f);

        transform.rotation *= Quaternion.Euler((angularSpeedDeg * Mathf.Deg2Rad) * Time.deltaTime);

        // roll fixed 0
        transform.rotation *= Quaternion.Euler((new Vector3(0, 0, -transform.eulerAngles.z) * Mathf.Deg2Rad) * Time.deltaTime);
    }

    void ChangeAndLimitThrust(float add) 
        => thrust = Mathf.Clamp(thrust + add, -10.0f, 40.0f);

    void ChangeAndLimitYawAngularSpeed(float add) 
        => angleRudder = Mathf.Clamp(angleRudder + add, -30.0f, 30.0f);

    void ChangeAndLimitPitchAngularSpeed(float add) 
        => angleAileron = Mathf.Clamp(angleAileron + add, -30.0f, 30.0f);

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
            // Thrust Uo
            { KeyCode.Q, () => { ChangeAndLimitThrust(+1.0f * Time.deltaTime); } },
            // Thrust Down
            { KeyCode.Z, () => { ChangeAndLimitThrust(-1.0f * Time.deltaTime); } },
            // Yaw Left
            { KeyCode.A, () => { ChangeAndLimitYawAngularSpeed(-30.0f * Time.deltaTime); } },
            // Yaw Right
            { KeyCode.D, () => { ChangeAndLimitYawAngularSpeed(+30.0f * Time.deltaTime); } },
            // Pitch Down
            { KeyCode.W, () => { ChangeAndLimitPitchAngularSpeed(+30.0f * Time.deltaTime); } },
            // Pitch Up
            { KeyCode.S, () => { ChangeAndLimitPitchAngularSpeed(-30.0f * Time.deltaTime); } },
            // Ballast more air
            { KeyCode.E, () => { ballastAir = Mathf.Clamp(ballastAir + 0.1f * Time.deltaTime, 9.6f, 10.0f); } },
            // Ballast more water
            { KeyCode.C, () => { ballastAir = Mathf.Clamp(ballastAir - 0.1f * Time.deltaTime, 9.6f, 10.0f); } },
            // Reset Yaw/Pitch/Ballast
            { KeyCode.X, () => { 
                angleRudder -= angleRudder * Time.deltaTime;
                angleAileron -= angleAileron * Time.deltaTime;
                ballastAir -= (ballastAir - 9.8f) * Time.deltaTime;
            } },
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