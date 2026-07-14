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

    /// <summary>
    /// Returns a component attached to the game object
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="go"></param>
    /// <param name="showErrorInConsole">when true, logs an error in the console if nothing found</param>
    /// <returns></returns>
    public static T GetComponentOnObject<T>(this GameObject go, bool showErrorInConsole) where T : Component
    {
        // get the component
        T component = go.GetComponent<T>();
        if ((showErrorInConsole) && (component == null))
            Debug.LogError(string.Format("Unable to find component '{0}' on object '{1}'", typeof(T).Name, go.name));

        return component;
    }

    /// <summary>
    /// Returns a component attached to the game object
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="trans"></param>
    /// <param name="showErrorInConsole">when true, logs an error in the console if nothing found</param>
    /// <returns></returns>
    public static T GetComponentOnObject<T>(this Transform trans, bool showErrorInConsole) where T : Component
    {
        // get the component
        T component = trans.GetComponent<T>();
        if ((showErrorInConsole) && (component == null))
            Debug.LogError(string.Format("Unable to find component '{0}' on object '{1}'", typeof(T).Name, trans.name));

        return component;
    }

    #endregion

    #region GetComponentOnObjectOrParent

    /// <summary>
    /// Returns a component attached to the game object, or its parent
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="go"></param>
    /// <param name="showErrorInConsole">when true, logs an error in the console if nothing found</param>
    /// <returns></returns>
    public static T GetComponentOnObjectOrParent<T>(this GameObject go, bool showErrorInConsole) where T : Component
    {
        // get the component
        T component = go.GetComponentInParent<T>();
        if ((showErrorInConsole) && (component == null))
            Debug.LogError(string.Format("Unable to find component '{0}' on object (or parent) '{1}'", typeof(T).Name,
                go.name));

        return component;
    }

    #endregion

    #region HasComponent

    /// <summary>
    /// Returns true if the game object has this component
    /// </summary>
    /// <param name="go"></param>
    public static bool HasComponent<T>(this GameObject go) where T : Component
    {
        return (go.GetComponent<T>() != null);
    }

    #endregion

    #region AddOrGetComponent

    public static T AddOrGetComponent<T>(this GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        if (component == null)
        {
            component = go.AddComponent<T>();
        }

        return component;
    }

    public static T GetComponentOrAdd<T>(this GameObject go) where T : Component
    {
        return go.AddOrGetComponent<T>();
    }

    public static T GetIComponent<T>(this GameObject go) where T : class
    {
        return go.GetComponent(typeof(T)) as T;
    }

    #endregion

    #region GetComponentsInChildrenWithTag

    /// <summary>
    /// Returns all components in the game object and children with the specified tag
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="go"></param>
    /// <param name="tag"></param>
    /// <returns></returns>
    public static List<T> GetComponentsInChildrenWithTag<T>(this GameObject go, string tag) where T : Component
    {
        List<T> results = new List<T>();

        // check if the object has this tag
        if (go.CompareTag(tag))
        {
            var component = go.GetComponent<T>();
            if (component != null)
                results.Add(component);
        }

        // loop through all children with this tag
        foreach (Transform t in go.transform)
            results.AddRange(t.gameObject.GetComponentsInChildrenWithTag<T>(tag));

        return results;
    }

    #endregion

    #region GetInterface

    /// <summary>
    /// Returns the interface on this game object
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="go"></param>
    /// <remarks>
    /// Suggested by: A.Killingbeck
    /// Link: http://forum.unity3d.com/members/a-killingbeck.560711/
    /// </remarks>
    public static T GetInterface<T>(this GameObject go) where T : class
    {
        if (!typeof(T).IsInterface)
        {
            Debug.LogError(typeof(T).ToString() + " is not an interface");
            return null;
        }

        return go.GetComponents<Component>().OfType<T>().FirstOrDefault();
    }

    #endregion

    #region GetInterfaceInChildren

    /// <summary>
    /// Returns the first matching interface on this game object's children
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="go"></param>
    /// <remarks>
    /// Suggested by: A.Killingbeck
    /// Link: http://forum.unity3d.com/members/a-killingbeck.560711/
    /// </remarks>
    public static T GetInterfaceInChildren<T>(this GameObject go) where T : class
    {
        if (!typeof(T).IsInterface)
        {
            Debug.LogError(typeof(T).ToString() + " is not an interface");
            return null;
        }

        return go.GetComponentsInChildren<Component>().OfType<T>().FirstOrDefault();
    }

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

    /// <summary>
    /// Returns all matching interfaces on this game object's children
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="go"></param>
    /// <remarks>
    /// Suggested by: A.Killingbeck
    /// Link: http://forum.unity3d.com/members/a-killingbeck.560711/
    /// </remarks>
    public static IEnumerable<T> GetInterfacesInChildren<T>(this GameObject go) where T : class
    {
        if (!typeof(T).IsInterface)
        {
            Debug.LogError(typeof(T).ToString() + " is not an interface");
            return Enumerable.Empty<T>();
        }

        return go.GetComponentsInChildren<Component>(true).OfType<T>();
    }

    #endregion

    // Helper function to access all components of a type on the game object.
    public static void ForEachChildOfType<T>(this GameObject gameObject, Action<T> callback)
    {
        foreach (T t in gameObject.GetComponentsInChildren<T>())
        {
            callback(t);
        }
    }

    // Helper function to access all components of a type on root of the given the game object.
    public static void ForEachRootChildOfType<T>(this GameObject gameObject, Action<T> callback)
    {
        gameObject.transform.root.gameObject.ForEachChildOfType<T>(callback);
    }

    /// <summary>
    /// Returns all monobehaviours that are of type T, as T. Works for interfaces
    /// </summary>
    /// <typeparam name="T">class type</typeparam>
    /// <param name="gObj"></param>
    /// <returns></returns>
    public static T[] GetClasses<T>(this GameObject gObj) where T : class
    {
        var ts = gObj.GetComponents(typeof(T));

        var ret = new T[ts.Length];
        for (int i = 0; i < ts.Length; i++)
        {
            ret[i] = ts[i] as T;
        }

        return ret;
    }

    /// <summary>
    /// Returns all classes of type T (casted to T)
    /// works with interfaces
    /// </summary>
    /// <typeparam name="T">interface type</typeparam>
    /// <param name="gObj"></param>
    /// <returns></returns>
    public static T[] GetClasses<T>(this Transform gObj) where T : class
    {
        return gObj.gameObject.GetClasses<T>();
    }

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

    /// <summary>
    /// Gets all monobehaviours in children that implement the class of type T (casted to T)
    /// works with interfaces
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="gObj"></param>
    /// <returns></returns>
    public static T[] GetClassesInChildren<T>(this GameObject gObj) where T : class
    {
        var ts = gObj.GetComponentsInChildren(typeof(T));

        var ret = new T[ts.Length];
        for (int i = 0; i < ts.Length; i++)
        {
            ret[i] = ts[i] as T;
        }

        return ret;
    }

    /// <summary>
    ///
    /// Returns the first instance of the monobehaviour that is of the class type T (casted to T)
    /// works with interfaces
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="gObj"></param>
    /// <returns></returns>
    public static T GetClassInChildren<T>(this GameObject gObj) where T : class
    {
        return gObj.GetComponentInChildren(typeof(T)) as T;
    }

    /// <summary>
    /// executes message with the component of type TI if it exists in gameobject's heirarchy. this executes on all behaviours that implement TI.
    /// parm is included in the action, to help reduce closures
    /// </summary>
    /// <typeparam name="TI">component type to get</typeparam>
    /// <typeparam name="TParm">type of the parameter to pass into the message</typeparam>
    /// <param name="gobj"></param>
    /// <param name="message">action to run on each component that matches TI</param>
    /// <param name="parm">the object to pass into the message. this reduces closures.</param>
    public static void DoMessage<TI, TParm>(this GameObject gobj, Action<TI, TParm> message, TParm parm)
        where TI : class
    {
        var ts = gobj.GetComponentsInChildren(typeof(TI));
        for (int i = 0; i < ts.Length; i++)
        {
            var comp = ts[i] as TI;
            if (comp != null)
            {
                message(comp, parm);
            }
        }
    }

    /// <summary>
    /// executes message with the component of type TI if it exists in gameobject's heirarchy. this executes for all behaviours that implement TI.
    /// It is recommended that you use the other DoMessage if you need to pass a variable into the message, to reduce garbage pressure due to lambdas.
    /// </summary>
    /// <typeparam name="TI"></typeparam>
    /// <param name="gobj"></param>
    /// <param name="message"></param>
    public static void DoMessage<TI>(this GameObject gobj, Action<TI> message) where TI : class
    {
        var ts = gobj.GetComponentsInChildren(typeof(TI));
        for (int i = 0; i < ts.Length; i++)
        {
            var comp = ts[i] as TI;
            if (comp != null)
            {
                message(comp);
            }
        }
    }

    /// <summary>
    /// Gets a component, adding it if it doesn't exist
    /// </summary>
    public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
    {
        var toGet = gameObject.GetComponent<T>();
        if (toGet != null) return toGet;
        return gameObject.AddComponent<T>();
    }

    /// <summary>
    /// Gets a component, adding it if it doesn't exist
    /// </summary>
    public static T GetOrAddComponent<T>(this Component component) where T : Component
    {
        var toGet = component.gameObject.GetComponent<T>();
        if (toGet != null) return toGet;
        return component.gameObject.AddComponent<T>();
    }

    /// <summary>
    /// Find all Components of specified interface.
    /// EDITOR/SETUP-ONLY: performs a full-scene scan with a GetComponent per Transform.
    /// Do NOT call from gameplay hot paths (Update/per-frame).
    /// </summary>
    public static T[] FindObjectsOfInterface<T>() where T : class
    {
        var transforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);

        var results = new List<T>(transforms.Length);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].GetComponent(typeof(T)) is T match)
            {
                results.Add(match);
            }
        }
        return results.ToArray();
    }

    /// <summary>
    /// Find all Components of specified interface along with Component itself
    /// </summary>
    public static ComponentOfInterface<T>[] FindObjectsOfInterfaceAsComponents<T>() where T : class
    {
        return Object.FindObjectsByType<Component>(FindObjectsSortMode.None)
            .Where(c => c is T)
            .Select(c => new ComponentOfInterface<T>(c, c as T)).ToArray();
    }

    public struct ComponentOfInterface<T>
    {
        public readonly Component Component;
        public readonly T Interface;

        public ComponentOfInterface(Component component, T @interface)
        {
            Component = component;
            Interface = @interface;
        }
    }
}
}

