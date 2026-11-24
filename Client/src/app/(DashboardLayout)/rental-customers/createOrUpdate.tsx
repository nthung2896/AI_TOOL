"use client";
import { useEffect } from "react";
import { Form, Input, Modal, Switch } from "antd";
import type { RentalCustomer, RentalCustomerInput } from "@/services/rentalCustomer/rentalCustomer.service";

export default function CreateOrUpdate({ item, busy, onClose, onSave }: {
  item: RentalCustomer | null; busy: boolean; onClose: () => void; onSave: (input: RentalCustomerInput) => Promise<void>;
}) {
  const [form] = Form.useForm();
  useEffect(() => { form.setFieldsValue(item ? { ...item, expiresAt: item.expiresAt.slice(0, 10), password: undefined } : {
    name: "", userName: "", email: "", enabled: true, expiresAt: new Date(Date.now() + 30 * 86400000).toISOString().slice(0, 10), password: "",
  }); }, [item, form]);
  return <Modal open title={item ? "Cập nhật khách thuê" : "Thêm tài khoản khách thuê"} onCancel={onClose} confirmLoading={busy} okText="Lưu" cancelText="Hủy" onOk={() => form.submit()} destroyOnHidden>
    <Form form={form} layout="vertical" onFinish={async values => onSave({ ...values, id: item?.id, expiresAt: new Date(values.expiresAt + "T23:59:59+07:00").toISOString() })}>
      <Form.Item name="name" label="Tên khách" rules={[{ required: true, whitespace: true }, { max: 150 }]}><Input maxLength={150} /></Form.Item>
      <Form.Item name="userName" label="Tên đăng nhập" rules={[{ required: true, whitespace: true }, { max: 100 }]}><Input autoComplete="off" maxLength={100} /></Form.Item>
      <Form.Item name="email" label="Email" rules={[{ required: true }, { type: "email" }]}><Input type="email" maxLength={256} /></Form.Item>
      {!item && <Form.Item name="password" label="Mật khẩu ban đầu" rules={[{ required: true }, { min: 8, max: 128 }]}><Input.Password autoComplete="new-password" /></Form.Item>}
      <Form.Item name="expiresAt" label="Sử dụng đến ngày" rules={[{ required: true }]}><Input type="date" /></Form.Item>
      <Form.Item name="enabled" label="Cho phép sử dụng" valuePropName="checked"><Switch /></Form.Item>
    </Form>
  </Modal>;
}
