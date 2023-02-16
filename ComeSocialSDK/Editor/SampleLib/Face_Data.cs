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
    public object edges;
}

[System.Serializable]
public class Root
{
    public Face_Data[] data;
    public int status;
}