using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

[InitializeOnLoad]
public static class AnimatorWithKeyboardHotkeys
{
    private static readonly HashSet<KeyCode> activeKeys = new();
    private static bool undoGroupStarted = false;
    private static int currentUndoGroup = -1;
    private static double lastMoveTime = 0;
    private const double MoveThrottle = 0.016; // ~60 FPS

    static AnimatorWithKeyboardHotkeys()
    {
        HookGlobalEventHandler();
    }

    private static void HookGlobalEventHandler()
    {
        var field = typeof(EditorApplication).GetField("globalEventHandler", BindingFlags.Static | BindingFlags.NonPublic);
        if (field == null)
        {
            Debug.LogWarning("[AnimatorWithKeyboard] Could not find globalEventHandler.");
            return;
        }

        EditorApplication.CallbackFunction existing = (EditorApplication.CallbackFunction)field.GetValue(null);
        EditorApplication.CallbackFunction newHandler = () =>
        {
            existing?.Invoke();
            HandleGlobalKeyEvents();
        };

        field.SetValue(null, newHandler);
    }

    private static void HandleGlobalKeyEvents()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !AnimatorWithKeyboardShared.IsAnimatorFocused())
            return;

        Event e = Event.current;
        if (e == null || (e.type != EventType.KeyDown && e.type != EventType.KeyUp))
            return;

        KeyCode key = e.keyCode;
        bool isArrow = key is KeyCode.LeftArrow or KeyCode.RightArrow or KeyCode.UpArrow or KeyCode.DownArrow or KeyCode.LeftAlt;
        if (!isArrow)
            return;

        if (e.type == EventType.KeyDown)
            activeKeys.Add(key);
        else if (e.type == EventType.KeyUp)
            activeKeys.Remove(key);

        // When all keys are released, finalize undo and flush changes
        if (activeKeys.Count == 0 && undoGroupStarted)
        {
            Undo.CollapseUndoOperations(currentUndoGroup);
            undoGroupStarted = false;
            currentUndoGroup = -1;

            AnimatorWithKeyboardStateMover.FlushChanges();
            return;
        }

        // Determine movement vector
        Vector2 offset = Vector2.zero;
        float step = AnimatorWithKeyboardWindow.GetMoveStep();
        if (activeKeys.Contains(KeyCode.LeftArrow)) offset.x -= step;
        if (activeKeys.Contains(KeyCode.RightArrow)) offset.x += step;
        if (activeKeys.Contains(KeyCode.UpArrow)) offset.y -= step;
        if (activeKeys.Contains(KeyCode.DownArrow)) offset.y += step;

        if (offset == Vector2.zero)
            return;

        // Throttle movement speed for smoother visuals
        double now = EditorApplication.timeSinceStartup;
        if (now - lastMoveTime < MoveThrottle)
            return;

        lastMoveTime = now;

        // Begin undo group if needed
        if (!undoGroupStarted)
        {
            currentUndoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Move Animator Nodes");
            undoGroupStarted = true;
        }

        AnimatorWithKeyboardStateMover.MoveSelectedStates(offset);
        e.Use();
    }

}
