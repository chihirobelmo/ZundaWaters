using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour {

    [SerializeField] public GameObject player;
    [SerializeField] public GameObject mfd;
    [SerializeField] public Canvas canvas;
    [SerializeField] public Camera mainCamera;
    [SerializeField] public Camera mfdCamera;

    static public GameObject clientPlayer;
    static public GameObject clientMfd;
    static public Canvas clientCanvas;
    static public Camera clientMfdCamera;

    // Use this for initialization
    void Start ()
    {
        clientPlayer = Instantiate(player);
        clientMfd = Instantiate(mfd);
        clientCanvas = Instantiate(canvas);
        clientMfdCamera = Instantiate(mfdCamera);
    }
	
	// Update is called once per frame
	void Update ()
    {
        clientMfdCamera.transform.position = new Vector3(0, 100, 0);
        clientMfd.transform.position = clientMfdCamera.transform.position + mainCamera.transform.forward * 1.0f; ;
        clientMfd.transform.LookAt(clientMfdCamera.transform.position);
        clientMfd.transform.rotation *= Quaternion.Euler(90, 0, 0);
        clientMfdCamera.transform.LookAt(clientMfd.transform.position);

        var ri = clientCanvas.transform.GetChild(0).GetComponent<RectTransform>();
        var rt = canvas.GetComponent<RectTransform>();
        var size = Screen.height < 1024 ? Screen.height : 1024;
        ri.sizeDelta = new Vector2(size * 1.0f, size);
        ri.transform.position = new Vector3(-rt.position.x + ri.sizeDelta.x * 0.5f, -rt.position.y + ri.sizeDelta.y * 0.5f, 0.0f);
    }
}
