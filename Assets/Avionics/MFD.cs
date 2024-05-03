using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MFD : MonoBehaviour
{
    [SerializeField] public GameObject mfdScreen;
    [SerializeField] public RenderTexture renderTexture;
    [SerializeField] public Camera mfdCamera;
    [SerializeField] public Canvas canvas;
    [SerializeField] public float mfdposoffset;
    [SerializeField] public Vector3 mfdcampos;

    GameObject clientScreen;
    Camera clientMfdCamera;
    Canvas clientCanvas;

    // Start is called before the first frame update
    void Start()
    {
        clientScreen = Instantiate(mfdScreen);
        clientMfdCamera = Instantiate(mfdCamera);
        clientCanvas = Instantiate(canvas);

        clientCanvas.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateCanvasVisibility(clientCanvas, mfdcampos.x < 0 ? KeyCode.F1 : KeyCode.F2);
        UpdateMfdPositionAndRotateCameraToLookAt(transform, clientMfdCamera, mfdcampos, mfdposoffset);
        UpdateScreenPositionAndRotationToFolowMfdPanel(clientScreen, transform);
        UpdateCanvasPositionOnScreen(clientCanvas, mfdcampos.x);
    }

    static void UpdateCanvasVisibility(Canvas canvas, KeyCode key)
    {
        canvas.enabled = Input.GetKeyUp(key) ? !canvas.enabled : canvas.enabled;
    }

    /// <summary>
    /// Camera and MFD Panels are at the MFD Layer.
    /// MFD Panels moves around to make sun light direction change effect.
    /// Camera and MFD Looks at each other
    /// Makes Panels look more realistic with specular change...
    /// </summary>
    /// <param name="screen"></param>
    /// <param name="transform"></param>
    static void UpdateMfdPositionAndRotateCameraToLookAt(Transform transform, Camera clientMfdCamera, Vector3 mfdcampos, float mfdposoffset)
    {
        // camera positions
        clientMfdCamera.transform.position = mfdcampos;
        transform.position = clientMfdCamera.transform.position + Main.MainCamera.transform.forward * 1.0f;
        clientMfdCamera.transform.LookAt(transform.position);
        transform.LookAt(clientMfdCamera.transform.position);
        transform.rotation *= Quaternion.Euler(90, 0, 0);
        transform.position += transform.right * mfdposoffset;
    }

    /// <summary>
    /// Screen follows the MFD Panel
    /// </summary>
    /// <param name="screen"></param>
    /// <param name="transform"></param>
    static void UpdateScreenPositionAndRotationToFolowMfdPanel(GameObject screen, Transform transform)
    {
        // move the screen to the panel's position
        screen.transform.position = transform.position;
        screen.transform.rotation = transform.rotation;
    }

    /// <summary>
    /// With the screen resolution change, it can set the screen position at the proper positions.
    /// MFD1 goes to left bottom and MFD2 goes to right bottom.
    /// </summary>
    /// <param name="c"></param>
    /// <param name="side"></param>
    static void UpdateCanvasPositionOnScreen(Canvas c, float side)
    {
        // raw image
        var rawImage = c.transform.GetChild(0).GetComponent<RectTransform>();
        // canvas rect
        var canvasRect = c.GetComponent<RectTransform>(); 

        // if the screen height below 720p then use the screen height, otherwise use 2/3 of the screen height
        // this makes the MFD scren readable for any resolution.

        var size = Screen.height * (2.0f / 3.0f) < 720 ? Screen.height : Screen.height * (2.0f / 3.0f);

        rawImage.sizeDelta = new Vector2(size * 1.0f, size);
        rawImage.transform.position = new Vector3(
            canvasRect.position.x + canvasRect.sizeDelta.x * 0.5f * side + rawImage.sizeDelta.x * 0.5f * -side,
            canvasRect.position.y - canvasRect.sizeDelta.y * 0.5f + rawImage.sizeDelta.y * 0.5f, 0.0f);
    }

    static void RenderCPUExample(RenderTexture renderTexture)
    {
        // render teture
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

        Graphics.CopyTexture(tex, renderTexture);
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
