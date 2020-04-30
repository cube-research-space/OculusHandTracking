using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeInteraction : MonoBehaviour
{
    private OVRGrabbableExt _OVRGrabbable;

    // Start is called before the first frame update
    void Start()
    {
        _OVRGrabbable = GetComponent<OVRGrabbableExt>();
        _OVRGrabbable.allowOffhandGrab = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if hands or controllers are in use
        if (other.CompareTag("handLeft") || other.CompareTag("handRight") || other.CompareTag("controllerLeft") || other.CompareTag("controllerRight"))
        {
            _OVRGrabbable.allowOffhandGrab = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if hands or controllers are in use
        if (other.CompareTag("handLeft") || other.CompareTag("handRight") || other.CompareTag("controllerLeft") || other.CompareTag("controllerRight"))
        {
            _OVRGrabbable.allowOffhandGrab = false;
        }
    }
}
