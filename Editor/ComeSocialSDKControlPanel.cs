using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ComeSocial.Face.Drive;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Object = UnityEngine.Object;
using StreamReader = ComeSocial.Face.Drive.StreamReader;
using UnityEditor.PackageManager;


namespace ComeSocialSDK.Editor
{
    public class ComeSocialSDKControlPanel : EditorWindow
    {
        public string AABFile, IABFile = "";
        public string AABName, IABName = "";
        public string sessionID;
        private TabMenuController controller;
        private VisualElement root;

        private readonly string URL = "http://192.168.50.169/";
        public List<string> nowFaceInfo = new List<string>();

        public DropdownField upload_model;
        public TextField upload_name;
        public ObjectField upload_prefab;
        public VisualElement FaceImg_List;
        public VisualElement Face_imgSample;

        [Obsolete("Obsolete")]

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            root = rootVisualElement;

            // VisualElements objects can contain other VisualElement following a tree hierarchy.
            // VisualElement label = new Label("Hello World! From C#");
            // root.Add(label);

            // Import UXML
            var visualTree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Packages/com.neoworld.comesocial.unity/Editor/Uilib/ComeSocialSDKControlPanel.uxml");
            VisualElement labelFromUXML = visualTree.Instantiate();
            labelFromUXML.style.height = new StyleLength(Length.Percent(100));
            labelFromUXML.style.minWidth = new StyleLength(450);
            root.Add(labelFromUXML);

            controller = new TabMenuController(root);

            controller.RegisterTabCallbacks();

            InitBuild();
            initUpload();
        }


        [MenuItem("Come Social/Control Panel")]
        public static void ShowExample()
        {
            var wnd = GetWindow<ComeSocialSDKControlPanel>();
            wnd.titleContent = new GUIContent("Come Social Control Panel");
            wnd.minSize = new Vector2(450, 450);
            wnd.maxSize = new Vector2(450, 1080);
        }

        private void DisplayIcon(Texture2D texture)
        {
            pre_img.style.backgroundImage = texture;
        }

        #region build变量

        private Button test_btn, build_btn, upload_btn, sign_up, sign_in, refresh_btn;
        private ProgressBar info_pro;
        private ObjectField file_obj;
        private Label info_text;
        private VisualElement pre_img;
        private Object pre_obj;
        private TextField face_name, user_name, user_password;

        public string sourceScenePath =
            "Editor/SampleLib/SampleMap/Face_Sample_map_101.unity";

        public string oldtargetScenePath = "Editor/SampleLib/RenderMap/";
        public string AssetsBundlesPath = "Editor/AssetsBundle";
        public List<GameObject> skinObjects = new();

        #endregion

        #region build页

        [Obsolete("Obsolete")]

        private void InitBuild()
        {
            #region 注册按键

            build_btn = root.Query<Button>("build-btn");
            test_btn = root.Query<Button>("test-btn");
            info_pro = root.Query<ProgressBar>("info-pro");
            file_obj = root.Query<ObjectField>("file-obj");
            info_text = root.Query<Label>("info-text");
            pre_img = root.Query<VisualElement>("pre-img");
            face_name = root.Query<TextField>("face-name");
            upload_btn = root.Query<Button>("upload-btn");

            #endregion

            ResetBuild();

            file_obj.RegisterCallback<ChangeEvent<Object>>(OnFileChange);

            build_btn.RegisterCallback<ClickEvent>(OnBuildBtnClick);
            test_btn.RegisterCallback<ClickEvent>(OnTestBtnClick);
            upload_btn.RegisterCallback<ClickEvent>(OnUploadBtnClick);
        }

        private void OnFileChange(ChangeEvent<Object> evt)
        {
            upload_btn.SetEnabled(false);
            var T = TestType(file_obj.value); //检测传入的是否是prefab
            TestFacePreFab(T);
            if (T)
            {
                info_text.text = "已选择面具";
                pre_obj = file_obj.value;
                test_btn.SetEnabled(true);
                info_pro.value = 25;
                info_pro.Query<Label>().First().text = " 1/4 >> 点击Test检查";
            }
        }

        private void TestFacePreFab(bool T)
        {
            if (!T && file_obj.value.name != "拖入面具预制件")
            {
                file_obj.value = new Texture2D(10, 10) { name = "拖入面具预制件" };
                Dialog("仅接受面具PreFab（预制件）上传！请检查选择对象");
            }
        }

        public bool TestType(Object PreObject)
        {
            return PrefabUtility.IsPartOfPrefabAsset(PreObject);
        }

