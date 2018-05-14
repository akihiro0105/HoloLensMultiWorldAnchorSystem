using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MultiWorldAnchorSystem
{
    // Vuforiaマルチマーカー+マルチWorldAnchor位置設定
    // 初期配置
    // - Vuforiaでサブセンターを設置(1~)
    // - サブセンター前方にフロントアンカーを設置
    // - サブセンターの一定範囲にランダムにWorldAnchorを設置(10)
    // - サブセンターとフロントアンカーそれぞれのWorldAnchorとの相対位置を算出，保存
    // - 指定のサブセンターと同じ位置ににルートを指定
    // - それぞれのサブセンターはルートと同じ位置に自前のルートアンカーを設置
    // - サブセンターとルートアンカーの相対位置を保存

    // 復帰処理
    // - 復帰or起動時にWorldAnchorを復帰させる
    // - WorldAnchor復帰後それぞれのサブセンターごとにサブセンター復帰処理を行う
    // - サブセンターに属しているWorldAnchorからランダムに3点を選択しそれぞれの相対距離を求める
    // - 保存データから選択された番号と同じWorldAnchorとサブセンターとの相対位置を取得，相対距離を算出
    // - 保存データから算出した相対距離と実際に測定した相対距離を比較し許容範囲であればWorldAnchorは正常に設置されたと判断する
    // - 正常に設置された3点のWorldAnchorの相対位置からサブセンターとフロントアンカーの位置を算出する
    // - 算出，設置後サブセンターの正面がフロントアンカーになるように回転させる
    // - サブセンター設置後保存されたルートアンカーとの相対位置からルートアンカーを設置

    // 動作処理
    // - プレイヤーから一番近いサブセンターを取得し，サブセンターに紐づいているルートアンカーを空間の基準座標としてオブジェクトを配置する
    // - 一定間隔で復帰処理のサブセンター再設置を行い，ルートアンカーのずれを補正する

    public static class StaticParameter
    {
        public const float anchorDistance = 3.0f;// WorldAnchorの設置直径範囲
        public const int worldAnchorCount = 10;// WorldAnchor設置数
        public const float maxDistance = 0.001f;// WorldAnchor復帰計算時の誤差許容範囲
        public const int maxReturnCount = 50;// WorldAnchor復帰再計算の限度回数
        public const float minAnchorLength = 0.9f;// 最小復帰処理可能なWorldAnchor設置率
    }
    public class ModelPositionManager : MonoBehaviour
    {
        #region CenterAnchor
        public class CenterAnchor
        {
            public GameObject center { get; private set; }
            private GameObject front;
            public GameObject root { get; private set; }
            private GameObject[] worldAnchor;
            private bool[] isActiveWorldAnchor;

            private JsonAnchor json;

            public bool anchorLoaded { get; private set; }

            private int anchorAllLoaded = 0;
            private int CheckNum = 0;
            private Vector3 CheckPoint;
            public CenterAnchor(GameObject prefab, string name, int anchorCount)
            {
                center = GameObject.Instantiate(prefab);
                center.name = name;
                front = new GameObject(center.name + "_front");
                root = new GameObject(center.name + "_root");
                root.transform.SetParent(center.transform);
                worldAnchor = new GameObject[anchorCount];
                isActiveWorldAnchor = new bool[worldAnchor.Length];
                for (int i = 0; i < worldAnchor.Length; i++)
                {
                    worldAnchor[i] = GameObject.Instantiate(prefab);
                    worldAnchor[i].name = center.name + i.ToString();
                    isActiveWorldAnchor[i] = false;
                }
                CheckPoint = new Vector3();
            }

            public void LoadWorldAnchor(JsonAnchor _json, WorldAnchorControl anchor)
            {
                anchorLoaded = false;
                anchorAllLoaded = 0;
                json = _json;
                anchor.LoadedEvent += LoadedEvent;
                for (int i = 0; i < worldAnchor.Length; i++)
                {
                    isActiveWorldAnchor[i] = false;
                    anchor.LoadWorldAnchor(worldAnchor[i]);
                }
            }

            private void LoadedEvent(WorldAnchorControl self, GameObject go, bool success)
            {
                int count = 0;
                for (int i = 0; i < worldAnchor.Length; i++)
                {
                    if (worldAnchor[i].name == go.name)
                    {
                        if (success == true)
                        {
                            isActiveWorldAnchor[i] = true;
                        }
                        anchorAllLoaded++;
                    }
                    if (isActiveWorldAnchor[i] == true)
                    {
                        count++;
                    }
                }
                if (count > worldAnchor.Length * StaticParameter.minAnchorLength && anchorLoaded == false)
                {
                    anchorLoaded = SetCenterFromWorldAnchor();
                }
                if (anchorAllLoaded >= worldAnchor.Length)
                {
                    self.LoadedEvent -= LoadedEvent;
                }
            }

            public bool SetCenterFromWorldAnchor(int count = 0, System.Random _r = null)
            {
                // select worldanchor num
                List<int> list = new List<int>();
                for (int i = 0; i < isActiveWorldAnchor.Length; i++)
                {
                    if (isActiveWorldAnchor[i] == true)
                    {
                        list.Add(i);
                    }
                }
                System.Random r;
                if (_r == null)
                {
                    r = new System.Random();
                }
                else
                {
                    r = _r;
                }
                int point1 = list[r.Next(0, list.Count - 1)];
                int point2 = list[r.Next(0, list.Count - 1)];
                int point3 = list[r.Next(0, list.Count - 1)];
                // json distance
                Vector3 j1 = json.worldanchorCenter[point1].GetVector3();
                Vector3 j2 = json.worldanchorCenter[point2].GetVector3();
                Vector3 j3 = json.worldanchorCenter[point3].GetVector3();
                float j12 = Vector3.Distance(j1, j2);
                float j23 = Vector3.Distance(j2, j3);
                float j31 = Vector3.Distance(j3, j1);
                // real distance
                Vector3 r1 = worldAnchor[point1].transform.position;
                Vector3 r2 = worldAnchor[point2].transform.position;
                Vector3 r3 = worldAnchor[point3].transform.position;
                float r12 = Vector3.Distance(r1, r2);
                float r23 = Vector3.Distance(r2, r3);
                float r31 = Vector3.Distance(r3, r1);
                // cheack distance
                if (point1 != point2 && point2 != point3 && point3 != point1)
                {
                    if ((j12 - r12) < StaticParameter.maxDistance && (j23 - r23) < StaticParameter.maxDistance && (j31 - r31) < StaticParameter.maxDistance)
                    {
                        // set center pos
                        Vector3 x1 = -j1;
                        Vector3 x2 = j2 - j1;
                        Vector3 x4 = j3 - j1;
                        Vector3 x11 = r2 - r1;
                        Vector3 x31 = r3 - r1;
                        float a = (x11.z * x1.x * x4.x + x11.z * x1.z * x4.z - x31.z * x1.x * x2.x - x31.z * x1.z * x2.z) / (x11.z * x31.x - x31.z * x11.x);
                        float b = 0.0f;
                        float c = (x1.x * x2.x + x1.z * x2.z - x11.x * a) / (x11.z);
                        if (a != float.NaN && c != float.NaN)
                        {
                            center.transform.position = r1 + new Vector3(a, b, c);
                            CheckNum = point1;
                            CheckPoint = worldAnchor[CheckNum].transform.position;
                            // set front pos
                            j1 = json.worldanchorFront[point1].GetVector3();
                            j2 = json.worldanchorFront[point2].GetVector3();
                            j3 = json.worldanchorFront[point3].GetVector3();
                            r1 = worldAnchor[point1].transform.position;
                            r2 = worldAnchor[point2].transform.position;
                            r3 = worldAnchor[point3].transform.position;
                            x1 = -j1;
                            x2 = j2 - j1;
                            x4 = j3 - j1;
                            x11 = r2 - r1;
                            x31 = r3 - r1;
                            a = (x11.z * x1.x * x4.x + x11.z * x1.z * x4.z - x31.z * x1.x * x2.x - x31.z * x1.z * x2.z) / (x11.z * x31.x - x31.z * x11.x);
                            b = 0.0f;
                            c = (x1.x * x2.x + x1.z * x2.z - x11.x * a) / (x11.z);
                            front.transform.position = r1 + new Vector3(a, b, c);
                            // set center rot
                            center.transform.LookAt(front.transform, Vector3.up);
                            // set root
                            root.transform.localPosition = json.rootPosition.GetVector3();
                            root.transform.localRotation = json.rootRotation.GetQuaternion();
                            return true;
                        }

                    }
                }
                if (count < StaticParameter.maxReturnCount)
                {
                    return SetCenterFromWorldAnchor(count++, r);
                }
                else
                {
                    return false;
                }
            }

            public void SetCenterAndWorldAnchor(Vector3 pos, Quaternion rot, WorldAnchorControl anchor)
            {
                center.transform.SetPositionAndRotation(pos, rot);
                front.transform.position = center.transform.position + new Vector3(center.transform.forward.x, 0, center.transform.forward.z);
                center.transform.LookAt(front.transform, Vector3.up);

                System.Random r = new System.Random();
                for (int i = 0; i < worldAnchor.Length; i++)
                {
                    anchor.DeleteWorldAnchor(worldAnchor[i]);
                    Vector3 buf = center.transform.position;
                    buf.x += ((float)r.NextDouble() - 0.5f) * StaticParameter.anchorDistance * 2.0f;
                    buf.z += ((float)r.NextDouble() - 0.5f) * StaticParameter.anchorDistance * 2.0f;
                    worldAnchor[i].transform.position = buf;
                    anchor.SaveWorldAnchor(worldAnchor[i]);
                }
            }

            public void SetRootPosition(GameObject go)
            {
                root.transform.SetPositionAndRotation(go.transform.position, go.transform.rotation);
            }

            public JsonAnchor SaveWorldAnchor()
            {
                JsonAnchor json = new JsonAnchor(worldAnchor.Length);
                json.rootPosition.SetVector3(root.transform.localPosition);
                json.rootRotation.SetQuaternion(root.transform.localRotation);
                for (int i = 0; i < worldAnchor.Length; i++)
                {
                    json.worldanchorCenter[i].SetVector3(center.transform.InverseTransformPoint(worldAnchor[i].transform.position));
                    json.worldanchorFront[i].SetVector3(front.transform.InverseTransformPoint(worldAnchor[i].transform.position));
                }
                return json;
            }
            public void isView(bool flag)
            {
                SetActiveChiled(center, flag);
                for (int i = 0; i < worldAnchor.Length; i++)
                {
                    SetActiveChiled(worldAnchor[i], flag);
                }
            }

            private void SetActiveChiled(GameObject obj, bool flag)
            {
                for (int i = 0; i < obj.transform.childCount; i++)
                {
                    obj.transform.GetChild(i).gameObject.SetActive(flag);
                }
            }

            public bool CheckCenterPosition()
            {
                float xdis = Vector3.Distance(worldAnchor[CheckNum].transform.position, CheckPoint);
                return (xdis < 0.01f) ? true : false;
            }
        }
        #endregion

        public GameObject WorldAnchorPrefab;

        public delegate void FinishSettingEventHandler();
        public FinishSettingEventHandler FinishSettingEvent;

        private string AnchorLocalPositionFile = "info.json";

        // model setting
        private bool isActive = false;
        private bool isView = true;
        private WorldAnchorControl worldAnchorControl;

        private CenterAnchor[] SubCenterAnchor;
        private bool[] imageTrackingCount;
        private float t = 0.0f;

        // Use this for initialization
        void Start()
        {
            worldAnchorControl = GetComponent<WorldAnchorControl>();
        }

        public void InitModelPositionManager(GameObject[] targets)
        {
            SubCenterAnchor = new CenterAnchor[targets.Length];
            imageTrackingCount = new bool[SubCenterAnchor.Length];
            for (int i = 0; i < SubCenterAnchor.Length; i++)
            {
                SubCenterAnchor[i] = new CenterAnchor(WorldAnchorPrefab, targets[i].name, StaticParameter.worldAnchorCount);
                imageTrackingCount[i] = false;
            }

            LoadCenterData();
        }

        private void LoadCenterData()
        {
            StartCoroutine(LoadCoroutine());
        }

        private IEnumerator LoadCoroutine()
        {
            if (File.Exists(Application.persistentDataPath + "\\" + AnchorLocalPositionFile) == true)
            {
                string data = File.ReadAllText(Application.persistentDataPath + "\\" + AnchorLocalPositionFile);
                JsonCenter jsonCenter = new JsonCenter(SubCenterAnchor.Length, StaticParameter.worldAnchorCount);
                jsonCenter = JsonUtility.FromJson<JsonCenter>(data);

                if (jsonCenter != null)
                {
                    for (int i = 0; i < SubCenterAnchor.Length; i++)
                    {
                        yield return null;
                        SubCenterAnchor[i].LoadWorldAnchor(jsonCenter.worldanchor[i], worldAnchorControl);
                    }
                }
            }
        }

        public void TrackingImageTargetEvent(int num, Vector3 pos, Quaternion rot)
        {
            SubCenterAnchor[num].SetCenterAndWorldAnchor(pos, rot, worldAnchorControl);
            imageTrackingCount[num] = true;
            int count = 0;
            for (int i = 0; i < imageTrackingCount.Length; i++)
            {
                if (imageTrackingCount[i] == true)
                {
                    count++;
                }
            }
            if (count == SubCenterAnchor.Length)
            {
                for (int i = 0; i < SubCenterAnchor.Length; i++)
                {
                    SubCenterAnchor[i].SetRootPosition(SubCenterAnchor[0].center);
                }
                SaveCenterData();
                LoadCenterData();
                StopSetting();
            }
        }

        private void SaveCenterData()
        {
            JsonCenter jsonCenter = new JsonCenter(SubCenterAnchor.Length, StaticParameter.worldAnchorCount);
            for (int i = 0; i < SubCenterAnchor.Length; i++)
            {
                jsonCenter.worldanchor[i] = SubCenterAnchor[i].SaveWorldAnchor();
            }
            string data = JsonUtility.ToJson(jsonCenter);
            File.WriteAllText(Application.persistentDataPath + "\\" + AnchorLocalPositionFile, data);
        }

        void Update()
        {
            if (isActive == false)
            {
                bool resetflag = false;
                for (int i = 0; i < SubCenterAnchor.Length; i++)
                {
                    if (SubCenterAnchor[i].anchorLoaded == true)
                    {
                        if (SubCenterAnchor[i].CheckCenterPosition() == false)
                        {
                            resetflag = true;
                            break;
                        }
                    }
                }

                if (t > 10.0f)
                {
                    resetflag = true;
                    t = 0.0f;
                }
                else
                {
                    t += Time.deltaTime;
                }

                int num = -1;
                float distance = -1.0f;
                for (int i = 0; i < SubCenterAnchor.Length; i++)
                {
                    if (SubCenterAnchor[i].anchorLoaded == true)
                    {
                        // active
                        if (resetflag == true)
                        {
                            SubCenterAnchor[i].SetCenterFromWorldAnchor();
                        }
                        float buf = Vector3.Distance(SubCenterAnchor[i].center.transform.position, Camera.main.transform.position);
                        if (distance < 0.0f)
                        {
                            distance = buf;
                            num = i;
                        }
                        else
                        {
                            if (buf < distance)
                            {
                                distance = buf;
                                num = i;
                            }
                        }
                    }
                }
                if (num != -1)
                {
                    transform.SetPositionAndRotation(SubCenterAnchor[num].root.transform.position, SubCenterAnchor[num].root.transform.rotation);
                }
            }
        }

        public bool StartSetting()
        {
            if (isActive == false)
            {
                isActive = true;
                for (int i = 0; i < imageTrackingCount.Length; i++)
                {
                    imageTrackingCount[i] = false;
                }
                return true;
            }
            else
            {
                for (int i = 0; i < SubCenterAnchor.Length; i++)
                {
                    SubCenterAnchor[i].SetRootPosition(SubCenterAnchor[0].center);
                }
                SaveCenterData();
                LoadCenterData();
                StopSetting();
                return false;
            }
        }

        private void StopSetting()
        {
            isActive = false;
            if (FinishSettingEvent != null) FinishSettingEvent();
        }

        public bool IsViewObject()
        {
            isView = !isView;
            for (int i = 0; i < SubCenterAnchor.Length; i++)
            {
                SubCenterAnchor[i].isView(isView);
            }
            return isView;
        }
    }

    #region Json Parameter
    [Serializable]
    public class JsonVector3
    {
        public float x = 0.0f;
        public float y = 0.0f;
        public float z = 0.0f;
        public void SetVector3(Vector3 v)
        {
            x = (v.x == float.NaN) ? 0.0f : v.x;
            y = (v.y == float.NaN) ? 0.0f : v.y;
            z = (v.z == float.NaN) ? 0.0f : v.z;
        }
        public Vector3 GetVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    [Serializable]
    public class JsonQuaternion
    {
        public float x = 0.0f;
        public float y = 0.0f;
        public float z = 0.0f;
        public float w = 1.0f;
        public void SetQuaternion(Quaternion q)
        {
            x = (q.x == float.NaN) ? 0.0f : q.x;
            y = (q.y == float.NaN) ? 0.0f : q.y;
            z = (q.z == float.NaN) ? 0.0f : q.z;
            w = (q.w == float.NaN) ? 1.0f : q.w;
        }
        public Quaternion GetQuaternion()
        {
            return new Quaternion(x, y, z, w);
        }
    }

    [Serializable]
    public class JsonAnchor
    {
        public JsonVector3 rootPosition;
        public JsonQuaternion rootRotation;
        public JsonVector3[] worldanchorCenter;
        public JsonVector3[] worldanchorFront;
        public JsonAnchor(int length)
        {
            rootPosition = new JsonVector3();
            rootRotation = new JsonQuaternion();
            worldanchorCenter = new JsonVector3[length];
            worldanchorFront = new JsonVector3[length];
            for (int i = 0; i < worldanchorCenter.Length; i++)
            {
                worldanchorCenter[i] = new JsonVector3();
                worldanchorFront[i] = new JsonVector3();
            }
        }
    }

    [Serializable]
    public class JsonCenter
    {
        public JsonAnchor[] worldanchor;
        public JsonCenter(int length, int anchorlength)
        {
            worldanchor = new JsonAnchor[length];
            for (int i = 0; i < worldanchor.Length; i++)
            {
                worldanchor[i] = new JsonAnchor(anchorlength);
            }
        }
    }
    #endregion
}
