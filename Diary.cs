using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using Palmmedia.ReportGenerator.Core.Parser.Filtering;
//using UnityEngine.UIElements;

public class Diary : MonoBehaviour
{
    [SerializeField] public Button mood_1;
    [SerializeField] public Button mood_2;
    [SerializeField] public Button mood_3;
    [SerializeField] public Button mood_4;
    [SerializeField] public Button weather_1;
    [SerializeField] public Button weather_2;
    [SerializeField] public Button weather_3;
    [SerializeField] public Button weather_4;
    [SerializeField] public Button photo;
    [SerializeField] public Button back_to_menu;

    [SerializeField] public Button delete_all;//刪除所有日記內容

    [SerializeField] public RawImage displayImage;//圖片

    [SerializeField] public TMP_InputField content;//日記內容

    [SerializeField] public TextMeshProUGUI date;//日期


    void Start()
    {
        mood_1.interactable = true;
        mood_2.interactable = true;
        mood_3.interactable = true;
        mood_4.interactable = true;
        weather_1.interactable = true;
        weather_2.interactable = true;
        weather_3.interactable = true;
        weather_4.interactable = true;

        today_date();

        filePath = Path.Combine(Application.persistentDataPath, "daily_texts.json");
        currentDate = DateTime.Today;
        LoadFromFile();
        UpdateUI();

    }
    #region MyRegion
    /// <summary>
    /// 再按一次可以重新選擇?
    /// </summary>
    /// <param name="clicked"></param>
    
    private Button activeMoodButton = null;

    public void mood(Button clicked)
    {
        string dateKey = currentDate.ToString("yyyy-MM-dd");

        if (activeMoodButton == clicked)
        {
            // 如果再按一次同一個按鈕，就全部恢復可互動
            mood_1.interactable = true;
            mood_2.interactable = true;
            mood_3.interactable = true;
            mood_4.interactable = true;
            activeMoodButton = null;
            moodMap.Remove(dateKey);
        }
        else
        {
            // 否則讓其他按鈕不能互動
            mood_1.interactable = (clicked == mood_1);
            mood_2.interactable = (clicked == mood_2);
            mood_3.interactable = (clicked == mood_3);
            mood_4.interactable = (clicked == mood_4);
            activeMoodButton = clicked;
            moodMap[dateKey] = clicked.name;
        }
    }


    private Button activeWeatherButton = null;
    public void weather(Button clicked)
    {
        string dateKey = currentDate.ToString("yyyy-MM-dd");

        if (activeWeatherButton == clicked)
        {
            // 如果再按一次同一個按鈕，就全部恢復可互動
            weather_1.interactable = true;
            weather_2.interactable = true;
            weather_3.interactable = true;
            weather_4.interactable = true;
            activeWeatherButton = null;
            weatherMap.Remove(dateKey);
        }
        else
        {
            // 否則讓其他按鈕不能互動
            weather_1.interactable = (clicked == weather_1);
            weather_2.interactable = (clicked == weather_2);
            weather_3.interactable = (clicked == weather_3);
            weather_4.interactable = (clicked == weather_4);
            activeWeatherButton = clicked;
            weatherMap[dateKey] = clicked.name;
        }
    }

    [SerializeField] public AspectRatioFitter fitter;

    /// <summary>
    /// 選擇照片
    /// </summary>
    public void PickImage()
    {
        NativeGallery.GetImageFromGallery((path) =>
        {
            if (path != null)
            {
                Texture2D texture = NativeGallery.LoadImageAtPath(path, 1024);
                if (texture != null)
                {
                    // 設定圖片顯示在 RawImage 上
                    if (displayImage != null)
                    {
                        // 設置圖片
                        displayImage.texture = texture;

                        if (fitter != null)
                            fitter.enabled = true;

                        // 啟動 Coroutine 等下一幀調整比例
                        StartCoroutine(UpdateAspectRatioNextFrame(texture));

                        /*

                        // 獲取 RawImage 的大小
                        RectTransform rectTransform = displayImage.rectTransform;
                        float maxWidth = rectTransform.rect.width;
                        float maxHeight = rectTransform.rect.height;

                        // 計算圖片的寬高比
                        float textureWidth = texture.width;
                        float textureHeight = texture.height;
                        float textureRatio = textureWidth / textureHeight;

                        // 計算圖片顯示的寬度和高度，保持比例
                        float displayWidth = maxWidth;
                        float displayHeight = maxHeight;

                        if (textureRatio > 1) // 寬比高大
                        {
                            displayHeight = maxWidth / textureRatio; // 根據寬度計算高度
                        }
                        else // 高比寬大
                        {
                            displayWidth = maxHeight * textureRatio; // 根據高度計算寬度
                        }

                        // 設定 RawImage 的尺寸
                        displayImage.rectTransform.sizeDelta = new Vector2(displayWidth, displayHeight);
                        */

                        Debug.Log("設置圖片完成!");
                        
                    }
                    else
                    {
                        Debug.LogError("displayImage is null!");
                    }
                }
            }
        }, "選擇一張日記照片");

    }


