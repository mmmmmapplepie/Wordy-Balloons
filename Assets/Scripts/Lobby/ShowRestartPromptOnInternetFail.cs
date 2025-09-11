using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowRestartPromptOnInternetFail : MonoBehaviour {
  public GameObject restartAdviceTxt;
  void Start() {
    InternetConnectivityCheck.ConnectedStateEvent += ConnEvent;
  }
  void OnDestroy() {
    InternetConnectivityCheck.ConnectedStateEvent -= ConnEvent;
  }
  void ConnEvent(bool connected) {
    if (!connected) restartAdviceTxt.SetActive(true);
  }

}
