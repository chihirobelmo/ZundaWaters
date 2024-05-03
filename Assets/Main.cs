using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour {

    [SerializeField] public GameObject player;
    [SerializeField] public GameObject mfdpanel1;
    [SerializeField] public GameObject mfdpanel2;
    [SerializeField] public Camera mainCamera;

    static public GameObject clientPlayer;
    static public GameObject clientMfdPanel1;
    static public GameObject clientMfdPanel2;
    static public Camera MainCamera { get; set; }
    static public List<GameObject> NPCs;
    static public List<GameObject> torpedos = new List<GameObject>();

    static public Vector3 playerPosition;
    static public bool dontUpdate = false;

    public static float timeScale = 1.0f;

    // Use this for initialization

    static string GetSha256Hash(SHA256 shaHash, string input)
    {
        // Convert the input string to a byte array and compute the hash.
        byte[] data = shaHash.ComputeHash(Encoding.UTF8.GetBytes(input));

        // Create a new Stringbuilder to collect the bytes
        // and create a string.
        StringBuilder sBuilder = new StringBuilder();

        // Loop through each byte of the hashed data 
        // and format each one as a hexadecimal string.
        for (int i = 0; i < data.Length; i++)
        {
            sBuilder.Append(data[i].ToString("x2"));
        }

        // Return the hexadecimal string.
        return sBuilder.ToString();
    }

    void Start ()
    {
        clientPlayer = Instantiate(player);
        clientMfdPanel1 = Instantiate(mfdpanel1);
        clientMfdPanel2 = Instantiate(mfdpanel2);
        MainCamera = Instantiate(mainCamera);

        clientPlayer.GetComponent<ShipBehaviour>().IsPlayer = true;

        // Instansiate AI randomly
        NPCs = Enumerable.Range(0, UnityEngine.Random.Range(1, 2))
            .ToList()
            .Select(x => Instantiate(player))
            .ToList()
            .Select(x =>
            {
                float range = UnityEngine.Random.Range(800, 1200);
                float bearing = UnityEngine.Random.Range(270, 90) * Mathf.Deg2Rad;
                x.transform.position =
                player.transform.position + new Vector3(
                    range * Mathf.Cos(bearing),
                    UnityEngine.Random.Range(-25, 0),
                    range * Mathf.Sin(bearing));
                return x;
            })
            .ToList();
    }

    void KeyDown()
    {
        // KeyUP
        new Dictionary<KeyCode, System.Action> {
            // ]: Thrust Uo
            { KeyCode.RightBracket, () => { Mathf.Clamp(timeScale *= 2.0f, 1.0f, 128.0f); } },
            // [: Thrust Down
            { KeyCode.LeftBracket, () => { Mathf.Clamp(timeScale /= 2.0f, 1.0f, 128.0f); } },
        }
        .ToList()
        .Select(x => { if (Input.GetKeyUp(x.Key)) x.Value(); return 0; })
        .Sum();
    }
	
	// Update is called once per frame
	void Update ()
    {
        KeyDown();
    }

    private void LateUpdate()
    {
        NotifyFloatingPointResetTiming();
    }

    public static void NotifyFloatingPointResetTiming()
    {
        // floating point origin reset
        if (clientPlayer.transform.position.x >= 256)
        {
            ResetFloatingPointOrigin(new Vector3(-256f * 2.0f, 0, 0));
        }
        if (clientPlayer.transform.position.z >= 256)
        {
            ResetFloatingPointOrigin(new Vector3(0, 0, -256f * 2.0f));
        }
        if (clientPlayer.transform.position.x <= -256)
        {
            ResetFloatingPointOrigin(new Vector3(+256f * 2.0f, 0, 0));
        }
        if (clientPlayer.transform.position.z <= -256)
        {
            ResetFloatingPointOrigin(new Vector3(0, 0, +256f * 2.0f));
        }
    }

    /// <summary>
    /// Reset the floating point origin
    /// </summary>
    public static void ResetFloatingPointOrigin(Vector3 offsetForReset)
    {
        /*
         Q: How do games like KSP overcome the floating point precision limit

         https://www.reddit.com/r/Unity3D/comments/nozmk6/how_do_games_like_ksp_overcome_the_floating_point/

         They held a talk at Unite 2013. It's quite old, but I don't think the implementation has changed.
         TLDR; They use a technique called floating origin.
         They store the position of objects internally in double precision,
         but the transforms are not necessarily at the same position.
         When the player has moved a specific distance (like 1km) they move everything back 1km.
         This way the player is now at the world origin again. They do this for everything.
         */

        dontUpdate = true;
        // revert everything to orign
        NPCs.ForEach(x => {
            x.GetComponent<ShipBehaviour>().LastPosition += offsetForReset;
            x.transform.position += offsetForReset;
        });
        torpedos.ForEach(x => {
            x.GetComponent<Mk48Test>().OwnLastPosition += offsetForReset;
            x.transform.position += offsetForReset;
        });
        clientPlayer.GetComponent<ShipBehaviour>().LastPosition += offsetForReset;
        clientPlayer.transform.position += offsetForReset;
        offsetForReset = new Vector3(0, 0, 0);
        dontUpdate = false;
    }
}
