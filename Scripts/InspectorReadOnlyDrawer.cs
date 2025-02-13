using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

public class InspectorReadOnlyAttribute : PropertyAttribute { }

[CanEditMultipleObjects]
[CustomEditor(typeof(MonoBehaviour), true)]
public class DefaultReadOnlyEditor : Editor
{
    static Dictionary<Object, Dictionary<MemberInfo, (object value, float time, bool changed)>> cache
        = new Dictionary<Object, Dictionary<MemberInfo, (object, float, bool)>>();

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        foreach (var o in targets)
        {
            if (!cache.ContainsKey(o))
                cache[o] = new Dictionary<MemberInfo, (object, float, bool)>();

            var members = o.GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var m in members)
            {
                if (m.GetCustomAttribute<InspectorReadOnlyAttribute>() == null) continue;
                EditorGUI.BeginDisabledGroup(true);

                object val = null;
                if (m is FieldInfo f) val = f.GetValue(o);
                else if (m is PropertyInfo p) val = p.GetValue(o);

                if (!cache[o].TryGetValue(m, out var old))
                    cache[o][m] = (val, (float)EditorApplication.timeSinceStartup, false);

                var now = (float)EditorApplication.timeSinceStartup;
                bool changed = !AreEqual(old.value, val);
                cache[o][m] = (val, now, changed);

                EditorGUILayout.LabelField(m.Name, val == null ? "null" : val.ToString());

                if (now - old.time < 2)
                {
                    var style = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.green } };
                    EditorGUILayout.LabelField(changed ? "Value updated" : "Value updated (but not changed)", style);
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
