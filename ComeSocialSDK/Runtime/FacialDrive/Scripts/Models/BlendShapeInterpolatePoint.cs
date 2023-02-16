using System;
using UnityEngine;

namespace ComeSocial.Face.Drive
{
    /// <summary>
    /// Stores the override values for an individual blend shape.
    /// </summary>
    [Serializable]
    public class BlendShapeInterpolatePoint
    {
#pragma warning disable 649

        [Range(0f, 1)]
        [SerializeField]
        [Tooltip("Blend shape name to be overridden.")]
        float m_X;

        [Range(0f, 1)]
        [SerializeField]
        [Tooltip("Blend shape name to be overridden.")]
        float m_Y;
#pragma warning restore 649

        public float x { get { return m_X; } }
        public float y { get { return m_Y; } }


        public BlendShapeInterpolatePoint()
        {
            m_X = 0;
            m_Y = 0;
        }
    }
}
