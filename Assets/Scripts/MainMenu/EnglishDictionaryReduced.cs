using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class EnglishDictionaryReduced : MonoBehaviour {

  public static int wordCount = 0;
  public TextAsset wordlist;
  public List<DictionaryEntry> DictionaryList = new List<DictionaryEntry>();
  public static EnglishDictionaryReduced Instance;
  public EnglishDictionary dictionarySetup;
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
  public List<string> noMeaningWords;

  [ContextMenu("setup")]
  void SetupDictionary() {
    noMeaningWords.Clear();
    DictionaryList.Clear();
    string content = wordlist.text;

    string[] lines = content.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);

    // print(lines.Length);


    List<DictionaryEntry> englishFullDictionary = dictionarySetup.DictionaryList;
    int i = 0;
    foreach (string line in lines) {
      DictionaryEntry entry = englishFullDictionary.Find(x => x.word.ToLower() == line.ToLower());
      if (entry == null) { noMeaningWords.Add(line); print(i); i++; continue; }
      DictionaryEntry newE = new DictionaryEntry();
      newE.word = entry.word;
      foreach (string m in entry.description) {
        newE.description.Add(m);
      }

      DictionaryList.Add(newE);
      i++;
    }
  }

  // [ContextMenu("Print emtpy words")]
  // public void printEmpty() {
  // 	StringBuilder s = new StringBuilder();
  // 	foreach (string i in noMeaningWords) {
  // 		s.Append(i).Append(",");
  // 	}
  // 	print(s.ToString());
  // }

  // public TextAsset dictionaryJSON;
  // public List<DictionaryEntry> newDicList, testnewList;
  // [ContextMenu("copy new words")]
  // void Copy() {
  // 	foreach (DictionaryEntry entry in newDicList) {
  // 		int indexInDictionary = dictionarySetup.DictionaryList.FindIndex(x => x.word == entry.word);
  // 		if (indexInDictionary == -1) {
  // 			dictionarySetup.DictionaryList.Add(entry);
  // 			print("added word: " + entry.word);
  // 		} else {
  // 			DictionaryEntry dEntry = dictionarySetup.DictionaryList[indexInDictionary];
  // 			foreach (string des in entry.description) {
  // 				print("added meaning: " + des);
  // 				dEntry.description.Add(des);
  // 			}
  // 		}
  // 	}
  // }

  // [ContextMenu("setupFromJSON")]
  // void SetupDictionaryFromJSON() {
  // 	DictionaryEntryListFromJson entries = new DictionaryEntryListFromJson();

  // 	entries = JsonUtility.FromJson<DictionaryEntryListFromJson>(dictionaryJSON.text);
  // 	foreach (DictionaryEntryFromJson entry in entries.words) {
  // 		string word = entry.word;
  // 		string des = entry.description;
  // 		DictionaryEntry newEntry = new DictionaryEntry();
  // 		newEntry.word = word;
  // 		// newEntry.minLength = word.Length;
  // 		// newEntry.maxLength = word.Length;
  // 		// newEntry.minPos = -1;
  // 		// newEntry.maxPos = -1;
  // 		des = des.Trim();
  // 		newEntry.description.Add(des);

  // 		int indexInDictionary = newDicList.FindIndex(x => x.word == word);
  // 		if (indexInDictionary == -1) {
  // 			//add new word
  // 			// if (des.Length < newEntry.minLength) {
  // 			// 	newEntry.minLength = des.Length;
  // 			// 	newEntry.minPos = 0;
  // 			// }
  // 			// if (des.Length > newEntry.maxLength) {
  // 			// 	newEntry.maxLength = des.Length;
  // 			// 	newEntry.maxPos = 0;
  // 			// }
  // 			newDicList.Add(newEntry);
  // 		} else {
  // 			DictionaryEntry dEntry = newDicList[indexInDictionary];
  // 			// if (des.Length < dEntry.minLength) {
  // 			// 	dEntry.minLength = des.Length;
  // 			// 	dEntry.minPos = dEntry.description.Count;
  // 			// }
  // 			// if (des.Length > dEntry.maxLength) {
  // 			// 	dEntry.maxLength = des.Length;
  // 			// 	dEntry.maxPos = dEntry.description.Count;
  // 			// }
  // 			dEntry.description.Add(des);
  // 		}




  // 	}
  // 	// 	  string source = "abc    \t def\r\n789";
  // 	// string result = string.Concat(source.Where(c => !char.IsWhiteSpace(c)));

  // }




}
