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

    [SerializeField]
    ARAllPointCloudPointsParticleVisualizer.Mode m_Mode = ARAllPointCloudPointsParticleVisualizer.Mode.All;

    public ARAllPointCloudPointsParticleVisualizer.Mode mode
    {
        get => m_Mode;
        set => SetMode(value);
    }       

    public void SwitchVisualizationMode()
    {
        SetMode((ARAllPointCloudPointsParticleVisualizer.Mode)(((int)m_Mode + 1) % 2));
    }

    void OnEnable()
    {
        SetMode(m_Mode);
        GetComponent<ARPointCloudManager>().pointCloudsChanged += OnPointCloudsChanged;
    }
    /// <summary>
    /// Added Lines from my own
    /// </summary>    
    string pointCloudPath;   
    string pointCloudPathName;
    List<Vector3> addedPoints = new List<Vector3>();
    List<Vector3> addedPointsGT = new List<Vector3>();
    List<string> colliderHitName = new List<string>();

    public GameObject cube;
    public Text text;
    public Text debug;
    public GameObject gameObject;
    bool effectsOn;
    bool effectsOnPerc;
    string voteResult = "";

    Dictionary<string, Vector3> vector = new Dictionary<string, Vector3>();
    Dictionary<string, List<Vector3>> valueDictionary = new Dictionary<string, List<Vector3>>();
    Dictionary<string, int> voteCalculation = new Dictionary<string, int>();
    Dictionary<string, GameObject> availableParts = new Dictionary<string, GameObject>();

    List<GameObject> myChildObjects;
    List<string> nameList = new List<string>();


    StringBuilder m_StringBuilder = new StringBuilder();
    

    void OnPointCloudsChanged(ARPointCloudChangedEventArgs eventArgs)
    {
        m_StringBuilder.Clear();
        foreach (var pointCloud in eventArgs.updated)
        {
            m_StringBuilder.Append($"\n{pointCloud.trackableId}: ");
            if (m_Mode == ARAllPointCloudPointsParticleVisualizer.Mode.CurrentFrame)
            {
                if (pointCloud.positions.HasValue)
                {
                    m_StringBuilder.Append($"{pointCloud.positions.Value.Length}");
                }
                else
                {
                    m_StringBuilder.Append("0");
                }

                m_StringBuilder.Append(" points in current frame.");
            }
            else
            {
                var visualizer = pointCloud.GetComponent<ARAllPointCloudPointsParticleVisualizer>();
                if (visualizer)
                {
                    m_StringBuilder.Append($"{visualizer.pointCloudPosition.Count} total points");
                }

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

            
        }
    }


    void SetMode(ARAllPointCloudPointsParticleVisualizer.Mode mode)
    {
        m_Mode = mode;
        if (toggleButton)
        {
            var text = toggleButton.GetComponentInChildren<Text>();
            switch (mode)
            {
                case ARAllPointCloudPointsParticleVisualizer.Mode.All:
                    text.text = "All";
                    break;
                case ARAllPointCloudPointsParticleVisualizer.Mode.CurrentFrame:
                    text.text = "Current Frame";
                    break;
            }
        }

        var manager = GetComponent<ARPointCloudManager>();
        foreach (var pointCloud in manager.trackables)
        {
            var visualizer = pointCloud.GetComponent<ARAllPointCloudPointsParticleVisualizer>();
            if (visualizer)
            {
                visualizer.mode = mode;
            }
        }
    }

    public void CompilerData()
    {
        foreach (var name in nameList) //Generate GT data
        {
            List<Vector3> GTVectorList = new List<Vector3>(valueDictionary[name]);            
            GenerateSheets(GTVectorList, name);
        }

        GenerateSheets(addedPoints, "pointCloudRaw");
        GenerateSheets(addedPointsGT, "pointCloudGT");
    }

    public void GenerateSheets(List<Vector3> addedPoints, string pathname)
    {
        pointCloudPathName = pathname + ".ply";

        pointCloudPath = Application.persistentDataPath + "/" + pointCloudPathName;        

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
            
        });

        voteResult = "";

    }

    void Update()
    {        
        if (effectsOn)
        {
            RaycastHit hit;
            //for (int i = 0; i < 1000; i++)

            
            Vector3 forward = Camera.main.transform.TransformDirection(Random.Range(-10, 10), Random.Range(-10, 10), 100);

            if (Physics.Raycast(Camera.main.transform.position, forward, out hit))
            {                                                                       
                    GTUpdate(hit.collider.name, hit.point);
                    addedPointsGT.Add(hit.point);                    
            }

        }

        foreach (string name in colliderHitName)
        {
            CalculateVoteCount(name);
        }

        foreach (string name in nameList)
        {
            VoteResultPrint(name);
            if (effectsOnPerc)
            {
                CheckStatus(name);

            }
            else
                continue;
        }
                        
        voteResult = "";      
        voteCalculation.Clear();
 
    }

    
    void VoteResultPrint(string name)
    {
        if (voteCalculation.ContainsKey(name))
        {
            voteResult = voteResult + "\n" + name + ": " + voteCalculation[name].ToString();

            text.text = voteResult;

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

// For Toggle function to enable ACTIVE GT collection
    public void EffectsOn(bool value)
    {
        effectsOn = value;
    }

    public void EffectsOnPerc(bool value)
    {
        effectsOnPerc = value;
    }

    void GTUpdate(string name, Vector3 item)
    {
        List<Vector3> tempVectorList = new List<Vector3>(valueDictionary[name]);        
        valueDictionary.Remove(name);
        tempVectorList.Add(item);
        valueDictionary.Add(name, tempVectorList);
    }

    void CheckStatus(string name)
    {
        List<Vector3> GTVector = new List<Vector3>(valueDictionary[name]);
        float partNumber = voteCalculation[name];
        //GTVector = valueDictionary[name];  // Vector List of that part of points gather
        //partNumber = voteCalculation[name];  //Number of points gather for that part


        // If GT part available
        // check PC Raw


        debug.text = "first part";
        if (GTVector.Count() != 0)
        {
            debug.text = "second part";
            if (PercentageCount(GTVector, partNumber) < 0.2)
            {
                debug.text = "third part";
                Destroy(availableParts[name]);
                
            }
        }
        

    }

    float PercentageCount(List<Vector3> GTdata, float num)
    {
        return num/(GTdata.Count());
    }

    public void ChangeScene()
    {
        SceneManager.LoadScene("Meshing");
    }
}

