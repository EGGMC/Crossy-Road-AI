﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CustomDefinitions;

public class GameTreeAI {

    private GameState gameState;
    public bool Moved = true;

    private Collider playerCollider = new Collider();
    private List<Collider> carColliders = null;
    private List<Collider> logColliders = null;

    private float lookRadius = 2;
    private float AIMoveInterval;
    private int depthSetting;

    public GameTreeAI(GameState gameState, int depthSetting, float AIMoveInterval)
    {
        this.gameState = gameState;
        this.depthSetting = depthSetting;
        this.AIMoveInterval = AIMoveInterval;
    }

    public void FindBestMove()
    {
        // initialization for the colliders
        playerCollider = gameState.GetPlayer();
        carColliders = gameState.GetCarColliders(playerCollider, lookRadius);
        logColliders = gameState.GetLogColliders(playerCollider, lookRadius);

        var depth = depthSetting;
        var bestMove = recurseFunction(0, depth, playerCollider.transform.position, true);
        var move = (Direction)System.Enum.Parse(typeof(Direction), bestMove[1]);
        movePlayer(move);
        GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerControl>().clip();

        manualMoveAllObjects();

        clearStates(); // after finding a best move, clear all states and refind another

        Moved = true;
    }

    private void manualMoveAllObjects()
    {
        var logs = GameObject.FindGameObjectsWithTag("Log");
        var cars = GameObject.FindGameObjectsWithTag("Car");
        var logSpawners = GameObject.FindGameObjectsWithTag("LogSpawner");
        var carSpawners = GameObject.FindGameObjectsWithTag("CarSpawner");

        foreach (var log in logs)
            log.GetComponent<AutoMoveObjects>().ManualMove(AIMoveInterval);
        foreach (var car in cars)
            car.GetComponent<AutoMoveObjects>().ManualMove(AIMoveInterval);
        foreach (var s in logSpawners)
            s.GetComponent<Spawner>().manualUpdate(AIMoveInterval);
        foreach (var s in carSpawners)
            s.GetComponent<Spawner>().manualUpdate(AIMoveInterval);
    }

    List<string> recurseFunction(int agentIndex, int depth, Vector3 currentPosition, bool firstLayer)
    {
        if (gameState.isPlayerDead(playerCollider, logColliders, carColliders))
        {
            //GetScore Need to do a negative score!
            List<string> returnList = new List<string>();
            returnList.Add(-99999 + "");
            returnList.Add("");
            return returnList;
        };
        GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerControl>().clip(playerCollider.transform);
        if (gameState.isPlayerDead(playerCollider, logColliders, carColliders))
        {
            //GetScore Need to do a negative score!
            List<string> returnList = new List<string>();
            returnList.Add(-99999 + "");
            returnList.Add("");
            return returnList;
        };
        if (depth == 0)
        {
            //GetScore
            var score = gameState.GetScore(playerCollider) + "";
            List<Direction> actions = findAvailableMoves();

            List<string> returnList = new List<string>();
            if (actions.Count == 0)
            {
                returnList.Add(-1000 + "");
            }
            else if (int.Parse(score) < (int)currentPosition.z)
            {
                returnList.Add((int)currentPosition.z + "");
            }
            else
            {
                //returnList.Add(score);
                if (((int)playerCollider.transform.position.x) == 0)
                {
                    returnList.Add(score);
                }
                else
                {
                    returnList.Add((int)(int.Parse(score) * (1 / UnityEngine.Mathf.Abs((int)playerCollider.transform.position.x))) + "");
                }
            }
            returnList.Add("");
            return returnList;
        }

        if (agentIndex == 0)
        {
            List<Direction> actions = findAvailableMoves();
            List<List<string>> varminmax = new List<List<string>>();
            Vector3 currPlayerPos = playerCollider.transform.position;

            foreach (Direction dir in actions)
            {
                movePlayer(dir, true);
                List<string> value = new List<string>();
                //This can be removed!
                var oldScore = gameState.GetScore(playerCollider);
                value.Add(recurseFunction(agentIndex + 1, depth, currentPosition, false)[0]);
                value.Add(dir.ToString());
                varminmax.Add(value);
                playerCollider.transform.position = currPlayerPos;
            }
            List<string> highestValue = new List<string>();
            highestValue = varminmax[0]; //
            List<List<string>> allHighest = new List<List<string>>();
            //SET TO HIGHEST BEFORE FINDING MAX
            foreach (var value in varminmax)
            {
                int currScore = int.Parse(value[0]);
                int highScore = int.Parse(highestValue[0]);

                if (currScore > highScore)
                {
                    highestValue = value;
                    allHighest.Clear();
                    allHighest.Add(value);
                }
                else if (currScore == highScore)
                {
                    allHighest.Add(value);
                }
            }
            int index = Random.Range(0, allHighest.Count);

            return allHighest[index];
        }
        else
        {
            List<Vector3> prevCarPositions = new List<Vector3>();
            List<Vector3> prevLogPositions = new List<Vector3>();

            foreach (var car in carColliders)
            {
                prevCarPositions.Add(car.transform.position);
            }

            foreach (var log in logColliders)
            {
                prevLogPositions.Add(log.transform.position);
            }

            gameState.SetNextState(carColliders, AIMoveInterval);
            gameState.SetNextState(logColliders, AIMoveInterval);
            //recurse
            List<string> value = new List<string>();
            value.Add(recurseFunction(0, depth - 1, currentPosition, false)[0]);
            value.Add("ENEMIES_MOVE");

            //backtrack
            for (int i = 0; i < carColliders.Count; i++)
            {
                carColliders[i].transform.position = prevCarPositions[i];
            }
            for (int i = 0; i < logColliders.Count; i++)
            {
                logColliders[i].transform.position = prevLogPositions[i];
            }
            return value;
        }
    }


    void movePlayer(Direction dir, bool isTestPlayer = false)
    {
        var trans = (isTestPlayer) ? playerCollider.transform : null;

        switch (dir)
        {
            case Direction.FRONT:
                GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerControl>().MoveForward(trans);
                break;
            case Direction.BACK:
                GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerControl>().MoveBackward(trans);
                break;
            case Direction.RIGHT:
                GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerControl>().MoveRight(trans);
                break;
            case Direction.LEFT:
                GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerControl>().MoveLeft(trans);
                break;
            default:
                break;
        }
    }

    List<Direction> findAvailableMoves()
    {
        return GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerControl>().GetAvailableMoves(playerCollider);
    }

    // CRITICAL! RUN THIS AFTER FINDING A BEST MOVE
    void clearStates()
    {
        gameState.Clean(playerCollider, logColliders, carColliders);
        playerCollider = null;
        logColliders = null;
        carColliders = null;
    }
}
