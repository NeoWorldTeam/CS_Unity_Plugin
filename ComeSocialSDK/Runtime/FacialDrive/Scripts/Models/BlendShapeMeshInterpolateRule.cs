using System;
using System.Collections.Generic;
using UnityEngine;

namespace ComeSocial.Face.Drive
{
    /// <summary>
    /// 存储初始化的BS网格插值规则
    /// </summary>
    [Serializable]
    public class BlendShapeMeshInterpolateRule
    {
#pragma warning disable 649

        [SerializeField]
        [Tooltip("从BS集水平方向采集权重")]
        string horizontalBSSet;

        [SerializeField]
        [Tooltip("从BS集垂直方向采集权重")]
        string verticalBSSet;

        [SerializeField]
        [Tooltip("被插值BS目标")]
        string interpolateTargetBS;



        [SerializeField]
        [Tooltip("插值点水平分隔数")]
        int horizontalSection;

        [SerializeField]
        [Tooltip("插值点垂直分隔数")]
        int verticalSection;

        [SerializeField]
        [Tooltip("网格插值点数据,逗号分隔数据")]
        string interpolatePointValues;
#pragma warning restore 649

        bool _isValid = true;


        int[] horizontalBSIndexs;
        int[] verticalBSIndexs;
        int _targetBSIndex = -1;


        List<List<float>> bsMeshWights;

        public bool IsValid() { return _isValid; }
        public int TargetBSIndex() { return _targetBSIndex; }
        public bool NeedUpdate() { return horizontalBSIndexs == null; }



        public BlendShapeMeshInterpolateRule() { }


        internal void HandleBlendShapeRelative(string[] locations)
        {
            if (horizontalSection < 2 || verticalSection < 2)
            {
                Debug.LogError("Section 数据需要大于2");
                _isValid = false;
                return;
            }

            if(horizontalBSSet.Equals("") || verticalBSSet.Equals("") || interpolateTargetBS.Equals(""))
            {
                Debug.LogError("BS 没有输入");
                _isValid = false;
                return;
            }

            


            //解析插值点
            string[] pointsWeight = interpolatePointValues.Split(",");
            if (pointsWeight.Length != (horizontalSection * verticalSection))
            {
                if(pointsWeight.Length > 0) Debug.LogError("插值权重点数目不对");
                _isValid = false;
                return;
            }

            bsMeshWights = new List<List<float>>(horizontalSection);
            for (int i = 0; i < verticalSection; i++)
            {
                bsMeshWights.Add(new List<float>(horizontalSection));
                for (int j =0; j < horizontalSection; j++)
                {
                    string bsWeightStr = pointsWeight[i * horizontalSection + j];
                    float valueee = float.Parse(bsWeightStr);
                    bsMeshWights[i].Add(valueee);
                }
            }





            //计算BS真实Index
            //Dictionary<string, int> cleanBsIndexDic = CuculateMeshBlendShape(m_SkinnedMeshRenderers);

            //计算提供BS权重的BS Index
            {
                String[] horizontalBSs = horizontalBSSet.Split(",");
                String[] verticalBSs = verticalBSSet.Split(",");
                List<int> t_horizontalBSIndexs = new List<int>();
                List<int> t_verticalBSIndexs = new List<int>();

                for(int i = 0; i < locations.Length; i ++)
                {
                    string shapeName = locations[i];
                    int bsIndex = i;

                    if (findIndexFormBSName(horizontalBSs, shapeName))
                    {
                        t_horizontalBSIndexs.Add(bsIndex);
                    }

                    if (findIndexFormBSName(verticalBSs, shapeName))
                    {
                        t_verticalBSIndexs.Add(bsIndex);
                    }

                    if (interpolateTargetBS.ToLower().Trim().Equals(shapeName.ToLower().Trim()))
                    {
                        _targetBSIndex = bsIndex;
                    }
                }

                horizontalBSIndexs = t_horizontalBSIndexs.ToArray();
                verticalBSIndexs = t_verticalBSIndexs.ToArray();


                if (horizontalBSIndexs.Length < 1 )
                {
                    Debug.LogError("未找到横坐标上的BS 小于 1");
                    _isValid = false;
                    return;
                }else if(verticalBSIndexs.Length < 1)
                {
                    Debug.LogError("未找到纵坐标上的BS 小于 1");
                    _isValid = false;
                    return;
                }
                else if (horizontalBSIndexs.Length != horizontalBSs.Length)
                {
                    Debug.LogError("横坐标上的BS和输入BS 不一致");
                    _isValid = false;
                    return;
                }
                else if (verticalBSIndexs.Length != verticalBSs.Length)
                {
                    Debug.LogError("纵坐标上的BS和输入BS 不一致");
                    _isValid = false;
                    return;
                }
            }

            

            if(_targetBSIndex == -1)
            {
                Debug.LogError("未找到目标BS");
            }
        }

