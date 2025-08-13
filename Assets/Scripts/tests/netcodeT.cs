using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class netcodeT : MonoBehaviour {
	void Start() {
		NetworkManager.Singleton.OnServerStarted += ServerStart;
		NetworkManager.Singleton.OnServerStopped += serverstop;
		NetworkManager.Singleton.OnClientConnectedCallback += clientconn;
		NetworkManager.Singleton.OnClientDisconnectCallback += cliendiscon;
		NetworkManager.Singleton.OnClientStarted += clientsta;
		NetworkManager.Singleton.OnClientStopped += clientstop;
	}
	[ContextMenu("Check")]
	void Chekc() {
		FindObjectOfType<UnityTransport>().SetConnectionData("127.0.0.1", 7777);
		bool success = NetworkManager.Singleton.StartHost();
		print(success);
		NetworkManager.Singleton.Shutdown();

	}

	private void clientstop(bool obj) {
		print("client stopped");
	}

	private void clientsta() {
		print("	private void clientsta() {");

	}

	private void cliendiscon(ulong obj) {
		print("private void cliendiscon(ulong obj) {");

	}

	private void clientconn(ulong obj) {
		print("private void clientconn(ulong obj) {");

	}

	private void serverstop(bool obj) {
		print("private void serverstop(bool obj) {");

	}

	private void ServerStart() {
		print("	private void ServerStart() {");

	}
}
