using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class VersionWebRequester : MonoBehaviour {

  public string GameVersion = "1.0";
  const string VersionURL = "https://raw.githubusercontent.com/mmmmmapplepie/Wordy-Balloons/refs/heads/main/version.txt";


  public void CheckVersionCoroutine() {
    StartCoroutine(CheckVersionRoutine());
  }
  public IEnumerator CheckVersionRoutine() {
    Debug.LogWarning("Coroutine method");
    UnityWebRequest www = UnityWebRequest.Get(VersionURL);
    yield return www.SendWebRequest();

    if (www.result != UnityWebRequest.Result.Success) {
      Debug.LogError("Failed to check version: " + www.error);
      yield break;
    }

    // Get the version string from the text file
    string latestVersion = www.downloadHandler.text.Trim();
    print(latestVersion);

    if (Application.version != latestVersion) {
      // Disable multiplayer buttons or show update popup
      Debug.Log($"New version available! Current: {Application.version} Latest: {latestVersion}");
    } else {
      Debug.Log("Version is up to date.");
    }
  }

  public async void CheckVersionAsync() {
    Debug.LogWarning("async method");
    string version = "";
    try {
      version = await CheckVersionAsyncMethod();
    } catch (Exception e) {
      print(e);
    }
    print(version);
  }
  public async Task<string> CheckVersionAsyncMethod() {
    using UnityWebRequest www = UnityWebRequest.Get(VersionURL);
    var operation = www.SendWebRequest();

    // Await until the request is done
    while (!operation.isDone)
      await Task.Yield();

    if (www.result != UnityWebRequest.Result.Success) {
      Debug.LogError("Failed to check version: " + www.error);
      return "";
    }

    string latestVersion = www.downloadHandler.text.Trim();
    print(latestVersion);

    if (Application.version != latestVersion) {
      Debug.Log($"New version available! Current: {Application.version} Latest: {latestVersion}");
      // Disable buttons or show update popup
    } else {
      Debug.Log("Version is up to date.");
    }
    return latestVersion;
  }


}
