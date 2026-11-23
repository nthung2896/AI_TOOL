"use client";

import { ChangeEvent, useState } from "react";

export type Listing = {
  area: number; maxOccupants: number; roomType: string; furniture: string; amenities: string;
  nearbyPlaces: string; electricity: string; water: string; otherFees: string; deposit: string;
  accessHours: string; availableFrom: string; viewingHours: string; contactName: string;
  contactPhones: string; hashtags: string; ownerDirect: boolean; images: string[];
};
export type Room = { id: string; name: string; address: string; monthlyRent: number; details: string; available: boolean; listing: Listing };
const emptyListing: Listing = { area: 0, maxOccupants: 0, roomType: "", furniture: "", amenities: "", nearbyPlaces: "", electricity: "", water: "", otherFees: "", deposit: "", accessHours: "", availableFrom: "", viewingHours: "", contactName: "", contactPhones: "", hashtags: "", ownerDirect: false, images: [] };
const blankRoom = { id: "", name: "", address: "", monthlyRent: 3000000, details: "", available: true, listing: emptyListing };
const money = (n: number) => new Intl.NumberFormat("vi-VN").format(n) + " đ";
export const rentalBase = () => (process.env.NEXT_PUBLIC_API_URL || "http://localhost:5111").replace(/\/$/, "");
export const imageUrl = (id: string) => `${rentalBase()}/rental-media/${encodeURIComponent(id)}`;

function listingText(room: typeof blankRoom) {
  const l = room.listing;
  const lines = [room.name ? `CHO THUÊ ${room.name.toUpperCase()}` : "", room.address ? `Địa chỉ: ${room.address}` : "", `Giá thuê: ${money(room.monthlyRent)}/tháng`];
  if (l.area) lines.push(`Diện tích: ${l.area} m²`);
  if (l.maxOccupants) lines.push(`Số người tối đa: ${l.maxOccupants}`);
  const fields: [keyof Listing, string][] = [["roomType", "Loại phòng"], ["nearbyPlaces", "Vị trí"], ["furniture", "Nội thất"], ["amenities", "Tiện ích"], ["electricity", "Điện"], ["water", "Nước"], ["otherFees", "Chi phí khác"], ["deposit", "Đặt cọc"], ["accessHours", "Giờ giấc"], ["availableFrom", "Nhận phòng"], ["viewingHours", "Giờ xem phòng"]];
  fields.forEach(([key, label]) => { if (l[key]) lines.push(`${label}: ${l[key]}`); });
  if (room.details) lines.push(room.details);
  if (l.ownerDirect) lines.push("Chính chủ, không qua môi giới.");
  if (l.contactName || l.contactPhones) lines.push(`Liên hệ: ${[l.contactName, l.contactPhones].filter(Boolean).join(" — ")}`);
  lines.push("Nhắn Messenger để hỏi thêm và đặt lịch xem phòng nhé!");
  if (l.hashtags) lines.push(l.hashtags);
  return lines.filter(Boolean).join("\n");
}

export function RoomPhotos({ images, name }: { images: string[]; name: string }) {
  if (!images?.length) return null;
  return <div className={`ra-photo-collage ra-photo-count-${Math.min(images.length, 5)}`}>
    {images.slice(0, 5).map((id, i) => <a key={id} href={imageUrl(id)} target="_blank" rel="noreferrer" aria-label={`Xem ảnh ${i + 1} của ${name}`}><img src={imageUrl(id)} alt={`${name || "Phòng trọ"} — ảnh ${i + 1}`} loading="lazy" />{i === 4 && images.length > 5 && <span>+{images.length - 5}</span>}</a>)}
  </div>;
}

type Props = {
  rooms: Room[]; busy: boolean;
  api: (path: string, method?: string, body?: unknown) => Promise<any>;
  action: (work: () => Promise<unknown>) => Promise<void>;
  onDraft: (id: string) => void;
};

