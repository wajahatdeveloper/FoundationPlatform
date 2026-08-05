using UnityEngine;
using UnityEditor;

namespace AetherNexus.FoundationPlatform.Gizmos
{
    [CustomEditor(typeof(GizmosComponent))]
    [CanEditMultipleObjects]
    public class GizmosEditor : UnityEditor.Editor
    {
        SerializedProperty drawGizmo;
        SerializedProperty type;
        SerializedProperty color;
        SerializedProperty visibility;

        SerializedProperty drawFacingArrow;
        SerializedProperty facingArrowColor;
        SerializedProperty facingArrowOffset;
        SerializedProperty facingArrowLength;
        SerializedProperty facingArrowWidth;
        SerializedProperty facingArrowHeadLength;
        SerializedProperty facingArrowHeadAngle;

        SerializedProperty positionIsCenterCube;
        SerializedProperty cubeCenter;
        SerializedProperty cubeSize;

        SerializedProperty positionIsCenterFrustum;
        SerializedProperty frustumCenter;
        SerializedProperty fov;
        SerializedProperty maxRange;
        SerializedProperty minRange;
        SerializedProperty aspect;

        SerializedProperty screenRect;
        SerializedProperty texture;
        SerializedProperty mat;

        SerializedProperty positionIsCenterIcon;
        SerializedProperty iconCenter;
        SerializedProperty iconName;
        SerializedProperty allowScaling;

        SerializedProperty useTwoTransforms;
        SerializedProperty fromV;
        SerializedProperty toV;
        SerializedProperty fromTr;
        SerializedProperty toTr;

        SerializedProperty mesh;
        SerializedProperty transformIsMeshTransform;
        SerializedProperty meshPosition;
        SerializedProperty meshRotation;
        SerializedProperty meshScale;
        SerializedProperty subMeshIndex;

        SerializedProperty fromR;
        SerializedProperty directionR;

        SerializedProperty positionIsCenterSphere;
        SerializedProperty sphereCenter;
        SerializedProperty radiusS;

        SerializedProperty positionIsCenterWireCube;
        SerializedProperty wireCubeCenter;
        SerializedProperty wireCubeSize;

        SerializedProperty wireMesh;
        SerializedProperty transformIsWireMeshTransform;
        SerializedProperty wireMeshPosition;
        SerializedProperty wireMeshRotation;
        SerializedProperty wireMeshScale;
        SerializedProperty subWireMeshIndex;

        SerializedProperty positionIsCenterWireSphere;
        SerializedProperty wireSphereCenter;
        SerializedProperty radiusWS;

        SerializedProperty cam;
        SerializedProperty drawVertex;

        SerializedProperty useTwoTransformsLE;
        SerializedProperty startPointLE;
        SerializedProperty endPointLE;
        SerializedProperty fromTrLE;
        SerializedProperty toTrLE;
        SerializedProperty thickness;

        SerializedProperty positionCE;
        SerializedProperty rotationCE;
        SerializedProperty scaleCE;

        SerializedProperty positionWCE;
        SerializedProperty rotationWCE;
        SerializedProperty scaleWCE;

        SerializedProperty handleText;
        SerializedProperty positionIsCenterHandleText;
        SerializedProperty handleTextCenter;
        SerializedProperty handleTextOffset;
        SerializedProperty handleTextColor;
        SerializedProperty handleTextFontSize;
        SerializedProperty handleTextBold;
        SerializedProperty handleTextItalic;
        SerializedProperty handleTextAlignment;
        SerializedProperty handleTextBackground;
        SerializedProperty handleTextBackgroundColor;
        SerializedProperty handleTextShadow;
        SerializedProperty handleTextShadowColor;
        SerializedProperty handleTextShadowOffset;

        static Texture2D s_BackgroundTex;

