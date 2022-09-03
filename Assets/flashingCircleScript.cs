using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class flashingCircleScript : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
	public KMBombModule module;

    public GameObject[] circles;
    public KMSelectable startButton;
    public Material[] circleColours;
    public Material[] borderColours;

    private string[] colourSequence = new string[36];
    private int[] correctCircles = new int[2];
    private int[] circleStatus = new int[36];
    private bool[] correct = new bool[2];
    private bool isAnimating, activated;

    private float[][] flashSpeed = new float[][]
    {
        new float[10] { 1f / 1.08f, 1f / 1.08f, 1f / 1.08f, 1f / 1.08f, 1f / 1.08f, 1f / 1.08f, 1f / 1.08f, 2f / 1.08f, 2f / 1.08f, 1f / 1.08f },
        new float[10] { 1f / 1.868f, 1f/0.682f, 1f/1.892f, 1f/0.682f, 1f/1.883f, 1f/0.665f, 1f/1.273f, 95f / 60f, 95f / 60f, 95f / 60f },
        new float[10] { 1f / 1.216f, 1f / 0.604f, 1f / 1.197f, 1f / 0.607f, 1f / 0.914f, 1f / 0.903f, 1f / 0.881f, 1f / 0.6f, 1f / 0.307f, 1f / 0.818f},
        new float[10] { 59/60f, 59/60f, 59/60f, 59/60f, 59/60f, 59/60f, 59/60f, 118/60f, 118/60f, 59/60f },
        new float[10] { 60/60f, 60 / 60f, 60 / 60f, 60 / 60f, 60 / 60f , 60 / 60f , 60 / 60f , 60 / 60f , 60 / 60f , 60 / 60f },
        new float[10] { 1/0.82f, 1/0.817f, 1/2.197f, 1/0.547f, 1/0.818f, 1/0.816f, 1/1.093f, 1/1.09f, 1/0.506f, 1/1.09f },
        new float[10] { 1/1.394f, 1/1.065f, 1/1.592f, 1/1.606f, 1/1.408f, 1/1.066f, 1/1.058f, 1/1.063f, 1/1.046f, 1/1f },
        new float[10] { 1/0.472f, 1/0.818f, 1/1.057f, 1/1.398f, 1/0.472f, 1/0.818f, 1/1.039f, 1/0.94f, 1/0.462f, 1/0.469f },
    };
    private int selectedMusic;

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    void Awake()
    {
    	moduleId = moduleIdCounter++;

        startButton.OnInteract += delegate {
            colourFlash();
            startButton.AddInteractionPunch(0.25f);
            return false;
        };
        for (int i = 0; i < circles.Length; i++)
        {
            int j = i;
            circles[j].GetComponent<KMSelectable>().OnInteract += () => { circleHandler(j); return false; };
        }
    }

    void Start()
    {
        HashSet<int> numbers = new HashSet<int>();
        while (numbers.Count < 2) { numbers.Add(UnityEngine.Random.Range(0, 36)); }//Selecting correct circle pair
        int k = 0;
        foreach (int i in numbers) { correctCircles[k] = i; k++; }
        Array.Sort(correctCircles);
        HashSet<string> colors = new HashSet<string>();
        StringBuilder sb = new StringBuilder();
        while (colors.Count < 35)
        {
            sb.Remove(0, sb.Length);
            for (int i = 0; i < 10; i++)
            {
                int rnd = UnityEngine.Random.Range(0, circleColours.Length);
                switch (rnd)
                {
                    case 0:
                        sb.Append("R");
                        break;
                    case 1:
                        sb.Append("G");
                        break;
                    case 2:
                        sb.Append("B");
                        break;
                    case 3:
                        sb.Append("C");
                        break;
                    case 4:
                        sb.Append("M");
                        break;
                    case 5:
                        sb.Append("Y");
                        break;
                }
            }
            colors.Add(sb.ToString());
        }
        k = 0;
        foreach (string i in colors)
        {
            if (k == correctCircles[1])
            {
                colourSequence[k] = colourSequence[correctCircles[0]];
                k++;
            }
            colourSequence[k] = i;
            k++;
        }
        if (correctCircles[1] == 35) { colourSequence[35] = colourSequence[correctCircles[0]];}
        sb.Remove(0, sb.Length);
        Debug.LogFormat("[Flashing Circles #{0}]: The colour sequence for each circle is as follows:", moduleId);
        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < 6; j++)
            {
                sb.Append(colourSequence[i * 6 + j] + " ");
            }
            Debug.LogFormat("[Flashing Circles #{0}]: {1}", moduleId, sb.ToString());
            sb.Remove(0, sb.Length);
        }
        Debug.LogFormat("[Flashing Circles #{0}]: The correct circles selected are #{1} and #{2}, in reading order.", moduleId, correctCircles[0] + 1, correctCircles[1] + 1);
        for (int i = 0; i < circleStatus.Length; i++)
        {
            circleStatus[i] = 0;
        }

    }

    void colourFlash()
    {
        /*selectedMusic = 7;//Testing solve animation
        module.HandlePass();
        moduleSolved = true;
        Debug.LogFormat("[Flashing Circles #{0}]: Beta-solving! Displaying animation!", moduleId);
        foreach (GameObject m in circles)
        {
            m.GetComponent<MeshRenderer>().material = borderColours[0];
        }
        audio.PlaySoundAtTransform("Solve " + (selectedMusic + 1).ToString(), transform);
        StartCoroutine(solveAnim(selectedMusic));
        return;*/

        if (isAnimating || moduleSolved) { return; }
        int k = 0;
        activated = true;
        foreach (GameObject i in circles)
        {
            i.GetComponent<MeshRenderer>().material = borderColours[0];
        }
        Debug.LogFormat("<Flashing Circles #{0}>: Start button pressed, displaying colours...", moduleId);

        selectedMusic = UnityEngine.Random.Range(0, 8);
        audio.PlaySoundAtTransform("Track " + (selectedMusic+1).ToString(), transform);

        foreach (GameObject m in circles)
        {
            StartCoroutine(actualColourFlash(m.transform.GetChild(0).gameObject, k));
            k++;
        }
    }

    void circleHandler(int k)
    {
        if (isAnimating || !activated) { return; }
        if (!moduleSolved)
        {
            audio.PlaySoundAtTransform("Select", transform);
            for (int i = 0; i < 2; i++)
            {
                if (k == correctCircles[i])
                {
                    correct[i] = true;
                    circles[k].GetComponent<MeshRenderer>().material = borderColours[1];
                    circleStatus[k] = 1;
                    Debug.LogFormat("[Flashing Circles #{0}]: Circle #{1} is selected, and it's correct.", moduleId, k + 1);
                    if (correct.All(x => x))
                    {
                        module.HandlePass();
                        moduleSolved = true;
                        Debug.LogFormat("[Flashing Circles #{0}]: The correct pairs have been selected, module solved!", moduleId);
                        foreach (GameObject m in circles)
                        {
                            m.GetComponent<MeshRenderer>().material = borderColours[0];
                        }
                        audio.PlaySoundAtTransform("Solve " + (selectedMusic + 1).ToString(), transform);
                        StartCoroutine(solveAnim(selectedMusic));
                    }
                    return;
                }
            }
            module.HandleStrike();
            circles[k].GetComponent<MeshRenderer>().material = borderColours[2];
            circleStatus[k] = 2;
            Debug.LogFormat("[Flashing Circles #{0}]: Circle #{1} is selected, but it's incorrect. Strike.", moduleId, k + 1);
        }
        else
        {
            //Do something here (When I feel like it that is)
        }
    }

    IEnumerator actualColourFlash(GameObject k, int index)
    {
        isAnimating = true;
        float delta = 0f;
        if (selectedMusic == 1 || selectedMusic == 4 || selectedMusic == 5) 
        {
            while (delta < 1f)
            {
                if (selectedMusic == 1) { delta += Time.deltaTime * 1 / 0.761f; }
                else if (selectedMusic == 4) { delta += Time.deltaTime * 1 / 0.79f; }
                else if (selectedMusic == 5) { delta += Time.deltaTime * 1 / 0.884f; }
                k.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.white, Color.black, delta);
                yield return null;
            }
        }
        for (int i = 0; i < colourSequence[index].Length; i++)
        {
            Color colour;
            switch (colourSequence[index][i])
            {
                case 'R':
                    k.GetComponent<MeshRenderer>().material = circleColours[0];
                    colour = Color.red;
                    break;
                case 'G':
                    k.GetComponent<MeshRenderer>().material = circleColours[1];
                    colour = Color.green;
                    break;
                case 'B':
                    k.GetComponent<MeshRenderer>().material = circleColours[2];
                    colour = Color.blue;
                    break;
                case 'C':
                    k.GetComponent<MeshRenderer>().material = circleColours[3];
                    colour = Color.cyan;
                    break;
                case 'M':
                    k.GetComponent<MeshRenderer>().material = circleColours[4];
                    colour = Color.magenta;
                    break;
                case 'Y':
                    k.GetComponent<MeshRenderer>().material = circleColours[5];
                    colour = Color.yellow;
                    break;
                default:
                    colour = Color.white;
                    break;
            }
            delta = 0f;
            while (delta < 1f)
            {
                delta += Time.deltaTime * flashSpeed[selectedMusic][i];
                k.GetComponent<MeshRenderer>().material.color = Color.Lerp(colour, Color.white, delta);
                yield return null;
            }
        }
        isAnimating = false;
        for (int i = 0; i < circleStatus.Length; i++)
        {
            circles[i].GetComponent<MeshRenderer>().material = borderColours[circleStatus[i]];
        }
    }
    //Solve animations
    private float[][] solveSpeed = new float[][]//Note to self, time period in seconds per beat = BPM / 60
    {
        new float[]{ 1f/0.3185f, 1f/0.318f, 1f/0.318f, 1f/0.318f, 95f/60f, 95f / 60f, 95f / 60f, 95f / 60f, 95f / 60f, 95f / 60f, 95f / 60f, 95f / 60f, 95f / 60f, 95f / 60f, 1f/0.878f},
        new float[]{ 1f/0.272f, 1f/0.17f, 1f/0.427f, 1f/1.23f, 97f*2/60f, 97f * 2 / 60f , 97f * 2 / 60f , 97f * 2 / 60f, 97f * 2 / 60f, 97f * 2 / 60f, 97f * 2 / 60f, 97f * 2 / 60f, 97f * 2 / 60f, 97f * 2 / 60f - 0.2f, 1f /0.193f, 1f/0.446f, 1/1.113f },
        new float[]{ 1f/1.152f, 1/0.91f, 1/0.878f, 1/0.898f, 1/0.914f, 1/0.884f, 1/0.889f, 1/0.921f, 1/0.83f},
        new float[]{ 1f/1.068f, 1/1f, 118 / 60f, 118 / 60f, 118 / 60f, 118 / 60f, 118 / 60f, 118 / 60f, 118 / 60f, 118 / 60f, 118 / 60f, 118 / 60f, 118 / 60f, 118 / 60f, 118 / 60f, 118 / 60f, 59 / 60f },
        new float[]{ 1/1.1f, 1/1.35f, 1/1.35f, 1/1.35f, 1/1.35f, 1/1.35f, 1/1.35f, 1/2f },
        new float[]{ 1/0.5f, 220/60f, 220 / 60f, 220 / 60f, 1/0.821f, 1/0.813f, 1/1.487f, 1/0.417f, 220 / 60f, 220 / 60f, 220 / 60f, 1/0.83f, 1/0.817f, 1/0.813f, 1 /1.6f},
        new float[]{ 85/60f, 85/60f, 85/60f, 85/60f, 85/60f, 85/60f, 85/60f, 85/60f, 85/60f, 85/60f, 85/60f, 85/60f, 85/60f, 1/0.345f, 1 / 0.345f, 1 / 0.345f, 2 / 0.345f, 1 / 0.345f, 85/15f, 85/30f, 85/30f, 85/30f, 85/120f},
        new float[]{ 1/0.5f, 128/60f, 128 / 60f, 128 / 60f, 128/30f, 128/30f, 128/30f, 128/30f, 128/120f, 1/0.477f, 1/0.12f, 1/0.232f, 1/0.465f, 1/0.354f, 1/0.65f, 1/0.12f, 1/0.117f, 1/1.165f, 1/0.473f, 1/0.12f, 1/0.232f, 1/0.465f, 1/0.354f, 1/0.709f},
    };

    private float[][] firstTrackPattern = new float[][]
    {
        new float[]{0, 1, 2, 3, 4, 5, 6, 12, 13, 14, 15, 16, 17, 23, 29, 30, 31, 32, 33, 34, 35}, //S
        new float[]{0, 1, 2, 3, 4, 5, 6, 11, 12, 17, 18, 23, 24, 29, 30, 31, 32, 33, 34, 35}, //O
        new float[]{0, 6, 12, 18, 24, 30, 31, 32, 33, 34, 35}, //L
        new float[]{0, 5, 6, 11, 12, 17, 18, 23, 25, 28, 32, 33}, //V
        new float[]{0, 1, 2, 3, 4, 5, 6, 12, 13, 14, 15, 16, 17, 18, 24, 30, 31, 32, 33, 34, 35}, //E
        new float[]{0, 1, 2, 3, 4, 6, 11, 12, 17, 18, 23, 24, 29, 30, 31, 32, 33, 34}, //D
        new float[]{0, 5, 6, 7, 10, 11, 12, 14, 15, 17, 18, 23, 24, 29, 30, 35}, //M
        new float[]{0, 1, 2, 3, 4, 5, 6, 11, 12, 17, 18, 23, 24, 29, 30, 31, 32, 33, 34, 35}, //O
        new float[]{0, 1, 2, 3, 4, 6, 11, 12, 17, 18, 23, 24, 29, 30, 31, 32, 33, 34}, //D
        new float[]{0, 1, 2, 3, 4, 6, 11, 12, 17, 18, 23, 24, 29, 30, 31, 32, 33, 34}, //D
        new float[]{0, 5, 6, 11, 12, 17, 19, 20, 21, 22, 25, 28, 32, 33}, //Axo logo
        new float[]{0, 1, 2, 3, 4, 5, 6, 12, 18, 21, 22, 23, 24, 29, 30, 31, 32, 33, 34, 35}, //G
        new float[]{5, 11, 17, 23, 24, 29, 31, 32, 33, 34}, //J
    };

    IEnumerator solveAnim(int k)
    {
        float delta = 0f;
        float rnd = 0f;
        Color final = new Color(0f, 0f, 0f, 1f);
        Color c = new Color(0f, 0f, 0f, 1f);
        switch (k)
        {
            case 0:
                for (int i = 0; i < solveSpeed[k].Length; i++)
                {
                    delta = 0f;
                    while (delta < 1f)
                    {
                        delta += Time.deltaTime * solveSpeed[k][i];
                        for (int j = 0; j < circles.Length; j++)
                        {
                            switch (i)
                            {
                                case 0:
                                case 1:
                                case 2:
                                case 3:
                                    circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.black, Color.grey, delta);
                                    break;

                                default:
                                    if (firstTrackPattern[i - 4].Contains(j))
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.green, Color.black, delta);
                                    else
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.grey, Color.black, delta);
                                    break;
                                case 12:
                                    if (firstTrackPattern[i - 4].Contains(j))
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.green, Color.black, delta);
                                    else
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(new Color(0f, 0.7f, 0f, 1f), Color.black, delta);
                                    break;
                                case 13:
                                    if (firstTrackPattern[i - 4].Contains(j))
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.green, Color.black, delta);
                                    else
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(new Color(0f, 0.4f, 0f, 1f), Color.black, delta);
                                    break;
                                case 14:
                                    if (firstTrackPattern[i - 4].Contains(j))
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.blue, Color.black, delta);
                                    else
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(new Color(255 / 256f, 215 / 256f, 0 / 256f, 1f), Color.black, delta);
                                    break;
                            }
                        }
                        yield return null;
                    }
                }
                break;

            case 1:
                final = new Color(0f, 0.8f, 0f, 1f);
                c = final / 6;
                for (int i = 0; i < solveSpeed[k].Length; i++)
                {
                    delta = 0f;
                    while (delta < 1f)
                    {
                        delta += Time.deltaTime * solveSpeed[k][i];
                        for (int j = 0; j < circles.Length; j++)
                        {
                            switch (i)
                            {
                                case 0:
                                    circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.black, Color.grey, delta);
                                    break;
                                case 1:
                                case 2:
                                    if (j < 18 && i == 1 || j >= 18 && i == 2)
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.white, Color.black, delta);
                                    else
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.black, Color.black, delta);
                                    break;
                                case 3:
                                    circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.green, Color.black, delta);
                                    break;

                                case 4:
                                    if (i == 4 && j % 6 < 3 && j < 18)//Top left
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.red, Color.black, delta);
                                    break;
                                case 5:
                                    if (i == 5 && j % 6 >= 3 && j < 18)//Top right
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.blue, Color.black, delta);
                                    break;
                                case 6:
                                    if (i == 6 && j % 6 < 3 && j >= 18)//Bottom left
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.yellow, Color.black, delta);
                                    break;
                                case 7:
                                    if (i == 7 && j % 6 >= 3 && j >= 18)//Bottom right
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.white, Color.black, delta);
                                    break;

                                default:
                                    if (firstTrackPattern[i - 8].Contains(j))
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.green;
                                    else
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(final, final - c, delta);
                                    break;

                                case 14:
                                case 15:
                                    if ((j % 6 < 3 && i == 14) || (j % 6 >= 3 && i == 15))
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.white, Color.black, delta);
                                    else
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.black, Color.black, delta);
                                    break;

                                case 16:
                                    if (firstTrackPattern[10].Contains(j))
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.blue, Color.black, delta);
                                    else
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(new Color(255 / 256f, 215 / 256f, 0 / 256f, 1f), Color.black, delta);
                                    break;
                            }
                        }
                        yield return null;
                    }
                    if (i >= 8 && i < 14)
                        final -= c;
                }
                break;

            case 2:
                for (int i = 0; i < solveSpeed[k].Length; i++)
                {
                    delta = 0f;
                    while (delta < 1f)
                    {
                        delta += Time.deltaTime * solveSpeed[k][i];
                        for (int j = 0; j < circles.Length; j++)
                        {
                            switch (i)
                            {
                                case 0:
                                    circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.white, Color.black, delta);
                                    break;
                                default:
                                    if (firstTrackPattern[i - 1].Contains(j))
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.green, Color.black, delta);
                                    else
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.grey, Color.black, delta);
                                    break;
                                case 7:
                                    circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.green, Color.black, delta);
                                    break;
                                case 8:
                                    if (firstTrackPattern[10].Contains(j))
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.blue, Color.black, delta);
                                    else
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(new Color(255 / 256f, 215 / 256f, 0 / 256f, 1f), Color.black, delta);
                                    break;
                            }
                        }
                        yield return null;
                    }
                }
                break;

            case 3:
                for (int i = 0; i < solveSpeed[k].Length; i++)
                {
                    delta = 0f;
                    while (delta < 1f)
                    {
                        delta += Time.deltaTime * solveSpeed[k][i];
                        for (int j = 0; j < circles.Length; j++)
                        {
                            switch (i)
                            {
                                case 0:
                                    circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.white, Color.black, delta);
                                    break;
                                case 1:
                                    circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.grey, Color.black, delta);
                                    break;
                                default:
                                    if (i % 2 == 0) { final = new Color(0f, 1f, 0f, 1f); c = final / 2; }
                                    if (firstTrackPattern[i/2 - 1].Contains(j))
                                    {
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(final, final - c, delta); 
                                    }
                                    else
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.grey, Color.black, delta);
                                    break;
                                case 14:
                                case 15:
                                    if (i % 2 == 0) { final = new Color(0f, 1f, 0f, 1f); c = final / 2; }
                                    if (j % 6 == 0 || j % 6 == 5 || j < 6 || j > 29 || j == 14 || j == 15 || j == 20 || j == 21)//Border pattern with 2 by 2 center
                                    {
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(final, final - c, delta);
                                    }
                                    else
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.grey, Color.black, delta);
                                    break;
                                case 16:
                                    if (firstTrackPattern[10].Contains(j))
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.blue, Color.black, delta);
                                    else
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(new Color(255 / 256f, 215 / 256f, 0 / 256f, 1f), Color.black, delta);
                                    break;
                            }
                        }
                        yield return null;
                    }
                    if (i >= 2)
                        final -= c;
                }
                break;

            case 4:
                for (int i = 0; i < solveSpeed[k].Length; i++)
                {
                    delta = 0f;
                    while (delta < 1f)
                    {
                        delta += Time.deltaTime * solveSpeed[k][i];
                        rnd = UnityEngine.Random.Range(0.1f, 0.5f);
                        c = new Color(rnd, rnd, rnd, 1f);
                        for (int j = 0; j < circles.Length; j++)
                        {
                            switch (i)
                            {
                                case 0:
                                    circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.white, Color.black, delta);
                                    break;
                                default:
                                    if (firstTrackPattern[i - 1].Contains(j))
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.green - c, Color.black, delta);
                                    else
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.grey - c, Color.black, delta);
                                    break;
                                case 7:
                                    if (firstTrackPattern[10].Contains(j))
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.blue - c, Color.black, delta);
                                    else
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(new Color(255 / 256f, 215 / 256f, 0 / 256f, 1f), Color.black, delta);
                                    break;
                            }
                        }
                        yield return null;
                    }
                }
                break;

            case 5:
                for (int i = 0; i < solveSpeed[k].Length; i++)
                {
                    delta = 0f;
                    while (delta < 1f)
                    {
                        delta += Time.deltaTime * solveSpeed[k][i];
                        rnd = UnityEngine.Random.Range(0.1f, 0.3f);
                        c = new Color(rnd, rnd, rnd, 1f);
                        for (int j = 0; j < circles.Length; j++)
                        {
                            switch (i)
                            {
                                case 0:
                                    circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.white - c, Color.black + c, delta);
                                    break;
                                case 1:
                                    circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.red - c, Color.black + c, delta);
                                    break;
                                case 2:
                                    circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.green - c, Color.black + c, delta);
                                    break;
                                case 3:
                                    circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.blue - c, Color.black + c, delta);
                                    break;
                                default:
                                    if (i > 3 && i < 7 && firstTrackPattern[i - 4].Contains(j) || i > 10 && i < 14 && firstTrackPattern[i - 8].Contains(j))
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.green - c, Color.black + c, delta);
                                    else
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.black + c, Color.black + c, delta);
                                    break;
                                case 7:
                                    circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.black + c, Color.black + c, delta);
                                    break;
                                case 8:
                                    circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.white - c, Color.black + c, delta);
                                    break;
                                case 9:
                                    circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(new Color(255 / 256f, 215 / 256f, 0 / 256f, 1f), Color.black, delta);
                                    break;
                                case 10:
                                    if (firstTrackPattern[10].Contains(j))
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.blue - c, Color.black + c, delta);
                                    break;
                                case 14:
                                    if (firstTrackPattern[10].Contains(j))
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.blue - c, Color.black, delta);
                                    else
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(new Color(255 / 256f, 215 / 256f, 0 / 256f, 1f), Color.black, delta);
                                    break;
                            }
                        }
                        yield return null;
                    }
                }
                break;

            case 6:
                for (int i = 0; i < solveSpeed[k].Length; i++)
                {
                    var flash = new List<Color>() { Color.red, Color.blue, Color.green, Color.yellow, Color.cyan, Color.magenta };
                    Color rndFlash = flash[UnityEngine.Random.Range(0, flash.Count())];
                    delta = 0f;
                    while (delta < 1f)
                    {
                        delta += Time.deltaTime * solveSpeed[k][i];
                        for (int j = 0; j < circles.Length; j++)
                        {
                            switch (i)
                            {
                                case 0:
                                    rnd = UnityEngine.Random.Range(0f, 0.2f);
                                    c = new Color(rnd, rnd, rnd, 1f);
                                    circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.white - c, Color.black + c, delta);
                                    break;
                                default:
                                    if (i > 12)
                                    {

                                        rnd = UnityEngine.Random.Range(-0.3f - (i - 13) * 0.05f, 0.3f + (i - 13) * 0.05f );//Making the glitches more apparent over time
                                        c = new Color(rnd, rnd, rnd, 1f);
                                        if (firstTrackPattern[i - 13].Contains(j))
                                            circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.green + c;
                                        else
                                            circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.black + c;
                                    }
                                    else
                                    {
                                        rnd = UnityEngine.Random.Range(-0.4f, 0.4f);
                                        c = new Color(rnd, rnd, rnd, 1f);
                                        if (i % 2 == 0)
                                        {
                                            if ((j / 6 % 2 == 0 && j % 6 % 2 == 0) || (j / 6 % 2 != 0 && j % 6 % 2 != 0))
                                                circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(rndFlash, Color.black, delta) + c; 
                                            else
                                                circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.black;
                                        }
                                        else
                                        {
                                            if ((j / 6 % 2 != 0 && j % 6 % 2 == 0) || (j / 6 % 2 == 0 && j % 6 % 2 != 0))
                                                circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(rndFlash, Color.black, delta) + c; 
                                            else
                                                circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.black;
                                        }
                                    }
                                    break;
                                case 22:
                                    if (firstTrackPattern[10].Contains(j))
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.blue, Color.black, delta);
                                    else
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(new Color(255 / 256f, 215 / 256f, 0 / 256f, 1f), Color.black, delta);
                                    break;
                            }
                        }
                        yield return null;
                    }
                }
                break;

            case 7:
                for (int i = 0; i < solveSpeed[k].Length; i++)
                {
                    delta = 0f;
                    while (delta < 1f)
                    {
                        delta += Time.deltaTime * solveSpeed[k][i];
                        for (int j = 0; j < circles.Length; j++)
                        {
                            switch (i)
                            {
                                case 0:
                                    circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.white, Color.black, delta);
                                    break;

                                case 1:
                                case 2:
                                case 3:
                                    circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.green, Color.black, delta);
                                    break;

                                case 4:
                                case 5:
                                case 6:
                                case 7:
                                    circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.green, Color.gray, delta);
                                    break;

                                case 8:
                                    rnd = UnityEngine.Random.Range(-1.0f, 1.0f);
                                    c = new Color(rnd, rnd, rnd, 1f);
                                    circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.green - c, Color.black, delta);
                                    break;
                                default:
                                    circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.white, Color.black, delta);
                                    break;
                                case 10:
                                case 11:
                                case 12:
                                    if (j / 12 == i - 10)
                                    {
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.blue, Color.black, delta);
                                    }
                                    else
                                    {
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.black, Color.black, delta);
                                    }
                                    break;
                                case 13:
                                case 14:
                                    if (firstTrackPattern[i - 2].Contains(j))
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(new Color(80 / 256f, 200 / 256f, 120 / 256f, 1f), Color.black, delta);
                                    else
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(new Color(34 / 256f, 139 / 256f, 34 / 256f, 1f), Color.black, delta);
                                    break;
                                case 15:
                                case 16:
                                case 17:
                                    if (j / 12 == 17 - i)
                                    {
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.green, Color.black, delta);
                                    }
                                    else
                                    {
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.black, Color.black, delta);
                                    }
                                    break;
                                case 19:
                                case 20:
                                case 21:
                                    if ((j % 6) / 2 == i - 19)
                                    {
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.yellow, Color.black, delta);
                                    }
                                    else
                                    {
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.black, Color.black, delta);
                                    }
                                    break;
                                case 22:
                                    circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(new Color(255 / 256f, 215 / 256f, 0 / 256f, 1f), Color.black, delta);
                                    break;
                                case 23:
                                    if (firstTrackPattern[10].Contains(j))
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.blue, Color.black, delta);
                                    else
                                        circles[j].transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.color = Color.Lerp(new Color(255 / 256f, 215 / 256f, 0 / 256f, 1f), Color.black, delta);
                                    break;
                            }
                        }
                        yield return null;
                    }
                }
                break;

            default:
                yield return null;
                break;
        }
    }

    //Twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"<!{0} select X#> to select a circle, where X is a letter denoting column and # is a number denoting row, <!{0} select #> to select the #th circle, starting from top left as 1, counted in reading order, <!{0} start> to press the start button, note that you can only select one circle at a time";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant().Trim();
        string[] parameters = command.Split(' ');
        yield return null;
        if (Regex.IsMatch(command, @"^\s*start\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (isAnimating) { yield return "sendtochat This command isn't processed because the module is currently jammin' (performing an animation) right now."; yield break; }
            startButton.OnInteract();
        }
        else if (Regex.IsMatch(parameters[0], @"^\s*select\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (parameters.Length > 2) { yield return "sendtochat I'm sorry sweetie, but you can't select more than two circles and/or send something intelligible in one command!"; yield break; }
            if (parameters.Length < 2) { yield return "sendtochat I'm sorry sweetie, but you gotta specify which circle to select!"; yield break; }
            if (isAnimating) { yield return "sendtochat This command isn't processed because the module is currently jammin' (performing an animation) right now."; yield break; }
            int n = 0;
            bool c = int.TryParse(parameters[1], out n);
            if (c)
            {
                if (n < 1 || n > 36) { yield return "sendtochat I'm sorry sweetie, but there's no such thing as Circle #" + n + " in the module! Mind try again?"; yield break; }
                circles[n - 1].GetComponent<KMSelectable>().OnInteract();
                yield return null;
            }
            else
            {
                if (parameters[1].Length != 2 || !"abcdef".Contains(parameters[1][0]) || !"123456".Contains(parameters[1][1]))
                {
                    yield return "sendtochat I'm sorry sweetie, but I have no idea what you're trying to select! Mind try again?"; yield break;
                }
                circles["abcdef".IndexOf(parameters[1][0]) + "123456".IndexOf(parameters[1][1])*6].GetComponent<KMSelectable>().OnInteract();
            }
        }
        else
        {
            yield return "sendtochat I'm sorry sweetie, but I have no idea what are you trying to do! Mind try again?"; yield break;
        }
        yield return null;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!moduleSolved)
        {
            if (isAnimating) { yield return null; }
            else if (!activated) { startButton.OnInteract(); yield return null; }
            else
            {
                for (int i = 0; i < correctCircles.Length; i++)
                {
                    circles[correctCircles[i]].GetComponent<KMSelectable>().OnInteract();
                    yield return null;
                }
            }
        }
        yield return null;
    }

}
