using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class GameMainLoopManager
{
    private static GameMainLoopManager _instance;
    
    public static GameMainLoopManager Instance
    {

        get
        {
            if (_instance == null) _instance = new GameMainLoopManager();
            return _instance;
        }
    }

    public void AddActionDoInMainThread(Action func)
    {
        new Thread(new ThreadStart(func)).Start();
    }
    
    
}

