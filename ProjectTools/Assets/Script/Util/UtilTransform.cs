using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ETModel
{
    public class UtilTransform
    {
        public static Vector2 WorldToScreenPointProjected( Camera camera, Vector3 worldPos )
        {
            if (camera == null) return Vector2.zero;
            Vector3 camNormal = camera.transform.forward;
            Vector3 vectorFromCam = worldPos - camera.transform.position;
            float camNormDot = Vector3.Dot( camNormal, vectorFromCam.normalized);
            if ( camNormDot <= 0 )
            {
                float camDot = Vector3.Dot(camNormal, vectorFromCam);
                Vector3 proj = ( camNormal * camDot * 1.01f );
                worldPos = camera.transform.position + ( vectorFromCam - proj );
            }
            return RectTransformUtility.WorldToScreenPoint( camera, worldPos );
        }

    }
}
