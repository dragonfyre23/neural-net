﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vectrosity;

public class CurveFit2DCtrl : MonoBehaviour {

    public int numHiddenLayers; //NN dimensions
    public int hiddenLayerSize; //

    public int populationSize;          //total number of individuals
    public int numParents;              //number of parents per reproduction set
    public int numCrossoverPoints;      //number of crossover points in genetic combination. Will be used in GeneticIndividual.Reproduce(). should be x >= numParents.
    public float mutationChance;        //from 0 to 1. measure of genetic diversity. 0 is no mutation. 1 will cause an infinite loop after changing every weight.
    public float environmentalPressure; //from 0 to 1. 1 is no survivors
    public float eliteFraction;         //number of solutions to save from one generation to the next 0 is none 1 is all saved. (should never be > 1-environmentalPressure then errors) (if > like .2 then GA won't work well)
    public int tournamentSize;          //size of the randomly chosen subset from which the most fit individual will be chosen for reproduction.  must be 1 <= x <= populationSize.

    private GeneticAlgorithm ga;

    private bool isGAInitialized = false;
    public bool isRunning = false;
    public bool isFitnessControl = false;

    public int targetGeneration = 0;
    public float targetFitness = 0;

    public int numPoints;   //
    public int numTestPoints;   //

    private double[,] testInputSets;
    private double[,] testOutputSets;

    public string function;//name of function to be approximated

    public NeuralNet bestNet;//the highest fitness solution

    public VisualNet visualNet;
    public float vNetXArea;//area devoted to the net visualization
    public float vNetYArea;

    public GameObject configPanel;//fields to be filled by unity editor

    public InputField populationField;
    public InputField numParentsField;
    public InputField numCrossoverPointsField;
    public InputField mutationChanceField;
    public InputField environmentalPressureField;
    public InputField eliteFractionField;
    public InputField tournamentSizeField;

    public InputField numHiddenLayersField;
    public InputField hiddenLayerSizeField;
    public InputField numPointsField;
    public InputField numTestPointsField;
    public Dropdown functionDropdown;//end fields

    public Text functionContent;//hud field

    public Text fitnessContent;

    public GameObject configDoneButton;
    public GameObject configCloseButton;

    public GameObject HUDContainer;

    public InputField directoryField;
    public InputField filenameField;

    public Button pauseButton;
    public Button runButton;

    public Toggle subtractToggle;

    public Text currentGenerationContent;

    public Button generationControlButton;
    public Button fitnessControlButton;

    public InputField targetGenerationField;
    public InputField targetFitnessField;

    private VectorLine targetFunctionLine;
    private VectorLine NNFunctionLine;
    public Vector2 functionsOrigin;
    public Vector2 functionsScale;

    public InputField NNInputField;
    public Text NNOutputContent;

    private VectorLine fitnessHistoryLine;
    public Vector2 fitnessHistoryOrigin;
    public Vector2 fitnessHistoryScale;
    private float fitnessHistoryXStep;

    private List<float> fitnessRecord = new List<float>();


    // Use this for initialization
    void Start()
    {
        Screen.SetResolution(960, 540, false);
        configPanel.SetActive(true);
        VectorLine.canvas.sortingOrder = -1;
    }

    // Update is called once per frame
    void Update()
    {
        if (isGAInitialized)
        {
            currentGenerationContent.text = ga.generationCount.ToString();
            if (int.Parse(currentGenerationContent.text) > targetGeneration)
            {
                targetGeneration = int.Parse(currentGenerationContent.text);
                targetGenerationField.text = currentGenerationContent.text;
            }
            if (isRunning)
            {
                if (isFitnessControl)//fitness control
                {
                    if (bestNet.Fitness() < targetFitness)
                    {
                        //call training ga
                        bestNet = (NeuralNet)ga.TrainGeneration(1);
                        //update fitness graph
                        fitnessContent.text = bestNet.Fitness().ToString();
                        //TODO
                        //update vnet net
                        visualNet.net = bestNet;
                        //update function curves
                        drawNNFunction();
                        //updateFitnessHistoryGraph
                        drawFitnessHistory();
                    }
                }
                else//generation control
                {
                    if (int.Parse(currentGenerationContent.text) < targetGeneration)
                    {
                        //call training ga
                        bestNet = (NeuralNet)ga.TrainGeneration(1);
                        //update fitness graph
                        fitnessContent.text = bestNet.Fitness().ToString();
                        //TODO
                        //update vnet net
                        visualNet.net = bestNet;
                        //update function curves
                        drawNNFunction();
                        //updateFitnessHistoryGraph
                        drawFitnessHistory();
                    }
                }
            }
        }
    }

