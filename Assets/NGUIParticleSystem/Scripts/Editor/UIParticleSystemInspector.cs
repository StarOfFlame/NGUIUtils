/****************************************************************************
Copyright (c) 2014 dpull.com

http://www.dpull.com

ideas taken from:
    . The ocean spray in your face [Jeff Lander]
        http://www.double.co.nz/dust/col0798.pdf
    . Building an Advanced Particle System [John van der Burg]
        http://www.gamasutra.com/features/20000623/vanderburg_01.htm
    . LOVE game engine
        http://love2d.org/
****************************************************************************/

using UnityEditorInternal;
using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

[CanEditMultipleObjects]
[CustomEditor(typeof(UIParticleSystem), true)]
public class UIParticleSystemInspector : UIWidgetInspector
{
    class Cache
    {
        public Vector3 Postion;
        public Quaternion Rotation;
        public Vector3 Scale;
        public Dictionary<string, object> Items = new Dictionary<string, object>();
    }
    static Dictionary<int, Cache> CachedValues = new Dictionary<int, Cache>();

    object GetSerializedPropertyValue(SerializedObject so, FieldInfo filed)
    {
        var sp = so.FindProperty(filed.Name);
        if (sp == null)
            return null;

        if (filed.FieldType == typeof(int))
        {
            return sp.intValue;
        }
        else if (filed.FieldType == typeof(float))
        {
            return sp.floatValue;
        }
        else if (filed.FieldType == typeof(Vector2))
        {
            return sp.vector2Value;
        }
        else if (filed.FieldType == typeof(Vector3))
        {
            return sp.vector3Value;
        }
        else if (filed.FieldType == typeof(Vector4))
        {
            return sp.vector4Value;
        }                    
        else if (filed.FieldType == typeof(bool))
        {
            return sp.boolValue;
        }
        else if (filed.FieldType == typeof(string))
        {
            return sp.stringValue;
        }
        else if (filed.FieldType == typeof(Color))
        {
            return sp.colorValue;
        }
        else if (filed.FieldType.IsEnum)
        {
            return System.Enum.ToObject(filed.FieldType, sp.enumValueIndex);
        }
        else if(filed.FieldType.IsSubclassOf(typeof(UnityEngine.Object)))
        {
            return sp.objectReferenceValue;
        }
        else
        {
            Debug.Log("Unkown type:" + filed.FieldType.ToString());
            return null;
        }
    }

    void SetSerializedPropertyValue(SerializedObject so, string name, object value)
    {
        var sp = so.FindProperty(name);
        if (sp == null)
            return;

        var valueType = value.GetType();
        if (valueType == typeof(int))
        {
            sp.intValue = (int)value;
        }
        else if (valueType == typeof(float))
        {
            sp.floatValue = (float)value;;
        }
        else if (valueType == typeof(Vector2))
        {
            sp.vector2Value = (Vector2)value;;
        }
        else if (valueType == typeof(Vector3))
        {
            sp.vector3Value = (Vector3)value;;
        }
        else if (valueType == typeof(Vector4))
        {
            sp.vector4Value = (Vector4)value;;
        }                    
        else if (valueType == typeof(bool))
        {
            sp.boolValue = (bool)value;;
        }
        else if (valueType == typeof(string))
        {
            sp.stringValue = (string)value;;
        }
        else if (valueType == typeof(Color))
        {
            sp.colorValue = (Color)value;;
        }
        else if (valueType.IsEnum)
        {
            sp.enumValueIndex = (int)value;
        }
        else if(valueType.IsSubclassOf(typeof(UnityEngine.Object)))
        {
            sp.objectReferenceValue = (UnityEngine.Object)value;
        }
        else
        {
            // Debug.Log("Unkown type:" + valueType.ToString());
        }
    }


