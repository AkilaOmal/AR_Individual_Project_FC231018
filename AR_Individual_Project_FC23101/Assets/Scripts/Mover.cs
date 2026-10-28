using UnityEngine;
using System.Collections;

public class Mover : MonoBehaviour
{
    public IEnumerator MoveTo(Vector3 target, float duration)
    {
        Vector3 start = transform.position; float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }
    }
}