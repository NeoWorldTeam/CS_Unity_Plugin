using System;
using UnityEngine;

namespace ComeSocial.Face.Drive
{
    /// <inheritdoc cref="IUsesStreamReader" />
    /// <summary>
    /// Updates tracking pose values from the stream reader to the transformed referenced in this script.
    /// </summary>
    public class CharacterRigController : MonoBehaviour, IUsesStreamReader
    {
#pragma warning disable 649
        [Range(0f, 1f)]
        [SerializeField]
        [Tooltip("Amount to smoothing when returning to the start pose for the character when AR tracking is lost.")]
        float m_TrackingLossSmoothing = 0.1f;

        [SerializeField]
        [Tooltip("Enable controller driving eye bones pose.")]
        bool m_DriveEyes = true;

        [SerializeField]
        [Tooltip("Left eye bone transform")]
        Transform m_LeftEye;

        [SerializeField]
        [Tooltip("Right eye bone transform")]
        Transform m_RightEye;


        [SerializeField]
        [Tooltip("Max amount of x and y movement for the eyes.")] 
        Vector2 m_EyeAngleRange = new Vector2(30, 45);  

        [SerializeField]
        [Tooltip("Enable controller driving head bone pose.")]
        bool m_DriveHead = true;

        [SerializeField]
        [Tooltip("Head bone transform")]
        Transform m_HeadBone;

        [Range(0f, 1f)]
        [SerializeField]
        [Tooltip("Amount of smoothing to apply to head movement")]
        float m_HeadSmoothing = 0.1f;

        [Range(0f, 1f)]
        [SerializeField]
        [Tooltip("Amount of influence the AR head anchor pose has on the head bone.")]
        float m_HeadFollowAmount = 0.6f;

        [SerializeField]
        [Tooltip("Enable controller driving neck bone pose.")]
        bool m_DriveNeck = true;

        [SerializeField]
        [Tooltip("Neck bone transform")]
        Transform m_NeckBone;

        [Range(0f, 1f)]
        [SerializeField]
        [Tooltip("Amount of influence the AR head anchor pose has on the neck bone.")]
        float m_NeckFollowAmount = 0.4f;
#pragma warning restore 649

        int m_EyeLookDownLeftIndex;
        int m_EyeLookDownRightIndex;
        int m_EyeLookInLeftIndex;
        int m_EyeLookInRightIndex;
        int m_EyeLookOutLeftIndex;
        int m_EyeLookOutRightIndex;
        int m_EyeLookUpLeftIndex;
        int m_EyeLookUpRightIndex;

        float m_EyeLookDownLeft;
        float m_EyeLookDownRight;
        float m_EyeLookInLeft;
        float m_EyeLookInRight;
        float m_EyeLookOutLeft;
        float m_EyeLookOutRight;
        float m_EyeLookUpLeft;
        float m_EyeLookUpRight;

        Transform m_ARHeadPose;
        Transform m_ARNeckPose;
        Transform m_AREyePose;

        Transform m_ARLeftEyePose;
        Transform m_ARRightEyePose;

        Transform m_EyePoseLookAt;
        Transform m_EyeRightPoseLookAt;
        Transform m_EyeLeftPoseLookAt;

        //Transform m_LocalizedHeadParent;
        //Transform m_LocalizedHeadRot;
        //Transform m_LocalizedEyeRot;
        //Transform m_OtherThing;
        //Transform m_OtherLook;

        Pose m_HeadStartPose;
        Pose m_NeckStartPose;
        Pose m_RightEyeStartPose;
        Pose m_LeftEyeStartPose;

        Quaternion m_LastHeadRotation;
        Vector3 m_LastHeadPositon;
        Quaternion m_LastNeckRotation;
        //Quaternion m_LastLeftEyeRotation;
        //Quaternion m_LastRightEyeRotation;

        Quaternion m_BackwardRot = Quaternion.Euler(0, 180, 0);

        IStreamSettings m_LastStreamSettings;

        public IStreamReader streamReader { private get; set; }
        public Transform headBone {
            get { return m_HeadBone != null ? m_HeadBone : transform; }
            set { m_HeadBone = value; }
        }
        public Transform leftEyeBone {
            get { return m_LeftEye != null ? m_LeftEye : transform; }
            set { m_LeftEye = value; }
        }
        public Transform rightEyeBone
        {
            get { return m_RightEye != null ? m_RightEye : transform; }
            set { m_RightEye = value; }
        }

        public bool driveEyes { get { return m_DriveEyes; } }
        public bool driveHead { get { return m_DriveHead; } }
        public bool driveNeck { get { return m_DriveNeck; } }

        [NonSerialized]
        [HideInInspector]
        public Transform[] animatedBones = new Transform [4];

        void Start()
        {
            SetupCharacterRigController();
        }

