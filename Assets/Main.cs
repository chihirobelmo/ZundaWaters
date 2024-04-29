using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour {

    [SerializeField] public GameObject player;
    [SerializeField] public GameObject mfdpanel;
    [SerializeField] public GameObject mfdScreen1;
    [SerializeField] public Canvas canvas;
    [SerializeField] public Camera mainCamera;
    [SerializeField] public Camera mfdCamera;
    [SerializeField] public RenderTexture renderTextureScreen1;

    static public GameObject clientPlayer;
    static public GameObject clientMfdPanel;
    static public GameObject clientScreen1;
    static public Canvas clientCanvas;
    static public Camera clientMfdCamera;

    // Use this for initialization
    void Start ()
    {
        clientPlayer = Instantiate(player);
        clientMfdPanel = Instantiate(mfdpanel);
        clientCanvas = Instantiate(canvas);
        clientMfdCamera = Instantiate(mfdCamera);
        clientScreen1 = Instantiate(mfdScreen1);

        clientCanvas.enabled = false;
    }
	
	// Update is called once per frame
	void Update ()
    {
        clientMfdCamera.transform.position = new Vector3(0, 0, 0);
        clientMfdPanel.transform.position = clientMfdCamera.transform.position + mainCamera.transform.forward * 1.0f;
        clientMfdCamera.transform.LookAt(clientMfdPanel.transform.position);
        clientMfdPanel.transform.LookAt(clientMfdCamera.transform.position);
        clientMfdPanel.transform.rotation *= Quaternion.Euler(90, 0, 0);
        clientScreen1.transform.position = clientMfdPanel.transform.position;
        clientScreen1.transform.rotation = clientMfdPanel.transform.rotation * Quaternion.Euler(0,0,0);

        if (Input.GetKeyUp(KeyCode.Alpha1))
        {
            clientCanvas.enabled = !clientCanvas.enabled;
        }

        var ri = clientCanvas.transform.GetChild(0).GetComponent<RectTransform>();
        var rt = canvas.GetComponent<RectTransform>();
        var size = Screen.height * (2.0f / 3.0f) < 720 ? Screen.height : Screen.height * (2.0f / 3.0f);
        ri.sizeDelta = new Vector2(size * 1.0f, size);
        ri.transform.position = new Vector3(-rt.position.x + ri.sizeDelta.x * 0.5f, -rt.position.y + ri.sizeDelta.y * 0.5f, 0.0f);

        Texture2D tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        
        IEnumerable<Vector2> pix =
        from x in Enumerable.Range(0, 255)
        from y in Enumerable.Range(0, 255)
        select new Vector2(x, y);

        pix.Aggregate(tex, (acc, p) =>
        {
            /// initialize the texture with a black color
            acc.SetPixel((int)p.x, (int)p.y, new Color32(0x00, 0x00, 0x00, 0xff));
            return acc;
        });
        DrawLine(tex, 0, 0, 255, 255, 1, new Color32(0xff, 0xff, 0x00, 0xff));
        tex.Apply();

        Graphics.CopyTexture(tex, renderTextureScreen1);
    }

    static void DrawLine(Texture2D a_Texture, int x1, int y1, int x2, int y2, int lineWidth, Color a_Color)
    {
        float xPix = x1;
        float yPix = y1;

        float width = x2 - x1;
        float height = y2 - y1;
        float length = Mathf.Abs(width);
        if (Mathf.Abs(height) > length) length = Mathf.Abs(height);
        int intLength = (int)length;
        float dx = width / (float)length;
        float dy = height / (float)length;
        for (int i = 0; i <= intLength; i++)
        {
            a_Texture.SetPixel((int)xPix, (int)yPix, a_Color);

            xPix += dx;
            yPix += dy;
        }
    }
}
