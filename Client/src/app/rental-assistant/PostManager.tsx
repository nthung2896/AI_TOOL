"use client";

import { useState } from "react";
import { CheckCircleOutlined, CopyOutlined, EditOutlined, FileTextOutlined, PictureOutlined, SearchOutlined, SendOutlined, ThunderboltOutlined } from "@ant-design/icons";
import { Room, RoomPhotos } from "./RoomManager";
import GroupPicker from "./GroupPicker";

type Post = { id: string; roomId: string; content: string; status: string; images: string[]; targetGroupIds?: string[]; hasGroupSelection?: boolean; createdAt?: string };
type Delivery = { id: string; postId: string; groupId: string; status: string; detail: string; postUrl?: string };
type Group = { id: string; name: string; enabled: boolean; category?: string };
type Props = {
  rooms: Room[]; posts: Post[]; deliveries: Delivery[]; groups: Group[]; busy: boolean;
  draftRoom: string; setDraftRoom: (id: string) => void; instruction: string; setInstruction: (text: string) => void;
  api: (path: string, method?: string, body?: unknown) => Promise<any>;
  action: (work: () => Promise<unknown>) => Promise<void>;
};
const statuses: Record<string, string> = { Draft: "Bản nháp", Approved: "Đã duyệt", Queued: "Chờ gửi", Sending: "Đang gửi", Submitted: "Đã gửi · chờ kiểm tra", NeedsReview: "Cần kiểm tra", Cancelled: "Đã hủy", Skipped: "Đã bỏ qua" };
const money = (value: number) => new Intl.NumberFormat("vi-VN").format(value) + " đ/tháng";
const badge = (status: string) => <span className={`ra-post-status ra-post-status-${status}`}>{statuses[status] || status}</span>;

