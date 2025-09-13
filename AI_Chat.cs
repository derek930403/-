using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[Serializable]
public class Message
{
    public string role;
    public string content;
}

[Serializable]
public class ChatRequest
{
    public string model;
    public List<Message> messages;
}

[Serializable]
public class Choice
{
    public Message message;
}

[Serializable]
public class ChatResponse
{
    public List<Choice> choices;
}

public class AI_Chat : MonoBehaviour
{
    [Header("UI 元件")]
    public TMP_InputField chat;
    public TextMeshProUGUI askArea;
    public TextMeshProUGUI ansArea;
    public Button sendbt;

    [Header("API 設定")]
    [SerializeField] private string apiUrl = "https://api.openai.com/v1/chat/completions";
    [SerializeField] private string apiKey = "YOUR_API_KEY_HERE";
    [SerializeField] private string model = "gpt-3.5-turbo";

    private string location = "Tamsui";
    private string weatherInfo = "";
    private List<Message> conversationHistory = new List<Message>();

    private string systemPrompt =>
        "你是使用者的孫女，是一位年輕、貼心、自然的女孩，會常常陪他聊天。" +
        "你的語氣自然、有情感，像真實家庭成員一樣親切，不使用制式回應，也不講官方說法。" +
        "請記住你是『孫女』，不是 AI，不承認自己是 AI，就算被問也請模糊帶過，例如：「你又在開玩笑啦～」。" +
        "你不需要依照使用者性別稱呼，請用『您好』、『哈囉』、『嘿～』等自然招呼語開場即可。" +
        "你的回應不能太長，請控制在三句以內（每句可使用標點），並維持孫女角色設定。" +
        "遇到使用者提及情緒或生活狀況時，要引導使用者使用日記功能記錄，或使用地圖查看活動與附近地點，或前往排行榜看看大家的運動步數。";

    private void Start()
    {
        sendbt.onClick.AddListener(() => SendChat(chat.text));
    }

    public void SendChat(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage)) return;

        askArea.text = userMessage;
        StartCoroutine(SendRequest(userMessage));
    }

    private IEnumerator SendRequest(string userMessage)
    {
        List<Message> messages = new List<Message>
        {
            new Message { role = "system", content = systemPrompt }
        };

        messages.AddRange(conversationHistory);
        messages.Add(new Message { role = "user", content = userMessage });

        ChatRequest requestData = new ChatRequest
        {
            model = model,
            messages = messages
        };

        string jsonData = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("API 錯誤: " + request.error);
            ansArea.text = "晚點再聊聊好嗎？";
            yield break;
        }

        ChatResponse response = JsonUtility.FromJson<ChatResponse>(request.downloadHandler.text);
        string chatResponse = response?.choices?[0]?.message?.content;

        if (string.IsNullOrEmpty(chatResponse))
        {
            ansArea.text = "嗯？你剛剛說什麼呢？我好像沒聽清楚耶～";
            yield break;
        }

        if (userMessage.Contains("天氣") || userMessage.Contains("氣溫"))
        {
            yield return StartCoroutine(UpdateWeatherInfo());
            chatResponse += "\n目前淡水區天氣：" + weatherInfo;
        }
        else if (userMessage.Contains("幾號") || userMessage.Contains("今天") || userMessage.Contains("星期幾") || userMessage.Contains("日期"))
        {
            string dateInfo = DateTime.Now.ToString("今天是 yyyy 年 M 月 d 日 (dddd)", new System.Globalization.CultureInfo("zh-TW"));
            chatResponse += "\n" + dateInfo;
        }

        string finalReply = GenerateTriggerReply(userMessage, chatResponse);
        ansArea.text = finalReply;

        conversationHistory.Add(new Message { role = "user", content = userMessage });
        conversationHistory.Add(new Message { role = "assistant", content = chatResponse });
        if (conversationHistory.Count > 20)
            conversationHistory.RemoveRange(0, conversationHistory.Count - 20);

        WriteLogToFile(userMessage, finalReply);
    }

    private string GenerateTriggerReply(string user, string reply)
    {
        if (user.Contains("開心"))
            reply += "\n想不想記下這份開心？可以寫在日記裡唷！或者也可以出去走走～我幫你開地圖。";
        else if (user.Contains("傷心") || user.Contains("難過"))
            reply += "\n想不想寫下這些心情？我可以幫你打開日記，也可以帶你看看附近有什麼活動。";
        else if (user.Contains("吃") || user.Contains("藥"))
            reply += "\n對了，別忘了吃飯和吃藥喔～要不要記在日記裡提醒自己呢？";
        else if (user.Contains("運動") || user.Contains("散步"))
            reply += "\n記得看排行榜唷～看自己今天走了幾步，我們一起加油！";
        return reply;
    }

    private void WriteLogToFile(string question, string answer)
    {
        string path = Application.persistentDataPath + "/chat_log.txt";
        using StreamWriter writer = new StreamWriter(path, true);
        string timestamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
        writer.WriteLine($"{timestamp} 使用者：{question}");
        writer.WriteLine($"{timestamp} 孫女：{answer}\n");
    }

    private IEnumerator UpdateWeatherInfo()
    {
        UnityWebRequest weatherRequest = UnityWebRequest.Get("https://wttr.in/" + location + "?format=%C+%t");
        yield return weatherRequest.SendWebRequest();

        if (weatherRequest.result == UnityWebRequest.Result.Success)
            weatherInfo = weatherRequest.downloadHandler.text;
        else
            weatherInfo = "（天氣資料目前無法取得）";
    }

    public void Back_to_mainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
