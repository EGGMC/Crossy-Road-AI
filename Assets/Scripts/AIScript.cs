﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CustomDefinitions;

public class AIScript : MonoBehaviour {

    public GameState gameState;
    public bool AIEnabled;

    public bool init = false;
    public bool setNextState = false;
    public bool clear = false;
    public bool forward = false;
    public bool back = false;
    public bool left = false;
    public bool right = false;
    public bool getBestMove = false;

    public int depthSetting;

    private float lookRadius = 2;

    private Collider playerCollider = new Collider();
    private List<Collider> carColliders = null;
    private List<Collider> logColliders = null;

	public float AIMoveInterval = 0.1f;
	private float currInterval;
	private Settings settings;

    // Use this for initialization
    void Start() {
    	currInterval = AIMoveInterval;
        settings = GameObject.FindObjectOfType<Settings>().GetComponent<Settings>();
    }

	// Update is called once per frame
	void Update () {
        if (AIEnabled)
        {
            settings.setAutoMove(false);
            settings.setIsAI(true);

            if (currInterval < 0)
            {
				Debug.Log("Beauty is in the eye of the beholder");
                findBestMove();
                currInterval = AIMoveInterval;
            }
            currInterval -= Time.deltaTime;

            if (init)
            {
                init = false;

                playerCollider = gameState.GetPlayer();
                carColliders = gameState.GetCarColliders(playerCollider, lookRadius);
                logColliders = gameState.GetLogColliders(playerCollider, lookRadius);
            }

            if (setNextState)
            {
                setNextState = false;
                gameState.SetNextState(carColliders);
                gameState.SetNextState(logColliders);
            }

            if (clear)
            {
                clear = false;
                clearStates();
            }

            if (forward)
            {
                forward = false;
                if (playerCollider != null) GetComponent<PlayerControl>().MoveForward(playerCollider.transform);
            }

            if (back)
            {
                back = false;
                if (playerCollider != null) GetComponent<PlayerControl>().MoveBackward(playerCollider.transform);
            }

            if (right)
            {
                right = false;
                if (playerCollider != null) GetComponent<PlayerControl>().MoveRight(playerCollider.transform);
            }

            if (left)
            {
                left = false;
                if (playerCollider != null) GetComponent<PlayerControl>().MoveLeft(playerCollider.transform);
            }

			if (getBestMove)
            {
				getBestMove = false;
                findBestMove();
            }
        } else
        {
            settings.setAutoMove(true);
            settings.setIsAI(false);
        }
	}

    void findBestMove()
    {
        // find best move using expectimax?
        // initialization for the colliders
        playerCollider = gameState.GetPlayer();
        carColliders = gameState.GetCarColliders(playerCollider, lookRadius);
        logColliders = gameState.GetLogColliders(playerCollider, lookRadius);

		Vector3 prevPlayerPos = playerCollider.transform.position;
		List<Vector3> prevCarPositions = new List<Vector3>();
		List<Vector3> prevLogPositions = new List<Vector3>();

		foreach (var car in carColliders) {
			prevCarPositions.Add (car.transform.position);
		}

		foreach (var log in logColliders) {
			prevLogPositions.Add (log.transform.position);
		}

		//backtrack
		playerCollider.transform.position = prevPlayerPos;
		for (int i = 0; i < carColliders.Count; i++) {
			carColliders [i].transform.position = prevCarPositions [i];
		}
		for (int i = 0; i < logColliders.Count; i++) {
			logColliders [i].transform.position = prevLogPositions [i];
		}
		var depth = depthSetting;
		var bestMove = recurseFunction(0, depth, prevPlayerPos);
		var move = (Direction)System.Enum.Parse (typeof(Direction), bestMove [1]);
		movePlayer (move);

        manualMoveAllObjects();

        clearStates(); // after finding a best move, clear all states and refind another
    }

    private void manualMoveAllObjects()
    {
        var logs = GameObject.FindGameObjectsWithTag("Log");
        var cars = GameObject.FindGameObjectsWithTag("Car");
		System.Diagnostics.Stopwatch StopWatch = new System.Diagnostics.Stopwatch();
		StopWatch.Start();
        foreach (var log in logs)
            log.GetComponent<AutoMoveObjects>().ManualMove(AIMoveInterval);
        foreach (var car in cars)
            car.GetComponent<AutoMoveObjects>().ManualMove(AIMoveInterval);
		StopWatch.Stop();
		// Get the elapsed time as a TimeSpan value.
		var ts = StopWatch.Elapsed;
		// Format and display the TimeSpan value.
//		string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
//			ts.Hours, ts.Minutes, ts.Seconds,
//			ts.Milliseconds / 10);
		Debug.Log("RunTime " + ts);
		Debug.Log("KORE'");
    }


