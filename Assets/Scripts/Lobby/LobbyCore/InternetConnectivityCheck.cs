using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class InternetConnectivityCheck : MonoBehaviour {
	// string targetURL = "https://www.cloudflare.com";
	string targetURL = "https://1.1.1.1";
	int timeoutTime = 3;

	public static bool connected = false;
	void Start() {
		StartCoroutine(CheckConnection());
	}
	public static event Action<bool> ConnectedStateEvent;
	IEnumerator CheckConnection() {
		while (true) {
			if (Application.internetReachability == NetworkReachability.NotReachable) {
				ConnectedStateEvent?.Invoke(false);
				connected = false;
				yield return new WaitForSecondsRealtime(timeoutTime);
				continue;
			}
			UnityWebRequest ping = UnityWebRequest.Get(targetURL);
			ping.downloadHandler = null;
			ping.timeout = timeoutTime;
			float sendTime = Time.unscaledTime;
			yield return ping.SendWebRequest();

			ConnectedStateEvent?.Invoke(!(ping.result == UnityWebRequest.Result.ConnectionError));
			connected = !(ping.result == UnityWebRequest.Result.ConnectionError);

			yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, timeoutTime - (Time.unscaledTime - sendTime)));
		}

	}

}
