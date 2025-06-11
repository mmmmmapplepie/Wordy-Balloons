using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DisplayWordAndMeaning : MonoBehaviour {
	public TextMeshProUGUI word, meaning;
	public RectTransform scrollHolder;

	// void Start() {
	// 	Set("new word", "asdfadfasdfasdfasdf");
	// }

	public void Set(WordHolder data) {
		word.text = data.Word;
		meaning.text = data.Meanings;
		// print(meaning.GetPreferredValues());
		Vector2 txtBxSizePref = meaning.GetPreferredValues(scrollHolder.rect.width, Mathf.Infinity);
		// print(txtBxSizePref);

		scrollHolder.sizeDelta = new Vector2(scrollHolder.rect.width, txtBxSizePref.y);

		scrollHolder.anchoredPosition = Vector2.zero;

		LayoutRebuilder.ForceRebuildLayoutImmediate(scrollHolder);

	}

}
