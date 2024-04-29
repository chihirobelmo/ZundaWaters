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

    enum Bell
    {
        FullAhead,
        HalfAhead,
        SlowAhead,
        DeadSlowAhead,
        Stop,
        DeadSlowAstern,
        SlowAstern,
        HalfAstern,
        FullAstern
    }

    public bool IsPropellerUnderWater => Object3DPropellerAxis.position.y <= 0;

    /// <summary>
    /// Ship Forward Vector in world space.
    /// </summary>
    public Vector3 Forward => transform.forward;

    /// <summary>
    /// Ship Right Vector in world space.
    /// </summary>
    public Vector3 Right => transform.right;

    /// <summary>
    /// Ship Up Vector in world space.
    /// </summary>
    public Vector3 Up => transform.up;

    /// <summary>
    /// Ship Position in world space.
    /// </summary>
    public Vector3 Pos { get => transform.position; set => transform.position = value; }

    /// <summary>
    /// Ship Rotation in quaternion.
    /// </summary>
    public Quaternion Rot { get => transform.rotation; set => transform.rotation = value; }

    /// <summary>
    /// Rotation Speed in degree per second the ship rotates to fwd/aft (x-axis).
    /// </summary>
    public float FwdRotationSpeedDeg { get => angularSpeedDeg.x; set => angularSpeedDeg.x = value; }

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

    private Vector3 lastPosition_;

    public Vector3 LastPosition { get => lastPosition_; set => lastPosition_ = Pos; }

    /// <summary>
    /// Ship Movement Vector
    /// </summary>
    public Vector3 Vector => Pos - LastPosition;
    public float Knots => Vector.magnitude * KTS_TO_MPS;

    // Use this for initialization
    void Start()
    {
        velocityMPS = Forward * 5.0f/*knots*/ * KTS_TO_MPS;
        LastPosition = Pos;
    }

    LosAngelsClassFlightII updateLastPosition()
    {
        LastPosition = Pos;
        return this;
    }

    LosAngelsClassFlightII UpdateVelocity()
    {
        // propeller under the  water
        if (IsPropellerUnderWater)
        {
            velocityMPS += VtDt(waterDrag, kMass, Forward * thrustN, velocityMPS) * dt;
        }
        // cancel inartial velocity
        velocityMPS -= Vector3.ProjectOnPlane(velocityMPS, Right) * dt;
        velocityMPS -= Vector3.ProjectOnPlane(velocityMPS, Up) * dt;
        // TBD: calc drag to cancel inartial velocity, use ElipsoidProjectedAreaM2.
        return this;
    }

    LosAngelsClassFlightII UpdatePosition() {
        Pos += velocityMPS * dt;
        return this;
    }
    LosAngelsClassFlightII CalcAndApplyGravityAndFloat() {


        // Divide ship to each cell
        IEnumerable<Vector3> eachCellOfShip =
            from z in new int[] { -55, -27, 27, 55 }
            select Pos + Forward * z;

        int cellCount = eachCellOfShip.Count();

        // Calculate gravity and float makes velocity and rotation for each cell.
        eachCellOfShip.ToList().ForEach(p =>
        {
            // calculate N from gravity and float at each cell.
            // remember: v = omega * radius
            float omega = FwdRotationSpeedDeg;
            float radius = /*avoid 0 divide*/0.001f + (/*offset*/p.z - /*gravity center*/Pos.z);

            Vector3 dv = p.y > 0 ?
            VtDt(waterDrag, kMass, -Vector3.up * gravity, angularSpeedDeg * Mathf.Deg2Rad * radius) * dt * (1.0f / cellCount) : // in air
            VtDt(waterDrag, kMass, -Vector3.up * (gravity - ballastAirMPS2), angularSpeedDeg * Mathf.Deg2Rad * radius) * dt * (1.0f / cellCount); // under water
            dv.x = 0; dv.z = 0; // TBD: not sure why but only calculating y axis were not working well...so we remove this from Vector3.

            // apply N to velocity;
            velocityMPS += dv;
            FwdRotationSpeedDeg -= Mathf.Rad2Deg * dv.y * radius;
        });
        return this;
    }

    LosAngelsClassFlightII UpdateOrientaion()
    {
        const float maxYawDegPerSecond = 360.0f;

        // yaw and pitch
        angularSpeedDeg.y = angleRudderDeg; // Mathf.Clamp(angularSpeedDeg.y.Deg() + (angleRudderDeg - angularSpeedDeg.y).Degt() * dt, -80, 80);
        angularSpeedDeg.x = angleAileronDeg; // Mathf.Clamp(angularSpeedDeg.x.Deg() + (angleAileronDeg - angularSpeedDeg.x).Degt() * dt, -40, 40);

        Rot *= Quaternion.Euler((angularSpeedDeg * Mathf.Deg2Rad).Radt() * dt);

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
        Rot = Quaternion.Euler(transform.eulerAngles.x.Deg(), transform.eulerAngles.y.Deg(), 0.0f.Deg());
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
        .ForEach(x => x.Rotate(Forward, leftRotation * (thrustN / propellerRadiusMeter) * propellerRotationRatio, Space.World));

        return this;
    }

    LosAngelsClassFlightII UserControl()
    {
        const float thrustUp = +1.0f;
        const float thrustDown = -1.0f;
        const float pitchUp = -1.0f;
        const float pitchDown = +1.0f;
        const float leftYaw = -1.0f;
        const float rightYaw = +1.0f;

        new Dictionary<KeyCode, System.Action> {
            // Q: Thrust Uo
            { KeyCode.Q, () => ChangeAndLimitThrust(thrustUp * kThrustChangeRate * dt) },
            // Z: Thrust Down
            { KeyCode.Z, () => ChangeAndLimitThrust(thrustDown * kThrustChangeRate * dt) },
            // A: Yaw Left
            { KeyCode.A, () => ChangeAndLimitYawAngularSpeed(leftYaw * kSurfaceChangeRate * dt) },
            // D: Yaw Right
            { KeyCode.D, () => ChangeAndLimitYawAngularSpeed(rightYaw * kSurfaceChangeRate * dt) },
            // W: Pitch Down
            { KeyCode.W, () => ChangeAndLimitPitchAngularSpeed(pitchDown * kSurfaceChangeRate * dt) },
            // S: Pitch Up
            { KeyCode.S, () => ChangeAndLimitPitchAngularSpeed(pitchUp * kSurfaceChangeRate * dt) },
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
    void Update() =>
        UserControl()
        .UpdateVelocity()
        .CalcAndApplyGravityAndFloat()
        .UpdatePosition()
        .UpdateOrientaion()
        .StabilizeRoll()
        .Animation()
        .updateLastPosition();

    private void EndFrameJob()
    {
        // Update True XY
        trueX += transform.position.x - lastPosition.x;
        trueZ += transform.position.z - lastPosition.z;

        // Update LastPosition
        lastPosition = transform.position;

        // floating point origin reset
        if (transform.position.magnitude >= 100.0f)
        {
            lastPosition = new Vector3(0,transform.position.y,0);
            Main.floatOriginResetFlag = true;
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