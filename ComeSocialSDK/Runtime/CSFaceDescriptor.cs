using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ComeSocial;
using System;
using System.ComponentModel;

namespace ComeSocial.Face.Drive
{
    public class CSFaceDescriptor : MonoBehaviour, IFaceMaskFactory
    {
        [SerializeField]
        Transform m_HeadTransform;

        [SerializeField]
        Transform m_LeftEyeTransform;

        [SerializeField]
        Transform m_RightEyeTransform;

        [SerializeField]
        SkinnedMeshRenderer[] m_ExpressionTargets;



        public Transform HeadTransform
        {
            get { return m_HeadTransform != null ? m_HeadTransform : transform; }
            set { m_HeadTransform = value; }
        }

        public Transform LeftEyeTransform
        {
            get { return m_LeftEyeTransform != null ? m_LeftEyeTransform : transform; }
            set { m_LeftEyeTransform = value; }
        }

        public Transform RightEyeTransform
        {
            get { return m_RightEyeTransform != null ? m_RightEyeTransform : transform; }
            set { m_RightEyeTransform = value; }
        }

        public SkinnedMeshRenderer[] ExpressionTargets
        {
            get { return m_ExpressionTargets; }
            set { m_ExpressionTargets = value; }
        }




#if UNITY_EDITOR
        private void Start()
        {
            initCompenetnByDescriptor(this);
        }

        public void initCompenetnByDescriptor(CSFaceDescriptor faceDescriptor)
        {
            IStreamSource inputStream = null;
            CharacterRigController rigController = null;
            BlendShapesController blendShapesController = null;

#if UNITY_EDITOR
            inputStream = gameObject.AddComponent<NetworkStream>();
#else
            inputStream = gameObject.AddComponent<LocalStream>();
#endif
            StreamReader streamReader = gameObject.AddComponent<StreamReader>();
            streamReader.streamSource = inputStream;




            if (faceDescriptor.HeadTransform)
            {
                rigController = gameObject.AddComponent<CharacterRigController>();
                rigController.headBone = faceDescriptor.HeadTransform;
                rigController.leftEyeBone = faceDescriptor.LeftEyeTransform;
                rigController.rightEyeBone = faceDescriptor.RightEyeTransform;
                rigController.streamReader = streamReader;
            }


            if (faceDescriptor.ExpressionTargets != null && faceDescriptor.ExpressionTargets.Length > 0)
            {
                blendShapesController = gameObject.AddComponent<BlendShapesController>();
                blendShapesController.skinnedMeshRenderers = faceDescriptor.ExpressionTargets;
                blendShapesController.streamReader = streamReader;
            }
        }
#endif
    }
}


