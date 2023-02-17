using System;
using System.Collections.Generic;
using UnityEngine;

namespace ComeSocial.Face.Drive
{
    /// <inheritdoc cref="IStreamReader" />
    /// <summary>
    /// This component acts as the hub for using the Facial AR Remote in editor. It is responsible for processing the
    /// stream data from the stream source(s) to be used by connected controllers. It allows you to control the device
    /// connection, and record and playback captured streams to a character.
    /// </summary>
    public class StreamReader : MonoBehaviour, IStreamReader
    {
#pragma warning disable 649
        [SerializeField]
        [Tooltip("驱动的对象")]
        GameObject m_Character;


        [SerializeField]
        [Range(1, 512)]
        [Tooltip("丢失认定帧数")]
        int m_TrackingLossPadding = 64;

        [SerializeField]
        [Tooltip("(可选）手动指定 BlendShape Controller")]
        BlendShapesController m_BlendShapesControllerOverride;

        [SerializeField]
        [Tooltip("(可选）手动指定 Rig Controller")]
        CharacterRigController m_CharacterRigControllerOverride;

        [SerializeField]
        [Tooltip("(可选）手动指定 VFX Controller")]
        VFXController m_VFXControllerOverride;

        [SerializeField]
        [Tooltip("(可选）手动指定角色的起始姿势。")]
        Transform m_HeadBoneOverride;


        [SerializeField]
        [Tooltip("(可选）手动指定stream sources")]
        GameObject[] m_StreamSourceOverrides = { };
#pragma warning restore 649

        //驱动数据数据源
        IStreamSource m_ActiveStreamSource;

        //统计没有追踪到面部的帧数
        int m_TrackingLossCount;

        //头部姿态
        Pose m_HeadPose;
        //左眼姿态
        Pose m_LeftEyePose;
        //右眼姿态
        Pose m_RightEyePose;

        //上一次位置
        Pose m_LastHeadPose;


        
        float[] m_HeadPoseArray = new float[BlendShapeUtils.PoseFloatCount];
        float[] m_LeftEyePoseArray = new float[BlendShapeUtils.PoseFloatCount];
        float[] m_RightEyePoseArray = new float[BlendShapeUtils.PoseFloatCount];

        int[] m_FrameNumArray = new int[1];
        float[] m_FrameTimeArray = new float[1];

        HashSet<IStreamSource> m_Sources = new HashSet<IStreamSource>();

        //bs 缓存
        public float[] blendShapesBuffer { get; private set; }
        public bool trackingActive { get; private set; }

        public Pose headPose { get { return m_HeadPose; } }
        public Pose leftEyePose { get { return m_LeftEyePose; } }
        public Pose rightEyePose { get { return m_RightEyePose; } }
        public Transform headBone { get; private set; }
        public HashSet<IStreamSource> sources { get { return m_Sources; } }
        public BlendShapesController blendShapesController { get; private set; }
        public CharacterRigController characterRigController { get; private set; }
        public VFXController vfxController { get; private set; }

        public IStreamSource streamSource
        {
            get { return m_ActiveStreamSource; }
            set
            {
                if (m_ActiveStreamSource == value)
                    return;

                m_ActiveStreamSource = value;

                if (value == null)
                    return;

                if (value.streamSettings == null)
                    return;


                //init blendShapesBuffer
                var blendShapeCount = value.streamSettings.locations.Length;
                if (blendShapesBuffer == null || blendShapesBuffer.Length != blendShapeCount)
                    blendShapesBuffer = new float[blendShapeCount];
            }
        }

        [Serializable]
        class BlendShapeMSG
        {
            public List<BlendShapeMSGItem> items;
        }


        [Serializable]
        class BlendShapeMSGItem
        {
            public string name;
            public float weight;
        }


        [Serializable]
        class HeadPostMSG
        {
            public float[] rotation_quaternion;
        }

        [Serializable]
        class EyePoseMSG
        {
            public float[] left_rotation_quaternion;
            public float[] right_rotation_quaternion;
        }

        //缓存[name:index]
        Dictionary<string, int> blendshapeIndexCahceDictionary = null;


        //需要先运行
        void Awake()
        {
            if (headBone == null)
            {
                m_HeadPose = new Pose(Vector3.zero, Quaternion.identity);
                Debug.LogWarning("No Character head bone set. Using default pose.");
            }
            else
            {
                m_HeadPose = new Pose(headBone.position, headBone.rotation);
            }
            m_LastHeadPose = m_HeadPose;
            trackingActive = false;
            ConnectDependencies();
        }

        void Update()
        {
            //判断是否追踪到面部
            if (m_HeadPose != Pose.identity)
            {
                if (m_HeadPose == m_LastHeadPose)
                {
                    m_TrackingLossCount++;
                    trackingActive = m_TrackingLossCount <= m_TrackingLossPadding;
                }
                else
                {
                    m_TrackingLossCount = 0;
                    trackingActive = true;
                    m_LastHeadPose = m_HeadPose;
                }
            }
           
            //更新输入源
            foreach (var source in m_Sources)
            {
                source.StreamSourceUpdate();
            }
        }



