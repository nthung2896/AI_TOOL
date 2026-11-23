# AI TOOL — trợ lý cho thuê phòng qua Telegram

## Máy chủ cho thuê nhiều khách hàng

Bản API SQL Server + Next.js dùng tài khoản đăng nhập thay cho `X-Rental-Key`. Chế độ desktop bên dưới vẫn dành cho một người trên một máy.

1. Khởi động lại API với code mới, mở `/rental-assistant` hoặc `/dashboard`, đăng nhập bằng tài khoản quản trị hiện có. Vào **Tài khoản & khách thuê** để tạo khách, đặt hạn sử dụng, khóa/mở khóa và gia hạn.
2. Khách đăng nhập tại `/rental-assistant`, vào **Cấu hình của tôi**, bật trình duyệt riêng, lưu bot Telegram và numeric user ID của mình. Mỗi khách dùng một bot riêng.
3. Chọn **Kết nối Facebook**. Bấm ô trên ảnh trình duyệt, nhập email/mật khẩu/mã xác minh bằng thanh nhập bên dưới, rồi chọn **Nhập vào Facebook**. Facebook xử lý đăng nhập và xác minh. Chọn **Xong — tiếp tục worker** khi hoàn tất; phiên điều khiển hết sau 10 phút. Worker chờ trong thời gian này.
4. Thêm phòng, ảnh, nhóm và duyệt bài, sau đó bật trợ lý. Tài khoản mới bắt đầu tạm dừng và không nhận bot hoặc phiên Facebook của chủ.

Chủ hệ thống mặc định là tài khoản Identity `admin`; có thể đặt `RentalSaas:OwnerUserName` trước lần khởi động đầu. Dữ liệu cũ được giữ trong schema `rental` và gắn với chủ này. Mỗi khách có schema SQL `customer_<GUID>` riêng cho phòng, nhóm, bài đăng, hàng đợi, hội thoại và lịch hẹn. API xác định không gian từ tài khoản đã xác thực, không lấy tenant ID do trình duyệt gửi. Khách bị chặn khỏi các API quản trị cũ. Mật khẩu chung từng bỏ qua kiểm tra tài khoản đã được gỡ bỏ.

Phiên Facebook, ảnh và bộ nhận diện Messenger nằm trong `App_Data/customers/<GUID>/` dưới thư mục nội dung API. Token Telegram được bảo vệ bằng ASP.NET Data Protection. Sao lưu **cơ sở dữ liệu, thư mục khách và `App_Data/protection-keys` tại thư mục chạy API**; mất khóa bảo vệ sẽ không đọc được token đã lưu. Không đưa các thư mục này vào Git hoặc web tĩnh. Khóa/hết hạn chặn request ngay; bộ điều phối dừng worker và yêu cầu hủy tác vụ đang chạy. Đổi/reset mật khẩu vô hiệu hóa JWT cũ của API trợ lý.

Khi triển khai Internet: dùng HTTPS, đặt đúng `NEXT_PUBLIC_API_URL`, `AllowedOrigins`, khóa ký JWT riêng và đổi mật khẩu quản trị mặc định. Frontend và API nên cùng miền hoặc hai tên miền con cùng site để ảnh riêng dùng cookie HttpOnly. Cài Chromium Playwright trên máy chủ; tài khoản SQL của dịch vụ cần quyền tạo schema/bảng để cấp không gian khách. Bản này cấp/gia hạn thủ công, gồm quyền chủ hệ thống và khách thuê, mỗi khách một phiên Facebook. Chưa tích hợp thanh toán, gói hạn mức hoặc nhiều nhân viên trong cùng khách hàng. Chi phí AI dùng cấu hình máy chủ.

Kiểm tra: `dotnet run --project API/RentalAssistant.Checks`; thêm `-- --saas-integration` để thử cách ly bằng hai schema SQL tạm. Bộ kiểm tra `--saas-http` dùng API thử cổng 5119, bắt buộc khởi động API với `--environment Development --Kestrel:Endpoints:Http:Url=http://localhost:5119 --RentalSaas:RunWorkers=false` để không đăng bài, đọc Messenger hoặc gửi Telegram thật. Không dùng `RunWorkers=false` cho dịch vụ đang khai thác.

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

### Bộ đếm và khoảng cách giữa các lượt đăng

