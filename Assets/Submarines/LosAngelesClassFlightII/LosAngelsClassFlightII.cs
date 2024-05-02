using UnityEngine;

public class LosAngelsClassFlightII : MonoBehaviour
{
    Transform Object3DPropellerBlades => transform.GetChild(0).GetChild(0);
    Transform Object3DPropellerAxis => transform.GetChild(0).GetChild(8);

    // Use this for initialization
    void Start()
    {
        GetComponent<ShipBehaviour>().object3DPropellerAxis = Object3DPropellerAxis;
        GetComponent<ShipBehaviour>().object3DPropellerBlades = Object3DPropellerBlades;
    }

    // Update is called once per frame
    void Update()
    {
    }
}