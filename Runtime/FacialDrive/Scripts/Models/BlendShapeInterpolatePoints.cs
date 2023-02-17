using System;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

#if UNITY_EDITOR
using System.Collections;
#endif

namespace ComeSocial.Face.Drive
{
    /// <summary>
    /// 存储初始化的BS 插值点
    /// </summary>
    [Serializable]
    public class BlendShapeInterpolatePoints
    {
#pragma warning disable 649
        [SerializeField]
        [Tooltip("BS名称")]
        string m_Name;

        [SerializeField]
        [Tooltip("初始化插值")]
        BlendShapeInterpolatePoint[] m_BSInterpolatePoints = new BlendShapeInterpolatePoint[] { };
#pragma warning restore 649

        public string name { get { return m_Name; } }
        public BlendShapeInterpolatePoint[] interpolatePoints { get { return m_BSInterpolatePoints; } }
        public BlendShapeInterpolatePoints() { }
        public BlendShapeInterpolatePoints(string name)
        {
            m_Name = name;
        }

    }




    /// <summary>
    /// BS触发点
    /// </summary>
    [Serializable]
    public class BlendShapeTriggerPoint
    {
#pragma warning disable 649
        [SerializeField]
        [Tooltip("BS名称")]
        string m_Name;


        [SerializeField]
        [Range(0f, 100)]
        [Tooltip("触发权重")]
        float m_Trigger_weight;


        [SerializeField]
        bool m_IsTrigger = false;

        [SerializeField]
        private int m_LocationIndex = 0;

#pragma warning restore 649

        //记录位置
        public int locationIndex { get { return m_LocationIndex; } }

        public string name { get { return m_Name; } }
        public bool isTrigger { get { return m_IsTrigger; } }
        public float weight { get { return m_Trigger_weight; } }
        public BlendShapeTriggerPoint(string name,int index)
        {
            m_Name = name;
            m_LocationIndex = index;
        }

    }


    /// <summary>
    /// BS触发规则
    /// </summary>
    [Serializable]
    public class BlendShapeTriggerRule
    {
#pragma warning disable 649
        [SerializeField]
        [Tooltip("触发的特效")]
        VisualEffect[] m_vfxs = new VisualEffect[] { };

        [SerializeField]
        [Tooltip("冷却时间，单位秒")]
        float m_coolDownTime = 2;

        [SerializeField]
        [Tooltip("状态维持多久触发生效，单位秒")]
        float m_stateContinuousTime = 0.2f;


        [SerializeField]
        [Tooltip("初始化插值")]
        BlendShapeTriggerPoint[] m_BSTriggerPoints = new BlendShapeTriggerPoint[] { };
#pragma warning restore 649




        
        //被触发的点集合
        private BlendShapeTriggerPoint[] m_IsTriggerPoints = new BlendShapeTriggerPoint[] { };


        public float stateContinuousTime { get { return m_stateContinuousTime; }  }
        public float coolDownTime { get { return m_coolDownTime; } }
        public VisualEffect[] vfxs { get { return m_vfxs; } }
        public BlendShapeTriggerPoint[] TriggerPoints { get { return m_IsTriggerPoints; } }
        public BlendShapeTriggerRule() {
            m_stateContinuousTime = 0.2f;
            m_coolDownTime = 2;
        }
#if UNITY_EDITOR
        internal void InitBlendShapeConfig(BlendShapeIndexData[] indices)
        {

            var blendshapeCount = indices.Length;
            if (blendshapeCount == m_BSTriggerPoints.Length) return;
            //Debug.Log("===== InitBlendShapeConfig");
            var overridesCopy = new BlendShapeTriggerPoint[blendshapeCount];
            for (var i = 0; i < blendshapeCount; i++)
            {

                var indice = indices[i];

                var blendShapeOverride = m_BSTriggerPoints.FirstOrDefault(f => f.name == indice.name)
                    ?? new BlendShapeTriggerPoint(indice.name, i);

                //Debug.LogFormat("===== index:{0},name:{2},location:{1}", i, blendShapeOverride.locationIndex, blendShapeOverride.name);

                overridesCopy[i] = blendShapeOverride;
            }

            m_BSTriggerPoints = overridesCopy;

        }
#endif
        internal void CheckTrigger()
        {
            int[] overridesCopy = new int[m_BSTriggerPoints.Length];
            int copyIndex = 0;
            for (var i = 0; i < m_BSTriggerPoints.Length; i++)
            {
                if (m_BSTriggerPoints[i].isTrigger)
                {
                    overridesCopy[copyIndex] = i;
                    copyIndex = copyIndex + 1;
                }
            }

            Array.Resize<BlendShapeTriggerPoint>(ref m_IsTriggerPoints, copyIndex);
            for(var i = 0; i < copyIndex; i++)
            {
                var itemIndex = overridesCopy[i];
                m_IsTriggerPoints[i] = m_BSTriggerPoints[itemIndex];
            }
        }
    }
}
