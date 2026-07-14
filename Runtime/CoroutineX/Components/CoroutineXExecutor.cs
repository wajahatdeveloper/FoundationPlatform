using UnityEngine;


namespace AetherNexus.FoundationPlatform.CoroutineX
{
internal sealed class CoroutineXExecutor : MonoBehaviour
{
    internal static bool HideCoroutineXExecutor = true;

    internal static CoroutineXExecutor Instance { get; private set; }
    internal static bool HasInstance => Instance != null;

    internal CoroutineXOwner Owner { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstance()
    {
        Instance = new GameObject("CoroutineXExecutor").AddComponent<CoroutineXExecutor>();
        Instance.Owner = Instance.gameObject.AddComponent<CoroutineXOwner>();

        if (HideCoroutineXExecutor)
        {
            Instance.gameObject.hideFlags = HideFlags.HideInHierarchy;
        }

        DontDestroyOnLoad(Instance.gameObject);
    }
}}
