using System.Collections.Generic;
using UnityEngine;
using System;

public class EnglishDictionary : MonoBehaviour {
	public static Dictionary<string, List<string>> EN_Dictionary = new Dictionary<string, List<string>>();
	public TextAsset dictionaryJSON;
	[HideInInspector] public DictionaryEntryListFromJson entries = new DictionaryEntryListFromJson();
	void Start() {
		// SetupDictionary();
	}
	[ContextMenu("setup")]
	void SetupDictionary() {
		entries = JsonUtility.FromJson<DictionaryEntryListFromJson>(dictionaryJSON.text);
		//check if any of the words contain something that is not a normal character (abcd's). and list them along with the number index (so that i can edit).
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