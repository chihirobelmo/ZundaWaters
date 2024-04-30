using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;
using static StaticMath;

public class LosAngelsClassFlightII : MonoBehaviour
{
    // Ship Parameters
    [SerializeField] const float kMass = 7000/*ton displacement*/ * 1000/*kg*/ * 2.0f/*assume actual mass has twice displacement*/;
    [SerializeField] const float kThrustChangeRate = 0.1f;
    [SerializeField] const float kSurfaceChangeRate = 30.0f;
    [SerializeField] const float kBallastChangeRate = 3.0f;
    [SerializeField] const float kMaxBallast = +10.8f;
    [SerializeField] const float kMinBallast = +8.8f;

    [SerializeField] double trueX = 0.0;
    [SerializeField] double trueZ = 0.0;

    const float kLengthMeter = 110.0f;
    const float kRadiusMeter = 5.0f;
    const float kWaterlineMeter = +1.0f;
    public Bell currentBell = Bell.DeadSlowAhead;
    public float targetThrustN = TargetThrustN(Bell.DeadSlowAhead);
    public float targetPitchDeg = 0;
    public float targetAileronDeg = 0;

    PID aileronController = new PID(5.0f, 1.5f, 0);

    public enum Bell : int
    {
        MinInvalid, // please keep this at first.
        FlankAhead,
        FullAhead,
        HalfAhead,
        SlowAhead,
        DeadSlowAhead,
        Stop,
        DeadSlowAstern,
        SlowAstern,
        HalfAstern,
        FullAstern,
        FlankAstern,
        MaxInvalid // Please keep this at end.
    }

    public bool IsPropellerUnderWater => Object3DPropellerAxis.position.y <= 0;

    /// <summary>
    /// Angle aileron in degree per second.
    /// </summary>
    [SerializeField] float angleAileronDeg = 0.0f;

    /// <summary>
    /// Angle rudder in degree per second.
    /// </summary>
    [SerializeField] float angleRudderDeg = 0.0f;

    /// <summary>
    /// anti gravity acceleration in m/s^2 only works under the water.
    /// </summary>
    [SerializeField] float ballastAirMPS2 = 9.8f;

    /// <summary>
    /// thrust made by propeller in N.
    /// </summary>
    [SerializeField] float thrustN = 7.0f;

    /// <summary>
    /// velocityspeed) in m/s per xyz axis
    /// </summary>
    [SerializeField] Vector3 velocityMPS;

    /// <summary>
    /// degree per second ship rotates around gc to xyz axis.
    /// </summary>
    [SerializeField] Vector3 angularSpeedDeg;

    private Vector3 lastPosition;

    /// <summary>
    /// Ship Movement Vector
    /// </summary>
    public Vector3 Vector => transform.position - lastPosition;
    public float Knots => Vector.magnitude * KTS_TO_MPS;

    // Use this for initialization
    void Start()
    {
        velocityMPS = transform.forward * 5.0f/*knots*/ * KTS_TO_MPS;
        lastPosition = transform.position;
    }

    LosAngelsClassFlightII UpdateVelocity()
    {
        // propeller under the  water
        if (IsPropellerUnderWater)
            velocityMPS += VtDt(waterDrag, kMass, transform.forward * thrustN, velocityMPS) * dt;
        // cancel inartial velocity to ship right and up axis
        velocityMPS -= Vector3.ProjectOnPlane(velocityMPS, transform.right) * dt;
        velocityMPS -= Vector3.ProjectOnPlane(velocityMPS, transform.up) * dt;
        // TBD: calc drag to cancel inartial velocity, use ElipsoidProjectedAreaM2.
        return this;
    }

    LosAngelsClassFlightII UpdatePosition() {
        transform.position += velocityMPS * dt;
        return this;
    }
    LosAngelsClassFlightII CalcAndApplyGravityAndFloat() {

        // Divide ship to each cell
        IEnumerable<Vector3> eachCellOfShip =
            from z in new int[] { -55, -27, 27, 55 }
            select transform.position + transform.forward * z;

        int cellCount = eachCellOfShip.Count();

        // Calculate gravity and float makes velocity and rotation for each cell.
        eachCellOfShip.ToList().ForEach(p =>
        {
            // calculate N from gravity and float at each cell.
            // remember: v = omega * radius
            float omega = angularSpeedDeg.x;
            float radius = /*avoid 0 divide*/0.001f + (/*offset*/p.z - /*gravity center*/transform.position.z);

            // calc dV = dOmega * radius
            Vector3 dV = p.y > 0 ?
            // in air, current speed is considered "angular speed * radius", so angular speed is the omega
            VtDt(waterDrag, kMass, -Vector3.up * gravity, angularSpeedDeg * Mathf.Deg2Rad * radius) * dt * (1.0f / cellCount) :
            // under water
            VtDt(waterDrag, kMass, -Vector3.up * (gravity - ballastAirMPS2), angularSpeedDeg * Mathf.Deg2Rad * radius) * dt * (1.0f / cellCount);
            // TBD: not sure why but only calculating y axis were not working well...
            // and calc Vector3 like we do now makes X axis movement...so we remove this from Vector3.
            dV.x = 0; dV.z = 0;

            // apply dV to velocity;
            velocityMPS += dV;
            // apply dV = omega * radius to angular speed, consider dV = Omega. cos more radius more speed should apply...
            angularSpeedDeg.x -= Mathf.Rad2Deg * dV.y * radius;
        });
        return this;
    }

