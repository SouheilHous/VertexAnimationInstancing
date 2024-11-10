using System.Collections.Generic;
using UnityEngine;

namespace VertexAnimation
{
    public class MaterialAnimationController : MonoBehaviour
    {
        [HideInInspector]
        public string[] animationClipsNames;
        [HideInInspector]
        public Texture2D startEndFramesTexture;

        public int frameRate;
        [HideInInspector]
        public int selectedAnimation;
        [HideInInspector]
        public List<Material> materials = new List<Material>();
        [HideInInspector]
        public bool play;
        [HideInInspector]
        public ShaderAnimationNames animationsNames;
        [HideInInspector]
        public bool loop;
        [HideInInspector]
        public bool useSingleAnimation;
        [HideInInspector]
        public bool playAtStart = false;

        private float animationTime;
        private float duration;
        private float startFrame;
        private float endFrame;
        private bool stop;
        private float pausedTime;

        void Start()
        {
            Refresh();

            if (playAtStart)
            {
                loop = true;
                Play();
            }
        }

        private void OnValidate()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (startEndFramesTexture == null)
            {
                startEndFramesTexture = GetStartEndFrames();
            }

            LoadAnimationNames();
            CalculateFrameRate();

            // Initialize start and end frames for the selected animation
            if (!useSingleAnimation && startEndFramesTexture != null)
            {
                SetStartEndFrames(selectedAnimation);
            }
            else
            {
                duration = GetDuration();
            }
        }

        private Texture2D GetStartEndFrames()
        {
            if (!useSingleAnimation)
            {
                if (materials == null || materials.Count == 0)
                    return null;

                foreach (var material in materials)
                {
                    startEndFramesTexture = material.GetTexture("_StartEndFrames") as Texture2D;
                    return startEndFramesTexture;
                }
            }
            return null;
        }

        private void LoadAnimationNames()
        {
            if (!animationsNames)
            {
                Debug.LogWarning("No animation names scriptable object is attached");
                return;
            }

            if (animationsNames.names != null)
                animationClipsNames = animationsNames.names;
            else
                animationClipsNames = new string[] { "None" };
        }

        private void CalculateFrameRate()
        {
            if (materials == null || materials.Count == 0)
                return;

            Material material = materials[0];
            if (useSingleAnimation)
            {
                frameRate = Mathf.RoundToInt(material.GetFloat("_FrameRate"));
                return;
            }

            Texture2D animationMap = material.GetTexture("_AnimationMap") as Texture2D;
            float totalAnimationLength = material.GetFloat("_TotalAnimationsLength");

            if (animationMap != null && totalAnimationLength > 0)
            {
                frameRate = Mathf.RoundToInt(animationMap.height / totalAnimationLength);
            }
        }

        private void SetStartEndFrames(int animationIndex)
        {
            if (startEndFramesTexture == null)
                return;

            Color pixelColor = startEndFramesTexture.GetPixel(animationIndex, 0);
            startFrame = pixelColor.r;
            endFrame = pixelColor.g;
            duration = (endFrame - startFrame) / frameRate;

            foreach (var material in materials)
            {
                material.SetFloat("_StartFrame", startFrame);
                material.SetFloat("_EndFrame", endFrame);
                material.SetFloat("_AnimationLength", duration);
                material.SetFloat("_SelectedAnimation", animationIndex);
            }
        }

        void Update()
        {
            if (materials.Count == 0 || duration <= 0)
                return;

            if (stop)
            {
                ResetAnimation();
                return;
            }

            if (play)
            {
                animationTime += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(animationTime / duration);

                if (loop)
                {
                    normalizedTime = Mathf.Repeat(animationTime / duration, 1.0f);
                }
                else if (animationTime >= duration)
                {
                    Stop();
                }

                UpdateMaterialAnimationTime(normalizedTime);
            }
        }

        private void UpdateMaterialAnimationTime(float normalizedTime)
        {
            foreach (var material in materials)
            {
                material.SetFloat("_AnimationTime", normalizedTime);
                material.SetFloat("_Play", play ? 1.0f : 0.0f);
                material.SetFloat("_Loop", loop ? 1.0f : 0.0f);
            }
        }

        private float GetDuration()
        {
            if (useSingleAnimation)
            {
                Texture2D animationMap = materials[0].GetTexture("_AnimationMap") as Texture2D;
                return animationMap.height / frameRate;
            }
            else
            {
                return materials[0].GetFloat("_AnimationLength");
            }
        }

        public void Play()
        {
            if (!stop)
            {
                animationTime = pausedTime;
            }
            else
            {
                animationTime = 0f;
                stop = false;
            }

            play = true;
            SetStopBool(false);
        }

        public void Pause()
        {
            play = false;
            pausedTime = animationTime;
            SetStopBool(false);
        }

        public void Stop()
        {
            play = false;
            stop = true;
            pausedTime = 0f;
            ResetAnimation();
        }

        private void ResetAnimation()
        {
            animationTime = 0f;
            foreach (var material in materials)
            {
                material.SetFloat("_AnimationTime", 0f);
                material.SetFloat("_Play", 0f);
            }
        }

        public void SetStopBool(bool stopValue)
        {
            foreach (var material in materials)
            {
                material.SetFloat("_Stop", stopValue ? 1.0f : 0.0f);
            }
        }

        public void ToggleLoop()
        {
            loop = !loop;
        }

        public void SetSelectedAnimation(int animationIndex)
        {
            selectedAnimation = animationIndex;
            SetStartEndFrames(animationIndex);
            Play(); // Start playing the newly selected animation
        }
    }
}