        void Update()
        {
            var streamSource = streamReader.streamSource;
            if (streamSource == null || !streamSource.active)
                return;

            var streamSettings = streamReader.streamSource.streamSettings;
            if (streamSettings != m_LastStreamSettings)
                UpdateBlendShapeIndices(streamSettings);

            InterpolateBlendShapes();
        }

        void LateUpdate()
        {
            var streamSource = streamReader.streamSource;
            if (streamSource == null || !streamSource.active)
                return;

            UpdateBoneTransforms();
        }

        public void UpdateBlendShapeIndices(IStreamSettings settings)
        {
            m_LastStreamSettings = settings;
            m_EyeLookDownLeftIndex = settings.GetLocationIndex(BlendShapeUtils.EyeLookDownLeft);
            m_EyeLookDownRightIndex = settings.GetLocationIndex(BlendShapeUtils.EyeLookDownRight);
            m_EyeLookInLeftIndex = settings.GetLocationIndex(BlendShapeUtils.EyeLookInLeft);
            m_EyeLookInRightIndex = settings.GetLocationIndex(BlendShapeUtils.EyeLookInRight);
            m_EyeLookOutLeftIndex = settings.GetLocationIndex(BlendShapeUtils.EyeLookOutLeft);
            m_EyeLookOutRightIndex = settings.GetLocationIndex(BlendShapeUtils.EyeLookOutRight);
            m_EyeLookUpLeftIndex = settings.GetLocationIndex(BlendShapeUtils.EyeLookUpLeft);
            m_EyeLookUpRightIndex = settings.GetLocationIndex(BlendShapeUtils.EyeLookUpRight);
        }

