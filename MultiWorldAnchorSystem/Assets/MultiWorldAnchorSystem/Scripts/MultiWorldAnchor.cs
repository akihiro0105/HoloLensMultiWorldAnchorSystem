using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MultiWorldAnchorSystem
{
    /// <summary>
    /// <内部処理説明>
    /// CenterObject
    /// - hubオブジェクトから算出された空間全体の基準点オブジェクト
    /// 
    /// HubObject(root)
    /// - WorldAnchor群の相対位置から算出されたCenterObject位置算出のための特定エリア毎の基準点オブジェクト
    /// - CenterObjectとの相対座標、相対回転情報を持つ
    /// 
    /// HubObject(front)
    /// - WorldAnchor群の相対位置から算出されたhubオブジェクト(Root)の前方方向を指定するためのオブジェクト
    ///
    /// WorldAnchor群
    /// - HubObject位置算出のために周辺に複数配置されたWorldAnchorオブジェクト
    /// - HubObject(root,front)との相対座標、相対回転情報を持つ
    ///
    /// -----
    /// 
    /// HubObject位置決定時
    /// - HubObjectの位置を特定の場所へ指定後，指定位置周辺にランダムにWorldAnchorを複数配置する
    /// - WorldAnchor配置後HubObjectとWorldAnchorとの相対座標を記録しローカルファイルに保存する
    ///
    /// HubObject位置復帰方法
    /// - HubObjectに紐づいているWorldAnchor群の再設置を行う
    /// - 一定数再設置完了後ランダムにWorldAnchorを3点選択する
    /// - 選択されたWorldAnchor同士の相対距離を計測し，記録データから算出した相対距離と比較する
    /// - 記録データと現実データとの誤差が一定未満であれば選択された3点のWorldAnchorは正常な位置に設置されているとみなし，HubObjectを設置する
    /// - 記録データととの誤差が一定以上だった場合は別のWorldAnchorの組み合わせを選択して同様の比較を行う
    /// - 一定間隔で上記のHubObject位置の算出を行い補正を行う
    ///
    /// -----
    ///
    /// CenterObject位置決定時
    /// - HubObject設置時にCenterObjectの位置を決定する
    /// - 0番目のHubObject(root)位置をCenterObjectの位置とする
    /// - CenterObjectの前方方向を0番目のHubObject(front)の方向とする
    /// - ただし0番目HubObjectの高さ位置はCenterObjectと同じ位置とする
    ///
    /// CenterObject位置復帰方法
    /// - HoloLensから一番近いHubObjectを選択し，記録された相対座標からCenterObjectを算出
    /// - 一定間隔で上記のCenterObjectの算出を行い位置の補正を行う
    ///
    /// -----
    ///
    /// <利用コード>
    /// MultiWorldAnchor.cs : アンカー情報の読み込み保存と更新処理
    /// HubAnchor.cs : CenterObject,HubAnchorの位置合わせ処理
    /// WorldAnchorControl.cs : WorldAnchorの保存と読み込み制御処理
    /// JsonAnchorData.cs : アンカー情報のJson化と保存，読み込み
    /// 
    /// </summary>
    public struct StaticParameter
    {
        public const float anchorDistance = 3.0f;// WorldAnchorの設置直径範囲(正方形)
        public const int worldAnchorCount = 10;// WorldAnchor設置数
        public const float maxDistance = 0.001f;// WorldAnchor復帰計算時の誤差許容範囲
        public const int maxReturnCount = 50;// WorldAnchor復帰再計算の限度回数
        public const float minAnchorLength = 0.9f;// 最小復帰処理可能なWorldAnchor設置率
    }
    public class MultiWorldAnchor : MonoBehaviour
    {
        /// <summary>
        /// 基準オブジェクト
        /// </summary>
        [SerializeField] private GameObject worldCenterObject;

        /// <summary>
        /// Hubアンカー制御クラス
        /// </summary>
        private List<HubAnchor> hubAnchors = new List<HubAnchor>();

        /// <summary>
        /// WorldAnchor制御処理
        /// </summary>
        private WorldAnchorControl anchor;

        /// <summary>
        /// HubAnchor設置完了フラグ
        /// </summary>
        private bool isLoadedAnchor = false;

        /// <summary>
        /// タイミング管理用
        /// </summary>
        private float t = 0.0f;

        void Start()
        {
            anchor = GetComponent<WorldAnchorControl>();
            Init();
        }

        /// <summary>
        /// アンカーの初期化と再設置
        /// </summary>
        public void Init()
        {
            isLoadedAnchor = false;
            // アンカーデータ読み込み
            var data = JsonAnchorData.LoadAnchorData();
            if (data != null)
            {
                // 初期化
                var loadedCount = 0;
                for (int i = 0; i < data.Count; i++)
                {
                    var hub = new HubAnchor(i.ToString());
                    // アンカー再設置
                    hub.LoadedHubAnchor += () =>
                    {
                        loadedCount++;
                        if (loadedCount == data.Count)
                        {
                            isLoadedAnchor = true;
                        }
                    };
                    hub.LoadAnchorData(anchor, data[i]);
                    hubAnchors.Add(hub);
                }
            }
        }

        /// <summary>
        /// HubAnchorの再設置
        /// </summary>
        /// <param name="goList"></param>
        public void SetHubAnchor(GameObject[] goList)
        {
            // 初期化
            if (hubAnchors.Count != goList.Length)
            {
                hubAnchors.Clear();
                for (int i = 0; i < goList.Length; i++)
                {
                    hubAnchors.Add(new HubAnchor(i.ToString()));
                }
            }

            // 0番目をCenterに指定
            var savedCount = 0;
            for (int i = 0; i < goList.Length; i++)
            {
                hubAnchors[i].SetRootHubAndRootObjectTransform(goList[i].transform, goList[0].transform, anchor);
                // アンカー保存処理
                hubAnchors[i].SavedHubAnchor += () =>
                {
                    savedCount++;
                    if (savedCount == goList.Length)
                    {
                        // アンカーデータ保存
                        var list = new List<JsonHubAnchor>();
                        for (var j = 0; j < hubAnchors.Count; j++)
                        {
                            list.Add(hubAnchors[j].GetJsonHubAnchor());
                        }

                        JsonAnchorData.SaveAnchorData(list);

                        // アンカー再設置
                        isLoadedAnchor = false;
                        var loadedCount = 0;
                        for (var j = 0; j < list.Count; j++)
                        {
                            hubAnchors[j].LoadedHubAnchor += () =>
                            {
                                loadedCount++;
                                if (loadedCount == list.Count)
                                {
                                    isLoadedAnchor = true;
                                }
                            };
                            hubAnchors[j].LoadAnchorData(anchor, list[j]);
                        }
                    }
                };
            }
        }

        void Update()
        {
            // アンカー読み込み後更新処理
            if (isLoadedAnchor == true)
            {
                // アンカー位置再計算フラグ
                var resetflag = false;
                // アンカーずれ検知
                for (int i = 0; i < hubAnchors.Count; i++)
                {
                    if (hubAnchors[i].CheckDistanceDelta() == false)
                    {
                        resetflag = true;
                        break;
                    }
                }

                // 定期補正検知
                if (t > 10.0f)
                {
                    resetflag = true;
                    t = 0.0f;
                }
                else
                {
                    t += Time.deltaTime;
                }

                // アンカー補正処理
                int? num = null;
                float distance = 0.0f;
                for (var i = 0; i < hubAnchors.Count; i++)
                {
                    // アンカー位置再計算
                    if (resetflag == true)
                    {
                        // 保存が間に合わない
                        hubAnchors[i].CreateHubAnchor();
                    }

                    // 最近傍HubAnchor取得処理
                    var buf = Vector3.Distance(hubAnchors[i].GetRootHubObjectTransform().position,
                        Camera.main.transform.position);
                    if (num == null)
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

                // 参照HubAnchor変更処理
                if (num != null)
                {
                    var tf = hubAnchors[num.Value].GetCenterObjectTransform();
                    worldCenterObject.transform.SetPositionAndRotation(tf.position, tf.rotation);
                }
            }
        }
    }

}
