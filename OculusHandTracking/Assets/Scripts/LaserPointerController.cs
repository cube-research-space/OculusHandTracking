using UnityEngine;

/// <summary>
/// This class is responsible for controlling the Laser Pointer tool, generating the "laser-beam" and hit point objects, as well
/// as syncing these objects over the network.
/// </summary>
public class LaserPointerController : MonoBehaviour
{
    // Public Attributes
    public GameObject laserBeam;
    public GameObject laserHitPoint;
    public LayerMask validLaserHitLayer;
    //public AudioSource buttonClick;

    // Private Attributes
    private const float LASER_CENTRE_POINT = 1.5f;          // Half the length of the beam (in z-axis), update if this changes
    private OVRGrabbableExt _OVRGrabbable;
    private bool _LaserOn;
    private Vector3 _LaserLength;
    private OVRHand _Hand;

    #region Unity Methods
    private void Start()
    {
        laserBeam.SetActive(false);
        laserHitPoint.SetActive(false);
        _OVRGrabbable = GetComponent<OVRGrabbableExt>();
        _LaserLength = laserBeam.transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        if (_OVRGrabbable.isGrabbed)
        {
            // Show and update laser beam
            //if (OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger) || (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger)))
            if (_Hand.GetFingerPinchStrength(OVRHand.HandFinger.Index) > 0.3f)      // Trigger laser beam when index finger is pinched
            {
                // Turn on laser
                if (!laserBeam.activeSelf)
                {
                    _LaserOn = true;
                    ActivateLaserBeam(_LaserOn);
                }

                // Shoot RayCast and check if hit object (only applies to valid Layer)
                if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out RaycastHit hit, Mathf.Infinity, validLaserHitLayer))
                {
                    // Position and scale the laser
                    laserBeam.transform.position = Vector3.Lerp(transform.position, hit.point, 0.5f);
                    laserBeam.transform.localScale = new Vector3(laserBeam.transform.localScale.x, laserBeam.transform.localScale.y, hit.distance);

                    // Position and show the laser hit point
                    laserHitPoint.transform.position = hit.point;
                    laserHitPoint.SetActive(true);
                }
                else
                {
                    laserHitPoint.SetActive(false);
                    laserBeam.transform.localScale = _LaserLength;
                    laserBeam.transform.localPosition = Vector3.zero;
                    laserBeam.transform.Translate(Vector3.forward * LASER_CENTRE_POINT);
                }
            }
            else
            {
                // Turn off beam when trigger button is released
                if (laserBeam.activeSelf)
                {
                    DeactivateLaser();
                }
            }
        }
        else
        {
            // Turn off beam when player releases laser pointer tool
            if (laserBeam.activeSelf)
            {
                DeactivateLaser();
            }
        }
    }
    #endregion

    #region Laser Methods
    private void DeactivateLaser()
    {
        _LaserOn = false;
        laserHitPoint.SetActive(false);
        laserBeam.SetActive(false);
    }

    /// <summary>
    /// Toggles the laser beam on / off
    /// </summary>
    /// <param name="activate"></param>
    private void ActivateLaserBeam(bool activate)
    {
        laserBeam.SetActive(activate);
    }
    #endregion

    #region OnTrigger Events
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("handLeft") || other.CompareTag("handRight") || other.CompareTag("controllerLeft") || other.CompareTag("controllerRight"))
        {
            _OVRGrabbable.allowOffhandGrab = true;

            if (other.CompareTag("handLeft") || other.CompareTag("handRight"))
            {
                _Hand = other.gameObject.GetComponentInParent<OVRHand>();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("handLeft") || other.CompareTag("handRight") || other.CompareTag("controllerLeft") || other.CompareTag("controllerRight"))
        {
            _OVRGrabbable.allowOffhandGrab = false;
        }
    }
    #endregion
}