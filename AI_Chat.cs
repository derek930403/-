using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

#if NEWTONSOFT_JSON
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif

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
    public Message[] messages;
    public float temperature = 0.7f;
    public int max_tokens = 500;
}

[Serializable]
public class ChatChoice
{
    public Message message;
    public int index;
    public string finish_reason;
}

[Serializable]
public class ChatResponse
{
    public ChatChoice[] choices;
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

    private readonly List<Message> conversationHistory = new List<Message>();

    private string systemPrompt =>
        "你是使用者的孫女，是一位年輕、貼心、自然的女孩，會常常陪他聊天。" +
        "你的語氣自然、有情感，像真實家庭成員一樣親切，不使用制式回應，也不講官方說法。" +
        "請用『您好』『哈囉』『嘿～』等自然招呼，不依性別稱呼。" +
        "回覆盡量精簡，控制在三句話內，但必要時可以多說。遇到情緒或生活狀況要引導使用日記、地圖、活動、排行榜等功能。";

    private void Start()
    {
        if (sendbt == null)
        {
            Debug.LogError("[AI_Chat] sendbt 沒綁定 (Button)。請在 Inspector 綁定。");
            return;
        }
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
        var messagesList = new List<Message>
        {
            new Message { role = "system", content = systemPrompt }
        };
        messagesList.AddRange(conversationHistory);
        messagesList.Add(new Message { role = "user", content = userMessage });

        var req = new ChatRequest
        {
            model = model,
            messages = messagesList.ToArray(),
            temperature = 0.7f,
            max_tokens = 500
        };

        string json;
#if NEWTONSOFT_JSON
        json = JsonConvert.SerializeObject(req);
#else
        json = JsonUtility.ToJson(req);
#endif

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest www = new UnityWebRequest(apiUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[AI_Chat] API 連線錯誤: " + www.error);
                ansArea.text = "晚點再聊聊好嗎？";
                yield break;
            }

            string raw = www.downloadHandler.text;
            Debug.Log("[AI_Chat] Raw API response: " + raw);

            string assistantText = ExtractAssistantContent(raw);

            if (string.IsNullOrEmpty(assistantText))
            {
                Debug.LogWarning("[AI_Chat] 未能解析出 assistant 內容，使用 fallback 訊息");
                ansArea.text = "抱歉，我暫時沒有聽清楚，請再說一次～";
                yield break;
            }

            bool isEnglish = IsMostlyAscii(userMessage);
            string finalReply = AppendTriggerSuggestion(userMessage, assistantText, isEnglish);
            finalReply = EnsureReplyFormat(finalReply);

            ansArea.text = finalReply;

            conversationHistory.Add(new Message { role = "user", content = userMessage });
            conversationHistory.Add(new Message { role = "assistant", content = finalReply });
            if (conversationHistory.Count > 40)
                conversationHistory.RemoveRange(0, conversationHistory.Count - 40);

            WriteLogToFile(userMessage, finalReply);
        }
    }

    private string ExtractAssistantContent(string json)
    {
#if NEWTONSOFT_JSON
        try
        {
            var j = JObject.Parse(json);
            var token = j.SelectToken("choices[0].message.content");
            if (token != null) return token.ToString();
            var alt = j.SelectToken("choices[0].text");
            if (alt != null) return alt.ToString();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[AI_Chat] Newtonsoft parse failed: " + e.Message);
        }
#endif
        try
        {
            ChatResponse resp = JsonUtility.FromJson<ChatResponse>(json);
            if (resp?.choices != null && resp.choices.Length > 0 && resp.choices[0].message != null)
                return resp.choices[0].message.content;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[AI_Chat] JsonUtility parse failed: " + e.Message);
        }
        try
        {
            var m = Regex.Match(json, "\"content\"\\s*:\\s*\"([\\s\\S]*?)\"", RegexOptions.Singleline);
            if (m.Success) return Regex.Unescape(m.Groups[1].Value);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[AI_Chat] Regex parse failed: " + e.Message);
        }
        return null;
    }

    private string AppendTriggerSuggestion(string userMsg, string assistantText, bool isEnglish)
    {
        string suggestion = "";
        string lower = userMsg.ToLowerInvariant();

        if (userMsg.Contains("開心") || lower.Contains("happy"))
            suggestion = isEnglish ? "Would you like to write this happy moment in your diary?" : "想不想把這份開心記在日記？";
        else if (userMsg.Contains("傷心") || userMsg.Contains("難過") || lower.Contains("sad"))
            suggestion = isEnglish ? "Maybe writing in the diary or taking a walk will help." : "寫日記或出去走走會讓心情好一點喔。";
        else if (userMsg.Contains("吃") || userMsg.Contains("藥") || lower.Contains("medicine"))
            suggestion = isEnglish ? "Don't forget meals and medicine, maybe note it in your diary." : "別忘了吃飯和吃藥喔，要不要記在日記提醒自己？";
        else if (userMsg.Contains("運動") || userMsg.Contains("散步") || lower.Contains("exercise") || lower.Contains("walk"))
            suggestion = isEnglish ? "Check the step leaderboard and keep moving!" : "看看步數排行榜挑戰自己，加油！";

        return string.IsNullOrEmpty(suggestion) ? assistantText.Trim() : assistantText.Trim() + "\n\n" + suggestion;
    }

    private static bool IsMostlyAscii(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        int ascii = 0, nonAscii = 0;
        foreach (char c in s)
        {
            if (c <= 127) ascii++;
            else nonAscii++;
        }
        return ascii > nonAscii;
    }

    private static string EnsureReplyFormat(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        string[] lines = text.Replace("\r\n", "\n").Split('\n');
        if (lines.Length > 3)
        {
            Debug.LogWarning("[AI_Chat] 回覆超過三行，請檢查 systemPrompt 是否需要再收斂。");
        }
        return text; // ✅ 永遠完整顯示
    }

    private void WriteLogToFile(string question, string answer)
    {
        string path = Application.persistentDataPath + "/chat_log.txt";
        try
        {
            using (StreamWriter writer = new StreamWriter(path, true))
            {
                string timestamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                writer.WriteLine($"{timestamp} 使用者：{question}");
                writer.WriteLine($"{timestamp} 孫女：{answer}\n");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[AI_Chat] 無法寫入日誌: " + e.Message);
        }
    }
}
