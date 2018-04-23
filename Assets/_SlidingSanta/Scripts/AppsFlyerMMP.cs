using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppsFlyerMMP : MonoBehaviour
{

    void Start()
    {
        // For detailed logging
        //AppsFlyer.setIsDebug (true);
#if UNITY_IOS
        //Mandatory - set your AppsFlyer’s Developer key.
        AppsFlyer.setAppsFlyerKey ("aTYJZVwsYCTz8BbnbrDbxL");
        //Mandatory - set your apple app ID
        //NOTE: You should enter the number only and not the "ID" prefix
        AppsFlyer.setAppID ("YOUR_APP_ID_HERE");
        AppsFlyer.trackAppLaunch ();
#elif UNITY_ANDROID
        //Mandatory - set your Android package name
        AppsFlyer.setAppID("com.belizard.justslide");
        //Mandatory - set your AppsFlyer’s Developer key.
        AppsFlyer.init("aTYJZVwsYCTz8BbnbrDbxL");

#endif
    }


    public static void Score(int scoreValue)
    {
        Dictionary<string, string> score = new Dictionary<string, string>();
        score.Add("score", scoreValue.ToString());
        AppsFlyer.trackRichEvent("score", score);
        Debug.Log("AppsFlyer Steps Passed");
    }

    public static void PassHighScore()
    {
        Dictionary<string, string> score = new Dictionary<string, string>();
        score.Add("high_score_passed", "1");
        AppsFlyer.trackRichEvent("high_score_passed", score);
        Debug.Log("AppsFlyer Steps Passed");
    }



}