export default function PostManager({ rooms, posts, deliveries, groups, busy, draftRoom, setDraftRoom, instruction, setInstruction, api, action }: Props) {
  const [filter, setFilter] = useState("all");
  const [query, setQuery] = useState("");
  const [editing, setEditing] = useState<Post | null>(null);
  const [expanded, setExpanded] = useState<string[]>([]);
  const [notice, setNotice] = useState("");
  const [targetOverride, setTargetOverride] = useState<string[] | null>(null);
  const [postTargets, setPostTargets] = useState<Record<string, string[]>>({});
  const defaultTargets = groups.filter(g => g.enabled).map(g => g.id);
  const selectedTargets = (targetOverride ?? defaultTargets).filter(id => defaultTargets.includes(id));
  const targetsFor = (p: Post) => (postTargets[p.id] ?? (p.hasGroupSelection ? p.targetGroupIds || [] : defaultTargets)).filter(id => defaultTargets.includes(id));
  const available = rooms.filter(r => r.available);
  const selected = available.find(r => r.id === draftRoom);
  const enabledGroups = selectedTargets.length;
  const roomName = (id: string) => rooms.find(r => r.id === id)?.name || "Phòng không còn trong danh sách";
  const postDeliveries = (id: string) => deliveries.filter(d => d.postId === id);
  const matches = (p: Post, value: string) => value === "all" || (value === "draft" ? p.status === "Draft" : postDeliveries(p.id).some(d => value === "queued" ? ["Queued", "Sending"].includes(d.status) : value === "sent" ? d.status === "Submitted" : d.status === "NeedsReview"));
  const filters = [["all", "Tất cả"], ["draft", "Bản nháp"], ["queued", "Chờ gửi"], ["sent", "Đã gửi"], ["review", "Cần kiểm tra"]];
  const normalized = (value: string) => value.normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/đ/gi, "d").toLocaleLowerCase("vi");
  const visible = posts.filter(p => matches(p, filter) && normalized(roomName(p.roomId) + " " + p.content).includes(normalized(query.trim())));
  async function copy(content: string) {
    try { await navigator.clipboard.writeText(content); setNotice("Đã sao chép nội dung bài đăng."); }
    catch { setNotice("Chưa sao chép được. Bạn có thể chọn và sao chép trực tiếp nội dung bài."); }
  }
  return <div className="ra-posts-workspace">
    <section className="ra-post-composer">
      <div className="ra-composer-main">
        <div className="ra-composer-heading"><span className="ra-composer-icon"><ThunderboltOutlined aria-hidden="true" /></span><div><h2>Soạn bài cho thuê</h2><p>Từ thông tin phòng thật đến bài đăng sẵn sàng chia sẻ.</p></div><span className="ra-composer-tag">Trợ lý viết bài</span></div>
        <form onSubmit={e => { e.preventDefault(); void action(async () => { await api("posts", "POST", { roomId: draftRoom, instruction, groupIds: selectedTargets }); setFilter("draft"); setQuery(""); setNotice("Đã tạo bản nháp. Kiểm tra nội dung và ảnh trước khi duyệt đăng."); }); }}>
          <label>Phòng cho thuê<select aria-label="Phòng cho thuê" required disabled={busy || !available.length} value={draftRoom} onChange={e => setDraftRoom(e.target.value)}><option value="">Chọn phòng cần viết bài</option>{available.map(r => <option key={r.id} value={r.id}>{r.name}</option>)}</select></label>
          <label>Bạn muốn nhấn mạnh điều gì?<textarea aria-label="Yêu cầu viết bài" required rows={2} maxLength={2000} disabled={busy} placeholder="Ví dụ: Nhấn mạnh nội thất đầy đủ, gần trường và có thể chuyển vào ngay…" value={instruction} onChange={e => setInstruction(e.target.value)} /></label>
          <GroupPicker groups={groups} selected={selectedTargets} onChange={setTargetOverride} disabled={busy} />
          <div className="ra-composer-bottom"><span><PictureOutlined aria-hidden="true" /> Ảnh đã lưu của phòng sẽ đi kèm bản nháp</span><button disabled={busy || !selected || !instruction.trim()}><ThunderboltOutlined aria-hidden="true" /> {busy ? "Đang xử lý…" : "Tạo bản nháp"}</button></div>
        </form>
        {!available.length && <p className="ra-composer-warning">Chưa có phòng còn trống. <a href="#rooms">Thêm hoặc cập nhật phòng</a> để bắt đầu.</p>}
      </div>
      <aside className="ra-composer-room"><span className="ra-eyebrow">PHÒNG ĐƯỢC CHỌN</span>{selected ? <><h3>{selected.name}</h3><strong>{money(selected.monthlyRent)}</strong><p>{selected.address}</p><div className="ra-composer-room-meta"><span><PictureOutlined aria-hidden="true" /> {selected.listing?.images?.length || 0} ảnh</span>{!!selected.listing?.area && <span>{selected.listing.area} m²</span>}</div><a href="#rooms">Quản lý thông tin phòng</a></> : <><div className="ra-room-placeholder"><FileTextOutlined aria-hidden="true" /></div><h3>Bắt đầu từ một căn phòng</h3><p>Chọn phòng để xem thông tin và tạo nội dung phù hợp.</p></>}<div className="ra-composer-group"><SendOutlined aria-hidden="true" /><span><b>{enabledGroups} nhóm</b> đang được chọn để nhận bài</span></div></aside>
    </section>

    <div className="ra-post-metrics">{[["Bản nháp", posts.filter(p => p.status === "Draft").length, "draft"], ["Lượt chờ gửi", deliveries.filter(d => ["Queued", "Sending"].includes(d.status)).length, "queued"], ["Lượt đã gửi", deliveries.filter(d => d.status === "Submitted").length, "sent"], ["Lượt cần kiểm tra", deliveries.filter(d => d.status === "NeedsReview").length, "review"]].map(([label, count, kind]) => <button key={kind} className={`ra-post-metric ra-post-metric-${kind}`} onClick={() => setFilter(String(kind))}><span>{label}</span><strong>{count}</strong></button>)}</div>
    {notice && <div className="ra-post-notice" role="status"><CheckCircleOutlined aria-hidden="true" /><span>{notice}</span><button type="button" aria-label="Đóng thông báo" onClick={() => setNotice("")}>×</button></div>}
    <section aria-label="Danh sách bài đăng">
      <div className="ra-post-list-heading"><div><h2>Thư viện bài đăng <span>{posts.length}</span></h2><p>Kiểm tra nội dung, duyệt bài và theo dõi từng lượt gửi.</p></div><label className="ra-post-search"><SearchOutlined aria-hidden="true" /><input aria-label="Tìm bài đăng" type="search" placeholder="Tìm theo phòng, nội dung…" value={query} onChange={e => setQuery(e.target.value)} /></label></div>
      <div className="ra-post-filters" role="group" aria-label="Lọc bài đăng">{filters.map(([value, label]) => <button key={value} aria-pressed={filter === value} onClick={() => setFilter(value)}>{label}<span>{posts.filter(p => matches(p, value)).length}</span></button>)}</div>
      <div className="ra-post-grid">{visible.map(p => {
        const rows = postDeliveries(p.id);
        const targetIds = targetsFor(p);
        const sent = rows.filter(d => d.status === "Submitted").length;
        const review = rows.filter(d => d.status === "NeedsReview").length;
        const isExpanded = expanded.includes(p.id);
        const isEditing = editing?.id === p.id;
        const created = p.createdAt && !Number.isNaN(Date.parse(p.createdAt)) ? new Date(p.createdAt).toLocaleString("vi-VN", { day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit" }) : "";
        return <article className="ra-post-card" key={p.id}>
          <header className="ra-post-card-header"><span className="ra-post-avatar"><FileTextOutlined aria-hidden="true" /></span><div><h3>{roomName(p.roomId)}</h3><small>{created || "Bài đăng cho thuê phòng"}{!!p.images?.length && ` · ${p.images.length} ảnh`}</small></div>{badge(p.status)}</header>
          <div className="ra-post-card-body">{isEditing ? <label>Nội dung bản nháp<textarea aria-label="Nội dung bản nháp" rows={10} maxLength={10000} value={editing.content} onChange={e => setEditing({ ...editing, content: e.target.value })} /></label> : <><p className={`ra-post-content ${isExpanded ? "is-expanded" : ""}`}>{p.content}</p>{(p.content.length > 350 || p.content.split("\n").length > 8) && <button className="ra-link ra-expand-post" onClick={() => setExpanded(ids => isExpanded ? ids.filter(id => id !== p.id) : [...ids, p.id])}>{isExpanded ? "Thu gọn nội dung" : "Xem toàn bộ nội dung"}</button>}</>}<RoomPhotos images={p.images || []} name={roomName(p.roomId)} /></div>
          {p.status === "Draft" && <div className="ra-card-group-picker"><GroupPicker groups={groups} selected={targetIds} onChange={ids => setPostTargets(values => ({ ...values, [p.id]: ids }))} disabled={busy} /></div>}
          <div className="ra-post-card-actions">{isEditing ? <><button disabled={busy || !editing.content.trim()} onClick={() => action(async () => { await api(`posts/${p.id}`, "PUT", { content: editing.content }); setEditing(null); setNotice("Đã lưu nội dung bản nháp."); })}>Lưu nội dung</button><button className="ra-secondary" disabled={busy} onClick={() => setEditing(null)}>Hủy sửa</button></> : <><button className="ra-post-copy" onClick={() => void copy(p.content)}><CopyOutlined aria-hidden="true" /> Sao chép</button>{p.status === "Draft" && <><button className="ra-post-edit" disabled={busy} onClick={() => setEditing({ ...p })}><EditOutlined aria-hidden="true" /> Sửa bài</button><button className="ra-post-approve" disabled={busy || !targetIds.length} onClick={() => action(() => api(`posts/${p.id}/approve`, "POST", { groupIds: targetIds }))}><SendOutlined aria-hidden="true" /> Duyệt đăng · {targetIds.length} nhóm</button></>}</>}</div>
          {p.status === "Draft" && !targetIds.length && <p className="ra-post-help"><a href="#groups">Chọn nhóm Facebook</a> để duyệt đăng bài này.</p>}
          {!!rows.length && <details className="ra-post-deliveries" open={review > 0}><summary><span><SendOutlined aria-hidden="true" /> Lượt gửi nhóm <b>{rows.length}</b></span><span className={review ? "ra-delivery-review" : ""}>{review ? `${review} cần kiểm tra` : sent ? `${sent}/${rows.length} đã gửi` : "Đang xếp hàng"}</span></summary><div>{rows.map(d => <div className="ra-post-delivery-row" key={d.id}><div><strong>{groups.find(g => g.id === d.groupId)?.name || "Nhóm không còn trong danh sách"}</strong>{badge(d.status)}</div>{d.detail && <p>{d.detail}</p>}{d.postUrl && <p><a href={d.postUrl} target="_blank" rel="noreferrer">M? b?i Facebook</a></p>}{d.status === "Queued" && <button className="ra-link" disabled={busy} onClick={() => action(() => api(`deliveries/${d.id}/cancel`, "POST"))}>Hủy lượt gửi</button>}</div>)}<p className="ra-post-help">Đã gửi là đã thao tác gửi bài; nhóm có thể đang chờ quản trị viên duyệt.</p></div></details>}
          <details className="ra-post-reference"><summary>Mã bài đăng</summary><code>{p.id}</code></details>
        </article>;
      })}</div>
      {!visible.length && <div className="ra-post-empty"><FileTextOutlined aria-hidden="true" /><h3>{posts.length ? "Chưa có bài phù hợp" : "Bài đăng đầu tiên bắt đầu từ đây"}</h3><p>{posts.length ? "Thử đổi bộ lọc hoặc từ khóa tìm kiếm." : "Chọn phòng ở trên và tạo bản nháp. Nội dung và ảnh sẽ xuất hiện tại đây."}</p>{!!posts.length && <button className="ra-secondary" onClick={() => { setFilter("all"); setQuery(""); }}>Xem tất cả bài đăng</button>}</div>}
    </section>
  </div>;
}