Ở **Tổng quan** và **Soạn & đăng bài**, khối **Lịch gửi & hàng đợi** hiển thị bộ đếm từng giây, mốc sớm nhất được bắt đầu lượt tiếp theo, bài/phòng và nhóm sẽ nhận bài, nhóm đang xử lý nếu có, cùng tối đa 5 lượt kế tiếp theo đúng thứ tự. Dữ liệu hàng đợi cập nhật mỗi 5 giây; bộ đếm căn theo giờ API để không bị lệch do đồng hồ trên máy người dùng.

Trong **Khoảng cách giữa các lượt**, nhập số phút/giây hoặc chọn mốc gợi ý, rồi **Lưu thời gian**. Giá trị từ 1 giây đến 24 giờ được lưu tại `rental.State.PostIntervalSeconds`, có hiệu lực cho các lượt đang chờ và giữ nguyên khi khởi động lại. Nếu chưa lưu, ứng dụng dùng `RentalAssistant:PostIntervalSeconds` trong cấu hình làm mặc định. Giảm khoảng cách có thể cho phép lượt kế tiếp bắt đầu ngay khi worker rảnh.

Khoảng cách được tính từ lúc bắt đầu lượt trước, không phải lúc hoàn tất. Đây là thời điểm sớm nhất, không phải lịch hẹn chính xác: worker gửi tuần tự và có thể đang tải ảnh, đọc Messenger, đồng bộ tương tác hoặc thiết lập/tìm nhóm. Khi bộ đếm về 0 nhưng còn tác vụ, giao diện báo đang chờ worker; khi tạm dừng hoặc worker chưa chạy thì không hứa sẽ gửi ở mốc 0. Các nhóm tắt cho phép đăng hoặc phòng hết không được chọn làm lượt hợp lệ tiếp theo. Các lượt cùng thời gian tạo dùng ID để cố định thứ tự giữa giao diện và worker.

### Tìm nhóm theo danh mục và chọn nhóm cho từng bài

Trong **Nhóm Facebook**, chọn **Phòng trọ**, **Bất động sản**, **Giáo dục** hoặc **Khác**; thêm khu vực/từ khóa rồi nhấn **Tìm nhóm trên Facebook**. Worker dùng Chromium đang đăng nhập để đọc tối đa 20 nhóm từ kết quả hiển thị. Chỉ khi bạn tích kết quả và bấm **Lưu nhóm đã chọn**, nhóm mới được thêm vào danh sách cho phép đăng. Có thể lọc danh mục, sửa tên/link/danh mục, tắt cho phép đăng hoặc thêm nhóm bằng link thủ công. Không tự tham gia nhóm.

Có hai cách tìm: **Chọn theo danh mục** (có các chủ đề gợi ý để tích chọn) hoặc **Tự nhập từ khóa** (không cần danh mục). Có thể kết hợp chủ đề gợi ý với từ tự nhập. Dấu phẩy, chấm phẩy hoặc xuống dòng tách các cụm: ví dụ `sinh viên, nhà trọ, ccmn` tạo ba lượt tìm riêng rồi gộp link trùng, tối đa 5 cụm và 50 kết quả. Mỗi lượt đọc tối đa 20 nhóm đang hiển thị trong vùng kết quả tìm kiếm; không lấy link từ thanh bên. Tiền tố nhãn ảnh như `Profile photo of…` được loại khỏi tên nhóm.

Trong **Soạn & đăng bài**, mục **Nhóm nhận bài** cho phép chọn nhóm trước khi tạo bản nháp. Các lựa chọn được lưu cùng bản nháp; có thể thay đổi tiếp ngay trên thẻ bản nháp trước khi bấm **Duyệt đăng**. API chỉ xếp hàng vào đúng các ID được chọn và từ chối nhóm không tồn tại/đã tắt. Bỏ chọn hết không có nghĩa là đăng vào tất cả nhóm. Tắt một nhóm sau khi duyệt khiến các lượt chưa gửi vào nhóm đó được bỏ qua.

Với Telegram, `/duyet ID_BAI` dùng danh sách riêng đã lưu trên bản nháp, nếu có; các bản nháp tạo qua Telegram hoặc bản nháp cũ chưa có lựa chọn riêng vẫn dùng các nhóm đang bật. Khi cập nhật, khởi động lại API để thêm cột danh mục và danh sách nhóm nhận bài; dữ liệu cũ được giữ.

### Theo dõi bài và tương tác trên dashboard

