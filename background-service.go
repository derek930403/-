package main

import (
	"context"
	"encoding/json"
	"fmt"
	"html"
	"log"
	"net/http"
	"os"
	"os/signal"
	"strconv"
	"syscall"
	"time"
)

type TaskRequest struct {
	ReminderInterval int `json:"reminderInterval"`
	CompletionTime   int `json:"completionTime"`
}

func main() {
	stop := make(chan struct{})
	server := &http.Server{Addr: ":8080"}

	http.HandleFunc("/", func(w http.ResponseWriter, r *http.Request) {
		http.Redirect(w, r, "/task", http.StatusFound)
	})

	http.HandleFunc("/task", func(w http.ResponseWriter, r *http.Request) {
		// CORS 設定
		w.Header().Set("Access-Control-Allow-Origin", "*")
		w.Header().Set("Access-Control-Allow-Methods", "GET, POST, OPTIONS")
		w.Header().Set("Access-Control-Allow-Headers", "Content-Type")

		if r.Method == http.MethodOptions {
			w.WriteHeader(http.StatusNoContent)
			return
		}

		switch r.Method {
		case http.MethodPost:
			r.Body = http.MaxBytesReader(w, r.Body, 1048576)
			if r.Header.Get("Content-Type") != "application/json" {
				http.Error(w, "Content-Type must be application/json", http.StatusUnsupportedMediaType)
				return
			}

			var req TaskRequest
			if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
				http.Error(w, "Invalid request.", http.StatusBadRequest)
				return
			}

			if req.ReminderInterval <= 0 {
				req.ReminderInterval = 5000
			}
			if req.CompletionTime <= 0 {
				req.CompletionTime = 30000
			}

			renderHTML(w, req.ReminderInterval, req.CompletionTime)

		case http.MethodGet:
			renderHTML(w, 5000, 30000)

		default:
			http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		}
	})

	go func() {
		fmt.Println("服務端運行中，請訪問 http://localhost:8080/")
		if err := server.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			log.Fatal(err)
		}
	}()

	signalChan := make(chan os.Signal, 1)
	signal.Notify(signalChan, os.Interrupt, syscall.SIGTERM)
	go func() {
		<-signalChan
		fmt.Println("\n接收到中斷訊號，服務端即將關閉...")
		ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
		defer cancel()
		if err := server.Shutdown(ctx); err != nil {
			log.Printf("Server forced to shutdown: %v", err)
		}
		close(stop)
	}()

	<-stop
	fmt.Println("服務端已關閉")
}

func renderHTML(w http.ResponseWriter, reminderInterval, completionTime int) {
	w.Header().Set("Content-Type", "text/html; charset=utf-8")
	fmt.Fprintf(w, `
<!DOCTYPE html>
<html>
<head>
    <title>任務</title>
    <script type="text/javascript">
        let reminderInterval = %s;
        let completionTime = %s;

        const intervalId = setInterval(() => {
            alert("請記得完成任務！");
        }, reminderInterval);

        setTimeout(() => {
            clearInterval(intervalId);
            document.getElementById("message").textContent = "任務完成！";
        }, completionTime);
    </script>
</head>
<body>
    <p id="message">請先完成當前任務！！！</p>
</body>
</html>
`, html.EscapeString(strconv.Itoa(reminderInterval)), html.EscapeString(strconv.Itoa(completionTime)))
}
