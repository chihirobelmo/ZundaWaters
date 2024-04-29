using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public float targetRudderDeg = 0;

    PID thrustController = new PID(0.1f, 1.0f, 0);
    PID pitchController = new PID(0.1f, 5.0f, 0);

    

    public enum Bell : int
    {
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
        FlankAstern
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
        const float maxYawDegPerSecond = 360.0f;

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
        switch (bell)
        {
            case Bell.FlankAhead: return 40.0f;
            case Bell.FullAhead: return 30.0f;
            case Bell.HalfAhead: return 20.0f;
            case Bell.SlowAhead: return 10.0f;
            case Bell.DeadSlowAhead: return 5.0f;
            case Bell.Stop: return 0.0f;
            case Bell.DeadSlowAstern: return -5.0f;
            case Bell.SlowAstern: return -10.0f;
            case Bell.HalfAstern: return -20.0f;
            case Bell.FullAstern: return -30.0f;
            case Bell.FlankAstern: return -40.0f;
            default: return 0.0f;
        }
    }

    static public float TargetValueVector(float targetvalue, float currentValue, float startSpeedDecentRange)
        => (targetvalue - currentValue > 0 ? 1.0f : -1.0f) * Mathf.Clamp(Mathf.Abs(targetvalue - currentValue) / startSpeedDecentRange, 0.0f, 1.0f);

    float lastTimePressed;

    LosAngelsClassFlightII UserControl()
    {
        const float thrustUp = +1.0f;
        const float thrustDown = -1.0f;
        const float pitchUp = -1.0f;
        const float pitchDown = +1.0f;
        const float leftYaw = -1.0f;
        const float rightYaw = +1.0f;

        if (Input.anyKeyDown)
            lastTimePressed = Time.time;

        // KeyUP
        new Dictionary<KeyCode, System.Action> {
            // Q: Thrust Uo
            { KeyCode.Q, () => { targetThrustN = Mathf.Clamp(targetThrustN + 5.0f, -40.0f, 80.0f); } },
            // Z: Thrust Down
            { KeyCode.Z, () => { targetThrustN = Mathf.Clamp(targetThrustN - 5.0f, -40.0f, 80.0f); } },
            // W: Pitch Down
            { KeyCode.W, () => { targetPitchDeg = Mathf.Clamp(targetPitchDeg + 5.0f, -30.0f, 30.0f); } },
            // S: Pitch Up
            { KeyCode.S, () => { targetPitchDeg = Mathf.Clamp(targetPitchDeg - 5.0f, -30.0f, 30.0f); } },
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

    static float PIDAndLimitThrustN(PID pid, float thrustN, float targetThrustN)
    {
        thrustN += Mathf.Clamp(pid.run(thrustN, targetThrustN), -10.0f, 10.0f) * dt;
        return Mathf.Clamp(thrustN, -10.0f, 40.0f);
    }

    // Update is called once per frame
    void Update()
    {
        if (Main.offsetForReset.magnitude > 0) { return; }

        UserControl();

        thrustN = PIDAndLimitThrustN(thrustController, thrustN, targetThrustN);

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