    public void Back_to_mainMenu()
    {
        SceneManager.LoadScene(0);
    }


    public void today_date()
    {
        DateTime today = DateTime.Now;

        string[] weekDays = { "星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六" };
        string weekDay = weekDays[(int)today.DayOfWeek];

        string formattedDate = today.ToString("yyyy年M月d日 ") + weekDay;
        date.text = formattedDate;
    }
    #endregion

    
        public TMP_InputField inputField;
        public TMP_Text dateLabel; // 顯示目前是幾號
        

        private string filePath;
        private DateTime currentDate;

        [System.Serializable]
        public class DailyData
        {
            public List<DateEntry> entries = new List<DateEntry>();
        }

        [System.Serializable]
        public class DateEntry
        {
            public string date;
            public string text;
            public string imagePath;
            public string mood;
            public string weather;
        }

        private Dictionary<string, string> dateTextMap = new Dictionary<string, string>();
        private Dictionary<string, string> imagePaths = new Dictionary<string, string>();  // 儲存圖片路徑
        private Dictionary<string, string> moodMap = new Dictionary<string, string>();
        private Dictionary<string, string> weatherMap = new Dictionary<string, string>();

    public void GetImage()
    {
        NativeGallery.GetImageFromGallery((path) =>
        {
            if (path != null)
            {
                Texture2D texture = NativeGallery.LoadImageAtPath(path, -1);
                if (texture == null)
                {
                    Debug.LogError("讀取圖片失敗！");
                    return;
                }

                if (displayImage.texture != null)
                    Destroy(displayImage.texture);

                displayImage.texture = texture;

                // 儲存到 App 私有資料夾
                string today = currentDate.ToString("yyyy-MM-dd");
                string imageFileName = today + ".png";
                string savePath = Path.Combine(Application.persistentDataPath, imageFileName);

                File.Copy(path, savePath, true); // 寫入私有空間，方便讀寫

                // ✅ 新增：儲存一份到圖庫
                NativeGallery.SaveImageToGallery(savePath, "MyApp", imageFileName);

                imagePaths[today] = savePath;
                SaveToFile();
                Debug.Log("圖片儲存成功：" + savePath);
            }
        }, "選擇一張圖片", "image/*");
    }



    public void SaveTodayText()
        {
            string today = currentDate.ToString("yyyy-MM-dd");
            string text = inputField.text;

            dateTextMap[today] = text; // 更新/新增今天的內容
                                       //SaveImage();

        if (displayImage.texture != null)
        {
            Texture original = displayImage.texture;

            // 建立 RenderTexture
            RenderTexture rt = new RenderTexture(original.width, original.height, 0);
            RenderTexture.active = rt;

            // 把原始 texture 畫上去
            Graphics.Blit(original, rt);

            // 建立一個新的 readable 的 Texture2D
            Texture2D readableTex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            readableTex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            readableTex.Apply();

            // 清理
            RenderTexture.active = null;
            rt.Release();

            // 儲存圖片
            byte[] bytes = readableTex.EncodeToPNG();
            string imageFileName = today + ".png";
            string savePath = Path.Combine(Application.persistentDataPath, imageFileName);
            File.WriteAllBytes(savePath, bytes);
            imagePaths[today] = savePath;

            // 可選：儲存到圖庫
            NativeGallery.SaveImageToGallery(savePath, "MyApp", imageFileName, (success, path) =>
            {
                Debug.Log("儲存到圖庫：" + success + "，位置：" + path);
            });

            Destroy(readableTex);
        }

        SaveToFile();
        Debug.Log("儲存成功");
        }

        public void ChangeDay(int offset)
        {
            SaveTodayText(); // 先儲存當前頁
            currentDate = currentDate.AddDays(offset);
            UpdateUI();
        }