        public void SetupCharacterRigController()
        {
            Debug.Log("Start avatar Setup");

            if (m_DriveEyes)
            {
                if (m_LeftEye == null)
                {
                    Debug.LogWarning("Drive Eyes is set but Left Eye Bone returning NULL!");
                    m_DriveEyes = false;
                }

                if (m_RightEye == null)
                {
                    Debug.LogWarning("Drive Eyes is set but Right Eye Bone returning NULL!");
                    m_DriveEyes = false;
                }
            }

            if (m_DriveHead && m_HeadBone == null)
            {
                Debug.LogWarning("Drive Head is set but Head Bone returning NULL!");
                m_DriveHead = false;
            }

            if (m_DriveNeck && m_NeckBone == null)
            {
                Debug.LogWarning("Drive Neck is set but Neck Bone returning NULL!");
                m_DriveNeck = false;
            }

            Pose headWorldPose;
            Pose headLocalPose;
            if (m_DriveHead)
            {
                // ReSharper disable once PossibleNullReferenceException
                headWorldPose = new Pose(m_HeadBone.position, m_HeadBone.rotation);
                headLocalPose = new Pose(m_HeadBone.localPosition, m_HeadBone.localRotation);
            }
            else if (m_DriveNeck)
            {
                // ReSharper disable once PossibleNullReferenceException
                headWorldPose = new Pose(m_NeckBone.position, m_NeckBone.rotation);
                headLocalPose = new Pose(m_NeckBone.localPosition, m_NeckBone.localRotation);
            }
            else
            {
                headWorldPose = new Pose(transform.position, transform.rotation);
                headLocalPose = new Pose(transform.localPosition, transform.localRotation);
            }

            Pose neckWorldPose;
            Pose neckLocalPose;
            if (m_DriveNeck)
            {
                // ReSharper disable once PossibleNullReferenceException
                neckWorldPose = new Pose(m_NeckBone.position, m_NeckBone.rotation);
                neckLocalPose = new Pose(m_NeckBone.localPosition, m_NeckBone.localRotation);
            }
            else if (m_HeadBone)
            {
                // ReSharper disable once PossibleNullReferenceException
                neckWorldPose = new Pose(m_HeadBone.position, m_HeadBone.rotation);
                neckLocalPose = new Pose(m_HeadBone.localPosition, m_HeadBone.localRotation);
            }
            else
            {
                neckWorldPose = new Pose(transform.position, transform.rotation);
                neckLocalPose = new Pose(transform.localPosition, transform.localRotation);
            }

            Pose eyeLeftWorldPose;
            Pose eyeLeftLocalPose;
            Pose eyeRightWorldPose;
            Pose eyeRightLocalPose;
            if (m_DriveEyes)
            {
                // ReSharper disable once PossibleNullReferenceException
                eyeLeftWorldPose = new Pose(m_LeftEye.position, m_LeftEye.rotation);
                eyeLeftLocalPose = new Pose(m_LeftEye.localPosition, m_LeftEye.localRotation);
                // ReSharper disable once PossibleNullReferenceException
                eyeRightWorldPose = new Pose(m_RightEye.position, m_RightEye.rotation);
                eyeRightLocalPose = new Pose(m_RightEye.localPosition, m_RightEye.localRotation);
            }
            else
            {
                eyeLeftWorldPose = new Pose(headWorldPose.position, headWorldPose.rotation);
                eyeLeftLocalPose = new Pose(headLocalPose.position, headLocalPose.rotation);
                eyeRightWorldPose = new Pose(headWorldPose.position, headWorldPose.rotation);
                eyeRightLocalPose = new Pose(headLocalPose.position, headLocalPose.rotation);
            }

            // Set Head Look Rig
            var headPoseObject = new GameObject("head_pose");
            m_ARHeadPose = headPoseObject.transform;
            m_ARHeadPose.SetPositionAndRotation(headWorldPose.position, headWorldPose.rotation);

            var headOffset = new GameObject("head_offset").transform;
            headOffset.SetPositionAndRotation(headWorldPose.position, Quaternion.identity);
            headOffset.SetParent(transform, true);
            m_ARHeadPose.SetParent(headOffset, true);

            m_HeadStartPose = new Pose(headOffset.position, headOffset.rotation);
            //m_ARHeadPose.localRotation = Quaternion.identity;

            // Set Neck Look Rig
            var neckPoseObject = new GameObject("neck_pose"){ hideFlags = HideFlags.HideAndDontSave};
            m_ARNeckPose = neckPoseObject.transform;
            m_NeckStartPose = new Pose(neckLocalPose.position, neckLocalPose.rotation);
            m_ARNeckPose.SetPositionAndRotation(neckWorldPose.position, Quaternion.identity);

            var neckOffset = new GameObject("neck_offset"){ hideFlags = HideFlags.HideAndDontSave}.transform;
            neckOffset.SetPositionAndRotation(neckWorldPose.position, neckWorldPose.rotation);
            neckOffset.SetParent(transform, true);
            m_ARNeckPose.SetParent(neckOffset, true);
            m_ARNeckPose.localRotation = Quaternion.identity;

            //眼球的位置
            var leftEyePoseObject = new GameObject("left_eye_pose");
            m_ARLeftEyePose = leftEyePoseObject.transform;
            m_ARLeftEyePose.SetPositionAndRotation(eyeLeftWorldPose.position, eyeLeftWorldPose.rotation);

            //眼球计算变换的位置
            var leftEyeOffset = new GameObject("left_eye_offset").transform;
            leftEyeOffset.SetPositionAndRotation(eyeLeftWorldPose.position, Quaternion.identity);
            leftEyeOffset.SetParent(headOffset, true);

            m_LeftEyeStartPose = new Pose(leftEyeOffset.position, leftEyeOffset.rotation);
            m_ARLeftEyePose.SetParent(leftEyeOffset, true);
            //m_ARLeftEyePose.localRotation = Quaternion.identity;




            var rightEyePoseObject = new GameObject("right_eye_pose");
            m_ARRightEyePose = rightEyePoseObject.transform;
      
            m_ARRightEyePose.SetPositionAndRotation(eyeRightWorldPose.position, eyeRightWorldPose.rotation);

            var rightEyeOffset = new GameObject("right_eye_offset").transform;
            rightEyeOffset.SetPositionAndRotation(eyeRightWorldPose.position, Quaternion.identity);
            rightEyeOffset.SetParent(headOffset, true);


            m_RightEyeStartPose = new Pose(rightEyeOffset.position, rightEyeOffset.rotation);
            m_ARRightEyePose.SetParent(rightEyeOffset, true);


            if (m_HeadBone != null)
                animatedBones[0] = m_HeadBone;

            if (m_NeckBone != null)
                animatedBones[1] = m_NeckBone;

            if (m_LeftEye != null)
                animatedBones[2] = m_LeftEye;

            if (m_RightEye != null)
                animatedBones[3] = m_RightEye;
        }

        public void ResetBonePoses()
        {
            if (m_DriveEyes)
            {
                m_RightEye.localPosition = m_RightEyeStartPose.position;
                m_RightEye.localRotation = m_RightEyeStartPose.rotation;

                m_LeftEye.localPosition = m_LeftEyeStartPose.position;
                m_LeftEye.localRotation = m_LeftEyeStartPose.rotation;
            }

            if (m_DriveHead)
            {
                m_HeadBone.localPosition = m_HeadStartPose.position;
                m_HeadBone.localRotation = m_HeadStartPose.rotation;
            }

            if (m_DriveNeck)
            {
                m_NeckBone.localPosition = m_NeckStartPose.position;
                m_NeckBone.localRotation = m_NeckStartPose.rotation;
            }
        }

