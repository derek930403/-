using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System.IO;

public class stt : MonoBehaviour
{
    private Button sttButton;
    private TMP_InputField inputField;
    private bool isRecording = false;
    private AudioClip recordedClip;
    private string filePath;
    private bool isButtonPressed = false;
    private float recordingStartTime;

    public void Start()
    {
        GameObject sttButtonObject = GameObject.Find("sttButton");
        GameObject inputObj = GameObject.Find("輸入框");

        if (sttButtonObject != null)
        {
            sttButton = sttButtonObject.GetComponent<Button>();
            sttButton.onClick.AddListener(ToggleRecording);
        }
        else
        {
            sttButton = null;
            Debug.LogError("未找到 sttButton");
        }

        if (inputObj != null)
        {
            inputField = inputObj.GetComponent<TMP_InputField>();
            if (inputField == null)
            {
                inputField = inputObj.GetComponentInChildren<TMP_InputField>();
            }
        }
        else
        {
            Debug.LogError("未找到 輸入框");
        }
    }

    public void ToggleRecording()
    {
        if (isButtonPressed) return;
        isButtonPressed = true;

        if (isRecording == false)
        {
            StartRecording();
        }
        else
        {
            StopRecording();
        }
        StartCoroutine(EnableButtonAfterDelay());
    }

    // 2秒後才能再按一次按鈕
    IEnumerator EnableButtonAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        isButtonPressed = false;
    }

    void StartRecording()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("沒麥");
            return;
        }
        recordedClip = Microphone.Start(null, false, 15, 16000);
        isRecording = true;
        recordingStartTime = Time.time; // 記錄開始時間
        Debug.Log("開始錄音...");
    }

    void StopRecording()
    {
        Microphone.End(null);
        isRecording = false;
        Debug.Log("錄音結束");
        float recordingDuration = Time.time - recordingStartTime;
        Debug.Log("錄音時長: " + recordingDuration + " 秒");

        filePath = Path.Combine(Application.persistentDataPath, "recorded_audio.wav");
        SaveWavFile(filePath, recordedClip);
        StartCoroutine(UploadToGoogleSTT(filePath));
    }
    void SaveWavFile(string path, AudioClip clip)
    {
        byte[] wavData = ConvertAudioClipToWav(clip);
        File.WriteAllBytes(path, wavData);
    }
    byte[] ConvertAudioClipToWav(AudioClip clip)
    {
        int sampleCount = clip.samples * clip.channels;
        float[] samples = new float[sampleCount];
        clip.GetData(samples, 0);
        int headerSize = 44; // header array 44 bytes
        byte[] wavData = new byte[headerSize + sampleCount * 2];
        WriteWavHeader(wavData, clip);

        int offset = headerSize;
        for (int i = 0; i < sampleCount; i++)
        {
            short sample = (short)(samples[i] * short.MaxValue);
            wavData[offset++] = (byte)(sample & 0xff); // LSB (lowest byte)
            wavData[offset++] = (byte)((sample >> 8) & 0xff); // MSB (most significant byte)
        }

        return wavData;
    }

    // 音訊檔轉成wav格式 沒有套件只能手寫 我是天才王岑伃快誇我
    void WriteWavHeader(byte[] wavData, AudioClip clip)
    {
        int sampleRate = clip.frequency;
        int byteRate = sampleRate * clip.channels * 2; // 每秒16bit
        int dataSize = clip.samples * clip.channels * 2; // 每個樣本16bit
        int fileSize = 36 + dataSize; // RIFF, WAVE, fmt...
        WriteString(wavData, 0, "RIFF"); // 從 wavData的0bytes開始寫
        System.BitConverter.GetBytes(fileSize).CopyTo(wavData, 4);
        WriteString(wavData, 8, "WAVE");
        WriteString(wavData, 12, "fmt ");
        System.BitConverter.GetBytes(16).CopyTo(wavData, 16);  // 16bytes
        System.BitConverter.GetBytes((short)1).CopyTo(wavData, 20); // PCM格式
        System.BitConverter.GetBytes((short)clip.channels).CopyTo(wavData, 22);
        System.BitConverter.GetBytes(sampleRate).CopyTo(wavData, 24);
        System.BitConverter.GetBytes(byteRate).CopyTo(wavData, 28);
        System.BitConverter.GetBytes((short)(clip.channels * 2)).CopyTo(wavData, 32);
        System.BitConverter.GetBytes((short)16).CopyTo(wavData, 34);
        WriteString(wavData, 36, "data");
        System.BitConverter.GetBytes(dataSize).CopyTo(wavData, 40);
    }
    void WriteString(byte[] data, int offset, string value)
    {
        foreach (char c in value)
        {
            data[offset++] = (byte)c;
        }
    }
    IEnumerator UploadToGoogleSTT(string path)
    {
        byte[] audioData = File.ReadAllBytes(path);
        Debug.Log("Audio file size: " + audioData.Length);
        string base64Audio = System.Convert.ToBase64String(audioData);
        string json = "{ \"config\": { \"encoding\": \"LINEAR16\", \"sampleRateHertz\": 16000, \"languageCode\": \"zh-TW\" }, \"audio\": { \"content\": \"" + base64Audio + "\" } }";
        using (UnityWebRequest request = new UnityWebRequest("https://speech.googleapis.com/v1/speech:recognize?key=AIzaSyDvzWSFa-NCyvJBbIMWkHNlK2kz-BRx_FQ", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("成功轉錄: " + request.downloadHandler.text);
                string recognizedText = ExtractTranscription(request.downloadHandler.text);
                if (inputField != null)
                {
                    inputField.text = recognizedText;
                }
                else
                {
                    Debug.LogError("no 文字框");
                }
            }
            else
            {
                Debug.LogError("Google STT 失敗: " + request.error);
                Debug.LogError("回應內容: " + request.downloadHandler.text);
            }
        }
    }

    string ExtractTranscription(string jsonResponse)
    {
        var json = JsonUtility.FromJson<GoogleSTTResponse>(jsonResponse);
        if (json != null && json.results.Length > 0)
        {
            return json.results[0].alternatives[0].transcript;
        }
        return "語音辨識失敗";
    }
}

[System.Serializable]
public class GoogleSTTResponse
{
    public GoogleSTTResult[] results;
}

[System.Serializable]
public class GoogleSTTResult
{
    public GoogleSTTAlternative[] alternatives;
}

[System.Serializable]
public class GoogleSTTAlternative
{
    public string transcript;
}
