using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hand : MonoBehaviour {
	public enum State
	{
		EMPTY,
		TOUCHING,
		HOLDING
	};

	float delay = 500F;
	WebSocket w = new WebSocket(new System.Uri("ws://localhost:8080"));

	float minX = 0.3F;
	float maxX = 1.1F;
	float minY = -0.88F;
	float maxY = 0.0F;

	public OVRInput.Controller Controller = OVRInput.Controller.LTouch;
	public State mHandState = State.EMPTY;
	public Rigidbody AttachPoint = null;
	public bool IgnoreContactPoint = false;
	private Rigidbody mHeldObject;
	private FixedJoint mTempJoint;
	private Vector3 mOldVelocity;

	bool received = false;

	IEnumerator startSocket() {
		yield return StartCoroutine(w.Connect());
	}

	// Use this for initialization
	void Start () {
		if (AttachPoint == null)
		{
			AttachPoint = GetComponent<Rigidbody>();
		}
		StartCoroutine(startSocket ());
	}

	// Update is called once per frame
	void Update () {
		switch (mHandState)
		{
		case State.TOUCHING:
			if (mTempJoint == null && OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, Controller) >= 0.5f)
			{
				mHeldObject.velocity = Vector3.zero;
				mTempJoint = mHeldObject.gameObject.AddComponent<FixedJoint>();
				mTempJoint.connectedBody = AttachPoint;
				mHandState = State.HOLDING;
			}
			break;
		case State.HOLDING:
			if (mTempJoint != null && OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, Controller) < 0.5f)
			{
				Object.DestroyImmediate(mTempJoint);
				mTempJoint = null;
				throwObject();
				mHandState = State.EMPTY;
			}
			mOldVelocity = OVRInput.GetLocalControllerAngularVelocity(Controller);
			break;
		}

		if (OVRInput.GetDown(OVRInput.Button.One)) {
			minY = OVRInput.GetLocalControllerPosition (OVRInput.Controller.RTouch).y;
			maxX = OVRInput.GetLocalControllerPosition (OVRInput.Controller.RTouch).z;
		}

		if (OVRInput.GetDown (OVRInput.Button.Two)) {
			maxY = OVRInput.GetLocalControllerPosition (OVRInput.Controller.RTouch).y;
			minX = OVRInput.GetLocalControllerPosition (OVRInput.Controller.RTouch).z;
		}

		updateCurrentPosition();
	}

	void OnTriggerEnter(Collider collider)
	{
		if (mHandState == State.EMPTY && OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, Controller) < 0.5f)
		{
			GameObject temp = collider.gameObject;
			if (temp != null && temp.layer == LayerMask.NameToLayer("grabbable") && temp.GetComponent<Rigidbody>() != null)
			{
				mHeldObject = temp.GetComponent<Rigidbody>();
				mHandState = State.TOUCHING;
			}
		}
	}

	void OnTriggerExit(Collider collider)
	{
		if (mHandState != State.HOLDING)
		{
			if (collider.gameObject.layer == LayerMask.NameToLayer("grabbable"))
			{
				mHeldObject = null;
				mHandState = State.EMPTY;
			}
		}
	}

	void updateCurrentPosition() {

		Vector3 pos = OVRInput.GetLocalControllerPosition (OVRInput.Controller.RTouch);
		Vector2 newPos = new Vector2 ();
		newPos.x = (Mathf.Clamp(pos.z, minX, maxX) - minX) * (0.43F / Mathf.Abs (maxX - minX));
		newPos.y = (Mathf.Clamp(pos.y, minY, maxY) - minY) * (0.43F / Mathf.Abs (maxY - minY));

		Debug.Log ("Raw: " + pos + " Scaled: " + newPos);

		if (pos.z < minX) {
			Debug.Log ("TOO FAR BACK");
		}
		if (pos.z > maxX) {
			Debug.Log ("TOO FAR FORWARD");
		}
		if (pos.y < minY) {
			Debug.Log ("TOO FAR DOWN");
		}
		if (pos.y > maxY) {
			Debug.Log ("TOO FAR UP");
		}

		// Send across socket connection
		if (delay < 0) {
			w.SendString(newPos.x + "," + newPos.y);
			delay = 500F;
		}

		delay -= Time.deltaTime * 1000;
	}

	private void throwObject()
	{
		mHeldObject.velocity = OVRInput.GetLocalControllerVelocity(Controller);
		if (mOldVelocity != null)
		{
			mHeldObject.angularVelocity = OVRInput.GetLocalControllerAngularVelocity(Controller);
		}
		mHeldObject.maxAngularVelocity = mHeldObject.angularVelocity.magnitude;
	}
}