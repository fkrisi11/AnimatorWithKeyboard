using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class AnimatorWithKeyboardShared
{
    private static string[] AnimatorWindowNames = new string[] { "UnityEditor.Graphs.AnimatorControllerTool, UnityEditor.Graphs", "AnimatorControllerTool", "AnimatorWindow" };
    public static EditorWindow Animator = null;
    public static AnimatorController AnimatorController = null;
    public static MethodInfo RebuildGraphMethod = null;

    public static AnimatorController GetController()
    {
        if (Animator == null) return null;

        FieldInfo controllerField = Animator.GetType()
        .GetField("m_AnimatorController", BindingFlags.NonPublic | BindingFlags.Instance);

        if (controllerField == null) return null;

        AnimatorController controller = controllerField.GetValue(Animator) as AnimatorController;
        return controller;
    }

    public static bool IsAnimatorFocused()
    {
        if (Animator == null) return false;
        if (Animator.hasFocus) return true;

        return false;
    }

    public static bool IsAnimatorWindow(string nameToSearch)
    {
        if (nameToSearch == null) return false;

        for (int i = 0; i < AnimatorWindowNames.Length; i++)
        {
            if (AnimatorWindowNames[i].Trim().ToLower().Contains(nameToSearch.Trim().ToLower()))
            {
                return true;
            }
        }

        return false;
    }

    public static int GetSelectedLayerIndex()
    {
        if (Animator == null) return -1;

        if (Animator == null || Animator.GetType().Name != "AnimatorControllerTool")
            return -1;

        try
        {
            var type = Animator.GetType();

            // Try getting selectedLayerIndex property directly
            var prop = type.GetProperty("selectedLayerIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null && prop.PropertyType == typeof(int))
            {
                return (int)prop.GetValue(Animator);
            }

            // Fallback: access m_LayerEditor and its selectedLayerIndex
            var field = type.GetField("m_LayerEditor", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                object layerEditor = field.GetValue(Animator);
                if (layerEditor != null)
                {
                    var layerEditorType = layerEditor.GetType();
                    var indexProp = layerEditorType.GetProperty("selectedLayerIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (indexProp != null)
                    {
                        return (int)indexProp.GetValue(layerEditor);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[AnimatorWithKeyboard] Failed to get selected layer index: " + ex.Message);
        }

        return -1;
    }


    public static AnimatorControllerLayer GetSelectedLayer()
    {
        if (GetSelectedLayerIndex() == -1) return null;

        if (AnimatorController != null)
        {
            return AnimatorController.layers[GetSelectedLayerIndex()];
        }
        else
        {
            return null;
        }
    }

    public static void RebuildGraph(bool updateSelection = false)
    {
        if (Animator == null) return;
        if (RebuildGraphMethod == null) return;

        // Try calling RebuildGraph(bool) directly on the AnimatorControllerTool window
        RebuildGraphMethod.Invoke(Animator, new object[] { updateSelection });
    }

    public static List<AnimatorState> GetSelectedStates(bool includeNested = true)
    {
        AnimatorControllerLayer layer = GetSelectedLayer();
        if (layer == null) return null;

        var stateMachine = layer.stateMachine;

        var selectedStates = new List<AnimatorState>();

        foreach (var childState in stateMachine.states)
        {
            if (Selection.Contains(childState.state))
                selectedStates.Add(childState.state);
        }

        return selectedStates;
    }
}
