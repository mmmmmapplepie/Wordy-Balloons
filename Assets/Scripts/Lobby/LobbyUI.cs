using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Unity.Services.Relay;
using UnityEditor.PackageManager.Requests;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using UnityEngine.UI;
using Unity.Netcode;

public class LobbyUI : MonoBehaviour {

	#region subscribing/unsubscribing to events;

	// having the // comment at the end of the event sub means that i have checked the things are correctly fired (only once) for the situation.
	// i have to make sure that error cases fire only one event until exited the situation.
	void Start() {
		if (MyLobby.Instance == null) return;
		MyLobby.Instance.AuthenticationBegin += OpenLoadingPanel;
		MyLobby.Instance.AuthenticationSuccess += CloseTransitionPanels;
		MyLobby.Instance.AuthenticationFailure += AuthenticationFail;

		MyLobby.Instance.LobbyCreationBegin += OpenLoadingPanel;
		MyLobby.Instance.LobbyCreationSuccess += LobbyCreationSuccess;
		MyLobby.Instance.LobbyCreationFailure += LobbyCreationFail;

		MyLobby.Instance.HearbeatFailure += HearbeatFail;

		MyLobby.Instance.LobbyJoinBegin += OpenLoadingPanel;
		MyLobby.Instance.LobbyJoinSuccess += LobbyJoined;
		MyLobby.Instance.LobbyJoinFailure += LobbyJoinFail;

		MyLobby.Instance.LeaveLobbyBegin += LeaveLobbyBegin;

		MyLobby.Instance.ListLobbySuccess += LobbyListFound;
		MyLobby.Instance.ListLobbyFailure += ListLobbiesFail;

		NetcodeManager.LockOnLoading += DisableLeaving;
		NetcodeManager.StartSceneLoading += LoadingNextScene;
		NetcodeManager.StopSceneLoading += StopLoadingScene;
	}
	void OnDestroy() {
		if (MyLobby.Instance == null) return;
		MyLobby.Instance.AuthenticationBegin -= OpenLoadingPanel;
		MyLobby.Instance.AuthenticationSuccess -= CloseTransitionPanels;
		MyLobby.Instance.AuthenticationFailure -= AuthenticationFail;

		MyLobby.Instance.LobbyCreationBegin -= OpenLoadingPanel;
		MyLobby.Instance.LobbyCreationSuccess -= LobbyCreationSuccess;
		MyLobby.Instance.LobbyCreationFailure -= LobbyCreationFail;

		MyLobby.Instance.HearbeatFailure -= HearbeatFail;

		MyLobby.Instance.LobbyJoinBegin -= OpenLoadingPanel;
		MyLobby.Instance.LobbyJoinSuccess -= LobbyJoined;
		MyLobby.Instance.LobbyJoinFailure -= LobbyJoinFail;

		MyLobby.Instance.LeaveLobbyBegin -= LeaveLobbyBegin;

		MyLobby.Instance.ListLobbySuccess -= LobbyListFound;
		MyLobby.Instance.ListLobbyFailure -= ListLobbiesFail;

		NetcodeManager.LockOnLoading -= DisableLeaving;
		NetcodeManager.StartSceneLoading -= LoadingNextScene;
		NetcodeManager.StopSceneLoading -= StopLoadingScene;
	}




	#endregion

	#region Interaction functions
	public UnityEditor.SceneAsset mainMenuScene;
	public static event System.Action GoingToMainMenu;
	public void GoToScene(UnityEditor.SceneAsset scene) {
		if (scene == mainMenuScene) GoingToMainMenu?.Invoke();
		//have to stop NGO and lobby if main menu
		SceneManager.LoadScene(scene.name);
	}
	public void RetryAuthentication() {
		MyLobby.Instance.Authentication();
	}


	[SerializeField] GameObject lobbyCreationPanel;
	public TMP_InputField lobbyName, lobbyCode;
	public TMP_Dropdown lobbyModeDropDown, lobbyPlayerNumDropDown;
	public void GoToCreateLobby() {
		//reset lobby name and mode and maxplayer
		lobbyModeDropDown.value = 0;
		lobbyPlayerNumDropDown.value = 0;
		lobbyName.text = "New Lobby";
		lobbyCreationPanel.SetActive(true);
	}
	public void CloseLobbyCreation() {
		lobbyCreationPanel.SetActive(false);
	}

	//imma disable changing lobby mode and name once started.
	public string ConvertDropDownValueToGameMode(int index) {
		switch (index) {
			case 0:
				return "Normal";
			case 1:
				return "Eraser";
			case 2:
				return "OwnEnemy";
			case 3:
				return "Pacifist";
			default:
				return "Normal";
		}
	}
	public void ConvertGameModeToDropDownValue(string newMode) {
		int index = 0;
		switch (newMode) {
			case "Normal":
				index = 0;
				break;
			case "Eraser":
				index = 1;
				break;
			case "OwnEnemy":
				index = 2;
				break;
			case "Pacifist":
				index = 3;
				break;
		}
		lobbyModeDropDown.value = index;
	}
	public void CreateLobby() {
		MyLobby.Instance.CreateLobby(lobbyName.text, ConvertDropDownValueToGameMode(lobbyModeDropDown.value), lobbyPlayerNumDropDown.value + 2);
	}
	public void QuickJoin() {
		MyLobby.Instance.QuickJoinLobby();
	}
	public void JoinLobbyByCode() {
		MyLobby.Instance.JoinLobbyByCode(lobbyCode.text);
		lobbyCode.text = "";
	}