export default function RoomManager({ rooms, busy, api, action, onDraft }: Props) {
  const [room, setRoom] = useState(blankRoom);
  const [uploading, setUploading] = useState(false);
  const [uploadError, setUploadError] = useState("");
  const [saved, setSaved] = useState("");
  const l = room.listing;
  const patch = (value: Partial<Listing>) => { setSaved(""); setRoom(r => ({ ...r, listing: { ...r.listing, ...value } })); };
  const edit = (r: Room) => { setRoom({ ...r, listing: { ...emptyListing, ...r.listing, images: [...(r.listing?.images || [])] } }); setSaved(""); setUploadError(""); window.scrollTo({ top: 0, behavior: "smooth" }); };
  const textField = (key: keyof Listing, label: string, placeholder: string, maxLength = 200) => <label key={key}>{label}<input maxLength={maxLength} value={String(l[key] || "")} placeholder={placeholder} onChange={e => patch({ [key]: e.target.value })} /></label>;
  async function upload(e: ChangeEvent<HTMLInputElement>) {
    const files = Array.from(e.target.files || []); e.target.value = "";
    if (!files.length) return;
    setUploadError(""); setSaved("");
    if (l.images.length + files.length > 10) { setUploadError("Mỗi phòng tối đa 10 ảnh. Hãy bỏ bớt ảnh trước khi tải thêm."); return; }
    if (files.some(f => f.size > 8 * 1024 * 1024 || !["image/jpeg", "image/png", "image/webp"].includes(f.type))) { setUploadError("Chọn ảnh JPG, PNG hoặc WebP, mỗi ảnh tối đa 8 MB."); return; }
    setUploading(true);
    try {
      const body = new FormData(); files.forEach(f => body.append("files", f));
      const images: string[] = await api("images", "POST", body);
      setRoom(r => ({ ...r, listing: { ...r.listing, images: [...r.listing.images, ...images] } }));
    } catch (error) { setUploadError(error instanceof Error ? error.message : "Không tải được ảnh. Vui lòng thử lại."); }
    finally { setUploading(false); }
  }
  return <div className="ra-room-workspace">
    <form className="ra-card ra-room-form" onSubmit={e => { e.preventDefault(); void action(async () => { await api(`rooms/${room.id || crypto.randomUUID()}`, "PUT", { ...room, id: room.id || undefined }); setRoom(blankRoom); setSaved("Đã lưu phòng và ảnh. Chọn Soạn bài để tạo bản nháp."); }); }}>
      <div className="ra-card-top"><div><h2>{room.id ? "Chỉnh sửa phòng trọ" : "Thêm phòng trọ"}</h2><p className="ra-muted">Thông tin và ảnh thật giúp bài đăng rõ ràng, dễ liên hệ.</p></div><span className="ra-badge">{room.id ? "Đang chỉnh sửa" : "Phòng mới"}</span></div>
      <fieldset disabled={busy || uploading}>
        <legend>Thông tin cơ bản</legend>
        <label>Tiêu đề / tên phòng *<input required maxLength={150} placeholder="Phòng đầy đủ nội thất — ngõ 1 Bùi Xương Trạch" value={room.name} onChange={e => setRoom({ ...room, name: e.target.value })} /></label>
        <label>Địa chỉ *<input required maxLength={400} placeholder="Số nhà, ngõ, đường, phường, quận, thành phố" value={room.address} onChange={e => setRoom({ ...room, address: e.target.value })} /></label>
        <div className="ra-form-grid">
          <label>Giá thuê / tháng (VNĐ) *<input required type="number" min={1} max={1000000000} value={room.monthlyRent} onChange={e => setRoom({ ...room, monthlyRent: Number(e.target.value) })} /></label>
          <label>Diện tích (m²)<input type="number" min={0} max={10000} step="0.1" value={l.area || ""} placeholder="30" onChange={e => patch({ area: Number(e.target.value) })} /></label>
          {textField("roomType", "Loại phòng", "Phòng khép kín, studio, có gác…")}
          <label>Số người tối đa<input type="number" min={0} max={100} value={l.maxOccupants || ""} placeholder="2" onChange={e => patch({ maxOccupants: Number(e.target.value) })} /></label>
        </div>
        {textField("nearbyPlaces", "Vị trí & địa điểm lân cận", "Cách Ngã Tư Sở 1 km, gần chợ, trường đại học…", 1000)}
      </fieldset>
      <fieldset disabled={busy || uploading}><legend>Ảnh phòng <span>{l.images.length}/10 ảnh</span></legend>
        <label className="ra-upload-zone"><span className="ra-upload-icon" aria-hidden="true">＋</span><strong>{uploading ? "Đang tải ảnh…" : "Chọn nhiều ảnh phòng"}</strong><span>JPG, PNG, WebP · tối đa 8 MB/ảnh · ảnh đầu tiên là ảnh bìa</span><input type="file" accept="image/jpeg,image/png,image/webp" multiple disabled={uploading || busy || l.images.length >= 10} onChange={upload} /></label>
        {!!l.images.length && <div className="ra-image-picker">{l.images.map((id, i) => <div key={id}><img src={imageUrl(id)} alt={`Ảnh phòng ${i + 1}`} /><span className="ra-image-number">{i === 0 ? "Ảnh bìa" : `Ảnh ${i + 1}`}</span><div><button type="button" className="ra-secondary" disabled={i === 0} onClick={() => patch({ images: [id, ...l.images.filter(x => x !== id)] })}>Làm bìa</button><button type="button" className="ra-remove-photo" onClick={() => patch({ images: l.images.filter(x => x !== id) })} aria-label={`Bỏ ảnh ${i + 1}`}>Bỏ ảnh</button></div></div>)}</div>}
        {uploadError && <p className="ra-error" role="alert">{uploadError}</p>}
      </fieldset>
      <fieldset disabled={busy || uploading}><legend>Nội thất & tiện ích</legend>
        <label>Nội thất có sẵn<textarea aria-label="Nội thất có sẵn" maxLength={1000} rows={2} placeholder="Giường, tủ, điều hòa, nóng lạnh, bếp từ, rèm cửa…" value={l.furniture} onChange={e => patch({ furniture: e.target.value })} /></label>
        <label>Tiện ích<textarea aria-label="Tiện ích" maxLength={1000} rows={2} placeholder="Chỗ để xe miễn phí, camera an ninh, thang máy, máy giặt…" value={l.amenities} onChange={e => patch({ amenities: e.target.value })} /></label>
      </fieldset>
      <fieldset disabled={busy || uploading}><legend>Chi phí & điều kiện thuê</legend><div className="ra-form-grid">
        {textField("electricity", "Tiền điện", "3.500 đ/kWh")}{textField("water", "Tiền nước", "100.000 đ/người/tháng")}
        {textField("deposit", "Tiền cọc", "1 tháng tiền thuê")}{textField("otherFees", "Phí dịch vụ khác", "Internet, vệ sinh, gửi xe…", 500)}
        {textField("accessHours", "Giờ giấc ra vào", "Tự do, có chìa khóa riêng", 300)}{textField("availableFrom", "Thời gian nhận phòng", "Có thể chuyển vào ở ngay")}
      </div>{textField("viewingHours", "Giờ xem phòng", "18h–21h hằng ngày, liên hệ trước", 300)}</fieldset>
      <fieldset disabled={busy || uploading}><legend>Liên hệ & nội dung bổ sung</legend><div className="ra-form-grid">
        {textField("contactName", "Người liên hệ", "Anh / chị…", 150)}{textField("contactPhones", "Số điện thoại / Zalo", "Có thể nhập nhiều số, cách nhau bằng dấu phẩy")}
      </div><label>Mô tả thêm<textarea aria-label="Mô tả thêm" maxLength={5000} rows={3} placeholder="Đặc điểm nổi bật, yêu cầu người thuê, lưu ý khác…" value={room.details} onChange={e => setRoom({ ...room, details: e.target.value })} /></label>
        {textField("hashtags", "Hashtag bài đăng", "#phongtro #ngatuso #thanhxuan", 500)}
        <div className="ra-room-checks"><label className="ra-check"><input type="checkbox" checked={l.ownerDirect} onChange={e => patch({ ownerDirect: e.target.checked })} />Chính chủ, không qua môi giới</label><label className="ra-check"><input type="checkbox" checked={room.available} onChange={e => setRoom({ ...room, available: e.target.checked })} />Còn phòng cho thuê</label></div>
      </fieldset>
      <div className="ra-actions"><button disabled={busy || uploading}>{uploading ? "Đang tải ảnh…" : room.id ? "Lưu thay đổi" : "Lưu phòng"}</button>{room.id && <button type="button" disabled={busy || uploading} className="ra-secondary" onClick={() => { setRoom(blankRoom); setUploadError(""); }}>Hủy sửa</button>}<span className="ra-muted">* Thông tin bắt buộc</span></div>
      {saved && <p className="ra-success" role="status">{saved}</p>}
    </form>
    <aside className="ra-room-aside">
      <article className="ra-card ra-post-preview"><div className="ra-card-top"><h2>Xem trước bài đăng</h2><span className="ra-badge">Bản mẫu</span></div><p className="ra-muted">Nội dung cập nhật theo thông tin bạn nhập.</p><div className="ra-preview-author"><span aria-hidden="true">{(l.contactName || "C").slice(0, 1).toUpperCase()}</span><div><strong>{l.contactName || "Chủ phòng"}</strong><small>Bài đăng cho thuê phòng</small></div></div><p className="ra-pre">{listingText(room)}</p><RoomPhotos images={l.images} name={room.name} />{!l.images.length && <div className="ra-photo-placeholder">Thêm ảnh để xem bố cục bài đăng</div>}</article>
      <div className="ra-card-top"><h2>Phòng đã lưu <span className="ra-muted">({rooms.length})</span></h2></div>
      {!rooms.length && <div className="ra-empty">Chưa có phòng. Điền thông tin và lưu phòng đầu tiên.</div>}
      {rooms.map(r => <article className="ra-card ra-saved-room" key={r.id}><div className="ra-card-top"><h2>{r.name}</h2><span className="ra-badge">{r.available ? "Còn trống" : "Đã hết"}</span></div><strong className="ra-price">{money(r.monthlyRent)}<small> / tháng{r.listing?.area ? ` · ${r.listing.area} m²` : ""}</small></strong><p>{r.address}</p>{r.listing?.contactPhones && <p>Liên hệ: {r.listing.contactPhones}</p>}<RoomPhotos images={r.listing?.images || []} name={r.name} /><div className="ra-actions"><button type="button" className="ra-secondary" disabled={busy || uploading} onClick={() => edit(r)}>Sửa thông tin</button><button type="button" disabled={busy || uploading || !r.available} onClick={() => onDraft(r.id)}>Soạn bài</button></div></article>)}
    </aside>
  </div>;
}
