using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Unity.Services.Relay;

public class LobbyUI : MonoBehaviour {

	#region subscribing/unsubscribing to events;

	// having the // comment at the end of the event sub means that i have checked the things are correctly fired (only once) for the situation.
	// i have to make sure that error cases fire only one event until exited the situation.
	void Start() {
		if (MyLobby.Instance == null) return;
		MyLobby.Instance.LobbyChangedEvent += LobbyUpdate;


		MyLobby.Instance.AuthenticationBegin += OpenLoadingPanel;//
		MyLobby.Instance.AuthenticationSuccess += CloseTransitionPanels;//
		MyLobby.Instance.UnityServiceFailure += UnityServiceFail;//
		MyLobby.Instance.AuthenticationFailure += AuthenticationFail;//

		MyLobby.Instance.LobbyCreationBegin += OpenLoadingPanel;//
		MyLobby.Instance.LobbyCreationSuccess += LobbyCreated;//
		MyLobby.Instance.LobbyCreationFailure += LobbyCreationFail;//
		MyLobby.Instance.LobbyCreationFailureRelay += LobbyCreationRelayFail;
		MyLobby.Instance.RelayFailure += RelayFail;

		MyLobby.Instance.HearbeatFailure += HearbeatFail;

		MyLobby.Instance.LobbyJoinBegin += OpenLoadingPanel;//
		MyLobby.Instance.LobbyJoinSuccess += CloseTransitionPanels;
		MyLobby.Instance.LobbyJoinFailure += LobbyJoinFail;

		MyLobby.Instance.LeaveLobbyBegin += OpenLoadingPanel;//
		MyLobby.Instance.LeaveLobbySuccess += CloseTransitionPanels;
		MyLobby.Instance.LeaveLobbyFailure += LobbyLeaveFail;

		MyLobby.Instance.DeleteLobbyBegin += OpenLoadingPanel;//
		MyLobby.Instance.DeleteLobbySuccess += CloseTransitionPanels;
		MyLobby.Instance.DeleteLobbyFailure += LobbyDeleteFail;

		MyLobby.Instance.ListLobbySuccess += LobbyListFound;
		MyLobby.Instance.ListLobbyFailure += ListLobbiesFail;
	}




	#endregion


	#region Event Functions
	[SerializeField] GameObject LoadingPanel, ErrorPanel;
	[SerializeField] GameObject MainMenuBtn, RetryAuthenticationBtn, CloseErrorPanelBtn;

