using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static StaticMath;

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

    [SerializeField] Vector3 LOS;
    [SerializeField] Vector2 LOSAzEl;
    [SerializeField] float range;
    [SerializeField] Vector3 Vown;
    [SerializeField] Vector3 Vtarget;
    [SerializeField] Vector3 Vrelative;
    [SerializeField] float Ne = 3; // 有効航法定数
    [SerializeField] float N;
    [SerializeField] Vector3 Omega;
    [SerializeField] Vector3 ownForward;
    [SerializeField] Vector3 ownForwardHeadingPitchRoll;

    // Update is called once per frame
    void Update()
    {
        transform.position += transform.forward * 55 * KTS_TO_MPS * dt;

        if (Target == null)
            return;

        // reference https://qiita.com/oshin_game/items/98374999774e0312b8fa

        LOS = Target.transform.position - transform.position; // LOSベクトル
        range = LOS.magnitude; // ターゲットとの距離
        LOSAzEl = new Vector2(
                Mathf.Atan2(LOS.x, LOS.z) * Mathf.Rad2Deg,
                Mathf.Atan2(LOS.y, Mathf.Sqrt(LOS.x * LOS.x + LOS.z * LOS.z)) * Mathf.Rad2Deg
            ); // LOSベクトルの角度

        Vown = OwnLastPosition - transform.position; // ミサイル速度
        Vtarget = TargetLastPosition - OwnLastPosition; // ターゲット速度
        Vrelative = Vown - Vtarget; // 相対速度

        N = Ne * Vrelative.magnitude * range / Vector3.Dot(Vown, LOS); // 航法定数

        // 指令角速度を計算し、ローカル系に変換
        Omega = N * Vector3.Cross(Vrelative, LOS) / (range * range);
        transform.rotation *= Quaternion.Euler(Omega * dt);

        ownForward = transform.forward;
        ownForwardHeadingPitchRoll = new Vector3(transform.eulerAngles.y, transform.eulerAngles.x.TruePitchDeg(), transform.eulerAngles.z.TruePitchDeg());
    }
}
