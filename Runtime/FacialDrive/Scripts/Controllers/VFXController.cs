using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace ComeSocial.Face.Drive
{
    /// <inheritdoc cref="IUsesStreamReader" />
    /// <summary>
    /// 获取一些变量以驱动 vfx
    /// </summary>
    public class VFXController : MonoBehaviour, IUsesStreamReader
    {


        [SerializeField]
        [Tooltip("获取BS的来源")]
        BlendShapesController blendShapeController;


        [SerializeField]
        [Tooltip("可选，指定参考的MeshRender")]
        SkinnedMeshRenderer selectMeshRender;


        [SerializeField]
        [Tooltip("触发规则")]
        BlendShapeTriggerRule[] rules = new BlendShapeTriggerRule[] {};


        //记录满足触发条件的持续时间
        float[] triggerStateIndex = new float[] { };
        //当前VFX冷却时间
        float currentCoolDownTime = 0;

        bool isInitData = false;


        void Start()
        {
            isInitData = false;
        }

        void Update()
        {
            if(isInitData == false)
            {

                CheckTriggerRule();
                isInitData = true;
            }



            //当前是否在冷却
            if (currentCoolDownTime > 0)
            {
                currentCoolDownTime -= Time.deltaTime;
                return;
            }

            //规则检查
            if(blendShapeController != null)
            {
                var m_Indices = blendShapeController.blendShapeIndices;
                if (m_Indices.Count == 0) return;
                BlendShapeIndexData[] indices = m_Indices[selectMeshRender];
                float[] blendShapes = blendShapeController.CalculateBlendShapes;

                //检索生效的
                for (var i = 0; i < rules.Length; i++)
                {
                    var rule = rules[i];
                    var points = rule.TriggerPoints;
                    bool ruleTrigger = false;
                    foreach (var p in points)
                    {
                        BlendShapeIndexData datum = indices[p.locationIndex];
                        if (datum.index < 0)
                        {
                            Debug.LogError("VFX 触发了不存在的BlendShape");
                            continue;
                        }

                        var weight = blendShapes[datum.index];
                        //Debug.LogFormat("indicesBS：{0},weight:{1}", datum.name, weight);
                        ruleTrigger = ruleTrigger || (weight >= p.weight);


                        //一个没有满足就是不满足
                        if (!ruleTrigger) break;
                    }


                    //检查是否触发
                    if (ruleTrigger)
                    {
                        //Debug.Log("触发");
                        //更新持续时间                        
                        triggerStateIndex[i] += Time.deltaTime;
                        if(triggerStateIndex[i] > rule.stateContinuousTime)
                        {
                            //激活
                            ActiveRule(rule);
                            return;
                        }
                    }
                    else
                    {
                        //Debug.Log("不触发");
                        triggerStateIndex[i] = 0;
                    }
                }

                
                
            }
        }



        private void ActiveRule(BlendShapeTriggerRule rule)
        {
            //Debug.Log("ActiveRule");
            VisualEffect[] vfxs = rule.vfxs;
            foreach(var vfx in vfxs)
            {
                vfx.Play();
            }

            ResetState(rule);
        }

        private void ResetState(BlendShapeTriggerRule rule)
        {
            currentCoolDownTime = rule.coolDownTime;
            for(var i = 0; i < triggerStateIndex.Length; i++)
            {
                triggerStateIndex[i] = 0;
            }
        }



        

        private void CheckTriggerRule()
        {
            //检查更新
            foreach (BlendShapeTriggerRule rule in rules)
            {
                rule.CheckTrigger();
            }

            if (triggerStateIndex.Length != rules.Length)
                Array.Resize(ref triggerStateIndex, rules.Length);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (blendShapeController == null)
            {
                blendShapeController = gameObject.GetComponent<BlendShapesController>();
            }

            if (selectMeshRender == null && blendShapeController != null && blendShapeController.skinnedMeshRenderers.Length > 0)
            {
                selectMeshRender = blendShapeController.skinnedMeshRenderers[0];
            }


            Debug.Log("VFXController OnValidate");
            

            UpdateRule();
            CheckTriggerRule();
        }

        private void UpdateRule()
        {
            if (streamReader == null)
                return;

            var streamSource = streamReader.streamSource;
            if (streamSource == null)
                return;

            var streamSettings = streamSource.streamSettings;
            if (streamSettings == null || streamSettings.locations == null || streamSettings.locations.Length == 0)
                return;

            var m_Indices = blendShapeController.blendShapeIndices;
            if (m_Indices.Count == 0) blendShapeController.UpdateBlendShapeIndices(streamSettings);
            m_Indices = blendShapeController.blendShapeIndices;
            if (m_Indices.Count > 0)
            {
                BlendShapeIndexData[] indices = m_Indices[selectMeshRender];
                foreach (BlendShapeTriggerRule rule in rules)
                {
                    rule.InitBlendShapeConfig(indices);
                }
            }
        }
#endif



        //implement IUsesStreamReader
        public IStreamReader streamReader { private get; set; }
    }
}
