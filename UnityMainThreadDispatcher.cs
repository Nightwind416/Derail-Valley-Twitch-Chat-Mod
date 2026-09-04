using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Provides thread-safe execution of actions on Unity's main thread.
/// </summary>
/// <remarks>
/// Unity's API is not thread-safe and must be accessed from the main thread.
/// This dispatcher ensures that operations from background threads are properly
/// queued and executed on the main thread. The singleton is registered in Awake so
/// <see cref="Instance"/> never has to call into Unity from a background thread.
/// </remarks>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    /// <summary>Queue of actions pending execution on the main thread</summary>
    private static readonly Queue<Action> _executionQueue = new();

    /// <summary>Singleton instance of the dispatcher</summary>
    private static UnityMainThreadDispatcher? _instance;

    private void Awake()
    {
        _instance = this;
    }

    /// <summary>
    /// Gets the singleton instance of the UnityMainThreadDispatcher.
    /// Creates a new GameObject with the dispatcher if one doesn't exist (main thread only).
    /// </summary>
    /// <returns>The singleton instance of UnityMainThreadDispatcher</returns>
    public static UnityMainThreadDispatcher Instance()
    {
        // Reference comparison on purpose: Unity's overloaded == is not safe off the main thread.
        UnityMainThreadDispatcher? instance = _instance;
        if (instance is null)
        {
            instance = FindObjectOfType<UnityMainThreadDispatcher>();
            if (instance is null)
            {
                var obj = new GameObject("UnityMainThreadDispatcher");
                instance = obj.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(obj);
            }
            _instance = instance;
        }
        return instance;
    }

    /// <summary>
    /// Adds an action to the execution queue to be executed on the main thread.
    /// </summary>
    /// <param name="action">The action to be executed on the main thread</param>
    public void Enqueue(Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }

    /// <summary>
    /// Unity Update method that processes all queued actions on the main thread.
    /// Each action runs outside the lock and is isolated, so one failure cannot block the rest.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
    private void Update()
    {
        while (true)
        {
            Action action;
            lock (_executionQueue)
            {
                if (_executionQueue.Count == 0)
                {
                    return;
                }
                action = _executionQueue.Dequeue();
            }

            try
            {
                action();
            }
            catch (Exception ex)
            {
                TwitchChat.Main.LogEntry("UnityMainThreadDispatcher", $"Queued action failed: {ex.Message}");
            }
        }
    }
}
