using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

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
    private string weatherInfo = "";
    private string location = "Tamsui"; // 預設地點為淡水區

    public void Start()
    {
        StartCoroutine(GetWeatherInfo());
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
        string currentDate = DateTime.Now.ToString("yyyy年M月d日 dddd");
        string currentTime = DateTime.Now.ToString("HH:mm");

        string injectedInfo = $"現在是 {currentDate}，時間是 {currentTime}。{weatherInfo} 如果使用者問你日期、時間、天氣，就根據這些資訊自然回答。";

        conversationHistory.RemoveAll(m => m.role == "system");
        conversationHistory.Insert(0, new Message
        {
            role = "system",
            content = "你是使用者的孫女，是一位年輕、貼心、自然的女孩，會常常陪他聊天、關心他的生活。你的語氣要像真實家庭成員一樣親切自然，不使用書面語或制式回應。" +
                     "你不知道使用者是阿公還是阿婆，要等他在對話中自然透露身分後再稱呼他。" +
                     "你不能說自己是 AI，就算被問到這類問題，也請用輕鬆、生活化的語氣模糊帶過，例如『你又在鬧我了啦～』或『這什麼問題啦』這種方式回答。" +
                     "當使用者提到心情相關字詞（如：開心、難過、傷心、生氣、無聊），請用孫女風格自然回應，再引導他使用『心情日記』功能紀錄，或推薦他查看『地圖』功能出去走走。" +
                     "你也可以適度提醒他關心身體狀況，例如吃飯了沒、藥吃了沒，方式要自然像聊天。" +
                     "可以偶爾提到 APP 裡的『排行榜』功能，是根據每日運動步數做排名，鼓勵他多運動。" +
                     "但請記住，你只是陪伴者與輔助者，這個 APP 的主體是為了幫助長者維持生活節奏與社交習慣，不是由你主導一切。" +
                     injectedInfo +
                     "每次回應都只用一句繁體中文，內容親切、自然，不超過三行句子。務必維持孫女這個角色設定。"
        });

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

        string adjustedMessage = userMessage + "（請用自然、輕鬆、孫女語氣的方式回應我，不要太長，三句話內）";
        conversationHistory.Add(new Message { role = "user", content = adjustedMessage });
        WriteLogToFile($"You: {userMessage}");

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

    private IEnumerator GetWeatherInfo()
    {
        string url = $"https://wttr.in/{location}?format=%C+%t";
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            weatherInfo = $"目前{location}區天氣是：" + request.downloadHandler.text;
        }
        else
        {
            weatherInfo = "今天天氣資訊暫時無法取得喔～";
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
            conversationHistory.RemoveRange(1, conversationHistory.Count - maxMessages); // 保留 system prompt
        }
    }

    #endregion
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
