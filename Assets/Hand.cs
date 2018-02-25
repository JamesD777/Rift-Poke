using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hand : MonoBehaviour {
    public enum State //possible states of our hand
    {
        EMPTY,
        TOUCHING,
        HOLDING
    };

    public OVRInput.Controller Controller = OVRInput.Controller.LTouch; //signifies which hand the controller represents
    public State mHandState = State.EMPTY; //check to find out the state
    public Rigidbody AttachPoint = null; //point to  determine where on the object we attatch our hand to when we grab it
    public bool IgnoreContactPoint = false; 
    private Rigidbody mHeldObject;
    private FixedJoint mTempJoint;
    private Vector3 mOldVelocity;

    // Use this for initialization
    void Start () {
		if (AttachPoint == null)
        {
            AttachPoint = GetComponent<Rigidbody>();
        }
	}
	
	void Update () { //call once per frame to check hand state and update things like the joint/velocity
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
                    mHandState = State.EMPTY;
                }
                mOldVelocity = OVRInput.GetLocalControllerAngularVelocity(Controller);
                break;
        }
    }

    void OnTriggerEnter(Collider collider) //checks if we are already holding something and if we can hold the item we are attempting to grab.
    {
        if (mHandState == State.EMPTY && OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, Controller) < 0.5f) //if valid, set it  to be held and touching
        {
            GameObject temp = collider.gameObject;
            if (temp != null && temp.layer == LayerMask.NameToLayer("grabbable") && temp.GetComponent<Rigidbody>() != null)
            {
                mHeldObject = temp.GetComponent<Rigidbody>();
                mHandState = State.TOUCHING;
            }
        }
    }

    void OnTriggerExit(Collider collider) //if holding something, drop and set hand state to empty
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
}
