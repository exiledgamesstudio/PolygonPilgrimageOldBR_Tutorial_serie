using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BRS_ChangeCircle : MonoBehaviour
{
	[Range(0, 360)]
	public int Segments;
	
	[Range(0,5000)]
	public float XRadius;

	//THIS IS NOT USED - SHOULD BE ELIMINATED
	/*
	[Range(0,5000)]
	public float YRadius;
	*/

	[Range(1, 20)]
	public int WallMaxHeight;

	//default to 50%
	[Range(10, 100)]
	public int ZoneRadiusFactor = 50;
	
	[Header("Shrinking Zones")]
	public List<int> ZoneTimes;

	#region Private Members
	private bool Shrinking;  // this can be set to PUBLIC in order to troubleshoot.  It will show a checkbox in the Inspector
	[SerializeField]
	private int countdownPrecall = 10;  //this MIGHT be public, but it should not need to be changed
	private int timeToShrink = 30; //seconds
	[SerializeField]
	private int count = 0;
	[SerializeField]
	private bool newCenterObtained = false;
	[SerializeField] 
	private Vector3 centerPoint = new Vector3(1200, -100, 1200);
	[SerializeField]
	private float distanceToMoveCenter;
	private WorldCircle circle;
	private LineRenderer renderer;
	private GameObject ZoneWall;
	private float [] radii = new float[2];
	private float shrinkRadius;
	private int zoneRadiusIndex = 0;
	private int zoneTimesIndex = 0;
	private float timePassed;
	#endregion

	void Start ()
	{
		renderer = gameObject.GetComponent<LineRenderer>();
		//radii[0] = XRadius;  radii[1] = YRadius;
		circle = new WorldCircle(ref renderer, Segments, XRadius, XRadius);
		ZoneWall = GameObject.FindGameObjectWithTag ("ZoneWall");
		
		timePassed = Time.deltaTime;
	}


	void Update ()
	{
		Debug.Log("time passed " + Time.realtimeSinceStartup);
		ZoneWall.transform.localScale = new Vector3 ((XRadius * 0.01f), WallMaxHeight, (XRadius * 0.01f));
		
		if(Shrinking)
		{
			// we need a new center point (that is within the bounds of the current zone)
			if (!newCenterObtained)
			{
				Debug.Log("current center " + centerPoint);
				centerPoint = NewCenterPoint(transform.position, XRadius, shrinkRadius);
				distanceToMoveCenter = Vector3.Distance(transform.position, centerPoint); //this is used in the Lerp (below)
				newCenterObtained = (centerPoint != new Vector3(0, -100, 0));
			}
			
			Debug.Log("New Center Point is " + centerPoint);
			
			// move the center point, over time
			transform.position = Vector3.Lerp(transform.position, centerPoint, (distanceToMoveCenter / timeToShrink) * Time.deltaTime );
			
			// shrink the zone diameter, over time
			XRadius = Mathf.MoveTowards(XRadius, shrinkRadius, (shrinkRadius / timeToShrink) * Time.deltaTime);
			circle.Draw(Segments, XRadius, XRadius);
			
			// MoveTowards will continue infinitum, so we must test that we have gotten close enough to be DONE
			if (1 > (XRadius - shrinkRadius))
			{
				timePassed = Time.deltaTime;
				Shrinking = false;
				newCenterObtained = false;
			}
		}
		else
		{
			timePassed += Time.deltaTime; // increment clock time
		}
		
		// have we passed the next threshold for time delay?
		if (((int) timePassed)  > ZoneTimes[zoneTimesIndex])
		{
			shrinkRadius = ShrinkCircle((float)(XRadius * (ZoneRadiusFactor * 0.01)))[1];  //use the ZoneRadiusFactor as a percentage
			Shrinking = true;
			timePassed = Time.deltaTime;  //reset the time so other operations are halted.
			NextZoneTime();
		}

		// COUNT DOWN
		// we need to begin counting down
		if (timePassed > (ZoneTimes[zoneTimesIndex] - countdownPrecall))
		{
			if (ZoneTimes[zoneTimesIndex] - (int) timePassed != count)
			{
				// this ensures our value never falls below zero
				count = Mathf.Clamp(ZoneTimes[zoneTimesIndex] - (int) timePassed, 1, 1000);
				
				//FILL IN APPROPRIATE UI CALLS HERE FOR THE COUNTDOWN
				Debug.Log("Shrinking in " + count + " seconds.");
			}
		}
	}

	// ***********************************
	// PRIVATE (helper) FUNCTIONS
	// ***********************************
	private Vector3 FeedPoint = Vector3.zero;
	// want circles to follow a pattern in a direction
	bool setchooser;
	bool chosenset;
	private Time timerun;
	bool SetChooser()
    {
		if(!chosenset)
        {
			setchooser = System.Convert.ToBoolean(Random.Range(0, 1));
			chosenset = true;
		}
		return setchooser;
	}

	private Vector3 NewCenterPoint(Vector3 currentCenter, float currentRadius, float newRadius)
	{
		var offset = UnityEngine.Random.RandomRange(-120, 120);
		Debug.Log("orientation chooser " + SetChooser());

		Vector3 newPoint;
		if(FeedPoint == Vector3.zero)
        {
			FeedPoint = new Vector3(1200, 0, 1200);
		}

		// get a positive offset
		if (setchooser)
        {
			newPoint = new Vector3(FeedPoint.x + offset, 0, FeedPoint.z + offset);
        }
		else
		{
			newPoint = new Vector3(FeedPoint.x - offset, 0, FeedPoint.z - offset);
		}

		FeedPoint = newPoint;
		//bool chooser = (bool)orientationchooser;
		//Vector3 newPoint = Vector3.zero;

		var totalCountDown = 30000; //prevent endless loop which will kill Unity
		var foundSuitable = false;
		while (!foundSuitable)
		{
			 totalCountDown--;
			 Vector2 randPoint = Random.insideUnitCircle * (currentRadius * 2.0f);
			 newPoint = new Vector3(randPoint.x, 0, randPoint.y);
			 foundSuitable = (Vector3.Distance(currentCenter, newPoint) < 400);
			 if (totalCountDown < 1)
			   return new Vector3(0, -100, 0);  //explicitly define an error has occured.  In this case we did not locate a reasonable point
		}
		return newPoint;
	}

	private int NextZoneTime()
	{
		//if we have exceeded the count, just start over
		if (zoneTimesIndex >= ZoneTimes.Count -1) // Lists are zero-indexed
		  zoneTimesIndex = -1;  // the fall-through (below) will increment this
		Debug.Log("zonetimes index " + (int)zoneTimesIndex);
		// next time to wait
		return ZoneTimes[++zoneTimesIndex];
	}

	// This is a general purpose method
	private float[] ShrinkCircle(float amount)
	{
		float newXR = circle.radii[0] - amount;
		float newYR = circle.radii[1] - amount;
		float [] retVal = new float[2];
		retVal[0] = newXR;
		retVal[1] = newYR;
		return retVal;
	}
}
