using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class VersionWebRequester {

  // static string GameVersionTemp = "0.0";
  const string VersionURL = "https://raw.githubusercontent.com/mmmmmapplepie/Wordy-Balloons/refs/heads/main/version.txt";



  public static async void CheckVersionAsync() {
    string version = "";
    try {
      version = await CheckVersionAsyncMethod();
    } catch (Exception e) {
      Debug.LogWarning(e);
    }
  }
  public static async Task<string> CheckVersionAsyncMethod() {
    using UnityWebRequest www = UnityWebRequest.Get(VersionURL);
    var operation = www.SendWebRequest();

    while (!operation.isDone) await Task.Yield();

    if (www.result != UnityWebRequest.Result.Success) {
      throw new Exception("Version check failure");
    }

    string latestVersion = www.downloadHandler.text.Trim();

    // if (GameVersionTemp != latestVersion) {
    // 	return "";
    // }
    if (Application.version != latestVersion) {
      return "";
    }
    return latestVersion;
  }


















  // public void CheckVersionCoroutine(MonoBehaviour mono) {
  // 	mono.StartCoroutine(CheckVersionRoutine());
  // }
  // public IEnumerator CheckVersionRoutine() {
  // 	Debug.LogWarning("Coroutine method");
  // 	UnityWebRequest www = UnityWebRequest.Get(VersionURL);
  // 	yield return www.SendWebRequest();

  // 	if (www.result != UnityWebRequest.Result.Success) {
  // 		Debug.LogError("Failed to check version: " + www.error);
  // 		yield break;
  // 	}

  // 	// Get the version string from the text file
  // 	string latestVersion = www.downloadHandler.text.Trim();
  // 	Debug.Log(latestVersion);

  // 	if (Application.version != latestVersion) {
  // 		// Disable multiplayer buttons or show update popup
  // 		Debug.Log($"New version available! Current: {Application.version} Latest: {latestVersion}");
  // 	} else {
  // 		Debug.Log("Version is up to date.");
  // 	}
  // }

}