        ////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////从网络数据更新////////////////////////////////

        void IStreamReader.UpdateStreamData(byte[] buffer, int offset = 0, int len = 0)
        {
            string contetntJson = System.Text.Encoding.UTF8.GetString(buffer, 0, len - 1);
            UpdateStreamData(contetntJson);
        }

        void IStreamReader.UpdateHeadPoseStreamData(byte[] buffer, int offset = 0, int len = 0)
        {
            string contetntJson = System.Text.Encoding.UTF8.GetString(buffer, 0, len - 1);
            UpdateHeadPoseStreamData(contetntJson);
        }

        void IStreamReader.UpdateEyePoseStreamData(byte[] buffer, int offset = 0, int len = 0)
        {
            string contetntJson = System.Text.Encoding.UTF8.GetString(buffer, 0, len - 1);
            UpdateEyePoseStreamData(contetntJson);
        }


        void UpdateStreamData(string dataStr)
        {
            BlendShapeMSG bsMsg = JsonUtility.FromJson<BlendShapeMSG>(dataStr);

            var streamSettings = streamSource.streamSettings;

            if (blendshapeIndexCahceDictionary == null)
            {
                blendshapeIndexCahceDictionary = new Dictionary<string, int>();
                foreach (var bsItem in bsMsg.items)
                {
                    var index = -1;
                    foreach (var mapping in streamSettings.mappings)
                    {
                        var to = mapping.to;
                        var from = mapping.from;
                        if (bsItem.name.Contains(from))
                            index = Array.IndexOf(streamSettings.locations, from);

                        if (bsItem.name.Contains(to))
                            index = Array.IndexOf(streamSettings.locations, from);
                    }


                    if (index != -1)
                    {
                        blendshapeIndexCahceDictionary[bsItem.name] = index;
                        blendShapesBuffer[index] = bsItem.weight;
                    }
                    else
                    {

                        Debug.Log("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx");
                    }
                }

            }
            else
            {
                foreach (var bsItem in bsMsg.items)
                {
                    var index = blendshapeIndexCahceDictionary[bsItem.name];
                    blendShapesBuffer[index] = bsItem.weight;
                }

            }
        }

        void UpdateHeadPoseStreamData(string dataStr)
        {
            HeadPostMSG bsMsg = JsonUtility.FromJson<HeadPostMSG>(dataStr);
            m_HeadPoseArray[3] = bsMsg.rotation_quaternion[0];
            m_HeadPoseArray[4] = bsMsg.rotation_quaternion[1];
            m_HeadPoseArray[5] = bsMsg.rotation_quaternion[2];
            m_HeadPoseArray[6] = bsMsg.rotation_quaternion[3];

            BlendShapeUtils.ArrayToPose(m_HeadPoseArray, ref m_HeadPose);
        }

        void UpdateEyePoseStreamData(string dataStr)
        {
            EyePoseMSG bsMsg = JsonUtility.FromJson<EyePoseMSG>(dataStr);
            m_LeftEyePoseArray[3] = bsMsg.left_rotation_quaternion[0];
            m_LeftEyePoseArray[4] = bsMsg.left_rotation_quaternion[1];
            m_LeftEyePoseArray[5] = bsMsg.left_rotation_quaternion[2];
            m_LeftEyePoseArray[6] = bsMsg.left_rotation_quaternion[3];

            m_RightEyePoseArray[3] = bsMsg.right_rotation_quaternion[0];
            m_RightEyePoseArray[4] = bsMsg.right_rotation_quaternion[1];
            m_RightEyePoseArray[5] = bsMsg.right_rotation_quaternion[2];
            m_RightEyePoseArray[6] = bsMsg.right_rotation_quaternion[3];

            BlendShapeUtils.ArrayToPose(m_LeftEyePoseArray, ref m_LeftEyePose);
            BlendShapeUtils.ArrayToPose(m_RightEyePoseArray, ref m_RightEyePose);
        }

        ////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////
        ///








        ////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////从本地数据更新////////////////////////////////


        static Vector3 GetPositionToLHC(Matrix4x4 matrix)
        {
            // Convert from ARKit's right-handed coordinate
            // system to Unity's left-handed
            Vector3 position = matrix.GetColumn(3);
            position.z = -position.z;

            return position;
        }

