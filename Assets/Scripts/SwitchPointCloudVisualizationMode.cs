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

    [SerializeField]
    Button m_ToggleButton;

    public Button toggleButton
    {
        get => m_ToggleButton;
        set => m_ToggleButton = value;
    }

    [SerializeField]
    Text m_Log;

    public Text log
    {
        get => m_Log;
        set => m_Log = value;
    }
   

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
    public GameObject gameObject;
    public Material material;
    bool effectsOn;
    bool updateCheck;
    
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
        if (log)
        {
            log.text = m_StringBuilder.ToString();
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
            //for (int x = -2; x < 2; x++)
            //{
            //    for (int y = -2; y < 2; y++)
            //    {
            //        float screenX = 0 + x;
            //        float screenY = 0 + y;

            //        Vector3 forward = Camera.main.transform.TransformDirection(screenX, screenY, 100);

            //        RaycastHit[] hits;


            //        hits = Physics.RaycastAll(Camera.main.transform.position, forward);
            //        for (int i = 0; i < hits.Length; i++)
            //        {
            //            if (hits[i].collider.CompareTag("CAD"))
            //            {
            //                GTUpdate(hits[i].collider.name, hits[i].point);
            //                addedPointsGT.Add(hits[i].point);
            //            }
            //        }
            //    }
            //}

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
                if (PercentageCount(voteCalculation[name], GTPart[name]) > 10)
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

    void GTUpdate(string name, Vector3 item)
    {
        List<Vector3> tempVectorList = new List<Vector3>(valueDictionary[name]);        
        valueDictionary.Remove(name);
        tempVectorList.Add(item);
        valueDictionary.Add(name, tempVectorList);
    }


    float PercentageCount(float part, float gt)
    {
        return part * 100 / gt;
    }

    public void ChangeScene()
    {
        SceneManager.LoadScene("Meshing");
    }
}

