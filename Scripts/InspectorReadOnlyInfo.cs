using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

public class InspectorReadOnlyInfo : MonoBehaviour { }

[CustomEditor(typeof(InspectorReadOnlyInfo))]
public class InspectorReadOnlyInfoEditor : Editor
{
    static Dictionary<MonoBehaviour, Dictionary<MemberInfo, (object value, float lastChangeTime, bool showMessage)>> valueCache
        = new Dictionary<MonoBehaviour, Dictionary<MemberInfo, (object, float, bool)>>();

    static Dictionary<MonoBehaviour, bool> foldouts = new Dictionary<MonoBehaviour, bool>();

    static Dictionary<System.Type, MemberInfo[]> memberCache = new Dictionary<System.Type, MemberInfo[]>();

    static bool displayUpdateMessages = true;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        displayUpdateMessages = EditorGUILayout.Toggle("Display update messages", displayUpdateMessages);
        float now = (float)EditorApplication.timeSinceStartup;
        var allMono = FindObjectsOfType<MonoBehaviour>();
        var validObjects = new List<MonoBehaviour>();
        foreach (var mb in allMono)
        {
            var members = GetReadOnlyMembers(mb);
            if (members.Length > 0)
            {
                validObjects.Add(mb);
                if (!valueCache.ContainsKey(mb))
                    valueCache[mb] = new Dictionary<MemberInfo, (object, float, bool)>();
                if (!foldouts.ContainsKey(mb))
                    foldouts[mb] = false;
            }
        }
        var keys = new List<MonoBehaviour>(valueCache.Keys);
        foreach (var mb in keys)
        {
            if (!validObjects.Contains(mb))
            {
                valueCache.Remove(mb);
                foldouts.Remove(mb);
            }
        }
        EditorGUILayout.Space();
        foreach (var mb in validObjects)
        {
            if (mb == null) continue;
            foldouts[mb] = EditorGUILayout.Foldout(foldouts[mb], $"{mb.name} ({mb.GetType().Name})", true);
            if (!foldouts[mb]) continue;
            EditorGUI.indentLevel++;
            var members = GetReadOnlyMembers(mb);
            foreach (var member in members)
            {
                object currentValue = GetMemberValue(member, mb);
                if (!valueCache[mb].TryGetValue(member, out var record))
                {
                    record = (currentValue, now, false);
                    valueCache[mb][member] = record;
                }
                bool changed = !Equals(record.value, currentValue);
                if (changed)
                {
                    record = (currentValue, now, true);
                    valueCache[mb][member] = record;
                }
                else if (record.showMessage && (now - record.lastChangeTime >= 2f))
                {
                    record = (record.value, record.lastChangeTime, false);
                    valueCache[mb][member] = record;
                }
                EditorGUILayout.LabelField(member.Name, currentValue == null ? "null" : currentValue.ToString());
                if (displayUpdateMessages && record.showMessage && (now - record.lastChangeTime < 2f))
                {
                    var style = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.green } };
                    EditorGUILayout.LabelField("Value updated", style);
                }
            }
            EditorGUI.indentLevel--;
        }
        if (EditorApplication.isPlaying) Repaint();
    }

    private MemberInfo[] GetReadOnlyMembers(MonoBehaviour mb)
    {
        var type = mb.GetType();
        if (memberCache.TryGetValue(type, out var members))
            return members;
        var allMembers = type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var list = new List<MemberInfo>();
        foreach (var member in allMembers)
        {
            if (member.GetCustomAttribute<InspectorReadOnlyAttribute>() != null)
                list.Add(member);
        }
        members = list.ToArray();
        memberCache[type] = members;
        return members;
    }

    private object GetMemberValue(MemberInfo member, MonoBehaviour mb)
    {
        if (member is FieldInfo field) return field.GetValue(mb);
        if (member is PropertyInfo prop) return prop.GetValue(mb);
        return null;
    }
}
