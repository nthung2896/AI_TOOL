"use client";
import { useCallback, useEffect, useState } from "react";
import { ClockCircleOutlined, FacebookOutlined, OrderedListOutlined, ReloadOutlined, SettingOutlined } from "@ant-design/icons";

type QueueItem = { deliveryId: string; postId: string; groupId: string; roomName: string; groupName: string; groupUrl: string; postPreview: string; createdAt: string };
type Queue = { serverNow: string; intervalSeconds: number; paused: boolean; enabled: boolean; browserEnabled: boolean; lastStartedAt: string | null; nextAllowedAt: string | null; queuedCount: number; eligibleCount: number; sending: QueueItem | null; upcoming: QueueItem[]; activity: { running: boolean; task: string; since: string } };
type Props = { api: (path: string, method?: string, body?: unknown) => Promise<any>; onNextDelivery?: (id: string) => void };
const tasks: Record<string,string> = { idle: "Sẵn sàng nhận lượt", posting: "Đang xử lý lượt đăng", inbox: "Đang kiểm tra Messenger", metrics: "Đang đọc tương tác Facebook", postLinks: "Đang tìm link bài đã gửi", groupSearch: "Đang tìm nhóm Facebook", messengerSetup: "Đang thiết lập Messenger", telegram: "Đang kiểm tra lệnh Telegram", telegramNotices: "Đang gửi thông báo Telegram" };
const duration = (seconds: number) => seconds % 60 === 0 ? `${seconds / 60} phút` : seconds > 60 ? `${Math.floor(seconds / 60)} phút ${seconds % 60} giây` : `${seconds} giây`;
const clock = (seconds: number) => { const hours = Math.floor(seconds / 3600); const mins = Math.floor(seconds / 60) % 60; return `${hours ? `${String(hours).padStart(2,"0")}:` : ""}${String(mins).padStart(2,"0")}:${String(seconds % 60).padStart(2,"0")}`; };

