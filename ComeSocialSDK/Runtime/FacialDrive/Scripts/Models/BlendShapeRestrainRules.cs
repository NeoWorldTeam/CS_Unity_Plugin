using System;
using System.Collections.Generic;
using UnityEngine;

namespace ComeSocial.Face.Drive
{

    [Serializable]
    public class BlendShapeRestrain
    {
        [SerializeField]
        [Tooltip("BS名称")]
        string blendShapeName;

        [Range(0f, 1)]
        [SerializeField]
        [Tooltip("BS权重限制｜触发")]
        float weight;


        public int BSIndex = -1;

        public string BlendShapeName { get { return blendShapeName; } }
        public float Weight { get { return weight; } }
    }

    /// <summary>
    /// 抑制BS网格插值规则
    /// </summary>
    [Serializable]
    public class BlendShapeRestrainRules
    {
#pragma warning disable 649

        [SerializeField]
        [Tooltip("BS权重抑制触发的源")]
        BlendShapeRestrain[] sourceBlendShapeRestrain;

        [SerializeField]
        [Tooltip("BS权重抑制触发的目标")]
        BlendShapeRestrain targetBlendShapeRestrain;


#pragma warning restore 649




        private bool _isValid = true;
        public BlendShapeRestrainRules() { }
        public BlendShapeRestrain[] SourceBlendShapeRestrain() { return sourceBlendShapeRestrain; }
        public BlendShapeRestrain TargetBlendShapeRestrain() { return targetBlendShapeRestrain; }

        internal void HandleBlendShapeRelative(string[] locations)
        {
            if (sourceBlendShapeRestrain == null || sourceBlendShapeRestrain.Length == 0)
            {
                Debug.LogError("BS抑制：源BS没有输入");
                _isValid = false;
                return;
            }

            //解析源BS
            foreach (var item in sourceBlendShapeRestrain)
            {
                if (item == null || item.BlendShapeName == null || item.BlendShapeName.Equals(""))
                {
                    Debug.LogError("BS抑制：源BS没有输入");
                    _isValid = false;
                    return;
                }

                int index = findIndexFormBSName(locations, item.BlendShapeName);
                if (index >= 0)
                {
                    item.BSIndex = index;
                }
                else
                {
                    Debug.LogFormat("BS抑制：源BS:{0} 没有匹配", item.BlendShapeName);
                    _isValid = false;
                    return;
                }
            }

            if(targetBlendShapeRestrain == null || targetBlendShapeRestrain.BlendShapeName == null || targetBlendShapeRestrain.BlendShapeName.Equals(""))
            {
                Debug.LogError("BS抑制：目标BS没有输入");
                _isValid = false;
                return;
            }
            else
            {
                int index = findIndexFormBSName(locations, targetBlendShapeRestrain.BlendShapeName);
                if (index >= 0)
                {
                    targetBlendShapeRestrain.BSIndex = index;
                }
                else
                {
                    Debug.LogFormat("BS抑制：目标BS:{0} 没有匹配", targetBlendShapeRestrain.BlendShapeName);
                    _isValid = false;
                    return;
                }
            }
        }


        private int findIndexFormBSName(string[] bsList, string shapeName)
        {

            for (int i = 0; i < bsList.Length; i++)
            {
                string bs = bsList[i];

                if (bs.ToLower().Trim().Equals(shapeName.ToLower().Trim()))
                {
                    return i;
                }
            }
            return -1;
        }

        internal bool IsValid()
        {
            return _isValid;
        }

        internal bool NeedUpdate()
        {
            return targetBlendShapeRestrain.BSIndex == -1;
        }

        internal bool IsSourceActive(float[] currentBlendShapesWeight)
        {
            foreach(var item in sourceBlendShapeRestrain)
            {
                float currentValue = currentBlendShapesWeight[item.BSIndex];
                float activeValue = item.Weight;
                if(currentValue < activeValue)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
        
    