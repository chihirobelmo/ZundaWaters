﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LosAngelsClassFlightII : MonoBehaviour
{
    // Use this for initialization
    void Start()
    {
        thrust = transform.forward * 1.0f;
        velocityMPS = transform.forward * 5.0f * KTS_TO_MPS;
        lastPosition = transform.position;
    }

    public static float VtPerDt(float k, float m, float a, float v)
    {
        return (-k / m) * (v * v - m * Mathf.Abs(a) / k);
    }

    const float KTS_TO_MPS = 1.94384f;
    const float gravity = 9.8f;
    const float mass = 7000/*ton displacement*/ * 1000/*kg*/ * 2.0f/*assume actual mass has twice displacement*/;
    const float waterDrag = 500000f;
    const float airDrag = 100000f;
    [SerializeField] float ballastAir = 9.8f;
    [SerializeField] Vector3 thrust; // m/s2
    [SerializeField] Vector3 velocityMPS;
    [SerializeField] Vector3 gravityVelocityMPS;
    [SerializeField] Vector3 gravityAngularDeg;
    [SerializeField] Vector3 angularSpeedDeg;
    Vector3 lastPosition;
    Vector3 lastVector;
    Vector3 vector;

    void updateInfo()
    {
        thrust = transform.forward * thrust.magnitude;
        vector = lastPosition - transform.position;
        lastPosition = transform.position;
        lastVector = vector;
    }

    void UpdateVelocity()
    {
        if ((transform.position + transform.forward * -50.0f).y <= 0)
        {
            velocityMPS += new Vector3(
                VtPerDt(waterDrag, mass, thrust.x, velocityMPS.x) * Time.deltaTime,
                VtPerDt(waterDrag, mass, thrust.y, velocityMPS.y) * Time.deltaTime,
                VtPerDt(waterDrag, mass, thrust.z, velocityMPS.z) * Time.deltaTime
                );
        }
        //velocityMPS += (transform.forward * velocityMPS.magnitude - velocityMPS) * Time.deltaTime; // cancel inartia bit by a bit
    }

    void UpdatePosition()
    {
        transform.position += (velocityMPS + gravityVelocityMPS) * Time.deltaTime;
    }

    void CalcGravityAndFloat() {

       // gravity for each cell

       IEnumerable < Vector3 > eachCellOfShip =
           from z in Enumerable.Range(-55,55)
           select transform.position + 
           transform.forward * z;

        int cellCount = eachCellOfShip.Count();

        eachCellOfShip.ToList().ForEach(p =>
        {
            float offset = (p.z - transform.position.z);
            if (p.y > 0)
            { // in air
                gravityVelocityMPS += -Vector3.up * VtPerDt(airDrag, mass, gravity, gravityVelocityMPS.magnitude) * Time.deltaTime * (1.0f / cellCount);
                gravityAngularDeg.x += Mathf.Rad2Deg * (2.0f * Mathf.PI / 60.0f) * gravity * offset * Time.deltaTime * (1.0f / cellCount);
            }
            if (p.y <= 0)
            { // under water
                gravityVelocityMPS += (gravity - ballastAir > 0 ? - Vector3.up : Vector3.up) * VtPerDt(airDrag, mass, Mathf.Abs(gravity - ballastAir), gravityVelocityMPS.magnitude) * Time.deltaTime * (1.0f / cellCount);
                gravityAngularDeg.x += Mathf.Rad2Deg * (2.0f * Mathf.PI / 60.0f) * (gravity - ballastAir) * offset * Time.deltaTime * (1.0f / cellCount);
            }
        });
    }

    void UpdateOrientaion()
    {
        // yaw
        transform.rotation *= Quaternion.Euler((angularSpeedDeg * Mathf.Deg2Rad + gravityAngularDeg * Mathf.Deg2Rad) * Time.deltaTime);
        // roll fixed 0
        transform.rotation *= Quaternion.Euler((new Vector3(0, 0, -transform.eulerAngles.z) * Mathf.Deg2Rad) * Time.deltaTime);
        //transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, 0);
        //transform.GetChild(0).rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, -(vector - lastVector).magnitude * Mathf.Rad2Deg);
    }

    void ChangeAndLimitThrust(float add)
    {
        thrust += thrust.normalized * add;
        thrust = thrust.normalized * (thrust.magnitude > 20.0f ? 20.0f : thrust.magnitude < -5.0f ? -5.0f : thrust.magnitude);
    }

    void ChangeAndLimitYawAngularSpeed(float add)
    {
        angularSpeedDeg.y += add * velocityMPS.magnitude / (1.0f * KTS_TO_MPS);
        angularSpeedDeg.y = angularSpeedDeg.y > 360.0f ? 360.0f : angularSpeedDeg.y < -360.0f ? -360.0f : angularSpeedDeg.y;
    }

    void ChangeAndLimitPitchAngularSpeed(float add)
    {
        angularSpeedDeg.x += add * velocityMPS.magnitude / (1.0f * KTS_TO_MPS);
        angularSpeedDeg.x = angularSpeedDeg.x > 360.0f ? 360.0f : angularSpeedDeg.x < -360.0f ? -360.0f : angularSpeedDeg.x;

        // limit pitch angle
        if (transform.eulerAngles.x > 180.0f && transform.eulerAngles.x < 330.0f)
        {
            if (angularSpeedDeg.x < 0)
                angularSpeedDeg.x += 30.0f * Time.deltaTime;
        }
        if (transform.eulerAngles.x < 180.0f && transform.eulerAngles.x > 30.0f)
        {
            if (angularSpeedDeg.x > 0)
                angularSpeedDeg.x -= 30.0f * Time.deltaTime;
        }
    }

    // Update is called once per frame
    void Update()
    {
        updateInfo();

        // rotate propeller
        transform.GetChild(0).GetChild(0).Rotate(transform.forward, -1 * thrust.magnitude * 40.0f, Space.World);
        transform.GetChild(0).GetChild(8).Rotate(transform.forward, -1 * thrust.magnitude * 40.0f, Space.World);

        UpdateVelocity();
        CalcGravityAndFloat();
        UpdatePosition();
        UpdateOrientaion();

        new Dictionary<KeyCode, System.Action> {
            { KeyCode.Q, () => { ChangeAndLimitThrust(+1.0f * Time.deltaTime); } },
            { KeyCode.Z, () => { ChangeAndLimitThrust(-1.0f * Time.deltaTime); } },
            { KeyCode.A, () => { ChangeAndLimitYawAngularSpeed(-30.0f * Time.deltaTime); } },
            { KeyCode.D, () => { ChangeAndLimitYawAngularSpeed(+30.0f * Time.deltaTime); } },
            { KeyCode.W, () => { ChangeAndLimitPitchAngularSpeed(+30.0f * Time.deltaTime); } },
            { KeyCode.S, () => { ChangeAndLimitPitchAngularSpeed(-30.0f * Time.deltaTime); } },
            { KeyCode.E, () => { ballastAir += ballastAir >= 9.9f ? 9.9f : +0.1f * Time.deltaTime; } },
            { KeyCode.C, () => { ballastAir += ballastAir <= 9.6f ? 9.6f : -0.1f * Time.deltaTime; } },
            { KeyCode.X, () => { angularSpeedDeg *= 0.0f; ballastAir = -9.8f; } },
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