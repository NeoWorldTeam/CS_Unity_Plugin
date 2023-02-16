using System;
using UnityEngine;

namespace ComeSocial.Face.Drive
{
    /// <summary>
    /// 存储初始化的BS 插值点
    /// </summary>
    [Serializable]
    public class BlendShapeMeshInterpolatePoints
    {
#pragma warning disable 649
        [SerializeField]
        [Tooltip("BS名称")]
        string m_Name;

        [SerializeField]
        [Tooltip("初始化插值")]
        BlendShapeMeshInterpolatePoint[] m_BSInterpolatePoints = new BlendShapeMeshInterpolatePoint[] { };
#pragma warning restore 649

        public string name { get { return m_Name; } }
        public BlendShapeMeshInterpolatePoint[] interpolatePoints { get { return m_BSInterpolatePoints; } }
        public BlendShapeMeshInterpolatePoints() { }
        public BlendShapeMeshInterpolatePoints(string name)
        {
            m_Name = name;
        }

    }
}
