using UnityEditor;
using UnityEngine;

public class AnimatorWithKeyboardWindow : EditorWindow
{
    private const string MoveStepKey = "AnimatorWithKeyboard.MoveStep";
    private float moveStep;

    [MenuItem("TohruTheDragon/Animator With Keyboard")]
    public static void ShowWindow()
    {
        GetWindow<AnimatorWithKeyboardWindow>("Animator With Keyboard Settings");
    }

    private void OnEnable()
    {
        moveStep = EditorPrefs.GetFloat(MoveStepKey, 10f);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Animator State Movement", EditorStyles.boldLabel);

        moveStep = EditorGUILayout.FloatField("Move Step (units)", moveStep);

        if (moveStep < 0.1f)
            moveStep = 0.1f;

        if (GUI.changed)
        {
            EditorPrefs.SetFloat(MoveStepKey, moveStep);
        }
    }

    public static float GetMoveStep() => EditorPrefs.GetFloat(MoveStepKey, 10f);
}