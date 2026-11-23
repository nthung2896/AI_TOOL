# AI TOOL — trợ lý cho thuê phòng qua Telegram

## Chạy ngay trên máy Windows

**Bấm `Start-AI-Tool.cmd` ở thư mục gốc.** Bản biên dịch sẵn nằm trong `dist/AI_TOOL`. Script mở ứng dụng và trình duyệt tại `http://127.0.0.1:5112/rental-desktop/`, tự đăng nhập bằng khóa quản lý cục bộ. Giữ cửa sổ chạy mở khi muốn bot hoạt động; nhấn Ctrl+C để tắt.

- Không cần npm, Node.js, SQL Server hoặc khóa AI để mở bản cục bộ. Cần .NET 8 Runtime (máy phát triển hiện tại đã có). Nếu chưa có bản biên dịch, script tự build và cần .NET 8 SDK.
- Chế độ **Mẫu miễn phí** tạo bài và trả lời theo dữ liệu phòng, không phải AI suy luận. Có thể soạn bài bằng ChatGPT Plus, bấm **Sửa / dán bài ChatGPT** rồi duyệt đăng.
- Vào **Cấu hình** để nhập bot token Telegram và numeric user ID của bạn. Bấm **Kiểm tra token đã lưu** để kiểm tra kết nối. Không có bot token thì vẫn sử dụng đầy đủ bảng quản lý và lệnh ngay trên web.
- Bấm **Mở Facebook để đăng nhập**, đăng nhập tài khoản cá nhân và mở khóa Messenger trực tiếp trong Chromium nếu được yêu cầu.
- Đăng nhóm cần kiểm tra các nút soạn/đăng theo giao diện tài khoản. Trả lời Messenger cần cấu hình CSS selector tin đến/tin đi và thuộc tính ID ổn định trong phần nâng cao; có nút **Kiểm tra nhận diện** không gửi tin. Chưa có phiên Facebook thật để xác minh tự động nhận diện cho tài khoản của bạn; không coi việc mở được ứng dụng là đã kết nối được Messenger.
- Dữ liệu lưu bền tại `App_Data/rental-desktop/data.json`, cấu hình riêng tại `settings.json`, profile Chromium cùng thư mục. Có bản `.backup` của lần ghi trước; nút **Tải bản sao dữ liệu** xuất dữ liệu nghiệp vụ, không chứa bot token/khóa AI. Không chỉnh/xóa các file trong lúc ứng dụng đang chạy.
- Luôn khởi động ở trạng thái tạm dừng. Đầu tiên thêm phòng và chọn nhóm; sau khi duyệt bài mới bấm **Bật trợ lý**.
- Vào **Thử trả lời** để kiểm tra câu trả lời miễn phí ngay trên máy, không nhắn cho khách thật.
- Chế độ mẫu nhận lịch theo cú pháp `Hẹn xem | Tên phòng | Họ tên | 0912345678 | dd/MM/yyyy HH:mm`. Trợ lý gửi lại thông tin; khách nhắn `xác nhận` mới lưu lịch. Có thể ghi lịch thủ công trên giao diện.
- Chọn **AI API** khi có khóa riêng, hoặc **AI local** với Ollama đã cài model (`http://127.0.0.1:11434/v1/`). Ứng dụng không tự tải model. ChatGPT Plus không cung cấp khóa API cho worker.

Bản cục bộ chỉ nghe loopback, không công khai Internet. Một tiến trình giữ khóa thư mục dữ liệu để tránh hai worker gửi trùng. Tác vụ gửi bị gián đoạn chuyển `NeedsReview`, không tự gửi lại.

Nếu cần đổi cổng hoặc biên dịch lại:

```powershell
.\Start-AI-Tool.ps1 -Port 5115 -Rebuild
```

Dữ liệu bản cục bộ độc lập với SQL Server; chưa có đồng bộ tự động giữa hai chế độ.

Đã kiểm tra bản đóng gói bằng trình duyệt Chromium: thêm phòng, tạo/sửa bài, dán nội dung ChatGPT Plus, thử trả lời, duyệt hàng đợi, ghi lịch và giao diện màn hình nhỏ. Bộ kiểm tra đạt 47 kiểm tra; các kiểm tra này không đăng bài hoặc nhắn tin tới Facebook thật.