	List<string> recurseFunction(int agentIndex, int depth, Vector3 currentPosition) {
		if (gameState.isPlayerDead(playerCollider,logColliders,carColliders))
		{
            //GetScore Need to do a negative score!
			List<string> returnList = new List<string>();
			returnList.Add(-99999 + "");
			returnList.Add("");
			return returnList;
		};
		if (depth == 0) {
			//GetScore
			var score = gameState.GetScore(playerCollider) + "";
			List<Direction> actions = findAvailableMoves();

			List<string> returnList = new List<string>();
//			Debug.Log("fuck fuck " + currentPosition.x);
			if (actions.Count == 0) {
				Debug.Log("supreme");
				returnList.Add(-1000 + "");
			} /*else if ((int)playerCollider.transform.position.x == 4 || (int)playerCollider.transform.position.x == -4) {
				returnList.Add(-1000 + "");
			}*/ else if (int.Parse(score) < (int)currentPosition.z) {
				returnList.Add((int)currentPosition.z + "");
			} else {
				//returnList.Add(score);
				if (((int)playerCollider.transform.position.x) == 0) {
					returnList.Add(score);
				} else {
					returnList.Add((int)(int.Parse(score) * (1 / UnityEngine.Mathf.Abs((int)playerCollider.transform.position.x))) + "");
				}
			}
			returnList.Add("");
			return returnList;
		}

		if (agentIndex == 0){
			List<Direction> actions = findAvailableMoves();
			List<List<string>> varminmax = new List<List<string>>();
			Vector3 currPlayerPos = playerCollider.transform.position;

			foreach(Direction dir in actions)
			{
				movePlayer(dir, true);
				List<string> value = new List<string> ();
				//This can be removed!
				var oldScore = gameState.GetScore(playerCollider);
				value.Add (recurseFunction (agentIndex + 1, depth, currentPosition) [0]);
				value.Add (dir.ToString());
				varminmax.Add(value);
				playerCollider.transform.position = currPlayerPos;
			}
			List<string> highestValue = new List<string>();
			highestValue = varminmax [0]; //
			List<List<string>> allHighest = new List<List<string>>();
			//SET TO HIGHEST BEFORE FINDING MAX
			foreach (var value in varminmax)
			{
				int currScore = int.Parse(value[0]);
				int highScore = int.Parse(highestValue[0]);

				if (currScore > highScore) {
					highestValue = value;
					allHighest.Clear ();
					allHighest.Add (value);
				} else if (currScore == highScore) {
					allHighest.Add (value);
				}
			}
			int index = Random.Range (0, allHighest.Count);
			return allHighest[index];
		} else {
			List<Vector3> prevCarPositions = new List<Vector3>();
			List<Vector3> prevLogPositions = new List<Vector3>();

			foreach (var car in carColliders) {
				prevCarPositions.Add (car.transform.position);
			}

			foreach (var log in logColliders) {
				prevLogPositions.Add (log.transform.position);
			}


			gameState.SetNextState (carColliders);
			gameState.SetNextState (logColliders);
			//recurse
			List<string> value = new List<string> ();
			value.Add (recurseFunction (0, depth - 1, currentPosition) [0]);
			value.Add("ENEMIES_MOVE");

			//backtrack
			for (int i = 0; i < carColliders.Count; i++) {
				carColliders [i].transform.position = prevCarPositions [i];
			}
			for (int i = 0; i < logColliders.Count; i++) {
				logColliders [i].transform.position = prevLogPositions [i];
			}
			return value;
		}
	}


	void movePlayer(Direction dir, bool isTestPlayer = false)
	{
		var trans = (isTestPlayer) ? playerCollider.transform : null;

		switch (dir) {
			case Direction.FRONT:
				GetComponent<PlayerControl>().MoveForward(trans);
				break;
			case Direction.BACK:
				GetComponent<PlayerControl>().MoveBackward(trans);
				break;
			case Direction.RIGHT:
				GetComponent<PlayerControl>().MoveRight(trans);
				break;
			case Direction.LEFT:
				GetComponent<PlayerControl>().MoveLeft(trans);
				break;
			default:
				break;
		}
	}

	List<Direction> findAvailableMoves()
    {
        return GetComponent<PlayerControl>().GetAvailableMoves(playerCollider);
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
