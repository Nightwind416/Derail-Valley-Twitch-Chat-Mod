using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Provides thread-safe execution of actions on Unity's main thread.
/// </summary>
/// <remarks>
/// Unity's API is not thread-safe and must be accessed from the main thread.
/// This dispatcher ensures that operations from background threads are properly
/// queued and executed on the main thread.
/// </remarks>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    /// <summary>Queue of actions pending execution on the main thread</summary>
    private static readonly Queue<Action> _executionQueue = new();

    /// <summary>Singleton instance of the dispatcher</summary>
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