using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utils : MonoBehaviour
{
    public static Vector3 RotatePoint(Vector3 pointToRotate, Vector3 origin, float angleOfRotation)
    {
        //angleOfRotation *= Mathf.Deg2Rad;
        float x = (pointToRotate.x - origin.x) * Mathf.Cos(angleOfRotation);
        x -= (pointToRotate.z - origin.z) * Mathf.Sin(angleOfRotation);
        x += origin.x;
        float y = (pointToRotate.x - origin.x) * Mathf.Sin(angleOfRotation);
        y += (pointToRotate.z - origin.z) * Mathf.Cos(angleOfRotation);
        y += origin.z;
        Vector3 p = new Vector3(x, 0, y);
        return p;
    }
}
