using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
    private Vector3 lastPosition;

    PID aileronController = new PID(5.0f, 1.5f, 0);
    PID rudderController = new PID(5.0f, 1.5f, 0);

    /// <summary>
    /// target Pitch Degree controled by PID
    /// </summary>
    [SerializeField] public float targetPitchDeg = 0;
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
        lastPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (Main.offsetForReset.magnitude > 0) { return; }

        UserControl();
        SurfaceControl();

        (velocityMPS, angularSpeedDeg) = UpdateVPAR(
            GetComponent<Rigidbody>(),
            transform, object3DPropellerAxis, Spec, velocityMPS, angularSpeedDeg,
            thrustN, ballastAirMPS2, object3DPropellerAxis.position.y < 0, angleAileronDeg, angleRudderDeg);

        AnimateModel();
        EndFrameJob();
    }

    public void SurfaceControl() {
        // control Thrust to target value, 
        thrustN += TargetValueVector(TargetThrustN(Spec, currentBell), thrustN, Spec.kThrustChangeRateNPerSec, Spec.kThrustChangeRateNPerSec) * dt;
        // PID aileron to target pitch
        targetAileronDeg = -Math.Clamp(aileronController.run(transform.TruePitchDeg(), targetPitchDeg), -40.0f, +40.0f);
        angleAileronDeg += TargetValueVector(targetAileronDeg, angleAileronDeg, 5.0f, 10.0f) * dt;
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
        trueX += transform.position.x - lastPosition.x;
        trueZ += transform.position.z - lastPosition.z;

        // Update LastPosition
        lastPosition = transform.position;

        if (!IsPlayer) { return; }

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

    public void UserControl()
    {
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
                aileronController.reset();
            } },
            // S: Pitch Up
            { KeyCode.S, () => {
                targetPitchDeg = Mathf.Clamp(targetPitchDeg + kDegUnit, -Spec.kMaxPitchDeg, Spec.kMaxPitchDeg);
                aileronController.reset();
            } },
            // A: Yaw Left
            { KeyCode.A, () => {
                targetRudderDeg = Mathf.Clamp(targetRudderDeg - kDegUnit, -Spec.kMaxRudderDeg, Spec.kMaxRudderDeg);
                rudderController.reset();
            } },
            // D: Yaw Right
            { KeyCode.D, () => {
                targetRudderDeg = Mathf.Clamp(targetRudderDeg + kDegUnit, -Spec.kMaxRudderDeg, Spec.kMaxRudderDeg);
                rudderController.reset();
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
        float max = 400.0f * spec.kMassKg;
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
        bool calcBuyonancyRotation = true;
        if (calcBuyonancyRotation) // its also broken though
        {
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
        }
        else // this truly mess things more than expected.
        {
            var forceRad =
                devz
                .Select(z =>
                {
                    rb.mass = spec.kMassKg;
                    Vector3 pos = ship.position + ship.forward * z;
                    float g = pos.y < 0 ? gravity - ballastAirMPS2 : gravity;
                    float force = -g * (spec.kMassKg / devz.Count());
                    rb.AddForceAtPosition(ship.forward * z, Vector3.up * force);
                    return 0;
                })
                .Sum();
        }

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

        // Velocity update
        var rho = waterRho;
        var mu = waterMu;
        var reynolds = Reynolds(rho, velocity.magnitude, spec.kLengthMeter, mu);
        velocity += VtDt(reynolds, spec.kMassKg, dVg + dVThrust, velocity) * dt;

        // yaw and pitch
        Vector3 surfacePowerDeg = new Vector3(angleAileronDeg, angleRudderDeg, 0.0f);

        // position and rotation update
        Vector3 position = ship.position + velocity * dt;
        Quaternion rotation = ship.rotation * Quaternion.Euler(surfacePowerDeg * dt + angular * dt);

        // Limit physics don't go crazy
        // Eventually this is not a phycsics simulatin aimed.
        rotation = Quaternion.Euler(
            rotation.eulerAngles.x > 270 ? Mathf.Clamp(rotation.eulerAngles.x, 345, 360) :
            rotation.eulerAngles.x < 90 ? Mathf.Clamp(rotation.eulerAngles.x, 0, 15) : 0,
            rotation.eulerAngles.y,
            Mathf.Clamp(rotation.eulerAngles.y, -15f, 15f));
        angular.x = Mathf.Clamp(angular.x, -15f, 15f);

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
