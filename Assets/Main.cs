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

    }
}
