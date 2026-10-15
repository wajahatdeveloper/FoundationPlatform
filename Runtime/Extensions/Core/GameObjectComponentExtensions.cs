using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Component-related extensions for GameObjects
/// </summary>
namespace AetherNexus.FoundationPlatform.Extensions
{
public static class GameObjectComponentExtensions
{

#region GetComponentOnObject
#endregion
#region GetComponentOnObjectOrParent
#endregion
#region HasComponent
#endregion
#region AddOrGetComponent
#endregion
#region GetComponentsInChildrenWithTag
#endregion
#region GetInterface
#endregion
#region GetInterfaceInChildren

    #endregion

    #region GetInterfaces

    /// <summary>
    /// Returns all interfaces on this game object matching this type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="go"></param>
    /// <remarks>
    /// Suggested by: A.Killingbeck
    /// Link: http://forum.unity3d.com/members/a-killingbeck.560711/
    /// </remarks>
    public static IEnumerable<T> GetInterfaces<T>(this GameObject go) where T : class
    {
        if (!typeof(T).IsInterface)
        {
            Debug.LogError(typeof(T).ToString() + " is not an interface");
            return Enumerable.Empty<T>();
        }

        return go.GetComponents<Component>().OfType<T>();
    }

#endregion
#region GetInterfacesInChildren
#endregion

    /// <summary>
    /// Returns the first monobehaviour that is of the class Type, as T
    /// works with interfaces
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="gObj"></param>
    /// <returns></returns>
    public static T GetClass<T>(this GameObject gObj) where T : class
    {
        return gObj.GetComponent(typeof(T)) as T;
    }
}
}

