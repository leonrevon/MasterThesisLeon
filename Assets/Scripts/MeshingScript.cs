using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityGoogleDrive;

public class MeshingScript : MonoBehaviour
{
    List<GameObject> myChildObjects;
    public GameObject gameObject;    
    List<string> nameList = new List<string>();
    public List<string> gtHitName = new List<string>();
    Dictionary<string, int> meshVote = new Dictionary<string, int>();
    Dictionary<string, int> gtNumber = new Dictionary<string, int>();
    

    public Text text;
    public Text text2;
    public Text text3;

    public Material material;
    string voteResult = "";
    string voteResult2 = "";
    string voteResult3 = "";
    string filePath;
    string filePathName;

    public void ChangeScene()
    {
        SceneManager.LoadScene("AllPointCloudPoints");
    }

    private void Start()
    {
        myChildObjects = gameObject.GetComponentsInChildren<Transform>().ToList().Select(x => x.gameObject).ToList();
        myChildObjects.ForEach(myChildObject =>
        {
            string name = myChildObject.name;
            nameList.Add(myChildObject.name);            
        });        
    }
    private void Update()
    {
        for (int k = -2; k < 2; k++)
        {
            for (int j = -2; j < 2; j++)
            {
                float screenX = 0 + j;
                float screenY = 0 + k;
                Vector3 forward = Camera.main.transform.TransformDirection(screenX, screenY, 100);
                RayCastMethod(forward);
            }
        }

        foreach (string name in nameList)
        {
            VoteResultPrint(name);            
        }

        voteResult = "";

        foreach (KeyValuePair<string, int> entry in meshVote)
        {
            if (entry.Value > 100)
            {
                GameObject.Find(entry.Key).GetComponent<Renderer>().sharedMaterial = material;
            }            
        }

    }   

    void RayCastMethod(Vector3 forward)
    {
        RaycastHit[] hits;
        hits = Physics.RaycastAll(Camera.main.transform.position, forward);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider.CompareTag("Mesh"))
            {
                Vector3 meshPoint = hits[i].point;

                for (int j = 0; j < hits.Length; j++)
                {
                    var dis = Vector3.Distance(meshPoint, hits[j].point);                    
                    if (dis < 0.01 && hits[j].collider.CompareTag("CAD"))
                    {
                        MeshCheckVote(hits[j].collider.name);
                    }
                }
            }
            else
            {
                GTCheckVote(hits[i].collider.name);
            }
        }
    }

    void MeshCheckVote(string name)
    {
        if (!meshVote.ContainsKey(name))        
            meshVote.Add(name, 0);
        
        meshVote[name]++;
    }

    void GTCheckVote(string name)
    {
        if (!gtNumber.ContainsKey(name))
            gtNumber.Add(name, 0);

        gtNumber[name]++;                
    }

    void VoteResultPrint(string name)
    {
        if (meshVote.ContainsKey(name))
        {
            voteResult = voteResult + "\n" + name + ": " + gtNumber[name].ToString();//GT captured            
            voteResult2 = voteResult2 + "\n" + name + ": " + PercentageCount(meshVote[name], gtNumber[name]).ToString("F2") + "%"; // Percentage
            voteResult3 = voteResult3 + "\n" + name + ": " + meshVote[name].ToString();//PC captured            


            text.text = voteResult;
            text2.text = voteResult2;
            text3.text = voteResult3;

            if (PercentageCount(meshVote[name], gtNumber[name]) > 0.1)
            {
                GameObject.Find(name).GetComponent<Renderer>().sharedMaterial = material;
            }
        }
    }

    float PercentageCount(float part, float gt)
    {
        return part * 100 / gt;
    }

    public void GenerateSheets()
    {
        filePathName = "meshGTAndPartsSummary.csv";

        filePath = Application.persistentDataPath + "/" + filePathName;

        StreamWriter csvWriter = new StreamWriter(filePath);
        csvWriter.WriteLine("Part Name,Total Part Number,Total GT Number");

        foreach (string names in nameList)
        {
            csvWriter.WriteLine(names + "," + meshVote[names] + "," + gtNumber[names]);
        }


        csvWriter.Flush();
        csvWriter.Close();

        var plyContent = File.ReadAllBytes(filePath);
        if (plyContent == null) return;

        var plyFile = new UnityGoogleDrive.Data.File() { Name = filePathName, Content = plyContent };
        GoogleDriveFiles.Create(plyFile).Send();
    }
}