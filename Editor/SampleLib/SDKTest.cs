using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ComeSocialSDK.Editor; 
public class SDKTest : EditorWindow
{
    [MenuItem("Come Social/Test BuildButton")]
    public void BuildTest()
    {
        ComeSocialSDKControlPanel CP = new ComeSocialSDKControlPanel();
        GameObject selectedPrefab = Selection.activeGameObject;
        if (selectedPrefab != null)
        {
            string prefabPath = AssetDatabase.GetAssetPath(selectedPrefab);
            string prefabName = System.IO.Path.GetFileNameWithoutExtension(prefabPath);
            CP.BuildAllAssetBundlePrefabstest(selectedPrefab,"TestrPrefab",true);
        }
    }

    [MenuItem("Come Social/Test UploadButton")]
    public void UploadTest()
    {
        string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        Debug.Log("Selected Asset Path: " + assetPath);
    }

}