	void OpenLoadingPanel() {
		HidePanelsInDefaultStateExceptChosen(LoadingPanel);
	}
	public void CloseTransitionPanels() {
		HidePanelsInDefaultStateExceptChosen(null);
	}
	void HidePanelsInDefaultStateExceptChosen(GameObject panelToOpen = null) {
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
	void UnityServiceFail(RequestFailedException e) {
		ErrorTxtBx.text = "Connection failed";
		HidePanelsInDefaultStateExceptChosen(ErrorPanel);
		RetryAuthenticationBtn.SetActive(true);
		CloseErrorPanelBtn.SetActive(false);
	}

	void AuthenticationFail(AuthenticationException e) {
		ErrorTxtBx.text = "Connection failed";
		HidePanelsInDefaultStateExceptChosen(ErrorPanel);
		RetryAuthenticationBtn.SetActive(true);
		CloseErrorPanelBtn.SetActive(false);
	}


	[SerializeField] GameObject LobbyCreation;
	[SerializeField] CanvasGroup Lobby;
	void LobbyCreated() {
		LobbyCreation.SetActive(false);

		Lobby.alpha = 1;
		Lobby.interactable = true;

		//update things to lobby:
		//players should be "already made"
		//lobby code
		//lobby mode
		//lobby name
		//lobby player number

		CloseTransitionPanels();
	}

	void RelayFail(RelayServiceException e) {

	}

	void LobbyCreationFail(LobbyServiceException e) {
		ErrorTxtBx.text = "Unable to create lobby.";
		HidePanelsInDefaultStateExceptChosen(ErrorPanel);
	}






	void LobbyJoined() {
		Lobby joinedLobby = MyLobby.Instance.joinedLobby;
		if (joinedLobby == null) return;

		//update the lobby stuff
	}


	void LobbyCreationRelayFail() {
		ErrorTxtBx.text = "Unable to create lobby.";
		HidePanelsInDefaultStateExceptChosen(ErrorPanel);
	}






	void HearbeatFail() {

	}
	void LobbyUpdate(ILobbyChanges lobbyChanges = null) {

	}
	void LobbyJoinFail(LobbyServiceException e) {

	}

	void LobbyLeaveFail(LobbyServiceException e) {

	}
	void LobbyDeleteFail(LobbyServiceException e) {

	}

	void LobbyListFound(List<Lobby> lobbies) {

	}

	void ListLobbiesFail(LobbyServiceException e) {

	}




	#endregion



	// #region LobbySettingsUI
	// [SerializeField] GameObject ModeDropdown;
	// [SerializeField] CanvasGroup LobbyPanel;
	// void OpenLobbyPanel(bool isHost) {
	// 	if (isHost) lobbyCodeTxt.text = lobbyCode;
	// 	LobbyPanel.blocksRaycasts = true;
	// 	LobbyPanel.alpha = 1f;
	// 	ModeDropdown.GetComponent<TMP_Dropdown>().interactable = isHost;
	// 	//if host then enable buttons etc matching that for the host.
	// }

	// int lobbyMaxPlayerNumber = 2;
	// public void ChangePlayerNumber(int num) {
	// 	lobbyMaxPlayerNumber = num + 2;
	// }

	// string mode = "Normal";


	// string lobbyName = "New Lobby";
	// public void ChangeLobbyName(string newName) {
	// 	lobbyName = newName;
	// }

	// public GameObject CreateLobbyPanel;
	// public TMP_Dropdown PlayerCountDropdown;
	// public void OpenLobbyCreationPanel(bool open) {
	// 	PlayerCountDropdown.value = 0;
	// 	lobbyMaxPlayerNumber = 2;
	// 	CreateLobbyPanel.SetActive(open);
	// }


	// string lobbyCode = "";


	// #endregion




	// void DisplayLobby(Lobby lobby) {
	// 	foreach (Transform t in lobbyListHolder) {
	// 		Destroy(t.gameObject);
	// 	}
	// 	GameObject lobbyItem = Instantiate(lobbyListItemPrefab, lobbyListHolder);
	// 	lobbyItem.GetComponent<LobbyOption>().SetOption(lobby, this);
	// }








	// public void ChangeGameMode(int index) {
	// 	switch (index) {
	// 		case 0:
	// 			mode = "Normal";
	// 			break;
	// 		case 1:
	// 			mode = "Eraser";
	// 			break;
	// 		case 2:
	// 			mode = "OwnEnemy";
	// 			break;
	// 		case 3:
	// 			mode = "Pacifist";
	// 			break;
	// 	}
	// 	if (hostLobby != null) {
	// 		UpdateLobbyData();
	// 	}
	// }
	// [SerializeField] TMP_Dropdown modeDisplayDropdown;
	// public void ChangeGameModeDisplay(string newMode) {
	// 	int index = 0;
	// 	switch (newMode) {
	// 		case "Normal":
	// 			index = 0;
	// 			mode = newMode;
	// 			break;
	// 		case "Eraser":
	// 			index = 1;
	// 			mode = newMode;
	// 			break;
	// 		case "OwnEnemy":
	// 			index = 2;
	// 			mode = newMode;
	// 			break;
	// 		case "Pacifist":
	// 			index = 3;
	// 			mode = newMode;
	// 			break;
	// 	}
	// 	modeDisplayDropdown.value = index;

	// }

}
