"use client";
import React, { useState } from "react";
import { Button, Modal, Space, Tag, Typography, Upload, message } from "antd";
import {
  DeleteOutlined,
  EyeOutlined,
  UploadOutlined,
} from "@ant-design/icons";
import type { RcFile, UploadProps } from "antd/es/upload";
import fileServerService from "@/libs/file-uploader/fileServer.service";
import { TaiLieuDinhKemType } from "@/types/taiLieuDinhKem/dto";

const { Text } = Typography;

const DEFAULT_ACCEPT = ".jpg,.jpeg,.png,.pdf,.doc,.docx,.xls,.xlsx";

export interface SingleFileUploaderProps {
  // ===== Controlled file value =====
  value?: TaiLieuDinhKemType | null;
  onChange?: (file: TaiLieuDinhKemType | null) => void;

  // ===== Upload context (chuyển thẳng vào fileServerService.uploadFiles) =====
  category: string; // 1 trong FileCategoryConstant
  subCategory?: string;
  taxCode?: string;
  itemId?: string;
  serialNumber?: string;
  includeExportInfo?: boolean;
  requiredKySo?: boolean;

  // ===== UI overrides =====
  accept?: string; // mặc định: .jpg,.jpeg,.png,.pdf,.doc,.docx,.xls,.xlsx
  maxMB?: number; // mặc định 50
  uploadLabel?: string; // text nút upload
  readOnly?: boolean; // disable upload + delete
  size?: "small" | "middle" | "large";

  // ===== Behavior =====
  // Có gọi API delete khi user xác nhận xoá không (mặc định true).
  // Set false nếu muốn chỉ clear khỏi state (file vẫn nằm trên server, xoá ở bước save form).
  deleteOnConfirm?: boolean;

  onUploadSuccess?: (file: TaiLieuDinhKemType) => void;
  onUploadError?: (err: unknown) => void;
  onDeleteSuccess?: () => void;
}

// Component upload 1 file (single) với 3 trạng thái:
//   - Chưa có → nút Upload
//   - Đang upload → loading
//   - Có file → Tag ext + tên + size + nút Xem / Xoá (kèm modal xác nhận)
//
// Sử dụng fileServerService.uploadFiles() đã chuẩn hoá theo spec lưu tài liệu.
const SingleFileUploader: React.FC<SingleFileUploaderProps> = ({
  value,
  onChange,
  category,
  subCategory,
  taxCode,
  itemId,
  serialNumber,
  includeExportInfo,
  requiredKySo,
  accept = DEFAULT_ACCEPT,
  maxMB = 50,
  uploadLabel = "Tải lên tài liệu",
  readOnly = false,
  size = "middle",
  deleteOnConfirm = true,
  onUploadSuccess,
  onUploadError,
  onDeleteSuccess,
}) => {
  const [uploading, setUploading] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const file = value ?? null;

  const setFile = (f: TaiLieuDinhKemType | null) => onChange?.(f);

  const uploadProps: UploadProps = {
    name: "file",
    multiple: false,
    showUploadList: false,
    accept,
    beforeUpload: (f) => {
      if (maxMB > 0 && f.size > maxMB * 1024 * 1024) {
        message.error(`File vượt quá ${maxMB}MB`);
        return Upload.LIST_IGNORE;
      }
      return true;
    },
    customRequest: async ({ file: rawFile, onSuccess, onError }) => {
      const rcFile = rawFile as RcFile;
      setUploading(true);
      try {
        const resp = await fileServerService.uploadFiles([rcFile as File], {
          category,
          subCategory,
          taxCode,
          itemId,
          serialNumber,
          requiredKySo,
          includeExportInfo,
        });
        if (resp?.data && resp.data.length > 0) {
          const uploaded = resp.data[0];
          setFile(uploaded);
          onUploadSuccess?.(uploaded);
          message.success("Upload thành công");
          onSuccess?.(uploaded);
        } else {
          throw new Error(resp?.message || "Upload thất bại");
        }
      } catch (err: any) {
        const msg =
          typeof err === "string"
            ? err
            : err?.message || err?.title || "Upload thất bại";
        message.error(msg);
        console.error("Upload error:", err);
        onUploadError?.(err);
        onError?.(err);
      } finally {
        setUploading(false);
      }
    },
  };

  const handlePreview = () => {
    if (!file) return;
    window.open(file.duongDanFile, "_blank", "noopener,noreferrer");
  };

  const handleConfirmDelete = async () => {
    if (!file) return;
    try {
      if (deleteOnConfirm) {
        const resp = await fileServerService.delete([file.id]);
        if (!resp?.status) {
          message.error(resp?.message || "Xoá thất bại");
          return;
        }
        message.success("Xoá file thành công");
      }
      setFile(null);
      onDeleteSuccess?.();
    } catch (err: any) {
      message.error(err?.message || "Xoá thất bại");
    } finally {
      setConfirmDelete(false);
    }
  };

  // ===== Render =====
  if (!file) {
    return (
      <Upload {...uploadProps} disabled={readOnly}>
        <Button
          type="primary"
          icon={<UploadOutlined />}
          size={size}
          loading={uploading}
          disabled={readOnly}
        >
          {uploading ? "Đang upload..." : uploadLabel}
        </Button>
      </Upload>
    );
  }

  return (
    <Space direction="vertical" size="small" style={{ width: "100%" }}>
      <div
        style={{
          padding: "10px 14px",
          border: "1px solid #f0f0f0",
          borderRadius: 8,
          background: "#fafafa",
        }}
      >
        <Space size={8} wrap>
          <Tag color="blue">{file.extension?.toUpperCase()}</Tag>
          <Text strong>{file.tenTaiLieu}</Text>
          {typeof file.kichThuoc === "number" && (
            <Text type="secondary" style={{ fontSize: 12 }}>
              ({Math.round(file.kichThuoc)} KB)
            </Text>
          )}
        </Space>
      </div>

      <Space>
        <Button
          type="primary"
          icon={<EyeOutlined />}
          size={size}
          onClick={handlePreview}
        >
          Xem file
        </Button>
        {!readOnly && (
          <Button
            danger
            icon={<DeleteOutlined />}
            size={size}
            onClick={() => setConfirmDelete(true)}
          >
            Xoá file
          </Button>
        )}
      </Space>

      <Modal
        title="Xác nhận xoá file"
        open={confirmDelete}
        onOk={handleConfirmDelete}
        onCancel={() => setConfirmDelete(false)}
        okText="Xoá"
        cancelText="Huỷ"
        okButtonProps={{ danger: true }}
      >
        <p>
          Bạn có chắc chắn muốn xoá file <strong>{file.tenTaiLieu}</strong>{" "}
          không? Hành động này không thể hoàn tác.
        </p>
      </Modal>
    </Space>
  );
};

export default SingleFileUploader;
