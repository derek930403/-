using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Chat_Animation : MonoBehaviour
{
    [Header("UI 元件")]
    [SerializeField] private Button chat_btn;
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private Animator ani;
    [SerializeField] private AI_Chat aiChat;

    [Header("動畫設定")]
    public string triggerName = "Play";
    private bool hasPlayed = false;

    private void Start()
    {
        if (chat_btn != null)
            chat_btn.onClick.AddListener(send);
    }

    public void send()
    {
        Debug.Log("press!");

        string userMessage = chatInputField?.text.Trim();
        if (string.IsNullOrEmpty(userMessage)) return;

        // 呼叫 AI 對話
        if (aiChat != null)
        {
            aiChat.SendChat(userMessage);
        }

        // 播放動畫（僅一次）
        if (!hasPlayed && ani != null)
        {
            Debug.Log("act!!");
            ani.SetTrigger(triggerName);
            hasPlayed = true;
        }

        // 清空輸入欄位
        if (chatInputField != null)
        {
            chatInputField.text = "";
        }
    }
}
