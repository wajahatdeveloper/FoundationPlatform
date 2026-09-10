using UnityEngine;

namespace AetherNexus.FoundationPlatform.Behaviours
{
    [AddComponentMenu("FoundationPlatform/Editor/Inspector Separator")]
    [Icon("Packages/com.aethernexus.foundationplatform/Editor/Icons/InspectorSeparator.png")]
    public class InspectorSeparator : MonoBehaviour
    {
        [SerializeField] string _label;
    }
}
