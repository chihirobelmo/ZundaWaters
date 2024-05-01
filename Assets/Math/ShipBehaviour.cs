using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using static StaticMath;

public class ShipBehaviour : MonoBehaviour
{
    public string hashId;
    public struct ShipSpec
    {
        public float kMassKg;
        public float kThrustChangeRateNPerSec;
        public float kSurfaceChangeRateDegPerSec;
        public float kBallastChangeRateMeterPerSec2;
        public float kMaxBallastAirMPS2;
        public float kMinBallastAirMPS2;
        public float kLengthMeter;
        public float kRadiusMeter;
        public float kMaxPitchDeg;
        public float kMaxAileronDeg;
        public float kMaxRudderDeg;
        public float kPropellerRadiusMeter;
    }

    public double trueX = 0.0;
    public double trueZ = 0.0;
    public Bell currentBell = Bell.DeadSlowAhead;

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
    [SerializeField] public Vector3 velocityMPS;
    /// <summary>
    /// degree per second ship rotates around gc to xyz axis.
    /// </summary>
    [SerializeField] public Vector3 angularSpeedDeg;
    /// <summary>
    /// degree per second ship rotates around gc to xyz axis.
    /// </summary>
    [SerializeField] public Vector3 angularSpeedInAirDeg;

    ShipSpec Spec { get; set; }
    Transform Object3DPropellerBlades { get; set; }
    Transform Object3DPropellerAxis { get; set; }

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
    }

    // Update is called once per frame
    void Update()
    {
        //// control Thrust to target value, 
        //thrustN += TargetValueVector(TargetThrustN(currentBell), thrustN, 2.5f, 1.0f) * dt;
        //// PID aileron to target pitch
        //truePitch = transform.TruePitch();
        //targetAileronDeg = -Math.Clamp(aileronController.run(truePitch, targetPitchDeg), -40.0f, +40.0f);
        //angleAileronDeg += TargetValueVector(targetAileronDeg, angleAileronDeg, 5.0f, 10.0f) * dt;

       UpdateVPAR(transform, Object3DPropellerAxis, Spec, velocityMPS, angularSpeedDeg,
            thrustN, ballastAirMPS2, Object3DPropellerAxis.position.y < 0, angleAileronDeg, angleRudderDeg);

        //Animation();
        //EndFrameJob();
    }

    /// <summary>
    /// Return Vecloty in m/s, Position in meter, and Angular in degree, and Rotation i nquateonion.
    /// </summary>
    public static void UpdateVPAR(
        Transform ship, Transform propeller, ShipSpec spec, Vector3 velocity, Vector3 angular, 
        float thrustN, float ballastAirMPS2, bool isPropellerUnderWater, float angleAileronDeg, float angleRudderDeg)
    {
        // ship forward and astern position in world space meter.
        Vector3 shipForward = ship.position + ship.forward * spec.kLengthMeter * 0.5f;
        Vector3 shipAstern = ship.position - ship.forward * spec.kLengthMeter * 0.5f;

        // divide ships in each cell to calculate gravity and buyonancy.
        var dividedPos =
        from z in Enumerable.Range((int)(-spec.kLengthMeter / 2), (int)(+spec.kLengthMeter / 2))
        from y in Enumerable.Range((int)-spec.kRadiusMeter, (int)+spec.kRadiusMeter)
        select ship.position + ship.forward * z + ship.up * y;

        // ballast affects power change by how percentage ship under water.
        var affectingBallast = dividedPos.Select(pos => pos.y <= 0 ? ballastAirMPS2 : 0).Average();

        // gravity vs buyonancy result
        Vector3 g = Vector3.up * -(gravity - affectingBallast);

        // thrust available if only propeller under water
        Vector3 thrust = isPropellerUnderWater ? ship.forward * thrustN : new Vector3();

        // Velocity update
        velocity += VtDt(ship.position.y > 0 ? airDrag : waterDrag, spec.kMassKg, thrust / spec.kMassKg, velocity) * dt;
        velocity.y += g.y;

        // yaw and pitch
        Vector3 surfacePowerDeg = new Vector3(angleAileronDeg, angleRudderDeg, 0.0f);

        // position and rotation update
        Vector3 position = ship.position + velocity * dt;
        Quaternion rotation = ship.rotation * Quaternion.Euler(surfacePowerDeg * Mathf.Deg2Rad * dt);

        // Stabilize Roll which might be game-ish idea but needed for player.
        rotation = Quaternion.Euler(rotation.eulerAngles.x, rotation.eulerAngles.y, 0.0f);

        // update ship position and rotation.
        ship.position = position;
        ship.rotation = rotation;

        //// rotate with gravity ////

        // the point ship is on the surface.
        Vector3 x0 = shipAstern;
        Vector3 n = Vector3.up; // normal of surface
        Vector3 m = (shipForward - shipAstern).normalized; // velocity;
        float h = n.magnitude; // = (n,x) where x is surface point we want to calc.
        Vector3 surfacePoint = x0 + ((h - Vector3.Dot(n, x0)) / Vector3.Dot(n, m)) * m;

        bool frontAboveSurface = shipForward.y > 0 && shipAstern.y <= 0;
        bool asternAboveSurface = shipForward.y <= 0 && shipAstern.y > 0;
        bool bothUnderWaterOrInAir = !frontAboveSurface && !asternAboveSurface;

        float rotationWay = frontAboveSurface ? 1.0f : asternAboveSurface ? -1.0f : 0.0f;

        float fallLength =
            frontAboveSurface ? (shipForward - surfacePoint).magnitude :
            asternAboveSurface ? (shipAstern - surfacePoint).magnitude :
            0.0001f;

        float buyoLength =
            frontAboveSurface ? (shipAstern - surfacePoint).magnitude :
            asternAboveSurface ? (shipForward - surfacePoint).magnitude :
            0.0001f;

        // fall with gravity
        angular.x += rotationWay * OmegaRad(gravity, fallLength);
        // float with buyonancy
        angular.x += rotationWay * OmegaRad(gravity - affectingBallast, buyoLength);
        // should we reset them?
        angular.x = bothUnderWaterOrInAir ? 0.0f : angular.x;

        ship.RotateAround(surfacePoint - ship.position, ship.right, angular.x * Mathf.Rad2Deg * dt);
        //ship.rotation *= Quaternion.Euler(angular * dt);
    }

    static public Bell ChangeBell(Bell bell, int add)
    {
        return (int)bell + add <= (int)Bell.MinInvalid ? Bell.FlankAhead :
               (int)bell + add >= (int)Bell.MaxInvalid ? Bell.FlankAstern :
               bell + add;
    }

    public void UserControl()
    {
        const int thrustUp = -1;
        const int thrustDown = +1;

        const float kDegUnit = 5.0f;
        const float kMps2Unit = 0.2f;

        // KeyUP
        new Dictionary<KeyCode, System.Action> {
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
            { KeyCode.W, () => {
                targetRudderDeg = Mathf.Clamp(targetRudderDeg - kDegUnit, -Spec.kMaxRudderDeg, Spec.kMaxRudderDeg);
                rudderController.reset();
            } },
            // D: Yaw Right
            { KeyCode.S, () => {
                targetRudderDeg = Mathf.Clamp(targetRudderDeg + kDegUnit, -Spec.kMaxRudderDeg, Spec.kMaxRudderDeg);
                rudderController.reset();
            } },
            // E: Ballast more air
            { KeyCode.E, () => {
                targetBallastAirMPS2 = Mathf.Clamp(targetBallastAirMPS2 + kMps2Unit, Spec.kMinBallastAirMPS2, Spec.kMaxBallastAirMPS2);
            } },
            // C: Ballast more water
            { KeyCode.C, () => {
                targetBallastAirMPS2 = Mathf.Clamp(targetBallastAirMPS2 - kMps2Unit, Spec.kMinBallastAirMPS2, Spec.kMaxBallastAirMPS2);
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
}
