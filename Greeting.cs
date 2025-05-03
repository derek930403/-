using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Greeting : MonoBehaviour
{
    [SerializeField] public TMP_Text GreetingArea;

    private string[] greetings = {
        "嗨，今天也是美好的一天!",
        "準備好了嗎？",
        "又見面了！",
        "祝你過得開心！"
    };

    // Start is called before the first frame update
    void Start()
    {
        int index = Random.Range(0, greetings.Length);
        string selectedGreeting = greetings[index];

        // 顯示問候語（這裡用 Debug.Log，你可以改成顯示在 UI 上）
        Debug.Log(selectedGreeting);
        GreetingArea.text = selectedGreeting;
    }

    
}
