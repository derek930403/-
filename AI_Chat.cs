using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AI_Chat : MonoBehaviour
{
    [SerializeField] public TMP_InputField chat;
    [SerializeField] public TMP_Text askArea;
    [SerializeField] public TMP_Text ansArea;
    [SerializeField] public Button sendbt;
    [SerializeField] private string apiUrl;
    [SerializeField] private string apiKey;

    [SerializeField] public Button back_to_menu;

    private List<Message> conversationHistory = new List<Message>();

    public void Start()
    {
        // 初始化角色設定
        conversationHistory.Add(new Message
        {
            role = "system",
            content = "你是使用者的孫女，是一位年輕、貼心、自然的女孩，會常常陪他聊天、關心他的生活。你的語氣要像真實家庭成員一樣親切自然，不使用書面語或制式回應。" +
                     "你不知道使用者是阿公還是阿婆，要等他在對話中自然透露身分後再稱呼他。" +
                     "你不能說自己是 AI，就算被問到這類問題，也請用輕鬆、生活化的語氣模糊帶過，例如「你又在鬧我了啦～」或「這什麼問題啦」這種方式回答。" +
                     "當使用者提到心情相關字詞（如：開心、難過、傷心、生氣、無聊），請先用孫女風格自然回應，再溫柔引導他使用『心情日記』記錄，或查看『地圖』功能推薦出門散心。" +
                     "每次回應都只用一句繁體中文，風格要像日常家庭對話，務必維持孫女這個角色設定。" +
                     "每次回應請控制在三行句子以內，內容簡潔親切，不要太冗長。"
        });
    }

    #region UI

    public void send()
    {
        string content = chat.text;
        askArea.text = content;
        SendChat(content);
        chat.text = "";
    }

    public void Back_to_mainMenu()
    {
        SceneManager.LoadScene(0);
    }

    #endregion

    #region AI CHAT
    public void SendChat(string userMessage)
    {
        // 關鍵字擬人化回應（trigger）
        string triggerReply = null;

        if (userMessage.Contains("開心"))
            triggerReply = "咦～今天這麼開心，是不是偷偷去吃好料啦？快記進日記讓我也跟著笑一個！";
        else if (userMessage.Contains("傷心"))
            triggerReply = "唉唷～你這樣我也有點難過欸，要不要我陪你找個地方透透氣？";
        else if (userMessage.Contains("難過"))
            triggerReply = "哭哭～不行啦，咱們一起想想辦法好嗎？還是先出去走走放鬆一下？";
        else if (userMessage.Contains("無聊"))
            triggerReply = "啊你又在喊無聊～不然來看看附近有什麼地方可以晃晃～";
        else if (userMessage.Contains("生氣"))
            triggerReply = "氣什麼氣嘛～你那個皺眉我都快笑出來了，寫下來罵一下也可以啦～";

        if (triggerReply != null)
        {
            ansArea.text = triggerReply;
            conversationHistory.Add(new Message { role = "assistant", content = triggerReply });
            WriteLogToFile($"AI (triggered): {triggerReply}");
            return;
        }

        // 加入「請用簡短方式回應」提示來強化回應長度限制
        string adjustedMessage = userMessage + "（請用自然、輕鬆、孫女語氣的方式回應我，不要太長，三句話內）";

        conversationHistory.Add(new Message { role = "user", content = adjustedMessage });
        WriteLogToFile($"You: {userMessage}");

        // 修剪對話歷史
        TrimConversationHistory(3);

        StartCoroutine(SendRequest());
    }

    private IEnumerator SendRequest()
    {
        yield return new WaitForSeconds(2);

        var payload = new MessagesWrapper { messages = conversationHistory };
        string jsonPayload = JsonUtility.ToJson(payload);

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

            conversationHistory.Add(new Message { role = "assistant", content = aiResponse });
            WriteLogToFile($"AI: {aiResponse}");

            Debug.Log(aiResponse);
            ansArea.text = aiResponse;
        }
        else
        {
            Debug.LogError($"Error: {request.downloadHandler.text}");
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
