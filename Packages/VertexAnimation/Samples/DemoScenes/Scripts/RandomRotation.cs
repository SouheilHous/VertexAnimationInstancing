using System.Collections;
using UnityEngine;

public class SmoothRandomRotation : MonoBehaviour
{
    public GameObject objectToRotate;
    public AnimationCurve accelerationCurve; // Animation curve to control acceleration and deceleration
    public float maxRotationSpeed = 10f; // Maximum rotation speed in degrees per second
    public float minRotationDuration = 5f; // Minimum duration for rotation
    public float maxRotationDuration = 10f; // Maximum duration for rotation
    public float minStopDuration = 5f; // Minimum duration for stopping
    public float maxStopDuration = 10f; // Maximum duration for stopping

    void Start()
    {
        if (objectToRotate == null)
        {
            objectToRotate = this.gameObject;
        }

        StartCoroutine(RotationCycle());
    }

    IEnumerator RotationCycle()
    {
        while (true)
        {
            // Rotate with acceleration and deceleration
            float rotationDuration = Random.Range(minRotationDuration, maxRotationDuration);
            yield return StartCoroutine(RotateWithAccelerationAndDeceleration(rotationDuration));

            // Stop for a random duration
            float stopDuration = Random.Range(minStopDuration, maxStopDuration);
            yield return new WaitForSeconds(stopDuration);
        }
    }

    IEnumerator RotateWithAccelerationAndDeceleration(float duration)
    {
        float halfDuration = duration / 2f;
        float elapsedTime = 0f;

        // Accelerate phase
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / halfDuration;
            float curveValue = accelerationCurve.Evaluate(normalizedTime);
            float currentSpeed = maxRotationSpeed * curveValue;

            objectToRotate.transform.eulerAngles += new Vector3(0, currentSpeed * Time.deltaTime, 0);

            yield return null;
        }

        elapsedTime = 0f;

        // Decelerate phase
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / halfDuration;
            float curveValue = accelerationCurve.Evaluate(1f - normalizedTime);
            float currentSpeed = maxRotationSpeed * curveValue;

            objectToRotate.transform.eulerAngles += new Vector3(0, currentSpeed * Time.deltaTime, 0);

            yield return null;
        }
    }
}
