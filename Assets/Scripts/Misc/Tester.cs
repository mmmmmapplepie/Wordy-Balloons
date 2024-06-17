using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Tester : MonoBehaviour {
	public TextMeshProUGUI txt;
	string t = "1\n\"\\";
	//   1
	//   "\

	public static event System.Func<System.Func<bool>> ee;
	void Start() {
		print(t);
		txt.text = t;
		ee += GetFunc;
	}

	public void Change(string s) {
		System.Func<bool> ftn = ee.Invoke();
		print(ftn());
	}
	int i = 0;
	System.Func<bool> GetFunc() {
		i++;
		return i % 2 == 1 ? T : F;
	}
	bool T() {
		return true;
	}
	bool F() {
		return false;
	}
}
