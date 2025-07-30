using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Chat_Animation : MonoBehaviour
{
    public TMP_InputField inputField;
    public Button sendButton;
    public GameObject chatBox;
    public Animator animator;
    public GameObject bubble;
    public GameObject chatTextArea;

    private bool isTyping = false;
    private string latestText = "";

    private void Start()
    {
        sendButton.onClick.AddListener(send);
    }

    public void send()
    {
        if (string.IsNullOrWhiteSpace(inputField.text)) return;

        latestText = inputField.text;
        Debug.Log("press!");
        chatBox.SetActive(true);
        StartCoroutine(AnimateText(latestText));
        inputField.text = "";
    }

    IEnumerator AnimateText(string content)
    {
        isTyping = true;
        bubble.SetActive(true);
        animator.Play("Typing");

        TMP_Text displayText = chatTextArea.GetComponent<TMP_Text>();
        displayText.text = "";

        foreach (char letter in content.ToCharArray())
        {
            displayText.text += letter;
            yield return new WaitForSeconds(0.03f);
        }

        animator.Play("Idle");
        isTyping = false;
    }
}
