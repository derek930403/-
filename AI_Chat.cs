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
    [SerializeField] private string model = "gpt-3.5-turbo"; // 可切換為 gpt-4o

    private string location = "Tamsui";
    private string weatherInfo = "";

    private readonly string systemPrompt =
        "你是使用者的孫女，是一位年輕、貼心、自然的女孩，會常常陪他聊天。" +
        "你的語氣自然、有情感，像真實家庭成員一樣親切，不使用制式回應，也不講官方說法。" +
        "請記住你是『孫女』，不是 AI，不承認自己是 AI，就算被問也請模糊帶過，例如：「你又在開玩笑啦～」。" +
        "你不知道使用者是阿公還是阿婆，請等他在對話中自然透露身份後再稱呼他。" +
        "你的回應不能太長，請控制在三句以內（每句可使用標點），並維持孫女角色設定。";

    private void Start()
    {
        sendbt.onClick.AddListener(() => SendChat(chat.text));
        StartCoroutine(UpdateWeatherInfo());
    }

    public void SendChat(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage)) return;

        askArea.text = userMessage;
        StartCoroutine(SendRequest(userMessage));
    }

    private IEnumerator SendRequest(string userMessage)
    {
        var messages = new List<Message>
        {
            new Message { role = "system", content = systemPrompt },
            new Message { role = "user", content = userMessage }
        };

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
            ansArea.text = "孫女現在有點連不上網，晚點再聊聊好嗎？";
            yield break;
        }

        ChatResponse response = JsonUtility.FromJson<ChatResponse>(request.downloadHandler.text);
        string chatResponse = response?.choices?[0]?.message?.content;

        if (string.IsNullOrEmpty(chatResponse))
        {
            ansArea.text = "嗯？你剛剛說什麼呢？我好像沒聽清楚耶～";
            yield break;
        }

        string finalReply = GenerateTriggerReply(userMessage, chatResponse);
        ansArea.text = finalReply + "\n" + weatherInfo;

        WriteLogToFile(userMessage, finalReply);
    }

    private string GenerateTriggerReply(string user, string reply)
    {
        if (user.Contains("開心"))
            reply += "\n想不想出門走走呀？我可以幫你打開地圖～";
        else if (user.Contains("傷心") || user.Contains("難過"))
            reply += "\n不然寫寫日記，或我們來聊聊也好～";
        else if (user.Contains("吃") || user.Contains("藥"))
            reply += "\n對了，別忘了吃飯和吃藥喔～";
        else if (user.Contains("運動") || user.Contains("散步"))
            reply += "\n你的步數也會上排行榜，我會幫你記得！";

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
            weatherInfo = "目前天氣：" + weatherRequest.downloadHandler.text;
        else
            weatherInfo = "（天氣資料目前無法取得）";
    }

    public void Back_to_mainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
