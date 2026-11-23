"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import "./rental.css";
import RoomManager, { Room } from "./RoomManager";
import PostManager from "./PostManager";
import PostingQueueCard from "./PostingQueueCard";
import GroupManager, { RentalGroup } from "./GroupManager";
import MessengerSetupCard from "./MessengerSetupCard";
import OverviewDashboard, { TrackedDelivery } from "./OverviewDashboard";
import CustomerSettings, { CustomerAccount, accountApi, accountBase } from "./CustomerSettings";

type Group = RentalGroup;
type Thread = { id: string; customerName: string; url: string; autoReply: boolean; status: string; lastError: string; lastReply: string };
type Post = { id: string; roomId: string; content: string; status: string; images: string[] };
type Delivery = TrackedDelivery;
type Appointment = { id: string; roomId: string; customerName: string; phone: string; visitAt: string; status: string };
type Overview = { state: { paused: boolean }; browser: string; enabled: boolean; browserEnabled: boolean; aiConfigured: boolean; telegramConfigured: boolean; rooms: Room[]; groups: Group[]; threads: Thread[]; posts: Post[]; deliveries: Delivery[]; appointments: Appointment[] };
const money = (n: number) => new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(n);
const date = (value: string) => new Date(value).toLocaleString("vi-VN", { timeZone: "Asia/Ho_Chi_Minh" });
const browserLabels: Record<string, string> = { SettingUpMessenger: "Đang thiết lập Messenger", Ready: "Sẵn sàng", UploadingPhotos: "Đang tải ảnh", SubmittingPost: "Đang gửi bài", FindingPostLink: "Đang tự lấy link bài", PostLinkPending: "Đã gửi · chờ link bài", PublishNeedsReview: "Đăng bị dừng · cần kiểm tra", LoginRequired: "Cần đăng nhập", NotStarted: "Chưa mở trình duyệt" };
const labels: Record<string, string> = { Draft: "Bản nháp", Approved: "Đã duyệt", Queued: "Chờ đăng", Submitted: "Đã gửi · cần kiểm tra", NeedsReview: "Cần kiểm tra", Sending: "Đang gửi", Cancelled: "Đã hủy", Skipped: "Đã bỏ qua", New: "Chờ đồng bộ", NewIncoming: "Khách mới", Watching: "Đang theo dõi", Replying: "Đang trả lời", CustomerConfirmed: "Khách đã xác nhận" };

