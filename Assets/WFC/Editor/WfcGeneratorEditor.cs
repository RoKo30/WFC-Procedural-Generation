using UnityEditor;
using UnityEngine;

namespace ProceduralGen.Wfc.Editor
{
    [CustomEditor(typeof(WfcGenerator))]
    public sealed class WfcGeneratorEditor : UnityEditor.Editor
    {
        private const float PreviewSize = 160f;

        private SerializedProperty _input;
        private SerializedProperty _settings;

        private WfcGenerator Generator => (WfcGenerator)target;

        private void OnEnable()
        {
            _input = serializedObject.FindProperty("inputImage");
            _settings = serializedObject.FindProperty("settings");
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable() => EditorApplication.update -= OnEditorUpdate;

        // Update() does not run in edit mode, so the inspector drives animation there.
        private void OnEditorUpdate()
        {
            if (Generator == null || !Generator.IsAnimating) return;
            if (!Application.isPlaying) Generator.Tick();
            SceneView.RepaintAll();
            Repaint();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawInput();
            EditorGUILayout.Space();
            DrawSettings();
            EditorGUILayout.Space();
            DrawPropertiesExcluding(serializedObject, "m_Script", "inputImage", "settings");

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            DrawControls();
            DrawStatus();
        }

        private void DrawInput()
        {
            EditorGUILayout.PropertyField(_input);
            var texture = _input.objectReferenceValue as Texture2D;
            if (texture == null) return;

            if (WfcTextureImport.NeedsFix(texture))
            {
                EditorGUILayout.HelpBox(
                    "WFC needs exact pixel colours: Read/Write on, Point filter, no compression, no resizing, no mipmaps.",
                    MessageType.Warning);
                if (GUILayout.Button("Fix Import Settings"))
                    WfcTextureImport.Fix(texture);
            }
            else
            {
                EditorGUILayout.LabelField($"Input: {texture.width}×{texture.height} px", EditorStyles.miniLabel);
            }
        }

        private void DrawSettings()
        {
            SerializedProperty property = _settings.Copy();
            SerializedProperty end = property.GetEndProperty();
            int kernel = _settings.FindPropertyRelative("kernelSize").intValue;

            bool enterChildren = true;
            while (property.NextVisible(enterChildren) && !SerializedProperty.EqualContents(property, end))
            {
                enterChildren = false;
                if (property.name == "overlap") DrawOverlap(property, kernel);
                else EditorGUILayout.PropertyField(property, true);
            }
        }

        private static void DrawOverlap(SerializedProperty overlap, int kernel)
        {
            int max = Mathf.Max(1, kernel - 1);
            overlap.intValue = Mathf.Clamp(overlap.intValue, 1, max);

            using (new EditorGUI.DisabledScope(max == 1))
                EditorGUILayout.IntSlider(overlap, 1, max, new GUIContent("Overlap", overlap.tooltip));

            int stride = kernel - overlap.intValue;
            string unit = stride == 1 ? "column/row" : "columns/rows";
            EditorGUILayout.LabelField(" ",
                $"Neighbours share {overlap.intValue} of {kernel}; each new cell adds {stride} {unit}.",
                EditorStyles.miniLabel);
        }

        private void DrawControls()
        {
            WfcGenerator generator = Generator;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate")) generator.Generate();
                if (GUILayout.Button("Animate")) generator.BeginAnimated();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Step")) generator.StepOnce();
                using (new EditorGUI.DisabledScope(!generator.IsAnimating))
                    if (GUILayout.Button("Stop")) generator.Stop();
            }

            // The generator may write back a randomized seed.
            serializedObject.Update();
        }

        private void DrawStatus()
        {
            WfcGenerator generator = Generator;
            EditorGUILayout.HelpBox(generator.StatusMessage, MessageType.None);

            Texture2D output = generator.OutputTexture;
            if (output == null) return;

            float aspect = (float)output.height / output.width;
            float width = Mathf.Min(PreviewSize, EditorGUIUtility.currentViewWidth - 40f);
            Rect rect = GUILayoutUtility.GetRect(width, width * aspect, GUILayout.ExpandWidth(false));
            EditorGUI.DrawTextureTransparent(rect, output, ScaleMode.ScaleToFit);
        }
    }
}
