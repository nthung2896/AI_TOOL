"use client";
import { useCallback, useEffect, useState } from "react";
import { Alert, Button, Card, Form, Input, Modal, Select, Space, Table, Tag, message } from "antd";
import { DeleteOutlined, EditOutlined, EyeOutlined, KeyOutlined, PlusOutlined, SearchOutlined } from "@ant-design/icons";
import service, { Administration, CustomerPage, RentalCustomer, RentalCustomerInput } from "@/services/rentalCustomer/rentalCustomer.service";
import { accountApi } from "@/app/rental-assistant/CustomerSettings";
import CreateOrUpdate from "./createOrUpdate";
import Detail from "./detail";

export default function RentalCustomersPage() {
  const [access, setAccess] = useState<Administration | null>(null);
  const [data, setData] = useState<CustomerPage>();
  const [search, setSearch] = useState({ keyword: "", status: "", pageIndex: 1, pageSize: 20 });
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [editing, setEditing] = useState<RentalCustomer | null | undefined>();
  const [detail, setDetail] = useState<RentalCustomer>();
  const [reset, setReset] = useState<RentalCustomer>();
  const [password, setPassword] = useState("");
  const [deleting, setDeleting] = useState<RentalCustomer>();
  const can = (action: string) => !!access?.customerActions[action] && !!access.entries.some(e => e.url === "/rental-customers");
  const load = useCallback(async () => {
    setBusy(true); setError("");
    try { setData(await service.getData(search)); } catch (e) { setError(e instanceof Error ? e.message : "Không tải được dữ liệu."); }
    finally { setBusy(false); }
  }, [search]);
  useEffect(() => { void accountApi("administration").then(setAccess).catch(e => setError(e.message)); }, []);
  useEffect(() => { if (access?.customerActions.GetData && access.entries.some(e => e.url === "/rental-customers")) void load(); }, [access, load]);
  async function run(action: () => Promise<unknown>, success: string) {
    setBusy(true); setError("");
    try { await action(); message.success(success); await load(); } catch (e) { setError(e instanceof Error ? e.message : "Không thực hiện được."); }
    finally { setBusy(false); }
  }
  async function save(input: RentalCustomerInput) {
    await run(async () => { if (editing) await service.update(input); else await service.create(input); setEditing(undefined); }, "Đã lưu khách thuê.");
  }
  if (access && !can("GetData")) return <Alert type="error" title="Bạn không có quyền truy cập chức năng quản lý khách thuê." />;
  return <Card title="Tài khoản & khách thuê" extra={can("Create") && <Button type="primary" icon={<PlusOutlined />} onClick={() => setEditing(null)}>Thêm mới</Button>}>
    {error && <Alert type="error" title={error} showIcon style={{ marginBottom: 16 }} />}
    <Form layout="inline" onFinish={values => setSearch({ ...search, keyword: values.keyword || "", status: values.status || "", pageIndex: 1 })} style={{ gap: 12, marginBottom: 20 }}>
      <Form.Item name="keyword"><Input allowClear placeholder="Tên khách, tên đăng nhập, email" style={{ width: 300 }} /></Form.Item>
      <Form.Item name="status"><Select allowClear placeholder="Trạng thái" style={{ width: 170 }} options={[{ value: "active", label: "Đang hoạt động" }, { value: "expired", label: "Đã hết hạn" }, { value: "locked", label: "Đã khóa" }]} /></Form.Item>
      <Button htmlType="submit" icon={<SearchOutlined />}>Tìm kiếm</Button>
      <Button onClick={() => void load()} disabled={busy}>Làm mới</Button>
    </Form>
    <Table<RentalCustomer> rowKey="id" loading={busy} dataSource={data?.items || []} scroll={{ x: 1100 }} pagination={{ current: search.pageIndex, pageSize: search.pageSize, total: data?.totalCount || 0, showSizeChanger: true, showTotal: total => `${total} tài khoản`, onChange: (pageIndex, pageSize) => setSearch({ ...search, pageIndex, pageSize }) }} columns={[
      { title: "STT", width: 65, render: (_, __, index) => (search.pageIndex - 1) * search.pageSize + index + 1 },
      { title: "Tên khách", dataIndex: "name" }, { title: "Tên đăng nhập", dataIndex: "userName" }, { title: "Email", dataIndex: "email" },
      { title: "Hạn sử dụng", render: (_, item) => new Date(item.expiresAt).toLocaleDateString("vi-VN") },
      { title: "Trạng thái", render: (_, item) => <Tag color={item.active ? "green" : "red"}>{item.legacy ? "Chủ hệ thống" : item.active ? "Đang hoạt động" : item.enabled ? "Đã hết hạn" : "Đã khóa"}</Tag> },
      { title: "Thao tác", fixed: "right", width: 270, render: (_, item) => <Space wrap>
        {can("Get") && <Button title="Chi tiết" icon={<EyeOutlined />} disabled={busy} onClick={() => void run(async () => setDetail(await service.get(item.id)), "Đã tải chi tiết.")} />}
        {!item.legacy && can("Update") && <Button title="Chỉnh sửa" icon={<EditOutlined />} disabled={busy} onClick={() => setEditing(item)} />}
        {!item.legacy && can("ResetPassword") && <Button title="Đặt lại mật khẩu" icon={<KeyOutlined />} disabled={busy} onClick={() => { setPassword(""); setReset(item); }} />}
        {!item.legacy && can("Delete") && <Button danger title="Xóa" icon={<DeleteOutlined />} disabled={busy} onClick={() => setDeleting(item)} />}
      </Space> },
    ]} />
    {editing !== undefined && <CreateOrUpdate item={editing} busy={busy} onClose={() => setEditing(undefined)} onSave={save} />}
    {detail && <Detail item={detail} onClose={() => setDetail(undefined)} />}
    <Modal open={!!deleting} title="Xóa tài khoản khách thuê" okText="Xóa" okButtonProps={{ danger: true }} cancelText="Hủy" confirmLoading={busy} onCancel={() => setDeleting(undefined)} onOk={() => void run(async () => { await service.delete(deleting!.id); setDeleting(undefined); }, "Đã xóa tài khoản.")}>
      <p>Xóa tài khoản <b>{deleting?.name}</b>? Khách sẽ không thể đăng nhập và trợ lý của khách sẽ dừng hoạt động.</p>
    </Modal>
    <Modal open={!!reset} title={`Đặt lại mật khẩu — ${reset?.name || ""}`} okText="Lưu mật khẩu" cancelText="Hủy" confirmLoading={busy} onCancel={() => setReset(undefined)} okButtonProps={{ disabled: password.length < 8 || password.length > 128 }} onOk={() => void run(async () => { await service.resetPassword(reset!.id, password); setReset(undefined); setPassword(""); }, "Đã đặt lại mật khẩu và thu hồi các phiên đăng nhập cũ.")}>
      <p>Mật khẩu có ít nhất 8 ký tự, gồm chữ hoa, chữ thường và số.</p><Input.Password autoComplete="new-password" value={password} maxLength={128} onChange={e => setPassword(e.target.value)} />
    </Modal>
  </Card>;
}
