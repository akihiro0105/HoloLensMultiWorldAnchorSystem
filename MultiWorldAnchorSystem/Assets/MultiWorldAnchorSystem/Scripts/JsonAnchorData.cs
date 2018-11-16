﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MultiWorldAnchorSystem
{
    public class JsonAnchorData
    {
        private static string fileName = "info.json";

        public static List<JsonAnchor> LoadAnchorData()
        {
            if (File.Exists(Application.persistentDataPath + "\\" + fileName) == true)
            {
                string data = File.ReadAllText(Application.persistentDataPath + "\\" + fileName);
                var json = new JsonCenter();
                json = JsonUtility.FromJson<JsonCenter>(data);
                return json.worldanchor;
            }
            else
            {
                return null;
            }
        }

        public static void SaveAnchorData(List<JsonAnchor> data)
        {
            string json = JsonUtility.ToJson(new JsonCenter() {worldanchor = data});
            File.WriteAllText(Application.persistentDataPath + "\\" + fileName, json);
        }
    }

    [Serializable]
    public class JsonVector3
    {
        public float x = 0.0f;
        public float y = 0.0f;
        public float z = 0.0f;
        public JsonVector3() { }
        public JsonVector3(Vector3 v)
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
        public JsonQuaternion() { }
        public JsonQuaternion(Quaternion q)
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
        public JsonVector3 rootPosition=new JsonVector3();
        public JsonQuaternion rootRotation=new JsonQuaternion();
        public List<JsonVector3> worldanchorCenter=new List<JsonVector3>();
        public List<JsonVector3> worldanchorFront=new List<JsonVector3>();
    }

    [Serializable]
    public class JsonCenter
    {
        public List<JsonAnchor> worldanchor=new List<JsonAnchor>();
    }
}