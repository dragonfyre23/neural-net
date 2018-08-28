﻿using System;
using System.Collections;
using System.Collections.Generic;


public class Layer
{
    readonly int numNodesPrevious;       //number of nodes in the previous layer
    readonly int numNodes;               //number of nodes in this layer
    Random rand;
    double[,] weights;                  //the weights connecting the previous layer to this layer
    double[] biases;                    //one bias for each node in this layer
    double[] values;                    //values used for feed-forward calculation

    public Layer(int numNodesPrevious, int numNodes)
    {
        weights = new double[numNodes, numNodesPrevious];
        biases = new double[numNodes];
        values = new double[numNodes];
        Rand = new Random();
    }

    public Layer(int numNodesPrevious, int numNodes, Random rand)
    {
        weights = new double[numNodes, numNodesPrevious];
        biases = new double[numNodes];
        values = new double[numNodes];
        this.Rand = rand;
    }

    public Layer(int numNodesPrevious, int numNodes, Random rand, double[,] weights)
    {
        this.weights = weights; ;
        biases = new double[numNodes];
        values = new double[numNodes];
        this.Rand = rand;
    }

    public double[,] Weights
    {
        get
        {
            return weights;
        }

        set
        {
            weights = value;
        }
    }

    public double[] Biases
    {
        get
        {
            return biases;
        }

        set
        {
            biases = value;
        }
    }

    public double[] Values
    {
        get
        {
            return values;
        }

        set
        {
            values = value;
        }
    }

    public Random Rand
    {
        get
        {
            return rand;
        }

        set
        {
            rand = value;
        }
    }

    void RandomizeWeights()             //randomizes biases and weights
    {
        for (int i = 0; i < weights.GetLength(0); i++)
        {
            for (int j = 0; j < weights.GetLength(1); j++)
            {
                weights[i, j] = Rand.NextDouble();
            }           
        }
        for (int i = 0; i < biases.GetLength(0); i++)
        {
            biases[i] = Rand.NextDouble();
        }
    }

    double[] FeedForward(double[] previousValues)
    {
        double sum;                                     //temporary summation variable
        for (int i = 0; i < numNodes; i++)              //iterate once per value to be calculated
        {
            sum = 0;
            sum += biases[i];
            for (int j = 0; j < numNodesPrevious; j++)  //iterate once per weight
            {
                sum += weights[i, j] * previousValues[j];
            }
            sum = Functions.Sigmoid(sum);    //normalize values between 0 and 1
            values[i] = sum;
        }
        return values;
    }

    double CalculateTotalError(double[] error)            //use only for output layer
    {
        double MSE = 0;                                     //mean square error
        for (int i = 0; i < error.GetLength(0); i++)
        {
            MSE += error[i]*error[i];
        }
        MSE *= 1 / (2 * error.GetLength(0));
        return MSE;
    }

    double[,] BackpropagateOutput(double[] targets)     //use only for the output layer //targets must have length equal to numNodes
    {
        double[,] deltaWeights = new double[weights.GetLength(0), weights.GetLength(1)];//same size as weights
        for (int i = 0; i < numNodes; i++)              
        {
            for (int j = 0; j < numNodesPrevious; j++)  //iterate once per weight
            {
                deltaWeights[i, j] = targets[i] - values[i];
            }
        }
        return deltaWeights;
    }

    double[,] BackpropagateHidden()
    {
        double[,] deltaWeights = new double[weights.GetLength(0), weights.GetLength(1)];//same size as weights
        return deltaWeights;
    }

}

public static class Functions
{
    public static double Sigmoid(double x)//is bounded between 0 and 1
    {
        return 1.0 / (1.0 + System.Math.Exp(-x));
    }
    public static double SigmoidDeritive(double x)
    {
        return x * (1 - x);
    }
}