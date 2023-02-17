using System.Collections.Generic;
using UnityEngine;

namespace ComeSocial.Face.Drive
{
    /// <summary>
    /// stream reader 接口
    /// </summary>
    public interface IStreamReader
    {
        /// <summary>
        /// 当前活动的 stream source
        /// </summary>
        IStreamSource streamSource { get; }


        /// <summary>
        /// 是否追踪到面部
        /// </summary>
        bool trackingActive { get; }

        /// <summary>
        /// 当前BS的权重
        /// </summary>
        float[] blendShapesBuffer { get; }

        //姿态
        Pose headPose { get; }
        Pose leftEyePose { get; }
        Pose rightEyePose { get; }


        //Remote 更新
        void UpdateStreamData(byte[] buffer, int offset = 0, int len = 0);
        void UpdateHeadPoseStreamData(byte[] buffer, int offset = 0, int len = 0);
        void UpdateEyePoseStreamData(byte[] buffer, int offset = 0, int len = 0);

        //Local 更新
        void UpdateARFaceFromLocal(bool currentIsTracked, ref Dictionary<string, float> currentBlendShapes, ref Matrix4x4 currentTransform, ref Pose currentLeftEyePose, ref Pose currentRightEyePose);
        void UpdateARFaceBlendShapeFromLocal(ref Dictionary<string, float> blendShapeDatas);
    }
}
