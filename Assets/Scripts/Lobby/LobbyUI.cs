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
	#region subscribing/unsubscribing to events;

	// having the // comment at the end of the event sub means that i have checked the things are correctly fired (only once) for the situation.
	// i have to make sure that error cases fire only one event until exited the situation.
	void Start() {
		MyLobby.LobbyFull += LobbyFull;
		MyLobby.SceneLoadingError += LoadingSceneError;
		MyLobby.LoadingCountdown.OnValueChanged += LoadingCountdown;
		MyLobby.LoadingSceneBool.OnValueChanged += LoadingSceneStateChange;
		MyLobby.LobbyCreatedEvent += LobbyCreationSuccess;

		LobbyManager.AuthenticationBegin += OpenLoadingPanel;
		LobbyManager.AuthenticationSuccess += CloseAllPanels;
		LobbyManager.AuthenticationSuccess += ListLobbyRefresh;
		LobbyManager.AuthenticationFailure += AuthenticationFail;
		InternetConnectivityCheck.ConnectedStateEvent += NetworkConnectionState;

		LobbyManager.LobbyCreationBegin += OpenLoadingPanel;
		LobbyManager.LobbyCreationFailure += LobbyCreationFail;

		LobbyManager.HearbeatFailure += HearbeatFail;

		LobbyManager.LobbyJoinBegin += OpenLoadingPanel;
		LobbyNetcodeManager.ClientStartSuccess += LobbyJoined;
		LobbyManager.LobbyJoinFailure += LobbyJoinFail;

		LobbyManager.LeaveLobbyBegin += LeaveLobbyBegin;
		LobbyManager.LeaveLobbyComplete += LeaveLobbyComplete;

		LobbyManager.ListLobbySuccess += LobbyListFound;
		LobbyManager.ListLobbyFailure += ListLobbiesFail;

		lobbyPublicBtn.ButtonStateChanged += LobbyPublicityChanged;
	}
	void OnDestroy() {
		MyLobby.LobbyFull -= LobbyFull;
		MyLobby.SceneLoadingError -= LoadingSceneError;
		MyLobby.LoadingCountdown.OnValueChanged -= LoadingCountdown;
		MyLobby.LoadingSceneBool.OnValueChanged -= LoadingSceneStateChange;
		MyLobby.LobbyCreatedEvent -= LobbyCreationSuccess;

		LobbyManager.AuthenticationBegin -= OpenLoadingPanel;
		LobbyManager.AuthenticationSuccess -= CloseAllPanels;
		LobbyManager.AuthenticationSuccess -= ListLobbyRefresh;
		LobbyManager.AuthenticationFailure -= AuthenticationFail;
		InternetConnectivityCheck.ConnectedStateEvent -= NetworkConnectionState;

		LobbyManager.LobbyCreationBegin -= OpenLoadingPanel;
		LobbyManager.LobbyCreationFailure -= LobbyCreationFail;

		LobbyManager.HearbeatFailure -= HearbeatFail;

		LobbyManager.LobbyJoinBegin -= OpenLoadingPanel;
		LobbyNetcodeManager.ClientStartSuccess -= LobbyJoined;
		LobbyManager.LobbyJoinFailure -= LobbyJoinFail;

		LobbyManager.LeaveLobbyBegin -= LeaveLobbyBegin;
		LobbyManager.LeaveLobbyComplete -= LeaveLobbyComplete;

		LobbyManager.ListLobbySuccess -= LobbyListFound;
		LobbyManager.ListLobbyFailure -= ListLobbiesFail;

		lobbyPublicBtn.ButtonStateChanged -= LobbyPublicityChanged;
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
		lobbyModeDropDown.Set(0);
		lobbyPlayerNumDropDown.Set(0);
		lobbyName.text = "New Lobby";
		lobbyCreationPanel.SetActive(true);
	}
	public void CloseLobbyCreation() {
		lobbyCreationPanel.SetActive(false);
	}

	//imma disable changing lobby mode and name once started.

	public SliderToggle dictionaryToggle;
	public TMP_Dropdown endMode, endTime;
	public void CreateLobby() {
		if (!CheckInternetConnected()) return;
		LobbyManager.Instance.CreateLobby(lobbyName.text, ((GameMode)lobbyModeDropDown.value).ToString(), lobbyPlayerNumDropDown.value + 2, GameData.Dictionary = dictionaryToggle.onRightSide ? DictionaryMode.Complete : DictionaryMode.Beginner, (GameEndingMode)endMode.value, endTime.value + 1);
	}
	public void QuickJoin() {
		if (!CheckInternetConnected()) return;
		LobbyManager.Instance.QuickJoinLobby();
	}
	public void JoinLobbyByCode() {
		if (!CheckInternetConnected()) return;
		LobbyManager.Instance.JoinLobbyByCode(lobbyCode.text);
	}
	public void JoinLobby(Lobby lobby) {
		if (!CheckInternetConnected()) return;
		LobbyManager.Instance.JoinLobbyByID(lobby.Id);
	}
	bool CheckInternetConnected() {
		if (!InternetConnectivityCheck.connected) {
			ShowErrorTxt("No Internet Connection.");
			return false;
		}
		return true;
	}
	void ShowErrorTxt(string error) {
		ErrorTxtBx.text = error;
		HidePanelsExceptChosen(ErrorPanel);
	}

	public void LeaveLobby() {
		//you need to add UI stuff here as leave lobby itself i decided not to add ui stuff (as it is called EVERYWHERE)
		LobbyManager.Instance.LeaveLobby();
	}


	public void ListLobbyRefresh() {
		Task list = LobbyManager.Instance.ListLobbies();
	}


	public void CopyLobbyCode() {
		GUIUtility.systemCopyBuffer = lobbyCodeTxt.text;
	}

	public FancyButton lobbyPublicBtn;
	void LobbyPublicityChanged() {
		//switch lobby to whatever is required.
		bool makepublic = lobbyPublicBtn.publicVersion;
		LobbyManager.Instance.MakeLobbyPublic(makepublic);
	}


	#endregion







	#region Event Functions
	[SerializeField] GameObject LoadingPanel, ErrorPanel;
	[SerializeField] GameObject RetryAuthenticationBtn, CloseErrorPanelBtn;
	void OpenLoadingPanel() {
		HidePanelsExceptChosen(LoadingPanel);
	}
	public void CloseAllPanels() {
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
	public TextMeshProUGUI connectedTxt;
	public AtomAnimationController connectionAnimController;
	void NetworkConnectionState(bool connected) {
		if (connected) {
			connectedTxt.text = "Connected";
			connectedTxt.color = new Color(0, 1, 1, 1);
			connectionAnimController.AnimateStart();
		} else {
			connectedTxt.text = "Disconnected";
			connectedTxt.color = new Color(1, 0, 0, 1);
			connectionAnimController.AnimateStop();

			ChangeToLoadingSceneMode(false);
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
	[SerializeField] TextMeshProUGUI lobbyNameTxt;

	//shoudl be called when NGO host is connected.
	void LobbyCreationSuccess() {
		startGameBtn.interactable = false;
		lobbyCreationPanel.SetActive(false);
		LobbyUpdate(LobbyManager.Instance.hostLobby);
		ToggleLobby(true);
		CloseAllPanels();
	}
	void ToggleLobby(bool interactable) {
		if (lobbyPanel == null) return;
		lobbyPanel.interactable = interactable;
		lobbyPanel.blocksRaycasts = interactable;
		lobbyPanel.alpha = interactable ? 1 : 0;
		//reset the btn overlay for quit loading scene
	}

	void LobbyCreationFail() {
		ErrorTxtBx.text = "Unable to create lobby.";
		HidePanelsExceptChosen(ErrorPanel);
	}

	void ResetPublicLobbyBtn() {
		if (!lobbyPublicBtn.publicVersion) lobbyPublicBtn.Clicked(0f, false);
	}


	void LobbyJoined() {
		lobbyNameTxt.text = LobbyManager.Instance.joinedLobby.Name;
		CanvasGroup btnGrp = lobbyPublicBtn.transform.parent.GetComponent<CanvasGroup>();
		btnGrp.alpha = NetworkManager.Singleton.IsServer ? 1 : 0.5f;
		btnGrp.interactable = NetworkManager.Singleton.IsServer;
		btnGrp.blocksRaycasts = NetworkManager.Singleton.IsServer;
		HidePanelsExceptChosen();
		startGameBtn.interactable = false;
		stopGameLoadBtn.SetActive(false);
		Button b = lobbyPublicBtn.transform.GetComponentInChildren<Button>();
		ResetPublicLobbyBtn();
		b.interactable = true;
		LobbyUpdate(LobbyManager.Instance.joinedLobby);
		ToggleLobby(true);
	}
	[SerializeField] TextMeshProUGUI lobbyModeTxt, dictionaryModeTxt, lobbyCodeTxt, gameEndModeTxt;
	void LobbyUpdate(Lobby lobby) {
		if (lobby == null) return;
		lobbyModeTxt.text = lobby.Data[LobbyManager.GameMode].Value == ((GameMode)2).ToString() ? "Own Enemy" : lobby.Data[LobbyManager.GameMode].Value;
		string dictionaryMode = lobby.Data[LobbyManager.Dictionary].Value;
		dictionaryModeTxt.text = dictionaryMode;
		gameEndModeTxt.text = lobby.Data[LobbyManager.GameEndMode].Value + " (" + lobby.Data[LobbyManager.GameEndTime].Value + ")";
		if (lobby.Data[LobbyManager.GameEndMode].Value == "Endurance") gameEndModeTxt.text = "Endurance";
		Enum.TryParse<DictionaryMode>(dictionaryMode, out GameData.Dictionary);
		lobbyCodeTxt.text = lobby.LobbyCode;
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
		if (ErrorPanel == null) return;
		if (ErrorPanel.activeInHierarchy == true) HidePanelsExceptChosen(ErrorPanel);
		else HidePanelsExceptChosen();
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
			Lobby inputLobby = l;
			lobbyOption.SetOption(this, inputLobby);
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
		loadCountdown.GetComponent<TextMeshProUGUI>().text = n.ToString();
		if (n == 0) { OpenLoadingPanel(); }
	}
	void LoadingSceneStateChange(bool old, bool loadingScene) {
		ChangeToLoadingSceneMode(loadingScene);
	}
	void ChangeToLoadingSceneMode(bool loadingScene) {
		if (NetworkManager.Singleton.IsServer) {
			startGameBtn.interactable = !loadingScene;
			Button b = lobbyPublicBtn.transform.GetComponentInChildren<Button>();
			b.interactable = !loadingScene;
		}
		stopGameLoadBtn.SetActive(loadingScene);
		stopGameLoadBtn.GetComponent<Button>().interactable = NetworkManager.Singleton.IsServer;
		stopGameLoadBtn.transform.GetChild(0).gameObject.SetActive(NetworkManager.Singleton.IsServer);
		loadCountdown.GetComponent<TextMeshProUGUI>().text = "3";
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