        void OnRefreshBtnClick(ClickEvent evt)
        {
            GetMasks();

        }

        private void OnTestBtnClick(ClickEvent evt)
        {
            Debug.Log("Test button clicked!");

            //生成缩略图
            
            if (!CheckPrefab(file_obj.value as GameObject))
            {
                ResetBuild();
                return;
            }

            CreateScene();
            OutRenderTexture();


            info_text.text = "检查通过";
            info_pro.value = 50;
            build_btn.SetEnabled(true);
            info_pro.Query<Label>().First().text = " 2/4 >> 点击Build构建";
        }

        [Obsolete("Obsolete")]
        private void OnBuildBtnClick(ClickEvent evt)
        {
            info_pro.value = 75;
            info_pro.Query<Label>().First().text = "开始构建，请等待";
            BuildAllAssetBundlePrefabs(file_obj.value, face_name.text, true);
            upload_prefab.value = file_obj.value;
            upload_btn.SetEnabled(true);
            GetMasks();
            // Debug.Log("Build button clicked!");
        }


        private Scene CreateScene()
        {
            var targetScenePath = oldtargetScenePath + face_name.text + "_Render.unity";
            Debug.Log(targetScenePath);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log(AssetDatabase.CopyAsset(sourceScenePath, targetScenePath));
            AssetDatabase.SaveAssets();
            var Ns = EditorSceneManager.OpenScene(targetScenePath);
            EditorSceneManager.SaveOpenScenes();
            Instantiate(file_obj.value);

            return Ns;
        }

        public string preImgName, preImgPath = "";

