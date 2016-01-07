using UnityEditorInternal;
using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

[CanEditMultipleObjects]
[CustomEditor(typeof(UIRichText), true)]
public class UIRichTextInspector : UIWidgetInspector
{
	string LastText;
    protected override void DrawCustomProperties ()
    {        
		base.DrawCustomProperties();
		var sp = NGUIEditorTools.DrawProperty("Text", serializedObject, "mText"); 
		if (LastText != sp.stringValue) 
		{
			LastText = sp.stringValue;
			((UIRichText)target).Text = LastText;
		}

		NGUIEditorTools.DrawProperty("AutoHight", serializedObject, "AutoHight");        
    }
}
