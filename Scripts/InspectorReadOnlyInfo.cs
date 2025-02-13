using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

public class InspectorReadOnlyInfo : MonoBehaviour { }

[CustomEditor(typeof(InspectorReadOnlyInfo))]
public class InspectorReadOnlyInfoEditor : Editor
{
    static Dictionary<MonoBehaviour, Dictionary<MemberInfo, (object value, float lastChangeTime, bool showMessage)>> storage
        = new Dictionary<MonoBehaviour, Dictionary<MemberInfo, (object, float, bool)>>();

    static Dictionary<MonoBehaviour, bool> foldouts = new Dictionary<MonoBehaviour, bool>();

    static Dictionary<System.Type, MemberInfo[]> memberCache = new Dictionary<System.Type, MemberInfo[]>();

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        float now = (float)EditorApplication.timeSinceStartup;
        var allMono = FindObjectsOfType<MonoBehaviour>();
        var validObjects = new List<MonoBehaviour>();
        foreach (var m in allMono)
        {
            var members = GetInspectorReadOnlyMembers(m);
            if (members.Length > 0)
            {
                validObjects.Add(m);
                if (!storage.ContainsKey(m))
                    storage[m] = new Dictionary<MemberInfo, (object, float, bool)>();
                if (!foldouts.ContainsKey(m))
                    foldouts[m] = false;
            }
        }
        var keys = new List<MonoBehaviour>(storage.Keys);
        foreach (var m in keys)
        {
            if (!validObjects.Contains(m))
            {
                storage.Remove(m);
                foldouts.Remove(m);
            }
        }

        EditorGUILayout.Space();
        foreach (var m in validObjects)
        {
            if (m == null) continue;
            foldouts[m] = EditorGUILayout.Foldout(foldouts[m], $"{m.name} ({m.GetType().Name})", true);
            if (!foldouts[m]) continue;
            EditorGUI.indentLevel++;
            var members = GetInspectorReadOnlyMembers(m);
            foreach (var mm in members)
            {
                object currentValue = GetMemberValue(mm, m);
                if (!storage[m].TryGetValue(mm, out var record))
                {
                    record = (currentValue, now, false);
                    storage[m][mm] = record;
                }
                bool changed = !Equals(record.value, currentValue);
                if (changed)
                {
                    record = (currentValue, now, true);
                    storage[m][mm] = record;
                }
                else if (record.showMessage && (now - record.lastChangeTime >= 2f))
                {
                    record = (record.value, record.lastChangeTime, false);
                    storage[m][mm] = record;
                }
                EditorGUILayout.LabelField(mm.Name, currentValue == null ? "null" : currentValue.ToString());
                if (record.showMessage && (now - record.lastChangeTime < 2f))
                {
                    var style = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.green } };
                    EditorGUILayout.LabelField("Value updated", style);
                }
            }
            EditorGUI.indentLevel--;
        }
        if (EditorApplication.isPlaying) Repaint();
    }

    private MemberInfo[] GetInspectorReadOnlyMembers(MonoBehaviour m)
    {
        var type = m.GetType();
        if (memberCache.TryGetValue(type, out var members))
            return members;

        var allMembers = type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var list = new List<MemberInfo>();
        foreach (var mm in allMembers)
        {
            if (mm.GetCustomAttribute<InspectorReadOnlyAttribute>() != null)
                list.Add(mm);
        }
        members = list.ToArray();
        memberCache[type] = members;
        return members;
    }

    private object GetMemberValue(MemberInfo mm, MonoBehaviour m)
    {
        if (mm is FieldInfo fi)
            return fi.GetValue(m);
        else if (mm is PropertyInfo pi)
            return pi.GetValue(m);
        return null;
    }
}
