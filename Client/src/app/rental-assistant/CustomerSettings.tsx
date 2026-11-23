"use client";
import { useCallback, useEffect, useRef, useState } from "react";

export type CustomerAccount = { id: string; name: string; expiresAt: string; enabled: boolean; isOwner: boolean; browserEnabled: boolean; telegramOwnerId: number; telegramConfigured: boolean; legacy: boolean };
type Customer = CustomerAccount & { userName: string; active: boolean };
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
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [screen, setScreen] = useState("");
  const [remote, setRemote] = useState(false);
  const [text, setText] = useState("");
  const [secret, setSecret] = useState(true);
  const screenUrl = useRef("");
  const [newCustomer, setNewCustomer] = useState({ name: "", userName: "", email: "", password: "", expiresAt: new Date(Date.now() + 30 * 86400000).toISOString().slice(0, 10) });
  const [oldPassword, setOldPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [resetCustomer, setResetCustomer] = useState("");
  const [resetPassword, setResetPassword] = useState("");
  const loadCustomers = useCallback(async () => { if (account.isOwner) setCustomers(await accountApi("customers")); }, [account.isOwner]);
  useEffect(() => { void loadCustomers().catch(e => setError(e.message)); }, [loadCustomers]);
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
    {account.isOwner && <><form className="ra-card" onSubmit={e => { e.preventDefault(); void run(async () => { await accountApi("customers", "POST", { ...newCustomer, expiresAt: new Date(newCustomer.expiresAt + "T23:59:59+07:00").toISOString() }); setNewCustomer({ ...newCustomer, name: "", userName: "", email: "", password: "" }); await loadCustomers(); }, "Đã cấp tài khoản. Khách đăng nhập tại /rental-assistant bằng tên và mật khẩu bạn vừa tạo."); }}><h2>Cấp tài khoản khách thuê</h2><div className="ra-customer-fields">{[["name", "Tên khách"], ["userName", "Tên đăng nhập"], ["email", "Email"], ["password", "Mật khẩu ban đầu"], ["expiresAt", "Sử dụng đến ngày"]].map(([key, label]) => <label key={key}>{label}<input required type={key === "password" ? "password" : key === "email" ? "email" : key === "expiresAt" ? "date" : "text"} autoComplete="off" minLength={key === "password" ? 8 : undefined} value={newCustomer[key as keyof typeof newCustomer]} onChange={e => setNewCustomer({ ...newCustomer, [key]: e.target.value })} /></label>)}</div><button disabled={busy}>Cấp tài khoản</button></form>
      <section className="ra-card"><h2>Khách thuê · {customers.filter(c => !c.legacy).length}</h2>{customers.map(c => <div key={c.id} className="ra-customer-row"><div><b>{c.name}</b><p>{c.userName} · {c.legacy ? "Chủ hệ thống" : c.active ? "Đang hoạt động" : c.enabled ? "Đã hết hạn" : "Đã khóa"}</p></div>{!c.legacy && <><label>Hạn sử dụng<input aria-label={`Gia hạn ${c.name}`} type="date" value={c.expiresAt.slice(0, 10)} onChange={e => setCustomers(customers.map(x => x.id === c.id ? { ...x, expiresAt: e.target.value } : x))} /></label><button disabled={busy} onClick={() => run(async () => { await accountApi(`customers/${c.id}`, "PUT", { enabled: c.enabled, expiresAt: new Date(c.expiresAt.slice(0, 10) + "T23:59:59+07:00").toISOString() }); await loadCustomers(); })}>Lưu hạn</button><button disabled={busy} className="ra-secondary" onClick={() => run(async () => { await accountApi(`customers/${c.id}`, "PUT", { enabled: !c.enabled, expiresAt: c.expiresAt.length === 10 ? new Date(c.expiresAt + "T23:59:59+07:00").toISOString() : c.expiresAt }); await loadCustomers(); })}>{c.enabled ? "Khóa" : "Mở khóa"}</button></>}</div>)}</section></>}
    {account.isOwner && customers.some(c => !c.legacy) && <form className="ra-card" onSubmit={e => { e.preventDefault(); void run(async () => { await accountApi(`customers/${resetCustomer}/password`, "PUT", { password: resetPassword }); setResetPassword(""); }, "Đã đặt lại mật khẩu khách. Các phiên đăng nhập cũ đã bị thu hồi."); }}><h2>Đặt lại mật khẩu khách</h2><div className="ra-customer-fields"><label>Khách thuê<select required value={resetCustomer} onChange={e => setResetCustomer(e.target.value)}><option value="">Chọn khách thuê</option>{customers.filter(c => !c.legacy).map(c => <option key={c.id} value={c.id}>{c.name} ({c.userName})</option>)}</select></label><label>Mật khẩu mới cho khách<input required type="password" minLength={8} maxLength={128} autoComplete="off" value={resetPassword} onChange={e => setResetPassword(e.target.value)} /></label></div><button disabled={busy || !resetCustomer}>Đặt lại mật khẩu</button></form>}
  </div>;
}
