using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour {

	public void GoToScene(UnityEditor.SceneAsset scene) {
		SceneManager.LoadScene(scene.name);
	}
}
