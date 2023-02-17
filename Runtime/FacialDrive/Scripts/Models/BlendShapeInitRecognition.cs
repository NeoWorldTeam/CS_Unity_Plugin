using System;
using UnityEngine;

namespace ComeSocial.Face.Drive
{
    /// <summary>
    /// 存储初始化的BS权重
    /// </summary>
    [Serializable]
    public class BlendShapeInitRecognition
    {
#pragma warning disable 649
        [SerializeField]
        [Tooltip("BS名称")]
        string m_Name;

        [Range(0, 1)]
        [SerializeField]
        [Tooltip("初始化差值")]
        float m_BlendShapeInitOffset = 0f;
#pragma warning restore 649
        public string name { get { return m_Name; } }
        public float blendShapeInitOffset { get { return m_BlendShapeInitOffset; } set { m_BlendShapeInitOffset = value; } }

        BlendShapeInitRecognition(){}

        public BlendShapeInitRecognition(string name)
        {
            m_Name = name;
        }
    }
}
