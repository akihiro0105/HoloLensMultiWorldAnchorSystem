using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MultiWorldAnchorSystem
{
    /// <summary>
    /// WorldAnchor情報の保存と再生
    /// </summary>
    public class JsonAnchorData
    {
        private static string fileName = "info.json";

        /// <summary>
        /// HubAnchorデータの読み込み
        /// </summary>
        /// <returns></returns>
        public static List<JsonHubAnchor> LoadAnchorData()
        {
            var path = Application.persistentDataPath + "\\" + fileName;
            if (File.Exists(path) == true)
            {
                var data = File.ReadAllText(path);
                var json = new JsonCenter();
                json = JsonUtility.FromJson<JsonCenter>(data);
                return json.worldanchor;
            }
            else return null;
        }

        /// <summary>
        /// HubAnchorデータの保存
        /// </summary>
        /// <param name="data"></param>
        public static void SaveAnchorData(List<JsonHubAnchor> data)
        {
            var json = JsonUtility.ToJson(new JsonCenter() {worldanchor = data});
            File.WriteAllText(Application.persistentDataPath + "\\" + fileName, json);
        }
    }

    [Serializable]
    public class JsonHubAnchor
    {
        public Vector3 rootPosition = new Vector3();
        public Quaternion rootRotation = new Quaternion();
        public List<Vector3> worldanchorCenter = new List<Vector3>();
        public List<Vector3> worldanchorFront = new List<Vector3>();
    }

    [Serializable]
    public class JsonCenter
    {
        public List<JsonHubAnchor> worldanchor = new List<JsonHubAnchor>();
    }
}
