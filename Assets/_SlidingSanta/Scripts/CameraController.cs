using UnityEngine;
using System.Collections;
using UnityStandardAssets_ImageEffects;

namespace SgLib
{
    public class CameraController : MonoBehaviour
    {
        public PlayerController playerController;

        [Header("Shaking Effect")]
        // How long the camera shaking.
        public float shakeDuration = 0.1f;
        // Amplitude of the shake. A larger value shakes the camera harder.
        public float shakeAmount = 0.2f;
        public float decreaseFactor = 0.3f;

        private BlurOptimized blurComp;
        private Vector3 originalPos;
        private float currentShakeDuration;
        private float currentDistance;

        void Start()
        {
            blurComp = GetComponent<BlurOptimized>();
            blurComp.enabled = false;
        }

        // Update is called once per frame
        void LateUpdate()
        {
            if (GameManager.Instance.GameState == GameState.GameOver || GameManager.Instance.GameState == GameState.Prepare)
            {
                return;
            }
            else if (playerController.startRun)
            {
                transform.position += Vector3.back * playerController.turnRightSpeed * Time.deltaTime;
            }
        }

        public void EnableBlur()
        {
            blurComp.enabled = true;
        }

        public void DisableBlur()
        {
            blurComp.enabled = false;
        }

        public void ShakeCamera()
        {
            StartCoroutine(Shake());
        }

        IEnumerator Shake()
        {
            originalPos = transform.position;
            currentShakeDuration = shakeDuration;
            while (currentShakeDuration > 0)
            {
                transform.position = originalPos + Random.insideUnitSphere * shakeAmount;
                currentShakeDuration -= Time.deltaTime * decreaseFactor;
                yield return null;
            }
            transform.position = originalPos;
        }
    }
}
