using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class scrolltest : MonoBehaviour {
  public GameObject txtObj;
  public Transform holder;
  RectTransform holderRT;
  float topBottomPadding = 5f;
  float rightLeftPadding = 20f;

  void Start() {
    holderRT = holder.GetComponent<RectTransform>();
  }
  public string chat;
  public void Chat1() {
    AddChat(chat);
  }
  public void Chat2() {
    AddChat("laskdjf;laskdfj\na;sldfkjas;lkdfj");
  }
  void AddChat(string msg) {
    float yPos = holderRT.anchoredPosition.y;

    GameObject newChat = Instantiate(txtObj, holder);
    TextMeshProUGUI txtT = newChat.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
    txtT.text = msg;
    RectTransform txtRT = txtT.GetComponent<RectTransform>();
    Vector2 prefSize = txtT.GetPreferredValues(holderRT.rect.width - rightLeftPadding * 2f, Mathf.Infinity);
    txtRT.sizeDelta = new Vector2(holderRT.rect.width - rightLeftPadding * 2f, (prefSize.y));

    RectTransform txtHolderRT = newChat.GetComponent<RectTransform>();
    txtHolderRT.sizeDelta = new Vector2(holderRT.rect.width, prefSize.y + 2f * topBottomPadding);

    if (Mathf.Abs(yPos) < 0.05f) {
      holderRT.anchoredPosition = new Vector2(holderRT.anchoredPosition.x, 0f);
      LayoutRebuilder.ForceRebuildLayoutImmediate(holderRT);
      return;
    }
    // print(yPos);
    // print(txtHolderRT.sizeDelta.y);

    holderRT.anchoredPosition = new Vector2(holderRT.anchoredPosition.x, yPos - (prefSize.y + 2f * topBottomPadding + 5f));

    LayoutRebuilder.ForceRebuildLayoutImmediate(holderRT);




  }


  IEnumerator EndOfFrameCall(System.Action ftn) {
    yield return new WaitForEndOfFrame();
    ftn();
  }




}
