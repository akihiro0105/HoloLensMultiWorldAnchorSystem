using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiWorldAnchorSystem
{

    public class HubAnchor
    {
        public delegate void HubAnchorEventHandler();

        /// <summary>
        /// HubAnchor読み込み完了通知
        /// </summary>
        public HubAnchorEventHandler LoadedHubAnchor;

        /// <summary>
        /// HubAnchor保存完了通知
        /// </summary>
        public HubAnchorEventHandler SavedHubAnchor;

        /// <summary>
        /// センターオブジェクト
        /// </summary>
        private GameObject centerObject;

        /// <summary>
        /// Hubオブジェクト(root)
        /// </summary>
        private GameObject rootHubObject;

        /// <summary>
        /// Hubオブジェクト(front)
        /// </summary>
        private GameObject frontHubObject;

        /// <summary>
        /// WorldAnchor群
        /// </summary>
        private GameObject[] worldAnchorObjects;
        /// <summary>
        /// WorldAnchor群有効化フラグ
        /// </summary>
        private bool[] isWorldAnchorObjects;

        /// <summary>
        /// 記録されたjsonのアンカーデータ
        /// </summary>
        private JsonHubAnchor jsonHubAnchor;

        /// <summary>
        /// 保存データ比較用位置
        /// </summary>
        private Vector3 checkPosition;

        /// <summary>
        /// 保存データ比較用WorldAnchor番号
        /// </summary>
        private int checkNumber;

        /// <summary>
        /// HubAnchorの初期化
        /// </summary>
        /// <param name="anchorName"></param>
        public HubAnchor(string anchorName)
        {
            // GameObject初期化
            centerObject = new GameObject(anchorName);
            rootHubObject = new GameObject(anchorName + "_hubRoot");
            rootHubObject.transform.SetParent(centerObject.transform);
            frontHubObject = new GameObject(anchorName + "_hubFront");
            worldAnchorObjects = new GameObject[StaticParameter.worldAnchorCount];
            isWorldAnchorObjects = new bool[worldAnchorObjects.Length];
            for (var i = 0; i < worldAnchorObjects.Length; i++)
            {
                worldAnchorObjects[i] = new GameObject(anchorName + "_" + i);
                isWorldAnchorObjects[i] = false;
            }
        }

        /// <summary>
        /// Anchor情報のWorldAnchorからの取得
        /// </summary>
        public void LoadAnchorData(WorldAnchorControl anchor, JsonHubAnchor json)
        {
            jsonHubAnchor = json;
            // WorldAnchorの読み込み
            anchor.LoadedEvent += LoadedEvent;
            for (int i = 0; i < worldAnchorObjects.Length; i++)
            {
                isWorldAnchorObjects[i] = false;
                anchor.LoadWorldAnchor(worldAnchorObjects[i]);
            }
        }

        /// <summary>
        /// WorldAnchor読み込み完了処理
        /// </summary>
        /// <param name="self"></param>
        /// <param name="go"></param>
        /// <param name="success"></param>
        private void LoadedEvent(WorldAnchorControl self, GameObject go, bool success)
        {
            // WorldAnchor読み込み完了処理
            var activeCount = 0;
            for (var i = 0; i < worldAnchorObjects.Length; i++)
            {
                if (go.name == worldAnchorObjects[i].name && success == true) isWorldAnchorObjects[i] = true;
                if (isWorldAnchorObjects[i] == true) activeCount++;
            }

            if (activeCount > worldAnchorObjects.Length * StaticParameter.minAnchorLength)
            {
                // HubAnchor組み立て
                CreateHubAnchor();
                if (LoadedHubAnchor != null) LoadedHubAnchor();
                self.LoadedEvent -= LoadedEvent;
            }
        }

        /// <summary>
        /// HubAnchorの組み立て
        /// </summary>
        public void CreateHubAnchor(int count = 0, System.Random _r = null)
        {
            // select worldanchor num
            var list = new List<int>();
            for (var i = 0; i < isWorldAnchorObjects.Length; i++)
            {
                if (isWorldAnchorObjects[i] == true) list.Add(i);
            }

            if (list.Count == 0) return;
            var r = _r ?? new System.Random();
            var point1 = list[r.Next(0, list.Count - 1)];
            var point2 = list[r.Next(0, list.Count - 1)];
            var point3 = list[r.Next(0, list.Count - 1)];
            // json distance
            Vector3 j1 = jsonHubAnchor.worldanchorCenter[point1].GetVector3();
            Vector3 j2 = jsonHubAnchor.worldanchorCenter[point2].GetVector3();
            Vector3 j3 = jsonHubAnchor.worldanchorCenter[point3].GetVector3();
            float j12 = Vector3.Distance(j1, j2);
            float j23 = Vector3.Distance(j2, j3);
            float j31 = Vector3.Distance(j3, j1);
            // real distance
            Vector3 r1 = worldAnchorObjects[point1].transform.position;
            Vector3 r2 = worldAnchorObjects[point2].transform.position;
            Vector3 r3 = worldAnchorObjects[point3].transform.position;
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
                    if (!float.IsNaN(a) && !float.IsNaN(c))
                    {
                        centerObject.transform.position = r1 + new Vector3(a, b, c);
                        checkNumber = point1;
                        checkPosition = worldAnchorObjects[point1].transform.position;
                        // set front pos
                        j1 = jsonHubAnchor.worldanchorFront[point1].GetVector3();
                        j2 = jsonHubAnchor.worldanchorFront[point2].GetVector3();
                        j3 = jsonHubAnchor.worldanchorFront[point3].GetVector3();
                        r1 = worldAnchorObjects[point1].transform.position;
                        r2 = worldAnchorObjects[point2].transform.position;
                        r3 = worldAnchorObjects[point3].transform.position;
                        x1 = -j1;
                        x2 = j2 - j1;
                        x4 = j3 - j1;
                        x11 = r2 - r1;
                        x31 = r3 - r1;
                        a = (x11.z * x1.x * x4.x + x11.z * x1.z * x4.z - x31.z * x1.x * x2.x - x31.z * x1.z * x2.z) / (x11.z * x31.x - x31.z * x11.x);
                        b = 0.0f;
                        c = (x1.x * x2.x + x1.z * x2.z - x11.x * a) / (x11.z);
                        frontHubObject.transform.position = r1 + new Vector3(a, b, c);
                        // set center rot
                        centerObject.transform.LookAt(frontHubObject.transform, Vector3.up);
                        // set root
                        rootHubObject.transform.localPosition = jsonHubAnchor.rootPosition.GetVector3();
                        rootHubObject.transform.localRotation = jsonHubAnchor.rootRotation.GetQuaternion();
                        return;
                    }

                }
            }
            if (count < StaticParameter.maxReturnCount)
            {
                CreateHubAnchor(count+1, r);
            }
        }

        /// <summary>
        /// RootHubとCenterの設置とworldanchorの設置処理
        /// </summary>
        /// <param name="rootHub"></param>
        /// <param name="center"></param>
        /// <param name="anchor"></param>
        public void SetRootHubAndRootObjectTransform(Transform rootHub,Transform center, WorldAnchorControl anchor)
        {
            rootHubObject.transform.SetPositionAndRotation(rootHub.position, rootHub.rotation);

            centerObject.transform.SetPositionAndRotation(center.position, center.rotation);
            frontHubObject.transform.position = centerObject.transform.position + new Vector3(centerObject.transform.forward.x, 0, centerObject.transform.forward.z);
            centerObject.transform.LookAt(frontHubObject.transform, Vector3.up);

            var r = new System.Random();
            for (int i = 0; i < worldAnchorObjects.Length; i++)
            {
                anchor.DeleteWorldAnchor(worldAnchorObjects[i]);
                var buf = centerObject.transform.position;
                buf.x += ((float)r.NextDouble() - 0.5f) * StaticParameter.anchorDistance * 2.0f;
                buf.z += ((float)r.NextDouble() - 0.5f) * StaticParameter.anchorDistance * 2.0f;
                worldAnchorObjects[i].transform.position = buf;
                isWorldAnchorObjects[i] = false;
                anchor.SavedEvent += SavedEvent;
                anchor.SaveWorldAnchor(worldAnchorObjects[i]);
            }
        }

        /// <summary>
        /// WorldAnchor保存完了処理
        /// </summary>
        /// <param name="self"></param>
        /// <param name="go"></param>
        /// <param name="success"></param>
        private void SavedEvent(WorldAnchorControl self, GameObject go, bool success)
        {
            // WorldAnchor保存完了処理
            var activeCount = 0;
            for (var i = 0; i < worldAnchorObjects.Length; i++)
            {
                if (go.name == worldAnchorObjects[i].name && success == true) isWorldAnchorObjects[i] = true;
                if (isWorldAnchorObjects[i] == true) activeCount++;
            }

            if (activeCount > worldAnchorObjects.Length * StaticParameter.minAnchorLength)
            {
                // HubAnchor組み立て
                if (SavedHubAnchor != null) SavedHubAnchor();
                self.SavedEvent -= SavedEvent;
            }
        }

        /// <summary>
        /// Center情報を出力
        /// </summary>
        /// <returns></returns>
        public Transform GetCenterObjectTransform()
        {
            return centerObject.transform;
        }

        /// <summary>
        /// HubAnchor情報を出力
        /// </summary>
        /// <returns></returns>
        public Transform GetRootHubObjectTransform()
        {
            return rootHubObject.transform;
        }

        /// <summary>
        /// AnchorデータをJson形式に出力
        /// </summary>
        /// <returns></returns>
        public JsonHubAnchor GetJsonHubAnchor()
        {
            var json = new JsonHubAnchor();
            json.rootPosition = new JsonVector3(rootHubObject.transform.localPosition);
            json.rootRotation = new JsonQuaternion(rootHubObject.transform.localRotation);
            for (int i = 0; i < worldAnchorObjects.Length; i++)
            {
                json.worldanchorCenter.Add(new JsonVector3(centerObject.transform.InverseTransformPoint(worldAnchorObjects[i].transform.position)));
                json.worldanchorFront.Add(new JsonVector3(frontHubObject.transform.InverseTransformPoint(worldAnchorObjects[i].transform.position)));
            }

            return json;
        }

        /// <summary>
        /// 保存データとの誤差を判定
        /// </summary>
        /// <returns></returns>
        public bool CheckDistanceDelta()
        {
            var dis = Vector3.Distance(worldAnchorObjects[checkNumber].transform.position, checkPosition);
            return (dis < StaticParameter.maxDistance) ? true : false;
        }
    }
}

