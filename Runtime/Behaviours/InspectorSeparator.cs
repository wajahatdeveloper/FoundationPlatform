using UnityEngine;
using AetherNexus.FoundationPlatform.Utilities.Menus;

namespace AetherNexus.FoundationPlatform.Behaviours
{
    [AddComponentMenu("FoundationPlatform/Editor/Inspector Separator")]
    [Icon("Packages/com.aethernexus.foundationplatform/Editor/Icons/InspectorSeparator.png")]
    [DesignerIcon(DesignerSymbol.Widget)]
    public class InspectorSeparator : MonoBehaviour
    {
        [SerializeField] string _label;
    }
}
