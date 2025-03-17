using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour {
#if UNITY_EDITOR
	public void GoToScene(UnityEditor.SceneAsset scene) {
		SceneManager.LoadScene(scene.name);
	}
#endif
	public void GoToScene(string name) {
		if (SceneManager.GetSceneByName(name) == null) return;
		SceneManager.LoadScene(name);
	}

	public void ActivateObject(GameObject o) {
		o.SetActive(true);
	}

	public void CloseGame() {
		Application.Quit();
	}
}
