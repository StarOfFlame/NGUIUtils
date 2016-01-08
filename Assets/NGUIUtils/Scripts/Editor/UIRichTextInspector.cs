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
	public enum FontType
	{
		NGUI,
		Unity,
	}
	
	UIRichText mLabel;
	FontType mFontType;
	
	protected override void OnEnable ()
	{
		base.OnEnable();
		SerializedProperty bit = serializedObject.FindProperty("mFont");
		mFontType = (bit != null && bit.objectReferenceValue != null) ? FontType.NGUI : FontType.Unity;
	}
	
	void OnNGUIFont (Object obj)
	{
		serializedObject.Update();
		
		SerializedProperty sp = serializedObject.FindProperty("mFont");
		sp.objectReferenceValue = obj;
		
		sp = serializedObject.FindProperty("mTrueTypeFont");
		sp.objectReferenceValue = null;
		
		serializedObject.ApplyModifiedProperties();
		NGUISettings.ambigiousFont = obj;
	}
	
	void OnUnityFont (Object obj)
	{
		serializedObject.Update();
		
		SerializedProperty sp = serializedObject.FindProperty("mTrueTypeFont");
		sp.objectReferenceValue = obj;
		
		sp = serializedObject.FindProperty("mFont");
		sp.objectReferenceValue = null;
		
		serializedObject.ApplyModifiedProperties();
		NGUISettings.ambigiousFont = obj;
	}
	
	/// <summary>
	/// Draw the label's properties.
	/// </summary>
	
	protected override bool ShouldDrawProperties ()
	{
		mLabel = mWidget as UIRichText;
		
		GUILayout.BeginHorizontal();
		
		mFontType = (FontType)EditorGUILayout.EnumPopup(mFontType, "DropDown", GUILayout.Width(74f));
		if (NGUIEditorTools.DrawPrefixButton("Font", GUILayout.Width(64f)))
		{
			if (mFontType == FontType.NGUI)
			{
				ComponentSelector.Show<UIFont>(OnNGUIFont);
			}
			else
			{
				ComponentSelector.Show<Font>(OnUnityFont, new string[] { ".ttf", ".otf" });
			}
		}
		
		bool isValid = false;
		SerializedProperty fnt = null;
		SerializedProperty ttf = null;
		
		if (mFontType == FontType.NGUI)
		{
			GUI.changed = false;
			fnt = NGUIEditorTools.DrawProperty("", serializedObject, "mFont", GUILayout.MinWidth(40f));
			
			if (fnt.objectReferenceValue != null)
			{
				if (GUI.changed) serializedObject.FindProperty("mTrueTypeFont").objectReferenceValue = null;
				NGUISettings.ambigiousFont = fnt.objectReferenceValue;
				isValid = true;
			}
		}
		else
		{
			GUI.changed = false;
			ttf = NGUIEditorTools.DrawProperty("", serializedObject, "mTrueTypeFont", GUILayout.MinWidth(40f));
			
			if (ttf.objectReferenceValue != null)
			{
				if (GUI.changed) serializedObject.FindProperty("mFont").objectReferenceValue = null;
				NGUISettings.ambigiousFont = ttf.objectReferenceValue;
				isValid = true;
			}
		}
		
		GUILayout.EndHorizontal();
		
		if (mFontType == FontType.Unity)
		{
			EditorGUILayout.HelpBox("Dynamic fonts suffer from issues in Unity itself where your characters may disappear, get garbled, or just not show at times. Use this feature at your own risk.\n\n" +
			                        "When you do run into such issues, please submit a Bug Report to Unity via Help -> Report a Bug (as this is will be a Unity bug, not an NGUI one).", MessageType.Warning);
		}
		
		EditorGUI.BeginDisabledGroup(!isValid);
		{
			UIFont uiFont = (fnt != null) ? fnt.objectReferenceValue as UIFont : null;
			Font dynFont = (ttf != null) ? ttf.objectReferenceValue as Font : null;
			
			if (uiFont != null && uiFont.isDynamic)
			{
				dynFont = uiFont.dynamicFont;
				uiFont = null;
			}
			
			if (dynFont != null)
			{
				GUILayout.BeginHorizontal();
				{
					EditorGUI.BeginDisabledGroup((ttf != null) ? ttf.hasMultipleDifferentValues : fnt.hasMultipleDifferentValues);
					
					SerializedProperty prop = NGUIEditorTools.DrawProperty("Font Size", serializedObject, "mFontSize", GUILayout.Width(142f));
					NGUISettings.fontSize = prop.intValue;
					
					prop = NGUIEditorTools.DrawProperty("", serializedObject, "mFontStyle", GUILayout.MinWidth(40f));
					NGUISettings.fontStyle = (FontStyle)prop.intValue;
					
					NGUIEditorTools.DrawPadding();
					EditorGUI.EndDisabledGroup();
				}
				GUILayout.EndHorizontal();
				
				NGUIEditorTools.DrawProperty("Material", serializedObject, "mMaterial");
			}
			else if (uiFont != null)
			{
				GUILayout.BeginHorizontal();
				SerializedProperty prop = NGUIEditorTools.DrawProperty("Font Size", serializedObject, "mFontSize", GUILayout.Width(142f));
				NGUISettings.fontSize = prop.intValue;
				GUILayout.EndHorizontal();
			}
			
			bool ww = GUI.skin.textField.wordWrap;
			GUI.skin.textField.wordWrap = true;
			SerializedProperty sp = serializedObject.FindProperty("mText");

			if (sp.hasMultipleDifferentValues)
			{
				NGUIEditorTools.DrawProperty("", sp, GUILayout.Height(128f));
			}
			else
			{
				GUIStyle style = new GUIStyle(EditorStyles.textField);
				style.wordWrap = true;
				
				float height = style.CalcHeight(new GUIContent(sp.stringValue), Screen.width - 100f);
				bool offset = true;
				
				if (height > 90f)
				{
					offset = false;
					height = style.CalcHeight(new GUIContent(sp.stringValue), Screen.width - 20f);
				}
				else
				{
					GUILayout.BeginHorizontal();
					GUILayout.BeginVertical(GUILayout.Width(76f));
					GUILayout.Space(3f);
					GUILayout.Label("Text");
					GUILayout.EndVertical();
					GUILayout.BeginVertical();
				}
				Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(height));
				
				GUI.changed = false;
				string text = EditorGUI.TextArea(rect, sp.stringValue, style);
				if (GUI.changed) sp.stringValue = text;
				
				if (offset)
				{
					GUILayout.EndVertical();
					GUILayout.EndHorizontal();
				}
			}
			
			GUI.skin.textField.wordWrap = ww;

			NGUIEditorTools.DrawProperty("Resize hight", serializedObject, "mResizeHight");
			
			GUILayout.BeginHorizontal();
			GUILayout.Label("Effect", GUILayout.Width(76f));
			sp = NGUIEditorTools.DrawProperty("", serializedObject, "mEffectStyle", GUILayout.MinWidth(16f));
			
			EditorGUI.BeginDisabledGroup(!sp.hasMultipleDifferentValues && !sp.boolValue);
			{
				NGUIEditorTools.DrawProperty("", serializedObject, "mEffectColor", GUILayout.MinWidth(10f));
				GUILayout.EndHorizontal();
				
				GUILayout.BeginHorizontal();
				{
					GUILayout.Label(" ", GUILayout.Width(56f));
					NGUIEditorTools.SetLabelWidth(20f);
					NGUIEditorTools.DrawProperty("X", serializedObject, "mEffectDistance.x", GUILayout.MinWidth(40f));
					NGUIEditorTools.DrawProperty("Y", serializedObject, "mEffectDistance.y", GUILayout.MinWidth(40f));
					NGUIEditorTools.DrawPadding();
					NGUIEditorTools.SetLabelWidth(80f);
				}
			}
			EditorGUI.EndDisabledGroup();
			GUILayout.EndHorizontal();
			
			sp = NGUIEditorTools.DrawProperty("Float spacing", serializedObject, "mUseFloatSpacing", GUILayout.Width(100f));
			
			if (!sp.boolValue)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label("Spacing", GUILayout.Width(56f));
				NGUIEditorTools.SetLabelWidth(20f);
				NGUIEditorTools.DrawProperty("X", serializedObject, "mSpacingX", GUILayout.MinWidth(40f));
				NGUIEditorTools.DrawProperty("Y", serializedObject, "mSpacingY", GUILayout.MinWidth(40f));
				NGUIEditorTools.DrawPadding();
				NGUIEditorTools.SetLabelWidth(80f);
				GUILayout.EndHorizontal();
			}
			else
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label("Spacing", GUILayout.Width(56f));
				NGUIEditorTools.SetLabelWidth(20f);
				NGUIEditorTools.DrawProperty("X", serializedObject, "mFloatSpacingX", GUILayout.MinWidth(40f));
				NGUIEditorTools.DrawProperty("Y", serializedObject, "mFloatSpacingY", GUILayout.MinWidth(40f));
				NGUIEditorTools.DrawPadding();
				NGUIEditorTools.SetLabelWidth(80f);
				GUILayout.EndHorizontal();
			}
		}
		EditorGUI.EndDisabledGroup();
		return isValid;
	}
}
