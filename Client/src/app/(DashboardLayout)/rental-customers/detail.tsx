"use client";
import { Descriptions, Modal, Tag } from "antd";
import type { RentalCustomer } from "@/services/rentalCustomer/rentalCustomer.service";
export default function Detail({ item, onClose }: { item: RentalCustomer; onClose: () => void }) {
  return <Modal open title="Chi tiết tài khoản khách thuê" onCancel={onClose} footer={null}><Descriptions column={1} bordered items={[
    { key: "name", label: "Tên khách", children: item.name },
    { key: "userName", label: "Tên đăng nhập", children: item.userName },
    { key: "email", label: "Email", children: item.email },
    { key: "expiresAt", label: "Hạn sử dụng", children: new Date(item.expiresAt).toLocaleString("vi-VN") },
    { key: "status", label: "Trạng thái", children: <Tag color={item.active ? "green" : "red"}>{item.legacy ? "Chủ hệ thống" : item.active ? "Đang hoạt động" : item.enabled ? "Đã hết hạn" : "Đã khóa"}</Tag> },
  ]} /></Modal>;
}