Trang Tổng quan có biểu đồ hoạt động 7/14/30 ngày, phân bổ trạng thái và số cảm xúc/bình luận/chia sẻ. Dữ liệu tổng hợp từ tối đa 100 bài, 200 lượt gửi gần nhất; dấu `—` nghĩa là chưa đọc được số liệu, không phải 0. Biểu đồ hoạt động dùng ngày tạo lượt gửi theo giờ Việt Nam.

Sau khi gửi, worker tự lấy link từ phản hồi tạo bài của Facebook và chờ tối đa khoảng 35 giây cho bài xuất hiện trên bảng tin. Hỗ trợ link `/posts/ID`, `/permalink/ID`, `pfbid` và timestamp `multi_permalinks`. Link được lưu cùng lượt gửi và tự xếp hàng đọc tương tác. Nếu chưa có link (ví dụ bài chờ nhóm duyệt), trạng thái giữ **Đã gửi · chờ link**, không tự gửi lại bài.

Các lượt đã gửi còn thiếu link được tự tìm lại khoảng 15 phút/lần qua tìm kiếm nhóm, đối chiếu nội dung và thời gian gửi để tránh nhầm bài trùng nội dung. Có thể nhấn **Tìm lại link** hoặc **Đồng bộ tương tác** để xếp hàng kiểm tra ngay. Nếu không đủ dữ liệu để xác định duy nhất bài, ứng dụng tiếp tục chờ thay vì gắn bừa link. Nhập tay chỉ nằm trong **Tùy chọn → Nhập link dự phòng**, không còn là bước bắt buộc.

**Lưu & đồng bộ** hoặc **Đồng bộ tương tác** xếp hàng đọc số liệu; worker sử dụng cùng cửa sổ Chromium để xử lý tuần tự, không thao tác trình duyệt song song với đăng bài. Worker và BrowserEnabled cần bật, phiên Facebook cần có quyền xem bài. Telegram là tùy chọn cho các tác vụ điều khiển qua web. Các link đã kiểm tra được đọc lại khoảng 3 phút/lần. Nếu phiên đăng nhập hết hạn hoặc Facebook thay đổi giao diện/ẩn số đếm, dashboard báo chưa đọc được thay vì tạo số liệu giả. Cảm xúc là tổng phản ứng đang hiển thị, có thể được Facebook làm tròn.

Khởi động lại API sau khi cập nhật để thêm các cột theo dõi vào `rental.Deliveries`. Không có thao tác tự động đăng lại các lượt gửi cũ.

Kiểm tra backend không cần Facebook/Telegram thật:

```powershell
dotnet run --project API/RentalAssistant.Checks/RentalAssistant.Checks.csproj
```

Bộ kiểm tra dùng HTTP giả lập cho AI, kiểm tra xác thực khóa quản lý, URL Facebook và schema/index SQL Server mà không kết nối database.

- `/help`: hướng dẫn.
- `/phong`: phòng còn trống và ID.
- `/dang ID_PHONG yêu cầu`: tạo bản nháp, chưa đăng.
- `/dang Ngõ 1 Bùi Xương Trạch Viết tin cho thuê phòng`: chọn theo đúng tên phòng (có thể gõ không dấu), tạo bản nháp. Nếu tên trùng hoặc nhắc nhiều phòng, bot yêu cầu chọn ID từ `/phong`.
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

Hệ thống lưu ID tin cuối trước khi gửi, có unique index cho cặp bài/nhóm và lịch theo tin xác nhận. Nếu mất kết nối sau thao tác gửi hoặc tiến trình bị dừng, tác vụ chuyển `NeedsReview`, không tự gửi lại. Thông báo xác minh đăng xong và tương tác có hàng đợi lưu trong database; các phản hồi lệnh Telegram thông thường vẫn dùng gửi trực tiếp.

### Điều khiển từ điện thoại và nhận thông báo

Máy chạy API và Chromium cần được bật, có mạng, Facebook đã đăng nhập; không cần điện thoại ở cùng mạng. Nhắn bot `/dang Ngõ 1 Bùi Xương Trạch Viết bài cho thuê phòng, nhấn mạnh vị trí và nội thất`. Đọc bản nháp, gửi `/duyet ID_BAI` mà bot trả về, rồi `/chay` nếu trợ lý đang tạm dừng.

