using System.Collections.Generic;
using UnityEngine;
using System;

public class EnglishDictionary : MonoBehaviour {
	public static int wordCount = 0;
	public TextAsset dictionaryJSON;
	public List<DictionaryEntry> DictionaryList = new List<DictionaryEntry>();
	public static EnglishDictionary Instance;
	void Awake() {
		if (Instance == null) {
			Instance = this;
			wordCount = DictionaryList.Count;
			DontDestroyOnLoad(gameObject);
		} else {
			Destroy(gameObject);
		}
	}

	public List<DictionaryEntry> GetDictionaryList() {
		return DictionaryList;
	}

	[ContextMenu("setup")]
	void SetupDictionary() {
		DictionaryEntryListFromJson entries = new DictionaryEntryListFromJson();

		entries = JsonUtility.FromJson<DictionaryEntryListFromJson>(dictionaryJSON.text);
		foreach (DictionaryEntryFromJson entry in entries.words) {
			string word = entry.word;
			string des = entry.description;
			DictionaryEntry newEntry = new DictionaryEntry();
			newEntry.word = word;
			// newEntry.minLength = word.Length;
			// newEntry.maxLength = word.Length;
			// newEntry.minPos = -1;
			// newEntry.maxPos = -1;
			des = des.Trim();
			newEntry.description.Add(des);

			int indexInDictionary = DictionaryList.FindIndex(x => x.word == word);
			if (indexInDictionary == -1) {
				//add new word
				// if (des.Length < newEntry.minLength) {
				// 	newEntry.minLength = des.Length;
				// 	newEntry.minPos = 0;
				// }
				// if (des.Length > newEntry.maxLength) {
				// 	newEntry.maxLength = des.Length;
				// 	newEntry.maxPos = 0;
				// }
				DictionaryList.Add(newEntry);
			} else {
				DictionaryEntry dEntry = DictionaryList[indexInDictionary];
				// if (des.Length < dEntry.minLength) {
				// 	dEntry.minLength = des.Length;
				// 	dEntry.minPos = dEntry.description.Count;
				// }
				// if (des.Length > dEntry.maxLength) {
				// 	dEntry.maxLength = des.Length;
				// 	dEntry.maxPos = dEntry.description.Count;
				// }
				dEntry.description.Add(des);
			}




			//remove empty space from description.




		}
		// 	  string source = "abc    \t def\r\n789";
		// string result = string.Concat(source.Where(c => !char.IsWhiteSpace(c)));

	}
}

[Serializable]
public class DictionaryEntryFromJson {
	public string word;
	public string description;
}
[Serializable]
public class DictionaryEntryListFromJson {
	public List<DictionaryEntryFromJson> words = new List<DictionaryEntryFromJson>();
}
[Serializable]
public class DictionaryEntry {
	//the word
	public string word;
	//longest word/explanation phrase
	// public int maxLength;
	// //where to find the longest one
	// public int maxPos;
	// //shortest word/explanation phrase
	// public int minLength;
	// //where to find the shortest one
	// public int minPos;
	//parent list contains individual descriptions for words
	public List<string> description = new List<string>();
}