using System.Collections;
using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;


public class CreateRuntimeModel : MonoBehaviour
{
    public ModelAsset modelAsset;
    private Model runtimeModel;
    // Start is called before the first frame update
    void Start()
    {
        runtimeModel = ModelLoader.Load(modelAsset);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
