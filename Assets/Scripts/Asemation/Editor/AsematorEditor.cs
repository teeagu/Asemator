using UnityEngine;
using UnityEditor;
using System.IO;

namespace Asemation
{
    [CustomEditor(typeof(Asemator))]
    public class AsematorEditor : Editor
    {
        Asemator asemator;

        void OnEnable()
        {
            asemator = (Asemator)target;
        }

        public override void OnInspectorGUI()
        {
            asemator.aseFile = EditorGUILayout.ObjectField("Aseprite File", asemator.aseFile, typeof(Object), false);
            if (asemator.aseFile) asemator.pixelPerUnit = EditorGUILayout.IntField("Pixels Per Unit", asemator.pixelPerUnit);
            if (!asemator.aseFile) return;
            string path = AssetDatabase.GetAssetPath(asemator.aseFile.GetInstanceID());
            if (asemator.asePath != path) asemator.asePath = path;
        }
    }
}