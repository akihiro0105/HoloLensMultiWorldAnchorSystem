using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiWorldAnchorSystem
{

    public class HubAnchor 
    {
    }

    public class CenterAnchor
    {
        /// <summary>
        /// Hubアンカーの有効フラグ
        /// </summary>
        public bool Enable = false;

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


        public GameObject root { get; private set; }
        private bool[] isActiveWorldAnchor;

        private JsonHubAnchor json;

        public bool anchorLoaded { get; private set; }

        private int anchorAllLoaded = 0;
        private int CheckNum = 0;
        private Vector3 CheckPoint;
        public CenterAnchor(string name, int anchorCount)
        {
            centerObject = new GameObject(name);
            rootHubObject = new GameObject(name+"_hubRoot");
            frontHubObject = new GameObject(name + "_hubFront");

            worldAnchorObjects = new GameObject[anchorCount];
            isActiveWorldAnchor = new bool[worldAnchorObjects.Length];
            for (int i = 0; i < worldAnchorObjects.Length; i++)
            {
                worldAnchorObjects[i] = new GameObject(name + "_"+i.ToString());
                isActiveWorldAnchor[i] = false;
            }


            root = new GameObject(centerObject.name + "_root");
            root.transform.SetParent(centerObject.transform);
            
          
            CheckPoint = new Vector3();
            Enable = false;
        }

        public void LoadWorldAnchor(JsonHubAnchor _json, WorldAnchorControl anchor)
        {
            anchorLoaded = false;
            anchorAllLoaded = 0;
            json = _json;
            anchor.LoadedEvent += LoadedEvent;
            for (int i = 0; i < worldAnchorObjects.Length; i++)
            {
                isActiveWorldAnchor[i] = false;
                anchor.LoadWorldAnchor(worldAnchorObjects[i]);
            }
        }

        private void LoadedEvent(WorldAnchorControl self, GameObject go, bool success)
        {
            int count = 0;
            for (int i = 0; i < worldAnchorObjects.Length; i++)
            {
                if (worldAnchorObjects[i].name == go.name)
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
            if (count > worldAnchorObjects.Length * StaticParameter.minAnchorLength && anchorLoaded == false)
            {
                anchorLoaded = SetCenterFromWorldAnchor();
            }
            if (anchorAllLoaded >= worldAnchorObjects.Length)
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
                    if (a != float.NaN && c != float.NaN)
                    {
                        centerObject.transform.position = r1 + new Vector3(a, b, c);
                        CheckNum = point1;
                        CheckPoint = worldAnchorObjects[CheckNum].transform.position;
                        // set front pos
                        j1 = json.worldanchorFront[point1].GetVector3();
                        j2 = json.worldanchorFront[point2].GetVector3();
                        j3 = json.worldanchorFront[point3].GetVector3();
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
            centerObject.transform.SetPositionAndRotation(pos, rot);
            frontHubObject.transform.position = centerObject.transform.position + new Vector3(centerObject.transform.forward.x, 0, centerObject.transform.forward.z);
            centerObject.transform.LookAt(frontHubObject.transform, Vector3.up);

            System.Random r = new System.Random();
            for (int i = 0; i < worldAnchorObjects.Length; i++)
            {
                anchor.DeleteWorldAnchor(worldAnchorObjects[i]);
                Vector3 buf = centerObject.transform.position;
                buf.x += ((float)r.NextDouble() - 0.5f) * StaticParameter.anchorDistance * 2.0f;
                buf.z += ((float)r.NextDouble() - 0.5f) * StaticParameter.anchorDistance * 2.0f;
                worldAnchorObjects[i].transform.position = buf;
                anchor.SaveWorldAnchor(worldAnchorObjects[i]);
            }

            Enable = true;
        }

        public void SetRootPosition(GameObject go)
        {
            root.transform.SetPositionAndRotation(go.transform.position, go.transform.rotation);
        }

        public Vector3 GetCenterLocalPosition()
        {
            return root.transform.localPosition;
        }

        public Quaternion GetCenterLocalRotation()
        {
            return root.transform.localRotation;
        }

        public Vector3[] GetWorldAnchorLocalPositionRoot()
        {
            var list = new Vector3[worldAnchorObjects.Length];
            for (int i = 0; i < list.Length; i++)
            {
                list[i]= centerObject.transform.InverseTransformPoint(worldAnchorObjects[i].transform.position);
            }
            return list;
        }

        public Vector3[] GetWorldAnchorLocalPositionFront()
        {
            var list = new Vector3[worldAnchorObjects.Length];
            for (int i = 0; i < list.Length; i++)
            {
                list[i] = frontHubObject.transform.InverseTransformPoint(worldAnchorObjects[i].transform.position);
            }
            return list;
        }

        public GameObject GetCenterObject()
        {
            return centerObject;
        }

        public void isView(bool flag)
        {
            SetActiveChiled(centerObject, flag);
            for (int i = 0; i < worldAnchorObjects.Length; i++)
            {
                SetActiveChiled(worldAnchorObjects[i], flag);
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
            var xdis = Vector3.Distance(worldAnchorObjects[CheckNum].transform.position, CheckPoint);
            return (xdis < 0.01f) ? true : false;
        }
    }
}