export default function PostingQueueCard({ api, onNextDelivery }: Props) {
  const [queue, setQueue] = useState<Queue | null>(null);
  const [offset, setOffset] = useState(0);
  const [now, setNow] = useState(() => Date.now());
  const [value, setValue] = useState("5");
  const [unit, setUnit] = useState("minutes");
  const [dirty, setDirty] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const apply = useCallback((next: Queue) => {
    if (!next || typeof next.intervalSeconds !== "number" || !Array.isArray(next.upcoming) || !next.activity) throw new Error("Chưa tải được hàng đợi. Khởi động lại API đã cập nhật rồi thử lại.");
    setQueue(current => current && Date.parse(current.serverNow) > Date.parse(next.serverNow) ? current : next);
    setOffset(Date.parse(next.serverNow) - Date.now());
    onNextDelivery?.(next.upcoming[0]?.deliveryId || "");
  }, [onNextDelivery]);
  const refresh = useCallback(async () => { apply(await api("posting/queue")); setError(""); }, [api, apply]);
  useEffect(() => {
    void refresh().catch(e => setError(e.message));
    const poll = setInterval(() => { void refresh().catch(e => setError(e.message)); }, 5000);
    const timer = setInterval(() => setNow(Date.now()), 1000);
    return () => { clearInterval(poll); clearInterval(timer); };
  }, [refresh]);
  useEffect(() => {
    if (!queue || dirty) return;
    setUnit(queue.intervalSeconds % 60 === 0 ? "minutes" : "seconds");
    setValue(String(queue.intervalSeconds % 60 === 0 ? queue.intervalSeconds / 60 : queue.intervalSeconds));
  }, [queue?.intervalSeconds, dirty]);
  const seconds = Math.round(Number(value) * (unit === "minutes" ? 60 : 1));
  const remaining = queue?.nextAllowedAt ? Math.max(0, Math.ceil((Date.parse(queue.nextAllowedAt) - (now + offset)) / 1000)) : 0;
  const next = queue?.upcoming[0];
  const status = !queue ? "Đang tải hàng đợi" : !queue.enabled ? "Worker chưa được bật" : !queue.browserEnabled ? "Trình duyệt đang tắt" : queue.paused ? "Đang tạm dừng" : !queue.activity.running ? "Worker chưa chạy" : queue.sending ? "Đang gửi một lượt" : !next ? "Hàng đợi trống" : remaining > 0 ? "Đang chờ đến lượt" : queue.activity.task !== "idle" ? "Đủ thời gian · đang chờ worker" : "Sẵn sàng cho lượt tiếp theo";
  const inactive = !queue || !queue.enabled || !queue.browserEnabled || queue.paused || !queue.activity.running;
  return <section className="ra-posting-queue" aria-label="Hàng đợi và thời gian đăng bài">
    <div className="ra-queue-heading"><div><h2><ClockCircleOutlined aria-hidden /> Lịch gửi & hàng đợi</h2><p>Biết nhóm nào sắp nhận bài và còn chờ bao lâu.</p></div><button type="button" className="ra-link" onClick={() => void refresh().then(() => setError("")).catch(e => setError(e.message))}><ReloadOutlined aria-hidden /> Cập nhật</button></div>
    {error && <p className="ra-error" role="alert">{error}</p>}
    <div className="ra-queue-grid"><div className={`ra-countdown-panel ${inactive ? "is-paused" : ""}`}><span className="ra-queue-state">{status}</span><strong className="ra-queue-countdown" aria-label="Thời gian còn lại">{inactive || !next ? "—" : clock(remaining)}</strong><p>{queue?.sending ? next ? "Đang hoàn tất lượt hiện tại; thời gian dưới đây là mốc sớm nhất cho lượt kế tiếp." : "Đang hoàn tất lượt gửi cuối cùng trong hàng đợi." : next && !inactive ? remaining > 0 ? "đến mốc được phép bắt đầu lượt kế tiếp" : "Worker sẽ lấy lượt khi hoàn tất tác vụ hiện tại." : queue?.paused ? "Bật trợ lý để tiếp tục gửi các lượt đang chờ." : !next ? "Duyệt một bài đăng để thêm lượt gửi vào hàng đợi." : "Kiểm tra worker và trình duyệt để tiếp tục."}</p>{queue?.nextAllowedAt && next && <small>Mốc sớm nhất: {new Date(queue.nextAllowedAt).toLocaleTimeString("vi-VN", { hour:"2-digit",minute:"2-digit",second:"2-digit" })}</small>}<div className="ra-queue-activity">{queue && <><span className="ra-queue-dot" />{tasks[queue.activity.task] || queue.activity.task}</>}</div></div>
      <div className="ra-next-delivery"><span className="ra-queue-eyebrow">{queue?.sending ? "NHÓM KẾ TIẾP SAU LƯỢT HIỆN TẠI" : "NHÓM SẼ NHẬN BÀI TIẾP THEO"}</span>{next ? <><h3><FacebookOutlined aria-hidden />{next.groupName}</h3><p><b>Bài:</b> {next.roomName}</p>{next.postPreview && <blockquote>{next.postPreview}</blockquote>}<a href={next.groupUrl} target="_blank" rel="noreferrer">Mở nhóm Facebook</a><small>Đã xếp hàng: {new Date(next.createdAt).toLocaleString("vi-VN")}</small></> : <h3>Chưa có lượt đang chờ hợp lệ</h3>}{queue?.sending && <div className="ra-current-delivery"><span>Đang gửi tới</span><b>{queue.sending.groupName}</b><small>{queue.sending.roomName}</small></div>}</div>
      <form className="ra-posting-settings" onSubmit={e => { e.preventDefault(); setSaving(true); setError(""); setNotice(""); void api("posting/settings", "PUT", { intervalSeconds: seconds }).then(next => { apply(next); setDirty(false); setNotice(`Đã lưu khoảng cách ${duration(next.intervalSeconds)}. Hàng đợi áp dụng ngay.`); }).catch(e => setError(e.message)).finally(() => setSaving(false)); }}><h3><SettingOutlined aria-hidden /> Khoảng cách giữa các lượt</h3><p>Đang áp dụng: <b>{queue ? duration(queue.intervalSeconds) : "…"}</b></p><div className="ra-interval-input"><input aria-label="Khoảng cách đăng bài" required type="number" min={unit === "minutes" ? 1 / 60 : 1} max={unit === "minutes" ? 1440 : 86400} step="any" value={value} disabled={saving} onChange={e => { setValue(e.target.value); setDirty(true); }} /><select aria-label="Đơn vị thời gian đăng" value={unit} disabled={saving} onChange={e => { const previousSeconds = Number(value) * (unit === "minutes" ? 60 : 1); setUnit(e.target.value); setValue(String(e.target.value === "minutes" ? previousSeconds / 60 : previousSeconds)); setDirty(true); }}><option value="minutes">phút</option><option value="seconds">giây</option></select></div><div className="ra-interval-presets">{[30,60,180,300,600].map(preset => <button key={preset} type="button" disabled={saving} aria-pressed={seconds === preset} onClick={() => { setUnit(preset % 60 === 0 ? "minutes" : "seconds"); setValue(String(preset % 60 === 0 ? preset / 60 : preset)); setDirty(true); }}>{duration(preset)}</button>)}</div><button type="submit" disabled={saving || !dirty || !queue || seconds < 1 || seconds > 86400 || !Number.isFinite(seconds)}>{saving ? "Đang lưu…" : "Lưu thời gian"}</button><small>Lưu vào database, không mất khi khởi động lại. Giảm thời gian có thể cho phép lượt kế tiếp bắt đầu ngay.</small></form></div>
    {notice && <p className="ra-success" role="status">{notice}</p>}
    {queue && <div className="ra-queue-footer"><span><OrderedListOutlined aria-hidden /> <b>{queue.queuedCount}</b> lượt chờ · <b>{queue.eligibleCount}</b> hợp lệ</span>{queue.queuedCount > queue.eligibleCount && <span>Nhóm tắt hoặc phòng hết sẽ được bỏ qua.</span>}<span>Khoảng cách tính từ lúc bắt đầu lượt trước; worker gửi tuần tự, thời điểm có thể trễ hơn mốc đếm.</span></div>}
    {!!queue?.upcoming.length && <details className="ra-upcoming-deliveries"><summary>Xem {queue.upcoming.length} lượt kế tiếp theo thứ tự</summary><ol>{queue.upcoming.map(item => <li key={item.deliveryId} className={item.deliveryId === next?.deliveryId ? "is-next" : ""}><span>{item.groupName}</span><small>{item.roomName}</small>{item.deliveryId === next?.deliveryId && <b>Tiếp theo</b>}</li>)}</ol></details>}
  </section>;
}
