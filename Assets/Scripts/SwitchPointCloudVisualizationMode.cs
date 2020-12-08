using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityGoogleDrive;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARPointCloudManager))]


public class SwitchPointCloudVisualizationMode : MonoBehaviour
{
  
    void OnEnable()
    {        
        GetComponent<ARPointCloudManager>().pointCloudsChanged += OnPointCloudsChanged;
    }
    /// <summary>
    /// Added Lines from my own
    /// </summary>        
    List<Vector3> addedPoints = new List<Vector3>();
    List<Vector3> addedPointsGT = new List<Vector3>();
    List<string> colliderHitName = new List<string>();
    List<string> gtHitName = new List<string>();

    public GameObject cube;
    public Text text;
    public Text text2;
    public Text text3;
    public Text text4;
    public GameObject gameObject;
    public Material material;
    bool effectsOn;  
    
    string voteResult = "";
    string voteResult2 = "";
    string voteResult3 = "";
    

    Dictionary<string, Vector3> vector = new Dictionary<string, Vector3>();
    Dictionary<string, List<Vector3>> valueDictionary = new Dictionary<string, List<Vector3>>();
    Dictionary<string, int> voteCalculation = new Dictionary<string, int>();
    Dictionary<string, GameObject> availableParts = new Dictionary<string, GameObject>();
    Dictionary<string, Material> defaultMaterial = new Dictionary<string, Material>();
    Dictionary<string, int> GTPart = new Dictionary<string, int>();           

    List<GameObject> myChildObjects;
    List<string> nameList = new List<string>();

    StringBuilder m_StringBuilder = new StringBuilder();
    

    void OnPointCloudsChanged(ARPointCloudChangedEventArgs eventArgs)
    {
        m_StringBuilder.Clear();
        foreach (var pointCloud in eventArgs.updated)
        {
            m_StringBuilder.Append($"\n{pointCloud.trackableId}: ");

            var visualizer = pointCloud.GetComponent<ARAllPointCloudPointsParticleVisualizer>();
            if (visualizer)
            {
                m_StringBuilder.Append($"{visualizer.pointCloudPosition.Count} total points");
            }

        }        

        foreach (var pointCloud in eventArgs.added)
        {
            var visualizer = pointCloud.GetComponent<ARAllPointCloudPointsParticleVisualizer>();
            addedPoints = visualizer.pointCloudPosition;
            colliderHitName = visualizer.colliderHitName;
            gtHitName = visualizer.gtHitName;                 
        }        
        
    }

    public void CompilerData()
    {
        foreach(var name in nameList) //Generate GT data
        {
            List<Vector3> GTVectorList = new List<Vector3>(valueDictionary[name]);
            GenerateSheets(GTVectorList, name);
        }


        GenerateSheets(addedPoints, "pointCloudRaw");
        GenerateSheets(addedPointsGT, "pointCloudGT");
    }

    public void GenerateSheets(List<Vector3> addedPoints, string pathname)
    {

        string pointCloudPathName = pathname + ".ply";

        string pointCloudPath = Application.persistentDataPath + "/" + pointCloudPathName;        

        StreamWriter plyWriter = new StreamWriter(pointCloudPath);
        plyWriter.WriteLine("ply");
        plyWriter.WriteLine("format ascii 1.0");
        plyWriter.WriteLine("element vertex " + addedPoints.Count);
        plyWriter.WriteLine("property float x");
        plyWriter.WriteLine("property float y");
        plyWriter.WriteLine("property float z");
        plyWriter.WriteLine("end_header");

        for (int i = 0; i < addedPoints.Count; i++)
        {
            plyWriter.WriteLine(addedPoints[i].x + " " + addedPoints[i].y + " " + addedPoints[i].z);
        }

        plyWriter.Flush();
        plyWriter.Close();

        var plyContent = File.ReadAllBytes(pointCloudPath);
        if (plyContent == null) return;
        
        var plyFile = new UnityGoogleDrive.Data.File() { Name = pointCloudPathName, Content = plyContent };        
        GoogleDriveFiles.Create(plyFile).Send();
    }

