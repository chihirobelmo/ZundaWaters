using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using static StaticMath;
using static UnityEngine.GraphicsBuffer;

public class Mk48Test : MonoBehaviour
{
    public GameObject Target { get; set; }
    public GameObject Shooter { get; set; }
    public Vector3 TargetLastPosition { get; set; }

    public Vector3 OwnLastPosition { get; set; }

    public void Fire(GameObject target, GameObject shooter)
    {
        Target = target;
        Shooter = shooter;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    [SerializeField] public Phase phase = Phase.Track;
    [SerializeField] public float Ne = 3; // 有効航法定数

    public enum Phase
    {
        Search,
        Acquire,
        Track,
        Engage,
        Terminal
    }

    // Update is called once per frame
    void Update()
    {
        OwnLastPosition = transform.position;
        transform.position += transform.forward * 55 * KTS_TO_MPS * dt;

        if (Target == null)
            return;

        var targetVelocity = Target.TryGetComponent<ShipBehaviour>(out ShipBehaviour sb) ?
            sb.velocityMPS * dt :
            Target.transform.position - TargetLastPosition + /*in case missile get 2 frame while target get 1 frame only*/Target.transform.forward * ESP;

        transform.rotation = InterceptGuidance.QuadraticPN(Ne, Target, TargetLastPosition, transform.position, transform.rotation, (OwnLastPosition - transform.position).magnitude, 10.0f);
    }
}
