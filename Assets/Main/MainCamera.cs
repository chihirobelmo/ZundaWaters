using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCamera : MonoBehaviour {

    [SerializeField] public Material theSkyBox;
    [SerializeField] public Material theWaterBox;
    [SerializeField] public Light theSunLight;
    private float CameraDistanceFromPlayer_meter = 200;
    private Vector3 CameraPosFromPlayer = new Vector3(45,45,45);

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
        transform.position = Main.clientPlayer.transform.position
            + new Vector3(
            Mathf.Cos(Mathf.Deg2Rad * CameraPosFromPlayer.x),
            Mathf.Sin(Mathf.Deg2Rad * CameraPosFromPlayer.y),
            Mathf.Sin(Mathf.Deg2Rad * CameraPosFromPlayer.x)).normalized
            * CameraDistanceFromPlayer_meter;
        transform.LookAt(Main.clientPlayer.transform.position);

        if (Input.GetKey(KeyCode.Mouse1))
        {
            CameraPosFromPlayer.y += Input.GetAxis("Mouse Y") * Time.deltaTime * (1f / 0.016f);
            CameraPosFromPlayer.x += Input.GetAxis("Mouse X") * Time.deltaTime * (1f / 0.016f);
            CameraPosFromPlayer.y = (CameraPosFromPlayer.y > 60) ? 60 : (CameraPosFromPlayer.y < -60) ? -60 : CameraPosFromPlayer.y;
            CameraPosFromPlayer.x = CameraPosFromPlayer.x > 360 ? 360 - CameraPosFromPlayer.x : CameraPosFromPlayer.x < 0 ? CameraPosFromPlayer.x + 360 : CameraPosFromPlayer.x;
        }
        CameraDistanceFromPlayer_meter += Input.mouseScrollDelta.y * (CameraDistanceFromPlayer_meter / 20.0f);
        CameraDistanceFromPlayer_meter = CameraDistanceFromPlayer_meter < 65 ? 65 : CameraDistanceFromPlayer_meter > 500 ? 500 : CameraDistanceFromPlayer_meter;

        theSunLight.GetComponent<Light>().intensity = transform.position.y < 0 ? 1 : 1;
        theSunLight.GetComponent<Light>().color = transform.position.y < 0 ? new Color(0,0.2f,1.0f) : Color.white;
        RenderSettings.skybox = transform.position.y < 0 ? theWaterBox : theSkyBox;
    }
}