        private void OutRenderTexture()
        {
            try
            {
                var cam = Camera.main;
                var currentRT = RenderTexture.active;
                var rt = new RenderTexture(1024, 1024, 24);
                cam.targetTexture = rt;
                RenderTexture.active = rt;

                // TextureFormat _texFormat;
                // _texFormat = TextureFormat.RGBAFloat;
                cam.Render();

                var tex = new Texture2D(1024, 1024);
                tex.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);
                tex.Apply();
                DisplayIcon(tex);

                cam.targetTexture = null;
                RenderTexture.active = null;
                RenderTexture.active = currentRT;
                byte[] bytes;
                bytes = tex.EncodeToPNG();
                preImgName = face_name.text + ".png";
                preImgPath = "Editor/SampleLib/RenderMap/RenderPng/" +
                             preImgName;
                File.WriteAllBytes(preImgPath,
                    bytes);
            }
            catch (Exception e)
            {
                Debug.Log(e);
                OutRenderTexture();
            }
            // return new RenderTexture();
        }

        public void Dialog(string message)
        {
            EditorUtility.DisplayDialog("错误", message, "OK"); //不是prefab则报错
        }

        public void ResetBuild()
        {
            info_text.text = "请选择面具";
            info_pro.Query<Label>().First().text = " 0/4 >> 拖入面具";
            file_obj.value = new Texture2D(10, 10) { name = "拖入面具预制件" };
            info_pro.value = 0;
            build_btn.SetEnabled(false);
            test_btn.SetEnabled(false);
            AABFile = "";
            IABFile = "";
            AABName = "";
            IABName = "";
        }


        #region 检查函数

        public void CheckSkinnedMeshRenderer(GameObject pre)
        {
            skinObjects.Clear();
            var prefab = pre;
            CheckSkinnedMeshRendererInPrefab(prefab);
            // Debug.Log("Finished checking SkinnedMeshRenderer in prefab and its children.");
        }

        private void CheckSkinnedMeshRendererInPrefab(GameObject parent)
        {
            foreach (var renderer in parent.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                skinObjects.Add(renderer.gameObject);
                // Debug.Log(renderer.gameObject);
            }
        }


        public bool CheckPrefab(GameObject pre)
        {
            // return true;

            CheckSkinnedMeshRenderer(pre);
            if (skinObjects.Count == 0)
            {
                ResetBuild();
                Dialog("prefab没有动画模型！请检查！");
                return false;
            }

            if (!CheckBlendShapes(skinObjects))
            {
                Dialog("缺少可驱动的blendshape，请检查!");
                return false;
            }

            // if (!CheckScript(skinObjects))//每个skinmeshrender都需要挂脚本时启用
            if (!CheckScript(file_obj.value as GameObject))
            {
                Dialog("缺少驱动脚本，请检查脚本配置！");
                return false;
            }

            if (!CheckOtherScript(file_obj.value as GameObject)) return false;

            return true;
        }

        public bool CheckScript(GameObject skin)
        {
            // return true;
            // foreach (GameObject skin in skinList)
            // {
            //if (skin.GetComponent<LocalStream>() == null)
            //    // skin.AddComponent<LocalStream>();
            //    return false;

            if (skin.GetComponent<CSFaceDescriptor>() == null)
                // skin.AddComponent<StreamReader>();
                return false;

            //if (skin.GetComponent<BlendShapesController>() == null)
            //    // skin.AddComponent<BlendShapesController>();
            //    return false;

            //if (skin.GetComponent<CharacterRigController>() == null)
            //    // skin.AddComponent<CharacterRigController>();
            //    return false;
            //// }
            return true;
        }

        public bool CheckBlendShapes(List<GameObject> skinList)
        {
            foreach (var skin in skinList)
            {
                var renderer = skin.GetComponent<SkinnedMeshRenderer>();

                if (renderer.sharedMesh.blendShapeCount > 5) return true;
            }

            return false;
        }

        public bool CheckOtherScript(GameObject s)
        {
            var skinList = new List<GameObject>();
            var CheckScripts = new List<string>
                { "LocalStream", "StreamReader", "BlendShapesController", "CharacterRigController" };
            foreach (var script in s.GetComponentsInChildren<MonoBehaviour>(true)) skinList.Add(script.gameObject);

            foreach (var skin in skinList)
            {
                var scripts = skin.GetComponents<MonoBehaviour>();
                foreach (var script in scripts)
                    if (!CheckScripts.Contains(script.GetType().Name))
                    {
                        Dialog($"{skin.name}存在非法脚本{script.GetType().Name},请检查并删除！");
                        return false;
                    }
            }

            return true;
        }


        [Obsolete("Obsolete")]
        public void BuildAllAssetBundlePrefabs(Object FacePrefab, string name, bool text)
        {
            if (!Directory.Exists(AssetsBundlesPath)) Directory.CreateDirectory(AssetsBundlesPath);

            var selection = new[] { FacePrefab };
            // 设置 AssetBundle 名称
            var Facename = name;
            var assetBundleName = $"{Facename}.assetbundle";

            var Aniordname = AssetsBundlesPath + "/A_" + assetBundleName;
            var iosname = AssetsBundlesPath + "/I_" + assetBundleName;

            // 导出 AssetBundle
            BuildPipeline.BuildAssetBundle(FacePrefab, selection, Aniordname,
                BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets,
                BuildTarget.Android);
            BuildPipeline.BuildAssetBundle(FacePrefab, selection, iosname,
                BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets, BuildTarget.iOS);
            AssetDatabase.Refresh();
            AABFile = Aniordname;
            IABFile = iosname;
            AABName = "A_" + assetBundleName;
            IABName = "I_" + assetBundleName;
            if (text)
            {
                info_pro.value = 100;
                info_pro.Query<Label>().First().text = "构建完成，请上传";
            }
        }

        [Obsolete("Obsolete")]

        public void BuildAllAssetBundlePrefabstest(Object FacePrefab, string name, bool text)
        {
            if (!Directory.Exists(AssetsBundlesPath)) Directory.CreateDirectory(AssetsBundlesPath);

            var selection = new[] { FacePrefab };
            // 设置 AssetBundle 名称
            var Facename = name;
            var assetBundleName = $"{Facename}.assetbundle";

            var Aniordname = AssetsBundlesPath + "/A_" + assetBundleName;
            var iosname = AssetsBundlesPath + "/I_" + assetBundleName;

            // 导出 AssetBundle
            BuildPipeline.BuildAssetBundle(FacePrefab, selection, Aniordname,
                BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets,
                BuildTarget.Android);
            BuildPipeline.BuildAssetBundle(FacePrefab, selection, iosname,
                BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets, BuildTarget.iOS);
            AssetDatabase.Refresh();
        }

        #endregion

        #endregion

        #region Account页

        private void initUpload()
        {
            sign_in = root.Query<Button>("sign-in");
            sign_up = root.Query<Button>("sign-up");
            user_name = root.Query<TextField>("user-name");
            user_password = root.Query<TextField>("user-password");
            sign_in.RegisterCallback<ClickEvent>(OnSigninBtnClick);
            upload_model = root.Query<DropdownField>("upload-model");
            upload_name = root.Query<TextField>("upload-name");
            upload_prefab = root.Query<ObjectField>("upload-prefab");
            FaceImg_List = root.Query<VisualElement>("FaceImg-List");
            upload_model.RegisterValueChangedCallback(OnUploadModelChange);
            Face_imgSample = FaceImg_List.Query<VisualElement>("FaceImg");
            //FaceImg_List.Remove(Face_imgSample);
            upload_prefab.SetEnabled(false);
            refresh_btn = root.Query<Button>("refresh-btn");
            upload_name.SetEnabled(false);
            // refresh_btn.SetEnabled(false);
            upload_btn.SetEnabled(false);
            refresh_btn.RegisterCallback<ClickEvent>(OnRefreshBtnClick);

        }

        private void OnUploadModelChange(ChangeEvent<string> evt)
        {
            // Debug.Log(upload_model.index);
            if (upload_model.index == 0)
            {
                // CreatMask();
                // Debug.Log("创建");
                FaceImg_List.SetEnabled(false);
                FaceImg_List.style.opacity = 0;
                upload_name.SetEnabled(true);

            }
            else
            {
                // Debug.Log("上传");
                FaceImg_List.SetEnabled(true);
                FaceImg_List.style.opacity = 100;
                upload_name.SetEnabled(false);

                //查询面具
                GetMasks();

            }
        }

        private void OnSigninBtnClick(ClickEvent evt)
        {
            var request = SDKLogin(user_name.text);
            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                // Debug.Log(request.error);
            }
            else
            {
                Debug.Log(request.downloadHandler.text);
                var headers = request.GetResponseHeaders();

                // if (headers.ContainsKey("Set-Cookie")) 
                Debug.Log("Cookie: " + headers["Set-Cookie"]);
                var cookie = headers["Set-Cookie"];
                var pattern = "_session_id=([^;]+);";
                var match = Regex.Match(cookie, pattern);
                if (match.Success)
                {
                    sessionID = match.Groups[1].Value;
                    Debug.Log(sessionID);
                    GetMasks();
                }
                else
                    Dialog("未获取到登录信息，请重试");
            }
        }

        public UnityWebRequest SDKLogin(string name)
        {
            // Debug.Log(URL+"api/v1/login");
            var request = new UnityWebRequest(URL + "api/v1/login", "POST");
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Accept", "*/*");
            // request.SetRequestHeader("Host", URL);
            request.SetRequestHeader("Connection", "keep-alive");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes($"name={name}"));
            request.uploadHandler.contentType = "application/x-www-form-urlencoded";
            AsyncOperation asyncOp = request.SendWebRequest();
            while (!asyncOp.isDone)
                continue;

            // Debug.Log("登录");
            // wait for request to complete
            return request;
        }

        public void GetMasks()
        {
            string url = URL + "api/v0/masks";

            UnityWebRequest webRequest = UnityWebRequest.Get(url);
            webRequest.SetRequestHeader("Cookie", $"_session_id={sessionID}");
            webRequest.SetRequestHeader("User-Agent", "Apifox/1.0.0 (https://www.apifox.cn)");
            webRequest.SetRequestHeader("Accept", "*/*");
            // webRequest.SetRequestHeader("Host", URL);
            webRequest.SetRequestHeader("Connection", "keep-alive");

            AsyncOperation asyncOperation = webRequest.SendWebRequest();
            while (!asyncOperation.isDone) continue;
            string json = webRequest.downloadHandler.text;
            // MaskData data = JsonUtility.FromJson<MaskData>(json);
            Debug.Log(json);
            Root jsroot = JsonUtility.FromJson<Root>(json);
            if (FaceImg_List.childCount != 0)
            {
                List<VisualElement> va = FaceImg_List.Children().ToList();
                foreach (var fi in va)
                {
                    try
                    {
                        // Debug.Log(fi);
                        FaceImg_List.Remove(fi);
                    }
                    catch (Exception)
                    {

                    }
                }

            }

            // Debug.Log("id: " +jsroot.data[0].id);
            foreach (Face_Data fd in jsroot.data)
            {
                int vID = 1;
                if (fd.edges != null)
                {
                    vID = fd.edges.bundle[fd.edges.bundle.Length-1].verionID+1;
                }

                string tool = $"{fd.name},{fd.id},{vID}";
                CretaeMaskUI(fd.thumbnail_url, fd.name, tool);

            }


            // Debug.Log(data.value);
        }

        private void CretaeMaskUI(string imgUrl, string Fname, string tool)
        {
            Texture2D raw = new Texture2D(100, 100);
            UnityWebRequest m_Request = UnityWebRequestTexture.GetTexture(imgUrl);
            AsyncOperation asyncOperation = m_Request.SendWebRequest();
            asyncOperation.completed += (AsyncOperation obj) =>
            {
                if (m_Request.result == UnityWebRequest.Result.ConnectionError)
                {
                    Debug.Log("加载失败");
                }
                else
                {
                    // Debug.Log(asyncOperation.isDone);
                    // Debug.Log(asyncOperation.progress);
                    raw = DownloadHandlerTexture.GetContent(m_Request);

                    VisualElement NewFace = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.neoworld.comesocial.unity/Editor/SampleLib/FaceImg.uxml").Instantiate();
                    NewFace.Query<Button>().First().style.backgroundImage = raw;
                    NewFace.Query<Button>().First().tooltip = tool;
                    NewFace.Query<Label>().First().text = Fname;
                    NewFace.RegisterCallback<ClickEvent>(OnImgClick);
                    FaceImg_List.Add(NewFace);
                    // byte[] bytes = raw.EncodeToPNG();
                    // File.WriteAllBytes(Application.dataPath + "/SavedTexture.png", bytes);

                }
            };



        }

        public void CreatMask(string uname)
        {

            WWWForm form = new WWWForm();
            form.AddField("name", uname);
            UnityWebRequest www = UnityWebRequest.Post(URL + "api/v0/mask", form);
            www.SetRequestHeader("Accept", "*/*");
            www.SetRequestHeader("Host", "192.168.50.169");
            www.SetRequestHeader("Connection", "keep-alive");
            www.SetRequestHeader("Cookie", $"_session_id={sessionID}");
            www.SendWebRequest();
            
            while (!www.isDone)
            {
                // Wait for request to complete
            }

            if (www.result == UnityWebRequest.Result.ProtocolError)
            {
                Console.WriteLine(www.error);
            }
            else
            {
                Console.WriteLine(www.downloadHandler.text);
                // Debug.Log(upload_name.text);
            }

            string maskid = www.downloadHandler.text;
            string pattern = @"\""id\""\s*:\s*(\d+),";
            Match match = Regex.Match(maskid, pattern);
            string idStr = match.Groups[1].Value;
            nowFaceInfo = new List<string>();
            nowFaceInfo.Add(uname);
            nowFaceInfo.Add(idStr);
            nowFaceInfo.Add("1");
            Debug.Log(nowFaceInfo.ToString());
        }
    





        
        public bool CreateUpdate = true;
        private void OnUploadBtnClick(ClickEvent evt)
        {
            // Debug.Log(nowFaceInfo[0]+nowFaceInfo[1]+nowFaceInfo[2]);
            if (upload_model.index ==0)
            {
                CreatMask(upload_name.text);
                
            }
            UploadAB(AABFile, AABName,true,"Android");
            UploadAB(IABFile, IABName,true,"iPhone"); 
            //缩略图
            UploadAB(preImgPath, preImgName,false,"iPhone");
            GetMasks();
        }

        public void UploadAB(string path, string name,bool file,string phone)
        {
            var form = new WWWForm();
            if (file)
            {
                form.AddBinaryData("upload[]", File.ReadAllBytes(path), name, "multipart/form-data");
                form.AddField("mask_id", nowFaceInfo[1]);
                form.AddField("type", "mask");
                form.AddField("version_id", nowFaceInfo[2]);
                form.AddField("platform", phone);
            }
            else
            {
                form.AddBinaryData("upload[]", File.ReadAllBytes(path), name, "multipart/form-data");
                form.AddField("mask_id", nowFaceInfo[1]);
                form.AddField("type", "mask_thumbnail");
                form.AddField("version_id", nowFaceInfo[2]);
                form.AddField("platform", phone);
            }
            using (var www = UnityWebRequest.Post(URL+"upload", form))
            {
                www.SetRequestHeader("Cookie", $"_session_id={sessionID}");

                AsyncOperation asyncOp = www.SendWebRequest();
                while (!asyncOp.isDone) continue;
                // wait for request to complete
                if (www.result == UnityWebRequest.Result.ProtocolError)
                    Debug.Log(www.result == UnityWebRequest.Result.ConnectionError);
                else
                    Debug.Log("Upload complete!");
            }

            // Debug.Log(name);
        }

        #endregion

        #region Upload

        private void OnImgClick(ClickEvent evt)
        {
            //string clickFaceName = evt
            Button clickedButton = evt.target as Button;
            Debug.Log(clickedButton.tooltip);
            nowFaceInfo = clickedButton.tooltip.Split(",").ToList();
            CreateUpdate = false;
            ControlCreateSwitch(CreateUpdate);
            upload_name.value = nowFaceInfo[0];
        }

        private void ControlCreateSwitch(bool S)
        {
            if (S)
            {
                upload_model.index = 0;
            }
            else
            {
                upload_model.index = 1;

            }
        }

        #endregion
    }
}