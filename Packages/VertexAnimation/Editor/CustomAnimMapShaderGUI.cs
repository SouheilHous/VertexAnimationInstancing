using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace VertexAnimation.Editor
{
    public class CustomAnimMapShaderGUI : ShaderGUI
    {
        private bool isPlaying;
        private bool isLooping;

        private int selectedAnimation;
        private int maxAnimations = 1;
        private int frameRate = 0;
        private string[] animationNames;


        static MaterialProperty FindAndRemoveProperty(string propertyName, List<MaterialProperty> propertyList, bool propertyIsMandatory = true)
        {
            for (var i = 0; i < propertyList.Count; i++)
                if (propertyList[i] != null && propertyList[i].name == propertyName)
                {
                    var property = propertyList[i];
                    propertyList.RemoveAt(i);
                    return property;
                }

            if (propertyIsMandatory)
                throw new System.ArgumentException("Could not find MaterialProperty: '" + propertyName + "', Num properties: " + propertyList.Count);
            return null;
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            // Find properties
            List<MaterialProperty> propertyList = new List<MaterialProperty>();
            MaterialProperty animationTime = FindProperty("_AnimationTime", properties, false);

            MaterialProperty selectedAnimationProp = FindProperty("_SelectedAnimation", properties, false);
            MaterialProperty animationMapProp = FindProperty("_AnimationMap", properties, false);
            MaterialProperty startEndFramesProp = FindProperty("_StartEndFrames", properties, false);
            MaterialProperty animationLengthProp = FindProperty("_AnimationLength", properties, false);
            MaterialProperty totalAnimationLengthProp = FindProperty("_TotalAnimationsLength", properties, false);
            MaterialProperty BaseMapProp = FindProperty("_BaseMap", properties, false);
            MaterialProperty EmissionMapProp = FindProperty("_EmissionMap", properties, false);
            MaterialProperty UseEmissionProp = FindProperty("_UseEmmision", properties, false);
            MaterialProperty EmissionColorProp = FindProperty("_EmissionColor", properties, false);


            propertyList.Add(animationTime);

            propertyList.Add(BaseMapProp);

            propertyList.Add(UseEmissionProp);

            propertyList.Add(EmissionColorProp);

            propertyList.Add(EmissionMapProp);

            propertyList.Add(animationMapProp);

            propertyList.Add(totalAnimationLengthProp);

            propertyList.Add(startEndFramesProp);



            // Load animation names
            if (animationNames == null)
            {
                Material material = materialEditor.target as Material;
                string materialPath = AssetDatabase.GetAssetPath(material);
                string directoryPath = Path.GetDirectoryName(materialPath);

                ShaderAnimationNames animNames = AssetDatabase.LoadAssetAtPath<ShaderAnimationNames>(Path.Combine(directoryPath, "AnimationNames.asset"));

                if (animNames != null)
                {
                    animationNames = animNames.names;
                    maxAnimations = animationNames.Length;
                }
                else
                {
                    animationNames = new string[] { "None" };
                    maxAnimations = 1;
                }
            }

            // Update maxAnimations based on _StartEndFrames texture
            if (startEndFramesProp != null && startEndFramesProp.textureValue != null)
            {
                maxAnimations = startEndFramesProp.textureValue.width;
            }
            else
            {
                maxAnimations = 1;
            }

            if (frameRate != Mathf.RoundToInt(animationMapProp.textureValue.height / totalAnimationLengthProp.floatValue))

                frameRate = Mathf.RoundToInt(animationMapProp.textureValue.height / totalAnimationLengthProp.floatValue);


            // Int slider for selecting the current animation
            if (selectedAnimationProp != null)
            {
                selectedAnimation = EditorGUILayout.Popup("Selected Animation", selectedAnimation, animationNames);

                // selectedAnimation = EditorGUILayout.IntSlider("Selected Animation", selectedAnimation, 0, maxAnimations - 1);

                // Save the selected animation index in the material
                if (selectedAnimation != (int)selectedAnimationProp.floatValue)
                {
                    SetPropertyForAllMaterials(materialEditor, selectedAnimationProp, selectedAnimation);

                    if (startEndFramesProp.textureValue != null)
                    {
                        Texture2D startEndFramesTexture = startEndFramesProp.textureValue as Texture2D;
                        Color pixelColor = startEndFramesTexture.GetPixel(selectedAnimation, 0);

                        float startFrame = pixelColor.r;
                        float endFrame = pixelColor.g;

                        // Calculate animation length
                        float animationLength = endFrame - startFrame;

                        SetPropertyForAllMaterials(materialEditor, animationLengthProp, animationLength / frameRate);
                    }
                }
            }

            foreach (var prop in propertyList)
            {
                materialEditor.ShaderProperty(prop, prop.displayName);
            }
        }

        private void SetPropertyForAllMaterials(MaterialEditor materialEditor, MaterialProperty property, float value)
        {
            if (property != null)
            {
                materialEditor.RegisterPropertyChangeUndo(property.displayName);
                foreach (Material material in materialEditor.targets)
                {
                    material.SetFloat(property.name, value);
                }
            }
        }
    }

}
