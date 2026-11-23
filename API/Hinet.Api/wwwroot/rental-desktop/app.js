"use strict";
(() => {
  const root = document.getElementById("app");
  const state = { key: new URLSearchParams(location.hash.slice(1)).get("key") || "", data: null, settings: null, tab: "overview", room: null, group: null, thread: null, busy: false, modal: null, chat: "", command: "", error: "" };
  if (location.hash) history.replaceState(null, "", location.pathname);
  const esc = v => String(v ?? "").replace(/[&<>"']/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);
  const labels = { Draft: "Bản nháp", Approved: "Đã duyệt", Queued: "Chờ đăng", Submitted: "Đã gửi · kiểm tra duyệt", NeedsReview: "Cần kiểm tra", Sending: "Đang gửi", Cancelled: "Đã hủy", Completed: "Đã xem phòng", Skipped: "Đã bỏ qua", New: "Chờ đồng bộ", NewIncoming: "Khách mới", Watching: "Đang theo dõi", Replying: "Đang trả lời", CustomerConfirmed: "Khách xác nhận", OwnerCreated: "Bạn đã đặt lịch" };
  const badge = s => `<span class="ra-badge ${s === "NeedsReview" ? "ra-danger" : ""}">${esc(labels[s] || s)}</span>`;
  const money = n => Number(n).toLocaleString("vi-VN") + "đ";
  const day = d => new Date(d).toLocaleString("vi-VN", { timeZone: "Asia/Ho_Chi_Minh" });
  const mode = m => ({ template: "Mẫu miễn phí", api: "AI API", local: "AI trên máy" })[m] || m;
  const empty = text => `<div class="ra-empty">${esc(text)}</div>`;
  const button = (action, text, id = "", secondary = false) => `<button type="button" data-action="${action}" data-id="${esc(id)}" class="${secondary ? "ra-secondary" : ""}">${esc(text)}</button>`;
  const input = (name, title, value = "", type = "text", attributes = "") => `<label>${esc(title)}<input name="${name}" type="${type}" value="${esc(value)}" ${attributes}></label>`;
  const check = (name, title, value) => `<label class="ra-check"><input name="${name}" type="checkbox" ${value ? "checked" : ""}>${esc(title)}</label>`;
  const textarea = (name, title, value, rows = 4) => `<label>${esc(title)}<textarea name="${name}" aria-label="${esc(title)}" rows="${rows}" maxlength="10000">${esc(value)}</textarea></label>`;
  const roomName = id => state.data.rooms.find(r => r.id === id)?.name || "Phòng đã thay đổi";
  function toast(text) { const el = document.getElementById("toast"); el.textContent = text; el.classList.add("visible"); clearTimeout(toast.timer); toast.timer = setTimeout(() => el.classList.remove("visible"), 5500); }
  async function api(path, method = "GET", body) {
    const response = await fetch("/api/rental-assistant/" + path, { method, headers: { "X-Rental-Key": state.key, "Content-Type": "application/json" }, body: body === undefined ? undefined : JSON.stringify(body), cache: "no-store" });
    if (!response.ok) {
      if (response.status === 401) throw new Error("Khóa quản lý chưa đúng. Mở lại bằng file Start-AI-Tool.cmd hoặc nhập khóa từ settings.json.");
      let error; try { error = await response.json(); } catch { error = {}; }
      throw new Error(error.error || `Không thực hiện được thao tác (${response.status}).`);
    }
    const text = await response.text(); return text ? JSON.parse(text) : null;
  }
  async function refresh() { state.data = await api("overview"); }
  async function work(action) {
    if (state.busy) return;
    state.busy = true; state.error = "";
    root.querySelectorAll("button").forEach(b => b.disabled = true);
    try { await action(); await refresh(); render(); }
    catch (e) { state.error = e.message; toast(e.message); const alert = document.getElementById("error"); if (alert) { alert.textContent = e.message; alert.hidden = false; } }
    finally { state.busy = false; root.querySelectorAll("button").forEach(b => b.disabled = false); }
  }
  function renderLogin() {
    root.innerHTML = `<main class="ra-login"><form class="ra-connect" data-form="login"><span class="ra-brand">AI TOOL / RENTAL DESK</span><h1>Trợ lý nhà trọ</h1><p>Ứng dụng chạy trên máy bạn. Mở bằng file khởi động để đăng nhập tự động.</p>${input("key", "Khóa quản lý", "", "password", "required minlength=32 autocomplete=off")}<button>Mở bảng điều khiển</button><p class="ra-field-help">Khóa nằm trong thư mục App_Data/rental-desktop/settings.json, không phải mật khẩu Facebook.</p></form></main>`;
  }
  function overview() {
    const d = state.data;
    return `<div class="ra-stats">${[["Phòng còn trống", d.rooms.filter(r => r.available).length], ["Nhóm đã chọn", d.groups.filter(g => g.enabled).length], ["Bài chờ đăng", d.deliveries.filter(x => x.status === "Queued").length], ["Lịch sắp tới", d.appointments.filter(a => new Date(a.visitAt) > new Date() && a.status !== "Cancelled").length]].map(([label, n]) => `<article><span>${label}</span><strong>${n}</strong></article>`).join("")}</div>
      <div class="ra-columns"><article class="ra-card"><h2>Bắt đầu trong vài bước</h2>${[["Thêm phòng thật", d.rooms.length > 0, "rooms"], ["Chọn nhóm được phép đăng", d.groups.some(g => g.enabled), "groups"], ["Kết nối Telegram nếu muốn ra lệnh từ xa", d.telegramConfigured, "settings"], ["Đăng nhập Facebook trong Chromium", d.browser === "Ready", "settings"]].map(([name, ready, tab]) => `<div class="ra-progress"><b>${ready ? "✓" : "·"}</b><span>${name}</span><button class="ra-link" data-action="tab" data-id="${tab}">Mở</button></div>`).join("")}<p class="ra-note">Ứng dụng khởi động ở trạng thái tạm dừng. Soạn và duyệt bài trước khi bật trợ lý.</p></article>
      <article class="ra-card"><h2>Chạy được khi chưa có khóa AI</h2><p>Chế độ hiện tại: <b>${esc(mode(d.aiMode))}</b>.</p><p>Mẫu miễn phí dùng đúng giá, địa chỉ và tiện ích bạn nhập để tạo bài, trả lời khách và thu lịch hẹn theo biểu mẫu. Đây là trả lời theo quy tắc, không phải AI suy luận.</p><p>Đổi sang AI API hoặc AI local trong Cấu hình khi sẵn sàng.</p>${button("tab", "Soạn bài đầu tiên", "posts")}</article></div>
      <article class="ra-card"><h2>Ra lệnh tại đây hoặc qua Telegram</h2><form data-form="command"><div class="ra-inline"><label>Lệnh / yêu cầu<input name="text" required maxlength="2000" placeholder="Đăng bài cho thuê phòng trọ…"></label><button>Thực hiện</button></div></form><div class="ra-chip-row">${["/phong", "/trangthai", "/lich", "/help"].map(s => button("command", s, s, true)).join("")}</div>${state.command ? `<pre class="ra-preview-bubble">${esc(state.command)}</pre>` : ""}</article>
      <article class="ra-card"><h2>Tình trạng kết nối</h2><div class="ra-health">${[["Lưu dữ liệu", "Cục bộ · tự lưu"], ["Telegram", d.telegramConfigured ? "Đã cấu hình" : "Chưa kết nối"], ["Messenger", d.selectorsConfigured ? "Đã cấu hình nhận diện" : "Cần thiết lập tin đến/đi"], ["Facebook", d.browser]].map(([a,b]) => `<div><span>${esc(a)}</span><strong>${esc(b)}</strong></div>`).join("")}</div>${d.lastError ? `<p class="ra-error">${esc(d.lastError)}</p>` : ""}<p class="ra-field-help">Đăng nhóm và inbox cá nhân cần phiên Facebook của bạn. Kết quả đăng có thể chờ quản trị viên nhóm duyệt.</p></article>`;
  }
  function rooms() {
    const r = state.room || { name: "", address: "", monthlyRent: 3000000, details: "", available: true };
    return `<div class="ra-columns"><form class="ra-card" data-form="room"><h2>${r.id ? "Sửa phòng" : "Thêm phòng trọ"}</h2>${input("name", "Tên phòng", r.name, "text", "required maxlength=150")}${input("address", "Địa chỉ", r.address, "text", "required maxlength=400")}${input("monthlyRent", "Giá thuê / tháng (VNĐ)", r.monthlyRent, "number", "required min=1 max=1000000000")}${textarea("details", "Tiện ích, điện nước, cọc, lưu ý và giờ xem phòng", r.details)}${check("available", "Còn phòng", r.available)}<div class="ra-actions"><button>Lưu phòng</button>${r.id ? button("new-room", "Hủy sửa", "", true) : ""}</div></form><div>${state.data.rooms.map(x => `<article class="ra-card"><div class="ra-card-top"><h2>${esc(x.name)}</h2>${badge(x.available ? "Còn trống" : "Đã hết")}</div><strong class="ra-price">${money(x.monthlyRent)}<small> / tháng</small></strong><p>${esc(x.address)}</p><p class="ra-pre">${esc(x.details)}</p><code class="ra-room-id">${esc(x.id)}</code><div class="ra-actions">${button("edit-room", "Sửa thông tin", x.id, true)}${button("draft-room", "Soạn bài", x.id)}</div></article>`).join("") || empty("Chưa có phòng. Nhập thông tin thật để bắt đầu.")}</div></div>`;
  }
  function groups() {
    const g = state.group || { name: "", url: "", enabled: false };
    return `<article class="ra-card"><h2>Tìm nhóm trên Facebook</h2><form data-form="search"><div class="ra-inline"><label>Từ khóa<input name="query" required placeholder="Phòng trọ quận 7"></label><button>Tìm nhóm</button></div></form><p class="ra-field-help">Cần đăng nhập Facebook. Chỉ tìm các nhóm đang hiển thị; bạn kiểm tra quy định và chọn nhóm được phép đăng.</p></article><div class="ra-columns"><form class="ra-card" data-form="group"><h2>${g.id ? "Sửa nhóm" : "Thêm nhóm bằng link"}</h2>${input("name", "Tên nhóm", g.name, "text", "required maxlength=150")}${input("url", "Link nhóm", g.url, "url", "required placeholder=https://www.facebook.com/groups/123456")}${check("enabled", "Tôi có quyền đăng và muốn chọn nhóm này", g.enabled)}<button>Lưu nhóm</button></form><div>${state.data.groups.map(x => `<article class="ra-card"><div class="ra-card-top"><h2>${esc(x.name)}</h2>${badge(x.enabled ? "Đã chọn" : "Chưa chọn")}</div><a href="${esc(x.url)}" target="_blank" rel="noreferrer">Mở nhóm Facebook</a><div class="ra-actions">${button("edit-group", "Sửa", x.id, true)}${button("toggle-group", x.enabled ? "Bỏ chọn" : "Chọn nhóm", x.id)}</div></article>`).join("") || empty("Chưa có nhóm. Thêm link hoặc tìm nhóm phía trên.")}</div></div>`;
  }
  function roomOptions(selected) { return state.data.rooms.filter(r => r.available).map(r => `<option value="${r.id}" ${r.id === selected ? "selected" : ""}>${esc(r.name)}</option>`).join(""); }
  function posts() {
    return `<form class="ra-card" data-form="draft"><h2>Soạn bài · ${esc(mode(state.data.aiMode))}</h2><div class="ra-inline"><label>Phòng<select name="roomId" required><option value="">Chọn phòng còn trống</option>${roomOptions(state.draftRoom)}</select></label><label>Yêu cầu<input name="instruction" value="Viết bài cho thuê phòng, rõ giá và tiện ích." maxlength="2000"></label><button>Tạo bản nháp</button></div><p class="ra-field-help">Chế độ mẫu dùng nội dung phòng; bạn có thể sửa bài hoặc dán nội dung đã viết bằng ChatGPT Plus trước khi duyệt.</p></form>${state.data.posts.map(p => `<article class="ra-card"><div class="ra-card-top"><h2>${esc(roomName(p.roomId))}</h2>${badge(p.status)}</div><p class="ra-pre">${esc(p.content)}</p><small>${esc(p.id)}</small><div class="ra-actions">${button("copy-post", "Sao chép nội dung", p.id, true)}${p.status === "Draft" ? button("edit-post", "Sửa / dán bài ChatGPT", p.id, true) + button("approve", `Duyệt đăng ${state.data.groups.filter(g => g.enabled).length} nhóm`, p.id) : ""}</div>${state.data.deliveries.filter(x => x.postId === p.id).map(d => `<div class="ra-delivery"><span>${esc(state.data.groups.find(g => g.id === d.groupId)?.name || "Nhóm")}</span>${badge(d.status)}${d.status === "Queued" ? button("cancel-delivery", "Hủy lượt", d.id, true) : ""}<small>${esc(d.detail)}</small></div>`).join("")}</article>`).join("") || empty("Các bản nháp sẽ xuất hiện ở đây.")}`;
  }
  function threads() {
    const t = state.thread || { customerName: "", url: "", autoReply: false };
    return `<div class="ra-notice">Messenger cá nhân cần đăng nhập và cấu hình nhận diện. Hội thoại cũ được lấy mốc khi đồng bộ, chỉ trả lời tin mới. Bật tự trả lời cho từng khách hoặc cho khách mới trong Cấu hình.</div><div class="ra-columns"><form class="ra-card" data-form="thread"><h2>Thêm / sửa khách</h2>${input("customerName", "Tên khách", t.customerName, "text", "required maxlength=150")}${input("url", "Link hội thoại", t.url, "url", "required placeholder=https://www.facebook.com/messages/t/123456")}${check("autoReply", "Cho phép trợ lý tự trả lời hội thoại này", t.autoReply)}<button>Lưu khách</button></form><div>${state.data.threads.map(x => `<article class="ra-card"><div class="ra-card-top"><h2>${esc(x.customerName || "Khách Messenger")}</h2>${badge(x.status)}</div><a href="${esc(x.url)}" target="_blank" rel="noreferrer">Mở Messenger</a><p>Tự trả lời: <b>${x.autoReply ? "Đang bật" : "Đang tắt"}</b></p>${x.lastError ? `<p class="ra-error">${esc(x.lastError)}</p>` : ""}${x.lastReply ? `<p class="ra-pre">${esc(x.lastReply)}</p>` : ""}${button("edit-thread", "Quản lý khách", x.id, true)}</article>`).join("") || empty("Chưa có khách. Thêm link hoặc bật đồng bộ sau khi thiết lập nhận diện.")}</div></div>`;
  }
  function appointments() {
    return `<form class="ra-card" data-form="appointment"><h2>Ghi lịch thủ công</h2><p class="ra-field-help">Lịch từ Messenger tự lưu sau khi khách xác nhận. Bạn cũng có thể nhập lịch chốt qua điện thoại tại đây.</p><div class="ra-columns"><div>${input("customerName", "Tên khách", "", "text", "required")}${input("phone", "Số điện thoại", "", "tel", "required pattern='(0[0-9]{9}|\\+84[0-9]{9})'")}</div><div><label>Phòng<select name="roomId" required>${roomOptions("")}</select></label>${input("visitAt", "Giờ xem (giờ Việt Nam)", "", "datetime-local", "required")}</div></div><button>Lưu lịch xem phòng</button></form><div class="ra-appointments">${state.data.appointments.map(a => `<article class="ra-card"><span class="ra-eyebrow">${esc(day(a.visitAt))}</span><h2>${esc(a.customerName)}</h2><p>${esc(roomName(a.roomId))}</p><p><a href="tel:${esc(a.phone)}">${esc(a.phone)}</a></p>${badge(a.status)}${!["Cancelled", "Completed"].includes(a.status) ? `<div class="ra-actions">${button("complete-appointment", "Đã xem", a.id, true)}${button("cancel-appointment", "Hủy lịch", a.id, true)}</div>` : ""}</article>`).join("") || empty("Chưa có lịch xem phòng.")}</div>`;
  }
  function settings() {
    const s = state.settings;
    if (!s) return empty("Đang tải cấu hình…");
    return `<form class="ra-card ra-settings" data-form="settings"><h2>Kết nối & vận hành</h2><p class="ra-field-help">Thay đổi được lưu trên máy bạn. Lưu cấu hình sẽ tạm dừng trợ lý để bạn kiểm tra trước khi bật lại.</p>${check("enabled", "Cho phép worker chạy", s.enabled)}${check("browserEnabled", "Cho phép mở Chromium và thao tác Facebook", s.browserEnabled)}
      <h2 class="ra-section-title">Nội dung & trả lời</h2><label>Chế độ<select name="aiMode"><option value="template" ${s.aiMode === "template" ? "selected" : ""}>Mẫu miễn phí · không cần khóa AI</option><option value="api" ${s.aiMode === "api" ? "selected" : ""}>AI API · khóa riêng, tính phí riêng</option><option value="local" ${s.aiMode === "local" ? "selected" : ""}>AI local · Ollama tương thích OpenAI</option></select></label><p class="ra-field-help">Bạn có thể viết bài bằng ChatGPT Plus rồi dán vào bản nháp. Chế độ tự gọi AI không dùng gói Plus.</p>${input("aiBaseUrl", "AI Base URL", s.aiBaseUrl, "url", "required")}${input("aiModel", "Tên model (bỏ trống khi dùng mẫu)", s.aiModel)}${input("aiApiKey", s.aiKeySaved ? "Khóa AI mới · đã có khóa, bỏ trống để giữ" : "Khóa AI · không cần khi dùng mẫu/local", "", "password", "autocomplete=off")}${check("clearAiApiKey", "Xóa khóa AI đã lưu", false)}<p class="ra-field-help">Ollama: http://127.0.0.1:11434/v1/ và tên model đã tải trên máy. Ứng dụng không tự cài hoặc tải model.</p>
      <h2 class="ra-section-title">Telegram</h2><p>Tạo bot với <a href="https://t.me/BotFather" target="_blank" rel="noreferrer">BotFather</a>, nhập token và numeric user ID của chính bạn. Chỉ tài khoản đó có quyền ra lệnh.</p>${input("telegramToken", s.telegramTokenSaved ? "Bot token mới · đã lưu token, bỏ trống để giữ" : "Bot token", "", "password", "autocomplete=off")}${input("telegramOwnerId", "Telegram user ID (số)", s.telegramOwnerId || "", "number", "min=1 step=1")}${check("clearTelegramToken", "Xóa bot token đã lưu", false)}${button("test-telegram", "Kiểm tra token đã lưu", "", true)}
      <h2 class="ra-section-title">Facebook cá nhân</h2><p>Bấm mở Facebook, đăng nhập trực tiếp và mở khóa Messenger nếu được yêu cầu. Không nhập mật khẩu Facebook vào ứng dụng này.</p>${button("facebook", "Mở Facebook để đăng nhập")}${check("autoReplyNewConversations", "Tự trả lời hội thoại mới được phát hiện sau lần đồng bộ đầu", s.autoReplyNewConversations)}
      <details><summary>Nhận diện Messenger & nút đăng (thiết lập nâng cao)</summary><p class="ra-field-help">Selector phụ thuộc giao diện Facebook thực tế. Tin đến và tin đi phải là hai CSS selector chuẩn khác nhau; node tin đến phải có thuộc tính ID ổn định. Khi không nhận diện chắc chắn, hệ thống dừng hội thoại.</p>${input("incomingMessages", "CSS selector từng tin đến", s.incomingMessages)}${input("outgoingMessages", "CSS selector từng tin đi", s.outgoingMessages)}${input("messageIdAttribute", "Thuộc tính ID tin nhắn", s.messageIdAttribute)}${input("messageEditor", "Selector ô nhập Messenger", s.messageEditor)}${input("composeButton", "Selector nút soạn bài nhóm", s.composeButton)}${input("postEditor", "Selector ô soạn bài nhóm", s.postEditor)}${input("publishButton", "Selector nút Đăng", s.publishButton)}<p class="ra-field-help">Lưu cấu hình trước, sau đó kiểm tra nhận diện bằng link một hội thoại thử.</p>${input("checkUrl", "Link hội thoại thử", "", "url", "placeholder=https://www.facebook.com/messages/t/123456")}${button("check-selectors", "Kiểm tra nhận diện · không gửi tin", "", true)}</details>
      <h2 class="ra-section-title">Tần suất</h2><div class="ra-columns">${input("postIntervalSeconds", "Khoảng cách đăng nhóm (giây)", s.postIntervalSeconds, "number", "required min=60")}${input("inboxIntervalSeconds", "Khoảng kiểm tra inbox (giây)", s.inboxIntervalSeconds, "number", "required min=30")}</div><button>Lưu cấu hình</button></form>`;
  }
  function preview() { return `<article class="ra-card"><h2>Thử câu trả lời trước khi kết nối Facebook</h2><p>Trả lời dựa trên các phòng bạn đã nhập. Thao tác này không gửi tin ra ngoài và không lưu lịch thật.</p><form data-form="preview">${textarea("text", "Tin nhắn khách", "Cho mình xin giá và địa chỉ phòng", 3)}<button>Thử trả lời</button></form>${state.chat ? `<div class="ra-chat-answer">${esc(state.chat)}</div>` : ""}</article><article class="ra-card"><h2>Cách đặt lịch ở chế độ mẫu</h2><p>Khách gửi: <code>Hẹn xem | Tên phòng | Họ tên | Số điện thoại | dd/MM/yyyy HH:mm</code>.</p><p>Trợ lý gửi lại thông tin để khách xác nhận. Sau khi khách nhắn “xác nhận”, lịch được lưu và báo Telegram. Chế độ AI hỗ trợ khai thác thông tin hội thoại tự nhiên hơn.</p></article>`; }
  function render() {
    if (!state.data) return renderLogin();
    const d = state.data;
    const tabs = [["overview", "Tổng quan"], ["rooms", "Phòng trọ"], ["posts", "Bài đăng"], ["groups", "Nhóm Facebook"], ["threads", "Khách & Messenger"], ["appointments", "Lịch xem phòng"], ["preview", "Thử trả lời"], ["settings", "Cấu hình"]];
    const title = tabs.find(t => t[0] === state.tab)?.[1] || "Tổng quan";
    const views = { overview, rooms, groups, posts, threads, appointments, settings, preview };
    root.innerHTML = `<main class="ra-app"><aside class="ra-sidebar"><span class="ra-brand">AI TOOL</span><h2>Rental desk</h2><p>Trợ lý cho thuê phòng</p><nav>${tabs.map(([id, name]) => `<button data-action="tab" data-id="${id}" class="${state.tab === id ? "active" : ""}">${name}</button>`).join("")}</nav><div class="ra-sidebar-note">C# · Chạy trên máy bạn<br>Dữ liệu lưu tự động</div><button class="ra-logout" data-action="backup">Tải bản sao dữ liệu</button></aside><section class="ra-main"><header class="ra-header"><div><span class="ra-eyebrow">KHÔNG GIAN LÀM VIỆC</span><h1>${title}</h1></div>${button("toggle", d.state.paused ? "Bật trợ lý" : "Tạm dừng trợ lý", "", !d.state.paused)}</header><div id="error" role="alert" class="ra-error" ${state.error ? "" : "hidden"}>${esc(state.error)}</div><div class="ra-statusbar"><span class="ra-dot ${d.state.paused ? "paused" : ""}"></span><span>${d.state.paused ? "Đang tạm dừng" : "Đang hoạt động"} · ${esc(mode(d.aiMode))}</span><span>Facebook: ${esc(d.browser)}</span><button class="ra-link" data-action="refresh">Làm mới</button></div>${views[state.tab]()}<footer class="ra-footer">AI TOOL · Bản chạy cục bộ · Không cần npm hoặc SQL Server để sử dụng bản này</footer></section></main>`;
    if (state.modal) {
      const p = state.modal;
      root.querySelector("main").insertAdjacentHTML("beforeend", `<div class="ra-dialog-backdrop"><form class="ra-dialog" data-form="edit-post"><h2>Sửa nội dung bài</h2><p>Bạn có thể dán bài viết đã soạn bằng ChatGPT Plus tại đây.</p>${textarea("content", "Nội dung", p.content, 10)}<div class="ra-actions"><button>Lưu bản nháp</button>${button("close-modal", "Hủy", "", true)}</div></form></div>`);
    }
  }
  root.addEventListener("submit", event => {
    const form = event.target.closest("form[data-form]"); if (!form) return; event.preventDefault();
    const f = new FormData(form), val = n => String(f.get(n) || ""), num = n => Number(f.get(n) || 0), yes = n => f.has(n);
    const kind = form.dataset.form;
    if (kind === "login") { state.key = val("key"); return work(async () => { await refresh(); state.settings = await api("settings"); }); }
    work(async () => {
      if (kind === "room") { await api(`rooms/${state.room?.id || crypto.randomUUID()}`, "PUT", { ...state.room, name: val("name"), address: val("address"), monthlyRent: num("monthlyRent"), details: val("details"), available: yes("available") }); state.room = null; toast("Đã lưu phòng."); }
      if (kind === "group") { await api(`groups/${state.group?.id || crypto.randomUUID()}`, "PUT", { ...state.group, name: val("name"), url: val("url"), enabled: yes("enabled") }); state.group = null; toast("Đã lưu nhóm."); }
      if (kind === "thread") { await api(`threads/${state.thread?.id || crypto.randomUUID()}`, "PUT", { ...state.thread, customerName: val("customerName"), url: val("url"), autoReply: yes("autoReply") }); state.thread = null; toast("Đã lưu khách."); }
      if (kind === "draft") { await api("posts", "POST", { roomId: val("roomId"), instruction: val("instruction") }); state.draftRoom = ""; toast("Đã tạo bản nháp. Kiểm tra nội dung trước khi duyệt."); }
      if (kind === "edit-post") { await api(`posts/${state.modal.id}`, "PUT", { content: val("content") }); state.modal = null; toast("Đã lưu nội dung."); }
      if (kind === "search") { const result = await api("groups/search", "POST", { query: val("query") }); toast(`Tìm thấy ${result.count} nhóm. Kiểm tra và chọn nhóm muốn đăng.`); }
      if (kind === "command") state.command = (await api("command", "POST", { text: val("text") })).reply;
      if (kind === "preview") { const result = await api("chat-preview", "POST", { text: val("text") }); state.chat = result.reply + (result.appointment ? "\n\nĐề xuất lịch: " + day(result.appointment.visitAt) + " — " + result.appointment.customerName + ". Chưa lưu lịch thật." : ""); }
      if (kind === "appointment") { await api("appointments", "POST", { customerName: val("customerName"), phone: val("phone"), roomId: val("roomId"), visitAt: val("visitAt") + ":00+07:00" }); toast("Đã lưu lịch xem phòng."); }
      if (kind === "settings") {
        const payload = { enabled: yes("enabled"), browserEnabled: yes("browserEnabled"), autoReplyNewConversations: yes("autoReplyNewConversations"), aiMode: val("aiMode"), aiBaseUrl: val("aiBaseUrl"), aiModel: val("aiModel"), aiApiKey: val("aiApiKey"), clearAiApiKey: yes("clearAiApiKey"), telegramToken: val("telegramToken"), telegramOwnerId: num("telegramOwnerId"), clearTelegramToken: yes("clearTelegramToken"), postIntervalSeconds: num("postIntervalSeconds"), inboxIntervalSeconds: num("inboxIntervalSeconds") };
        ["incomingMessages", "outgoingMessages", "messageIdAttribute", "messageEditor", "composeButton", "postEditor", "publishButton"].forEach(k => payload[k] = val(k));
        await api("settings", "PUT", payload); state.settings = await api("settings"); toast("Đã lưu cấu hình. Bật trợ lý khi đã kiểm tra xong.");
      }
    });
  });
  root.addEventListener("click", event => {
    const el = event.target.closest("[data-action]"); if (!el || state.busy) return;
    const action = el.dataset.action, id = el.dataset.id;
    if (action === "tab") { state.tab = id; state.error = ""; render(); return; }
    if (action === "edit-room") { state.room = state.data.rooms.find(r => r.id === id); render(); return; }
    if (action === "new-room") { state.room = null; render(); return; }
    if (action === "edit-group") { state.group = state.data.groups.find(g => g.id === id); render(); return; }
    if (action === "edit-thread") { state.thread = state.data.threads.find(t => t.id === id); render(); return; }
    if (action === "draft-room") { state.draftRoom = id; state.tab = "posts"; render(); return; }
    if (action === "edit-post") { state.modal = state.data.posts.find(p => p.id === id); render(); return; }
    if (action === "close-modal") { state.modal = null; render(); return; }
    work(async () => {
      if (action === "refresh") { state.settings = await api("settings"); }
      if (action === "toggle") { await api("state", "PUT", { paused: !state.data.state.paused }); toast(state.data.state.paused ? "Đã bật trợ lý." : "Đã tạm dừng."); }
      if (action === "facebook") { await api("browser/open", "POST", {}); toast("Đăng nhập trong cửa sổ Chromium trên máy bạn."); }
      if (action === "toggle-group") { const g = state.data.groups.find(x => x.id === id); await api(`groups/${id}`, "PUT", { ...g, enabled: !g.enabled }); }
      if (action === "approve") { await api(`posts/${id}/approve`, "POST", {}); toast("Đã xếp hàng đăng các nhóm được chọn."); }
      if (action === "copy-post") { await navigator.clipboard.writeText(state.data.posts.find(p => p.id === id).content); toast("Đã sao chép nội dung."); }
      if (action === "cancel-delivery") await api(`deliveries/${id}/cancel`, "POST", {});
      if (action === "command") state.command = (await api("command", "POST", { text: id })).reply;
      if (action === "test-telegram") { const result = await api("telegram/test", "POST", {}); toast(`Kết nối được bot @${result.username}. Nhắn /start cho bot rồi lưu Telegram user ID của bạn.`); }
      if (action === "check-selectors") { const url = root.querySelector('[name="checkUrl"]').value; const result = await api("browser/check", "POST", { url }); toast(JSON.stringify(result, null, 2)); }
      if (action === "complete-appointment" || action === "cancel-appointment") await api(`appointments/${id}/status`, "PUT", { status: action === "complete-appointment" ? "Completed" : "Cancelled" });
      if (action === "backup") {
        const response = await fetch("/api/rental-assistant/backup", { headers: { "X-Rental-Key": state.key } });
        if (!response.ok) throw new Error("Chưa tải được bản sao.");
        const url = URL.createObjectURL(await response.blob()), link = document.createElement("a"); link.href = url; link.download = `AI_TOOL-${new Date().toISOString().slice(0,10)}.json`; link.click(); setTimeout(() => URL.revokeObjectURL(url), 1000);
      }
    });
  });
  async function start() {
    if (!state.key) return renderLogin();
    try { await refresh(); state.settings = await api("settings"); render(); }
    catch (e) { renderLogin(); toast(e.message); }
  }
  setInterval(async () => {
    if (!state.data || state.busy || state.modal) return;
    try { await refresh(); if (state.tab === "overview" && !["INPUT", "TEXTAREA", "SELECT"].includes(document.activeElement.tagName)) render(); }
    catch { /* Keep forms intact while reconnecting. The next explicit action displays the error. */ }
  }, 10000);
  start();
})();
