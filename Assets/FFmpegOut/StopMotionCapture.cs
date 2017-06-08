﻿using UnityEngine;

namespace FFmpegOut
{
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("FFmpegOut/Camera Capture")]
    public class StopMotionCapture : MonoBehaviour
    {
        #region Editable properties
		public Camera captureCamera;
        [SerializeField] bool _setResolution = true;
        [SerializeField] int _width = 1280;
        [SerializeField] int _height = 720;
        [SerializeField] int _frameRate = 30;
        [SerializeField] bool _allowSlowDown = true;
        [SerializeField] FFmpegPipe.Codec _codec;
        [SerializeField] float _startTime = 0;
        [SerializeField] float _recordLength = 5;

        #endregion

        #region Private members

        [SerializeField, HideInInspector] Shader _shader;
        Material _material;

        FFmpegPipe _pipe;
        float _elapsed;
		bool _isRecording;

        RenderTexture _tempTarget;
        GameObject _tempBlitter;

        static int _activePipeCount;

		private byte[] currentTextureData;

		private int currentSnapshotCount = 0;

		private bool queuedSnapshot = false;
        #endregion

        #region MonoBehavior functions

        void OnValidate()
        {
            _recordLength = Mathf.Max(_recordLength, 0.01f);
        }

        void OnEnable()
        {
            if (!FFmpegConfig.CheckAvailable)
            {
                Debug.LogError(
                    "ffmpeg.exe is missing. " +
                    "Please refer to the installation instruction. " +
                    "https://github.com/keijiro/FFmpegOut"
                );
                enabled = false;
            }
        }

        void OnDisable()
        {
            if (_pipe != null) ClosePipe();
        }

        void OnDestroy()
        {
            if (_pipe != null) ClosePipe();
        }

        void Start()
        {
            _material = new Material(_shader);
        }

		public void StartRecording()
		{
			if (!_isRecording)
			{
				_isRecording = true;
				_elapsed = 0.0f;
				_startTime = Mathf.Max(Time.time, 0);
				if (_pipe == null)
				{
					OpenPipe ();
				}
			}
		}

		public void StopRecording()
		{
			if (_isRecording)
			{
				queuedSnapshot = false;
				_isRecording = false;
				_elapsed = 0.0f;
				if (_pipe != null)
					ClosePipe ();
			}
		}

		public void TakeSnapshot()
		{
			if (!_isRecording)
			{
				StartRecording ();
			}

			//queuedSnapshot = true;

			var renderTexture = RenderTexture.GetTemporary(1920, 1080);

			captureCamera.targetTexture = renderTexture;
			captureCamera.Render ();

			RenderTexture.active = renderTexture;
			Texture2D virtualPhoto = new Texture2D(renderTexture.width,renderTexture.height, TextureFormat.RGB24, false);
			virtualPhoto.ReadPixels( new Rect(0, 0, renderTexture.width,renderTexture.height), 0, 0);
			RenderTexture.active 		= null; 
			captureCamera.targetTexture = null;

			// Encode texture into PNG
			//byte[] bytes = virtualPhoto.GetRawTextureData();//virtualPhoto.EncodeToPNG();
			_pipe.Write(virtualPhoto.GetRawTextureData());

			Destroy(virtualPhoto);
			RenderTexture.ReleaseTemporary(renderTexture);
		}

        void Update()
        {
//			if (_isRecording)
//			{
//				_elapsed += Time.deltaTime;
//
//				if ( (Time.time - _startTime) >= _recordLength)
//				{
//					if (_pipe != null)
//						ClosePipe ();
//				}
//			}
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (_pipe != null && queuedSnapshot)
            {
				queuedSnapshot = false;

                var tempRT = RenderTexture.GetTemporary(source.width, source.height);
                Graphics.Blit(source, tempRT);

                var tempTex = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);
                tempTex.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0, false);
                tempTex.Apply();

                _pipe.Write(tempTex.GetRawTextureData());

				//currentTextureData = tempTex.GetRawTextureData ();

                Destroy(tempTex);
                RenderTexture.ReleaseTemporary(tempRT);

				currentSnapshotCount++;
            }

            Graphics.Blit(source, destination);
        }

        #endregion

        #region Private methods

        void OpenPipe()
        {
            if (_pipe != null) return;

            var camera = GetComponent<Camera>();
            var width = _width;
            var height = _height;

            // Apply the screen resolution settings.
            if (_setResolution)
            {
                _tempTarget = RenderTexture.GetTemporary(width, height);
                camera.targetTexture = _tempTarget;
                _tempBlitter = Blitter.CreateGameObject(camera);
            }
            else
            {
                width = camera.pixelWidth;
                height = camera.pixelHeight;
            }

            // Open an output stream.
            _pipe = new FFmpegPipe(name, width, height, _frameRate, _codec);
            _activePipeCount++;

            // Change the application frame rate on the first pipe.
            if (_activePipeCount == 1)
            {
                if (_allowSlowDown)
                    Time.captureFramerate = _frameRate;
                else
                    Application.targetFrameRate = _frameRate;
            }

            Debug.Log("Capture started (" + _pipe.Filename + ")");
        }

        void ClosePipe()
        {
            var camera = GetComponent<Camera>();

            // Destroy the blitter object.
            if (_tempBlitter != null)
            {
                Destroy(_tempBlitter);
                _tempBlitter = null;
            }

            // Release the temporary render target.
            if (_tempTarget != null && _tempTarget == camera.targetTexture)
            {
                camera.targetTexture = null;
                RenderTexture.ReleaseTemporary(_tempTarget);
                _tempTarget = null;
            }

            // Close the output stream.
            if (_pipe != null)
            {
                Debug.Log("Capture ended (" + _pipe.Filename + ")");

                _pipe.Close();
                _activePipeCount--;

                if (!string.IsNullOrEmpty(_pipe.Error))
                {
                    Debug.LogWarning(
                        "ffmpeg returned with a warning or an error message. " +
                        "See the following lines for details:\n" + _pipe.Error
                    );
                }

                _pipe = null;

                // Reset the application frame rate on the last pipe.
                if (_activePipeCount == 0)
                {
                    if (_allowSlowDown)
                        Time.captureFramerate = 0;
                    else
                        Application.targetFrameRate = -1;
                }
            }
        }

        #endregion
    }
}
