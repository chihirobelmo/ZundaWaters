using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    static public Camera clientMainCamera;

    static public Vector3 playerPosition;

    // Use this for initialization
    void Start ()
    {
        clientPlayer = Instantiate(player);
        clientMfdPanel1 = Instantiate(mfdpanel1);
        clientMfdPanel2 = Instantiate(mfdpanel2);
        clientMainCamera = mainCamera;
    }
	
	// Update is called once per frame
	void Update ()
    {
        ResetFloatingPointOrigin();
    }

    static public Vector3 offsetForReset;

    /// <summary>
    /// Reset the floating point origin
    /// </summary>
    void ResetFloatingPointOrigin()
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

        // revert everything to orign
        new List<GameObject> { clientPlayer }.ForEach(x => {
            x.transform.position += offsetForReset;
        });
        offsetForReset = new Vector3(0, 0, 0);
    }
}