        static Quaternion GetRotationToLHC(Matrix4x4 matrix)
        {
            // Convert from ARKit's right-handed coordinate
            // system to Unity's left-handed
            Quaternion rotation = QuaternionFromMatrix(matrix);
            rotation.z = -rotation.z;
            rotation.w = -rotation.w;

            return rotation;
        }
        static Quaternion QuaternionFromMatrix(Matrix4x4 m)
        {
            // Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
            Quaternion q = new Quaternion();
            q.w = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] + m[1, 1] + m[2, 2])) / 2;
            q.x = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] - m[1, 1] - m[2, 2])) / 2;
            q.y = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] + m[1, 1] - m[2, 2])) / 2;
            q.z = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] - m[1, 1] + m[2, 2])) / 2;
            q.x *= Mathf.Sign(q.x * (m[2, 1] - m[1, 2]));
            q.y *= Mathf.Sign(q.y * (m[0, 2] - m[2, 0]));
            q.z *= Mathf.Sign(q.z * (m[1, 0] - m[0, 1]));
            return q;
        }

        void IStreamReader.UpdateARFaceFromLocal(bool currentIsTracked, ref Dictionary<string, float> currentBlendShapes, ref Matrix4x4 currentTransform, ref Pose currentLeftEyePose, ref Pose currentRightEyePose)
        {
            if (currentIsTracked && currentBlendShapes != null && currentTransform != null && currentLeftEyePose != null && currentRightEyePose != null)
            {
                Vector3 position = GetPositionToLHC(currentTransform);
                //Debug.LogFormat("1 face {0},{1},{2}", position.x, position.y, position.z);
                Quaternion rotation = GetRotationToLHC(currentTransform);
                m_HeadPose = new Pose(position, rotation);
                m_LeftEyePose = currentLeftEyePose;
                m_RightEyePose = currentRightEyePose;
            }
        }

        void IStreamReader.UpdateARFaceBlendShapeFromLocal(ref Dictionary<string, float> blendShapeDatas) {
            if (blendshapeIndexCahceDictionary == null)
            {

                Debug.Log("BS mappings : "+ blendShapeDatas.Count);
                var streamSettings = streamSource.streamSettings;
                blendshapeIndexCahceDictionary = new Dictionary<string, int>();
                foreach (var bsItem in blendShapeDatas)
                {
                    var index = -1;
                    foreach (var mapping in streamSettings.mappings)
                    {
                        var to = mapping.to;
                        var from = mapping.from;
                        
                        if (bsItem.Key.Contains(from))
                            index = Array.IndexOf(streamSettings.locations, from);

                        if (bsItem.Key.Contains(to))
                            index = Array.IndexOf(streamSettings.locations, from);
                    }


                    if (index != -1)
                    {
                        blendshapeIndexCahceDictionary[bsItem.Key] = index;
                        blendShapesBuffer[index] = bsItem.Value;
                    }
                    else
                    {

                        Debug.Log("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx");
                    }
                }

            }
            else
            {
                foreach (var bsItem in blendShapeDatas)
                {
                    var index = blendshapeIndexCahceDictionary[bsItem.Key];
                    blendShapesBuffer[index] = bsItem.Value;
                }

            }
        }


        ////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////


        //链接默认对象依赖
        public void ConnectDependencies()
        {
            m_Sources.UnionWith(GetComponentsInChildren<IStreamSource>());
            foreach (var go in m_StreamSourceOverrides)
            {
                if (go == null) continue;
                m_Sources.UnionWith(go.GetComponentsInChildren<IStreamSource>());
            }

            foreach (var source in m_Sources)
            {
                ConnectInterfaces(source);
                streamSource = source;
            }

            if (m_Character != null)
            {
                blendShapesController = m_BlendShapesControllerOverride != null
                    ? m_BlendShapesControllerOverride
                    : m_Character.GetComponentInChildren<BlendShapesController>();

                characterRigController = m_CharacterRigControllerOverride != null
                    ? m_CharacterRigControllerOverride
                    : m_Character.GetComponentInChildren<CharacterRigController>();

                vfxController = m_VFXControllerOverride != null
                    ? m_VFXControllerOverride
                    : m_Character.GetComponentInChildren<VFXController>();

                headBone = m_HeadBoneOverride != null
                    ? m_HeadBoneOverride
                    : characterRigController != null
                        ? characterRigController.headBone
                        : null;
            }
            else
            {
                blendShapesController = m_BlendShapesControllerOverride;
                characterRigController = m_CharacterRigControllerOverride;
                vfxController = m_VFXControllerOverride;
                headBone = m_HeadBoneOverride;
            }

            //cameraTransform = m_CameraOverride == null ? Camera.main ? Camera.main.transform : null : m_CameraOverride.transform;

            if (blendShapesController != null)
                ConnectInterfaces(blendShapesController);

            if (characterRigController != null)
                ConnectInterfaces(characterRigController);


            if (vfxController != null)
                ConnectInterfaces(vfxController);
        }

        //链接设置streamReader接口
        void ConnectInterfaces(object obj)
        {
            var usesStreamReader = obj as IUsesStreamReader;
            if (usesStreamReader != null)
                usesStreamReader.streamReader = this;
        }

    }
}
