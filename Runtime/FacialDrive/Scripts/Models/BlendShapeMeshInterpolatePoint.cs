using System;
using UnityEngine;

namespace ComeSocial.Face.Drive
{
    [Serializable]
    public class BlendShapeMeshInterpolatePoint
    {
        float m_X;
        float m_Y;
        float m_weight;

        public float x { get { return m_X; } }
        public float y { get { return m_Y; } }
        public float weight { get { return m_weight; } }


        public BlendShapeMeshInterpolatePoint()
        {
            m_X = 0;
            m_Y = 0;
            m_weight = 0;
        }
    }
}
