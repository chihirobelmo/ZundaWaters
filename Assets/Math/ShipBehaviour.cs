using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static StaticMath;
using static UnityEditor.PlayerSettings;

public class ShipBehaviour : MonoBehaviour
{
    public ShipSpec Spec { get; set; }
    public bool IsPlayer { get; set; }

    [SerializeField] public double trueX = 0.0;
    [SerializeField] public double trueZ = 0.0;
    [SerializeField] public Bell currentBell = Bell.DeadSlowAhead;
    public Vector3 LastPosition { get; set; }

    PID thrustController = new PID(0.1f, 0.0f, 0);
    PID pitchController = new PID(0.1f, 0.0f, 0);
    PID aileronController = new PID(0.1f, 0.0f, 0);
    PID rudderController = new PID(0.1f, 0.0f, 0);

    /// <summary>
    /// target Pitch Degree controled by PID
    /// </summary>
    [SerializeField] public float targetPitchDeg = 0;
    [SerializeField] public float truePitch;
    /// <summary>
    /// target Aileron Degree controled by PID
    /// </summary>
    [SerializeField] public float targetAileronDeg = 0;
    /// <summary>
    /// target Rudder Degree controled by PID
    /// </summary>
    [SerializeField] public float targetRudderDeg = 0;
    /// <summary>
    /// target anti gravity acceleratio controled by PID
    /// </summary>
    [SerializeField] public float targetBallastAirMPS2 = 0;
    /// <summary>
    /// Angle aileron in degree.
    /// </summary>
    [SerializeField] public float angleAileronDeg = 0.0f;
    /// <summary>
    /// Angle rudder in degree.
    /// </summary>
    [SerializeField] public float angleRudderDeg = 0.0f;
    /// <summary>
    /// anti gravity acceleration in m/s^2 only works under the water.
    /// </summary>
    [SerializeField] public float ballastAirMPS2 = 9.8f;
    /// <summary>
    /// thrust made by propeller in N.
    /// </summary>
    [SerializeField] public float thrustN = 7.0f;
    /// <summary>
    /// velocityspeed) in m/s per xyz axis
    /// </summary>
    [SerializeField] public Vector3 velocityMPS = new Vector3();
    /// <summary>
    /// degree per second ship rotates around gc to xyz axis.
    /// </summary>
    [SerializeField] public Vector3 angularSpeedDeg = new Vector3();

    public Transform object3DPropellerBlades;
    public Transform object3DPropellerAxis;


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

    // Start is called before the first frame update
    void Start()
    {
        Spec = GetComponent<ShipSpec>();
        for (int sec = 0; sec < 600; sec++)
        {
            //velocityMPS += VtDt(waterDrag, Spec.kMassKg, transform.forward * TargetThrustN(Spec, currentBell) / Spec.kMassKg, velocityMPS);
        }
        LastPosition = transform.position;

        pitchController = new PID(Spec.kPitchPID.x, Spec.kPitchPID.y, Spec.kPitchPID.z);
        aileronController = new PID(Spec.kAileronPID.x, Spec.kAileronPID.y, Spec.kAileronPID.z);
    }

    // Update is called once per frame
    void Update()
    {
        if (Main.dontUpdate) { return; }

        UserControl();
        SurfaceControl();

        (velocityMPS, angularSpeedDeg) = UpdateVPAR(
            GetComponent<Rigidbody>(),
            transform, object3DPropellerAxis, Spec, velocityMPS, angularSpeedDeg,
            thrustN, ballastAirMPS2, object3DPropellerAxis.position.y < 0, angleAileronDeg, angleRudderDeg);

        AnimateModel();
        EndFrameJob();

        pitchController.SetPID(Spec.kPitchPID.x, Spec.kPitchPID.y, Spec.kPitchPID.z);
        aileronController.SetPID(Spec.kAileronPID.x, Spec.kAileronPID.y, Spec.kAileronPID.z);
    }

    public void SurfaceControl() {

        // control Thrust to target value, 
        thrustN += thrustController.run(thrustN, TargetThrustN(Spec, currentBell), dt) * dt;

        // PID aileron to target pitch
        truePitch = transform.eulerAngles.x.TruePitchDeg();
        // caution, PID does not works for inverse proportion. if minus aileron pitch up, then multiply minus one.
        targetAileronDeg = -pitchController.run(truePitch, targetPitchDeg, dt);
        angleAileronDeg += Math.Clamp(aileronController.run(angleAileronDeg, targetAileronDeg, dt) * dt, -Spec.kSurfaceChangeRateDegPerSec * dt, +Spec.kSurfaceChangeRateDegPerSec * dt);
        angleAileronDeg = Mathf.Clamp(angleAileronDeg, -Spec.kMaxAileronDeg, Spec.kMaxAileronDeg);

        angleRudderDeg += TargetValueVector(targetRudderDeg, angleRudderDeg, 5.0f, 10.0f) * dt;
        angleRudderDeg = Mathf.Clamp(angleRudderDeg, -Spec.kMaxRudderDeg, Spec.kMaxRudderDeg);
    }