export default function RentalAssistantPage({ embedded = false }: { embedded?: boolean }) {
  const [key, setKey] = useState("");
  const [userName, setUserName] = useState("");
  const [password, setPassword] = useState("");
  const [account, setAccount] = useState<CustomerAccount | null>(null);
  const [connected, setConnected] = useState(false);
  const [restoreSession, setRestoreSession] = useState(false);
  useEffect(() => {
    try {
      sessionStorage.removeItem("rental-management-key");
      const saved = sessionStorage.getItem("rental-access-token") || localStorage.getItem("AccessToken");
      if (saved) { setKey(saved); setRestoreSession(true); }
    } catch { /* Storage may be unavailable in a restricted browser. */ }
  }, []);
  const [data, setData] = useState<Overview | null>(null);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [tab, setTab] = useState("overview");
  useEffect(() => {
    const syncTab = () => { const next = window.location.hash.slice(1); setTab(["overview", "rooms", "posts", "groups", "threads", "appointments", "settings"].includes(next) ? next : "overview"); };
    syncTab(); window.addEventListener("hashchange", syncTab);
    return () => window.removeEventListener("hashchange", syncTab);
  }, []);
  const [thread, setThread] = useState<Partial<Thread>>({ customerName: "", url: "", autoReply: false });
  const [draftRoom, setDraftRoom] = useState("");
  const [nextDeliveryId, setNextDeliveryId] = useState("");
  const [instruction, setInstruction] = useState("Viết tin cho thuê phòng, thân thiện, rõ giá và tiện ích.");
  const api = useCallback(async (path: string, method = "GET", body?: unknown) => {
    const base = (process.env.NEXT_PUBLIC_API_URL || "http://localhost:5111").replace(/\/$/, "");
    const result = await fetch(`${base}/api/rental-assistant/${path}`, {
      method, credentials: "include", headers: body instanceof FormData ? { Authorization: `Bearer ${key}` } : { Authorization: `Bearer ${key}`, "Content-Type": "application/json" },
      body: body instanceof FormData ? body : body === undefined ? undefined : JSON.stringify(body), cache: "no-store",
    });
    if (!result.ok) {
      if (result.status === 401) {
        try { sessionStorage.removeItem("rental-access-token"); localStorage.removeItem("AccessToken"); } catch { }
        setConnected(false);
        throw new Error("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
      }
      const text = await result.text();
      throw new Error(result.status >= 500 ? "API chưa sẵn sàng. Kiểm tra kết nối SQL Server và chạy khởi tạo database." : text || `Lỗi ${result.status}`);
    }
    const text = await result.text();
    return text ? JSON.parse(text) : null;
  }, [key]);
  const refresh = useCallback(async () => {
    const next = await api("overview");
    // Establish the private media cookie before mounting room and post images.
    const nextAccount = await accountApi("me");
    setAccount(nextAccount); setData(next); setConnected(true);
    try { sessionStorage.setItem("rental-access-token", key); } catch { }
  }, [api, key]);
  useEffect(() => {
    if (!restoreSession) return;
    let cancelled = false;
    setBusy(true);
    refresh().catch(e => { if (!cancelled) setError(e.message); }).finally(() => {
      if (!cancelled) { setBusy(false); setRestoreSession(false); }
    });
    return () => { cancelled = true; };
  }, [restoreSession, refresh]);
  const lock = () => {
    try { sessionStorage.removeItem("rental-access-token"); localStorage.removeItem("AccessToken"); } catch { }
    setRestoreSession(false); setConnected(false); setData(null); setAccount(null); setKey("");
  };
  async function action(work: () => Promise<unknown>) {
    setBusy(true); setError("");
    try { await work(); await refresh(); } catch (e) { setError(e instanceof Error ? e.message : "Không thực hiện được thao tác."); }
    finally { setBusy(false); }
  }
  useEffect(() => {
    if (!connected) return;
    const timer = setInterval(() => { refresh().catch(e => setError(e.message)); }, 10000);
    return () => clearInterval(timer);
  }, [connected, refresh]);
  const submit = (e: FormEvent, work: () => Promise<unknown>) => { e.preventDefault(); void action(work); };
  const roomName = (id: string) => data?.rooms.find(r => r.id === id)?.name || id;
  const badge = (status: string) => <span className={`ra-badge ${status === "NeedsReview" ? "ra-danger" : ""}`}>{labels[status] || status}</span>;

  async function login() {
    const response = await fetch(`${accountBase()}/api/Account/Login`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ userName, password }) });
    const result = await response.json();
    if (!response.ok || !result.status || !result.data?.token) throw new Error("Tên đăng nhập hoặc mật khẩu không đúng, hoặc tài khoản đang bị khóa.");
    sessionStorage.setItem("rental-access-token", result.data.token);
    setKey(result.data.token); setPassword(""); setRestoreSession(true);
  }
  if (!connected || !data) return <main className={"ra-login" + (embedded ? " ra-embedded" : "")}><form className="ra-connect" onSubmit={async e => { e.preventDefault(); setBusy(true); setError(""); try { await login(); } catch (err) { setError(err instanceof Error ? err.message : "Không đăng nhập được."); } finally { setBusy(false); } }}>
    <span className="ra-brand">AI TOOL / RENTAL DESK</span><h1>Trợ lý nhà trọ</h1>
    <p>Điều khiển qua Telegram. Theo dõi phòng, bài đăng, khách và lịch xem ở một nơi.</p>
    <label>Tên đăng nhập<input required autoComplete="username" value={userName} onChange={e => setUserName(e.target.value)} placeholder="Tài khoản được cấp" /></label>
    <label>Mật khẩu<input required type="password" autoComplete="current-password" value={password} onChange={e => setPassword(e.target.value)} /></label>
    <button disabled={busy}>{busy ? "Đang kết nối…" : "Mở bảng điều khiển"}</button>
    {error && <p role="alert" className="ra-error">{error}</p>}
    <small>Mỗi tài khoản có dữ liệu và phiên Facebook riêng. Liên hệ quản trị viên để được cấp tài khoản hoặc gia hạn.</small>
  </form></main>;

  return <main className={"ra-app" + (embedded ? " ra-embedded" : "") + (tab === "overview" ? " ra-overview" : "")}>
    {!embedded && <aside className="ra-sidebar"><span className="ra-brand">AI TOOL</span><h2>Rental desk</h2><p>Trợ lý cho thuê phòng</p>
      <nav>{[["overview", "Tổng quan"], ["rooms", "Phòng trọ"], ["posts", "Bài đăng"], ["groups", "Nhóm Facebook"], ["threads", "Khách & Messenger"], ["appointments", "Lịch xem phòng"], ["settings", account?.isOwner ? "Tài khoản & khách thuê" : "Cấu hình của tôi"]].map(([id, label]) => <button key={id} className={tab === id ? "active" : ""} onClick={() => { setTab(id); window.location.hash = id; }}>{label}</button>)}</nav>
      <div className="ra-sidebar-note">Telegram là nơi ra lệnh.<br />Đây là nơi kiểm tra và quản lý.</div>
      <button className="ra-logout" onClick={lock}>Khóa bảng điều khiển</button>
    </aside>}
    <section className="ra-main"><header className="ra-header"><div><span className="ra-eyebrow">KHÔNG GIAN LÀM VIỆC</span><h1>{({ overview: "Hôm nay có gì?", rooms: "Quản lý phòng trọ", posts: "Nội dung & lượt đăng", groups: "Nhóm Facebook", threads: "Khách & Messenger", appointments: "Lịch xem phòng", settings: "Tài khoản & cấu hình" } as Record<string, string>)[tab]}</h1></div>
      <button disabled={busy} className={data.state.paused ? "" : "ra-secondary"} onClick={() => action(() => api("state", "PUT", { paused: !data.state.paused }))}>{data.state.paused ? "Bật trợ lý" : "Tạm dừng trợ lý"}</button></header>
      {error && <div role="alert" className="ra-error">{error}</div>}
      <div className="ra-statusbar"><span className={`ra-dot ${data.state.paused || !data.enabled ? "paused" : ""}`} />{!data.enabled ? "Worker chưa bật trong cấu hình" : data.state.paused ? "Trợ lý đang tạm dừng" : "Trợ lý đang hoạt động"}<span>Facebook: {browserLabels[data.browser] || data.browser}</span><button className="ra-link" disabled={busy} onClick={() => action(refresh)}>Làm mới</button><button className="ra-link" onClick={() => { setTab("settings"); window.location.hash = "settings"; }}>{account?.isOwner ? "Tài khoản & khách thuê" : "Cấu hình của tôi"}</button>{embedded && <button className="ra-link" disabled={busy} onClick={lock}>Đăng xuất</button>}</div>
      {tab === "settings" && account && <CustomerSettings account={account} onSaved={() => void refresh()} />}

      {(tab === "overview" || tab === "posts") && <PostingQueueCard api={api} onNextDelivery={setNextDeliveryId} />}
      {tab === "overview" && <OverviewDashboard data={data} busy={busy} api={api} action={action} />}

      {tab === "rooms" && <RoomManager rooms={data.rooms} busy={busy} api={api} action={action} onDraft={id => { setDraftRoom(id); setTab("posts"); window.location.hash = "posts"; }} />}

      {tab === "groups" && <GroupManager groups={data.groups} busy={busy} api={api} action={action} />}

      {tab === "posts" && <PostManager nextDeliveryId={nextDeliveryId} rooms={data.rooms} posts={data.posts} deliveries={data.deliveries} groups={data.groups} busy={busy} draftRoom={draftRoom} setDraftRoom={setDraftRoom} instruction={instruction} setInstruction={setInstruction} api={api} action={action} />}

      {tab === "threads" && <><MessengerSetupCard threads={data.threads} api={api} /><div className="ra-notice">Chỉ theo dõi những hội thoại hiện trong inbox trình duyệt hoặc được thêm dưới đây. Tin cũ được lấy làm mốc khi khởi động, không trả lời lại hàng loạt. Cần cấu hình selector tin đến/tin đi trước khi bật.</div><div className="ra-columns"><form className="ra-card" onSubmit={e => submit(e, async () => { await api(`threads/${thread.id || crypto.randomUUID()}`, "PUT", thread); setThread({ customerName: "", url: "", autoReply: false }); })}><h2>Thêm / sửa khách</h2><label>Tên khách<input required maxLength={150} value={thread.customerName} onChange={e => setThread({ ...thread, customerName: e.target.value })} /></label><label>Link hội thoại Facebook<input required type="url" placeholder="https://www.facebook.com/messages/t/123456" value={thread.url} onChange={e => setThread({ ...thread, url: e.target.value })} /></label><label className="ra-check"><input type="checkbox" checked={thread.autoReply} onChange={e => setThread({ ...thread, autoReply: e.target.checked })} />Cho phép AI tự trả lời khách này</label><button disabled={busy}>Lưu khách</button></form><div>{data.threads.length === 0 && <article className="ra-card">Chưa có khách. Khi bật worker, các hội thoại đang hiển thị sẽ được đồng bộ.</article>}{data.threads.map(t => <article className="ra-card" key={t.id}><div className="ra-card-top"><h2>{t.customerName || "Khách Messenger"}</h2>{badge(t.status)}</div><a href={t.url} target="_blank" rel="noreferrer">Mở Messenger</a><p>AI trả lời: <b>{t.autoReply ? "Đang bật" : "Đang tắt"}</b></p>{t.lastError && <p className="ra-error">{t.lastError}</p>}{t.lastReply && <p className="ra-pre">Phản hồi gần nhất: {t.lastReply}</p>}<button className="ra-secondary" onClick={() => setThread(t)}>Quản lý khách</button></article>)}</div></div></>}

      {tab === "appointments" && <><div className="ra-notice">Lịch được lưu sau khi khách xác nhận lại thông tin. Thời gian hiển thị theo giờ Việt Nam. Gửi /lich qua Telegram để xem nhanh.</div>{data.appointments.length === 0 ? <div className="ra-empty">Chưa có lịch hẹn. Khi khách xác nhận, thông tin sẽ được lưu tại đây.</div> : <div className="ra-appointments">{data.appointments.map(a => <article className="ra-card" key={a.id}><span className="ra-eyebrow">{date(a.visitAt)}</span><h2>{a.customerName}</h2><p>{roomName(a.roomId)}</p><p><a href={`tel:${a.phone}`}>{a.phone}</a></p>{badge(a.status)}</article>)}</div>}</>}
      <footer className="ra-footer">AI TOOL · Trợ lý cho thuê phòng · Dữ liệu cập nhật mỗi 10 giây</footer>
    </section>
  </main>;
}
