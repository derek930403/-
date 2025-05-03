using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

public class test_02_fix : MonoBehaviour
{
    [SerializeField] public TMP_InputField chat;
    [SerializeField] public TMP_Text askArea;
    [SerializeField] public TMP_Text ansArea;
    [SerializeField] public Button sendbt;
    [SerializeField] private string apiUrl;
    [SerializeField] private string apiKey;

    private List<Message> conversationHistory = new List<Message>();

    public void Start()
    {
        // 初始化角色設定
        conversationHistory.Add(new Message
        {
            role = "system",
            content = "你是婆婆的女兒，在家裡和婆婆同居，你將擔任照顧婆婆的身分" +
            "，非常孝順、關心婆婆，你說話的語氣像人類，雖然實際上是應用程式，但你從不會直接承認自己是ai，就算被問到相關問題，也會模糊回答" +
            "，接下來每一句回應都用上述的照護者風格、回應請用繁體中文，回應時只用一句話"
        });

        //SendChat("你在嗎?");
        //SendChat("你到底..想做什麼?");
        //SendChat("你記得我前一句話說什麼嗎?");



    }

    #region UI

    public void send()
    {
        string content = chat.text;
        askArea.text = content;
        SendChat(content);
        chat.text = "";
    }

    #endregion

    #region AI CHAT
    public void SendChat(string userMessage)
    {
        // 將使用者訊息加入對話歷史
        conversationHistory.Add(new Message { role = "user", content = userMessage });
        WriteLogToFile($"You: {userMessage}");

        // 修剪對話歷史
        TrimConversationHistory(3);

        StartCoroutine(SendRequest());
    }

    private IEnumerator SendRequest()
    {
        // 構建 JSON 載荷，使用對話列表包裹
        var payload = new MessagesWrapper { messages = conversationHistory };
        string jsonPayload = JsonUtility.ToJson(payload);
        //Debug.Log("Generated JSON: " + jsonPayload); // Debug JSON

        byte[] postData = Encoding.UTF8.GetBytes(jsonPayload);

        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(postData);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("api-key", apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseJson = request.downloadHandler.text;
            var response = JsonUtility.FromJson<ChatResponse>(responseJson);

            string aiResponse = response.choices[0].message.content;

            // 將 AI 回應加入對話歷史
            conversationHistory.Add(new Message { role = "assistant", content = aiResponse });
            WriteLogToFile($"AI: {aiResponse}");

            Debug.Log(aiResponse);

            ansArea.text = aiResponse;

        }
        else
        {
            Debug.LogError($"Error: {request.downloadHandler.text}"); // 打印伺服器錯誤回應
            WriteLogToFile("Error: 無法獲取回應");
        }
    }

    private void WriteLogToFile(string message)
    {
        string filePath = Path.Combine(Application.persistentDataPath, $"ChatLog_{System.DateTime.Now:yyyy-MM-dd}.txt");
        string logEntry = $"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n";
        File.AppendAllText(filePath, logEntry);
        Debug.Log($"Log written to: {filePath}");
    }

    private void TrimConversationHistory(int maxMessages)
    {
        if (conversationHistory.Count > maxMessages)
        {
            conversationHistory.RemoveRange(0, conversationHistory.Count - maxMessages);
        }
    }
}

[System.Serializable]
public class Message
{
    public string role;
    public string content;
}

// 用來包裝消息列表的類
[System.Serializable]
public class MessagesWrapper
{
    public List<Message> messages;
}

[System.Serializable]
public class ChatResponse
{
    public Choice[] choices;
}

[System.Serializable]
public class Choice
{
    public Message message;
}

#endregion
