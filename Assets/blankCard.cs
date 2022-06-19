﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;

public class blankCard : MonoBehaviour {

	// Use this for initialization
	public new KMAudio audio;
	public KMBombInfo bomb;
	static int moduleIdCounter = 1;
	int moduleId;
	public KMBombModule module;
	public KMSelectable clearButton;
	public KMSelectable[] points;
	public AudioClip solveSFX;
	public Transform markedLine;
	public Transform[] lines;
	public LineRenderer lineRenderer;
	public MeshCollider canvas;
	public Transform canvasTrans;
	public Material[] cards;
	public MeshRenderer cardForm;
	public Material white;
	private int numConsecutiveLines = 0;
	private int numLineCounter = 0;
	private ArrayList connectedLines = new ArrayList();
	private int[] lastConnection = new int[2];
	private int[] index = { 0, 1, 2, 3 };
	private int[] card = new int[2];
	private float[] Xpos = {0.0222f, 0.035f, 0.04764f, 0.06044f};
	private float[] Ypos = { 0.0171f, 0.00486f, -0.0074f, -0.0196f, -0.03186f};
	private float[][][] connectedRange = {
		new float[][]{
		new float[]{ 0.11f, 0.19f, 0.05f, 0.13f },
		new float[]{ 0.79f, 0.90f, 0.87f, 0.94f }
		},

		new float[][]{
		new float[]{ 0.79f, 0.90f, 0.05f, 0.13f },
		new float[]{ 0.10f, 0.20f, 0.87f, 0.94f }
		},
		new float[][]{
		new float[]{ 0.79f, 0.90f, 0.05f, 0.13f },
		new float[]{ 0.11f, 0.19f, 0.87f, 0.94f }
		},
		new float[][]{
		new float[]{ 0.11f, 0.19f, 0.05f, 0.13f },
		new float[]{ 0.79f, 0.90f, 0.87f, 0.94f },
		}
	};
	private int[][] connectPoint =
	{
		new int[]{ 1, 2 },
		new int[]{ 0, 3 },
		new int[]{ 0, 3 },
		new int[]{ 1, 2 }
	};
	
