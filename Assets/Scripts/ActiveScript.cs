using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ActiveScript : MonoBehaviour
{
    public GameObject gameObject;

    private void Start()
    {
        gameObject = GetComponent<GameObject>();
    }
    void Update()
    {
        
    }

    public void GameObjectVisible(bool newValue)
    {
        gameObject.SetActive(newValue);
    }
}