    private void setupUI()
    {
        //pull info from input fields
        //ga
        populationSize = int.Parse(populationField.text);
        numParents = int.Parse(numParentsField.text);
        numCrossoverPoints = int.Parse(numCrossoverPointsField.text);
        mutationChance = float.Parse(mutationChanceField.text);
        environmentalPressure = float.Parse(environmentalPressureField.text);
        eliteFraction = float.Parse(eliteFractionField.text);
        tournamentSize = int.Parse(tournamentSizeField.text);
        //nn
        numHiddenLayers = int.Parse(numHiddenLayersField.text);
        hiddenLayerSize = int.Parse(hiddenLayerSizeField.text);
        numPoints = int.Parse(numPointsField.text);
        numTestPoints = int.Parse(numTestPointsField.text);

        switch (functionDropdown.value)//convert dropdown value to function name
        {
            case 0:
                function = "constant";
                break;
            case 1:
                function = "linear";
                break;
            case 2:
                function = "negativeLinear";
                break;
            case 3:
                function = "piecewiseLinear";
                break;
            case 4:
                function = "quadratic";
                break;
            case 5:
                function = "cubic";
                break;
            case 6:
                function = "squareRoot";
                break;
            case 7:
                function = "sine";
                break;
            case 8:
                function = "cosine";
                break;
            default:
                function = "null";
                break;
        }

        functionContent.text = function;

        //generate test inputs and outputs from correct function and based on numTestPoints
        testInputSets = new double[numTestPoints, 1];
        testOutputSets = new double[numTestPoints, 1];
        for (int i = 0; i <= numTestPoints-1; i++)
        {
            testInputSets[i, 0] = (float)i / (float)(numTestPoints - 1f);
            testOutputSets[i, 0] = functionEvaluate((float)testInputSets[i, 0]);
        }

        NeuralNet net = new NeuralNet(1, 1, numHiddenLayers, hiddenLayerSize, testInputSets, testOutputSets);//create net with test sets filled
        ga = new GeneticAlgorithm(net, populationSize, numParents, environmentalPressure, eliteFraction, numCrossoverPoints, mutationChance, tournamentSize);
        isGAInitialized = true;
        bestNet = (NeuralNet) ga.individuals[0];
        visualNet.net = bestNet;
        visualNet.layerSeparation = vNetXArea / (numHiddenLayers + 1);
        visualNet.nodeSeparation = vNetYArea / (hiddenLayerSize + 1);
        visualNet.Initialize();

        targetFunctionLine = new VectorLine("targetFunctionLine", new List<Vector2>(), 2.0f, LineType.Continuous);
        NNFunctionLine = new VectorLine("NNFunctionLine", new List<Vector2>(), 2.0f, LineType.Points);
        fitnessHistoryLine = new VectorLine("fitnessHistoryLine", new List<Vector2>(), 2.0f, LineType.Continuous);
        VectorLine fitHistAxesLine = new VectorLine("fitHistAxesLine", new List<Vector2>(), 2.0f, LineType.Continuous);
        fitHistAxesLine.points2.Add(new Vector2(495, 230));
        fitHistAxesLine.points2.Add(new Vector2(495, 25));
        fitHistAxesLine.points2.Add(new Vector2(900, 25));

        drawTargetFunction();
        drawNNFunction();
        drawFitnessHistory();
        fitHistAxesLine.Draw();

        NNInputField.text = "0";
        updateNNOutput();
    }

    public void drawTargetFunction()
    {
        for (int i = 0; i <= numPoints-1; i++)
        {
            float newPointX = ((float)i / (float)(numPoints - 1f));
            float newPointY = functionEvaluate(newPointX);
            targetFunctionLine.points2.Add(new Vector2(functionsOrigin.x + newPointX * functionsScale.x, functionsOrigin.y + newPointY*functionsScale.y));
        }
        
        targetFunctionLine.Draw();
        targetFunctionLine.SetColor(Color.blue);
    }

    public void drawNNFunction()
    {
        NNFunctionLine.points2.Clear();
        for (int i = 0; i < numPoints; i++)
        {
            float newPointX = ((float)i / (float)(numPoints-1));
            float newPointY = (float) bestNet.FeedForward(new double[] { newPointX })[0];
            NNFunctionLine.points2.Add(new Vector2(functionsOrigin.x + newPointX * functionsScale.x, functionsOrigin.y + newPointY * functionsScale.y));
        }
        
        NNFunctionLine.Draw();
        NNFunctionLine.SetColor(Color.red);

        updateNNOutput();
    }

