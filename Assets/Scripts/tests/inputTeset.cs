using UnityEngine;
using System.Collections;
public class inputTeset : MonoBehaviour {
	void Update() {
		string input = Input.inputString;
		if (input != null && input.Length > 0) {
			//check enter
			//if (input == "\r");
			//check backspace
			//if (input == "\b");

			//process rest of it.
			print(input);
		}

	}
	// void OnGUI() {
	// 	Event e = Event.current;
	// 	if (e.isKey && e.type == EventType.KeyUp) {
	// 		print(e.character);
	// 	}
	// }
}