	private int[][] connectLine =
	{
		new int[]{ 0, 1 },
		new int[]{ 0, 2 },
		new int[]{ 1, 3 },
		new int[]{ 2, 3 }
	};
	bool? solving = null;
	int connectedPoint = -1;
	void Start () 
	{
		moduleId = moduleIdCounter++;
		card[0] = UnityEngine.Random.Range(0, 13);
		markedLine.transform.localPosition = new Vector3(Xpos[card[0] % 4], 0.01559f, Ypos[card[0] / 4]);
		card[1] = UnityEngine.Random.Range(0, 4);	
		int angle = 45 * card[1];
		markedLine.transform.Rotate(Vector3.up, angle);
		Debug.LogFormat("[Blank Card #{0}] Marked Card: {1} of {2}", moduleId, new string[] { "Ace", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Jack", "Queen", "King"}[card[0]], new string[] {"Spades", "Hearts", "Clubs", "Diamonds"}[card[1]]);
		foreach (int i in index)
		{
			points[i].OnInteract = delegate { StartCoroutine(connecting(i)); return false; };
			points[i].OnInteractEnded = delegate { };
		}
		clearButton.OnInteract = delegate {
			clearButton.AddInteractionPunch();
			audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform); reset(); 
			return false; 
		};
		lineRenderer.positionCount = 2;
		
		cards[0].color = new Color(0f, 0f, 0f);
		lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
		lineRenderer.startColor = Color.white;
		lineRenderer.endColor = Color.white;
	}
	private IEnumerator connecting(int point)
	{
		yield return null;
		//float forward = Vector3.Dot(Camera.main.transform.TransformVector(Vector3.forward), module.transform.TransformVector(Vector3.forward));
		//Debug.LogFormat("[Blank Card #{0}] {1}", moduleId, forward);
		
		float scalar = transform.lossyScale.x;
		lineRenderer.startWidth = 0.01f * scalar;
		lineRenderer.endWidth = 0.01f * scalar;
		lineRenderer.SetPosition(0, new Vector3(points[point].transform.localPosition.x, 1f, points[point].transform.localPosition.z));

		connectedPoint = -1;
		while (Input.GetMouseButton(0))
		{
			
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			canvas.Raycast(ray, out hit, 100.0f);
			Vector2 v = hit.textureCoord;
			lineRenderer.SetPosition(1, new Vector3(points[point].transform.localPosition.x, 1f, points[point].transform.localPosition.z));
			while (v.x == 0 && v.y == 0)
			{
				
				ray = Camera.main.ScreenPointToRay(Input.mousePosition);
				canvas.Raycast(ray, out hit, 100.0f);
				v = hit.textureCoord;
				yield return new WaitForSeconds(0.01f);
			}
			if (Input.GetMouseButton(0))
				lineRenderer.SetPosition(1, new Vector3((1f - v.x) - 0.5f, 1f, (1f - v.y) - 0.5f));

			//Debug.LogFormat("[Blank Card #{0}] {1} {2}", moduleId, v.x, v.y);
			//Debug.LogFormat("[Blank Card #{0}] {1} ", moduleId, lineRenderer.startWidth);
			if (v.x >= connectedRange[point][0][0] && v.x <= connectedRange[point][0][1] && v.y >= connectedRange[point][0][2] && v.y <= connectedRange[point][0][3] && !(connectedLines.Contains(connectLine[point][0])))
			{
				produceLine(point, 0);
				break;
			}
			else if (v.x >= connectedRange[point][1][0] && v.x <= connectedRange[point][1][1] && v.y >= connectedRange[point][1][2] && v.y <= connectedRange[point][1][3] && !(connectedLines.Contains(connectLine[point][1])))
			{
				produceLine(point, 1);
				break;
			}
			
			yield return new WaitForSeconds(0.01f);
		}
		connect(point);
	}

	private void produceLine(int point, int ix)
    {
		Debug.Log(point);
		lines[connectLine[point][ix]].transform.localPosition = new Vector3(lines[connectLine[point][ix]].transform.localPosition.x, 0.55f, lines[connectLine[point][ix]].transform.localPosition.z);
		connectedPoint = connectPoint[point][ix];
		connectedLines.Add(connectLine[point][ix]);
	}
	private void connect(int point)
    {
		lineRenderer.SetPosition(0, new Vector3(0f, 0f, 0f));
		lineRenderer.SetPosition(1, new Vector3(0f, 0f, 0f));
		points[point].OnInteractEnded();
		if (connectedPoint != -1)
		{
			updateInteractions();
			lastConnection[0] = point;
			lastConnection[1] = connectedPoint;
			numLineCounter++;
			if (points[connectedPoint].OnInteract != null)
				points[connectedPoint].OnInteract();
			else
			{
				numConsecutiveLines = numLineCounter;
				numLineCounter = 0;
				if (connectedLines.Count == 4)
					StartCoroutine(formCard());
			}
		}
		else
		{
			numConsecutiveLines = numLineCounter;
			numLineCounter = 0;
		}
	}
	private void updateInteractions()
	{
		if (connectedLines.Contains(0) && connectedLines.Contains(1))
			points[0].OnInteract = null;
		if (connectedLines.Contains(0) && connectedLines.Contains(2))
			points[1].OnInteract = null;
		if (connectedLines.Contains(1) && connectedLines.Contains(3))
			points[2].OnInteract = null;
		if (connectedLines.Contains(2) && connectedLines.Contains(3))
			points[3].OnInteract = null;
	}
	private IEnumerator formCard()
	{
		clearButton.OnInteract = null;
		Debug.LogFormat("[Blank Card #{0}] Lines: {1} {2} {3} {4}", moduleId, connectedLines[0], connectedLines[1], connectedLines[2], connectedLines[3]);
		Debug.LogFormat("[Blank Card #{0}] Last Connection: {1} {2}", moduleId, lastConnection[0], lastConnection[1]);
		int suit = (int)connectedLines[0];
		int rank;
		if(numConsecutiveLines == 4)
			rank = 12;
		else
		{
			int[] ranks;
			switch(Array.IndexOf(new int[] { 0, 2, 3, 1 }, (int)connectedLines[1]) - Array.IndexOf(new int[] { 0, 2, 3, 1 }, suit))
			{
				case 1:
				case -3:
					ranks = new int[] { 0, 1, 2, 3};
					break;
				case 2:
				case -2:
					ranks = new int[] { 4, 5, 6, 7 };
					break;
				default:
					ranks = new int[] { 8, 9, 10, 11 };
					break;
			}
			ArrayList vals = new ArrayList() { 0, 2, 3, 1 };
			vals.Remove(suit);
			int firstLine = (int)vals[(vals.IndexOf(connectedLines[1]) + 1) % 3];
			if (firstLine == (int)connectedLines[2])
				ranks = new int[] { ranks[0], ranks[2] };
			else
				ranks = new int[] { ranks[1], ranks[3] };
			switch(lastConnection[0] + "" + lastConnection[1])
			{
				case "10":
				case "32":
				case "02":
				case "13":
					rank = ranks[0];
					break;
				default:
					rank = ranks[1];
					break;
			}
		}
		Debug.LogFormat("[Blank Card #{0}] Card Formed: {1} of {2}", moduleId, new string[] { "Ace", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Jack", "Queen", "King" }[rank], new string[] { "Spades", "Hearts", "Clubs", "Diamonds" }[suit]);
		cardForm.material = cards[(rank * 4) + suit];
		
		yield return new WaitForSeconds(1f);
		for(float aa = 0; aa <= 1.0f; aa += 0.01f)
		{
			
			cardForm.material.color = new Color(aa, aa, aa);
			yield return new WaitForSeconds(0.01f);
		}
		solving = card[0] == rank && card[1] == suit;
		yield return new WaitForSeconds(1f);
		if (solving.Value)
		{
			audio.PlaySoundAtTransform(solveSFX.name, transform);
			module.HandlePass();
		}
		else
		{
			module.HandleStrike();
			cardForm.material.color = new Color(0f, 0f, 0f);
			reset();
		}
	}
	private void reset()
	{
		solving = null;
		foreach (int i in index)
		{
			points[i].OnInteract = delegate { StartCoroutine(connecting(i)); return false; };
			lines[i].transform.localPosition = new Vector3(lines[i].transform.localPosition.x, -1f, lines[i].transform.localPosition.z);
		}
		clearButton.OnInteract = delegate {
			clearButton.AddInteractionPunch();
			audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform); reset();
			return false;
		};
		numConsecutiveLines = 0;
		connectedLines.Clear();
	}

	private readonly string TwitchHelpMessage = @"Use [!{0} connect 1 2 4, 4 3] to connect those dots. Spaces separate dots part of the same stroke. Commas separate completed strokes. Use [!{0} clear to clear the canvas.";
#pragma warning disable 414

	IEnumerator ProcessTwitchCommand(string command)
	{
		command = command.Trim(' ', ',', ';').ToUpperInvariant();
		Match m = Regex.Match(command, @"^(?:CONNECT\s*)?((?:[1-4](?:[\s,;]+|$))+)$");
		if (command == "CLEAR")
        {
			yield return null;
			clearButton.OnInteract();
        }
		else if (m.Success)
		{
			yield return null;
			int[][] strokes = m.Groups[1].Value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(stroke =>
					stroke.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(pos => pos[0] - '1').ToArray()).ToArray();
			Debug.Log(strokes.Select(x => x.Join(" ")).Join("|"));
			foreach (int[] stroke in strokes)
            {
				points[stroke.First()].OnInteract();
				connectedPoint = stroke.First();
				foreach (int pos in stroke)
                {
					produceLine(connectedPoint, 0);
					connect(connectedPoint);
					yield return new WaitForSeconds(0.1f);
                }
				points[stroke.Last()].OnInteractEnded();
            }
			if (solving != null)
				yield return solving.Value ? "solve" : "strike";
		}

	}

	IEnumerator TwitchHandleForcedSolve()
	{
		yield return null;
	}
}
