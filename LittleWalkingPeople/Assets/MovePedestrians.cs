using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;


public class MovePedestrians : MonoBehaviour {
	public float scaling = 0.1f;
	public GameObject prefab;

	private class TrackInfo
	{
		public int startFrame;
		public int frameCount;
		public List<Vector2> points;

		public void setPoints(List<Vector2> newPoints) {
			this.points = newPoints;
		}
	}
		
	private List<GameObject> people;
	private List<MovePedestrians.TrackInfo> tracks;
	private int currentFrame = 0; //Starting Frame
	private int waiting = 0;
	private int step = 1;
	private bool loaded;

	//initialize file browser
	private FileBrowser fb = new FileBrowser();
	private string path = "";

	private Camera mainCam;

	void Start() {
		tracks = new List<MovePedestrians.TrackInfo>();
		people = new List<GameObject>();
		
		loaded = false;

		//Set up camera camera position
		mainCam = Camera.main;
		mainCam.transform.position = new Vector3 (24.91f, 12.19f, -7.1f);
		mainCam.transform.localEulerAngles = new Vector3 (22.32f, 81.73f, 0.34f);
		mainCam.fieldOfView = 60;
		mainCam.transform.localScale = new Vector3 (1, 1, 1);

	}	

	void OnGUI(){
		if (path == "") {
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Selected File: " + path);
			GUILayout.EndHorizontal ();
			//draw and display output
			if (fb.draw ()) { //true is returned when a file has been selected
				//the output file is a member if the FileInfo class, if cancel was selected the value is null
				path = (fb.outputFile == null) ? "cancel hit" : fb.outputFile.FullName;
			}
		} else if (!loaded) {
			load();
		}
	}

	void load() {
		string line;
		char[] delimiterChars = { ' ','\t' };

		//Read file
		using (StreamReader reader = new StreamReader(path))
		{
			//First section
			do{
				line = reader.ReadLine();
				if (line != ""){
					GameObject currentPerson = Instantiate(prefab) as GameObject;

					people.Add(currentPerson);
					string[] frameInfo = line.Split(delimiterChars);
					int startFrame = Convert.ToInt32(frameInfo[0]);
					int frameLength = Convert.ToInt32(frameInfo[1]);
					
					TrackInfo track = new TrackInfo();
					track.startFrame = startFrame;
					track.frameCount = frameLength;
					track.points = new List<Vector2>();
					tracks.Add(track);
				}
			} while (line != "");
			
			//Second section
			for (int j = 0; j < tracks.Count; j++) {
				TrackInfo track = new TrackInfo ();;
				track.points = new List<Vector2> ();
				track.frameCount = tracks [j].frameCount;

				for (int i = 0; i < track.frameCount; i++) {
					line = reader.ReadLine();
					if (line == null){
						Debug.LogError("File out of bounds.");
					}
					
					String[] locInfo = line.Split(delimiterChars);
					
					Vector2 point;
					point.x = float.Parse(locInfo[0]);
					point.y = float.Parse(locInfo[1]);
						
					track.points.Add(point);
				}

				// Find overall direction model is moving in
				Vector2 summedDirection = new Vector2 (track.points [0].x, track.points [0].y);
				Vector2 startPoint = new Vector2 (track.points [0].x, track.points [0].y);
				foreach (Vector2 curTrack in track.points) {
					if (startPoint.x < curTrack.x) {
						summedDirection.x += curTrack.x;
					} else if (startPoint.x > curTrack.x) {
						summedDirection.x -= curTrack.x;
					}

					if (startPoint.y < curTrack.y) {
						summedDirection.y += curTrack.y;
					} else if (startPoint.y > curTrack.y) {
						summedDirection.y -= curTrack.y;
					}
				}

				summedDirection.x /= track.points.Count;
				summedDirection.y /= track.points.Count;


				// Set color based on overall direction
				GameObject currentPerson = people[j];
				GameObject maleModel = currentPerson.transform.GetChild (0).gameObject;
				SkinnedMeshRenderer currentPersonRenderer = maleModel.GetComponent<SkinnedMeshRenderer>();
				Material newMaterial = new Material(Shader.Find("Standard"));
				Color newColor;

				if (summedDirection.x <= startPoint.x) {
					newColor = Color.blue;
				} else {
					newColor = Color.green;
				}

				newMaterial.color = newColor;
				newMaterial.SetFloat("_Metallic", 0.115f);
				newMaterial.SetFloat("_Glossiness", 0.329f);
				currentPersonRenderer.material = newMaterial;

				// If summedDirection.x < startPoint.x = moving down
				List<Vector2> newPoints = new List<Vector2>();
				Vector2 prevPoint = new Vector2(track.points [0].x, track.points [0].y);
				Vector2 newPoint = new Vector2();
				int y = 51;

				for (int i = 0; i < track.points.Count; i++) {
					// Adjusting x and y points
					if (summedDirection.x <= startPoint.x) {
						newPoint.x = --prevPoint.x;
					} else if (summedDirection.x > startPoint.x) {
						newPoint.x = ++prevPoint.x;
					}

					if (i % 15 == 0) {
						y = UnityEngine.Random.Range(45, 65);
					}

					newPoint.y = y;
					newPoints.Add(newPoint);
				}
					
				tracks[j].points = newPoints;

//				foreach (Vector2 point in newPoints) {
//					Debug.Log(point.ToString());
//				}
			}

		}
		loaded = true;

		// Restart player
		currentFrame = 0;
	}

	//Update each "person" during each frame
	void Update(){
		if (Input.GetKey ("escape")) {
			Application.Quit();
		}

		if (Input.GetKey ("space")) {
			// Restart player
			currentFrame = 0;
		}

		int personNum = 0;

		// Go through each track and get the corresponding frame
		// if it exists

		foreach (TrackInfo track in tracks) {
			GameObject person = people[personNum] as GameObject;

			if (track.startFrame < currentFrame && currentFrame < (track.startFrame + track.frameCount)) {

				// access tack  position at current frame
				int i = currentFrame - track.startFrame;
				Vector2 point = track.points[i];

				float x = point.x*scaling;
				float y = point.y*scaling*-1;
				Vector3 location = new Vector3(x, 0.8f, y);

				float distance = Vector3.Distance(person.transform.position, location);

				// set up person as they first appear
				if (!person.activeSelf) {
					person.SetActive(true);
					person.transform.LookAt (location);
					person.transform.position = location;
					continue;
				}

				// only change view direction for major shifts
				if (distance > 0.1)  {
					person.transform.LookAt (location);
				}

				person.transform.position = Vector3.MoveTowards(person.transform.position, location, 1 * Time.deltaTime);

				//Debug.Log("Frame: " + currentFrame + " Person: " + personNum + " X: " + x + " Y: " + y);
			} else {
				person.SetActive(false);
				person.GetComponent<Animation>().Stop ();
			}

			personNum++;
		}
		if (waiting%step == 0) {
			currentFrame++;
		}
		waiting++;
	}

}


