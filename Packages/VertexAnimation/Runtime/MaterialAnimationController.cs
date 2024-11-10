using System.Collections.Generic;
using System.Diagnostics;
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

        private float startTime;
        private bool stop;
        private float pausedTime;

        [HideInInspector]
        public bool playAtStart = false;


        private float duration;
        private float startFrame;
        private float endFrame;

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
            CalculateAnimationLength();

        }

        private void CalculateAnimationLength()
        {
            if (useSingleAnimation)
            {
                duration = GetDuration();
                return;
            }

            if (startEndFramesTexture != null && materials.Count > 0)
            {
                // Fetch start and end frames from the texture
                Color pixelColor = startEndFramesTexture.GetPixel(selectedAnimation, 0);
                startFrame = pixelColor.r;
                endFrame = pixelColor.g;

                // Calculate duration
                duration = (endFrame - startFrame) / frameRate;

                // Update materials
                foreach (var material in materials)
                {
                    material.SetFloat("_StartFrame", startFrame);
                    material.SetFloat("_EndFrame", endFrame);
                    material.SetFloat("_AnimationLength", duration);
                    material.SetFloat("_SelectedAnimation", selectedAnimation);
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("StartEndFramesTexture or materials are missing!");
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
                UnityEngine.Debug.LogWarning("No animation names scriptable object is attached");
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

        void Update()
        {
            if (materials.Count == 0)
                return;

            float duration = GetDuration();

            if (duration <= 0)
                return;

            float time = Time.time;
            float elapsedTime = time - startTime;

            foreach (var material in materials)
            {
                if (stop)
                {
                    material.SetFloat("_AnimationTime", 0);
                    stop = false;
                    play = false;
                    continue;
                }

                if (play)
                {
                    float sliderValue;
                    if (loop)
                    {
                        sliderValue = Mathf.Repeat(elapsedTime / duration, 1.0f);
                    }
                    else
                    {
                        sliderValue = Mathf.Clamp01(elapsedTime / duration);
                        if (elapsedTime >= duration)
                        {
                            Stop();
                        }
                    }
                    material.SetFloat("_AnimationTime", sliderValue);
                }

                material.SetFloat("_Play", play ? 1.0f : 0.0f);
                material.SetFloat("_Loop", loop ? 1.0f : 0.0f);
            }
        }

        private float GetDuration()
        {
            if (useSingleAnimation)
            {
                Texture2D animationMap = materials[0].GetTexture("_AnimationMap") as Texture2D;
                float height = animationMap.height;
                return height / frameRate;
            }
            else
                return materials[0].GetFloat("_AnimationLength");

        }

        public void Play()
        {
            if (!stop) // If not stopped, calculate the start time based on the paused time
            {
                startTime = Time.time - pausedTime;
            }
            else // If stopped, start from the beginning
            {
                startTime = Time.time;
                stop = false;
            }
            play = true;
            SetStopBool(false);
        }

        public void Pause()
        {
            play = false;
            pausedTime = Time.time - startTime;
            SetStopBool(false);
        }

        public void Stop()
        {
            play = false;
            stop = true;
            pausedTime = 0f;
            SetStopBool(true);
        }

        public void SetStopBool(bool stopValue)
        {
            if (materials.Count == 0)
                return;
            foreach (var material in materials)
            {
                material.SetFloat("_Stop", stopValue ? 1.0f : 0.0f);
            }
        }

        public void ToggleLoop()
        {
            loop = !loop;
        }
    }

}
