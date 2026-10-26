using System.Collections.Generic;
using UnityEngine;

namespace SHIN
{
    public enum ResourceTraceKind
    {
        Asset,
        Label,
        Instance
    }

    public enum ResourceTraceState
    {
        Loading,
        Held,
        Released,
        Failed
    }

    public sealed class ResourceTraceEntry
    {
        public string Key { get; internal set; }
        public ResourceTraceKind Kind { get; internal set; }
        public ResourceTraceState State { get; internal set; }
        public float StartedAt { get; internal set; }
        public float FinishedAt { get; internal set; }
        public bool HasFinished { get; internal set; }
    }

    public partial class ResourceManager
    {
        private static readonly List<ResourceTraceEntry> TraceEntries = new();

        public static IReadOnlyList<ResourceTraceEntry> LoadTraces => TraceEntries;

        public static void ClearLoadTraces()
        {
            TraceEntries.Clear();
        }

        private static void TraceLoadStarted(string key, ResourceTraceKind kind)
        {
            if (string.IsNullOrEmpty(key))
                return;

            var entry = FindOpen(key, kind);
            if (entry != null)
                return;

            TraceEntries.Add(new ResourceTraceEntry
            {
                Key = key,
                Kind = kind,
                State = ResourceTraceState.Loading,
                StartedAt = Time.realtimeSinceStartup
            });
        }

        private static void TraceLoadFinished(string key, ResourceTraceKind kind, bool success)
        {
            var entry = FindOpen(key, kind);
            if (entry == null)
                return;

            entry.State = success ? ResourceTraceState.Held : ResourceTraceState.Failed;
            entry.FinishedAt = Time.realtimeSinceStartup;
            entry.HasFinished = true;
        }

        private static void TraceReleased(string key, ResourceTraceKind kind)
        {
            var entry = FindOpen(key, kind);
            if (entry == null)
            {
                TraceEntries.Add(new ResourceTraceEntry
                {
                    Key = key,
                    Kind = kind,
                    State = ResourceTraceState.Released,
                    StartedAt = Time.realtimeSinceStartup,
                    FinishedAt = Time.realtimeSinceStartup,
                    HasFinished = true
                });
                return;
            }

            entry.State = ResourceTraceState.Released;
            entry.FinishedAt = Time.realtimeSinceStartup;
            entry.HasFinished = true;
        }

        private static ResourceTraceEntry FindOpen(string key, ResourceTraceKind kind)
        {
            for (var i = TraceEntries.Count - 1; i >= 0; i--)
            {
                var entry = TraceEntries[i];
                if (entry.Key != key || entry.Kind != kind)
                    continue;
                if (entry.State == ResourceTraceState.Released || entry.State == ResourceTraceState.Failed)
                    continue;
                return entry;
            }

            return null;
        }
    }
}
