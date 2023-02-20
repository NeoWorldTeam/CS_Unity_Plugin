using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System.IO;
using Object = UnityEngine.Object;

public class FaceTest : EditorWindow
{
    [MenuItem("Window/UI Toolkit/FaceTest")]
    // public PrefabUtility

    public static void ShowExample()
    {
        FaceTest wnd = GetWindow<FaceTest>();
        wnd.titleContent = new GUIContent("FaceTest");
    }
    private ObjectField uxmlField;
    private Button UploadButton;
    private VisualElement root;
    public string sourceScenePath = "Assets/GameAssets/Maps/SampleMap/Face_Sample_map_101.unity";
    public string targetScenePath = "Assets/GameAssets/Maps/RenderMap/";
    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        root = rootVisualElement;


        // Import UXML
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.neoworld.comesocial.unity/Editor/Uilib/FaceTest.uxml");
        VisualElement labelFromUXML = visualTree.Instantiate();
        root.Add(labelFromUXML);
        // Get a reference to the field from UXML and assign it its value.
        uxmlField = root.Q<ObjectField>("PrefabSelector");
        uxmlField.value = new Texture2D(10, 10) { name = "拖入面具预制件" };
        UploadButton = root.Q<Button>("UploadButton");
        UploadButton.SetEnabled(false);
        // Mirror value of uxml field into the C# field.
        uxmlField.RegisterCallback<ChangeEvent<Object>>((evt) =>
        {
            Debug.Log(uxmlField.value);
            bool T = TestType(uxmlField.value); //检测传入的是否是prefab
            TestFacePreFab(T);
        });
        UploadButton.RegisterCallback<MouseUpEvent>((evt) => Upload());
    }

    public bool TestType(Object PreObject )
    {
        return PrefabUtility.IsPartOfPrefabAsset(PreObject);
    }

    private void TestFacePreFab(bool T)
    {
        targetScenePath = "Assets/GameAssets/Maps/RenderMap/";

        if (T || uxmlField.value.name == "拖入面具预制件")
        {
            UploadButton.SetEnabled(true);
            CreateScene();
            
            OutRenderTexture();

        }
        else
        {
            EditorUtility.DisplayDialog("错误", "仅接受面具PreFab（预制件）上传！请检查选择对象", "OK"); //不是prefab则报错
            uxmlField.value = new Texture2D(10, 10) { name = "拖入面具预制件" };
            UploadButton.SetEnabled(false);
        }

    }

    private void DisplayIcon(Texture2D texture)
    {
        root.Q<VisualElement>("PreviewImage").style.backgroundImage=texture;
    }
    
    private void Upload()
    {
        OutRenderTexture();
        EditorUtility.DisplayDialog("成功", "面具以上传", "OK");
    }

    private Scene CreateScene()
    {
        targetScenePath = targetScenePath + uxmlField.value.name + "_Render.unity" ;
        Debug.Log(targetScenePath);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log(AssetDatabase.CopyAsset(sourceScenePath,targetScenePath ));
        AssetDatabase.SaveAssets();
        Scene Ns = EditorSceneManager.OpenScene(targetScenePath);
        EditorSceneManager.SaveOpenScenes();
        GameObject.Instantiate(uxmlField.value);

        return Ns;
    }

    private void OutRenderTexture()
    {
        try
        {


            Camera cam = Camera.main;
        
            var currentRT = RenderTexture.active;
            // RenderTexture rt = new RenderTexture(1024,1024,24);
            RenderTexture rt =  Resources.Load<RenderTexture>("Assets/GameAssets/Maps/RenderMap/RenderPng/OutRender.renderTexture");

            cam.targetTexture = rt;

            cam.Render();
            RenderTexture.active = rt;

            Texture2D tex = new Texture2D(1024, 1024);
            tex.ReadPixels(new Rect(0, 0,1024,1024), 0, 0);
            tex.Apply();
            DisplayIcon(tex);

            cam.targetTexture = null;
            RenderTexture.active = null; 
            RenderTexture.active = currentRT;
            byte[] bytes;
            bytes = tex.EncodeToPNG();
            File.WriteAllBytes("Assets/GameAssets/Maps/RenderMap/RenderPng/"  + uxmlField.value.name + ".png" , bytes);
            
        }
        catch (Exception)
        {
            OutRenderTexture();
        }
        
        
        // return new RenderTexture();
    }
}
