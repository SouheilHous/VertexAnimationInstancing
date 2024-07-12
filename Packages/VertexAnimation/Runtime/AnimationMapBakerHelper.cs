
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
    public struct AnimData
    {
        #region FIELDS

        private int _vertexCount;
        private int _mapWidth;
        private readonly List<AnimationState> _animClips;
        private string _name;

        private Animation _animation;
        private SkinnedMeshRenderer _skin;

        public List<AnimationState> AnimationClips => _animClips;
        public int MapWidth => _mapWidth;
        public string Name => _name;

        #endregion

        public AnimData(Animation anim, SkinnedMeshRenderer smr, string goName)
        {
            _vertexCount = smr.sharedMesh.vertexCount;
            _mapWidth = Mathf.NextPowerOfTwo(_vertexCount);
            _animClips = new List<AnimationState>(anim.Cast<AnimationState>());
            _animation = anim;
            _skin = smr;
            _name = goName;
        }

        #region METHODS

        public void AnimationPlay(string animName)
        {
            _animation.Play(animName);
        }

        public void SampleAnimAndBakeMesh(ref Mesh m)
        {
            SampleAnim();
            BakeMesh(ref m);
        }

        private void SampleAnim()
        {
            if (_animation == null)
            {
                Debug.LogError("animation is null!!");
                return;
            }

            _animation.Sample();
        }

        private void BakeMesh(ref Mesh m)
        {
            if (_skin == null)
            {
                Debug.LogError("skin is null!!");
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

        public void SetAnimData(SkinnedMeshRenderer smr, Animation anim)
        {
            if (smr == null)
            {
                Debug.LogError("SkinnedMeshRenderer is null!");
                return;
            }

            if (anim == null)
            {
                Debug.LogError("Animation is null!");
                return;
            }

            _bakedMesh = new Mesh();
            _animData = new AnimData(anim, smr, smr.name);
        }

        public List<BakedData> Bake(int frameRate)
        {
            if (_animData == null)
            {
                Debug.LogError("Bake data is null!");
                return _bakedDataList;
            }
            animationNames = new List<string>();
            foreach (var t in _animData.Value.AnimationClips)
            {
                if (!t.clip.legacy)
                {
                    Debug.LogError($"{t.clip.name} is not legacy!");
                    continue;
                }
                animationNames.Add(t.clip.name); // Save animation name
                BakePerAnimClip(t, frameRate);
            }

            CreateStartEndFramesTexture();

            return _bakedDataList;
        }

        public ShaderAnimationNames SaveAnimationNames()
        {
            ShaderAnimationNames animNames = ScriptableObject.CreateInstance<ShaderAnimationNames>();
            animNames.names = animationNames.ToArray();
            return animNames;
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
            foreach (var clip in _animData.Value.AnimationClips)
            {
                totalFrames += Mathf.CeilToInt(clip.clip.length * frameRate);
                animationNames.Add(clip.clip.name); // Save animation name
            }

            int width = _animData.Value.MapWidth;
            int height = totalFrames;
            Texture2D animMap = new Texture2D(width, height, TextureFormat.RGBAHalf, false);

            animMap.name = string.Format($"{_animData.Value.Name}_Combined.animMap");

            int currentFrame = 0;
            foreach (var clip in _animData.Value.AnimationClips)
            {
                int clipFrames = Mathf.CeilToInt(clip.clip.length * frameRate);
                _startEndFramesList.Add(new Vector2(currentFrame, currentFrame + clipFrames - 1)); // Corrected frame indexing
                BakeClipFrames(clip, animMap, frameRate, currentFrame, clipFrames);
                currentFrame += clipFrames;
            }

            animMap.Apply();

            CreateStartEndFramesTexture();

            return new BakedData($"{_animData.Value.Name}_Combined", totalFrames / (float)frameRate, animMap);
        }

        private void BakeClipFrames(AnimationState clip, Texture2D animMap, int frameRate, int startFrame, int clipFrames)
        {
            float sampleTime = 0;
            float perFrameTime = clip.length / clipFrames;

            for (int i = 0; i < clipFrames; i++)
            {
                clip.time = sampleTime;
                _animData.Value.AnimationPlay(clip.name);
                _animData.Value.SampleAnimAndBakeMesh(ref _bakedMesh);

                for (int j = 0; j < _bakedMesh.vertexCount; j++)
                {
                    var vertex = _bakedMesh.vertices[j];
                    animMap.SetPixel(j, startFrame + i, new Color(vertex.x, vertex.y, vertex.z));
                }

                sampleTime += perFrameTime;
            }
        }

        private void BakePerAnimClip(AnimationState curAnim, int frameRate)
        {
            var curClipFrame = Mathf.CeilToInt(curAnim.clip.length * frameRate);
            var perFrameTime = curAnim.length / curClipFrame;

            var animMap = new Texture2D(_animData.Value.MapWidth, curClipFrame, TextureFormat.RGBAHalf, false);
            animMap.name = string.Format($"{_animData.Value.Name}_{curAnim.name}.animMap");
            _animData.Value.AnimationPlay(curAnim.name);

            for (var i = 0; i < curClipFrame; i++)
            {
                curAnim.time = i * perFrameTime;
                _animData.Value.SampleAnimAndBakeMesh(ref _bakedMesh);

                for (var j = 0; j < _bakedMesh.vertexCount; j++)
                {
                    var vertex = _bakedMesh.vertices[j];
                    animMap.SetPixel(j, i, new Color(vertex.x, vertex.y, vertex.z));
                }
            }
            animMap.Apply();

            _startEndFramesList.Add(new Vector2(0, curClipFrame - 1)); // Corrected frame indexing

            Debug.Log(curClipFrame);

            _bakedDataList.Add(new BakedData(animMap.name, curAnim.clip.length, animMap));
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


