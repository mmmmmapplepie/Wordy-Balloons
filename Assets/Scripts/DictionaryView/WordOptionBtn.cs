using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WordOptionBtn : MonoBehaviour {
	public TextMeshProUGUI wordTxt;
	WordHolder wordData;
	public static event System.Action<WordHolder> btnClicked;
	public AudioClip clip;
	public void SetData(WordHolder data) {
		if (wordData == null) return;
		wordData = data;
		wordTxt.text = wordData.Word;
	}
	public void BtnClick() {
		btnClicked?.Invoke(wordData);
		AudioPlayer.PlayOneShot_Static(clip);
	}


}
