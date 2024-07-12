using UnityEngine;

namespace VertexAnimation.Editor
{
    using UnityEditor;
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MaterialAnimationController))]
    public class MaterialAnimationControllerEditor : Editor
    {
        private void OnEnable()
        {
            // Subscribe to the update event
            EditorApplication.update += UpdateProgress;
        }

        private void OnDisable()
        {
            // Unsubscribe from the update event
            EditorApplication.update -= UpdateProgress;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MaterialAnimationController controller = (MaterialAnimationController)target;

            EditorGUILayout.Space();
            SerializedProperty PlayAtStart = serializedObject.FindProperty("playAtStart");
            EditorGUILayout.PropertyField(PlayAtStart, true);
            EditorGUILayout.Space();

            // Toggle Play/Pause button
            if (GUILayout.Button(controller.play ? "Pause" : "Play"))
            {
                if (controller.play)
                {
                    controller.Pause();
                }
                else
                {
                    controller.Play();
                }
            }

            if (GUILayout.Button("Stop"))
            {
                controller.Stop();
            }

            if (GUILayout.Button(controller.loop ? "Disable Loop" : "Enable Loop"))
            {
                controller.ToggleLoop();
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Use Single Animation Field", EditorStyles.boldLabel);
            SerializedProperty useSingleAnimationField = serializedObject.FindProperty("useSingleAnimation");
            EditorGUILayout.PropertyField(useSingleAnimationField, true);

            EditorGUILayout.Space();

            if (!controller.useSingleAnimation)
            {
                EditorGUILayout.LabelField("Animation Names File", EditorStyles.boldLabel);

                SerializedProperty animationsNames = serializedObject.FindProperty("animationsNames");
                EditorGUILayout.PropertyField(animationsNames, true);

                EditorGUILayout.Space();
            }


            EditorGUILayout.LabelField("Materials List", EditorStyles.boldLabel);

            SerializedProperty materialsProperty = serializedObject.FindProperty("materials");
            EditorGUILayout.PropertyField(materialsProperty, true);

            serializedObject.ApplyModifiedProperties();


            if (controller.materials.Count > 0)
            {
                Material material = controller.materials[0];
                float animationTime = material.GetFloat("_AnimationTime");
                float progress = animationTime;

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Animation Progress", EditorStyles.boldLabel);
                Rect rect = GUILayoutUtility.GetRect(18, 18, "TextField");
                EditorGUI.ProgressBar(rect, progress, "Animation Progress");
                EditorGUILayout.Space();
            }



            if (!controller.useSingleAnimation)
            {
                if (controller.animationClipsNames != null && controller.animationClipsNames.Length > 0)
                {
                    controller.selectedAnimation = EditorGUILayout.Popup("Selected Animation", controller.selectedAnimation, controller.animationClipsNames);

                    if (controller.selectedAnimation != (int)controller.materials[0].GetFloat("_SelectedAnimation"))
                    {
                        foreach (var material in controller.materials)
                        {
                            material.SetFloat("_SelectedAnimation", controller.selectedAnimation);

                            if (controller.startEndFramesTexture != null)
                            {
                                Color pixelColor = controller.startEndFramesTexture.GetPixel(controller.selectedAnimation, 0);
                                float startFrame = pixelColor.r;
                                float endFrame = pixelColor.g;

                                // Calculate animation length
                                float animationLength = endFrame - startFrame;
                                material.SetFloat("_AnimationLength", animationLength / controller.frameRate);
                            }
                            else
                            {
                                Debug.Log("startEndFramesTexture is null");
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("selected animation is null " + controller.selectedAnimation + " and " + (int)controller.materials[0].GetFloat("_SelectedAnimation"));
                    }
                }

            }
        }

        private void UpdateProgress()
        {
            // Force the inspector to repaint
            Repaint();
        }
    }
}