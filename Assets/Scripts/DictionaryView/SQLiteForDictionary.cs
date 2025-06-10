using SQLite;
using UnityEngine;

public class SQLiteForDictionary : MonoBehaviour {
	void Awake() {
		print(Application.streamingAssetsPath);
		System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(Application.streamingAssetsPath);
		print(dir.GetFiles().Length);
		var db = new SQLiteConnection(Application.streamingAssetsPath + "/" + DBName);
		db.CreateTable<PlayerT>();
		print(dir.GetFiles().Length);


	}

	const string DBName = "EnglishDictionaryDB.db";
	void Start() {
		return;

		// 1. Create a connection to the database.
		// The special ":memory:" in-memory database and
		// URIs like "file:///somefile" are also supported
		// var db = new SQLiteConnection($"{Application.persistentDataPath}/" + DBName);
		var db = new SQLiteConnection(Application.streamingAssetsPath + DBName);

		// 2. Once you have defined your entity, you can automatically
		// generate tables in your database by calling CreateTable
		db.CreateTable<PlayerT>();

		// 3. You can insert rows in the database using Insert
		// The Insert call fills Id, which is marked with [AutoIncremented]
		var newPlayer = new PlayerT {
			Name = "gilzoide",
		};
		db.Insert(newPlayer);
		Debug.Log($"Player new ID: {newPlayer.Id}");
		// Similar methods exist for Update and Delete.

		// 4.a The most straightforward way to query for data
		// is using the Table method. This can take predicates
		// for constraining via WHERE clauses and/or adding ORDER BY clauses
		var query = db.Table<PlayerT>().Where(p => p.Name.StartsWith("g"));
		foreach (PlayerT player in query) {
			Debug.Log($"Found player named {player.Name} with ID {player.Id}");
		}

		// 4.b You can also make queries at a low-level using the Query method
		var players = db.Query<PlayerT>("SELECT * FROM Player WHERE Id = ?", 1);
		foreach (PlayerT player in players) {
			Debug.Log($"Player with ID 1 is called {player.Name}");
		}

		// 5. You can perform low-level updates to the database using the Execute
		// method, for example for running PRAGMAs or VACUUM
		db.Execute("VACUUM");
	}
}


public class PlayerT {
	[PrimaryKey, AutoIncrement]
	public int Id { get; set; }
	public string Name { get; set; }
}