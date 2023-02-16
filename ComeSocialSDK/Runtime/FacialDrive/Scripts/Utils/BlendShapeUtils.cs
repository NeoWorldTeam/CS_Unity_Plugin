using System;
using UnityEngine;



namespace ComeSocial.Face.Drive
{
    public static class BlendShapeUtils
    {
        public const int PoseFloatCount = 7;
        public const int PoseSize = 4 * 4 * 4;

        public const string  BrowDownLeft        =   "BrowDownLeft";
        public const string  BrowDownRight       =   "BrowDownRight";
        public const string  BrowInnerUp         =   "BrowInnerUp";
        public const string  BrowOuterUpLeft     =   "BrowOuterUpLeft";
        public const string  BrowOuterUpRight    =   "BrowOuterUpRight";
        public const string  CheekPuff           =   "CheekPuff";
        public const string  CheekSquintLeft     =   "CheekSquintLeft";
        public const string  CheekSquintRight    =   "CheekSquintRight";
        public const string  EyeBlinkLeft        =   "EyeBlinkLeft";
        public const string  EyeBlinkRight       =   "EyeBlinkRight";
        public const string  EyeLookDownLeft     =   "EyeLookDownLeft";
        public const string  EyeLookDownRight    =   "EyeLookDownRight";
        public const string  EyeLookInLeft       =   "EyeLookInLeft";
        public const string  EyeLookInRight      =   "EyeLookInRight";
        public const string  EyeLookOutLeft      =   "EyeLookOutLeft";
        public const string  EyeLookOutRight     =   "EyeLookOutRight";
        public const string  EyeLookUpLeft       =   "EyeLookUpLeft";
        public const string  EyeLookUpRight      =   "EyeLookUpRight";
        public const string  EyeSquintLeft       =   "EyeSquintLeft";
        public const string  EyeSquintRight      =   "EyeSquintRight";
        public const string  EyeWideLeft         =   "EyeWideLeft";
        public const string  EyeWideRight        =   "EyeWideRight";
        public const string  JawForward          =   "JawForward";
        public const string  JawLeft             =   "JawLeft";
        public const string  JawOpen             =   "JawOpen";
        public const string  JawRight            =   "JawRight";
        public const string  MouthClose          =   "MouthClose";
        public const string  MouthDimpleLeft     =   "MouthDimpleLeft";
        public const string  MouthDimpleRight    =   "MouthDimpleRight";
        public const string  MouthFrownLeft      =   "MouthFrownLeft";
        public const string  MouthFrownRight     =   "MouthFrownRight";
        public const string  MouthFunnel         =   "MouthFunnel";
        public const string  MouthLeft           =   "MouthLeft";
        public const string  MouthLowerDownLeft  =   "MouthLowerDownLeft";
        public const string  MouthLowerDownRight =   "MouthLowerDownRight";
        public const string  MouthPressLeft      =   "MouthPressLeft";
        public const string  MouthPressRight     =   "MouthPressRight";
        public const string  MouthPucker         =   "MouthPucker";
        public const string  MouthRight          =   "MouthRight";
        public const string  MouthRollLower      =   "MouthRollLower";
        public const string  MouthRollUpper      =   "MouthRollUpper";
        public const string  MouthShrugLower     =   "MouthShrugLower";
        public const string  MouthShrugUpper     =   "MouthShrugUpper";
        public const string  MouthSmileLeft      =   "MouthSmileLeft";
        public const string  MouthSmileRight     =   "MouthSmileRight";
        public const string  MouthStretchLeft    =   "MouthStretchLeft";
        public const string  MouthStretchRight   =   "MouthStretchRight";
        public const string  MouthUpperUpLeft    =   "MouthUpperUpLeft";
        public const string  MouthUpperUpRight   =   "MouthUpperUpRight";
        public const string  NoseSneerLeft       =   "NoseSneerLeft";
        public const string  NoseSneerRight      =   "NoseSneerRight";
        public const string  TongueOut           =   "TongueOut";