    public void AnimateModel()
    {
        new List<Transform> { object3DPropellerBlades, object3DPropellerAxis }
        .ForEach(x => x.Rotate(
            transform.forward, 
            Spec.kPropellerRotationCounterClockWise * ((thrustN / Spec.kMassKg) / Spec.kPropellerRadiusMeter) * Spec.kPropellerRotationRadPerThrustN, 
            Space.World));
    }

    private void EndFrameJob()
    {

        // Update True XY
        trueX += transform.position.x - LastPosition.x;
        trueZ += transform.position.z - LastPosition.z;

        // Update LastPosition
        LastPosition = transform.position;
    }

    public void UserControl()
    {
        if (!IsPlayer) { return; }

        const int thrustUp = -1;
        const int thrustDown = +1;

        const float kDegUnit = 5.0f;
        const float kMps2Unit = 0.2f;

        // KeyUP
        new Dictionary<KeyCode, Action> {
            // Q: Thrust Uo
            { KeyCode.Q, () => { currentBell = ChangeBell(currentBell, thrustUp); } },
            // Z: Thrust Down
            { KeyCode.Z, () => { currentBell = ChangeBell(currentBell, thrustDown); } },
            // W: Pitch Down
            { KeyCode.W, () => {
                targetPitchDeg = Mathf.Clamp(targetPitchDeg - kDegUnit, -Spec.kMaxPitchDeg, Spec.kMaxPitchDeg);
            } },
            // S: Pitch Up
            { KeyCode.S, () => {
                targetPitchDeg = Mathf.Clamp(targetPitchDeg + kDegUnit, -Spec.kMaxPitchDeg, Spec.kMaxPitchDeg);
            } },
            // A: Yaw Left
            { KeyCode.A, () => {
                targetRudderDeg = Mathf.Clamp(targetRudderDeg - kDegUnit, -Spec.kMaxRudderDeg, Spec.kMaxRudderDeg);
            } },
            // D: Yaw Right
            { KeyCode.D, () => {
                targetRudderDeg = Mathf.Clamp(targetRudderDeg + kDegUnit, -Spec.kMaxRudderDeg, Spec.kMaxRudderDeg);
            } },
            // E: Ballast more air
            { KeyCode.E, () => {
                targetBallastAirMPS2 = Mathf.Clamp(targetBallastAirMPS2 + kMps2Unit, Spec.kMinBallastAirMeterPerSec2, Spec.kMaxBallastAirMeterPerSec2);
            } },
            // C: Ballast more water
            { KeyCode.C, () => {
                targetBallastAirMPS2 = Mathf.Clamp(targetBallastAirMPS2 - kMps2Unit, Spec.kMinBallastAirMeterPerSec2, Spec.kMaxBallastAirMeterPerSec2);
            } },
            // X: Reset Yaw/Pitch/Ballast
            { KeyCode.X, () => {
                targetPitchDeg = 0.0f;
                targetRudderDeg = 0.0f;
                targetBallastAirMPS2 = gravity;
            } },
        }
        .ToList()
        .Select(x => { if (Input.GetKeyUp(x.Key)) x.Value(); return 0; })
        .Sum();
    }

    static public float TargetThrustN(ShipSpec spec, Bell bell)
    {
        float max = spec.kMaxThrustPowerMass * spec.kMassKg;
        if (new Dictionary<Bell, float> {
            { Bell.FlankAhead, max * 1.0f },
            { Bell.FullAhead, max * 0.8f },
            { Bell.HalfAhead, max * 0.6f },
            { Bell.SlowAhead, max * 0.4f },
            { Bell.DeadSlowAhead, max * 0.2f },
            { Bell.Stop, 0.0f },
            { Bell.DeadSlowAstern, -max * 0.2f },
            { Bell.SlowAstern, -max * 0.4f },
            { Bell.HalfAstern, -max * 0.6f },
            { Bell.FullAstern, -max * 0.8f },
            { Bell.FlankAstern, -max * 1.0f }
        }.TryGetValue(bell, out float targetThrustN))
        {
            return targetThrustN;
        }
        return 0.0f;
    }

