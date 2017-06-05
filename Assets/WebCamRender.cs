using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WebCamRender : MonoBehaviour {

	WebCamTexture tex;

	public MeshRenderer meshRenderer;

	// Use this for initialization
	void Start () {

		WebCamDevice[] devices = WebCamTexture.devices;
		
		tex = new WebCamTexture (devices[0].name,1920, 1080);

		meshRenderer = GetComponent<MeshRenderer> ();
		meshRenderer.material.mainTexture = tex;
		tex.Play ();
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
