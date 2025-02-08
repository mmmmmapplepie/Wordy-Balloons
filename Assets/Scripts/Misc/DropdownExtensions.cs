using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class DropdownExtensions {
	public static void SetOptions(this TMP_Dropdown dd, List<string> dropdownOptions, int dropdownSetValue = 0) {
		if (dd == null) return;
		if (dropdownOptions == null || dropdownOptions.Count < 1) { Debug.LogWarning("No options for dropdown"); return; }
		dd.ClearOptions();
		dd.AddOptions(dropdownOptions);
		dd.Set(Mathf.Min(dropdownSetValue, dropdownOptions.Count));
	}
}