    /// <summary>
    /// Return Vecloty in m/s, Position in meter, and Angular in degree, and Rotation i nquateonion.
    /// </summary>
    public static Tuple<Vector3, Vector3> UpdateVPAR(
        Rigidbody rb, Transform ship, Transform propeller, ShipSpec spec, Vector3 velocity, Vector3 angular, 
        float thrustN, float ballastAirMPS2, bool isPropellerUnderWater, float angleAileronDeg, float angleRudderDeg)
    {
        // divide ships in each cell to calculate gravity and buyonancy.
        List<float> devz = FloatRange(-spec.kLengthMeter * 0.33f, +spec.kLengthMeter * 0.33f, 2);
        List<float> devy = new List<float> { 0 }; // only one cell for now. otherwise ship sinks from surface.

        //List<float> devz = new List<float> { -spec.kLengthMeter * 0.33f, +spec.kLengthMeter * 0.33f };
        //List<int> devy = Enumerable.Range((int)-spec.kRadiusMeter, (int)+spec.kRadiusMeter).ToList();

        var dividedPos =
        from z in devz
        from y in devy
        select ship.position + ship.forward * z + ship.up * y;

        // rotate with each point gravity and buyonancy.
        var forceRad =
            devz
            .Select(z =>
            {
                Vector3 pos = ship.position + ship.forward * z;
                float g = pos.y < 0 ? gravity - ballastAirMPS2 : gravity;
                float force = z * -g * (spec.kMassKg / devz.Count());
                return force * ship.TruePitchDeg().Abs().Cos();
            })
            .Sum();

        // inartia of ship
        float inartiaX = (1.0f / 12.0f) * spec.kMassKg * spec.kLengthMeter * spec.kLengthMeter
            * (1 + 3 * (spec.kRadiusMeter / spec.kLengthMeter) * (spec.kRadiusMeter / spec.kLengthMeter));

        Vector3 buyonancyRotation = new Vector3((float)(forceRad / inartiaX), 0, 0);
        angular += buyonancyRotation * Mathf.Rad2Deg * dt;

        // gravity vs buyonancy
        var forceG =
            dividedPos.Select(pos =>
            {
                float g = pos.y < 0 ? gravity - ballastAirMPS2 : gravity;
                float force = -g * (spec.kMassKg / dividedPos.Count());
                return force;
            })
            .Sum();

        // gravity vs buyonancy result
        Vector3 dVg = Vector3.up * forceG / spec.kMassKg;

        // thrust available if only propeller under water
        Vector3 dVThrust = (isPropellerUnderWater ? ship.forward * thrustN : new Vector3()) / spec.kMassKg;

        // Reynolds number
        var reynolds = Reynolds(ship.position.y < 0 ? waterRho : airRho, velocity.magnitude, spec.kLengthMeter, ship.position.y < 0 ? waterMu : airMu);

        // Velocity update
        velocity += VtDt(reynolds, spec.kMassKg, dVg + dVThrust, velocity) * dt;

        // forward speed and drag power
        float forwardSpeed = velocity.magnitude * Vector3.Dot(velocity, ship.forward);
        float Drag = /*CD*/1.0f * spec.kRadiusMeter * spec.kRadiusMeter * Mathf.PI * waterRho * forwardSpeed * forwardSpeed * 0.5f;

        // yaw and pitch
        float magicMult = 0.1f;
        Vector3 surfacePowerRad = new Vector3(
            magicMult * Mathf.Sin(angleAileronDeg * Mathf.Deg2Rad) * forwardSpeed,
            magicMult * Mathf.Sin(angleRudderDeg * Mathf.Deg2Rad) * forwardSpeed, 0.0f);

        // position and rotation update
        Vector3 position = ship.position + velocity * dt;
        Quaternion rotation = ship.rotation * Quaternion.Euler(surfacePowerRad * dt + angular * dt);

        // Limit physics don't go crazy
        // Eventually this is not a phycsics simulatin aimed.
        rotation = Quaternion.Euler(
            rotation.eulerAngles.x >= 270 ? Mathf.Clamp(rotation.eulerAngles.x, 315, 360) :
            rotation.eulerAngles.x <= 90 ? Mathf.Clamp(rotation.eulerAngles.x, 0, 45) : 0,
            rotation.eulerAngles.y,
            0.0f);
        angular.x = Mathf.Clamp(angular.x, -15f, 15f);

        // need to stop angular by drag
        angular.x = VtDt(reynolds, spec.kMassKg, 0, angular.x * spec.kLengthMeter * 0.5f) * dt / (spec.kLengthMeter * 0.5f);

        // update ship position and rotation.
        ship.position = position;
        ship.rotation = rotation;

        return Tuple.Create(velocity, angular);
    }

    static public Bell ChangeBell(Bell bell, int add)
    {
        return (int)bell + add <= (int)Bell.MinInvalid ? Bell.FlankAhead :
               (int)bell + add >= (int)Bell.MaxInvalid ? Bell.FlankAstern :
               bell + add;
    }
}
