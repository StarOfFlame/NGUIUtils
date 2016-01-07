using UnityEditorInternal;
using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

[CanEditMultipleObjects]
[CustomEditor(typeof(UIRichText), true)]
public class UIRichTextInspector : Editor
{
	public override void OnInspectorGUI()
    {        
		var target = (UIRichText)this.target;
		target.Text = EditorGUILayout.TextField ("Text", target.Text);
		target.AutoHight = EditorGUILayout.Toggle ("AutoHight", target.AutoHight);
    }
}
