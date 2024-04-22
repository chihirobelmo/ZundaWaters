using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Behaviour : MonoBehaviour {

    float KTS_TO_MPS = 1.94384f;
    float speed_kts = 5.0f;
    float pitch_deg = 0;
    Vector3 velocity_kts = new Vector3(0, 0, 0);
    Vector3 angularSpeed_rad = new Vector3(0, 0, 0);

    void OnSpeedChange() { velocity_kts = transform.forward * speed_kts; }
    void OnUpdatePosition() { transform.position += velocity_kts * KTS_TO_MPS * Time.deltaTime; }
    void OnUpdateOrientation()
    {
        transform.rotation *= Quaternion.Euler(angularSpeed_rad * Time.deltaTime);
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, -angularSpeed_rad.y);
        if (transform.eulerAngles.x > 180.0f && transform.eulerAngles.x < 330.0f)
        {
            if (angularSpeed_rad.x < 0)
                angularSpeed_rad.x += 30.0f * Time.deltaTime;
        }
        if (transform.eulerAngles.x < 180.0f && transform.eulerAngles.x > 30.0f)
        {
            if (angularSpeed_rad.x > 0)
                angularSpeed_rad.x -= 30.0f * Time.deltaTime;
        }
    }

    float CalcPitch(Transform transform)
    {
        Matrix4x4 M = transform.worldToLocalMatrix;
        return Mathf.Atan2(-M.m10, Mathf.Sqrt(M.m01 * M.m01 + M.m00 * M.m00));
    }

    /*
    roll = atan2(M[1][2], M[1][1])
    pitch = atan2(-M[1][0], sqrt(M[0][1] * M[0][1] + M[0][0] * M[0][0]))
    yaw = atan2(M[2][0], M[0][0])
    */

    void ChangeAndLimitSpeed(float add)
    {
        speed_kts += add;
        speed_kts = speed_kts > 30.0f ? 30.0f : speed_kts < -5.0f ? -5.0f : speed_kts;
    }

    void ChangeAndLimitYawAngularSpeed(float add)
    {
        angularSpeed_rad.y += add;
        angularSpeed_rad.y = angularSpeed_rad.y > 30.0f ? 30.0f : angularSpeed_rad.y < -30.0f ? -30.0f : angularSpeed_rad.y;
    }

    void ChangeAndLimitPitch(float add)
    {
        angularSpeed_rad.x += add;
        angularSpeed_rad.x = angularSpeed_rad.x > 10.0f ? 10.0f : angularSpeed_rad.x < -10.0f ? -10.0f : angularSpeed_rad.x;
    }

    // Use this for initialization
    void Start()
    {
        velocity_kts = transform.forward * speed_kts;
    }

    // Update is called once per frame
    void Update()
    {
        // rotate propeller
        transform.GetChild(0).GetChild(0).RotateAroundLocal(new Vector3(0, 0, 1), -1 * speed_kts / 80.0f);
        transform.GetChild(0).GetChild(8).RotateAroundLocal(new Vector3(0, 0, 1), -1 * speed_kts / 80.0f);

        OnUpdatePosition();
        OnSpeedChange();
        OnUpdateOrientation();

        new Dictionary<KeyCode, System.Action> {
            { KeyCode.Q, () => { ChangeAndLimitSpeed(+5.0f * Time.deltaTime); } },
            { KeyCode.Z, () => { ChangeAndLimitSpeed(-5.0f * Time.deltaTime); } },
            { KeyCode.A, () => { ChangeAndLimitYawAngularSpeed(-30.0f * Time.deltaTime); } },
            { KeyCode.D, () => { ChangeAndLimitYawAngularSpeed(+30.0f * Time.deltaTime); } },
            { KeyCode.W, () => { ChangeAndLimitPitch(+30.0f * Time.deltaTime); } },
            { KeyCode.S, () => { ChangeAndLimitPitch(-30.0f * Time.deltaTime); } },
        }
        .ToList()
        .ForEach(x => { if (Input.GetKey(x.Key)) x.Value(); });
    }

    private void OnCollisionEnter(Collision collision)
    {
        speed_kts = 0.0f;
    }

    private void OnCollisionStay(Collision collision)
    {
    }

    private void OnCollisionExit(Collision collision)
    {
    }
}
