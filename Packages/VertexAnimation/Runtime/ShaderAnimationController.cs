using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VertexAnimation
{
    public class ShaderAnimationController : MonoBehaviour
    {
        // Start is called before the first frame update
        public Slider animationLengthSlider;
        public Button playButton;

        public Material[] targetMaterials; // Assign this in the Inspector

        private bool isPlaying = false;
        private float animationLength = 1.666f;

        void Start()
        {
            if (animationLengthSlider != null)
            {
                animationLengthSlider.minValue = 0.1f;
                animationLengthSlider.maxValue = 5.0f;
                animationLengthSlider.value = animationLength;
                animationLengthSlider.onValueChanged.AddListener(OnAnimationLengthChanged);
            }

            if (playButton != null)
            {
                playButton.onClick.AddListener(TogglePlay);
            }

            UpdateMaterials();
        }

        void Update()
        {
            if (isPlaying)
            {
                foreach (var material in targetMaterials)
                {
                    material.SetFloat("_AnimTime", Time.time % animationLength); // Assuming _AnimTime is used to control the animation
                }
            }
        }

        private void TogglePlay()
        {
            isPlaying = !isPlaying;
            UpdateMaterials();
        }

        private void OnAnimationLengthChanged(float value)
        {
            animationLength = value;
            UpdateMaterials();
        }

        private void UpdateMaterials()
        {
            foreach (var material in targetMaterials)
            {
                material.SetFloat("_AnimationLength", animationLength);
                material.SetFloat("_Play", isPlaying ? 1.0f : 0.0f);
            }
        }
    }
}