        void OnEnable()
        {
            drawGizmo = serializedObject.FindProperty("drawGizmo");
            type = serializedObject.FindProperty("type");
            color = serializedObject.FindProperty("color");
            visibility = serializedObject.FindProperty("visibility");

            drawFacingArrow = serializedObject.FindProperty("drawFacingArrow");
            facingArrowColor = serializedObject.FindProperty("facingArrowColor");
            facingArrowOffset = serializedObject.FindProperty("facingArrowOffset");
            facingArrowLength = serializedObject.FindProperty("facingArrowLength");
            facingArrowWidth = serializedObject.FindProperty("facingArrowWidth");
            facingArrowHeadLength = serializedObject.FindProperty("facingArrowHeadLength");
            facingArrowHeadAngle = serializedObject.FindProperty("facingArrowHeadAngle");

            positionIsCenterCube = serializedObject.FindProperty("positionIsCenterCube");
            cubeCenter = serializedObject.FindProperty("cubeCenter");
            cubeSize = serializedObject.FindProperty("cubeSize");

            positionIsCenterFrustum = serializedObject.FindProperty("positionIsCenterFrustum");
            frustumCenter = serializedObject.FindProperty("frustumCenter");
            fov = serializedObject.FindProperty("fov");
            maxRange = serializedObject.FindProperty("maxRange");
            minRange = serializedObject.FindProperty("minRange");
            aspect = serializedObject.FindProperty("aspect");

            screenRect = serializedObject.FindProperty("screenRect");
            texture = serializedObject.FindProperty("texture");
            mat = serializedObject.FindProperty("mat");

            positionIsCenterIcon = serializedObject.FindProperty("positionIsCenterIcon");
            iconCenter = serializedObject.FindProperty("iconCenter");
            iconName = serializedObject.FindProperty("iconName");
            allowScaling = serializedObject.FindProperty("allowScaling");

            useTwoTransforms = serializedObject.FindProperty("useTwoTransforms");
            fromV = serializedObject.FindProperty("fromV");
            toV = serializedObject.FindProperty("toV");
            fromTr = serializedObject.FindProperty("fromTr");
            toTr = serializedObject.FindProperty("toTr");

            mesh = serializedObject.FindProperty("mesh");
            transformIsMeshTransform = serializedObject.FindProperty("transformIsMeshTransform");
            meshPosition = serializedObject.FindProperty("meshPosition");
            meshRotation = serializedObject.FindProperty("meshRotation");
            meshScale = serializedObject.FindProperty("meshScale");
            subMeshIndex = serializedObject.FindProperty("subMeshIndex");

            fromR = serializedObject.FindProperty("fromR");
            directionR = serializedObject.FindProperty("directionR");

            positionIsCenterSphere = serializedObject.FindProperty("positionIsCenterSphere");
            sphereCenter = serializedObject.FindProperty("sphereCenter");
            radiusS = serializedObject.FindProperty("radiusS");

            positionIsCenterWireCube = serializedObject.FindProperty("positionIsCenterWireCube");
            wireCubeCenter = serializedObject.FindProperty("wireCubeCenter");
            wireCubeSize = serializedObject.FindProperty("wireCubeSize");

            wireMesh = serializedObject.FindProperty("wireMesh");
            transformIsWireMeshTransform = serializedObject.FindProperty("transformIsWireMeshTransform");
            wireMeshPosition = serializedObject.FindProperty("wireMeshPosition");
            wireMeshRotation = serializedObject.FindProperty("wireMeshRotation");
            wireMeshScale = serializedObject.FindProperty("wireMeshScale");
            subWireMeshIndex = serializedObject.FindProperty("subWireMeshIndex");

            positionIsCenterWireSphere = serializedObject.FindProperty("positionIsCenterWireSphere");
            wireSphereCenter = serializedObject.FindProperty("wireSphereCenter");
            radiusWS = serializedObject.FindProperty("radiusWS");

            cam = serializedObject.FindProperty("cam");
            drawVertex = serializedObject.FindProperty("drawVertex");

            useTwoTransformsLE = serializedObject.FindProperty("useTwoTransformsLE");
            startPointLE = serializedObject.FindProperty("startPointLE");
            endPointLE = serializedObject.FindProperty("endPointLE");
            fromTrLE = serializedObject.FindProperty("fromTrLE");
            toTrLE = serializedObject.FindProperty("toTrLE");
            thickness = serializedObject.FindProperty("thickness");

            positionCE = serializedObject.FindProperty("positionCE");
            rotationCE = serializedObject.FindProperty("rotationCE");
            scaleCE = serializedObject.FindProperty("scaleCE");

            positionWCE = serializedObject.FindProperty("positionWCE");
            rotationWCE = serializedObject.FindProperty("rotationWCE");
            scaleWCE = serializedObject.FindProperty("scaleWCE");

            handleText = serializedObject.FindProperty("handleText");
            positionIsCenterHandleText = serializedObject.FindProperty("positionIsCenterHandleText");
            handleTextCenter = serializedObject.FindProperty("handleTextCenter");
            handleTextOffset = serializedObject.FindProperty("handleTextOffset");
            handleTextColor = serializedObject.FindProperty("handleTextColor");
            handleTextFontSize = serializedObject.FindProperty("handleTextFontSize");
            handleTextBold = serializedObject.FindProperty("handleTextBold");
            handleTextItalic = serializedObject.FindProperty("handleTextItalic");
            handleTextAlignment = serializedObject.FindProperty("handleTextAlignment");
            handleTextBackground = serializedObject.FindProperty("handleTextBackground");
            handleTextBackgroundColor = serializedObject.FindProperty("handleTextBackgroundColor");
            handleTextShadow = serializedObject.FindProperty("handleTextShadow");
            handleTextShadowColor = serializedObject.FindProperty("handleTextShadowColor");
            handleTextShadowOffset = serializedObject.FindProperty("handleTextShadowOffset");
        }