    private float functionEvaluate(float x)
    {
        if (function == "constant") { return 0.5f; }
        else if (function == "linear"){ return x; }
        else if (function == "negativeLinear") { return 1f - x; }
        else if (function == "piecewiseLinear")
        {
            if(x >= 0 && x < 0.5f) { return 2f * x; }
            else if(x >= 0.5f && x < 0.625f) { return 3f - 4f * x; }
            else if(x >= 0.625f && x < 0.75f) { return -0.75f + 2f * x; }
            else if(x >= 0.75f && x <= 1f) { return 2.25f - 2f * x; }
            else { return 0f; }
        }
        else if (function == "quadratic") { return x*x; }
        else if (function == "cubic") { return (0.5f*Mathf.Pow(2f*x-1f, 3))+0.5f; }
        else if (function == "squareRoot") { return Mathf.Sqrt(x); }
        else if (function == "sine") { return 0.5f*Mathf.Sin(2*Mathf.PI*x)+0.5f; }
        else if (function == "cosine") { return 0.5f * Mathf.Cos(2 * Mathf.PI * x) + 0.5f; }
        else {
            Debug.Log("Function Evaluate Error");
            return 0f;
        }
    }

    private void drawFitnessHistory()
    {
        fitnessRecord.Add((float)bestNet.Fitness());
        fitnessHistoryLine.points2.Clear();
        fitnessHistoryXStep = fitnessHistoryScale.x / ((float)fitnessRecord.Count - 1);

        for (int i = 0; i < fitnessRecord.Count; i++)
        {
            float newPointX = fitnessHistoryOrigin.x + (float)i * fitnessHistoryXStep;
            float newPointY = fitnessHistoryOrigin.y + (fitnessRecord[i]+bestNet.numOutputs)*fitnessHistoryScale.y;
            fitnessHistoryLine.points2.Add(new Vector2(newPointX, newPointY));
        }

        if (fitnessHistoryLine.points2.Count >= 2)
        {
            fitnessHistoryLine.Draw();
            fitnessHistoryLine.SetColor(Color.green);
        }

    }

    public void configDone()
    {
        setupUI();
        HUDContainer.SetActive(true);
        configPanel.SetActive(false);
    }

    public void configClose()
    {
        configPanel.SetActive(false);
        HUDContainer.SetActive(true);
    }

    public void showDetails()
    {
        //convert fields to be uneditable
        populationField.readOnly = true;
        numParentsField.readOnly = true;
        numCrossoverPointsField.readOnly = true;
        mutationChanceField.readOnly = true;
        environmentalPressureField.readOnly = true;
        eliteFractionField.readOnly = true;
        tournamentSizeField.readOnly = true;
        numHiddenLayersField.readOnly = true;
        hiddenLayerSizeField.readOnly = true;
        numPointsField.readOnly = true;
        numTestPointsField.readOnly = true;
        functionDropdown.interactable = false;

        //hide hud and show config panel
        HUDContainer.SetActive(false);
        configPanel.SetActive(true);
        configCloseButton.SetActive(true);
        configDoneButton.SetActive(false);
    }

    public void saveAsCSV()
    {
        bestNet.WriteToFile(directoryField.text, filenameField.text);
    }

    public void pause()
    {
        isRunning = false;
        pauseButton.interactable = false;
        runButton.interactable = true;
    }

    public void run()
    {
        isRunning = true;
        pauseButton.interactable = true;
        runButton.interactable = false;
    }

    public void enableGenerationControl()
    {
        isFitnessControl = false;
        fitnessControlButton.interactable = true;
        generationControlButton.interactable = false;
        targetFitnessField.interactable = false;
        targetGenerationField.interactable = true;
    }

    public void enableFitnessControl()
    {
        isFitnessControl = true;
        fitnessControlButton.interactable = false;
        generationControlButton.interactable = true;
        targetFitnessField.interactable = true;
        targetGenerationField.interactable = false;
    }

    public void updateTargetGeneration()
    {
        targetGeneration = int.Parse(targetGenerationField.text);
    }

    public void addGenerations1()
    {
        int n;
        if (subtractToggle.isOn) n = -1;
        else n = 1;
        targetGenerationField.text = (int.Parse(targetGenerationField.text) + n).ToString();
        updateTargetGeneration();
    }

    public void addGenerations10()
    {
        int n;
        if (subtractToggle.isOn) n = -10;
        else n = 10;
        targetGenerationField.text = (int.Parse(targetGenerationField.text) + n).ToString();
        updateTargetGeneration();
    }

    public void addGenerations100()
    {
        int n;
        if (subtractToggle.isOn) n = -100;
        else n = 100;
        targetGenerationField.text = (int.Parse(targetGenerationField.text) + n).ToString();
        updateTargetGeneration();
    }

    public void updateTargetFitness()
    {
        targetFitness = float.Parse(targetFitnessField.text);
    }

    public void updateNNOutput()
    {
        float input;
        if(float.TryParse(NNInputField.text, out input))
        {
            input = float.Parse(NNInputField.text);
        }
        else
        {
            NNInputField.text = "0";
            input = 0;
        }
        float output = (float)bestNet.FeedForward(new double[] { input })[0];
        NNOutputContent.text = output.ToString();
    }
}