    public void GenerateSummary()
    {

        enabled = false;
        string filePathName = "GTAndPartsSummary.csv";

        string filePath = Application.persistentDataPath + "/" + filePathName;

        StreamWriter csvWriter = new StreamWriter(filePath);
        csvWriter.WriteLine("Part Name,Total Part Number,Total GT Number");


        foreach (string name in colliderHitName)
        {
            CalculateVoteCount(name);
        }

        foreach (string name in gtHitName)
        {
            CalculateGTCount(name);
        }

        foreach (var names in nameList)
        {
            if (voteCalculation.ContainsKey(names))
                csvWriter.WriteLine(names + "," + voteCalculation[names] + "," + GTPart[names]);
        }

        csvWriter.WriteLine("Non Available Parts");
        foreach(var names in nameList)
        {
            csvWriter.WriteLine(names);
        }

        csvWriter.Flush();
        csvWriter.Close();

        var csvContent = File.ReadAllBytes(filePath);
        if (csvContent == null) return;

        var csvFile = new UnityGoogleDrive.Data.File() { Name = filePathName, Content = csvContent };
        GoogleDriveFiles.Create(csvFile).Send();
        enabled = true;
    }

    void Start()
    {
        myChildObjects = gameObject.GetComponentsInChildren<Transform>().ToList().Select(x => x.gameObject).ToList();
        myChildObjects.ForEach(myChildObject =>
        {
            string name = myChildObject.name;
            List<Vector3> GTUpdate = new List<Vector3>();
            nameList.Add(myChildObject.name);
            vector.Add(name, Vector3.zero);                        
            valueDictionary.Add(name, GTUpdate);
            availableParts.Add(name, myChildObject);
            defaultMaterial.Add(name, myChildObject.GetComponent<Renderer>().material);


        });       
    }

    void Update()
    {                         

        foreach (string name in colliderHitName)
        {
            CalculateVoteCount(name);
        }

        foreach (string name in gtHitName)
        {
            CalculateGTCount(name);
        }

        foreach (string name in nameList)
        {            
            VoteResultPrint(name);

        }
                        
        voteResult = "";
        voteResult2 = "";
        voteResult3 = "";
        //voteResult4 = "";

        voteCalculation.Clear();
        GTPart.Clear();        
    }

    
    void VoteResultPrint(string name)
    {       
        if (voteCalculation.ContainsKey(name))
        {            
            voteResult = voteResult + "\n" + name + ": " + GTPart[name].ToString(); //GT captured
            voteResult2 = voteResult2 + "\n" + name + ": " + PercentageCount(voteCalculation[name] , GTPart[name]).ToString("F2")+ "%"; 
            voteResult3 = voteResult3 + "\n" + name + ": " + voteCalculation[name].ToString(); //PC Captured

            text.text = voteResult;
            text2.text = voteResult2;
            text3.text = voteResult3;
            
            if (effectsOn)
            {
                if (PercentageCount(voteCalculation[name], GTPart[name]) > PercentageDynamic(name))
                {
                    GameObject.Find(name).GetComponent<Renderer>().sharedMaterial = material;
                }
                else
                {
                    GameObject.Find(name).GetComponent<Renderer>().sharedMaterial = defaultMaterial[name];
                }
                
            }



        }
    }
    void CalculateVoteCount (string name)
    {        
            if (!voteCalculation.ContainsKey(name))
            {
                voteCalculation.Add(name, 0);
            }
          
            voteCalculation[name]++;                                              
    }

    void CalculateGTCount (string name)
    {
        if (!GTPart.ContainsKey(name))
        {
            GTPart.Add(name, 0);
        }

        GTPart[name]++;
    }

// For Toggle function to enable ACTIVE GT collection
    public void EffectsOn(bool value)
    {
        effectsOn = value;
    }   
  

    float PercentageCount(float part, float gt)
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
        
        return p * 100;
    }

    public void ChangeScene()
    {
        SceneManager.LoadScene("Meshing");
    }
}

