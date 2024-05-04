using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LosAngelsClassFlightII : MonoBehaviour
{
    [SerializeField] public GameObject mk48;
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
        if (!GetComponent<ShipBehaviour>().IsPlayer)
            return;

        if (Input.GetKeyUp(KeyCode.F))
        {
            Main.torpedos.Add(
                Instantiate(mk48, transform.position + transform.forward * GetComponent<ShipSpec>().kLengthMeter * 0.5f, transform.rotation)
                );
            if (Main.torpedos.Last().TryGetComponent(out TorpedoBehaviour tb)) { 
                tb.HandOff(Main.NPCs.Last(), Main.clientPlayer)
                  .Invoke(transform.position + transform.forward * GetComponent<ShipSpec>().kLengthMeter * 0.5f);
                Main.MainCamera.GetComponent<MainCamera>().Target = Main.torpedos.Last();
            }
        }
    }
}