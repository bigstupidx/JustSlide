using UnityEngine;
using System.Collections;
using System;

#if EASY_MOBILE
using EasyMobile;
#endif

namespace SgLib
{
    public class ScreenshotSharer : MonoBehaviour
    {
        public enum SharedImageType
        {
            PNG,
            GIF,
            Both
        }

        [Header("Check to disable sharing")]
        public bool disableSharing = false;

        [Header("Sharing Config")]
        public SharedImageType sharedImageFormat = SharedImageType.Both;
        [Tooltip("Any instances of [score] will be replaced by the actual score achieved in the last game, [AppName] will be replaced by the app name declared in AppInfo")]
        [TextArea(3, 3)]
        public string shareMessage = "Awesome! I've just scored [score] in [AppName]! [#AppName]";
        public string gifFilename = "animated_screenshot";
        public string pngFilename = "screenshot";

        [Header("GIF Settings")]
        [Tooltip("Enable this to automatically set GIF height based on the specified width and the screen aspect ratio")]
        public bool gifAutoHeight = true;
        public int gifWidth = 320;
        public int gifHeight = 480;
        [Range(1, 30), Tooltip("Frame per second")]
        public int gifFps = 15;
        [Range(0.1f, 30f), Tooltip("GIF length in seconds")]
        public float gifLength = 3f;
        [Tooltip("0: loop forver; -1: loop disabled; >0: loop a set number of times")]
        public int gifLoop = 0;
        [Range(1, 100)]
        public int gifQuality = 80;
        [Tooltip("Priority of the GIF generating thread")]
        public System.Threading.ThreadPriority gifThreadPriority = System.Threading.ThreadPriority.BelowNormal;

        [Header("Giphy Credentials - leave both empty to use Giphy Beta key")]
        public string giphyUsername;
        public string giphyApiKey;
        [Tooltip("Comma-delimited tags to use when uploading GIF to Giphy")]
        public string giphyUploadTags = "unity, mobile, game";

        public static ScreenshotSharer Instance { get; private set; }

        public Texture2D CapturedScreenshot { get; private set; }

        #if UNITY_ANDROID
        RenderTexture tempRT;
        #endif

        #if EASY_MOBILE
        Recorder recorder;

        public AnimatedClip RecordedClip { get; private set; }
        #endif

        void Awake()
        {
            if (Instance)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        void OnEnable()
        {
            GameManager.GameStateChanged += GameManager_GameStateChanged;
        }

        void OnDisable()
        {
            GameManager.GameStateChanged -= GameManager_GameStateChanged;
        }

        void OnDestroy()
        {
            #if EASY_MOBILE
            if (RecordedClip != null)
            {
                RecordedClip.Dispose();
                RecordedClip = null;
            }
            #endif
        }

        void GameManager_GameStateChanged(GameState newState, GameState oldState)
        {
            #if EASY_MOBILE
            if (newState == GameState.Playing && oldState == GameState.Prepare)
            {
                if (PremiumFeaturesManager.Instance.enablePremiumFeatures && !disableSharing && (sharedImageFormat == SharedImageType.GIF || sharedImageFormat == SharedImageType.Both))
                {
                    if (RecordedClip != null)
                    {
                        RecordedClip.Dispose();
                    }

                    recorder = Camera.main.GetComponent<Recorder>();

                    if (recorder == null)
                    {
                        recorder = Camera.main.gameObject.AddComponent<Recorder>();
                        recorder.Setup(gifAutoHeight, gifWidth, gifHeight, gifFps, Mathf.RoundToInt(gifLength));
                    }
                    recorder.Record();
                }
            }
            else if (newState == GameState.PreGameOver)
            {
                if (PremiumFeaturesManager.Instance.enablePremiumFeatures && !disableSharing && (sharedImageFormat == SharedImageType.PNG || sharedImageFormat == SharedImageType.Both))
                {
                    StartCoroutine(CRCaptureScreenshot());
                }
            }
            else if (newState == GameState.GameOver)
            {
                StartCoroutine(CRProcessGameOverMedia(0.5f));
            }
            #endif
        }

        #if EASY_MOBILE
        IEnumerator CRProcessGameOverMedia(float delay)
        {
            if (delay > 0)
                yield return new WaitForSeconds(delay);

            if (recorder != null)
            {
                RecordedClip = recorder.Stop();
            }

            
#if UNITY_ANDROID
            if (PremiumFeaturesManager.Instance.enablePremiumFeatures && !disableSharing && (sharedImageFormat == SharedImageType.PNG || sharedImageFormat == SharedImageType.Both))
            {
                if (tempRT != null)
                {
                    RenderTextureToScreenshot(tempRT);
                    RenderTexture.ReleaseTemporary(tempRT);
                    tempRT = null;
                }
                else
                {
                    Debug.Log("Something wrong happened: no render texture was captured. Please check.");
                }
            }
            #endif
        }
        #endif

        IEnumerator CRCaptureScreenshot()
        {
            // Wait for right timing to take screenshot
            yield return new WaitForEndOfFrame();

            #if UNITY_ANDROID
            // Temporarily render the camera content to our screenshotRenderTexture.
            // Later we'll share the screenshot from this rendertexture.
            if (tempRT == null)
                tempRT = RenderTexture.GetTemporary(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            else
                tempRT.DiscardContents();

            RenderTexture lastRT = Camera.main.targetTexture;
            Camera.main.targetTexture = tempRT;
            Camera.main.Render();
            Camera.main.targetTexture = lastRT;
            #else
            if (CapturedScreenshot == null)
                CapturedScreenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);

            CapturedScreenshot.ReadPixels(new Rect(0, 0, CapturedScreenshot.width, CapturedScreenshot.height), 0, 0);
            CapturedScreenshot.Apply();
            #endif
        }

        void RenderTextureToScreenshot(RenderTexture rt)
        {
            // Read the rendertexture contents
            RenderTexture.active = rt;

            if (CapturedScreenshot == null)
                CapturedScreenshot = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);

            CapturedScreenshot.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            CapturedScreenshot.Apply();

            RenderTexture.active = null;
        }
    }
}
