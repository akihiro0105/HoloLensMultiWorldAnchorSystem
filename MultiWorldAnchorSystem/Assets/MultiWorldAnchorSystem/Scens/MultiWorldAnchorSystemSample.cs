using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.XR.WSA.Input;
using MultiWorldAnchorSystem;

/// <summary>
/// MultiWorldAnchorSystemのサンプル
/// - AirTapでMultiWorldAnchor設置
/// - ドラッグでHubAnchor移動
/// 
/// <動作確認>
/// 1. サンプルシーンでは位置合わせを行いたいモデルを円柱形，基準HubAnchorのモデルを立方体モデル，サポートHubAnchorモデルを球モデルとしている
/// 2. アプリ起動後ドラッグで立方体と球モデルを移動して，AirTapで円柱のMultiWorldAnchorを設置する
/// 3. 一旦アプリを終了させて再度起動するとMultiWorldAnchorで設置した円柱形モデルのみが設置したときの物理空間に固定される
/// </summary>
public class MultiWorldAnchorSystemSample : MonoBehaviour
{
    [SerializeField] private GameObject[] hubAnchorGameObjects;

    // Use this for initialization
    void Start () {
        var multiWorldAnchor = GetComponent<MultiWorldAnchor>();
        var gesture = new GestureRecognizer();
        // AirTap時の動作を定義
        gesture.Tapped += args =>
        {
            // MultiWorldAnchor設置
            multiWorldAnchor.SetHubAnchor(hubAnchorGameObjects);
        };
        // ドラッグ時の動作を定義
        gesture.NavigationUpdated += args =>
        {
            // 基準モデル移動
            var delta = args.normalizedOffset;
            for (int i = 0; i < hubAnchorGameObjects.Length; i++)
            {
                hubAnchorGameObjects[i].transform.position += delta / 100.0f;
            }
        };
        // ジェスチャー認識開始
        gesture.StartCapturingGestures();
    }
}
