using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionManager : MonoBehaviour {
	public Button server, client, host;


	public void Server() {
		NetworkManager.Singleton.StartServer();
	}
	public void Host() {
		NetworkManager.Singleton.StartHost();
	}
	public void Client() {
		NetworkManager.Singleton.StartClient();
	}

}
