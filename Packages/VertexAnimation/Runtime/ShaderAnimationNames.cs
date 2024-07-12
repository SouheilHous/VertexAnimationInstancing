using UnityEngine;

namespace VertexAnimation
{
    [CreateAssetMenu(fileName = "ShaderAnimationNames", menuName = "ScriptableObjects/ShaderAnimationNames", order = 1)]
    public class ShaderAnimationNames : ScriptableObject
    {
        public string[] names;
    }

}
