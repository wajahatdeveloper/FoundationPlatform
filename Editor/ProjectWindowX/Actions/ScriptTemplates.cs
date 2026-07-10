namespace ProjectWindowX {
    /// <summary>Script/shader templates for the create-actions menu. {NAME} is replaced by the asset name.</summary>
    internal static class ScriptTemplates {

        internal const string MonoBehaviour =
@"using UnityEngine;

public class {NAME} : MonoBehaviour
{
    private void Awake()
    {
    }
}
";

        internal const string PlainClass =
@"public class {NAME}
{
}
";

        internal const string Interface =
@"public interface {NAME}
{
}
";

        internal const string Struct =
@"public struct {NAME}
{
}
";

        internal const string Enum =
@"public enum {NAME}
{
    None = 0,
}
";

        internal const string ScriptableObject =
@"using UnityEngine;

[CreateAssetMenu(menuName = ""{NAME}"")]
public class {NAME} : ScriptableObject
{
}
";

        internal const string EditorScript =
@"using UnityEditor;
using UnityEngine;

public class {NAME}
{
}
";

        internal const string CustomEditor =
@"using UnityEditor;
using UnityEngine;

[CustomEditor(typeof({TARGET}))]
public class {NAME} : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }
}
";

        internal const string UnlitShader =
@"Shader ""Unlit/{NAME}""
{
    Properties
    {
        _MainTex (""Texture"", 2D) = ""white"" {}
        _Color (""Color"", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { ""RenderType""=""Opaque"" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv) * _Color;
            }
            ENDCG
        }
    }
}
";

        internal const string Asmdef =
@"{
    ""name"": ""{NAME}"",
    ""references"": [],
    ""includePlatforms"": [],
    ""excludePlatforms"": [],
    ""allowUnsafeCode"": false,
    ""autoReferenced"": true
}
";
    }
}
