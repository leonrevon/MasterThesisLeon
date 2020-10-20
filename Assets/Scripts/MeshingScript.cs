using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MeshingScript : MonoBehaviour
{
    List<GameObject> myChildObjects;
    public GameObject gameObject;    
    List<string> nameList = new List<string>();
    Dictionary<string, int> meshVote = new Dictionary<string, int>();   
    
    public Text text;
    public Material material;
    string voteResult = "";

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
            // do something with entry.Value or entry.Key
        }

    }

    void CheckVotes(string name)
    {
        int val = meshVote[name];
        if(val > 100)
        {
            GameObject.Find(name).GetComponent<Material>().SetColor("_Color", Color.green);
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
        }
    }

    void MeshCheckVote(string name)
    {
        if (!meshVote.ContainsKey(name))
        {
            meshVote.Add(name, 0);
        }

        meshVote[name]++;
    }

    void VoteResultPrint(string name)
    {
        if (meshVote.ContainsKey(name))
        {
            voteResult = voteResult + "\n" + name + ": " + meshVote[name].ToString();

            text.text = voteResult;                                    
        }
    }
}