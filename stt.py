import json
import base64
import requests
import pyaudio
import threading
import os

API_ENDPOINT = "https://speech.googleapis.com/v1p1beta1/speech:recognize"
API_KEY = "AIzaSyDvzWSFa-NCyvJBbIMWkHNlK2kz-BRx_FQ"

# 取得目前檔案所在的資料夾路徑
CURRENT_DIR = os.path.dirname(os.path.abspath(__file__))
JSON_PATH = os.path.join(CURRENT_DIR, "stt.json")

def record():
    chunk = 1024
    format = pyaudio.paInt16
    channels = 1
    rate = 16000
    audio = pyaudio.PyAudio()
    print("開始錄音")
    print("按下 Enter 鍵停止錄音...")
    
    # 建立一個事件來控制錄音
    stop_event = threading.Event()
    
    # 建立一個函數來監聽 Enter 鍵
    def wait_for_enter():
        input()  # 等待 Enter 鍵
        stop_event.set()
    
    # 在背景執行 Enter 鍵監聽
    threading.Thread(target=wait_for_enter, daemon=True).start()
    
    stream = audio.open(format=format, channels=channels,
                       rate=rate, frames_per_buffer=chunk, input=True)

    frames = []
    
    while not stop_event.is_set():
        data = stream.read(chunk, exception_on_overflow=False)
        frames.append(data)
    
    print("停止錄音")
    stream.stop_stream()
    stream.close()
    audio.terminate()
    audio_data = b''.join(frames)
    audio_data64 = base64.b64encode(audio_data).decode("utf-8")
    return audio_data64

def updateSttJson(audio_data64):
    print(f"正在讀取檔案：{JSON_PATH}")  # 除錯用
    with open(JSON_PATH, "r", encoding="utf-8") as json_file:
        request_data = json.load(json_file)
    request_data["audio"]["content"] = audio_data64
    with open(JSON_PATH, "w", encoding="utf-8") as json_file:
        json.dump(request_data, json_file, ensure_ascii=False, indent=4)

def saveResultToFile(result_text):
    file_path = os.path.join(CURRENT_DIR, "result.txt")
    with open(file_path, "a", encoding="utf-8") as file:
        file.write(result_text + "\n")
    print(f"結果已儲存到: {file_path}")

def ReturnResult():
    with open(JSON_PATH, "r", encoding="utf-8") as json_file:
        request_data = json.load(json_file)

    response = requests.post(
        f"{API_ENDPOINT}?key={API_KEY}",
        json=request_data
    )

    if response.status_code == 200:
        result = response.json()
        for result_item in result['results']:
            transcript = result_item['alternatives'][0]['transcript']
            print(transcript)
            saveResultToFile(transcript)
    else:
        print("API 請求失敗")

a = record()
updateSttJson(a)
ReturnResult()
   