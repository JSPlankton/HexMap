using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudControl : MonoBehaviour
{
    public float Duration = 10.0f;
    public AnimationCurve LifeTime = AnimationCurve.Linear(0.0f, 0.0f, 1f, 1.0f);
    public float LiftOffset = 0.0f;
    public float RotateSpeed = 1f;
    public Transform RotateTarget;
    private Vector3 relativeDistance;
    private Material mat;
    // Start is called before the first frame update
    void Start()
    {
        if (RotateTarget != null)
        {
            relativeDistance = transform.position - RotateTarget.position;
        }
        mat = this.GetComponentInChildren<MeshRenderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        if (RotateTarget != null)
        {
            transform.position = RotateTarget.position + relativeDistance;
            transform.RotateAround(RotateTarget.position, Vector3.up, RotateSpeed * Time.deltaTime);
            relativeDistance = transform.position - RotateTarget.position;
        }
        else
        {
            this.transform.RotateAround(Vector3.zero, Vector3.up, RotateSpeed * 0.01f);
        }
        if (mat != null)
        {
            float timeline = ((Time.time + LiftOffset) % Duration) / Duration;
            mat.SetFloat("_Dissolve",LifeTime.Evaluate(timeline));
        }
    }
}
