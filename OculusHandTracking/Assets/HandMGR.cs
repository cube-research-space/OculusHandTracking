using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandMGR : MonoBehaviour
{
    public OVRHand leftHand;
    public OVRHand rightHand;
    public GameObject leftController;
    public GameObject rightController;

    // Update is called once per frame
    void Update()
    {
        CheckForHands();
    }

    private void CheckForHands()
    {
        if (leftHand.IsTracked || rightHand.IsTracked)
        {
            // Disable controllers if hands are tracked
            leftController.SetActive(false);
            rightController.SetActive(false);
        }
        else
        {
            // Enable controllers if hands are not tracked / controllers are activated
            leftController.SetActive(true);
            rightController.SetActive(true);
        }
    }
}