        //private Dictionary<string, int> CuculateMeshBlendShape(SkinnedMeshRenderer[] m_SkinnedMeshRenderers)
        //{
        //    Dictionary<string, int> cleanBsIndexDic = new Dictionary<string, int>();
        //    foreach (var renderer in m_SkinnedMeshRenderers)
        //    {
        //        if (renderer == null)
        //        {
        //            Debug.LogWarning("Null element in SkinnedMeshRenderer list in " + this);
        //            continue;
        //        }

        //        if (renderer.sharedMesh == null)
        //        {
        //            Debug.LogWarning("Missing mesh in " + renderer);
        //            continue;
        //        }

        //        var mesh = renderer.sharedMesh;
        //        var count = mesh.blendShapeCount;

        //        for (var i = 0; i < count; i++)
        //        {
        //            var shapeName = mesh.GetBlendShapeName(i).ToLower();
        //            if (cleanBsIndexDic.ContainsKey(shapeName))
        //            {
        //                int index = cleanBsIndexDic[shapeName];
        //                if (index != i)
        //                {
        //                    Debug.LogError("bs index != i");
        //                }
        //            }
        //            else
        //            {
        //                cleanBsIndexDic[shapeName] = i;
        //            }
        //        }


        //    }

        //    return cleanBsIndexDic;
        //}

        private bool findIndexFormBSName(string[] bsList, string shapeName)
        {
            string lowerBSName = shapeName.ToLower();
            foreach (var item in bsList)
            {
                if (lowerBSName.Contains(item.ToLower().Trim()))
                {
                    return true;
                }
            }


            return false;
        }



        //计算插值
        internal float InterpolateValue(float[] currentBlendShapesWeight)
        {
            if (!_isValid) return -1;

            //计算平均值
            float hValue = CuculateInterpolateInputValue(horizontalBSIndexs, currentBlendShapesWeight);
            float vValue = CuculateInterpolateInputValue(verticalBSIndexs, currentBlendShapesWeight);

            //计算点位
            //0..section - 1 
            int hIndex = CuculateInterpolatePointIndex(hValue, horizontalSection);
            int vIndex = CuculateInterpolatePointIndex(vValue, verticalSection);

            //计算周围四个点
            float minxminy = bsMeshWights[vIndex][hIndex];
            float maxxminy = bsMeshWights[vIndex][hIndex + 1];
            float minxmaxy = bsMeshWights[vIndex + 1][hIndex];
            float maxxmaxy = bsMeshWights[vIndex + 1][hIndex + 1];

            //Debug.LogFormat("hValue:{0},vValue:{1},hIndex:{2},vIndex:{3},minxminy:{4},maxxminy:{5},minxmaxy:{6},maxxmaxy:{7}", hValue, vValue, hIndex, vIndex, minxminy, maxxminy, minxmaxy, maxxmaxy);


            //进行插值
            float h_space = 1.0f / (horizontalSection - 1);
            float h_weight = hValue / h_space - hIndex;

            float v_space = 1.0f / (verticalSection - 1);
            float v_weight = vValue / v_space - vIndex;

            //Debug.LogFormat("h_space:{0},h_weight:{1},v_weight:{2}", h_space, h_weight, v_weight);


            float a1 = Mathf.Lerp(minxminy, maxxminy, h_weight);
            float a2 = Mathf.Lerp(minxmaxy, maxxmaxy, h_weight);
            float result = Mathf.Lerp(a1, a2, v_weight);
            //Debug.Log(result);
            return result;
        }

        private int CuculateInterpolatePointIndex(float vvvvv, int section)
        {
            if (vvvvv > 0)
            {
                float space = 1.0f / (section - 1);
                for (int i = 0; i < section; i++)
                {
                    float ssss  = (float)i * space;
                    if (vvvvv < ssss)
                    {
                        return i - 1;
                    }
                }

                return section - 1;
            }
            

            return 0;
        }

        private float CuculateInterpolateInputValue(int[] BSIndexs, float[] currentBlendShapesWeight)
        {
            float result = 0;
            foreach(int index in BSIndexs)
            {
                result += currentBlendShapesWeight[index];
            }

            result /= BSIndexs.Length;
            return result;
        }
    }
}
        
    