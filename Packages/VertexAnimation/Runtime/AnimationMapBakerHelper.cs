
/*
 * Created by jiadong chen
 * https://jiadong-chen.medium.com/
 * 用来烘焙动作贴图。烘焙对象使用Animation组件，并且在导入时设置Rig为Legacy
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace VertexAnimation
{
    public enum AnimationSourceType
    {
        Legacy,
        Animator,
        None
    }
    public struct AnimData
    {
        #region FIELDS

        private int _vertexCount;
        private int _mapWidth;
        private List<AnimationClip> _animClips;
        private string _name;

        private Animation _animation; // Legacy Animation
        private Animator _animator;   // Animator for Generic and Humanoid
        private SkinnedMeshRenderer _skin;
        private AnimationSourceType _sourceType;

        public List<AnimationClip> AnimationClips => _animClips;
        public int MapWidth => _mapWidth;
        public string Name => _name;

        public AnimationSourceType SourceType=> _sourceType;

        public Animator Animator => _animator;
        public Animation Animation => _animation;

        public SkinnedMeshRenderer SkinnedMeshRenderer => _skin;

        #endregion

        public AnimData(Animation anim, SkinnedMeshRenderer smr, string goName)
        {
            _vertexCount = smr.sharedMesh.vertexCount;
            _mapWidth = Mathf.NextPowerOfTwo(_vertexCount);
            _animClips = new List<AnimationClip>(anim.Cast<AnimationState>().Select(state => state.clip));
            _animation = anim;
            _animator = null;
            _skin = smr;
            _name = goName;
            _sourceType = AnimationSourceType.Legacy;
        }

        public AnimData(Animator animator, SkinnedMeshRenderer smr, string goName)
        {
            _vertexCount = smr.sharedMesh.vertexCount;
            _mapWidth = Mathf.NextPowerOfTwo(_vertexCount);
            _animClips = new List<AnimationClip>(animator.runtimeAnimatorController.animationClips);
            _animation = null;
            _animator = animator;
            _skin = smr;
            _name = goName;
            _sourceType = AnimationSourceType.Animator;
        }

        #region METHODS

        public void AnimationPlay(string animName)
        {
            if (_sourceType == AnimationSourceType.Legacy)
            {
                _animation.Play(animName);
            }
            else if (_sourceType == AnimationSourceType.Animator)
            {
                var clip = _animClips.FirstOrDefault(c => c.name == animName);
                if (clip != null)
                {
                    _animator.Play(clip.name);
                    _animator.Update(0); // Required to apply the first frame of the animation
                }
            }
        }

        public void SampleAnimAndBakeMesh(ref Mesh m)
        {
            SampleAnim();
            BakeMesh(ref m);
        }

        private void SampleAnim()
        {
            if (_sourceType == AnimationSourceType.Legacy)
            {
                if (_animation == null)
                {
                    Debug.LogError("Animation is null!!");
                    return;
                }
                _animation.Sample();
            }
            else if (_sourceType == AnimationSourceType.Animator)
            {
                if (_animator == null)
                {
                    Debug.LogError("Animator is null!!");
                    return;
                }
                _animator.Update(0); // Sample the current frame
            }
        }

        private void BakeMesh(ref Mesh m)
        {
            if (_skin == null)
            {
                Debug.LogError("SkinnedMeshRenderer is null!!");
                return;
            }

            _skin.BakeMesh(m);
        }

        #endregion
    }

    public struct BakedData
    {
        #region FIELDS

        private readonly string _name;
        private readonly float _animLen;
        private readonly byte[] _rawAnimMap;
        private readonly int _animMapWidth;
        private readonly int _animMapHeight;

        #endregion

        public BakedData(string name, float animLen, Texture2D animMap)
        {
            _name = name;
            _animLen = animLen;
            _animMapHeight = animMap.height;
            _animMapWidth = animMap.width;
            _rawAnimMap = animMap.GetRawTextureData();
        }

        public int AnimMapWidth => _animMapWidth;

        public string Name => _name;

        public float AnimLen => _animLen;

        public byte[] RawAnimMap => _rawAnimMap;

        public int AnimMapHeight => _animMapHeight;
    }

    public class AnimationMapBakerHelper
    {
        private AnimData? _animData = null;
        private Mesh _bakedMesh;
        private readonly List<BakedData> _bakedDataList = new List<BakedData>();
        private Texture2D _startEndFramesTexture;
        private List<Vector2> _startEndFramesList = new List<Vector2>();
        private List<string> animationNames = new List<string>();
        private AnimationSourceType _sourceType;

        // Method to set AnimData based on the chosen source type
        public void SetAnimData(SkinnedMeshRenderer smr, AnimationSourceType sourceType, Animation animation = null, Animator animator = null)
        {
            if (smr == null)
            {
                Debug.LogError("SkinnedMeshRenderer is null!");
                return;
            }

            _bakedMesh = new Mesh();

            switch (sourceType)
            {
                case AnimationSourceType.Legacy:
                    if (animation == null)
                    {
                        Debug.LogError("Animation is null!");
                        return;
                    }
                    _animData = new AnimData(animation, smr, smr.name);
                    break;

                case AnimationSourceType.Animator:
                    if (animator == null)
                    {
                        Debug.LogError("Animator is null!");
                        return;
                    }
                    _animData = new AnimData(animator, smr, smr.name);
                    break;
            }
        }

        public List<BakedData> Bake(int frameRate)
        {
            if (_animData == null)
            {
                Debug.LogError("Bake data is null!");
                return _bakedDataList;
            }

            animationNames = new List<string>();
            foreach (var clip in _animData.Value.AnimationClips)
            {
                animationNames.Add(clip.name); // Save animation name
                BakePerAnimClip(clip, frameRate);
            }

            CreateStartEndFramesTexture();

            return _bakedDataList;
        }

        public BakedData BakeAllAnimations(int frameRate)
        {
            if (_animData == null)
            {
                Debug.LogError("Bake data is null!");
                return default;
            }

            animationNames = new List<string>();
            int totalFrames = 0;

            // Calculate the total number of frames and store animation names
            foreach (var clip in _animData.Value.AnimationClips)
            {
                totalFrames += Mathf.CeilToInt(clip.length * frameRate);
                animationNames.Add(clip.name);
            }

            int width = _animData.Value.MapWidth;
            int height = totalFrames;
            Texture2D animMap = new Texture2D(width, height, TextureFormat.RGBAHalf, false);

            animMap.name = string.Format($"{_animData.Value.Name}_Combined.animMap");

            int currentFrame = 0;
            foreach (var clip in _animData.Value.AnimationClips)
            {
                int clipFrames = Mathf.CeilToInt(clip.length * frameRate);
                _startEndFramesList.Add(new Vector2(currentFrame, currentFrame + clipFrames - 1)); // Corrected frame indexing
                BakeClipFrames(clip, animMap, frameRate, currentFrame, clipFrames);
                currentFrame += clipFrames;
            }

            animMap.Apply();

            CreateStartEndFramesTexture();

            return new BakedData($"{_animData.Value.Name}_Combined", totalFrames / (float)frameRate, animMap);
        }

        private void BakeClipFrames(AnimationClip clip, Texture2D animMap, int frameRate, int startFrame, int clipFrames)
        {
            float perFrameTime = clip.length / clipFrames; // Time per frame

            for (int i = 0; i < clipFrames; i++)
            {
                if (_animData.Value.SourceType == AnimationSourceType.Animator)
                {
                    // Play the animation clip and update the Animator
                    _animData.Value.Animator.Play(clip.name, 0, i / (float)clipFrames); // Normalize time [0, 1]
                    _animData.Value.Animator.Update(0);
                }
                else if (_animData.Value.SourceType == AnimationSourceType.Legacy)
                {
                    // Directly advance the animation and sample
                    _animData.Value.AnimationPlay(clip.name); // Play the clip
                    _animData.Value.Animation[clip.name].time = i * perFrameTime;
                    _animData.Value.Animation.Sample();
                }

                // Bake the current frame's mesh data
                _animData.Value.SampleAnimAndBakeMesh(ref _bakedMesh);

                // Store the vertex positions in the texture
                for (int j = 0; j < _bakedMesh.vertexCount; j++)
                {
                    var vertex = _bakedMesh.vertices[j];
                    animMap.SetPixel(j, startFrame + i, new Color(vertex.x, vertex.y, vertex.z));
                }
            }

            animMap.Apply(); // Finalize the texture
        }



        private void BakePerAnimClip(AnimationClip clip, int frameRate)
        {
            int curClipFrame = Mathf.CeilToInt(clip.length * frameRate); // Total frames for the clip
            float perFrameTime = clip.length / curClipFrame;

            var animMap = new Texture2D(_animData.Value.MapWidth, curClipFrame, TextureFormat.RGBAHalf, false);
            animMap.name = string.Format($"{_animData.Value.Name}_{clip.name}.animMap");

            for (int i = 0; i < curClipFrame; i++)
            {
                if (_animData.Value.SourceType == AnimationSourceType.Animator)
                {
                    // Play the animation clip and update the Animator
                    _animData.Value.Animator.Play(clip.name, 0, i / (float)curClipFrame); // Normalize time [0, 1]
                    _animData.Value.Animator.Update(0);
                }
                else if (_animData.Value.SourceType == AnimationSourceType.Legacy)
                {
                    // Directly advance the animation and sample
                    _animData.Value.AnimationPlay(clip.name); // Play the clip
                    _animData.Value.Animation[clip.name].time = i * perFrameTime;
                    _animData.Value.Animation.Sample();
                }

                // Bake the current frame's mesh data
                _animData.Value.SampleAnimAndBakeMesh(ref _bakedMesh);

                // Store the vertex positions in the texture
                for (int j = 0; j < _bakedMesh.vertexCount; j++)
                {
                    var vertex = _bakedMesh.vertices[j];
                    animMap.SetPixel(j, i, new Color(vertex.x, vertex.y, vertex.z));
                }
            }

            animMap.Apply(); // Finalize the texture

            _startEndFramesList.Add(new Vector2(0, curClipFrame - 1)); // Add to start-end frames list
            Debug.Log($"Baked {curClipFrame} frames for clip {clip.name}");

            _bakedDataList.Add(new BakedData(animMap.name, clip.length, animMap));
        }

        public ShaderAnimationNames SaveAnimationNames()
        {
            ShaderAnimationNames animNames = ScriptableObject.CreateInstance<ShaderAnimationNames>();
            animNames.names = animationNames.ToArray();
            return animNames;
        }

        private void CreateStartEndFramesTexture()
        {
            if (_startEndFramesList.Count == 0)
                return;

            _startEndFramesTexture = new Texture2D(_startEndFramesList.Count, 1, TextureFormat.RGBAHalf, false);
            _startEndFramesTexture.name = "AnimationListTimes";

            for (int i = 0; i < _startEndFramesList.Count; i++)
            {
                var startEnd = _startEndFramesList[i];
                _startEndFramesTexture.SetPixel(i, 0, new Color(startEnd.x, startEnd.y + 1, 0, 0));
            }

            _startEndFramesTexture.Apply();
        }

        public Texture2D GetStartEndFramesTexture()
        {
            return _startEndFramesTexture;
        }
    }
}


