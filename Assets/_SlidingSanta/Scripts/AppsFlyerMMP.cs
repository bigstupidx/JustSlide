﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppsFlyerMMP : MonoBehaviour
{

    void Start()
    {
        /* Mandatory - set your AppsFlyer’s Developer key. */
        AppsFlyer.setAppsFlyerKey("aTYJZVwsYCTz8BbnbrDbxL");
        /* For detailed logging */
        /* AppsFlyer.setIsDebug (true); */
    #if UNITY_IOS
            /* Mandatory - set your apple app ID
               NOTE: You should enter the number only and not the "ID" prefix */
            AppsFlyer.setAppID("1347027671");
            AppsFlyer.trackAppLaunch();
    #elif UNITY_ANDROID
           /* Mandatory - set your Android package name */
           AppsFlyer.setAppID ("YOUR_ANDROID_PACKAGE_NAME_HERE");
           /* For getting the conversion data in Android, you need to add the "AppsFlyerTrackerCallbacks" listener.*/
           AppsFlyer.init ("YOUR_DEV_KEY","AppsFlyerTrackerCallbacks");
    #endif
    }

    public static void Score(int scoreValue)
    {
        Dictionary<string, string> score = new Dictionary<string, string>();
        score.Add("score", scoreValue.ToString());
        AppsFlyer.trackRichEvent("score", score);
        Debug.Log("AppsFlyer score Passed");
    }

    public static void HighScore()
    {
        Dictionary<string, string> score = new Dictionary<string, string>();
        score.Add("high_score_passed", "1");
        AppsFlyer.trackRichEvent("high_score", score);
        Debug.Log("AppsFlyer High Score Passed");
    }


}