## Chế độ tích hợp API C# + Next.js + SQL Server

Module C# trong `API/Hinet.Api/RentalAssistant`, giao diện Next.js tại `/rental-assistant`.
SQL Server: database `AI_TOOL`, schema riêng `rental`. Không chạy migration PostgreSQL cũ cho module này.

## Trạng thái và giới hạn

- Có luồng AI soạn bài, duyệt qua Telegram/web, xếp hàng đăng nhóm, tìm nhóm qua giao diện, đồng bộ hội thoại đang hiển thị, trả lời và ghi lịch hẹn.
- Đăng nhóm và Messenger cá nhân sử dụng Chromium/Playwright, không dùng Facebook Groups API. Facebook có thể thay đổi giao diện hoặc yêu cầu xác minh. Cần kiểm tra selector trên tài khoản thực tế trước khi bật. Chưa được xác minh end-to-end với tài khoản Facebook thật.
- Không vượt CAPTCHA/checkpoint, không tự tham gia nhóm và không bảo đảm tìm được mọi nhóm hoặc mọi hội thoại. Tìm nhóm lấy tối đa 20 link đang hiển thị; đồng bộ inbox lấy tối đa 30 link đang hiển thị, không cuộn toàn bộ lịch sử.
- Chưa có đăng ảnh/video, lịch đăng theo giờ cụ thể, hoặc trả lời tin nhắn mã hóa mà trình duyệt chưa mở khóa. Hàng đợi hiện chạy tuần tự với khoảng cách cấu hình.
- Khi worker khởi động và nhận diện hội thoại cũ lần đầu, tin gần nhất được dùng làm mốc, không trả lời lại lịch sử. Hội thoại mới phát hiện sau đó có thể tự trả lời nếu `AutoReplyNewConversations=true`. Tin nhắn yêu cầu kết nối/ẩn chưa hiển thị trong inbox phải thêm link thủ công.
- `Submitted` nghĩa là đã bấm gửi, chưa xác minh bài được xuất bản hoặc quản trị viên duyệt. `NeedsReview` phải kiểm tra thủ công, không tự gửi lại.

## Cấu hình

Tạo bot qua BotFather. Lấy numeric user ID Telegram của chính bạn. Bot chỉ xử lý chat riêng có cả sender ID và chat ID bằng `TelegramOwnerId`.

Dùng biến môi trường trong PowerShell chạy API hoặc .NET user-secrets; không đưa token vào mã nguồn:

```powershell
$env:RentalAssistant__Enabled = 'true'
$env:RentalAssistant__ManagementKey = '<chuỗi ngẫu nhiên ít nhất 32 ký tự>'
$env:RentalAssistant__TelegramToken = '<bot token>'
$env:RentalAssistant__TelegramOwnerId = '<numeric user ID>'
$env:RentalAssistant__AiApiKey = '<AI API key>'
$env:RentalAssistant__AiModel = '<model hỗ trợ chat/completions>'
$env:RentalAssistant__BrowserEnabled = 'true'
```

`AiBaseUrl` cần HTTPS và kết thúc bằng `/`, mặc định `https://api.openai.com/v1/`. Có thể dùng nhà cung cấp tương thích Chat Completions. Không có AI key/model thì không tạo bài hoặc trả lời bằng AI.

Khóa quản lý được nhập trên web, chỉ giữ trong bộ nhớ trang. Đây là công cụ một chủ, không dùng chung khóa với nhiều người. Khi truy cập ngoài máy, phục vụ cả API và web bằng HTTPS. Không công khai profile Chromium, database hoặc cổng điều khiển trình duyệt.

## Khởi tạo và chạy

```powershell
cd API/Hinet.Api
dotnet restore
dotnet run -- --init-db
dotnet build
pwsh bin/Debug/net8.0/playwright.ps1 install chromium
dotnet run
```

