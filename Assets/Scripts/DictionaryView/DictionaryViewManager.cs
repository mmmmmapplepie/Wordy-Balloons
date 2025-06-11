using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WebSocketSharp;

public class DictionaryViewManager : MonoBehaviour {
	public TMP_InputField searchInput;
	public Transform wordResultHolder;



	public SQLiteForDictionary db;
	string previousQuery = "";

	public Transform alphabetBtnHolder;

	void Start() {
		WordOptionBtn.btnClicked += WordClicked;

		char currentChar = 'a';
		foreach (Transform t in alphabetBtnHolder) {
			string letter = currentChar.ToString();
			t.GetComponent<Button>().onClick.AddListener(() => QueryForString(letter));
			currentChar++;
		}
	}


	public void ClearInputField() {
		searchInput.Set("");
		EventSystem.current.SetSelectedGameObject(searchInput.gameObject);
	}
	public dragTest resultScroller;
	public void QueryForString(string s) {
		if (previousQuery.ToLower() == s.ToLower()) return;
		List<WordHolder> matches = db.GetWordStartingWithString(s);
		resultScroller.SetScroller(matches);

		previousQuery = s;
	}

	public DisplayWordAndMeaning live, copy;
	WordHolder liveWord = null;
	void WordClicked(WordHolder data) {
		live.Set(data);
		liveWord = data;
	}
	public void CopyData() {
		if (liveWord == null) return;
		copy.Set(liveWord);
	}


	public void ReturnToMainMenu() {
		SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
	}


	void OnDestroy() {
		WordOptionBtn.btnClicked -= WordClicked;
	}

}
