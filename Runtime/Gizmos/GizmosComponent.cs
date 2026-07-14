using System;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Gizmos
{

    public class GizmosComponent : MonoBehaviour
    {

        public static readonly string[] Type = new string[] {
            "Cube",
            "Frustum",
            "GUITexture",
            "Icon",
            "Line",
            "Mesh",
            "Ray",
            "Sphere",
            "WireCube",
            "WireMesh",
            "WireSphere",
            "CameraOrthographic",
            "LineExtended",
            "CubeExtended",
            "WireCubeExtended"
        };

        public string type = "Cube";
        public Color color =  Color.red;
        public bool active = true;

        //Cube
        public bool positionIsCenterCube = true;
        public Vector3 cubeCenter = Vector3.zero;
        public Vector3 cubeSize = Vector3.one;
        //Frustrum
        public bool positionIsCenterFrustum = true;
        public Vector3 frustumCenter = Vector3.zero;
        public float fov;
        public float maxRange;
        public float minRange;
        public float aspect;
        //GUITexture
        public Rect screenRect;
        public Texture texture;
        public Material mat = null;
        //Icon
        public bool positionIsCenterIcon = true;
        public Vector3 iconCenter;
        public string iconName;
        public bool allowScaling = true;
        //Line
        public bool useTwoTransforms = false;
        public Vector3 fromV;
        public Vector3 toV;
        public Transform fromTr;
        public Transform toTr;
        //Mesh
        public Mesh mesh;
        public bool transformIsMeshTransform = true;
        public Vector3 meshPosition;
        public Vector3 meshRotation;
        public Vector3 meshScale = Vector3.one;
        public int subMeshIndex = -1;
        //Ray
        public Vector3 fromR;
        public Vector3 directionR;
        //Sphere
        public bool positionIsCenterSphere = true;
        public Vector3 sphereCenter = Vector3.zero;
        public float radiusS = 1f;
        //WireCube
        public bool positionIsCenterWireCube = true;
        public Vector3 wireCubeCenter = Vector3.zero;
        public Vector3 wireCubeSize = Vector3.one;
        //WireMesh
        public Mesh wireMesh;
        public bool transformIsWireMeshTransform = true;
        public Vector3 wireMeshPosition;
        public Vector3 wireMeshRotation;
        public Vector3 wireMeshScale = Vector3.one;
        public int subWireMeshIndex = -1;
        //Sphere
        public bool positionIsCenterWireSphere = true;
        public Vector3 wireSphereCenter = Vector3.zero;
        public float radiusWS = 1f;
        //CameraOrthographic
        public Camera cam;
        public bool drawVertex = false;
        //LineExtended
        public Vector3 startPointLE;
        public Vector3 endPointLE;
        public Transform fromTrLE;
        public Transform toTrLE;
        public float thickness = 0.05f;
        public bool useTwoTransformsLE = false;
        //CubeExtended
        public Vector3 positionCE = Vector3.zero;
        public Vector3 rotationCE = Vector3.zero;
        public Vector3 scaleCE = Vector3.one;
        //WireCubeExtended
        public Vector3 positionWCE = Vector3.zero;
        public Vector3 rotationWCE = Vector3.zero;
        public Vector3 scaleWCE = Vector3.one;


        void OnDrawGizmos()
        {
            if (!active)
                return;

            UnityEngine.Gizmos.color = color;

            switch (type)
            {
                case "Cube":
                    if (!positionIsCenterCube)
                        UnityEngine.Gizmos.DrawCube(cubeCenter, cubeSize);
                    else
                        UnityEngine.Gizmos.DrawCube(this.transform.position, cubeSize);
                    break;
                case "Frustum":
                    if (!positionIsCenterFrustum)
                        UnityEngine.Gizmos.DrawFrustum(frustumCenter, fov, maxRange, minRange, aspect);
                    else
                        UnityEngine.Gizmos.DrawFrustum(this.transform.position, fov, maxRange, minRange, aspect);
                    break;
                case "GUITexture":
                    if(texture != null)
                        UnityEngine.Gizmos.DrawGUITexture(screenRect, texture, mat);
                    break;
                case "Icon":
                    if (!positionIsCenterIcon)
                        UnityEngine.Gizmos.DrawIcon(iconCenter, iconName);
                    else
                        UnityEngine.Gizmos.DrawIcon(this.transform.position, iconName);
                    break;
                case "Line":
                    if (!useTwoTransforms)
                        UnityEngine.Gizmos.DrawLine(fromV, toV);
                    else
                        if(fromTr != null && toTr != null)
                            UnityEngine.Gizmos.DrawLine(fromTr.position, toTr.position);
                    break;
                case "Mesh":
                    if (!transformIsMeshTransform)
                        UnityEngine.Gizmos.DrawMesh(mesh, subMeshIndex, meshPosition, Quaternion.Euler(meshRotation), meshScale);
                    else
                        UnityEngine.Gizmos.DrawMesh(mesh, subMeshIndex, this.transform.position, this.transform.rotation, this.transform.localScale);
                    break;
                case "Ray":
                        UnityEngine.Gizmos.DrawRay(fromR, directionR);
                    break;
                case "Sphere":
                    if (positionIsCenterSphere)
                        UnityEngine.Gizmos.DrawSphere(this.transform.position, radiusS);
                    else
                        UnityEngine.Gizmos.DrawSphere(sphereCenter, radiusS);
                    break;
                case "WireCube":
                    if (!positionIsCenterWireCube)
                        UnityEngine.Gizmos.DrawWireCube(wireCubeCenter, wireCubeSize);
                    else
                        UnityEngine.Gizmos.DrawWireCube(this.transform.position, wireCubeSize);
                    break;
                case "WireMesh":
                    if (!transformIsWireMeshTransform)
                        UnityEngine.Gizmos.DrawWireMesh(wireMesh, subWireMeshIndex, wireMeshPosition, Quaternion.Euler(wireMeshRotation), wireMeshScale);
                    else
                        UnityEngine.Gizmos.DrawWireMesh(wireMesh, subWireMeshIndex, this.transform.position, this.transform.rotation, this.transform.localScale);
                    break;
                case "WireSphere":
                    if (positionIsCenterWireSphere)
                        UnityEngine.Gizmos.DrawWireSphere(this.transform.position, radiusWS);
                    else
                        UnityEngine.Gizmos.DrawWireSphere(wireSphereCenter, radiusWS);
                    break;
                case "CameraOrthographic":
                    GizmosExtensions.DrawCameraOrthographic(cam, color, drawVertex);
                    break;
                case "LineExtended":
                    if (!useTwoTransformsLE)
                        GizmosExtensions.DrawLineExtended(startPointLE, endPointLE, thickness);
                    else
                        if (fromTrLE != null && toTrLE != null)
                            GizmosExtensions.DrawLineExtended(fromTrLE.position, toTrLE.position, thickness);
                    break;
                case "CubeExtended":
                    GizmosExtensions.DrawCubeExtended(positionCE, Quaternion.Euler(rotationCE.x, rotationCE.y, rotationCE.z), scaleCE);
                    break;
                case "WireCubeExtended":
                    GizmosExtensions.DrawWireCubeExtended(transform.position + positionWCE, Quaternion.Euler(rotationWCE.x, rotationWCE.y, rotationWCE.z), scaleWCE);
                    break;
            }

            UnityEngine.Gizmos.color = Color.white;
        }

        public int GetTypeIndex()
        {
            for (int i = 0; i < Type.Length; i++)
            {
                if(type == Type[i])
                {
                    return i;
                }
            }
            return 0;
        }
    }
}
