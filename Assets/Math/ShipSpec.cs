using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipSpec : MonoBehaviour
{
    // for physics
    [SerializeField] public long kMassKg;
    [SerializeField] public long kThrustChangeRateNPerSec;
    [SerializeField] public float kSurfaceChangeRateDegPerSec;
    [SerializeField] public float kBallastChangeRateMeterPerSec2;
    [SerializeField] public float kMaxBallastAirMeterPerSec2;
    [SerializeField] public float kMinBallastAirMeterPerSec2;
    [SerializeField] public float kLengthMeter;
    [SerializeField] public float kRadiusMeter;
    [SerializeField] public float kMaxPitchDeg;
    [SerializeField] public float kMaxAileronDeg;
    [SerializeField] public float kMaxRudderDeg;

    // for animation
    [SerializeField] public float kPropellerRadiusMeter;
    [SerializeField] public float kPropellerRotationCounterClockWise;
    [SerializeField] public float kPropellerRotationRadPerThrustN;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