        static int GetTypeIndex(string typeName)
        {
            for (int i = 0; i < GizmosComponent.Type.Length; i++)
            {
                if (typeName == GizmosComponent.Type[i])
                    return i;
            }
            return 0;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(drawGizmo, new GUIContent("Draw Gizmo"));
            EditorGUILayout.PropertyField(visibility);
            EditorGUILayout.Separator();

            bool showShapeHelp = !drawGizmo.hasMultipleDifferentValues && !drawGizmo.boolValue;
            bool showShapeFields = drawGizmo.hasMultipleDifferentValues || drawGizmo.boolValue;

            if (showShapeFields)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = type.hasMultipleDifferentValues;
                int typePopupIndex = type.hasMultipleDifferentValues ? 0 : GetTypeIndex(type.stringValue);
                int newTypeIndex = EditorGUILayout.Popup("Gizmo Type", typePopupIndex, GizmosComponent.Type);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                    type.stringValue = GizmosComponent.Type[newTypeIndex];

                EditorGUILayout.PropertyField(color);

                if (type.hasMultipleDifferentValues)
                {
                    EditorGUILayout.HelpBox("Selected objects use different gizmo types. Choose a type above to align them, or edit each type's fields after unifying.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.BeginVertical("box");

                    int typeIndex = GetTypeIndex(type.stringValue);
                    switch (typeIndex)
                    {
                        case 0://Cube
                            EditorGUILayout.PropertyField(positionIsCenterCube, new GUIContent("Position is the center"));
                            if (positionIsCenterCube.hasMultipleDifferentValues || !positionIsCenterCube.boolValue)
                                EditorGUILayout.PropertyField(cubeCenter);
                            EditorGUILayout.PropertyField(cubeSize);
                            break;
                        case 1://Frustrum
                            EditorGUILayout.PropertyField(positionIsCenterFrustum, new GUIContent("Position is the center"));
                            if (positionIsCenterFrustum.hasMultipleDifferentValues || !positionIsCenterFrustum.boolValue)
                                EditorGUILayout.PropertyField(frustumCenter);
                            EditorGUILayout.PropertyField(fov);
                            EditorGUILayout.PropertyField(minRange);
                            EditorGUILayout.PropertyField(maxRange);
                            EditorGUILayout.PropertyField(aspect);
                            break;
                        case 2://GUITexture
                            EditorGUILayout.PropertyField(screenRect);
                            EditorGUILayout.PropertyField(texture);
                            EditorGUILayout.PropertyField(mat);
                            break;
                        case 3://Icon
                            EditorGUILayout.PropertyField(positionIsCenterIcon, new GUIContent("Position is the center"));
                            if (positionIsCenterIcon.hasMultipleDifferentValues || !positionIsCenterIcon.boolValue)
                                EditorGUILayout.PropertyField(iconCenter);
                            EditorGUILayout.PropertyField(iconName);
                            EditorGUILayout.PropertyField(allowScaling);
                            break;
                        case 4://Line
                            EditorGUILayout.PropertyField(useTwoTransforms, new GUIContent("Use two transforms"));
                            if (!useTwoTransforms.hasMultipleDifferentValues && !useTwoTransforms.boolValue)
                            {
                                EditorGUILayout.PropertyField(fromV);
                                EditorGUILayout.PropertyField(toV);
                            }
                            else if (!useTwoTransforms.hasMultipleDifferentValues && useTwoTransforms.boolValue)
                            {
                                EditorGUILayout.PropertyField(fromTr);
                                EditorGUILayout.PropertyField(toTr);
                            }
                            else
                            {
                                EditorGUILayout.PropertyField(fromV);
                                EditorGUILayout.PropertyField(toV);
                                EditorGUILayout.PropertyField(fromTr);
                                EditorGUILayout.PropertyField(toTr);
                            }
                            break;
                        case 5://Mesh
                            EditorGUILayout.PropertyField(mesh);
                            EditorGUILayout.PropertyField(transformIsMeshTransform, new GUIContent("Transform is Mesh Transform"));
                            if (transformIsMeshTransform.hasMultipleDifferentValues || !transformIsMeshTransform.boolValue)
                            {
                                EditorGUILayout.PropertyField(meshPosition);
                                EditorGUILayout.PropertyField(meshRotation);
                                EditorGUILayout.PropertyField(meshScale);
                            }
                            EditorGUILayout.PropertyField(subMeshIndex);
                            break;
                        case 6://Ray
                            EditorGUILayout.PropertyField(fromR);
                            EditorGUILayout.PropertyField(directionR);
                            break;
                        case 7://Sphere
                            EditorGUILayout.PropertyField(positionIsCenterSphere, new GUIContent("Position is the center"));
                            if (positionIsCenterSphere.hasMultipleDifferentValues || !positionIsCenterSphere.boolValue)
                                EditorGUILayout.PropertyField(sphereCenter);
                            EditorGUILayout.PropertyField(radiusS);
                            break;
                        case 8://WireCube
                            EditorGUILayout.PropertyField(positionIsCenterWireCube, new GUIContent("Position is the center"));
                            if (positionIsCenterWireCube.hasMultipleDifferentValues || !positionIsCenterWireCube.boolValue)
                                EditorGUILayout.PropertyField(wireCubeCenter);
                            EditorGUILayout.PropertyField(wireCubeSize);
                            break;
                        case 9://WireMesh
                            EditorGUILayout.PropertyField(wireMesh);
                            EditorGUILayout.PropertyField(transformIsWireMeshTransform, new GUIContent("Transform is Mesh Transform"));
                            if (transformIsWireMeshTransform.hasMultipleDifferentValues || !transformIsWireMeshTransform.boolValue)
                            {
                                EditorGUILayout.PropertyField(wireMeshPosition);
                                EditorGUILayout.PropertyField(wireMeshRotation);
                                EditorGUILayout.PropertyField(wireMeshScale);
                            }
                            EditorGUILayout.PropertyField(subWireMeshIndex);
                            break;
                        case 10://WireSphere
                            EditorGUILayout.PropertyField(positionIsCenterWireSphere, new GUIContent("Position is the center"));
                            if (positionIsCenterWireSphere.hasMultipleDifferentValues || !positionIsCenterWireSphere.boolValue)
                                EditorGUILayout.PropertyField(wireSphereCenter);
                            EditorGUILayout.PropertyField(radiusWS);
                            break;
                        case 11://CameraOrthographic
                            EditorGUILayout.PropertyField(cam);
                            EditorGUILayout.PropertyField(drawVertex);
                            break;
                        case 12://LineExtended
                            EditorGUILayout.PropertyField(useTwoTransformsLE, new GUIContent("Use two transforms"));
                            if (!useTwoTransformsLE.hasMultipleDifferentValues && !useTwoTransformsLE.boolValue)
                            {
                                EditorGUILayout.PropertyField(startPointLE);
                                EditorGUILayout.PropertyField(endPointLE);
                            }
                            else if (!useTwoTransformsLE.hasMultipleDifferentValues && useTwoTransformsLE.boolValue)
                            {
                                EditorGUILayout.PropertyField(fromTrLE);
                                EditorGUILayout.PropertyField(toTrLE);
                            }
                            else
                            {
                                EditorGUILayout.PropertyField(startPointLE);
                                EditorGUILayout.PropertyField(endPointLE);
                                EditorGUILayout.PropertyField(fromTrLE);
                                EditorGUILayout.PropertyField(toTrLE);
                            }
                            EditorGUILayout.PropertyField(thickness);
                            break;
                        case 13://CubeExtended
                            EditorGUILayout.PropertyField(positionCE);
                            EditorGUILayout.PropertyField(rotationCE);
                            EditorGUILayout.PropertyField(scaleCE);
                            break;
                        case 14://WireCubeExtended
                            EditorGUILayout.PropertyField(positionWCE);
                            EditorGUILayout.PropertyField(rotationWCE);
                            EditorGUILayout.PropertyField(scaleWCE);
                            break;
                        case 15://HandleText
                            EditorGUILayout.PropertyField(handleText, new GUIContent("Text"));
                            EditorGUILayout.PropertyField(positionIsCenterHandleText, new GUIContent("Position is the center"));
                            if (positionIsCenterHandleText.hasMultipleDifferentValues || !positionIsCenterHandleText.boolValue)
                                EditorGUILayout.PropertyField(handleTextCenter, new GUIContent("Center"));
                            EditorGUILayout.PropertyField(handleTextOffset, new GUIContent("Offset"));
                            EditorGUILayout.PropertyField(handleTextColor, new GUIContent("Text Color"));
                            EditorGUILayout.PropertyField(handleTextFontSize, new GUIContent("Font Size"));
                            EditorGUILayout.PropertyField(handleTextBold, new GUIContent("Bold"));
                            EditorGUILayout.PropertyField(handleTextItalic, new GUIContent("Italic"));
                            EditorGUILayout.PropertyField(handleTextAlignment, new GUIContent("Alignment"));
                            EditorGUILayout.PropertyField(handleTextBackground, new GUIContent("Background"));
                            if (handleTextBackground.hasMultipleDifferentValues || handleTextBackground.boolValue)
                                EditorGUILayout.PropertyField(handleTextBackgroundColor, new GUIContent("Background Color"));
                            EditorGUILayout.PropertyField(handleTextShadow, new GUIContent("Shadow"));
                            if (handleTextShadow.hasMultipleDifferentValues || handleTextShadow.boolValue)
                            {
                                EditorGUILayout.PropertyField(handleTextShadowColor, new GUIContent("Shadow Color"));
                                EditorGUILayout.PropertyField(handleTextShadowOffset, new GUIContent("Shadow Offset"));
                            }
                            break;
                    }

                    EditorGUILayout.EndVertical();
                }
            }

            if (showShapeHelp)
                EditorGUILayout.HelpBox("Shape gizmo hidden; facing arrow can still draw if enabled.", MessageType.Info, true);

            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(drawFacingArrow, new GUIContent("Draw Facing Arrow"));
            if (drawFacingArrow.hasMultipleDifferentValues || drawFacingArrow.boolValue)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.PropertyField(facingArrowColor, new GUIContent("Arrow Color"));
                EditorGUILayout.PropertyField(facingArrowOffset, new GUIContent("Offset"));
                EditorGUILayout.PropertyField(facingArrowLength, new GUIContent("Length"));
                EditorGUILayout.PropertyField(facingArrowWidth, new GUIContent("Width"));
                EditorGUILayout.PropertyField(facingArrowHeadLength, new GUIContent("Head Length"));
                EditorGUILayout.PropertyField(facingArrowHeadAngle, new GUIContent("Head Angle"));
                EditorGUILayout.EndVertical();
            }