    LosAngelsClassFlightII UpdateOrientaion()
    {
        // yaw and pitch
        // somehow input yaw also inputs pitch so placeholder for now.
        angularSpeedDeg.y = angleRudderDeg; // Mathf.Clamp(angularSpeedDeg.y.Deg() + (angleRudderDeg - angularSpeedDeg.y).Degt() * dt, -80, 80);
        angularSpeedDeg.x = angleAileronDeg; // Mathf.Clamp(angularSpeedDeg.x.Deg() + (angleAileronDeg - angularSpeedDeg.x).Degt() * dt, -40, 40);

        transform.rotation *= Quaternion.Euler((angularSpeedDeg * Mathf.Deg2Rad).Radt() * dt);

        return this;
    }

    void ChangeAndLimitThrust(float dN)
        => thrustN = Mathf.Clamp(thrustN + dN, -10.0f, 40.0f);

    void ChangeAndLimitYawAngularSpeed(float dDeg)
        => angleRudderDeg = Mathf.Clamp(angleRudderDeg + dDeg, /*min*/-80.0f/*deg*/, /*max*/80.0f/*deg*/);

    void ChangeAndLimitPitchAngularSpeed(float dDeg)
        => angleAileronDeg = Mathf.Clamp(angleAileronDeg + dDeg, /*min*/-40.0f/*deg*/, /*max*/40.0f/*deg*/);

