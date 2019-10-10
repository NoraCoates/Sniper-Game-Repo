using UnityEngine;
using System.Collections;
using UnityEngine.Networking;


// Script by IJM: http://answers.unity3d.com/questions/29741/mouse-look-script.html
// Changed to fit standard C# conventions
 
// MouseLook rotates the transform based on the mouse delta.
// Minimum and Maximum values can be used to constrain the possible rotation

//wonky when online
//  view changes to latest player??
 
[AddComponentMenu("Camera-Control/Mouse Look")]
public class OwnMouseLook : NetworkBehaviour {
	 
	public enum RotationAxes { MouseXAndY = 0, MouseX = 1, 	MouseY = 2 };
	public RotationAxes axes = RotationAxes.MouseXAndY;
	public float sensitivityY = 15F;
    public float sensitivityX = 15F;
	 
	public float minimumX;
	public float maximumX;
	 
	public float minimumY;
	public float maximumY;

	float rotationX;
	float rotationY;
	 
	Quaternion originalRotation;

    public Rigidbody m_Rigidbody;
    public Camera Camera;
	 
	void Update()
	{
        if(!isLocalPlayer) {
            Camera.enabled = false;
            return;
        }
		if (axes == RotationAxes.MouseXAndY)
		{
			// Read the mouse input axis
            rotationX += Input.GetAxis("Mouse X") * sensitivityX;
			rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
			 
			rotationX = ClampAngle(rotationX, minimumX, maximumX);
			rotationY = ClampAngle(rotationY, minimumY, maximumY);
			 
			Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
			Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, -Vector3.right);
			 
            Camera.transform.localRotation = originalRotation * yQuaternion;
            m_Rigidbody.transform.localRotation = originalRotation * xQuaternion;
		}
		
	}
	 
	void Start()
	{
        Cursor.lockState = CursorLockMode.Locked;
		originalRotation = transform.localRotation;
	}
	 
	public static float ClampAngle(float angle, float min, float max)
	{
		if (angle < -360F)
			angle += 360F;
		if (angle > 360F)
			angle -= 360F;
		return Mathf.Clamp(angle, min, max);
	}
}