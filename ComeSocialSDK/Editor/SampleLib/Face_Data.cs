using UnityEngine;

[System.Serializable]
public class Face_Data
{
    public int id;
    public string create_time;
    public string update_time;
    public string name;
    public string desc;
    public string GUID;
    public string thumbnail_url;
    public string status;
    public string type;
    public int user_id;
    public Edges edges;
}

[System.Serializable]
public class Root
{
    public Face_Data[] data;
    public int status;
}

[System.Serializable]
public class Edges {
    public Bundle[] bundle;
}

[System.Serializable]
public class Bundle {
    public int id;
    public string create_time;
    public string update_time;
    public int verionID;
    public string bundle_url;
    public string status;
    public string platform;
    public int mask_id;
}