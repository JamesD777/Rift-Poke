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

	public Vector2 startPosition;
	public Vector2 currentPosition = new Vector2();
	float delay = 500F;

	WebSocket w = new WebSocket(new System.Uri("ws://localhost:8080"));

	public OVRInput.Controller Controller = OVRInput.Controller.LTouch;
	public State mHandState = State.EMPTY;
	public Rigidbody AttachPoint = null;
	public bool IgnoreContactPoint = false;
	private Rigidbody mHeldObject;
	private FixedJoint mTempJoint;
	private Vector3 mOldVelocity;

	IEnumerator startSocket() {
		yield return StartCoroutine(w.Connect());
		while (true)
		{
			string message = w.RecvString();
			if (message == "arduino") {
				Debug.Log (message);
				startPosition = currentPosition;
				currentPosition = new Vector2 (0, 0);
			}
			yield return 0;
		}
		w.Close ();
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

	Vector2 getStartPosition() {
		Vector3 pos = OVRInput.GetLocalControllerPosition (OVRInput.Controller.RTouch);
		return new Vector2 (pos.y, pos.z);
	}

	void updateCurrentPosition() {

		if (startPosition == null) {
			startPosition = getStartPosition ();
			return;
		}

		Vector3 pos = OVRInput.GetLocalControllerPosition (OVRInput.Controller.RTouch);

		// Send across socket connection
		currentPosition.x = Mathf.Abs(pos.z - startPosition.x);
		currentPosition.y = Mathf.Abs(pos.y - startPosition.y);

		if (delay < 0) {
			w.SendString(currentPosition.x + "," + currentPosition.y);
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