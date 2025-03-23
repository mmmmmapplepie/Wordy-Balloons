using Unity.Services.Lobbies.Models;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using UnityEngine.UI;
using System;
using Unity.Netcode;
[DefaultExecutionOrder(-100)]
public class LobbyUI : MonoBehaviour {

	void Awake() {
		SetGameModeDropdown();
	}


	#region subscribing/unsubscribing to events;

	// having the // comment at the end of the event sub means that i have checked the things are correctly fired (only once) for the situation.
	// i have to make sure that error cases fire only one event until exited the situation.
	void Start() {
		MyLobby.LobbyFull += LobbyFull;
		MyLobby.SceneLoadingError += LoadingSceneError;
		MyLobby.LoadingNextScene += OpenLoadingPanel;
		MyLobby.LoadingCountdown.OnValueChanged += LoadingCountdown;
		MyLobby.LoadingSceneBool.OnValueChanged += LoadingSceneStateChange;

		LobbyManager.AuthenticationBegin += OpenLoadingPanel;
		LobbyManager.AuthenticationSuccess += CloseTransitionPanels;
		LobbyManager.AuthenticationSuccess += ListLobbyRefresh;
		LobbyManager.AuthenticationFailure += AuthenticationFail;

		LobbyManager.LobbyCreationBegin += OpenLoadingPanel;
		MyLobby.LobbyCreatedEvent += LobbyCreationSuccess;
		LobbyManager.LobbyCreationFailure += LobbyCreationFail;

		LobbyManager.HearbeatFailure += HearbeatFail;

		LobbyManager.LobbyJoinBegin += OpenLoadingPanel;
		LobbyNetcodeManager.ClientStartSuccess += LobbyJoined;
		LobbyManager.LobbyJoinFailure += LobbyJoinFail;

		LobbyManager.LeaveLobbyBegin += LeaveLobbyBegin;
		LobbyManager.LeaveLobbyComplete += LeaveLobbyComplete;

		LobbyManager.ListLobbySuccess += LobbyListFound;
		LobbyManager.ListLobbyFailure += ListLobbiesFail;
	}
	void OnDestroy() {
		MyLobby.LobbyFull -= LobbyFull;
		MyLobby.SceneLoadingError -= LoadingSceneError;
		MyLobby.LoadingNextScene -= OpenLoadingPanel;
		MyLobby.LoadingCountdown.OnValueChanged -= LoadingCountdown;
		MyLobby.LoadingSceneBool.OnValueChanged -= LoadingSceneStateChange;

		LobbyManager.AuthenticationBegin -= OpenLoadingPanel;
		LobbyManager.AuthenticationSuccess -= CloseTransitionPanels;
		LobbyManager.AuthenticationSuccess -= ListLobbyRefresh;
		LobbyManager.AuthenticationFailure -= AuthenticationFail;

		LobbyManager.LobbyCreationBegin -= OpenLoadingPanel;
		MyLobby.LobbyCreatedEvent -= LobbyCreationSuccess;
		LobbyManager.LobbyCreationFailure -= LobbyCreationFail;

		LobbyManager.HearbeatFailure -= HearbeatFail;

		LobbyManager.LobbyJoinBegin -= OpenLoadingPanel;
		LobbyNetcodeManager.ClientStartSuccess -= LobbyJoined;
		LobbyManager.LobbyJoinFailure -= LobbyJoinFail;

		LobbyManager.LeaveLobbyBegin -= LeaveLobbyBegin;
		LobbyManager.LeaveLobbyComplete -= LeaveLobbyComplete;

		LobbyManager.ListLobbySuccess -= LobbyListFound;
		LobbyManager.ListLobbyFailure -= ListLobbiesFail;
	}




	#endregion

	#region Interaction functions
	public static event System.Action GoingToMainMenu;
	public void GoToScene(string scene) {
		if (scene == "MainMenu") GoingToMainMenu?.Invoke();
		//have to stop NGO and lobby if main menu
		SceneManager.LoadScene(scene);
	}
	public void RetryAuthentication() {
		LobbyManager.Instance.Authenticate();
	}


	[SerializeField] GameObject lobbyCreationPanel;
	public TMP_InputField lobbyName, lobbyCode;
	public TMP_Dropdown lobbyModeDropDown, lobbyPlayerNumDropDown;

	public void GoToCreateLobby() {
		//reset lobby name and mode and maxplayer
		lobbyModeDropDown.Set(0);
		lobbyPlayerNumDropDown.Set(0);
		lobbyName.text = "New Lobby";
		lobbyCreationPanel.SetActive(true);
	}
	public void CloseLobbyCreation() {
		lobbyCreationPanel.SetActive(false);
	}

