using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

public static class AnimatorWithKeyboardStateMover
{
    private static bool _dirtyPending = false;
    private static bool _graphRebuildPending = false;

    public static void MoveSelectedStates(Vector2 offset)
    {
        if (AnimatorWithKeyboardShared.AnimatorController == null) return;
        if (AnimatorWithKeyboardShared.AnimatorController.layers.Length == 0) return;

        MoveSelectedObjects(AnimatorWithKeyboardShared.AnimatorController, AnimatorWithKeyboardShared.GetSelectedLayer(), offset);

        _dirtyPending = true;
        _graphRebuildPending = true;
    }

    private static void MoveSelectedObjects(AnimatorController controller, AnimatorControllerLayer layer, Vector2 moveAmount)
    {
        if (controller == null) return;

        AnimatorStateMachine sm = layer.stateMachine;
        MoveInStateMachine(sm, moveAmount, null);
    }

    private static void MoveInStateMachine(AnimatorStateMachine sm, Vector2 move, AnimatorStateMachine parent)
    {
        var states = sm.states;
        bool anyChanged = false;

        // States
        for (int i = 0; i < sm.states.Length; i++)
        {
            var state = sm.states[i];
            if (Selection.Contains(state.state))
            {
                Undo.RecordObject(sm, "Move State");
                state.position += (Vector3)move;
                states[i] = state;
                anyChanged = true;
            }
        }

        if (anyChanged)
        {
            sm.states = states;
            EditorUtility.SetDirty(sm);
        }

        var subs = sm.stateMachines;
        bool changed = false;

        // Sub-StateMachines
        for (int i = 0; i < sm.stateMachines.Length; i++)
        {
            var sub = sm.stateMachines[i];
            if (Selection.Contains(sub.stateMachine))
            {
                Undo.RecordObject(sm, "Move Sub-StateMachine");
                sub.position += (Vector3)move;
                subs[i] = sub;
                changed = true;
            }

            MoveInStateMachine(sub.stateMachine, move, sm);
        }

        if (changed)
        {
            sm.stateMachines = subs;
            EditorUtility.SetDirty(sm);
        }

        bool movedAnyDefaultNode = false;

        // Entry/Exit/Any State nodes
        if (IsPseudoNodeSelected("EntryNode"))
        {
            Undo.RecordObject(sm, "Move Entry Node");
            sm.entryPosition += (Vector3)move;
            movedAnyDefaultNode = true;
        }

        if (IsPseudoNodeSelected("ExitNode"))
        {
            Undo.RecordObject(sm, "Move Exit Node");
            sm.exitPosition += (Vector3)move;
            movedAnyDefaultNode = true;
        }

        if (IsPseudoNodeSelected("AnyStateNode"))
        {
            Undo.RecordObject(sm, "Move Any State Node");
            sm.anyStatePosition += (Vector3)move;
            movedAnyDefaultNode = true;
        }

        if (parent != null && Selection.Contains(parent))
        {
            Undo.RecordObject(sm, "Move Parent StateMachine Node");
            sm.parentStateMachinePosition += (Vector3)move;
            movedAnyDefaultNode = true;
        }

        if (movedAnyDefaultNode)
        {
            var previousSelection = Selection.objects.ToArray();
            AnimatorWithKeyboardShared.RebuildGraph(true);
            Selection.objects = previousSelection;
        }
    }

    private static bool IsPseudoNodeSelected(string pseudoTypeName)
    {
        foreach (var obj in Selection.objects)
        {
            if (obj == null) continue;

            var type = obj.GetType();
            if (type.Name == pseudoTypeName || type.FullName.Contains($"Graphs.{pseudoTypeName}"))
            {
                return true;
            }
        }

        return false;
    }

    public static void FlushChanges()
    {
        if (_dirtyPending)
        {
            if (AnimatorWithKeyboardShared.AnimatorController != null)
                EditorUtility.SetDirty(AnimatorWithKeyboardShared.AnimatorController);

            _dirtyPending = false;
        }

        if (_graphRebuildPending)
            _graphRebuildPending = false;
    }

}
