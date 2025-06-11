using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SQLite;
using UnityEngine;
using WebSocketSharp;

public class SQLiteForDictionary : MonoBehaviour {
	const string DBName = "EnglishDictionaryDB.db";
	SQLiteConnection conn;
	void Start() {
		conn = new SQLiteConnection(Application.streamingAssetsPath + "/" + DBName);
		conn.CreateTable<WordHolder>();
		print(conn.Table<WordHolder>().Count());
	}

	public List<WordHolder> GetWordStartingWithString(string queryString) {
		if (queryString.IsNullOrEmpty()) return null;
		return conn.Table<WordHolder>().Where(p => p.Word.StartsWith(queryString, System.StringComparison.OrdinalIgnoreCase)).ToList();
	}

	void AddWord(DictionaryEntry entry) {
		if (conn == null) return;
		WordHolder newWord = new WordHolder();
		newWord.Word = entry.word;
		StringBuilder meanings = new StringBuilder();
		int i = 1;
		foreach (string s in entry.description) {
			meanings.Append($"{i}. ").Append(s);
			if (entry.description.Count > i) meanings.Append("\n");
			i++;
		}
		newWord.Meanings = meanings.ToString();
		conn.Insert(newWord);
	}

	public EnglishDictionary enDic;
	[ContextMenu("Add to DB")]
	public void AddToDB() {
		if (conn == null) return;
		if (enDic == null) return;
		conn.DropTable<WordHolder>();
		conn.CreateTable<WordHolder>();
		foreach (DictionaryEntry e in enDic.DictionaryList) {
			print(e.word + " adding");
			AddWord(e);
		}
		Debug.LogWarning("Finished process for adding");
		Debug.LogWarning("table count is:");
		Debug.LogWarning(conn.Table<WordHolder>().Count());
	}

	// public string searchWord;
	// [ContextMenu("matching")]
	// public void getWordsMatching() {
	// 	TableQuery<WordHolder> query = GetWordStartingWithString(searchWord);
	// 	foreach (WordHolder h in query) {
	// 		Debug.Log($"Found {h.Word} with ID {h.Id}");
	// 	}
	// }
	// [ContextMenu("matchingID")]
	// public void getWordsMatchingID() {
	// 	TableQuery<WordHolder> query = GetWordStartingWithID(110497);
	// 	foreach (WordHolder h in query) {
	// 		Debug.Log($"Found {h.Word} with ID {h.Id}");
	// 	}
	// }
	// public TableQuery<WordHolder> GetWordStartingWithID(int ID) {
	// 	return conn.Table<WordHolder>().Where(p => p.Id > ID);
	// }

	// void Start() {
	// 	// return;

	// 	// 1. Create a connection to the database.
	// 	// The special ":memory:" in-memory database and
	// 	// URIs like "file:///somefile" are also supported
	// 	var db = new SQLiteConnection($"{Application.persistentDataPath}/" + DBName);
	// 	// var db = new SQLiteConnection(Application.streamingAssetsPath + DBName);

	// 	// 2. Once you have defined your entity, you can automatically
	// 	// generate tables in your database by calling CreateTable
	// 	db.CreateTable<WordHolder>();

	// 	// 3. You can insert rows in the database using Insert
	// 	// The Insert call fills Id, which is marked with [AutoIncremented]
	// 	// var newPlayer = new WordHolder {
	// 	// 	Word = "gilzoide",
	// 	// };
	// 	// db.Insert(newPlayer);
	// 	// Debug.Log($"Player new ID: {newPlayer.Id}");
	// 	// Similar methods exist for Update and Delete.

	// 	// 4.a The most straightforward way to query for data
	// 	// is using the Table method. This can take predicates
	// 	// for constraining via WHERE clauses and/or adding ORDER BY clauses
	// 	var query = db.Table<WordHolder>().Where(p => p.Word.StartsWith("G", System.StringComparison.OrdinalIgnoreCase));
	// 	foreach (WordHolder player in query) {
	// 		Debug.Log($"Found player named {player.Word} with ID {player.Id}");
	// 	}

	// 	// 4.b You can also make queries at a low-level using the Query method
	// 	// var players = db.Query<WordHolder>("SELECT * FROM Player WHERE Id = ?", 1);
	// 	// foreach (WordHolder player in players) {
	// 	// 	Debug.Log($"Player with ID 1 is called {player.Word}");
	// 	// }

	// 	// 	// 5. You can perform low-level updates to the database using the Execute
	// 	// 	// method, for example for running PRAGMAs or VACUUM
	// 	// 	db.Execute("VACUUM");
	// }
	void OnDestroy() {
		if (conn == null) return;
		conn.Close();
	}

}


public class WordHolder {
	[PrimaryKey, AutoIncrement]
	public int Id { get; set; }
	public string Word { get; set; }
	public string Meanings { get; set; }
}