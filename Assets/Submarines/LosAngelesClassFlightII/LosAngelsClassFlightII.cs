using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;
using static StaticMath;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

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

    ShipBehaviour.ShipSpec spec = new ShipBehaviour.ShipSpec
    {
        kMassKg = kMass,
        kThrustChangeRateNPerSec = kMass,
        kSurfaceChangeRateDegPerSec = 30.0f,
        kBallastChangeRateMeterPerSec2 = 0.1f,
        kMaxBallastAirMPS2 = 10.0f,
        kMinBallastAirMPS2 = 9.6f,
        kLengthMeter = 110f,
        kRadiusMeter = 5f,
        kMaxPitchDeg = 15.0f,
        kMaxAileronDeg = 40.0f,
        kMaxRudderDeg = 80.0f,
        kPropellerRadiusMeter = 2.5f
    };

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

    public bool IsPropellerUnderWater => Object3DPropellerAxis.position.y <= 2.5f;

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

    /// <summary>
    /// degree per second ship rotates around gc to xyz axis.
    /// </summary>
    [SerializeField] Vector3 angularSpeedInAirDeg;

    private Vector3 lastPosition;

    /// <summary>
    /// Ship Movement Vector
    /// </summary>
    public Vector3 Vector => transform.position - lastPosition;
    public float Knots => Vector.magnitude * KTS_TO_MPS;

    // Use this for initialization
    void Start()
    {
        for (int sec = 0; sec < 600; sec++)
        {
            velocityMPS += VtDt(airDrag, kMass, transform.forward * TargetThrustN(currentBell) / kMass, velocityMPS);
        }
        lastPosition = transform.position;
    }

    LosAngelsClassFlightII UpdateVelocityAndBuyonancy()
    {
        // ship forward and astern position in world space meter.
        Vector3 shipForward = transform.position + transform.forward * kLengthMeter * 0.5f;
        Vector3 shipAstern = transform.position - transform.forward * kLengthMeter * 0.5f;

        // divide ships in each cell to calculate gravity and buyonancy.
        var dividedPos =
        from z in Enumerable.Range((int)(-kLengthMeter / 2), (int)(kLengthMeter / 2))
        from y in Enumerable.Range(-5, 5)
        select transform.position + transform.forward * z + transform.up * y;

        // ballast affects power change by how percentage ship under water.
        var affectingBallast = dividedPos.Select(pos => pos.y <= 0 ? ballastAirMPS2 : 0).Average();

        // gravity vs buyonancy result
        Vector3 g = Vector3.up * -(gravity - affectingBallast);

        // thrust available if only propeller under water
        Vector3 thrust = IsPropellerUnderWater ? transform.forward * thrustN / kMass : new Vector3();

        // Velocity update
        if (transform.position.y > 0) // ship in the air
        {
            velocityMPS += VtDt(airDrag, kMass, thrust, velocityMPS) * dt;
        }
        else // under water
        {
            velocityMPS += VtDt(waterDrag, kMass, thrust, velocityMPS) * dt;
        }
        velocityMPS.y += g.y * dt;

        //// rotate with gravity ////
        
        // TBD: still not sure about the best solution.
        Vector3 shipVector = shipForward - shipAstern;

        // the point ship is on the surface.
        Vector3 x0 = shipAstern;
        Vector3 n = Vector3.up; // normal of surface
        Vector3 m = (shipForward - shipAstern).normalized; // velocity;
        float h = n.magnitude; // = (n,x) where x is surface point we want to calc.
        Vector3 surfacePoint = x0 + ((h - Vector3.Dot(n,x0)) / Vector3.Dot(n,m)) * m;

        if (shipForward.y > 0 && shipAstern.y <= 0) {
            // forward fall with gravity
            angularSpeedInAirDeg.x += OmegaRad(gravity, (shipForward - surfacePoint).magnitude) * Mathf.Rad2Deg * dt;
            // astern float with buyonancy
            angularSpeedInAirDeg.x += OmegaRad(gravity - affectingBallast, (shipAstern - surfacePoint).magnitude) * Mathf.Rad2Deg * dt;
        }
        // astern fall
        else if (shipForward.y <= 0 && shipAstern.y > 0)
        {
            // astern fall with gravity
            angularSpeedInAirDeg.x -= OmegaRad(gravity, (shipAstern - surfacePoint).magnitude) * Mathf.Rad2Deg * dt;
            // forward float with buyonancy
            angularSpeedInAirDeg.x -= OmegaRad(gravity - affectingBallast, (shipForward - surfacePoint).magnitude) * Mathf.Rad2Deg * dt;
        }

        return this;
    }

    LosAngelsClassFlightII UpdatePosition() {
        transform.position += velocityMPS * dt;
        return this;
    }

    LosAngelsClassFlightII UpdateOrientaion()
    {
        // yaw and pitch
        // somehow input yaw also inputs pitch so placeholder for now.
        Vector3 surfacePowerDeg = new Vector3(angleAileronDeg, angleRudderDeg, 0.0f);

        transform.rotation *= Quaternion.Euler(((angularSpeedDeg + surfacePowerDeg + angularSpeedInAirDeg) * Mathf.Deg2Rad).Radt() * dt);

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
            { Bell.FlankAhead, 1.5f * kMass },
            { Bell.FullAhead, 1.2f * kMass },
            { Bell.HalfAhead, 0.9f * kMass },
            { Bell.SlowAhead, 0.6f * kMass },
            { Bell.DeadSlowAhead, 0.3f * kMass },
            { Bell.Stop, 0.0f },
            { Bell.DeadSlowAstern, -0.3f * kMass },
            { Bell.SlowAstern, -0.6f * kMass },
            { Bell.HalfAstern, -0.9f * kMass },
            { Bell.FullAstern, -1.2f * kMass },
            { Bell.FlankAstern, -1.5f * kMass }
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
        const float pitchUp = -1.0f;
        const float pitchDown = +1.0f;
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
            //{ KeyCode.W, () => ChangeAndLimitPitchAngularSpeed(pitchDown * kSurfaceChangeRate * dt) },
            // S: Pitch Up
            //{ KeyCode.S, () => ChangeAndLimitPitchAngularSpeed(pitchUp * kSurfaceChangeRate * dt) },
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
        thrustN += TargetValueVector(TargetThrustN(currentBell), thrustN, kMass, kMass) * dt;
        // PID aileron to target pitch
        truePitch = transform.TruePitch();
        targetAileronDeg = -Math.Clamp(aileronController.run(truePitch, targetPitchDeg), -40.0f, +40.0f);
        angleAileronDeg += TargetValueVector(targetAileronDeg, angleAileronDeg, 5.0f, 10.0f) * dt;

        (velocityMPS, angularSpeedDeg) = ShipBehaviour.UpdateVPAR(
            GetComponent<Rigidbody>(),
            transform, Object3DPropellerAxis, spec, velocityMPS, angularSpeedDeg,
            thrustN, ballastAirMPS2, Object3DPropellerAxis.position.y < 0, angleAileronDeg, angleRudderDeg);

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