        private void UpdateUI()
        {
            string dateKey = currentDate.ToString("yyyy-MM-dd");
            dateLabel.text = dateKey;

            if (dateTextMap.ContainsKey(dateKey))
                inputField.text = dateTextMap[dateKey];
            else
                inputField.text = "";

        if (displayImage.texture != null)
        {
            Destroy(displayImage.texture); // 釋放舊的圖片材質
        }

        if (imagePaths.ContainsKey(dateKey) && File.Exists(imagePaths[dateKey]))
        {
            /*    byte[] bytes = File.ReadAllBytes(imagePaths[dateKey]);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
                tex.LoadImage(bytes);
                displayImage.texture = tex;

                displayImage.material = null;*/

            LoadImage(imagePaths[dateKey]);
        }
        else
        {
            displayImage.texture = null; // 或者用一張預設圖片
         //   displayImage.rectTransform.sizeDelta = new Vector2(1920, 1080);
        }

        if (moodMap.ContainsKey(dateKey))
        {
            string moodName = moodMap[dateKey];

            // 依 mood name 控制四個按鈕狀態
            mood_1.interactable = (mood_1.name == moodName);
            mood_2.interactable = (mood_2.name == moodName);
            mood_3.interactable = (mood_3.name == moodName);
            mood_4.interactable = (mood_4.name == moodName);

            // 記錄當前活躍按鈕（若你需要取消選取用）
            if (mood_1.name == moodName) activeMoodButton = mood_1;
            else if (mood_2.name == moodName) activeMoodButton = mood_2;
            else if (mood_3.name == moodName) activeMoodButton = mood_3;
            else if (mood_4.name == moodName) activeMoodButton = mood_4;
        }
        else
        {
            // 沒選擇 mood → 四個都能按
            mood_1.interactable = true;
            mood_2.interactable = true;
            mood_3.interactable = true;
            mood_4.interactable = true;
            activeMoodButton = null;
        }

        if (weatherMap.ContainsKey(dateKey))
        {
            string weatherName = weatherMap[dateKey];

            // 依 mood name 控制四個按鈕狀態
            weather_1.interactable = (weather_1.name == weatherName);
            weather_2.interactable = (weather_2.name == weatherName);
            weather_3.interactable = (weather_3.name == weatherName);
            weather_4.interactable = (weather_4.name == weatherName);

            // 記錄當前活躍按鈕（若你需要取消選取用）
            if (weather_1.name == weatherName) activeWeatherButton = weather_1;
            else if (weather_2.name == weatherName) activeWeatherButton = weather_2;
            else if (weather_3.name == weatherName) activeWeatherButton = weather_3;
            else if (weather_4.name == weatherName) activeWeatherButton = weather_4;
        }
        else
        {
            // 沒選擇 mood → 四個都能按
            weather_1.interactable = true;
            weather_2.interactable = true;
            weather_3.interactable = true;
            weather_4.interactable = true;
            activeWeatherButton = null;
        }
    }

        private void SaveToFile()
        {
            DailyData data = new DailyData();
        foreach (var pair in dateTextMap)
        {
            DateEntry entry = new DateEntry();
            entry.date = pair.Key;
            entry.text = pair.Value;

            if (moodMap.ContainsKey(pair.Key))
            {
                entry.mood = moodMap[pair.Key];
            }

            if (weatherMap.ContainsKey(pair.Key))
            {
                entry.weather = weatherMap[pair.Key];
            }

            // 如果有圖片路徑，儲存它
            if (imagePaths.ContainsKey(pair.Key))
            {
                entry.imagePath = imagePaths[pair.Key]; // 儲存圖片路徑
            }

            data.entries.Add(entry);
        }

        string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(filePath, json);
        }


        private void LoadFromFile()
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                DailyData data = JsonUtility.FromJson<DailyData>(json);
                
                dateTextMap.Clear();
                imagePaths.Clear();
                moodMap.Clear();
                weatherMap.Clear();

            foreach (var entry in data.entries)
                {
                    dateTextMap[entry.date] = entry.text;

                if (!string.IsNullOrEmpty(entry.imagePath) && File.Exists(entry.imagePath))
                {
                    imagePaths[entry.date] = entry.imagePath;  // ✅ 關鍵補充：還原圖片路徑

                    // 只有今天的日期才需要立即顯示圖片
                    if (entry.date == currentDate.ToString("yyyy-MM-dd"))
                    {
                        LoadImage(entry.imagePath);
                    }

                    if (!string.IsNullOrEmpty(entry.mood))
                    {
                        moodMap[entry.date] = entry.mood;
                    }

                    if (!string.IsNullOrEmpty(entry.weather))
                    {
                        weatherMap[entry.date] = entry.weather;
                    }
                }
                
            }
            }
        }

    
    public void LoadImage(string imagePath)
    {
        if (displayImage.texture != null)
        {
            Destroy(displayImage.texture);
        }

        if (File.Exists(imagePath))
        {
            byte[] bytes = File.ReadAllBytes(imagePath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(bytes, true);  // 載入圖片
                                             // displayImage.texture = texture;  // 顯示圖片在 RawImage 上

            //displayImage.material = null;

            displayImage.texture = texture;
            StartCoroutine(UpdateAspectRatioNextFrame(texture));
            Debug.Log("已被呼叫!!");
        }
        
    }

    private IEnumerator UpdateAspectRatioNextFrame(Texture2D texture)
    {
        yield return null; // 等一幀再設定比例，避免 UI 未更新造成拉長


        if (fitter != null && texture != null)
        {
            float aspect = (float)texture.width / texture.height;
            fitter.aspectRatio = aspect;

            Debug.Log("圖片尺寸：" + texture.width + "x" + texture.height);
        }
    }






    public void DeleteAllData()
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        foreach (var path in imagePaths.Values)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
       
        // 清除記憶體資料
        dateTextMap.Clear();
        imagePaths.Clear();

        // 重設 UI 狀態
        inputField.text = "";
        dateLabel.text = DateTime.Today.ToString("yyyy-MM-dd");
        currentDate = DateTime.Today;

        if (displayImage.texture != null)
        {
            Destroy(displayImage.texture);
            displayImage.texture = null;
        }

        Debug.Log("日記資料與圖片皆已刪除！");

    }
}