Bot báo **ĐÃ GỬI BÀI** sau thao tác gửi. Khi xác minh mở được link bài và không thấy dấu hiệu chờ duyệt, bot báo **ĐÃ ĐĂNG XONG** kèm tên phòng, nhóm và URL. Bài chờ duyệt/chưa truy cập được không bị báo đăng xong sớm.

Số cảm xúc, bình luận, chia sẻ được kiểm tra định kỳ, mặc định khoảng 3 phút (`RentalAssistant:MetricsIntervalSeconds`, tối thiểu 60 giây). Lần đọc đầu làm mốc; những lần sau có tổng tăng, bot gửi một thông báo gộp số tăng và link bài. Đây là thay đổi tổng số đếm Facebook hiển thị, không xác định danh tính người thả tim và không phải thông báo tức thời. Thông báo thành công/tương tác được lưu hàng đợi trong database và thử lại khi Telegram lỗi; một sự cố đúng lúc Telegram đã nhận nhưng API chưa ghi nhận có thể gây lặp thông báo.

Khi tổng bình luận tăng, thông báo có tiêu đề **CÓ BÌNH LUẬN MỚI**, số tăng và link bài Facebook. Mọi thông báo tương tác đều kèm link bài tương ứng.

Tin Messenger được theo dõi độc lập với tự trả lời: hội thoại có `NotifyIncoming=true` (mặc định bật) vẫn báo Telegram khi `AutoReply=false`, kể cả trợ lý đang tạm dừng đăng/trả lời. Chỉ các hội thoại trong danh sách đã thêm hoặc inbox được đồng bộ mới được theo dõi; mỗi chu kỳ ưu tiên hội thoại có dấu chưa đọc và kiểm tra tối đa 5 hội thoại. Lần đầu lấy mốc lịch sử, sau đó dùng ID ổn định để gửi trích nội dung các tin khách mới kèm link `/messages/t/ID`. Tin do chủ tài khoản gửi không được coi là tin khách; tin cũ không bị gửi lại hàng loạt khi khởi động. Thông báo được lưu trước khi gửi và giữ lại để thử lại khi Telegram lỗi.

Để đọc nội dung/từng tin, cần cấu hình đúng `IncomingMessages`, `OutgoingMessages` và `MessageIdAttribute` theo DOM Messenger thực tế. Khi thiếu hai selector, bot cảnh báo một lần và chỉ báo hội thoại chưa đọc nếu Facebook có nhãn chưa đọc rõ ràng; không đoán chiều gửi hoặc bịa nội dung. Theo dõi này không bao phủ tin yêu cầu, tin ẩn hoặc các hội thoại chưa được inbox đồng bộ.

### Thiết lập Messenger bằng hai tin mẫu

Trên `/dashboard#threads`, chọn **Thiết lập nhận tin Messenger**, chọn hội thoại có tin của cả hai phía (hoặc dán link `/messages/t/ID` hay `/messages/e2ee/t/ID`), rồi nhấn **Mở thiết lập trong Chromium**. Trong cửa sổ Chromium, bấm trực tiếp một bong bóng tin khách gửi, tiếp theo một bong bóng tin bạn gửi. Không cần gửi tin thử cho khách.

Worker xử lý phiên này độc lập với trạng thái tạm dừng đăng bài và giữ các tác vụ trình duyệt khác chờ trong lúc thiết lập. Chỉ khi hai bộ nhận diện không trùng và đúng hai ID mẫu còn tìm thấy sau khi tải lại hội thoại, cấu hình mới được ghi vào `App_Data/messenger-selectors.json` và áp dụng ngay. Lần khởi động sau sẽ nạp lại file này. Không sửa key Telegram/OpenAI hay `appsettings.json`.

Nếu cần PIN Messenger, hãy hủy thiết lập, mở khóa lịch sử trong Chromium rồi bắt đầu lại. Phiên hết hạn sau 5 phút. Nếu DOM không có ID tin ổn định hoặc không phân biệt được hai chiều, ứng dụng báo lỗi và giữ cấu hình cũ; không tạo ID giả từ nội dung. Sau khi thành công, bot gửi **ĐÃ THIẾT LẬP MESSENGER**, lấy mốc lịch sử rồi báo những tin khách mới.

Dữ liệu hội thoại gần nhất được gửi tới nhà cung cấp AI để trả lời. Chỉ thu thập thông tin cần cho tư vấn/đặt lịch và giới hạn quyền truy cập dữ liệu khách.

