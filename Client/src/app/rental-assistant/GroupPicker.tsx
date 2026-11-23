"use client";
import { RentalGroup, groupCategories } from "./GroupManager";
type Props = { groups: Pick<RentalGroup, "id" | "name" | "enabled" | "category">[]; selected: string[]; onChange: (ids: string[]) => void; disabled?: boolean };
export default function GroupPicker({ groups, selected, onChange, disabled = false }: Props) {
  const available = groups.filter(g => g.enabled);
  const ids = selected.filter(id => available.some(g => g.id === id));
  return <fieldset className="ra-post-group-picker" disabled={disabled}><legend>Nhóm nhận bài <span>{ids.length} đã chọn</span></legend><div className="ra-post-group-tools"><button type="button" className="ra-link" onClick={() => onChange(available.map(g => g.id))}>Chọn tất cả</button><button type="button" className="ra-link" onClick={() => onChange([])}>Bỏ chọn</button><a href="#groups">Quản lý nhóm</a></div>{available.length ? <div className="ra-post-group-options">{available.map(g => <label key={g.id}><input type="checkbox" checked={ids.includes(g.id)} onChange={() => onChange(ids.includes(g.id) ? ids.filter(id => id !== g.id) : [...ids, g.id])} /><span>{g.name}<small>{groupCategories.find(c => c.id === g.category)?.name || "Khác"}</small></span></label>)}</div> : <p>Chưa có nhóm cho phép đăng. <a href="#groups">Tìm và chọn nhóm Facebook</a> để thêm vào danh sách.</p>}</fieldset>;
}
