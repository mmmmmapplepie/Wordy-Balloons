using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using System.Threading.Tasks;

public class NetcodeManager : MonoBehaviour {
  public static event Action ServerStartSuccess, ServerStartFail, ServerStoppedEvent, ShuttingDownNetwork, TransportFailureEvent;
  public static event Action ClientStartSuccess, ClientStartFail;
  public static event Action<ulong> ClientConnected, ClientDisconnected;
  public static event Action<bool> ClientStoppedEvent, ClientStartedEvent;

  public static NetcodeManager Instance = null;
  void Start() {
    if (Instance == null) {
      Instance = this;
      DontDestroyOnLoad(gameObject);
    } else {
      Destroy(gameObject);
      return;
    }

    NetworkManager.Singleton.OnClientConnectedCallback += ClientConnectedToNGO;
    NetworkManager.Singleton.OnServerStarted += ServerStarted;
    NetworkManager.Singleton.OnServerStopped += ServerStopped;
    NetworkManager.Singleton.OnTransportFailure += TransportFailure;

    NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnectedFromNGO;
    NetworkManager.Singleton.OnClientStopped += ClientStopped;
    NetworkManager.Singleton.OnClientStarted += ClientStarted;
  }

  public void OnDestroy() {
    if (Instance != this) return;
    if (NetworkManager.Singleton != null) {
      NetworkManager.Singleton.OnClientConnectedCallback -= ClientConnectedToNGO;
      NetworkManager.Singleton.OnServerStarted -= ServerStarted;
      NetworkManager.Singleton.OnServerStopped -= ServerStopped;
      NetworkManager.Singleton.OnTransportFailure -= TransportFailure;

      NetworkManager.Singleton.OnClientDisconnectCallback -= ClientDisconnectedFromNGO;
      NetworkManager.Singleton.OnClientStopped -= ClientStopped;
      NetworkManager.Singleton.OnClientStarted -= ClientStarted;
    }
    Instance = null;
  }
  public bool StartHost() {
    bool success = NetworkManager.Singleton.StartHost();
    if (!success) ServerStartFail?.Invoke();
    return success;
  }
  void ServerStarted() {
    ServerStartSuccess?.Invoke();
  }
  public bool StartClient() {
    bool success = NetworkManager.Singleton.StartClient();
    if (!success) ClientStartFail?.Invoke();
    return success;
  }

  public void ShutDownNetwork() {
    StartCoroutine(ShutdownRoutine());
  }
  public bool shuttingDown { get; private set; } = false;
  readonly object shutdownLock = new object();
  IEnumerator ShutdownRoutine() {
    lock (shutdownLock) {
      if (shuttingDown) yield break;
      shuttingDown = true;
    }
    ShuttingDownNetwork?.Invoke();
    if (NetworkManager.Singleton == null) { shuttingDown = false; yield break; }
    print("Shutdown NGO");
    NetworkManager.Singleton.Shutdown();
    while (true) {
      if (NetworkManager.Singleton == null) break;
      if (!NetworkManager.Singleton.ShutdownInProgress && !NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer) break;
      yield return null;
    }
    lock (shutdownLock) {
      shuttingDown = false;
    }
  }


  void ClientConnectedToNGO(ulong clientID) {
    print("m local id is : " + NetworkManager.Singleton.LocalClientId);
    print($"clinet started {clientID}");
    ClientConnected?.Invoke(clientID);
    if (clientID == NetworkManager.Singleton.LocalClientId) ClientStartSuccess?.Invoke();
  }
  void ClientDisconnectedFromNGO(ulong clientID) {
    // print("disconnected");
    ClientDisconnected?.Invoke(clientID);
  }
  void ServerStopped(bool stopped) {
    ServerStoppedEvent?.Invoke();
  }
  void ClientStarted() {
    print("clientStarted");
    ClientStartedEvent?.Invoke(NetworkManager.Singleton.IsHost);
  }
  void ClientStopped(bool wasHost) {
    // print("clientStopped");
    ClientStoppedEvent?.Invoke(wasHost);
  }
  void TransportFailure() {
    TransportFailureEvent?.Invoke();
  }

  public bool? InConnectedSession() {
    if (NetworkManager.Singleton == null) return null;
    if (NetworkManager.Singleton.ShutdownInProgress) return true;
    if (NetworkManager.Singleton.IsConnectedClient) return true;
    if (NetworkManager.Singleton.IsServer) return true;
    if (NetworkManager.Singleton.IsClient) return true;
    if (shuttingDown) return true;
    return false;
  }







}
