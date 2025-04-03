# 使用輕量級的 Go 官方鏡像作為基礎映像
FROM golang:1.20-alpine

# 設定工作目錄
WORKDIR /app

# 複製 Go 模組檔案並下載相依性
COPY go.mod go.sum ./
RUN go mod download

# 複製專案程式碼
COPY . .

# 編譯 Go 應用程式
RUN go build -o main .

# 指定執行的 Port
EXPOSE 8080

# 啟動應用程式
CMD ["./main"]