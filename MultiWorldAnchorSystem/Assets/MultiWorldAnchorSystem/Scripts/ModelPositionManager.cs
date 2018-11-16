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

    public struct StaticParameter
    {
        public const float anchorDistance = 3.0f;// WorldAnchorの設置直径範囲
        public const int worldAnchorCount = 10;// WorldAnchor設置数
        public const float maxDistance = 0.001f;// WorldAnchor復帰計算時の誤差許容範囲
        public const int maxReturnCount = 50;// WorldAnchor復帰再計算の限度回数
        public const float minAnchorLength = 0.9f;// 最小復帰処理可能なWorldAnchor設置率
    }
    public class ModelPositionManager : MonoBehaviour
    {

        public GameObject WorldAnchorPrefab;

        public delegate void FinishSettingEventHandler();
        public FinishSettingEventHandler FinishSettingEvent;

        // model setting
        private bool isActive = false;
        private WorldAnchorControl worldAnchorControl;

        private CenterAnchor[] SubCenterAnchor;
        private float t = 0.0f;

        // Use this for initialization
        void Start()
        {
            worldAnchorControl = GetComponent<WorldAnchorControl>();
        }

        public void InitModelPositionManager(GameObject[] targets)
        {
            SubCenterAnchor = new CenterAnchor[targets.Length];
            for (int i = 0; i < SubCenterAnchor.Length; i++)
            {
                SubCenterAnchor[i] = new CenterAnchor(WorldAnchorPrefab, targets[i].name, StaticParameter.worldAnchorCount);
            }

            LoadCenterData();
        }

        private void LoadCenterData()
        {
            StartCoroutine(LoadCoroutine());
        }

        private IEnumerator LoadCoroutine()
        {
            var data = JsonAnchorData.LoadAnchorData();
            if (data!=null)
            {
                for (int i = 0; i < SubCenterAnchor.Length; i++)
                {
                    yield return null;
                    SubCenterAnchor[i].LoadWorldAnchor(data[i], worldAnchorControl);
                }
            }
        }

        public void TrackingImageTargetEvent(int num, Vector3 pos, Quaternion rot)
        {
            SubCenterAnchor[num].SetCenterAndWorldAnchor(pos, rot, worldAnchorControl);
            int count = 0;
            for (int i = 0; i < SubCenterAnchor.Length; i++)
            {
                if (SubCenterAnchor[i].Enable == true)
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
            var list = new List<JsonAnchor>();
            for (var i = 0; i < SubCenterAnchor.Length; i++)
            {
                list.Add(SubCenterAnchor[i].SaveWorldAnchor());
            }
            JsonAnchorData.SaveAnchorData(list);
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
                for (int i = 0; i < SubCenterAnchor.Length; i++)
                {
                    SubCenterAnchor[i].Enable = false;
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

        public void IsViewObject(bool flag)
        {
            for (int i = 0; i < SubCenterAnchor.Length; i++)
            {
                SubCenterAnchor[i].isView(flag);
            }
        }
    }

}
