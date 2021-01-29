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
    public List<string> output = new List<string>();
    List<string> nameList = new List<string>();
    List<string> hitsBefore = new List<string>();

    Dictionary<string, int> cadHits = new Dictionary<string, int>();
    Dictionary<string, int> rHits = new Dictionary<string, int>();
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
        print();
    }

    void RayCastMethod()
    {
        
        for (int k = 0; k < 500; k++)
        {

            Ray ray = Camera.main.ViewportPointToRay(new Vector3(Random.value, Random.value, 0));
            RaycastHit[] hits;
            hits = Physics.RaycastAll(ray);
            int meshIndice = -1;
            for (int i = 0; i < hits.Length; i++)
            {

                if (hits[i].collider.CompareTag("CAD"))
                {

                    meshIndice = i;
                    if (hits.Length <= i + 1) continue;

                    string classifiedCadHit = null;

                    for (int x = 0; x < hits.Length; x++)
                    {
                        if (hits[x].collider.CompareTag("Mesh"))
                        {
                            var dis = Vector3.Distance(hits[x].point, hits[meshIndice].point);
                            if (dis < 0.01)
                            {
                                classifiedCadHit = hits[meshIndice].collider.name;
                            }
                        }

                    }
                    if (classifiedCadHit == null) continue;
                    List<string> hitsBefore = hits.ToList().GetRange(0, meshIndice).ToList().Select(x => x.collider.name).ToList();

                    if (!rHits.ContainsKey(classifiedCadHit))
                    {
                        rHits.Add(classifiedCadHit, 0);
                    }
                    if (!cadHits.ContainsKey(classifiedCadHit))
                    {
                        cadHits.Add(classifiedCadHit, 0);
                    }
                    rHits[classifiedCadHit]++;
                    cadHits[classifiedCadHit]++;
                    hitsBefore.ForEach(hit =>
                    {
                        if (!rHits.ContainsKey(hit))
                        {
                            rHits.Add(hit, 0);
                        }
                        if (!cadHits.ContainsKey(hit))
                        {
                            cadHits.Add(hit, 0);
                        }
                        cadHits[hit]++;
                    });
                }
                
            }
            //if (meshIndice < 0)
            //{
            //    hits.ToList().ForEach(hit =>
            //    {
            //        if (!rHits.ContainsKey(hit.collider.name))
            //        {
            //            rHits.Add(hit.collider.name, 0);
            //            cadHits.Add(hit.collider.name, 0);
            //        }

            //        cadHits[hit.collider.name]++;
            //    });
            //}
        }

    }

    //void MeshCheckVote(string name)
    //{
    //    if (!rHits.ContainsKey(name))
    //        rHits.Add(name, 0);
    //    rHits[name]++;
    //}

    //void GTCheckVote(string name)
    //{
    //    if (!cadHits.ContainsKey(name))
    //        cadHits.Add(name, 0);

    //    cadHits[name]++;
    //}


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
                MaterialPercentageChange(hit);
            }
        });

        //text.text = voteResult;
        //text2.text = voteResult2;
        //text3.text = voteResult3;
    }

    void VoteResultsSave(string name)
    {
        voteResult = voteResult + "\n" + name + ": " + cadHits[name].ToString();//GT captured               
        voteResult2 = voteResult2 + "\n" + name + ": " + PercentageCount(rHits[name], cadHits[name]).ToString("F2") + "%"; // Percentage        
        voteResult3 = voteResult3 + "\n" + name + ": " + rHits[name].ToString();//PC captured

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
        csvWriter.WriteLine("Part Name,Total Part Number,Total GT Number");

        foreach (string names in nameList)
        {
            if (cadHits.ContainsKey(names))
                csvWriter.WriteLine(names + "," + rHits[names] + "," + cadHits[names]);
        }
        csvWriter.Flush();
        csvWriter.Close();

        var csvContent = File.ReadAllBytes(filePath);
        if (csvContent == null) return;

        var csvFile = new UnityGoogleDrive.Data.File() { Name = filePathName, Content = csvContent };
        GoogleDriveFiles.Create(csvFile).Send();
        enabled = true;
    }

    public void print()
    {
        output.Clear();
        rHits.Keys.ToList().ForEach(hit =>
        {
            if (!hit.Contains("Mesh"))
            {
                output.Add(hit + ": " + rHits[hit] + " rHits, " + cadHits[hit] + " cadHits // " + (float)((float)((float)rHits[hit] / (float)cadHits[hit]) * 100.0f) + "%");
                MaterialPercentageChange(hit);
            }

        });

        voteResult = "";
        output.ToList().ForEach(line =>
        {
            voteResult = voteResult + "\n" + line;
        });

        //text.text = voteResult;


    }

    public void MaterialPercentageChange(string name)
    {
        if (PercentageCount(rHits[name], cadHits[name]) > 80)
        {
            GameObject.Find(name).GetComponent<Renderer>().sharedMaterial = material;
        }
        else if (PercentageCount(rHits[name], cadHits[name]) < 80)
        {
            GameObject.Find(name).GetComponent<Renderer>().sharedMaterial = defaultMaterial[name];
        }
    }
}