"use client";
import { useCallback, useEffect, useRef, useState } from "react";

export type CustomerAccount = { id: string; name: string; expiresAt: string; enabled: boolean; isOwner: boolean; browserEnabled: boolean; telegramOwnerId: number; telegramConfigured: boolean; legacy: boolean };
export const accountBase = () => (process.env.NEXT_PUBLIC_API_URL || "http://localhost:5111").replace(/\/$/, "");
export async function accountApi(path: string, method = "GET", body?: unknown) {
  const token = sessionStorage.getItem("rental-access-token") || localStorage.getItem("AccessToken") || "";
  const response = await fetch(`${accountBase()}/api/rental-account/${path}`, { method, credentials: "include", cache: "no-store", headers: { Authorization: `Bearer ${token}`, "Content-Type": "application/json" }, body: body === undefined ? undefined : JSON.stringify(body) });
  if (!response.ok) throw new Error((await response.text()) || `Không thực hiện được (${response.status}).`);
  if (path === "facebook/screen") return response.blob();
  const text = await response.text(); return text ? JSON.parse(text) : null;
}

export default function CustomerSettings({ account, onSaved }: { account: CustomerAccount; onSaved: () => void }) {
  const [enabled, setEnabled] = useState(account.browserEnabled);
  const [ownerId, setOwnerId] = useState(String(account.telegramOwnerId || ""));
  const [bot, setBot] = useState("");
  const [clearBot, setClearBot] = useState(false);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [screen, setScreen] = useState("");
  const [remote, setRemote] = useState(false);
  const [text, setText] = useState("");
  const [secret, setSecret] = useState(true);
  const screenUrl = useRef("");
  const [oldPassword, setOldPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  async function run(work: () => Promise<unknown>, success = "Đã lưu.") {
    setBusy(true); setError(""); setMessage("");
    try { await work(); setMessage(success); } catch (e) { setError(e instanceof Error ? e.message : "Không thực hiện được."); } finally { setBusy(false); }
  }
  const refreshScreen = useCallback(async () => {
    const blob = await accountApi("facebook/screen");
    const url = URL.createObjectURL(blob);
    if (screenUrl.current) URL.revokeObjectURL(screenUrl.current);
    screenUrl.current = url; setScreen(url);
  }, []);
  useEffect(() => {
    if (!remote) return;
    let live = true;
    let timer: ReturnType<typeof setTimeout>;
    const poll = async () => { try { await refreshScreen(); if (live) timer = setTimeout(poll, 2000); } catch { if (live) { setRemote(false); setError("Phiên điều khiển đã kết thúc. Bạn có thể mở lại."); } } };
    void poll();
    return () => { live = false; clearTimeout(timer); };
  }, [remote, refreshScreen]);
  useEffect(() => () => { if (screenUrl.current) URL.revokeObjectURL(screenUrl.current); }, []);
  async function input(kind: string, value = "", x = 0, y = 0) { await accountApi("facebook/input", "POST", { kind, text: value, x, y }); await refreshScreen(); }
  return <div className="ra-customer-settings">
    <div className="ra-customer-hero"><div><span className="ra-eyebrow">KHÔNG GIAN RIÊNG</span><h2>{account.name}</h2><p>Dữ liệu, nhóm đăng bài và phiên Facebook được tách riêng cho tài khoản của bạn.</p></div><span>Hạn sử dụng: <b>{new Date(account.expiresAt).toLocaleDateString("vi-VN")}</b></span></div>
    {error && <p role="alert" className="ra-error">{error}</p>}{message && <p role="status" className="ra-notice">{message}</p>}
    <div className="ra-grid"><section className="ra-card"><h2>Facebook & Telegram</h2>
      {account.legacy && <p>Phiên Facebook và dữ liệu cũ của chủ hệ thống được giữ nguyên. Khách thuê dùng phiên độc lập.</p>}<form onSubmit={e => { e.preventDefault(); void run(async () => { await accountApi("settings", "PUT", { browserEnabled: enabled, telegramOwnerId: Number(ownerId || 0), telegramToken: bot || null, clearTelegram: clearBot }); setBot(""); onSaved(); }); }}>
        <label className="ra-check"><input type="checkbox" checked={enabled} onChange={e => setEnabled(e.target.checked)} />Bật trình duyệt Facebook riêng</label>
        <label>Telegram bot token<input type="password" autoComplete="off" value={bot} onChange={e => setBot(e.target.value)} placeholder={account.telegramConfigured ? "Đã lưu • để trống để giữ nguyên" : "Token từ BotFather"} /></label>
        <label>Telegram user ID<input inputMode="numeric" pattern="[0-9]*" value={ownerId} onChange={e => setOwnerId(e.target.value)} placeholder="ID Telegram của bạn" /></label>
        <label className="ra-check"><input type="checkbox" checked={clearBot} onChange={e => setClearBot(e.target.checked)} />Ngắt kết nối bot đã lưu</label><button disabled={busy}>Lưu cấu hình</button>
      </form>
      <hr /><p>Đăng nhập trực tiếp vào Facebook trong phiên riêng. Hỗ trợ nhập mã xác minh; phiên điều khiển tự hết sau 10 phút.</p>
      <button disabled={busy || (!account.browserEnabled && !account.legacy)} onClick={() => run(async () => { await accountApi("facebook/start", "POST"); setRemote(true); }, "Đã mở phiên Facebook riêng.")}>Kết nối Facebook</button>
    </section><form className="ra-card" onSubmit={e => { e.preventDefault(); void run(async () => { await accountApi("password", "PUT", { oldPassword, newPassword }); setOldPassword(""); setNewPassword(""); }, "Đã đổi mật khẩu."); }}><h2>Đổi mật khẩu</h2><label>Mật khẩu hiện tại<input required type="password" autoComplete="current-password" value={oldPassword} onChange={e => setOldPassword(e.target.value)} /></label><label>Mật khẩu mới<input required minLength={8} type="password" autoComplete="new-password" value={newPassword} onChange={e => setNewPassword(e.target.value)} /></label><button disabled={busy}>Đổi mật khẩu</button><p>Mật khẩu tối thiểu 8 ký tự, có chữ hoa, chữ thường và số.</p></form></div>
    {remote && <section className="ra-card ra-remote"><div className="ra-card-top"><h2>Phiên Facebook của bạn</h2><button className="ra-secondary" onClick={() => run(async () => { await accountApi("facebook/finish", "POST"); setRemote(false); setScreen(""); onSaved(); }, "Đã kết thúc điều khiển. Worker tiếp tục xử lý.")}>Xong — tiếp tục worker</button></div><p>Bấm vào ô trên ảnh, nhập nội dung bên dưới rồi chọn “Nhập vào Facebook”. Worker tạm chờ khi bạn điều khiển.</p>
      {screen && <img src={screen} alt="Phiên đăng nhập Facebook riêng" onClick={e => { const rect = e.currentTarget.getBoundingClientRect(); void run(() => input("click", "", Math.floor((e.clientX - rect.left) * 1100 / rect.width), Math.floor((e.clientY - rect.top) * 760 / rect.height)), "Đã chọn vị trí."); }} />}
      <div className="ra-remote-tools"><input aria-label="Nội dung nhập vào ô Facebook đã chọn" type={secret ? "password" : "text"} autoComplete="off" value={text} onChange={e => setText(e.target.value)} placeholder="Email, mật khẩu hoặc mã xác minh…" /><button disabled={busy || !text} onClick={() => run(async () => { const value = text; setText(""); await input("text", value); }, "Đã nhập.")}>Nhập vào Facebook</button><label className="ra-check"><input type="checkbox" checked={secret} onChange={e => setSecret(e.target.checked)} />Ẩn nội dung</label></div>
      <div className="ra-remote-tools">{["Tab", "Enter", "Backspace", "Control+A", "Escape"].map(key => <button className="ra-secondary" key={key} disabled={busy} onClick={() => run(() => input("key", key), "")}>{key === "Control+A" ? "Chọn hết" : key}</button>)}<button className="ra-secondary" onClick={() => run(() => input("scroll", "", 0, 500), "")}>Cuộn xuống</button><button className="ra-secondary" onClick={() => run(() => input("scroll", "", 0, -500), "")}>Cuộn lên</button></div></section>}
    {account.isOwner && <section className="ra-card"><h2>Quản trị hệ thống</h2><p>Quản lý tài khoản khách thuê, vai trò và phân quyền trong phân hệ quản trị.</p><a href="/rental-customers">Mở quản lý tài khoản & khách thuê</a></section>}
  </div>;
}
