using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace VertexAnimation.Editor
{
    public class AnimationMapBaker : EditorWindow
    {
        #region Enums and Constants

        private enum SaveStrategy
        {
            AnimMap,
            Mat,
            Prefab
        }

        public enum FrameRate
        {
            Rate_12 = 0,
            Rate_15 = 1,
            Rate_24 = 2,
            Rate_30 = 3,
            Rate_60 = 4
        }

        private const string URPShader = "Shader Graphs/AnimMapURP";
        private const string URPShaderSingleAnimation = "Shader Graphs/AnimMapSingleURP";

        #endregion

        #region Fields

        public VisualTreeAsset uxml;
        public StyleSheet styleSheet;

        private static GameObject _targetGo;
        private static AnimationMapBakerHelper _baker;
        private static string _path = "Animation Baker";
        private static string _subPath = "Character";
        private static SaveStrategy _strategy = SaveStrategy.Prefab;
        private static Shader _animMapShader;
        private static readonly int MainTex = Shader.PropertyToID("_BaseMap");
        private static readonly int AnimMap = Shader.PropertyToID("_AnimationMap");
        private static readonly int AnimLen = Shader.PropertyToID("_AnimationLength");
        private static readonly int TotalAnimLen = Shader.PropertyToID("_TotalAnimationsLength");
        private static readonly int EndFrame = Shader.PropertyToID("_EndFrame");
        private static readonly int StartEndFrames = Shader.PropertyToID("_StartEndFrames");
        private static readonly int SelectedAnimation = Shader.PropertyToID("_SelectedAnimation");
        private static readonly int AnimationsCount = Shader.PropertyToID("_AnimationsCount");

        private static List<Material> _materials = new List<Material>();
        private static Texture2D animationList;

        private bool _isShadowEnabled = false;
        private bool supportLOD = false;
        private static int[] frameRates = new int[] { 12, 15, 24, 30, 60 };
        private static FrameRate selectedFrameRate = FrameRate.Rate_30;
        private bool bakeAllInOneTexture = false;
        private Texture2D startAndEndFramesTex;

        private ProgressBar _progressBar;
        private ScrollView _logArea;
        private Label _logLabel;

        private Dictionary<string, Texture2D> savedTextures = new Dictionary<string, Texture2D>();
        private Dictionary<string, Material> savedMaterials = new Dictionary<string, Material>();

        private ShaderAnimationNames animNamesFile;

        #endregion

        #region Editor Window Setup

        [MenuItem("Window/VertexAnimation/Animation Map Baker")]
        public static void ShowWindow()
        {
            AnimationMapBaker wnd = GetWindow<AnimationMapBaker>();
            wnd.titleContent = new GUIContent("Animation Map Baker");
            wnd.minSize = new Vector2(400, 400);
        }

        private void OnEnable()
        {
            savedTextures = new Dictionary<string, Texture2D>();
            savedMaterials = new Dictionary<string, Material>();

            var root = rootVisualElement;

            // Load and clone the UXML
            if (uxml != null)
            {
                uxml.CloneTree(root);
            }
            else
            {
                Debug.LogError("UXML file not found");
                return;
            }

            // Load and apply the USS
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }
            else
            {
                Debug.LogError("USS file not found");
                return;
            }

            BindUIElements(root);
        }

        private void BindUIElements(VisualElement root)
        {
            var targetGameObjectField = root.Q<ObjectField>("targetGameObjectField");
            if (targetGameObjectField != null)
            {
                targetGameObjectField.objectType = typeof(GameObject);

                // Try to set the initial value based on the current selection
                SetInitialTargetGameObject(targetGameObjectField);

                targetGameObjectField.RegisterValueChangedCallback(evt => OnTargetGameObjectChanged((GameObject)evt.newValue));

                // Register a callback for selection changes in the editor
                Selection.selectionChanged += () => OnEditorSelectionChanged(targetGameObjectField);
            }

            var shaderField = root.Q<ObjectField>("shaderField");
            if (shaderField != null)
            {
                shaderField.objectType = typeof(Shader);
                shaderField.value = Shader.Find(URPShader);
                shaderField.RegisterValueChangedCallback(evt => _animMapShader = (Shader)evt.newValue);
            }

            var outputPathField = root.Q<TextField>("outputPathField");
            if (outputPathField != null)
            {
                outputPathField.value = _path;
                outputPathField.RegisterValueChangedCallback(evt => _path = evt.newValue);
            }

            var subPathField = root.Q<TextField>("subPathField");
            if (subPathField != null)
            {
                subPathField.value = _subPath;
                subPathField.RegisterValueChangedCallback(evt => _subPath = evt.newValue);
            }

            var outputTypeField = root.Q<EnumField>("outputTypeField");
            if (outputTypeField != null)
            {
                outputTypeField.Init(_strategy);
                outputTypeField.RegisterValueChangedCallback(evt => _strategy = (SaveStrategy)evt.newValue);
            }

            var enableShadowField = root.Q<Toggle>("enableShadowField");
            if (enableShadowField != null)
            {
                enableShadowField.value = _isShadowEnabled;
                enableShadowField.RegisterValueChangedCallback(evt => _isShadowEnabled = evt.newValue);
            }

            var frameRateField = root.Q<EnumField>("frameRateField");
            if (frameRateField != null)
            {
                frameRateField.Init(selectedFrameRate);
                frameRateField.RegisterValueChangedCallback(evt => selectedFrameRate = (FrameRate)evt.newValue);
            }

            var bakeAllInOneField = root.Q<Toggle>("bakeAllInOneField");
            if (bakeAllInOneField != null)
            {
                bakeAllInOneField.value = bakeAllInOneTexture;
                bakeAllInOneField.RegisterValueChangedCallback(evt => bakeAllInOneTexture = evt.newValue);
                bakeAllInOneField.RegisterValueChangedCallback(evt => OnBakeTextureTypeChanged(evt.newValue, shaderField));
            }

            var supportLODField = root.Q<Toggle>("supportLODField");
            if (supportLODField != null)
            {
                supportLODField.value = supportLOD;
                supportLODField.RegisterValueChangedCallback(evt => supportLOD = evt.newValue);

            }

            var bakeButton = root.Q<Button>("bakeButton");
            if (bakeButton != null)
            {
                bakeButton.clicked += OnBakeButtonClicked;
            }

            // Initialize fields with default values
            _baker = new AnimationMapBakerHelper();
            _animMapShader = Shader.Find(URPShader);

            // Initialize materials list
            var materialsListView = root.Q<ListView>("materialsListView");
            if (materialsListView != null)
            {
                materialsListView.itemsSource = _materials;
                materialsListView.makeItem = () => new Label();
                materialsListView.bindItem = (element, i) => (element as Label).text = _materials[i].name;
                materialsListView.selectionType = SelectionType.Single;
                materialsListView.selectionChanged += OnMaterialSelected;
            }

            // Initialize progress bar and log area
            _progressBar = root.Q<ProgressBar>("progressBar");
            _logArea = root.Q<ScrollView>("logArea");
            _logLabel = new Label();
            _logArea.Add(_logLabel);
        }

        #endregion

        #region UI Callbacks

        private void OnTargetGameObjectChanged(GameObject newTarget)
        {
            _targetGo = newTarget;
            if (_targetGo != null)
            {
                string cleanName = newTarget.name.Replace("_", "");

                _materials = _targetGo.GetComponentsInChildren<Renderer>()
                                      .SelectMany(r => r.sharedMaterials)
                                      .Distinct()
                                      .ToList();

                var materialsListView = rootVisualElement.Q<ListView>("materialsListView");

                // Update _subPath and the UI TextField for subPath
                _subPath = cleanName;
                var subPathField = rootVisualElement.Q<TextField>("subPathField");
                if (subPathField != null)
                    subPathField.value = _subPath;

                if (materialsListView != null)
                {
                    materialsListView.itemsSource = _materials;
                    materialsListView.Rebuild();
                }

                savedTextures = new Dictionary<string, Texture2D>();
                savedMaterials = new Dictionary<string, Material>();

                // Show or hide the support LOD toggle based on whether the target has LODGroup components
                var supportLODField = rootVisualElement.Q<Toggle>("supportLODField");
                if (supportLODField != null)
                {
                    supportLODField.style.display = _targetGo.GetComponentsInChildren<LODGroup>().Count() > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }

        private void OnBakeTextureTypeChanged(bool bakeTextureType, ObjectField shader)
        {
            if (bakeTextureType)
                _animMapShader = Shader.Find(URPShader);
            else
                _animMapShader = Shader.Find(URPShaderSingleAnimation);

            shader.objectType = typeof(Shader);
            shader.value = _animMapShader;
        }

        private void SetInitialTargetGameObject(ObjectField targetGameObjectField)
        {
            var activeObject = Selection.activeObject as GameObject;
            if (activeObject != null)
            {
                var smrs = activeObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                if (smrs != null && smrs.Length > 0)
                {
                    targetGameObjectField.value = activeObject;
                    _targetGo = activeObject;
                    OnTargetGameObjectChanged(_targetGo);
                }
            }
        }

        private void OnEditorSelectionChanged(ObjectField targetGameObjectField)
        {
            var activeObject = Selection.activeObject as GameObject;
            if (activeObject != null)
            {
                var smrs = activeObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                if (smrs != null && smrs.Length > 0)
                {
                    targetGameObjectField.value = activeObject;
                    _targetGo = activeObject;
                    OnTargetGameObjectChanged(_targetGo);
                }
            }
        }

        private void OnMaterialSelected(IEnumerable<object> selectedItems)
        {
            // Handle material selection if needed
        }

        private void OnBakeButtonClicked()
        {
            if (_targetGo == null)
            {
                EditorUtility.DisplayDialog("Error", "Target GameObject is null!", "OK");
                return;
            }

            var smrs = GetSkinnedMeshRenderers(_targetGo);
            if (smrs == null || smrs.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "Target skinned mesh renderers are null!", "OK");
                return;
            }

            var go = new GameObject(_targetGo.name);
            bool saveIsDone = false;
            UpdateProgress(0, "Starting bake process...");
            int totalSteps = smrs.Length + 1; // +1 for final save step

            foreach (var sm in smrs)
            {
                if (_baker == null)
                {
                    _baker = new AnimationMapBakerHelper();
                }

                var haveAnimation = _targetGo.TryGetComponent<Animation>(out Animation anim);
                var haveAnimator= _targetGo.TryGetComponent<Animator>(out Animator animator);

                AnimationSourceType type = AnimationSourceType.Legacy;

                if(!haveAnimation && !haveAnimator) 
                {
                     type = AnimationSourceType.None;

                    EditorUtility.DisplayDialog("Error", "Target GameObject does not have Animator or animation clips!", "OK");
                    return;
                }

                if (haveAnimation )
                {
                    type = AnimationSourceType.Legacy;
                }
                else if (haveAnimator )
                {
                    type = AnimationSourceType.Animator;
                }

                _baker.SetAnimData(sm,type, anim, animator);

                if (bakeAllInOneTexture)
                {
                    BakedData bakedData = _baker.BakeAllAnimations(frameRates[(int)selectedFrameRate]);
                    Save(ref bakedData, go, sm);
                    saveIsDone = true;
                }
                else
                {
                    var list = _baker.Bake(frameRates[(int)selectedFrameRate]);

                    if (list == null || list.Count == 0) return;
                    foreach (var data in list)
                    {
                        var d = data;
                        Save(ref d, go, sm);
                        saveIsDone = true;
                    }
                }
                UpdateProgress((float)(smrs.ToList().IndexOf(sm) + 1) / totalSteps, $"Processed {sm.name}");
            }

            if (!saveIsDone)
            {
                DestroyImmediate(go);
                UpdateProgress(1, "Bake process failed.");
                return;
            }

            if (!_isShadowEnabled)
            {
                var renders = go.GetComponentsInChildren<MeshRenderer>();
                foreach (var render in renders)
                    render.shadowCastingMode = ShadowCastingMode.Off;
            }

            var AnimationManager = new GameObject(go.name + " Animation Manager");
            var Controller = AnimationManager.AddComponent<MaterialAnimationController>();

            if (!bakeAllInOneTexture)
            {
                Controller.useSingleAnimation = true;
            }
            else
            {
                if (animNamesFile != null && animNamesFile.names != null)
                {
                    Controller.animationsNames = animNamesFile;
                    Controller.animationClipsNames = animNamesFile.names;
                }
            }

            foreach (var mat in savedMaterials)
                Controller.materials.Add(mat.Value);

            var folderPath = CreateFolder();

            if (supportLOD)
            {
                CopyLODGroupSettings(_targetGo, go);
            }

            PrefabUtility.SaveAsPrefabAsset(go, Path.Combine(folderPath, $"{_subPath}.prefab").Replace("\\", "/"));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UpdateProgress(1, "Bake process completed successfully.");
        }

        private SkinnedMeshRenderer[] GetSkinnedMeshRenderers(GameObject target)
        {
            if (supportLOD)
            {
                return target.GetComponentsInChildren<SkinnedMeshRenderer>();
            }
            else
            {
                var sourceLodGroups = target.GetComponentsInChildren<LODGroup>();

                if (sourceLodGroups == null && sourceLodGroups.Length == 0)
                {
                    var lodGroup = sourceLodGroups[0];

                    if (lodGroup != null)
                    {
                        var lods = lodGroup.GetLODs();

                        if (lods != null && lods.Length > 0)
                        {
                            return lods[0].renderers.OfType<SkinnedMeshRenderer>().ToArray();
                        }
                    }
                }

                return target.GetComponentsInChildren<SkinnedMeshRenderer>().Where(smr => smr.GetComponentInParent<LODGroup>() == null).ToArray();
            }
        }

        private void CopyLODGroupSettings(GameObject source, GameObject destination)
        {
            var sourceLodGroups = source.GetComponentsInChildren<LODGroup>();

            if (sourceLodGroups == null || sourceLodGroups.Length == 0) return;

            var sourceLodGroup = sourceLodGroups[0];
            var destinationLodGroup = destination.AddComponent<LODGroup>();
            var sourceLODs = sourceLodGroup.GetLODs();
            var destinationLODs = new LOD[sourceLODs.Length];

            for (int i = 0; i < sourceLODs.Length; i++)
            {
                var sourceLOD = sourceLODs[i];
                var renderers = new List<Renderer>();

                foreach (var renderer in sourceLOD.renderers)
                {
                    var skinnedMeshRenderer = renderer;
                    if (skinnedMeshRenderer != null)
                    {
                        var child = FindChildByName(destination.transform, skinnedMeshRenderer.name);
                        if (child != null)
                        {
                            var destSkinnedMeshRenderer = child.GetComponent<Renderer>();
                            if (destSkinnedMeshRenderer != null)
                            {
                                renderers.Add(destSkinnedMeshRenderer);
                            }
                        }
                    }
                }

                destinationLODs[i] = new LOD(sourceLOD.screenRelativeTransitionHeight, renderers.ToArray());
            }

            destinationLodGroup.SetLODs(destinationLODs);
            destinationLodGroup.fadeMode = sourceLodGroup.fadeMode;
            destinationLodGroup.animateCrossFading = sourceLodGroup.animateCrossFading;
            destinationLodGroup.enabled = sourceLodGroup.enabled;
        }

        private Transform FindChildByName(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                {
                    return child;
                }
                var result = FindChildByName(child, name);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        #endregion

        #region Save Methods

        private void Save(ref BakedData data, GameObject TargetParent, SkinnedMeshRenderer targetMesh)
        {
            switch (_strategy)
            {
                case SaveStrategy.AnimMap:
                    SaveAsAsset(ref data);
                    break;
                case SaveStrategy.Mat:
                    SaveAsMat(ref data, targetMesh, !bakeAllInOneTexture);
                    break;
                case SaveStrategy.Prefab:
                    SaveAsPrefab(ref data, TargetParent, targetMesh, !bakeAllInOneTexture);
                    break;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private Texture2D SaveStartEndFramesTexture(Texture2D texture, GameObject TargetParent, SkinnedMeshRenderer targetMesh)
        {
            var folderPath = CreateFolder();
            string assetPath = Path.Combine(folderPath, texture.name + ".asset");

            if (!Directory.Exists(Path.GetDirectoryName(assetPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
            }

            if (savedTextures.ContainsKey(assetPath))
            {
                return savedTextures[assetPath];
            }

            AssetDatabase.CreateAsset(texture, assetPath);
            EditorUtility.SetDirty(texture);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            animNamesFile = _baker.SaveAnimationNames();
            animNamesFile.name = "AnimationNames";

            string animationAssetPath = Path.Combine(folderPath, animNamesFile.name + ".asset");

            if (!Directory.Exists(Path.GetDirectoryName(animationAssetPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(animationAssetPath));
            }

            AssetDatabase.CreateAsset(animNamesFile, animationAssetPath);
            EditorUtility.SetDirty(animNamesFile);

            savedTextures[assetPath] = texture;
            return texture;
        }

        private Material[] SaveAsMat(ref BakedData data, SkinnedMeshRenderer smr, bool singleAnimation)
        {
            if (_animMapShader == null)
            {
                EditorUtility.DisplayDialog("Error", "Shader is null!", "OK");
                return null;
            }

            if (_targetGo == null || smr == null)
            {
                EditorUtility.DisplayDialog("Error", "SkinnedMeshRenderer is null!", "OK");
                return null;
            }

            Shader shader = singleAnimation ? Shader.Find(URPShaderSingleAnimation) : _animMapShader;

            var materials = new Material[smr.sharedMaterials.Length];

            // Check if a texture with the same name as the SMR already exists in the folder
            string folderPath = CreateFolder();

            var animMap = SaveAsAsset(ref data);

            if (!singleAnimation && startAndEndFramesTex == null)
            {
                startAndEndFramesTex = SaveStartEndFramesTexture(_baker.GetStartEndFramesTexture(), null, null);
            }

            for (int i = 0; i < smr.sharedMaterials.Length; i++)
            {
                var originalMat = smr.sharedMaterials[i];
                string uniqueKey = smr.GetInstanceID() + "_" + i + "_" + (singleAnimation ? "Single" : "Multi");

                if (savedMaterials.ContainsKey(uniqueKey))
                {
                    materials[i] = savedMaterials[uniqueKey];
                    continue;
                }

                var mat = new Material(shader);
                mat.SetTexture(MainTex, originalMat.mainTexture);
                Debug.Log(smr.name + "  " + mat.name + " " + animMap.name);
                mat.SetTexture(AnimMap, animMap);

                if (singleAnimation)
                {
                    mat.SetFloat("_FrameRate", frameRates[(int)selectedFrameRate]);
                }
                else
                {
                    mat.SetFloat(AnimLen, data.AnimLen);
                    mat.SetFloat(TotalAnimLen, data.AnimLen);
                    mat.SetFloat(EndFrame, Mathf.RoundToInt(data.AnimLen * frameRates[(int)selectedFrameRate]));
                    mat.SetTexture(StartEndFrames, startAndEndFramesTex);
                    mat.SetFloat(SelectedAnimation, 1);
                    mat.SetFloat(AnimationsCount, startAndEndFramesTex.width);
                }

                materials[i] = mat;
                savedMaterials[uniqueKey] = mat;

                string assetPath = Path.Combine(folderPath, smr.name + $"{data.Name}_{i}.mat");

                if (!Directory.Exists(Path.GetDirectoryName(assetPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
                }

                AssetDatabase.CreateAsset(mat, assetPath);
            }

            return materials;
        }

        private Texture2D SaveAsAsset(ref BakedData data)
        {
            var folderPath = CreateFolder();
            string assetPath = Path.Combine(folderPath, data.Name + ".asset");

            if (!Directory.Exists(Path.GetDirectoryName(assetPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
            }

            if (savedTextures.ContainsKey(assetPath))
            {
                return savedTextures[assetPath];
            }

            var animMap = new Texture2D(data.AnimMapWidth, data.AnimMapHeight, TextureFormat.RGBAHalf, false);
            animMap.LoadRawTextureData(data.RawAnimMap);

            AssetDatabase.CreateAsset(animMap, assetPath);
            EditorUtility.SetDirty(animMap);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            savedTextures[assetPath] = animMap;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return animMap;
        }

        private void SaveAsPrefab(ref BakedData data, GameObject parent, SkinnedMeshRenderer targetMesh, bool singleAnimation)
        {
            var go = parent;
            var mat = SaveAsMat(ref data, targetMesh, singleAnimation);

            if (mat == null)
            {
                EditorUtility.DisplayDialog("Error", "Material is null!", "OK");
                return;
            }

            // Check if the mesh already exists in the parent
            var existingMesh = go.GetComponentsInChildren<MeshFilter>().FirstOrDefault(mf => mf.sharedMesh == targetMesh.sharedMesh);
            if (existingMesh == null)
            {
                var childMesh = new GameObject(targetMesh.name);
                childMesh.transform.SetParent(go.transform);
                childMesh.AddComponent<MeshRenderer>().sharedMaterials = mat;
                childMesh.AddComponent<MeshFilter>().sharedMesh = targetMesh.sharedMesh;
            }
            else
            {
                // Update the renderer materials if the mesh already exists
                existingMesh.GetComponent<MeshRenderer>().sharedMaterials = mat;
            }
        }

        #endregion

        #region Utility Methods

        private static string CreateFolder()
        {
            var folderPath = Path.Combine("Assets", _path, _subPath);
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string parentFolder = Path.Combine("Assets", _path);
                if (!AssetDatabase.IsValidFolder(parentFolder))
                {
                    AssetDatabase.CreateFolder("Assets", _path);
                }
                AssetDatabase.CreateFolder(parentFolder, _subPath);
            }
            return folderPath;
        }

        private void UpdateProgress(float progress, string message)
        {
            if (_progressBar != null)
            {
                _progressBar.value = progress * 100;
            }
            Log(message);
        }

        private void Log(string message)
        {
            if (_logLabel != null)
            {
                _logLabel.text += message + "\n";
                _logArea.ScrollTo(_logLabel);
            }
        }

        #endregion
    }
}
