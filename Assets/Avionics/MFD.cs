using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MFD : MonoBehaviour
{
    [SerializeField] public GameObject mfdScreen1;
    [SerializeField] public RenderTexture renderTexture;
    [SerializeField] public Camera mfdCamera;
    [SerializeField] public Canvas canvas;
    [SerializeField] public float mfdposoffset;
    [SerializeField] public Vector3 mfdcampos;

    GameObject clientScreen1;
    Camera clientMfdCamera;
    Canvas clientCanvas;

    // Start is called before the first frame update
    void Start()
    {
        clientScreen1 = Instantiate(mfdScreen1);
        clientMfdCamera = Instantiate(mfdCamera);
        clientCanvas = Instantiate(canvas);

        clientCanvas.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        // camera positions
        clientMfdCamera.transform.position = mfdcampos;
        transform.position = clientMfdCamera.transform.position + Main.clientMainCamera.transform.forward * 1.0f;
        clientMfdCamera.transform.LookAt(transform.position);
        transform.LookAt(clientMfdCamera.transform.position);
        transform.rotation *= Quaternion.Euler(90, 0, 0);
        transform.position += transform.right * mfdposoffset;

        // move the screen to the panel's position
        clientScreen1.transform.position = transform.position;
        clientScreen1.transform.rotation = transform.rotation * Quaternion.Euler(0, 0, 0);

        if (Input.GetKeyUp(mfdcampos.x < 0 ? KeyCode.F1 : KeyCode.F2))
        {
            clientCanvas.enabled = !clientCanvas.enabled;
        }

        UpdateCanvas(clientCanvas, mfdcampos.x);

        // render teture
        return;
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
    void UpdateCanvas(Canvas c, float side)
    {
        var ri = c.transform.GetChild(0).GetComponent<RectTransform>(); // raw image
        var cr = c.GetComponent<RectTransform>(); // canvas rect
        var size = Screen.height * (2.0f / 3.0f) < 720 ? Screen.height : Screen.height * (2.0f / 3.0f);

        ri.sizeDelta = new Vector2(size * 1.0f, size);
        ri.transform.position = new Vector3(
            cr.position.x + cr.sizeDelta.x * 0.5f * side + ri.sizeDelta.x * 0.5f * -side,
            cr.position.y - cr.sizeDelta.y * 0.5f + ri.sizeDelta.y * 0.5f, 0.0f);
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
