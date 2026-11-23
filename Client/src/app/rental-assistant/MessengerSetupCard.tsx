"use client";
import { useCallback, useEffect, useState } from "react";
import { CheckCircleOutlined, MessageOutlined, ToolOutlined } from "@ant-design/icons";

type Status = { state: string; message: string; configured: boolean };
type Props = { threads: { id: string; customerName: string; url: string }[]; api: (path: string, method?: string, body?: unknown) => Promise<any> };
export default function MessengerSetupCard({ threads, api }: Props) {
  const [url, setUrl] = useState("");
  const [status, setStatus] = useState<Status | null>(null);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const running = status?.state === "Queued" || status?.state === "Running";
  const refresh = useCallback(async () => setStatus(await api("messenger/setup")), [api]);
  useEffect(() => { void refresh().catch(e => setError(e.message)); }, [refresh]);
  useEffect(() => {
    if (!running) return;
    const timer = setInterval(() => { void refresh().catch(e => setError(e.message)); }, 2000);
    return () => clearInterval(timer);
  }, [running, refresh]);
  return <section className="ra-messenger-setup ra-card">
    <div className="ra-card-top"><div className="ra-messenger-setup-title"><span><MessageOutlined aria-hidden /></span><div><h2>Thiết lập nhận tin Messenger</h2><p>Chọn hai tin mẫu để ứng dụng phân biệt tin khách và tin của bạn.</p></div></div><span className={`ra-badge ${status && !status.configured ? "ra-danger" : ""}`}>{status ? status.configured ? "Đã cấu hình" : "Chưa cấu hình" : "Đang kiểm tra…"}</span></div>
    {!status?.configured && <p className="ra-messenger-setup-help">Cảnh báo Telegram xuất hiện vì ứng dụng chưa biết nhận diện tin đến/tin đi. Thiết lập bên dưới không gửi tin nhắn cho khách.</p>}
    <form onSubmit={e => { e.preventDefault(); setError(""); setBusy(true); void api("messenger/setup", "POST", { url }).then(setStatus).catch(e => setError(e.message)).finally(() => setBusy(false)); }}>
      {!!threads.length && <label>Chọn hội thoại đã đồng bộ<select aria-label="Chọn hội thoại thiết lập" disabled={running || busy} value={threads.some(t => t.url === url) ? url : ""} onChange={e => setUrl(e.target.value)}><option value="">Chọn hội thoại hoặc dán link bên dưới</option>{threads.map(t => <option key={t.id} value={t.url}>{t.customerName || t.url}</option>)}</select></label>}
      <div className="ra-messenger-setup-form"><label>Link hội thoại cần thiết lập<input aria-label="Link hội thoại cần thiết lập" required type="url" maxLength={400} value={url} disabled={running || busy} onChange={e => setUrl(e.target.value)} placeholder="https://www.facebook.com/messages/t/123456" /></label><button disabled={busy || running}><ToolOutlined aria-hidden /> Mở thiết lập trong Chromium</button>{running && <button className="ra-secondary" type="button" disabled={busy} onClick={() => { setBusy(true); void api("messenger/setup/cancel", "POST").then(refresh).catch(e => setError(e.message)).finally(() => setBusy(false)); }}>Hủy thiết lập</button>}</div>
    </form>
    <ol className="ra-messenger-steps"><li>Mở hội thoại có sẵn tin của cả hai phía.</li><li>Trong Chromium, bấm nội dung một tin <b>khách gửi</b>, rồi một tin <b>bạn gửi</b>.</li><li>Ứng dụng tải lại hội thoại để kiểm tra ID và chiều tin; chỉ lưu khi kiểm tra đạt.</li></ol>
    {status?.message && <p role="status" className={status.state === "Completed" ? "ra-success" : "ra-messenger-setup-status"}>{status.state === "Completed" && <CheckCircleOutlined aria-hidden />} {status.message}</p>}
    {error && <p role="alert" className="ra-error">{error}</p>}
    <small className="ra-muted">Phiên thiết lập hết hạn sau 5 phút. Nếu Messenger yêu cầu PIN, hãy hủy phiên, mở khóa lịch sử trong Chromium rồi thiết lập lại. Lịch sử cũ được lấy làm mốc; bot chỉ báo những tin mới sau đó. Nếu Messenger không cung cấp ID ổn định, cấu hình sẽ không được lưu.</small>
  </section>;
}