    protected override void DrawCustomProperties ()
    {
        if (Application.isPlaying)
        {
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Start Particle"))
                (target as UIParticleSystem).ResetSystem();
            
            if(GUILayout.Button("Stop Particle"))
                (target as UIParticleSystem).StopSystem();
            GUILayout.EndHorizontal();

            if(GUILayout.Button("Save"))
            {    
                var cache = new Cache(); 
                var fileds = typeof(UIParticleSystem).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                foreach(var filed in fileds)
                {
                    if (filed.GetCustomAttributes(typeof(System.NonSerializedAttribute), true).Length != 0)
                        continue;

                    var value = GetSerializedPropertyValue(serializedObject, filed);
                    if (value != null)
                        cache.Items[filed.Name] = value;
                }

                var system = target as UIParticleSystem;
                cache.Postion = system.transform.localPosition; 
                cache.Rotation = system.transform.localRotation; 
                cache.Scale = system.transform.localScale; 

                CachedValues[target.GetInstanceID()] = cache;
            }
        }
        else
        {
            var id = target.GetInstanceID();
            Cache cache;
            if (CachedValues.TryGetValue(id, out cache))
            {
                var system = target as UIParticleSystem;
                system.transform.localPosition = cache.Postion; 
                system.transform.localRotation = cache.Rotation; 
                system.transform.localScale = cache.Scale; 

                foreach (var item in cache.Items)
                {
                    SetSerializedPropertyValue(serializedObject, item.Key, item.Value);
                }

                CachedValues.Remove(id);
            }
        }


        if (NGUIEditorTools.DrawHeader("Position"))
        {
            NGUIEditorTools.BeginContents();
            NGUIEditorTools.DrawProperty("Start postion", serializedObject, "SourcePosition");
            NGUIEditorTools.DrawProperty("±", serializedObject, "SourcePositionVariance");
            NGUIEditorTools.EndContents();
            NGUIEditorTools.DrawProperty("Space", serializedObject, "SimulationSpace");        
        }

        NGUIEditorTools.DrawProperty("Emit rate", serializedObject, "EmissionRate");        
        NGUIEditorTools.DrawProperty("Duration", serializedObject, "Duration");
        NGUIEditorTools.DrawProperty("Total particles", serializedObject, "MaxParticles");
        
        GUILayout.BeginHorizontal();
        NGUIEditorTools.DrawProperty("Life", serializedObject, "ParticleLifespan");
        NGUIEditorTools.DrawProperty("±", serializedObject, "ParticleLifespanVariance");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        NGUIEditorTools.DrawProperty("Start size", serializedObject, "StartParticleSize");
        NGUIEditorTools.DrawProperty("±", serializedObject, "StartParticleSizeVariance");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();        
        NGUIEditorTools.DrawProperty("End Size", serializedObject, "FinishParticleSize");
        NGUIEditorTools.DrawProperty("±", serializedObject, "FinishParticleSizeVariance");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();        
        NGUIEditorTools.DrawProperty("Start spin", serializedObject, "RotationStart");
        NGUIEditorTools.DrawProperty("±", serializedObject, "RotationStartVariance");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();    
        NGUIEditorTools.DrawProperty("End spin", serializedObject, "RotationEnd");
        NGUIEditorTools.DrawProperty("±", serializedObject, "RotationEndVariance");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();    
        NGUIEditorTools.DrawProperty("Angle", serializedObject, "Angle");
        NGUIEditorTools.DrawProperty("±", serializedObject, "AngleVariance");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();    
        NGUIEditorTools.DrawProperty("Start color", serializedObject, "StartColor");
        NGUIEditorTools.DrawProperty("±", serializedObject, "StartColorVariance");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();    
        NGUIEditorTools.DrawProperty("End color", serializedObject, "FinishColor");
        NGUIEditorTools.DrawProperty("±", serializedObject, "FinishColorVariance");
        GUILayout.EndHorizontal();        

        var sp = NGUIEditorTools.DrawProperty("Mode", serializedObject, "EmitterType");
        switch ((UIParticleMode)sp.enumValueIndex)
        {
        case UIParticleMode.Gravity:
            NGUIEditorTools.DrawProperty("Gravity", serializedObject, "Gravity");

            GUILayout.BeginHorizontal();    
            NGUIEditorTools.DrawProperty("Speed", serializedObject, "Speed");
            NGUIEditorTools.DrawProperty("±", serializedObject, "SpeedVariance");
            GUILayout.EndHorizontal();        

            GUILayout.BeginHorizontal();    
            NGUIEditorTools.DrawProperty("Tang. acc", serializedObject, "TangentialAcceleration");
            NGUIEditorTools.DrawProperty("±", serializedObject, "TangentialAccelVariance");
            GUILayout.EndHorizontal();    

            GUILayout.BeginHorizontal();    
            NGUIEditorTools.DrawProperty("Radia acc", serializedObject, "RadialAcceleration");
            NGUIEditorTools.DrawProperty("±", serializedObject, "RadialAccelVariance");
            GUILayout.EndHorizontal();

            NGUIEditorTools.DrawProperty("IsDir", serializedObject, "RotationIsDir");
            break;
            
        case UIParticleMode.Radius:
            GUILayout.BeginHorizontal();    
            NGUIEditorTools.DrawProperty("Start radius", serializedObject, "StartRadius");
            NGUIEditorTools.DrawProperty("±", serializedObject, "StartRadiusVariance");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();    
            NGUIEditorTools.DrawProperty("End radius", serializedObject, "FinishRadius");
            NGUIEditorTools.DrawProperty("±", serializedObject, "FinishRadiusVariance");
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();    
            NGUIEditorTools.DrawProperty("Rotate", serializedObject, "RotatePerSecond");
            NGUIEditorTools.DrawProperty("±", serializedObject, "RotatePerSecondVariance");
            GUILayout.EndHorizontal();
            break;
        }

        if (NGUIEditorTools.DrawHeader("Particle sprite"))
        {
            NGUIEditorTools.BeginContents();
            ShowParticleSprite();
            base.DrawCustomProperties();
            NGUIEditorTools.EndContents();
        }
    }

    // copy from UISpriteInspector
    void ShowParticleSprite ()
    {
        GUILayout.BeginHorizontal();
        if (NGUIEditorTools.DrawPrefixButton("Atlas"))
            ComponentSelector.Show<UIAtlas>(obj=>{
                serializedObject.Update();
                SerializedProperty spNew = serializedObject.FindProperty("Atlas");
                spNew.objectReferenceValue = obj;
                serializedObject.ApplyModifiedProperties();
                NGUITools.SetDirty(serializedObject.targetObject);
                NGUISettings.atlas = obj as UIAtlas;
            });
        SerializedProperty atlas = NGUIEditorTools.DrawProperty("", serializedObject, "Atlas", GUILayout.MinWidth(20f));
        
        if (GUILayout.Button("Edit", GUILayout.Width(40f)))
        {
            if (atlas != null)
            {
                UIAtlas atl = atlas.objectReferenceValue as UIAtlas;
                NGUISettings.atlas = atl;
                NGUIEditorTools.Select(atl.gameObject);
            }
        }
        GUILayout.EndHorizontal();
        
        SerializedProperty sp = serializedObject.FindProperty("SpriteName");
        NGUIEditorTools.DrawAdvancedSpriteField(atlas.objectReferenceValue as UIAtlas, sp.stringValue, spriteName =>{
            serializedObject.Update();
            SerializedProperty spNew = serializedObject.FindProperty("SpriteName");
            spNew.stringValue = spriteName;
            serializedObject.ApplyModifiedProperties();
            NGUITools.SetDirty(serializedObject.targetObject);
            NGUISettings.selectedSprite = spriteName;
        }, false);
    }
}
