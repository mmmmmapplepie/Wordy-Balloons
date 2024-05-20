using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class AysncTesting : MonoBehaviour {
	public TextMeshProUGUI textbx;
	int i = 0;
	public async void Increment() {
		await Task.Delay(2000);
		i++;
		textbx.text = i.ToString();
	}
}