	public void LeaveLobby() {
		//you need to add UI stuff here as leave lobby itself i decided not to add ui stuff (as it is called EVERYWHERE)
		MyLobby.Instance.LeaveLobby();
	}


	public void ListLobbyRefresh() {
		Task list = MyLobby.Instance.ListLobbies();
	}


	#endregion







	#region Event Functions
	[SerializeField] GameObject LoadingPanel, ErrorPanel;
	[SerializeField] GameObject RetryAuthenticationBtn, CloseErrorPanelBtn;
	void OpenLoadingPanel() {
		HidePanelsExceptChosen(LoadingPanel);
	}
	public void CloseTransitionPanels() {
		HidePanelsExceptChosen(null);
	}
	void HidePanelsExceptChosen(GameObject panelToOpen = null) {
		//disable non basic items
		LoadingPanel.SetActive(false);
		ErrorPanel.SetActive(false);


		RetryAuthenticationBtn.SetActive(false);
		CloseErrorPanelBtn.SetActive(true);

		//only open the one you want.
		if (panelToOpen != null) {
			panelToOpen.SetActive(true);
		}
	}

	[SerializeField] TextMeshProUGUI ErrorTxtBx;
	void AuthenticationFail() {
		ErrorTxtBx.text = "Connection failed";
		HidePanelsExceptChosen(ErrorPanel);
		RetryAuthenticationBtn.SetActive(true);
		CloseErrorPanelBtn.SetActive(false);
	}

	[SerializeField] CanvasGroup lobbyPanel;

	//shoudl be called when NGO host is connected.
	void LobbyCreationSuccess() {
		lobbyCreationPanel.SetActive(false);
		LobbyUpdate(MyLobby.Instance.hostLobby);
		ToggleLobby(true);
		CloseTransitionPanels();
	}
	void ToggleLobby(bool interactable) {
		lobbyPanel.interactable = interactable;
		lobbyPanel.blocksRaycasts = interactable;
		lobbyPanel.alpha = interactable ? 1 : 0;
	}

	void LobbyCreationFail() {
		ErrorTxtBx.text = "Unable to create lobby.";
		HidePanelsExceptChosen(ErrorPanel);
	}





	void LobbyJoined() {
		HidePanelsExceptChosen();
		enterGameBtn.interactable = NetworkManager.Singleton.IsServer;
		LobbyUpdate(MyLobby.Instance.joinedLobby);
		ToggleLobby(true);
	}
	[SerializeField] TextMeshProUGUI lobbyModeTxt, lobbyCodeTxt;
	void LobbyUpdate(Lobby lobby) {
		if (lobby == null) return;
		lobbyModeTxt.text = lobby.Data[MyLobby.GameMode].Value;
		if (MyLobby.Instance.hostLobby != null) {
			lobbyCodeTxt.text = lobby.LobbyCode;
		}
	}

	void HearbeatFail() {
		ErrorTxtBx.text = "Connection failed";
		HidePanelsExceptChosen(ErrorPanel);
	}

	void LobbyJoinFail() {
		ErrorTxtBx.text = "Unable to join lobby";
		HidePanelsExceptChosen(ErrorPanel);
	}

	//dont forget to start shutdown for NGO.
	void LeaveLobbyBegin() {
		ToggleLobby(false);
	}

	public Transform lobbiesListHolder, lobbyOptionPrefab;
	void LobbyListFound(List<Lobby> lobbies) {
		foreach (Transform t in lobbiesListHolder) {
			Destroy(t.gameObject);
		}
		foreach (Lobby l in lobbies) {
			Transform newOption = Instantiate(lobbyOptionPrefab, lobbiesListHolder);
			//set the values for the script and give it a listener for the join lobby by ID thingy
			LobbyOption lobbyOption = newOption.GetComponent<LobbyOption>();
			lobbyOption.SetOption(l);
		}
	}
	public GameObject lobbyListingFailNotificationObject;
	void ListLobbiesFail() {
		lobbyListingFailNotificationObject.SetActive(true);
	}

	#endregion





	public Button leaveBtn, enterGameBtn;
	void LoadingNextScene() {
		enterGameBtn.interactable = false;
	}
	void DisableLeaving(bool lockOn) {
		leaveBtn.interactable = !lockOn;
	}
	void StopLoadingScene() {
		leaveBtn.interactable = true;
		if (NetworkManager.Singleton.IsServer) {
			enterGameBtn.interactable = true;
		}
	}


}
