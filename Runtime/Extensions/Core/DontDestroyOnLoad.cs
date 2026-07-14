using UnityEngine;

namespace AetherNexus.FoundationPlatform.Extensions
{
[DisallowMultipleComponent]
public class DontDestroyOnLoad : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        Debug.Log("The GameObject \"" + gameObject.name + "\" was set as DontDestroyOnLoad", this);
    }
}}
