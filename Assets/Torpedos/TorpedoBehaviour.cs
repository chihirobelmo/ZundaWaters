using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static StaticMath;
using static UnityEditor.PlayerSettings;

public class TorpedoBehaviour : MonoBehaviour
{
    public GameObject Target { get; set; }
    public GameObject Shooter { get; set; }
    public Vector3 TargetLastPosition { get; set; }

    public Vector3 OwnLastPosition { get; set; }

    public delegate bool ControlCommand();

    public delegate ControlCommand FireCommand(Vector3 launchPosition);

    public FireCommand HandOff(GameObject target, GameObject shooter)
    {
        Target = target;
        Shooter = shooter;

        predictedTargetVector = (target.transform.position - shooter.transform.position).normalized;

        // Fire Delegate
        return (Vector3 launchPosition) => 
        {
            transform.position = launchPosition;

            if (target == null || Shooter == null) return () => false;
            if (phase != Phase.AtTube) return () => false;

            phase = Phase.Fire;

            return () => false;
        };
    }

    // Start is called before the first frame update
    void Start()
    {
        launchTime = Time.time;
    }

    [SerializeField] public float launchTime;
    [SerializeField] public float searchStartTime;
    [SerializeField] public Phase phase = Phase.AtTube;
    [SerializeField] public float Ne = 3; // 有効航法定数
    [SerializeField] public Snake snakeState;
    [SerializeField] public Vector3 predictedTargetVector;
    [SerializeField] public Vector3 currentVector;

    public enum Phase
    {
        AtStock,
        AtTube,
        Fire,
        PurePursuit,
        Transit,
        Search,
        Terminal,
        Hit,
        Destroyed,
    }

    public enum Snake
    {
        LeftStart,
        RightStart,
        Left,
        Right
    }

    // Update is called once per frame
    void Update()
    {
        OwnLastPosition = transform.position;

        if (phase != Phase.AtStock && phase != Phase.AtTube)
        {
            transform.position += transform.forward * 55 * KTS_TO_MPS * dt;
        }

        if (Target == null || Shooter == null)
            return;

        var targetVelocity = Target.TryGetComponent<ShipBehaviour>(out ShipBehaviour sb) ?
            sb.velocityMPS * dt :
            Target.transform.position - TargetLastPosition + /*in case missile get 2 frame while target get 1 frame only*/Target.transform.forward * ESP;

        Vector3 los = Target.transform.position - transform.position;
        Vector2 losAngle = new Vector2(
            Mathf.Atan2(los.x, los.z) * Mathf.Rad2Deg,
            Mathf.Atan2(los.y, Mathf.Sqrt(los.z * los.z + los.z * los.x) * Mathf.Rad2Deg)
            );

        currentVector = transform.forward;

        new Dictionary<Phase, Action>
        {
            { Phase.AtStock, () => { } },
            { Phase.AtTube, () => { } },
            // During Fire Phase, torapedo has to take distance from shooter, to avoid collision
            { Phase.Fire, () => {
                if ((transform.position - Shooter.transform.position).magnitude > 500)
                    phase = Phase.PurePursuit;
            } },
            // During PurePursuit Phase, torapedo has to look at target predicted position
            { Phase.PurePursuit, () => {

                // placeholder: TBD to look at target predicted position or heading
                if (Vector3.Dot(currentVector, predictedTargetVector) > 0.95f) {
                    phase = Phase.Transit;
                }

                // placeholder: TBD to look at target predicted position or heading
                transform.rotation = InterceptGuidance.SimplifiedPN(
                    Ne, Target, TargetLastPosition,
                    transform.position, transform.rotation,
                    (OwnLastPosition - transform.position).magnitude, /*turn rate*/10.0f);
            } },
            // During Transit phase torpedo has to close to target predicted position before active seeker.
            { Phase.Transit, () => {
                
                // placeholder: TBD to start search adter transit certain range pre-input before fire.
                // placeholder: TBD to look at target predicted position or heading
                if (los.magnitude < 3000)
                {
                    searchStartTime = Time.time;
                    snakeState = Snake.Left;
                    phase = Phase.Search;
                }

            } },
            // During Search Phase, torpedo has to search target antil acquire, to acquire target torpedo will snake or circle.
            { Phase.Search, () => {
                
                // placeholder: TBD to acquire target by sonar active or passive.
                // placeholder: TBD to look at target acquired position or heading
                if (los.magnitude < 1000 && Vector3.Dot(currentVector, los.normalized) >= 0.75f)
                {
                    phase = Phase.Terminal;
                }

                if (snakeState == Snake.LeftStart)
                {
                    if (Vector3.Dot(currentVector, predictedTargetVector) >= 0.75f)
                        snakeState = Snake.Left;
                    transform.rotation *= Quaternion.Euler(0, /*turn rate*/-10 * dt, 0);
                }
                else if (snakeState == Snake.RightStart)
                {
                    if (Vector3.Dot(currentVector, predictedTargetVector) >= 0.75f)
                        snakeState = Snake.Right;
                    transform.rotation *= Quaternion.Euler(0, /*turn rate*/+10 * dt, 0);
                }
                else if (snakeState == Snake.Left)
                {
                    if (Vector3.Dot(currentVector, predictedTargetVector) < 0.75f) 
                        snakeState = Snake.RightStart;
                    transform.rotation *= Quaternion.Euler(0, /*turn rate*/-10 * dt, 0);
                }
                else if (snakeState == Snake.Right)
                {
                    if (Vector3.Dot(currentVector, predictedTargetVector) < 0.75f)
                        snakeState = Snake.LeftStart;
                    transform.rotation *= Quaternion.Euler(0, /*turn rate*/+10 * dt, 0);
                }

            } },
            // Terminal Phase, torpedo has to PN target.
            { Phase.Terminal, () => {
                
                // placeholder: TBD if passive and range unknown, maybe use predicted range.
                transform.rotation = InterceptGuidance.QuadraticPN(
                    Ne, Target, TargetLastPosition,
                    transform.position, transform.rotation,
                    (OwnLastPosition - transform.position).magnitude, /*turn rate*/10.0f);

            } },
            { Phase.Hit, () => { } },
            { Phase.Destroyed, () => { }}
        }
        .GetValueOrDefault(phase, () => { }).Invoke();
    }

    public void OnCollisionEnter(Collision collision)
    {
        Debug.Log(this + " Collides " + collision.gameObject + " as " + collision);

        Action destroyThis = () =>
        {
            Destroy(this, 0.016f);
            GetComponent<Renderer>().enabled = false;
        };

        if (Time.time - launchTime < 1 && collision.gameObject == Shooter)
            return;

        if (collision.gameObject.TryGetComponent(out ShipBehaviour sb))
            sb.Damage();

        destroyThis();
    }
}
