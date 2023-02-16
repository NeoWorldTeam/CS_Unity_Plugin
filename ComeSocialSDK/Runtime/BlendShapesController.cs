using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ComeSocial.Face.Drive
{
    /// <inheritdoc cref="IUsesStreamReader" />
    /// <summary>
    /// 将Stream Reader中的BS更新到skinned mesh renders。
    /// </summary>
    public class BlendShapesController : MonoBehaviour, IUsesStreamReader
    {
        [SerializeField]
        [Tooltip("Skinned Mesh Renders，包含将由该控制器驱动的混合形状。")]
        SkinnedMeshRenderer[] m_SkinnedMeshRenderers = new SkinnedMeshRenderer[] { new SkinnedMeshRenderer() };

        [Range(0f, 1)]
        [SerializeField]
        [Tooltip("对BS进行平滑处理。")]
        float m_BlendShapeSmoothing = 0.1f;

        [Range(0, 0.1f)]
        [SerializeField]
        [Tooltip("认定为新BS权重的最小变化阈值。")]
        float m_BlendShapeThreshold = 0.01f;

        [Range(0, 200f)]
        [SerializeField]
        [Tooltip("应用于BS权重的缩放系数。")]
        float m_BlendShapeCoefficient = 100f;

        [Range(0, 100)]
        [SerializeField]
        [Tooltip("BS权重可以达到的最大值")]
        float m_BlendShapeMax = 100f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("平滑参数，以便在跟踪丢失时将角色的BS恢复到初始姿势。")]
        float m_TrackingLossSmoothing = 0.1f;

        [SerializeField]
        [Tooltip("覆盖单个BS的设置")]
        BlendShapeOverride[] m_Overrides;


        [SerializeField]
        [Tooltip("是否计算初始化差值")]
        bool m_ExecuteInitOffset = true;

        [SerializeField]
        [Tooltip("初始化BS的差值")]
        BlendShapeInitRecognition[] m_InitRecognitionOffsets;

        [SerializeField]
        [Tooltip("BS插值点")]
        BlendShapeInterpolatePoints[] m_BSInterpolatePoints;

        [SerializeField]
        [Tooltip("BS网格插值点")]
        BlendShapeMeshInterpolateRule[] m_BSMeshInterpolateRules;

        [SerializeField]
        [Tooltip("BS相互抑制规则")]
        BlendShapeRestrainRules[] m_RestrainBSRules;

        //上一个 bs mapping
        IStreamSettings m_LastStreamSettings;

        //被变形的mesh数组
        public SkinnedMeshRenderer[] skinnedMeshRenderers {
            get { return m_SkinnedMeshRenderers; }
            set { m_SkinnedMeshRenderers = value; }
        }


        //mesh render的bs结构体
        public ref Dictionary<SkinnedMeshRenderer, BlendShapeIndexData[]> blendShapeIndices { get { return ref m_Indices; } }
        Dictionary<SkinnedMeshRenderer, BlendShapeIndexData[]> m_Indices = new Dictionary<SkinnedMeshRenderer, BlendShapeIndexData[]>();

        //是否初始化权重差值
        bool isInitOffset;
        //初始化权重参考数
        int numOfInitBSInputCount;
        //临时计算变量
        float[] tempCountInitOffset;
        

        //更新时计算bs值的数组
        float[] m_BlendShapes;
        //更新时计算bs缩放的数组
        float[] blendShapesScaled;
        //最终BS
        public ref float[] CalculateBlendShapes { get { return ref blendShapesScaled; } }
        //
        float[] currentBlendShapesWeight = null;




        











        void Start()
        {
            Debug.Log("BlendShapesController Start");
            isInitOffset = false;
            numOfInitBSInputCount = 101;
            tempCountInitOffset = null;

            var streamSource = streamReader.streamSource;
            if (streamSource == null)
            {
                Debug.LogError("Disabling BlendShapesController. No stream source set.", this);
                enabled = false;
                return;
            }

            var streamSettings = streamSource.streamSettings;
            if (streamSettings == null)
            {
                Debug.LogError("Disabling BlendShapesController. No stream settings", this);
                enabled = false;
                return;
            }
            var blendShapesCount = streamSettings.locations.Length;


            
            if (m_Overrides != null && m_Overrides.Length != blendShapesCount)
                Array.Resize(ref m_Overrides, blendShapesCount);

            if (m_InitRecognitionOffsets != null && m_InitRecognitionOffsets.Length != blendShapesCount)
                Array.Resize(ref m_InitRecognitionOffsets, blendShapesCount);

            if (m_BSInterpolatePoints != null && m_BSInterpolatePoints.Length != blendShapesCount)
                Array.Resize(ref m_BSInterpolatePoints, blendShapesCount);
            


            var filteredList = new List<SkinnedMeshRenderer>();
            foreach (var renderer in m_SkinnedMeshRenderers)
            {
                if (renderer == null)
                {
                    Debug.LogWarning("Null element in SkinnedMeshRenderer list in " + this);
                    continue;
                }

                if (renderer.sharedMesh == null)
                {
                    Debug.LogWarning("Missing mesh in " + renderer);
                    continue;
                }

                filteredList.Add(renderer);
            }

            m_SkinnedMeshRenderers = filteredList.ToArray();

            if (m_SkinnedMeshRenderers.Length < 1)
            {
                Debug.LogWarning("Blend Shape Controller has no valid Skinned Mesh Renderers.");
                enabled = false;
            }
        }

        void Update()
        {
            var streamSource = streamReader.streamSource;
            if (streamSource == null || !streamSource.active)
                return;

            var streamSettings = streamSource.streamSettings;
            if (streamSettings != m_LastStreamSettings)
                UpdateBlendShapeIndices(streamSettings);

            CheckInitOffset();
            InterpolateBlendShapes();

            foreach (var meshRenderer in m_SkinnedMeshRenderers)
            {
                BlendShapeIndexData[] indices = m_Indices[meshRenderer];
                var length = indices.Length;
                for (var i = 0; i < length; i++)
                {
                    BlendShapeIndexData datum = indices[i];
                    if (datum.index < 0)
                        continue;

                    var weight = blendShapesScaled[datum.index];
                    meshRenderer.SetBlendShapeWeight(i, weight);
                }
            }
        }



        //根据mapping 更新bs匹配位置
        public void UpdateBlendShapeIndices(IStreamSettings settings)
        { 
            m_LastStreamSettings = settings;
            
            string[] locations = settings.locations;
            var blendShapeCount = locations.Length; 
            m_BlendShapes = new float[blendShapeCount];
            blendShapesScaled = new float[blendShapeCount];
            m_Indices.Clear();

            //从mesh render中读取bs数量
            foreach (var meshRenderer in m_SkinnedMeshRenderers)
            {
                var mesh = meshRenderer.sharedMesh;
                var count = mesh.blendShapeCount;
                var indices = new BlendShapeIndexData[count];
                for (var i = 0; i < count; i++)
                {
                    string shapeName = mesh.GetBlendShapeName(i);
                    var index = -1;
                    foreach (Mapping mapping in m_LastStreamSettings.mappings)
                    {
                        string to = mapping.to;
                        string from = mapping.from;
                        if (shapeName.Contains(from))
                            index = Array.IndexOf(locations, from);

                        if (shapeName.Contains(to))
                            index = Array.IndexOf(locations, from);
                    }   

                    if (index < 0)
                    {
                        for (var j = 0; j < m_LastStreamSettings.locations.Length; j++)
                        {
                            if (shapeName.Contains(m_LastStreamSettings.locations[j]))
                            {
                                index = j;
                                break;
                            }
                        }
                    }



                    indices[i] = new BlendShapeIndexData(index, shapeName);

#if UNITY_EDITOR
                    if (index < 0)
                        Debug.LogWarningFormat("Blend shape {0} is not a valid AR blend shape", shapeName);
#endif
                }

                m_Indices.Add(meshRenderer, indices);
            }




        }


        /**
         *  检查初始化的BS offset
         */
        public void CheckInitOffset()
        {
            if (!m_ExecuteInitOffset) return;
            if (isInitOffset) return;
            if (!streamReader.trackingActive) return;
            if (m_InitRecognitionOffsets == null) return;

            numOfInitBSInputCount = numOfInitBSInputCount - 1;
            if (numOfInitBSInputCount % 10 == 0) Debug.Log("计算无表情BS权重");

            var blendShapeCount = streamReader.streamSource.streamSettings.locations.Length;
            if (tempCountInitOffset == null) tempCountInitOffset = new float[blendShapeCount];
            for (var i = 0; i < blendShapeCount; i++)
            {
                //识别的BS权重
                var blendShapeTarget = streamReader.blendShapesBuffer[i];
                tempCountInitOffset[i] = tempCountInitOffset[i] + blendShapeTarget;
            }

            if (numOfInitBSInputCount == 1)
            {
                isInitOffset = true;
                for (var i = 0; i < blendShapeCount; i++)
                {
                    var blendShapeInit = m_InitRecognitionOffsets[i];
                    blendShapeInit.blendShapeInitOffset = tempCountInitOffset[i] / 100;
                }
                
                Debug.Log("计算无表情BS权重 完成 ！！！！！！！！！");
            }



        }

        bool UseOverride(int index)
        {
            return m_Overrides != null && index < m_Overrides.Length
                && m_Overrides[index] != null && m_Overrides[index].useOverride;
        }

        /**
         *  计算应用的的BS 
         *  步骤
         *  1.是否更新的阈值
         *  2.缩放系数
         *  3.最大值限制
         */
        public void InterpolateBlendShapes(bool force = false)
        {
            var streamSettings = streamReader.streamSource.streamSettings;
            var blendShapeCount = streamSettings.locations.Length;
            var isTrackingActive = streamReader.trackingActive;


            //1.从arkit中读取bs数量，更新权重
            if (currentBlendShapesWeight == null || currentBlendShapesWeight.Length != streamReader.blendShapesBuffer.Length)
            {
                currentBlendShapesWeight = new float[streamReader.blendShapesBuffer.Length];
            }
            Array.Copy(streamReader.blendShapesBuffer, currentBlendShapesWeight, streamReader.blendShapesBuffer.Length);
          

            for (var i = 0; i < blendShapeCount; i++)
            {

                var blendShapeTarget = currentBlendShapesWeight[i];
                //2. 应用初始化 offset
                if (isInitOffset)
                {
                    var blendShapeInit = m_InitRecognitionOffsets[i];
                    blendShapeTarget = MathF.Max(0, MathF.Abs(blendShapeTarget - blendShapeInit.blendShapeInitOffset));
                }

                

                //3. 应用插值点
                if (m_BSInterpolatePoints != null)
                {
                    BlendShapeInterpolatePoint[] points = m_BSInterpolatePoints[i].interpolatePoints;
                    if (blendShapeTarget > 0 && points.Length > 0)
                    {
                        blendShapeTarget = CalculateInterpolateValue(blendShapeTarget, points);
                    }
                }
                currentBlendShapesWeight[i] = blendShapeTarget;

            }
            

            //4.网格插值
            if (m_BSMeshInterpolateRules != null)
            {
                
                foreach (BlendShapeMeshInterpolateRule BlendShapeMeshInterpolateRule in m_BSMeshInterpolateRules)
                {

                    if (BlendShapeMeshInterpolateRule.IsValid())
                    {

                        if (BlendShapeMeshInterpolateRule.NeedUpdate())
                        {
                            BlendShapeMeshInterpolateRule.HandleBlendShapeRelative(streamSettings.locations);
                        }

                        float weight = BlendShapeMeshInterpolateRule.InterpolateValue(currentBlendShapesWeight);
                        int index = BlendShapeMeshInterpolateRule.TargetBSIndex();
                        currentBlendShapesWeight[index] = weight;
                    }
                }
            }
            

            //5.权重抑制
            if(m_RestrainBSRules != null)
            {
                foreach (BlendShapeRestrainRules blendShapeRestrainRule in m_RestrainBSRules)
                {

                    if (blendShapeRestrainRule.IsValid())
                    {

                        if (blendShapeRestrainRule.NeedUpdate())
                        {
                            blendShapeRestrainRule.HandleBlendShapeRelative(streamSettings.locations);
                        }

                        if (blendShapeRestrainRule.IsSourceActive(currentBlendShapesWeight))
                        {
                            int index = blendShapeRestrainRule.TargetBlendShapeRestrain().BSIndex;
                            currentBlendShapesWeight[index] = MathF.Min(currentBlendShapesWeight[index], blendShapeRestrainRule.TargetBlendShapeRestrain().Weight);
                        }
                    }
                }
            }
            


            for (var i = 0; i < blendShapeCount; i++)
            {
                var blendShapeTarget = currentBlendShapesWeight[i];
                //之前计算得到的BS权重
                var blendShape = m_BlendShapes[i];
                //是否覆盖结果
                var useOverride = UseOverride(i);
                var blendShapeOverride = m_Overrides != null? m_Overrides[i] : null;
                var threshold = useOverride ? blendShapeOverride.blendShapeThreshold : m_BlendShapeThreshold;
                var offset = useOverride ? blendShapeOverride.blendShapeOffset : 0f;
                var smoothing = useOverride ? blendShapeOverride.blendShapeSmoothing : m_BlendShapeSmoothing;

                //1.是否更新的阈值
                if (force || isTrackingActive)
                {
                    if (Mathf.Abs(blendShapeTarget - blendShape) > threshold)
                        m_BlendShapes[i] = Mathf.Lerp(blendShapeTarget, blendShape, smoothing);
                }
                else
                {
                    m_BlendShapes[i] = Mathf.Lerp(0f, blendShape, m_TrackingLossSmoothing);
                }

                // 2.缩放系数 3.最大值限制
                if (useOverride)
                {
                    blendShapesScaled[i] = Mathf.Min(blendShape * blendShapeOverride.blendShapeCoefficient + offset,
                        blendShapeOverride.blendShapeMax);
                }
                else
                {
                    blendShapesScaled[i] = Mathf.Min(blendShape * m_BlendShapeCoefficient, m_BlendShapeMax);
                }
            }

            


        }

        private float CalculateInterpolateValue(float blendShapeTarget, BlendShapeInterpolatePoint[] points)
        {
            BlendShapeInterpolatePoint lastPoint = null;
            var pointSize = points.Length;

            for (var j = 0; j < points.Length; j++)
            {
                var currentPoint = points[j];
                //在插值点范围内
                if (blendShapeTarget <= currentPoint.x)
                {
                    float startX = 0;
                    float startY = 0;
                    if(lastPoint != null)
                    {
                        startX = lastPoint.x;
                        startY = lastPoint.y;
                    }
                    float interpolationValue = (blendShapeTarget - startX) / (currentPoint.x - startX);
                    return Mathf.Lerp(startY, currentPoint.y, interpolationValue);
                }
                lastPoint = currentPoint;
            }

            //遍历结束还没有找到
            {
                float startX = points[pointSize - 1].x;
                float startY = points[pointSize - 1].y;
                float interpolationValue = (blendShapeTarget - startX) / (1 - startX);
                return Mathf.Lerp(startY, 1, interpolationValue);
            }
            
        }


        void OnValidate()
        {
            Debug.Log("BlendShapesController OnValidate");
            if (streamReader == null)
                return;

            var streamSource = streamReader.streamSource;
            if (streamSource == null)
                return;

            var streamSettings = streamSource.streamSettings;
             if (streamSettings == null || streamSettings.locations == null || streamSettings.locations.Length == 0)
                return;

            if (streamSettings != m_LastStreamSettings)
                UpdateBlendShapeIndices(streamSettings);






            // We do our best to keep the overrides up-to-date with current settings, but it's possible to get out of sync
            string[] locations = streamSettings.locations;
            var blendshapeCount = locations.Length;


            //更新网格 Bs 序号
            if(m_BSMeshInterpolateRules != null)
            {
                foreach (var meshInterpolateRule in m_BSMeshInterpolateRules)
                {
                    meshInterpolateRule.HandleBlendShapeRelative(locations);
                }
            }
            

            //更新抑制 BS 序号
            if(m_RestrainBSRules != null)
            {
                foreach (var restrainBSRules in m_RestrainBSRules)
                {
                    restrainBSRules.HandleBlendShapeRelative(locations);
                }
            }
            


            if (m_Overrides != null && m_Overrides.Length != blendshapeCount)
            {
#if UNITY_EDITOR
                var overridesCopy = new BlendShapeOverride[blendshapeCount];

                for (var i = 0; i < blendshapeCount; i++)
                {
                    var location = locations[i];
                    var blendShapeOverride = m_Overrides.FirstOrDefault(f => f.name == location)
                        ?? new BlendShapeOverride(location);

                    overridesCopy[i] = blendShapeOverride;
                }

                m_Overrides = overridesCopy;
#endif
            }






            if (m_InitRecognitionOffsets != null && m_InitRecognitionOffsets.Length != blendshapeCount)
            {
#if UNITY_EDITOR
                var overridesCopy = new BlendShapeInitRecognition[blendshapeCount];

                for (var i = 0; i < blendshapeCount; i++)
                {
                    var location = locations[i];
                    var blendShapeOverride = m_InitRecognitionOffsets.FirstOrDefault(f => f.name == location)
                        ?? new BlendShapeInitRecognition(location);

                    overridesCopy[i] = blendShapeOverride;
                }

                m_InitRecognitionOffsets = overridesCopy;
#endif
            }






            if (m_BSInterpolatePoints != null && m_BSInterpolatePoints.Length != blendshapeCount)
            {
#if UNITY_EDITOR
                var overridesCopy = new BlendShapeInterpolatePoints[blendshapeCount];

                for (var i = 0; i < blendshapeCount; i++)
                {
                    var location = locations[i];
                    var blendShapeOverride = m_BSInterpolatePoints.FirstOrDefault(f => f.name == location)
                        ?? new BlendShapeInterpolatePoints(location); 

                    overridesCopy[i] = blendShapeOverride; 
                }

                m_BSInterpolatePoints = overridesCopy;
#endif
            }

            
        }



        //implement IUsesStreamReader
        public IStreamReader streamReader { private get; set; }
    }
}
