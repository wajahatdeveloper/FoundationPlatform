#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using AetherNexus.FoundationPlatform.Attributes;
    using UnityEditor;
    using UnityEditor.Callbacks;
    using UnityEngine;

    public class DeclarativeOrder
    {
        struct Edge
        {
            public Type from;
            public Type to;
        }

        private static HashSet<Type> nodes = new HashSet<Type>();
        private static HashSet<Edge> edges = new HashSet<Edge>();
        private static Dictionary<Type, int> depCount = new Dictionary<Type, int>();


        private static void AddNode(Type t)
        {
            nodes.Add(t);
            if (depCount.ContainsKey(t) == false)
            {
                depCount[t] = 0;
            }
        }

        [DidReloadScripts]
        public static void OnScriptsLoaded()
        {
            nodes.Clear();
            edges.Clear();
            depCount.Clear();

            Type runFirst = null;
            Type runLast = null;

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    if (assembly.FullName.Contains("Analyzer"))
                    {
                        continue;
                    }
                    
                    // Handle assemblies with types that can't be loaded (e.g., analyzer assemblies with missing dependencies)
                    // Use only the successfully loaded types
                    try
                    {
                        types = ex.Types;
                        if (types == null)
                        {
                            // If no types were loaded, skip this assembly
                            continue;
                        }
                    }
                    catch
                    {
                        // If accessing ex.Types itself throws an exception, skip this assembly
                        continue;
                    }
                }
                catch (Exception)
                {
                    // Skip assemblies that fail to load for any other reason
                    continue;
                }

                // Filter out null types before processing (can occur with ReflectionTypeLoadException)
                if (types == null)
                {
                    continue;
                }

                // Filter out null types from the array to prevent any null access
                var validTypes = new List<Type>();
                foreach (var t in types)
                {
                    try
                    {
                        // Double-check null and validate type before accessing properties
                        if (t != null && t.IsClass)
                        {
                            validTypes.Add(t);
                        }
                    }
                    catch
                    {
                        // Skip types that cause exceptions when accessing properties
                        continue;
                    }
                }

                foreach (Type typ in validTypes)
                {
                    try
                    {
                        bool hasOrdering = false;

                        // Additional null check and try-catch to prevent Unity assertion
                        if (typ != null && typ.IsSubclassOf(typeof(MonoBehaviour)))
                        {
                            object[] attribs = typ.GetCustomAttributes(true);
                            for (int i = 0; i < attribs.Length; i++)
                            {
                                var atype = attribs[i].GetType();
                                if (atype == typeof(RunAfter))
                                {
                                    hasOrdering = true;

                                    AddNode(typ);

                                    RunAfter deps = attribs[i] as RunAfter;

                                    foreach (Type depType in deps.All)
                                    {
                                        AddNode(depType);
                                        edges.Add(new Edge() { from = depType, to = typ });
                                        depCount[typ] = depCount[typ] + 1;
                                    }
                                }
                                else if (atype == typeof(RunBefore))
                                {
                                    hasOrdering = true;

                                    AddNode(typ);

                                    RunBefore deps = attribs[i] as RunBefore;

                                    foreach (Type depType in deps.All)
                                    {
                                        AddNode(depType);
                                        edges.Add(new Edge() { from = typ, to = depType });
                                        depCount[depType] = depCount[depType] + 1;
                                    }
                                }
                            }

                            for (int i = 0; i < attribs.Length; i++)
                            {
                                var atype = attribs[i].GetType();

                                if (atype == typeof(RunFirst))
                                {
                                    if (hasOrdering)
                                    {
                                        Debug.LogError("RunFirst must be used alone. It can't also be used with RunAfter, or RunBefore: " +
                                                       typ.ToString());
                                        return;
                                    }

                                    if (runFirst != null)
                                    {
                                        Debug.LogError("Two classes marked as 'RunFirst'. You can only pick one: " + runFirst.ToString() + " <=> " +
                                                       typ.ToString());
                                        return;
                                    }

                                    runFirst = typ;
                                }
                                else if (atype == typeof(RunLast))
                                {
                                    if (hasOrdering)
                                    {
                                        Debug.LogError("RunLast must be used alone. It can't also be used with RunAfter, or RunBefore: " +
                                                       typ.ToString());
                                        return;
                                    }

                                    if (runLast != null)
                                    {
                                        Debug.LogError("Two classes marked as 'RunLast'. You can only pick one: " + runLast.ToString() + " <=> " +
                                                       typ.ToString());
                                        return;
                                    }

                                    runLast = typ;
                                }
                            }

                        }
                    }
                    catch
                    {
                        // Skip types that cause exceptions (e.g., Unity internal assertions)
                        continue;
                    }
                }
            }

            if (runFirst != null && runFirst == runLast)
            {
                Debug.LogError("A class cannot be marked as RunFirst and RunLast at the same time: " + runLast.ToString());
                return;
            }

            List<Type> ordered = new List<Type>();
            Queue<Type> start = new Queue<Type>();
            foreach (Type typ in depCount.Keys)
            {
                if (depCount[typ] == 0)
                {
                    start.Enqueue(typ);
                }
            }

            while (start.Count > 0)
            {
                Type next = start.Dequeue();
                ordered.Remove(next);
                ordered.Add(next);
                // Snapshot the outgoing edges of 'next' so we don't mutate 'edges' while enumerating it.
                List<Edge> outgoing = new List<Edge>();
                foreach (Edge edge in edges)
                {
                    if (edge.from == next)
                    {
                        outgoing.Add(edge);
                    }
                }

                foreach (Edge edge in outgoing)
                {
                    edges.Remove(edge);

                    // The successor 'edge.to' lost one incoming edge. Enqueue it once it has no remaining incoming edges.
                    depCount[edge.to] = depCount[edge.to] - 1;
                    if (depCount[edge.to] == 0)
                    {
                        start.Enqueue(edge.to);
                    }
                }
            }

            int step = 10;

            Dictionary<Type, int> order = new Dictionary<Type, int>();
            for (int i = 0; i < ordered.Count; i++)
            {
                order[ordered[i]] = (i + (runFirst==null?1:2)) * step;
            }

            if (runLast != null)
            {
                order[runLast] = (ordered.Count + (runFirst == null ? 1 : 2)) * step;
            }

            if (runFirst != null)
            {
                order[runFirst] = step;
            }

            if (edges.Count > 0)
            {
                foreach (var ed in edges)
                {
                    Debug.LogError("Cannot update script order due to circular dependency: " + ed.from.ToString() + " <=> " + ed.to.ToString());
                }
                return;
            }
            else
            {
                MonoScript[] scripts = MonoImporter.GetAllRuntimeMonoScripts();

                for (int i = 0; i < scripts.Length; i++)
                {
                    Type t = scripts[i].GetClass();

                    if (t != null && order.ContainsKey(t))
                    {
                        if (MonoImporter.GetExecutionOrder(scripts[i]) != order[t])
                        {
                            MonoImporter.SetExecutionOrder(scripts[i], order[t]);
                        }
                        order.Remove(t);
                    }
                }

                if (order.Count > 0)
                {
                    foreach (var t in order)
                    {
                        Debug.LogWarning("Unable to set execution order of " + t.Key.FullName + ". The MonoBehaviour class name must match its filename.");
                    }
                }
            }
        }
    }
#endif