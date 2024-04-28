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
            CameraPosFromPlayer.y += Input.GetAxis("Mouse Y") * Time.deltaTime * 400.0f;
            CameraPosFromPlayer.x += Input.GetAxis("Mouse X") * Time.deltaTime * 400.0f;
            CameraPosFromPlayer.y = (CameraPosFromPlayer.y > 60) ? 60 : (CameraPosFromPlayer.y < -60) ? -60 : CameraPosFromPlayer.y;
            CameraPosFromPlayer.x = CameraPosFromPlayer.x > 360 ? 360 - CameraPosFromPlayer.x : CameraPosFromPlayer.x < 0 ? CameraPosFromPlayer.x + 360 : CameraPosFromPlayer.x;
        }
        CameraDistanceFromPlayer_meter += Input.mouseScrollDelta.y * (CameraDistanceFromPlayer_meter / 20.0f);
        CameraDistanceFromPlayer_meter = CameraDistanceFromPlayer_meter < 65 ? 65 : CameraDistanceFromPlayer_meter > 500 ? 500 : CameraDistanceFromPlayer_meter;

        theSunLight.GetComponent<Light>().intensity = LinearSaturate(1 + transform.position.y / 300, 0, 300);
        theSunLight.GetComponent<Light>().color = new Color(
            /*R*/ LinearSaturate(1 + transform.position.y / 1, 0, 1),
            /*G*/ LinearSaturate(1 + transform.position.y / 100, 0, 1),
            /*B*/ LinearSaturate(1 + transform.position.y / 300, 1, 1)
            );
        RenderSettings.skybox.SetColor("_SkyTint", new Color(
            /*R*/ LinearSaturate(1 + transform.position.y / 0.1f, 1, 1),
            /*G*/ LinearSaturate(1 + transform.position.y / 0.1f, 1, 1),
            /*B*/ LinearSaturate(1 + transform.position.y / 0.1f, 0, 1)
            ));
        RenderSettings.skybox.SetFloat("_AtmosphereThickness", LinearSaturate(0.5f - 4.5f * transform.position.y / 0.1f, 0.5f, 5));
        RenderSettings.skybox.SetFloat("_SunSize", LinearSaturate(0.04f + 0.04f * transform.position.y / 0.1f, 0, 1));
    }

    public static float LinearSaturate(System.Func<float, float> f, float x, float minx, float maxx){
        float ans = f(x);
        float max = f(maxx);
        float min = f(minx);
        return maxx > minx ? (ans > max ? max : ans < min ? min : ans) : (ans > min ? min : ans < max ? max: ans);
    }
    public static float LinearSaturate(float y, float min, float max)
    {
        return max > min ? (y > max ? max : y < min ? min : y) : (y > min ? min : y < max ? max : y);
    }
}