	List<string> gameModeOptions;
	void SetGameModeDropdown() {
		gameModeOptions = new List<string>(Enum.GetNames(typeof(GameMode)));
		lobbyModeDropDown.SetOptions(gameModeOptions);
	}

	//imma disable changing lobby mode and name once started.
	public string ConvertDropDownValueToGameModeString(int index) {
		return gameModeOptions[index];
	}
	public GameMode ConvertDropDownValueToGameMode(int index) {
		Enum.TryParse(gameModeOptions[index], out GameMode mode);
		return mode;
	}

	public void CreateLobby() {
		LobbyManager.Instance.CreateLobby(lobbyName.text, ConvertDropDownValueToGameModeString(lobbyModeDropDown.value), lobbyPlayerNumDropDown.value + 2);
	}
	public void QuickJoin() {
		LobbyManager.Instance.QuickJoinLobby();
	}
	public void JoinLobbyByCode() {
		LobbyManager.Instance.JoinLobbyByCode(lobbyCode.text);
		lobbyCode.text = "";
	}

	public void LeaveLobby() {
		//you need to add UI stuff here as leave lobby itself i decided not to add ui stuff (as it is called EVERYWHERE)
		LobbyManager.Instance.LeaveLobby();
	}


	public void ListLobbyRefresh() {
		Task list = LobbyManager.Instance.ListLobbies();
	}


	#endregion







	#region Event Functions
	[SerializeField] GameObject LoadingPanel, ErrorPanel;
	[SerializeField] GameObject RetryAuthenticationBtn, CloseErrorPanelBtn;
	void OpenLoadingPanel() {
		HidePanelsExceptChosen(LoadingPanel);
	}
	public void CloseTransitionPanels() {
		// if (!LobbyNetcodeManager.CanStopSceneLoading) return;
		HidePanelsExceptChosen(null);
	}
	void HidePanelsExceptChosen(GameObject panelToOpen = null) {
		if (LobbyManager.Instance == null) return;
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
		startGameBtn.interactable = false;
		lobbyCreationPanel.SetActive(false);
		LobbyUpdate(LobbyManager.Instance.hostLobby);
		ToggleLobby(true);
		CloseTransitionPanels();
	}
	void ToggleLobby(bool interactable) {
		if (lobbyPanel == null) return;
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
		startGameBtn.interactable = false;
		LobbyUpdate(LobbyManager.Instance.joinedLobby);
		ToggleLobby(true);
	}
	[SerializeField] TextMeshProUGUI lobbyModeTxt, lobbyCodeTxt;
	void LobbyUpdate(Lobby lobby) {
		if (lobby == null) return;
		lobbyModeTxt.text = lobby.Data[LobbyManager.GameMode].Value;
		if (LobbyManager.Instance.hostLobby != null) {
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
		// if (!LobbyNetcodeManager.CanStopSceneLoading) return;
		ToggleLobby(false);
	}
	void LeaveLobbyComplete() {
		HidePanelsExceptChosen();
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
	void ListLobbiesFail(List<Lobby> list) {
		lobbyListingFailNotificationObject.SetActive(true);
	}

	#endregion




	#region sceneLoading
	public Button leaveBtn, startGameBtn;
	public GameObject stopGameLoadBtn, loadCountdown;

	void LoadingSceneError() {
		ChangeToLoadingSceneMode(false);
	}
	void LoadingCountdown(int old, int n) {
		loadCountdown.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = n.ToString();
	}
	void LoadingSceneStateChange(bool old, bool loadingScene) {
		ChangeToLoadingSceneMode(loadingScene);
	}
	void ChangeToLoadingSceneMode(bool loadingScene) {
		if (NetworkManager.Singleton.IsServer) {
			startGameBtn.interactable = !loadingScene;
			stopGameLoadBtn.SetActive(loadingScene);
		}
		loadCountdown.SetActive(loadingScene);
		leaveBtn.interactable = !loadingScene;
	}
	void LoadingNextScene() {
		startGameBtn.interactable = false;
	}
	void DisableLeaving(bool lockOn) {
		if (lockOn) {
			OpenLoadingPanel();
		} else {
			HidePanelsExceptChosen();
		}
		leaveBtn.interactable = !lockOn;
		startGameBtn.interactable = false;
	}
	void LobbyFull(bool full) {
		startGameBtn.interactable = full;
	}




	#endregion

	public void EnableObject(GameObject o) {
		o.SetActive(true);
	}


}
