using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Graphs;
using UnityEngine;

[InitializeOnLoad]
public static class AnimatorControllerWatcher
{
    private static HashSet<EditorWindow> trackedWindows = new();
    private static AnimatorController _lastController;
    private static double _lastCheckTime = 0;
    private const double CheckInterval = 0.5;   

    static AnimatorControllerWatcher()
    {
        EditorApplication.update += DetectWindowChanges;
        EditorApplication.update += CheckControllerChange;
    }

    private static void CheckControllerChange()
    {
        if (EditorApplication.isPlaying) return;

        double currentTime = EditorApplication.timeSinceStartup;
        if (currentTime - _lastCheckTime < CheckInterval)
            return;

        _lastCheckTime = currentTime;

        if (AnimatorWithKeyboardShared.Animator == null) return;

        var current = AnimatorWithKeyboardShared.GetController();
        if (current != _lastController)
        {
            _lastController = current;
            AnimatorWithKeyboardShared.AnimatorController = current;
        }

        // AnimatorController was reloaded.
        if (_lastController == null && current != null)
        {
            _lastController = current;
        }
    }

    private static void DetectWindowChanges()
    {
        var allWindows = Resources.FindObjectsOfTypeAll<EditorWindow>();
        var currentSet = new HashSet<EditorWindow>(allWindows);

        foreach (var window in currentSet)
        {
            if (window != null)
            {
                if (window.GetType() != null)
                {
                    if (!trackedWindows.Contains(window) && AnimatorWithKeyboardShared.IsAnimatorWindow(window.GetType().FullName))
                    {
                        trackedWindows.Add(window);
                        AnimatorWithKeyboardShared.Animator = window;

                        AnimatorWithKeyboardShared.RebuildGraphMethod = AnimatorWithKeyboardShared.Animator.GetType().GetMethod("RebuildGraph", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        if (AnimatorWithKeyboardShared.RebuildGraphMethod == null)
                        {
                            Debug.LogWarning("[AnimatorWithKeyboard] RebuildGraph not found in AnimatorControllerTool.");
                            return;
                        }
                    }
                }
            }
        }

        var removed = new HashSet<EditorWindow>(trackedWindows);
        removed.ExceptWith(currentSet);

        foreach (var closed in removed)
        {
            if (AnimatorWithKeyboardShared.IsAnimatorWindow(closed.GetType().FullName))
            {
                AnimatorWithKeyboardShared.Animator = null;
                _lastController = null;
                AnimatorWithKeyboardShared.AnimatorController = null;
            }
        }

        trackedWindows = currentSet;
    }

}
