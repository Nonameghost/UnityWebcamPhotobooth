using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenshotCapture : MonoBehaviour {

	public Camera captureCamera;
	public RenderTexture renderTexture;

	public void TakeScreenshot()
	{
		StartCoroutine (CaptureScreenshot ());
	}

	IEnumerator CaptureScreenshot()
	{
		yield return new WaitForEndOfFrame ();

		captureCamera.targetTexture = renderTexture;
		captureCamera.Render ();

		RenderTexture.active = renderTexture;
		Texture2D virtualPhoto = new Texture2D(renderTexture.width,renderTexture.height, TextureFormat.RGB24, false);
		virtualPhoto.ReadPixels( new Rect(0, 0, renderTexture.width,renderTexture.height), 0, 0);
		RenderTexture.active 		= null; 
		captureCamera.targetTexture = null;

		// Encode texture into PNG
		byte[] bytes = virtualPhoto.EncodeToPNG();
		Object.Destroy(virtualPhoto);

		File.WriteAllBytes(Application.dataPath + "/Screenshot.png", bytes);

	}
}
