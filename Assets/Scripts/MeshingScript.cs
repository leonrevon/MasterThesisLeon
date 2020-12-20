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

    Dictionary<string, int> cadHits = new Dictionary<string, int>();
    Dictionary<string, int> rHits = new Dictionary<string, int>();
    //Dictionary<string, int> meshVote = new Dictionary<string, int>();
    //Dictionary<string, int> gtNumber = new Dictionary<string, int>();
    Dictionary<string, float> sizePart = new Dictionary<string, float>();
    Dictionary<string, Material> defaultMaterial = new Dictionary<string, Material>();

    public Text text;
    public Text text2;
    public Text text3;

    public Material material;
    string voteResult = "";
    string voteResult2 = "";
    string voteResult3 = "";
    public float distanceThreshold = 0.1f;

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
            defaultMaterial.Add(name, myChildObject.GetComponent<Renderer>().material);
        });
    }
    private void Update()
    {
        RayCastMethod();
        VoteResultPrint();
    }

    void RayCastMethod()
    {

        //for (int k = -5; k < 5; k++)
        //{
        //    for (int j = -5; j < 5; j++)
        //    {
        //float screenX = 0 + j;
        //float screenY = 0 + k;
        //Vector3 forward = Camera.main.transform.TransformDirection(screenX, screenY, 100);
        for (int k = 0; k < 100; k++)
        {

            int meshIndices = -1;
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(Random.value, Random.value, 0));

            RaycastHit[] hits;
            hits = Physics.RaycastAll(ray);
            //hits = Physics.RaycastAll(Camera.main.transform.position, forward);

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.CompareTag("Mesh"))
                {
                    Vector3 meshPoints = hits[i].point;
                    for (int x = 0; x < hits.Length; x++)
                    {
                        var dis = Vector3.Distance(hits[x].point, meshPoints);
                        if (dis < 0.01)
                        {
                            meshIndices = x;
                            MeshCheckVote(hits[x].collider.name);
                            GTCheckVote(hits[x].collider.name);
                            List<string> hitsBefore = hits.ToList().GetRange(0, x).ToList().Select(y => y.collider.name).ToList();
                            hitsBefore.ForEach(hit =>
                            {
                                GTCheckVote(hit);
                            });
                        }
                    }
                }

                if (meshIndices < 0)
                {
                    hits.ToList().ForEach(hit =>
                    {
                        GTCheckVote(hit.collider.name);
                    });
                }
            }
            //}
            //}

        }

    }



    void MeshCheckVote(string name)
    {
        if (!rHits.ContainsKey(name))
            rHits.Add(name, 0);
        rHits[name]++;
    }

    void GTCheckVote(string name)
    {
        if (!cadHits.ContainsKey(name))
            cadHits.Add(name, 0);

        cadHits[name]++;
    }


    void VoteResultPrint()
    {
        voteResult = "";
        voteResult2 = "";
        voteResult3 = "";

        rHits.Keys.ToList().ForEach(hit =>
        {
            if (!hit.Contains("Mesh"))
            {
                VoteResultsSave(hit);
            }
        });

        text.text = voteResult;
        text2.text = voteResult2;
        text3.text = voteResult3;
    }

    void VoteResultsSave(string name)
    {
        voteResult = voteResult + "\n" + name + ": " + cadHits[name].ToString();//GT captured               
        voteResult2 = voteResult2 + "\n" + name + ": " + PercentageCount(rHits[name], cadHits[name]).ToString("F2") + "%"; // Percentage
        //voteResult3 = voteResult3 + "\n" + name + ": " + distance.ToString();//PC captured
        voteResult3 = voteResult3 + "\n" + name + ": " + rHits[name].ToString();//PC captured

        if (PercentageCount(rHits[name], cadHits[name]) > PercentageDynamic(name))
        {
            GameObject.Find(name).GetComponent<Renderer>().sharedMaterial = material;
        }
        else if (PercentageCount(rHits[name], cadHits[name]) < PercentageDynamic(name))
        {
            GameObject.Find(name).GetComponent<Renderer>().sharedMaterial = defaultMaterial[name];
        }
    }

    double PercentageCount(double part, double gt)
    {
        return part * 100 / gt;
    }

    float PercentageDynamic(string name)
    {
        float size = GameObject.Find(name).GetComponent<MeshFilter>().mesh.bounds.size.sqrMagnitude;
        float p;
        if (size < 2000)
        {
            p = 400 / size;
        }

        else
            p = 0.1f;

        if (!sizePart.ContainsKey(name))
            sizePart.Add(name, p);
        return p * 100;
    }

    public void GenerateSummary()
    {
        enabled = false;
        string filePathName = "meshGTAndPartsSummary.csv";
        string filePath = Application.persistentDataPath + "/" + filePathName;

        StreamWriter csvWriter = new StreamWriter(filePath);
        csvWriter.WriteLine("Part Name,Total Part Number,Total GT Number,Dynamic Percentage");

        foreach (string names in nameList)
        {
            if (cadHits.ContainsKey(names))
                csvWriter.WriteLine(names + "," + rHits[names] + "," + cadHits[names] + "," + sizePart[names]);
        }
        csvWriter.Flush();
        csvWriter.Close();

        var csvContent = File.ReadAllBytes(filePath);
        if (csvContent == null) return;

        var csvFile = new UnityGoogleDrive.Data.File() { Name = filePathName, Content = csvContent };
        GoogleDriveFiles.Create(csvFile).Send();
        enabled = true;
    }
}