    /// <summary>
    /// force roll to 0.
    /// </summary>
    LosAngelsClassFlightII StabilizeRoll() {
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x.Deg(), transform.eulerAngles.y.Deg(), 0.0f.Deg());
        return this;
    }

    Transform Object3DPropellerBlades => transform.GetChild(0).GetChild(0);
    Transform Object3DPropellerAxis => transform.GetChild(0).GetChild(8);

    LosAngelsClassFlightII Animation()
    {
        const float propellerRadiusMeter = 2.5f;
        const float leftRotation = -1.0f;
        const float propellerRotationRatio = /*make it 1 rad at certain thrust*/(1.0f / /*thrust value where rotation becomes 1.0 rad*/10.0f);
        new List<Transform> { Object3DPropellerBlades, Object3DPropellerAxis }
        .ForEach(x => x.Rotate(transform.forward, leftRotation * (thrustN / propellerRadiusMeter) * propellerRotationRatio, Space.World));

        return this;
    }

    static public float TargetThrustN(Bell bell)
    {
        if (new Dictionary<Bell, float> {
            { Bell.FlankAhead, 40.0f },
            { Bell.FullAhead, 30.0f },
            { Bell.HalfAhead, 20.0f },
            { Bell.SlowAhead, 10.0f },
            { Bell.DeadSlowAhead, 5.0f },
            { Bell.Stop, 0.0f },
            { Bell.DeadSlowAstern, -5.0f },
            { Bell.SlowAstern, -10.0f },
            { Bell.HalfAstern, -20.0f },
            { Bell.FullAstern, -30.0f },
            { Bell.FlankAstern, -40.0f }
        }.TryGetValue(bell, out float targetThrustN)) {
            return targetThrustN;
        }
        return 0.0f;
    }

    static public Bell ChangeBell(Bell bell, int add) {
        return (int)bell + add <= (int)Bell.MinInvalid ? Bell.FlankAhead :
               (int)bell + add >= (int)Bell.MaxInvalid ? Bell.FlankAstern :
               bell + add;
    }

    static public float TargetValueVector(float targetvalue, float currentValue, float startSpeedDecentRange, float magnitude)
        => magnitude 
        * (targetvalue - currentValue > 0 ? 1.0f : -1.0f) 
        * Mathf.Clamp(Mathf.Abs(targetvalue - currentValue) / startSpeedDecentRange, 0.0f, 1.0f);

    float lastTimePressed;

    public float truePitch;

    LosAngelsClassFlightII UserControl()
    {
        const int thrustUp = -1;
        const int thrustDown = +1;
        const float pitchUp = +1.0f;
        const float pitchDown = -1.0f;
        const float leftYaw = -1.0f;
        const float rightYaw = +1.0f;

        if (Input.anyKeyDown)
            lastTimePressed = Time.time;

        // KeyUP
        new Dictionary<KeyCode, System.Action> {
            // Q: Thrust Uo
            { KeyCode.Q, () => { currentBell = ChangeBell(currentBell, thrustUp); } },
            // Z: Thrust Down
            { KeyCode.Z, () => { currentBell = ChangeBell(currentBell, thrustDown); } },
            // W: Pitch Down
            { KeyCode.W, () => { targetPitchDeg = Mathf.Clamp(targetPitchDeg - 5.0f, -15.0f, 15.0f); aileronController.reset(); } },
            // S: Pitch Up
            { KeyCode.S, () => { targetPitchDeg = Mathf.Clamp(targetPitchDeg + 5.0f, -15.0f, 15.0f); aileronController.reset(); } },
        }
        .ToList()
        .Select(x => { if (Input.GetKeyUp(x.Key)) x.Value(); return 0; })
        .Sum();

        // KeyHold
        new Dictionary<KeyCode, System.Action> {
            // W: Pitch Down
            { KeyCode.W, () => ChangeAndLimitPitchAngularSpeed(pitchDown * kSurfaceChangeRate * dt) },
            // S: Pitch Up
            { KeyCode.S, () => ChangeAndLimitPitchAngularSpeed(pitchUp * kSurfaceChangeRate * dt) },
            // A: Yaw Left
            { KeyCode.A, () => ChangeAndLimitYawAngularSpeed(leftYaw * kSurfaceChangeRate * dt) },
            // D: Yaw Right
            { KeyCode.D, () => ChangeAndLimitYawAngularSpeed(rightYaw * kSurfaceChangeRate * dt) },
            // E: Ballast more air
            { KeyCode.E, () => { ballastAirMPS2 = Mathf.Clamp(ballastAirMPS2 + kBallastChangeRate * dt, kMinBallast, kMaxBallast); } },
            // C: Ballast more water
            { KeyCode.C, () => { ballastAirMPS2 = Mathf.Clamp(ballastAirMPS2 - kBallastChangeRate * dt, kMinBallast, kMaxBallast); } },
            // X: Reset Yaw/Pitch/Ballast
            { KeyCode.X, () => {
                angleRudderDeg -= angleRudderDeg * dt;
                angleAileronDeg -= angleAileronDeg * dt;
                ballastAirMPS2 -= (ballastAirMPS2 - gravity) * dt;
            } },
        }
        .ToList()
        .Select(x => { if (Input.GetKey(x.Key)) x.Value(); return 0; })
        .Sum();

        return this;
    }

    // Update is called once per frame
    void Update()
    {
        if (Main.offsetForReset.magnitude > 0) { return; }

        UserControl();

        // control Thrust to target value, 
        thrustN += TargetValueVector(TargetThrustN(currentBell), thrustN, 2.5f, 1.0f) * dt;
        // PID aileron to target pitch
        truePitch = transform.TruePitch();
        targetAileronDeg = -Math.Clamp(aileronController.run(truePitch, targetPitchDeg), -40.0f, +40.0f);
        angleAileronDeg += TargetValueVector(targetAileronDeg, angleAileronDeg, 5.0f, 10.0f) * dt;

        UpdateVelocity();
        UpdatePosition();
        UpdateOrientaion();
        CalcAndApplyGravityAndFloat();
        StabilizeRoll();
        Animation();
        EndFrameJob();
    }

    private void EndFrameJob()
    {
        // Update True XY
        trueX += transform.position.x - lastPosition.x;
        trueZ += transform.position.z - lastPosition.z;

        // Update LastPosition
        lastPosition = transform.position;

        // floating point origin reset
        if (transform.position.x >= 256)
        {
            lastPosition = new Vector3(
                transform.position.x - 256f * 2.0f,
                transform.position.y,
                transform.position.z);
            Main.offsetForReset = lastPosition - transform.position;
        }
        if (transform.position.z >= 256)
        {
            lastPosition = new Vector3(
                transform.position.x,
                transform.position.y,
                transform.position.z - 256f * 2.0f);
            Main.offsetForReset = lastPosition - transform.position;
        }
        if (transform.position.x <= -256)
        {
            lastPosition = new Vector3(
                transform.position.x + 256f * 2.0f,
                transform.position.y,
                transform.position.z);
            Main.offsetForReset = lastPosition - transform.position;
        }
        if (transform.position.z <= -256)
        {
            lastPosition = new Vector3(
                transform.position.x,
                transform.position.y,
                transform.position.z + 256f * 2.0f);
            Main.offsetForReset = lastPosition - transform.position;
        }
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