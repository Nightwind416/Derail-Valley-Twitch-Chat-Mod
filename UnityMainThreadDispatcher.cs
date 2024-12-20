using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Provides a way to execute actions on Unity's main thread from other threads.
/// This is necessary because Unity's API is not thread-safe and must be called from the main thread.
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _executionQueue = new();

    private static UnityMainThreadDispatcher _instance = null!;

    /// <summary>
    /// Gets the singleton instance of the UnityMainThreadDispatcher.
    /// Creates a new GameObject with the dispatcher if one doesn't exist.
    /// </summary>
    /// <returns>The singleton instance of UnityMainThreadDispatcher</returns>
    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
        {
            _instance = FindObjectOfType<UnityMainThreadDispatcher>();
            if (_instance == null)
            {
                var obj = new GameObject("UnityMainThreadDispatcher");
                _instance = obj.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(obj);
            }
        }
        return _instance;
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
    /// Executes all pending actions in the execution queue.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
    private void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }
}