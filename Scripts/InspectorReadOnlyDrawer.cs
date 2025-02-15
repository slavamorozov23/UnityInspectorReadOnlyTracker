using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

public class InspectorReadOnlyAttribute : PropertyAttribute { }

[CanEditMultipleObjects]
[CustomEditor(typeof(MonoBehaviour), true)]
public class DefaultReadOnlyEditor : Editor
{
    static Dictionary<Object, Dictionary<MemberInfo, (object value, float changeTime, bool changed)>> cache
        = new Dictionary<Object, Dictionary<MemberInfo, (object, float, bool)>>();
    static Dictionary<System.Type, MemberInfo[]> memberCache = new Dictionary<System.Type, MemberInfo[]>();

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        foreach (var o in targets)
        {
            if (!cache.ContainsKey(o))
                cache[o] = new Dictionary<MemberInfo, (object, float, bool)>();

            var type = o.GetType();
            if (!memberCache.TryGetValue(type, out var members))
            {
                members = type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                memberCache[type] = members;
            }

            foreach (var m in members)
            {
                if (m.GetCustomAttribute<InspectorReadOnlyAttribute>() == null) continue;
                EditorGUI.BeginDisabledGroup(true);

                object val = m is FieldInfo f ? f.GetValue(o) : (m is PropertyInfo p ? p.GetValue(o) : null);
                float now = (float)EditorApplication.timeSinceStartup;
                if (!cache[o].TryGetValue(m, out var record))
                {
                    record = (val, now, false);
                    cache[o][m] = record;
                }
                else
                {
                    if (!AreEqual(record.value, val))
                    {
                        if (!record.changed)
                            record = (val, now, true);
                        else
                            record = (val, record.changeTime, true);
                        cache[o][m] = record;
                    }
                    else if (record.changed && now - record.changeTime >= 2f)
                    {
                        record = (val, record.changeTime, false);
                        cache[o][m] = record;
                    }
                }

                EditorGUILayout.LabelField(m.Name, val == null ? "null" : val.ToString());
                if (cache[o][m].changed && now - cache[o][m].changeTime < 2f)
                {
                    var style = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.green } };
                    EditorGUILayout.LabelField("Value updated", style);
                }
                EditorGUI.EndDisabledGroup();
            }
        }
        if (EditorApplication.isPlaying) Repaint();
    }

    static bool AreEqual(object a, object b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        return a.Equals(b);
    }
}
