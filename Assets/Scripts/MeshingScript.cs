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
    Dictionary<string, int> meshVote = new Dictionary<string, int>();
    Dictionary<string, int> gtNumber = new Dictionary<string, int>();
    Dictionary<string, float> sizePart = new Dictionary<string, float>();
    Dictionary<string, Material> defaultMaterial = new Dictionary<string, Material>();
    Dictionary<string, int> notVisibleDictionary = new Dictionary<string, int>();

    public Text text;
    public Text text2;
    public Text text3;
    public Text text4;

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
        foreach (string name in nameList)
        {
            VoteResultPrint(name);
        }
        voteResult = "";
        voteResult2 = "";
        voteResult3 = "";

    }

    void RayCastMethod()
    {

        for (int k = 0; k < 100; k++)
        {
            //Ray ray = Camera.main.transform.(new Vector3(Random.value, Random.value, 100));
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(Random.value, Random.value, 0));
            RaycastHit[] hits;
            hits = Physics.RaycastAll(ray);
            int meshIndice = -1;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.CompareTag("Mesh"))
                {
                    meshIndice = i;
                    if (hits.Length <= i + 1) continue;

                    string classifiedCadHit = null;

                    for (int x = meshIndice + 1; x < hits.Length; x++)
                    {
                        var dis = Vector3.Distance(hits[x].point, hits[meshIndice].point);

                        if (dis < distanceThreshold)
                        {
                            classifiedCadHit = hits[x].collider.name;
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
                //no r hit
            }
            if (meshIndice < 0)
            {
                hits.ToList().ForEach(hit =>
                {
                    if (!rHits.ContainsKey(hit.collider.name))
                    {
                        rHits.Add(hit.collider.name, 0);
                        cadHits.Add(hit.collider.name, 0);
                    }                    
                    cadHits[hit.collider.name]++;
                });
            }
        }



        //for (int k = -2; k < 2; k++)
        //{
        //    for (int j = -2; j < 2; j++)
        //    {
        //        float screenX = 0 + j;
        //        float screenY = 0 + k;
        //        Vector3 forward = Camera.main.transform.TransformDirection(screenX, screenY, 100);

        //        RaycastHit[] hits;
        //        hits = Physics.RaycastAll(Camera.main.transform.position, forward);

        //        for (int i = 0; i < hits.Length; i++)
        //        {
        //            if (hits[i].collider.CompareTag("Mesh"))
        //            {

        //                Vector3 meshPoints = hits[i].point;

        //                for (int x = 0; x < hits.Length; x++)
        //                {
        //                    var dis = Vector3.Distance(hits[x].point, meshPoints);

        //                    if (dis < 0.01)
        //                    {
        //                        MeshCheckVote(hits[x].collider.name);
        //                    }
        //                }

        //                for (int y = i; y >= 0; y--)
        //                {
        //                    GTCheckVote(hits[y].collider.name);
        //                }

        //            }

        //        }
        //    }
        //}
    }


    //    float closestDistance = 1;
    //    Collider closestCollider = null;


    //                        for (int x = 0; x< 200; x++)
    //                        {
    //                            Vector3 meshPoint = hits[i].point;
    //    Vector3 direction = Random.onUnitSphere;
    //    RaycastHit[] meshHits;
    //    meshHits = Physics.RaycastAll(meshPoint, direction);

    //                            for (int y = 0; y<meshHits.Length; y++)
    //                            {
    //                                var dis = Vector3.Distance(meshPoint, meshHits[y].point);
    //                                if (dis<closestDistance)
    //                                {
    //                                    closestCollider = meshHits[y].collider;
    //                                    closestDistance = dis;
    //                                }
    //                            }
    //                        }
    //                        MeshCheckVote(closestCollider.name);
    //GTCheckVote(closestCollider.name);

    //                        for (int z = i; z <= 0; z--)
    //                        {
    //                            GTCheckVote(hits[z].collider.name);
    //                        }
    //void VisibleCheckVote(string name)
    //{
    //    if (!notVisibleDictionary.ContainsKey(name))
    //        notVisibleDictionary.Add(name, 0);
    //    notVisibleDictionary[name]++;

    //}
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
        if (cadHits.ContainsKey(name))
        {
            if (!rHits.ContainsKey(name))
                rHits.Add(name, 0);
            
            VoteResultsSave(name);           

            if (PercentageCount(rHits[name], cadHits[name]) > PercentageDynamic(name))
            {
                GameObject.Find(name).GetComponent<Renderer>().sharedMaterial = material;

            }
            else if (PercentageCount(rHits[name], cadHits[name]) < PercentageDynamic(name))
            {
                GameObject.Find(name).GetComponent<Renderer>().sharedMaterial = defaultMaterial[name];

            }

            //else if (PercentageCount(notVisibleDictionary[name], gtNumber[name]) > PercentageDynamic(name))
            //{
            //    voteResult4 = voteResult4 + "\n" + name;

            //}


            text.text = voteResult;
            text2.text = voteResult2;
            text3.text = voteResult3;
            //text4.text = voteResult4;
        }

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
        csvWriter.WriteLine("Part Name,Total Part Number,Total GT Number,Dynamic Percentage");

        foreach (string names in nameList)
        {
            if (gtNumber.ContainsKey(names))
                csvWriter.WriteLine(names + "," + meshVote[names] + "," + gtNumber[names] + "," + sizePart[names]);
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