        /// <summary>
        /// Array of the blend shape locations supported by the unity ARKit plugin.
        /// </summary>
        /// <remarks>
        /// ARKIT_2_0 is a custom scripting define symbol and will need to be enabled in
        /// 'PlayerSettings>platform>Scripting Define Symbols' for use in build
        /// </remarks>
        public static readonly string[] Locations =
        {
            BrowDownLeft,
            BrowDownRight,
            BrowInnerUp,
            BrowOuterUpLeft,
            BrowOuterUpRight,
            CheekPuff,
            CheekSquintLeft,
            CheekSquintRight,
            EyeBlinkLeft,
            EyeBlinkRight,
            EyeLookDownLeft,
            EyeLookDownRight,
            EyeLookInLeft,
            EyeLookInRight,
            EyeLookOutLeft,
            EyeLookOutRight,
            EyeLookUpLeft,
            EyeLookUpRight,
            EyeSquintLeft,
            EyeSquintRight,
            EyeWideLeft,
            EyeWideRight,
            JawForward,
            JawLeft,
            JawOpen,
            JawRight,
            MouthClose,
            MouthDimpleLeft,
            MouthDimpleRight,
            MouthFrownLeft,
            MouthFrownRight,
            MouthFunnel,
            MouthLeft,
            MouthLowerDownLeft,
            MouthLowerDownRight,
            MouthPressLeft,
            MouthPressRight,
            MouthPucker,
            MouthRight,
            MouthRollLower,
            MouthRollUpper,
            MouthShrugLower,
            MouthShrugUpper,
            MouthSmileLeft,
            MouthSmileRight,
            MouthStretchLeft,
            MouthStretchRight,
            MouthUpperUpLeft,
            MouthUpperUpRight,
            NoseSneerLeft,
            NoseSneerRight,
            TongueOut,
        };

        /// <summary>
        /// Used for mapping the the blendshape locations this returns the index of the string in the Locations array.
        /// </summary>
        /// <param name="streamSettings">Stream Setting that contains the Locations array.</param>
        /// <param name="location">Name of blendshape location you want to find.</param>
        /// <returns>Index of string in Locations array.</returns>
        public static int GetLocationIndex(this IStreamSettings streamSettings, string location)
        {
            return Array.IndexOf(streamSettings.locations, location);
        }

        /// <summary>
        /// Takes a correctly formatted array and returns a pose from that array.
        /// </summary>
        /// <param name="poseArray">Array of floats that encodes a pose.</param>
        /// <param name="pose">Pose encoded in the float array.</param>
        public static void ArrayToPose(float[] poseArray, ref Pose pose)
        {

            pose.position = new Vector3(poseArray[0], poseArray[1], poseArray[2]);
            pose.rotation = new Quaternion(poseArray[3], poseArray[4], poseArray[5], poseArray[6]);




            return;
            Matrix4x4 m = new Matrix4x4(new Vector4(poseArray[0], poseArray[1], poseArray[2], poseArray[3]), new Vector4(poseArray[4], poseArray[5], poseArray[6], poseArray[7]), new Vector4(-poseArray[8], -poseArray[9], -poseArray[10], poseArray[11]), new Vector4(poseArray[12], poseArray[13], poseArray[14], poseArray[15]));
            //Debug.LogFormat("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15}",
            //    m.m00, m.m01, m.m02, m.m03,
            //    m.m10, m.m11, m.m12, m.m13,
            //    m.m20, m.m21, m.m22, m.m23,
            //    m.m30, m.m31, m.m32, m.m33);

            //Debug.LogFormat("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15}",
            //  poseArray[0], poseArray[1], poseArray[2], poseArray[3],
            //  poseArray[4], poseArray[5], poseArray[6], poseArray[7],
            //  poseArray[8], poseArray[9], poseArray[10], poseArray[11],
            //  poseArray[12], poseArray[13], poseArray[14], poseArray[15]);
            Vector3 position = m.ExtractPosition();
            Quaternion rotation = m.ExtractRotation();
            Vector3 scale = m.ExtractScale();

            Debug.Log("Y degress is : " + rotation.eulerAngles.y);
            //rotation = Quaternion.Euler(rotation.eulerAngles.x, -rotation.eulerAngles.y, rotation.eulerAngles.z);


            //rotation.z = -rotation.z; 
            //rotation.w = -rotation.w;

            //Debug.Log("==== After:");
            //Debug.Log("position == " + position);
            //Debug.Log("rotation == " + rotation);
            //Debug.Log("scale == " + scale);

            pose.position = position;
            pose.rotation = rotation;
        }

        /// <summary>
        /// Takes a pose and encodes the values to the given correctly formatted pose array.
        /// </summary>
        /// <param name="pose">Pose to encode in the float array.</param>
        /// <param name="poseArray">Float array to that the pose is encoded to.</param>
        public static void PoseToArray(Pose pose, float[] poseArray)
        {
            var position = pose.position;
            var rotation = pose.rotation;
            poseArray[0] = position.x;
            poseArray[1] = position.y;
            poseArray[2] = position.z;
            poseArray[3] = rotation.x;
            poseArray[4] = rotation.y;
            poseArray[5] = rotation.z;
            poseArray[6] = rotation.w;
        }
    }
}
