using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ShowFPS : MonoBehaviour {
	public TextMeshProUGUI fpsText;
	public float pollingTime = 1.0f;
	public float time;
	public int frameCount = 0;

    void Start()
    {
        fpsText = gameObject.GetComponent<TextMeshProUGUI>();
    }

	void Update () {
		time += Time.deltaTime;
		frameCount++;

		if(time >= pollingTime)
		{
			int frameRate = Mathf.RoundToInt(frameCount / time);
			fpsText.text = "FPS " + frameRate.ToString();

			time -= pollingTime;
			frameCount = 0;
		}
	}
}