using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneManagerAsync : MonoBehaviour {
  public static SceneManagerAsync Singleton;
  void Awake() {
    if (Singleton == null) {
      Singleton = this;
      DontDestroyOnLoad(gameObject);
    } else {
      Destroy(gameObject);
    }



    SceneManager.sceneLoaded += SceneLoaded;

  }
  void Start() {
    NetworkManager.Singleton.OnClientStarted += Connected;
    NetworkManager.Singleton.OnClientStopped += Disconnected;
  }

  void Connected() {
    NetworkManager.Singleton.SceneManager.OnSceneEvent += SceneEventStart;
  }
  public static bool PlayAnimations = true;
  void Disconnected(bool host) {
    if (NetworkManager.Singleton.SceneManager != null) NetworkManager.Singleton.SceneManager.OnSceneEvent -= SceneEventStart;
  }

  void OnDestroy() {
    if (NetworkManager.Singleton != null) {
      NetworkManager.Singleton.OnClientStarted -= Connected;
      NetworkManager.Singleton.OnClientStopped -= Disconnected;
    }
    SceneManager.sceneLoaded -= SceneLoaded;
  }

  private void SceneEventStart(SceneEvent sceneEvent) {
    if (sceneEvent.SceneEventType == SceneEventType.Unload) {
      if (PlayAnimations) StartAnimation();
      else PlayAnimations = true;
    }
    Debug.LogWarning(sceneEvent.SceneEventType);
  }

  private void SceneLoaded(Scene scene, LoadSceneMode mode) {
    Debug.LogWarning(scene.name + " ::: Scene loaded");
    StopAnimation();
  }

  public void LoadSceneAsync(string sceneName) {
    if (loadSceneRoutine != null) return;
    if (SceneManager.GetSceneByName(sceneName) == null) {
      Debug.LogError("No such scene");
      return;
    }
    Debug.LogWarning("starting load");
    loadSceneRoutine = StartCoroutine(LoadAsyncSceneRoutine(sceneName));
    StartAnimation();
  }
  IEnumerator LoadAsyncSceneRoutine(string sceneName) {
    AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
    load.allowSceneActivation = false;
    while (load.progress < 0.9f) yield return null;
    yield return new WaitForSecondsRealtime(OpenPeriod);
    Debug.LogWarning("allowing activation");
    load.allowSceneActivation = true;
    loadSceneRoutine = null;
  }



  public void StartAnimation() {
    if (panelAnim != null) StopCoroutine(panelAnim);
    panelAnim = StartCoroutine(PanelAnimation(true));
  }
  public void StopAnimation() {
    if (panelAnim != null) StopCoroutine(panelAnim);
    panelAnim = StartCoroutine(PanelAnimation(false));
  }
  Coroutine panelAnim, loadSceneRoutine;
  const float OpenPeriod = 0.5f, ClosePeriod = 0.3f;
  public CanvasGroup loadingCanvasGrp;
  IEnumerator PanelAnimation(bool open) {
    float t = 0;
    if (open) loadingCanvasGrp.gameObject.SetActive(true);
    loadingCanvasGrp.blocksRaycasts = open;
    float p = open ? OpenPeriod : ClosePeriod;
    float initial = loadingCanvasGrp.alpha;
    float final = open ? 1f : 0;
    while (t < p) {
      t += Time.unscaledDeltaTime;
      loadingCanvasGrp.alpha = Mathf.Lerp(initial, final, t / p);
      print(Time.unscaledTime);
      yield return null;
    }
    loadingCanvasGrp.alpha = final;
    if (!open) loadingCanvasGrp.gameObject.SetActive(false);
  }












}
