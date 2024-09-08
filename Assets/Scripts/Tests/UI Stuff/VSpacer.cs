using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class VSpacer : MonoBehaviour
{
    public Transform[] ts;
    public float offset;
    public float spacing;
    public bool children;


    void Update()
    {
        if (children)
        {
            if (ts == null || ts.Length != transform.childCount) ts = new Transform[transform.childCount];
            for (int i = 0; i < ts.Length; i ++)
            {
                ts[i] = transform.GetChild(i);
            }
        }
        if (ts == null) return;
        for (int i = 0; i < ts.Length; i ++)
        {
            ts[i].transform.localPosition = Vector3.up * (offset + spacing * i) + Vector3.right * ts[i].localPosition.x;
        }
    }
}