Lệnh `--init-db` chỉ hỗ trợ tên `AI_TOOL`: tạo database nếu thiếu, áp dụng 12 migration SQL Server để tạo bảng quản trị, tài khoản `admin` / `12345678`, quyền và menu, sau đó khởi tạo schema `rental`. Chạy trong thư mục `API/Hinet.Api`, dừng API đang chạy trước khi thực hiện. Tài khoản SQL cần quyền tạo database/schema/bảng. Có thể chạy lại: các migration đã áp dụng sẽ được bỏ qua. `--init-rental-db` vẫn chỉ khởi tạo schema rental.

Để xuất script SQL Server mà không kết nối database: `dotnet run -- --script-db`. Script `AI_TOOL.SqlServer.sql` được tạo trong thư mục hiện tại, tạo database `AI_TOOL` nếu thiếu, áp dụng migration quản trị và khởi tạo schema rental. Có thể mở script trong SSMS kết nối localhost và bấm Execute. Các migration trong dự án hiện dành cho SQL Server; không dùng chúng để cập nhật database PostgreSQL cũ.

Trong terminal khác:

```powershell
cd Client
npm ci
$env:NEXT_PUBLIC_API_URL = 'http://localhost:5111'
npm run dev
```

Mở `http://localhost:3000/rental-assistant`. Nếu chạy web ở origin khác, thêm origin đó vào `AllowedOrigins` của API. Worker dùng khóa SQL theo phiên để chỉ một tiến trình xử lý cùng database; profile là thư mục riêng `App_Data/facebook-profile`, được gitignore. Nếu mất kết nối giữ khóa, cần khởi động lại worker.

Bot sử dụng long polling, không cần public webhook. Nếu bot đã gắn webhook, gỡ webhook trước khi chạy polling. Máy chạy API phải có desktop tương tác để đăng nhập Chromium; không chạy browser worker dưới Windows Service không có desktop.

## Kiểm tra Facebook trước khi vận hành

1. Gửi `/facebook` cho bot; đăng nhập nick cá nhân trực tiếp trong Chromium. Không gửi mật khẩu Facebook vào bot.
2. Nếu Messenger yêu cầu PIN/khôi phục lịch sử mã hóa, thực hiện trực tiếp trong Chromium.
3. Kiểm tra selector `ComposeButton`, `PostEditor`, `PublishButton` trong `RentalOptions`; sửa theo giao diện và ngôn ngữ thực tế nếu cần.
4. Cấu hình `IncomingMessages`, `OutgoingMessages` bằng CSS selector chuẩn để chỉ khớp node từng tin đến và từng tin đi của hội thoại đang mở; không dùng chung selector cho hai chiều, không dùng selector mở rộng như `:has-text` cho hai trường này. Node tin đến phải có thuộc tính ID ổn định được khai báo trong `MessageIdAttribute`. Không có ID thì worker dừng hội thoại.
5. Cấu hình `MessageEditor` để khớp duy nhất ô soạn tin nhắn, không khớp ô tìm kiếm. Kiểm tra với một hội thoại thử nghiệm bạn kiểm soát trước khi bật rộng.
6. Thêm một phòng, một nhóm được phép đăng và một hội thoại thử nghiệm trên web. Bật tự trả lời cho hội thoại. Gửi `/chay` rồi gửi một tin mới từ phía khách để kiểm tra.
7. Chỉ bật `RentalAssistant__AutoReplyNewConversations=true` sau khi kiểm tra đúng tin đến/tin đi và ID ổn định. Thiết lập này áp dụng hội thoại mới được phát hiện, không tự bật lại các hội thoại đã tắt.

Nếu UI không cho truy xuất ID ổn định theo DOM, adapter này cần được điều chỉnh trước khi tự trả lời; không dùng nội dung tin nhắn làm ID vì khách có thể gửi hai tin giống nhau.

## Ảnh và thông tin bài cho thuê

Trang `/dashboard#rooms` có nền trắng, biểu mẫu gọn và bản xem trước bài đăng. Nhập nội thất, diện tích, vị trí lân cận, điện nước, tiền cọc, giờ giấc, liên hệ và hashtag; các trường bổ sung có thể để trống.

