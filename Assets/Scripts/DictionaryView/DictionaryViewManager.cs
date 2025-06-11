using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WebSocketSharp;

public class DictionaryViewManager : MonoBehaviour {
	public TMP_InputField searchInput;
	public Transform wordResultHolder;



	public SQLiteForDictionary db;
	public GameObject btnPrefab;
	Queue<GameObject> availableBtn = new Queue<GameObject>();
	List<GameObject> btnsInUse = new List<GameObject>();
	string previousQuery = "";

	public Transform alphabetBtnHolder;

	void Start() {
		WordOptionBtn.btnClicked += WordClicked;
		for (int i = 0; i < 200; i++) {
			CreateBtn();
		}

		char currentChar = 'a';
		foreach (Transform t in alphabetBtnHolder) {
			string letter = currentChar.ToString();
			t.GetComponent<Button>().onClick.AddListener(() => QueryForString(letter));
			currentChar++;
		}
	}
	void CreateBtn() {
		GameObject g = Instantiate(btnPrefab, wordResultHolder);
		g.SetActive(false);
		availableBtn.Enqueue(g);
	}

	public void ClearInputField() {
		searchInput.Set("");
	}

	public void QueryForString(string s) {
		if (previousQuery.ToLower() == s.ToLower()) return;
		List<WordHolder> matches = db.GetWordStartingWithString(s);

		foreach (GameObject g in btnsInUse) {
			g.SetActive(false);
			availableBtn.Enqueue(g);
		}

		foreach (WordHolder word in matches) {
			GameObject newObj = null;
			if (availableBtn.Count <= 0) {
				CreateBtn();
			}
			newObj = availableBtn.Dequeue();
			btnsInUse.Add(newObj);
			newObj.GetComponent<WordOptionBtn>().SetData(word);
			newObj.SetActive(true);
			newObj.transform.SetAsLastSibling();
		}

		wordResultHolder.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

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
