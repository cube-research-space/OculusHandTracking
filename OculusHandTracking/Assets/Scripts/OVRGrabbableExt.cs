using UnityEngine;
using System.Collections;

public class OVRGrabbableExt : OVRGrabbable
{
    /// <summary>
	/// If true, the object can currently be grabbed.
	/// </summary>
    public new bool allowOffhandGrab
    {
        get { return m_allowOffhandGrab; }
        set { m_allowOffhandGrab = value; }
    }
}