Mỗi phòng nhận tối đa 10 ảnh JPG/PNG/WebP, tối đa 8 MB mỗi ảnh. Có thể bỏ ảnh hoặc chọn ảnh bìa; ảnh bìa đứng đầu danh sách. Lưu phòng rồi chọn **Soạn bài**. Bản nháp lưu riêng danh sách ảnh tại thời điểm tạo, vì vậy sửa ảnh của phòng sau đó không thay đổi các bài đã duyệt. Muốn dùng ảnh mới, tạo bản nháp mới.

Ảnh được lưu trong `API/Hinet.Api/App_Data/rental-images` khi chạy API từ thư mục dự án, hoặc `<thư mục dữ liệu desktop>/images` ở chế độ desktop. Sao lưu cả thư mục ảnh cùng dữ liệu. API tự bổ sung hai cột JSON `rental.Rooms.Listing` và `rental.Posts.Images` khi khởi động, giữ nguyên dữ liệu cũ; tài khoản SQL cần quyền ALTER cho lần nâng cấp này. Các lệnh khởi tạo và xuất script SQL cũng bao gồm thay đổi đó.

Khi gửi Facebook, worker tải ảnh lên khung soạn bài trước khi bấm Đăng. Giao diện Facebook có thể thay đổi; nếu không nhận diện được ảnh đã tải, tác vụ dừng ở trạng thái cần kiểm tra.

## Lệnh Telegram

Kiểm tra backend không cần Facebook/Telegram thật:

```powershell
dotnet run --project API/RentalAssistant.Checks/RentalAssistant.Checks.csproj
```

Bộ kiểm tra dùng HTTP giả lập cho AI, kiểm tra xác thực khóa quản lý, URL Facebook và schema/index SQL Server mà không kết nối database.

- `/help`: hướng dẫn.
- `/phong`: phòng còn trống và ID.
- `/dang ID_PHONG yêu cầu`: tạo bản nháp, chưa đăng.
- Gõ yêu cầu bình thường: tạo bản nháp khi chỉ có một phòng còn trống.
- `/duyet ID_BAI`: xếp hàng một lượt cho mỗi nhóm đang bật.
- `/timnhom từ khóa`: tìm nhóm, lưu kết quả ở trạng thái chưa chọn; vào web chọn nhóm và kiểm tra quy định.
- `/facebook`: mở Chromium để đăng nhập.
- `/chay`, `/dung`: bật/tạm dừng đăng và trả lời. Việc gửi đang diễn ra có thể hoàn tất trước khi lệnh dừng được xử lý.
- `/trangthai`: trạng thái worker/browser/hàng đợi.
- `/lich`: 20 lịch xem sắp tới theo giờ Việt Nam.

## Ghi lịch và chống gửi trùng

AI đề xuất lịch dựa trên hội thoại và phòng còn trống. Hệ thống kiểm tra phòng, định dạng điện thoại Việt Nam, thời gian trong tương lai (tối đa 90 ngày), rồi gửi lại thông tin để khách xác nhận. Tin tiếp theo phải là `xác nhận`, `đồng ý`, `ok`, `xac nhan` hoặc `dong y` mới lưu lịch. Nếu khách sửa thông tin, hệ thống hỏi xác nhận lại.

Lịch được ghi trước khi gửi xác nhận Messenger, rồi thông báo Telegram. Nếu gửi Messenger bị lỗi, lịch đã lưu vẫn hiển thị trên web, hội thoại chuyển `NeedsReview`; cần kiểm tra thủ công. Chưa có kiểm tra trùng giờ giữa khách hoặc hủy/đổi lịch tự động.

Hệ thống lưu ID tin cuối trước khi gửi, có unique index cho cặp bài/nhóm và lịch theo tin xác nhận. Nếu mất kết nối sau thao tác gửi hoặc tiến trình bị dừng, tác vụ chuyển `NeedsReview`, không tự gửi lại. Thông báo Telegram chưa có outbox giao nhận riêng; nếu thông báo lỗi, xem kết quả trên web.

Dữ liệu hội thoại gần nhất được gửi tới nhà cung cấp AI để trả lời. Chỉ thu thập thông tin cần cho tư vấn/đặt lịch và giới hạn quyền truy cập dữ liệu khách.

