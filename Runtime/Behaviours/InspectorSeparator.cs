using UnityEngine;
using AetherNexus.FoundationPlatform.Utilities.Menus;

namespace AetherNexus.FoundationPlatform.Behaviours
{
    [AddComponentMenu("Editor Helpers/Inspector Separator")]
    [Icon("Packages/com.aethernexus.foundationplatform/Editor/Icons/InspectorSeparator.png")]
    [DesignerIcon(DesignerSymbol.Widget)]
    public class InspectorSeparator : MonoBehaviour, IEditorOnlyComponent
    {
        [SerializeField] string _label;
    }
}
