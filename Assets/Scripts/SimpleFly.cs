using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleFly : MonoBehaviour
{
    public Rigidbody rb;
    public float speed = 20;

    // Update is called once per frame
    void Update()
    {
        rb.velocity = transform.forward * speed;
    }
}