        public void InterpolateBlendShapes(bool force = false)
        {
            var streamSource = streamReader.streamSource;
            if (streamSource == null)
                return;

            if (force || streamReader.trackingActive)
            {
                LocalizeFacePose();

                //眼睛
                var leftEyePose = streamReader.leftEyePose;
                var rightEyePose = streamReader.rightEyePose;

                var leftEyeRot = m_BackwardRot * leftEyePose.rotation * m_BackwardRot;
                var rightEyeRot = m_BackwardRot * rightEyePose.rotation * m_BackwardRot;

                Vector3 leftEyeAngles = leftEyeRot.eulerAngles;
                leftEyeAngles.x = ClampEulerAngle(leftEyeAngles.x, m_EyeAngleRange.x);
                leftEyeAngles.y = ClampEulerAngle(leftEyeAngles.y, m_EyeAngleRange.y);
                leftEyeRot = Quaternion.Euler(leftEyeAngles);


                Vector3 rightEyeAngles = rightEyeRot.eulerAngles;
                rightEyeAngles.x = ClampEulerAngle(rightEyeAngles.x, m_EyeAngleRange.x);
                rightEyeAngles.y = ClampEulerAngle(rightEyeAngles.y, m_EyeAngleRange.y);
                rightEyeRot = Quaternion.Euler(rightEyeAngles);

                //leftEyeRot = Quaternion.Slerp(leftEyeRot, rightEyeRot, 0.5f) * m_BackwardRot;
                m_ARLeftEyePose.parent.localRotation= leftEyeRot;
                m_ARRightEyePose.parent.localRotation = rightEyeRot;


                //头部
#if UNITY_EDITOR
                var headPose = streamReader.headPose;
                var headRot = m_BackwardRot * headPose.rotation * m_BackwardRot;

                headRot = Quaternion.Slerp(m_HeadStartPose.rotation, headRot, m_HeadFollowAmount);
                m_ARHeadPose.parent.localRotation = Quaternion.Slerp(headRot, m_LastHeadRotation, m_HeadSmoothing);
                m_LastHeadRotation = m_ARHeadPose.parent.localRotation;
#else
                var headPose = streamReader.headPose;
                var headRot = headPose.rotation;
                var headPositon = headPose.position;

                
                m_ARHeadPose.rotation = headPose.rotation * m_BackwardRot;
                m_ARHeadPose.position = headPositon;
                //Debug.LogFormat("2 face {0},{1},{2}", m_ARHeadPose.position.x, m_ARHeadPose.position.y, m_ARHeadPose.position.z);
                
                
#endif

                //脖子
                var neckRot = headRot;
                neckRot = Quaternion.Slerp(m_NeckStartPose.rotation, neckRot, m_NeckFollowAmount);
                m_ARNeckPose.localRotation = Quaternion.Slerp(neckRot, m_LastNeckRotation, m_HeadSmoothing);
                m_LastNeckRotation = m_ARNeckPose.localRotation;
            }
            else
            {
                if (m_DriveEyes)
                {
                    //m_AREyePose.localRotation = Quaternion.Slerp(Quaternion.identity, m_AREyePose.localRotation, m_TrackingLossSmoothing);
                    //m_ARHeadPose.parent.localRotation = Quaternion.Slerp(Quaternion.identity, m_ARHeadPose.parent.localRotation, m_TrackingLossSmoothing);
                }

                m_LastHeadRotation = m_ARHeadPose.parent.localRotation;
            }

            if (force)
                UpdateBoneTransforms();
        }

        private float ClampEulerAngle(float x1, float x2)
        {
            if (x1 > 90)
            {
                return Mathf.Clamp(x1, 360 - x2, 360 + x2);
            }
            else
            {
                return Mathf.Clamp(x1, -x2, x2);
            }
        }

        void LocalizeFacePose()
        {
            //var headPose = streamReader.headPose;
            //m_LocalizedHeadParent.position = headPose.position;
            //m_LocalizedHeadParent.LookAt(streamReader.cameraPose.position);

            //m_LocalizedHeadRot.rotation = m_BackwardRot * headPose.rotation * m_BackwardRot;
            //m_OtherThing.LookAt(m_OtherLook, m_OtherLook.up);
        }

        void UpdateBoneTransforms()
        {
            if (m_DriveEyes)
            {
                //m_LeftEye.rotation = m_ARLeftEyePose.rotation;
                //m_RightEye.rotation = m_ARRightEyePose.rotation;
            }

            if (m_DriveHead)
            {

#if UNITY_EDITOR
                m_HeadBone.rotation = m_ARHeadPose.rotation;
#else
                m_HeadBone.rotation = m_ARHeadPose.rotation;
                m_HeadBone.position = m_ARHeadPose.position;
                //Debug.LogFormat("1 face {0},{1},{2}", m_HeadBone.position.x, m_HeadBone.position.y, m_HeadBone.position.z);
#endif
            }


            //if (m_DriveNeck )
            //    m_NeckBone.rotation = m_ARNeckPose.rotation;
        }
    }
}
