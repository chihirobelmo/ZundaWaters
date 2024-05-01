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

        //Animation();
        //EndFrameJob();
    }

    /// <summary>
    /// Return Vecloty in m/s, Position in meter, and Angular in degree, and Rotation i nquateonion.
    /// </summary>
    public static Tuple<Vector3, Vector3> UpdateVPAR(
        Rigidbody rb, Transform ship, Transform propeller, ShipSpec spec, Vector3 velocity, Vector3 angular, 
        float thrustN, float ballastAirMPS2, bool isPropellerUnderWater, float angleAileronDeg, float angleRudderDeg)
    {
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
        velocity += VtDt(ship.position.y > 0 ? airDrag : waterDrag, spec.kMassKg, g + thrust / spec.kMassKg, velocity) * dt;

        // yaw and pitch
        Vector3 surfacePowerDeg = new Vector3(angleAileronDeg, angleRudderDeg, 0.0f);

        // position and rotation update
        Vector3 position = ship.position + velocity * dt;
        Quaternion rotation = ship.rotation * Quaternion.Euler(surfacePowerDeg * dt);

        // Stabilize Roll which might be game-ish idea but needed for player.
        rotation = Quaternion.Euler(rotation.eulerAngles.x, rotation.eulerAngles.y, 0.0f);

        // update ship position and rotation.
        ship.position = position;
        ship.rotation = rotation;

        //// rotate with gravity ////

        //// ship forward and astern position in world space meter.
        //                _________________________
        // ship astern <- | gc4 | gc3 | gc2 | gc1 | -> ship forward
        //                -------------------------
        Vector3 gc1 = ship.position + ship.forward * spec.kLengthMeter * 0.25f; // gc of forward half of forward half
        Vector3 gc4 = ship.position - ship.forward * spec.kLengthMeter * 0.25f; // gc of astern half of astern half

        // consider each gc has ballast sphere around its position.
        var ballastSphere =
            from x in Enumerable.Range((int)(-spec.kRadiusMeter), (int)(+spec.kRadiusMeter))
            from y in Enumerable.Range((int)(-spec.kRadiusMeter), (int)(+spec.kRadiusMeter))
            from z in Enumerable.Range((int)(-spec.kRadiusMeter), (int)(+spec.kRadiusMeter))
            select new Vector3(x, y, z);

        // each ballast gravity and buyonancy
        var ballastGravityGC1 = ballastSphere.Select(pos => (gc1 + pos).y <= 0 ? ballastAirMPS2 : 0).Average();
        var ballastGravityGC4 = ballastSphere.Select(pos => (gc4 + pos).y <= 0 ? ballastAirMPS2 : 0).Average();

        // ship force
        float significant = 1000f;
        long force1 = (long)(significant * (gc1 - ship.position).magnitude * spec.kMassKg * 0.50f * -(gravity - ballastGravityGC1));
        long force4 = (long)(significant * (gc4 - ship.position).magnitude * spec.kMassKg * 0.50f * -(gravity - ballastGravityGC4));

        // merge forces
        long entireForce = force1 + force4;
        if (force1 != 0 && force4 != 0)
        {
            // interior division point of gc1 and gc4
            float n = 1 / force1;
            float m = 1 / force4;
            Vector3 forcePoint = ship.position + ship.forward * force1 / (force1 + force4);

            // ship rotation
            bool isAsternHeavy = (forcePoint - (ship.position - ship.forward * spec.kLengthMeter * 0.5f)).magnitude < spec.kLengthMeter * 0.5f;
            float omegaRad = (isAsternHeavy ? +1 : -1) * (entireForce / spec.kMassKg) * (forcePoint - ship.position).magnitude / significant;

            ship.Rotate(new Vector3(omegaRad * Mathf.Rad2Deg * dt, 0, 0));
        }
        else if (force1 != 0)
        {
            Vector3 forcePoint = gc1; 
            
            float omegaRad = -1 * (entireForce / spec.kMassKg) * (forcePoint - ship.position).magnitude / significant;
            ship.Rotate(new Vector3(omegaRad * Mathf.Rad2Deg * dt, 0, 0));
        }
        else if (force4 != 0)
        {
            Vector3 forcePoint = gc4;

            float omegaRad = +1 * (entireForce / spec.kMassKg) * (forcePoint - ship.position).magnitude / significant;
            ship.Rotate(new Vector3(omegaRad * Mathf.Rad2Deg * dt, 0, 0));
        }

        return Tuple.Create(velocity, rotation.eulerAngles);
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