            serializedObject.ApplyModifiedProperties();
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Active | GizmoType.Pickable)]
        static void DrawHandleTextGizmo(GizmosComponent component, GizmoType gizmoType)
        {
            if (component == null || !component.drawGizmo || component.type != "HandleText")
                return;

            bool selected = (gizmoType & (GizmoType.Selected | GizmoType.Active)) != 0;
            if (component.visibility == GizmoVisibility.SelectedOnly && !selected)
                return;

            if (string.IsNullOrEmpty(component.handleText))
                return;

            Vector3 pos = (component.positionIsCenterHandleText
                ? component.transform.position
                : component.handleTextCenter) + component.handleTextOffset;

            FontStyle fontStyle = FontStyle.Normal;
            if (component.handleTextBold && component.handleTextItalic)
                fontStyle = FontStyle.BoldAndItalic;
            else if (component.handleTextBold)
                fontStyle = FontStyle.Bold;
            else if (component.handleTextItalic)
                fontStyle = FontStyle.Italic;

            var style = new GUIStyle(EditorStyles.label)
            {
                fontSize = component.handleTextFontSize,
                fontStyle = fontStyle,
                alignment = component.handleTextAlignment,
                normal = { textColor = component.handleTextColor }
            };

            bool needsGui = component.handleTextBackground || component.handleTextShadow;
            if (!needsGui)
            {
                Handles.Label(pos, component.handleText, style);
                return;
            }

            EnsureBackgroundTex();
            Handles.BeginGUI();
            Vector2 guiPos = HandleUtility.WorldToGUIPoint(pos);
            if (component.handleTextShadow)
            {
                var shadowStyle = new GUIStyle(style)
                {
                    normal = { textColor = component.handleTextShadowColor }
                };
                DrawHandleTextGui(
                    guiPos + component.handleTextShadowOffset,
                    component.handleText,
                    shadowStyle,
                    false,
                    Color.clear);
            }

            DrawHandleTextGui(
                guiPos,
                component.handleText,
                style,
                component.handleTextBackground,
                component.handleTextBackgroundColor);
            Handles.EndGUI();
        }

        static void EnsureBackgroundTex()
        {
            if (s_BackgroundTex != null)
                return;
            s_BackgroundTex = new Texture2D(1, 1);
            s_BackgroundTex.SetPixel(0, 0, Color.white);
            s_BackgroundTex.Apply();
            s_BackgroundTex.hideFlags = HideFlags.HideAndDontSave;
        }

        static void DrawHandleTextGui(Vector2 guiPos, string text, GUIStyle style, bool drawBackground, Color backgroundColor)
        {
            Vector2 size = style.CalcSize(new GUIContent(text));
            Rect rect = new Rect(guiPos.x, guiPos.y, size.x, size.y);

            switch (style.alignment)
            {
                case TextAnchor.UpperCenter:
                case TextAnchor.MiddleCenter:
                case TextAnchor.LowerCenter:
                    rect.x -= size.x * 0.5f;
                    break;
                case TextAnchor.UpperRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.LowerRight:
                    rect.x -= size.x;
                    break;
            }

            switch (style.alignment)
            {
                case TextAnchor.MiddleLeft:
                case TextAnchor.MiddleCenter:
                case TextAnchor.MiddleRight:
                    rect.y -= size.y * 0.5f;
                    break;
                case TextAnchor.LowerLeft:
                case TextAnchor.LowerCenter:
                case TextAnchor.LowerRight:
                    rect.y -= size.y;
                    break;
            }

            if (drawBackground)
            {
                Color prev = GUI.color;
                GUI.color = backgroundColor;
                GUI.DrawTexture(rect, s_BackgroundTex);
                GUI.color = prev;
            }

            GUI.Label(rect, text, style);
        }
    }
}
