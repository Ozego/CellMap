using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ColorFitter : MonoBehaviour
{
    [SerializeField] Color target;
    [SerializeField] Color pointer;
    [SerializeField] Vector4 oldDiff;
    void Awake()
    {
        oldDiff = Vector4.one;
    }
    void Update()
    {
        for (int c = 0; c < 3; c++)
        {
            List<dVal> values = new List<dVal>();
            for (int v = 0; v < 128; v++)
            {
                dVal cVal = new dVal();
                cVal.value = pointer[c]+Random.Range(-oldDiff[c]/2f,oldDiff[c]/2f);
                cVal.diff = Mathf.Abs(target[c]-cVal.value);
                values.Add(cVal);
            }
            values.Sort((a,b)=>a.diff.CompareTo(b.diff));
            if(values[0].diff<oldDiff[c])
            {
                oldDiff[c] = values[0].diff;
                pointer[c] = values[0].value;
            }
        }
    }
    class dVal
    {
        public float diff;
        public float value;
    }
}
