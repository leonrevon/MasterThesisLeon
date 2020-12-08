using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TestingScript : MonoBehaviour
{
    private List<GameObject> myChildObjects;
    public GameObject gameObject;

    // Start is called before the first frame update
    void Start()
    {
        myChildObjects = gameObject.GetComponentsInChildren<Transform>().ToList().Select(x => x.gameObject).ToList();
        myChildObjects.ForEach(myChildObject =>
        {
            string name = myChildObject.name;
            float size = GameObject.Find(name).GetComponent<MeshFilter>().mesh.bounds.size.sqrMagnitude;
            Debug.Log(name + ": " + size);
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
