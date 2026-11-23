"use client";
import { useMemo, useState } from "react";
import {
  Alert,
  Button,
  Card,
  Col,
  Collapse,
  Divider,
  Form,
  Input,
  InputNumber,
  Row,
  Select,
  Space,
  Tag,
  Tooltip,
  Typography,
  message,
} from "antd";
import {
  AppstoreOutlined,
  BankOutlined,
  BookOutlined,
  BulbOutlined,
  CloudDownloadOutlined,
  CodeOutlined,
  CopyOutlined,
  FileProtectOutlined,
  PictureOutlined,
  ToolOutlined,
} from "@ant-design/icons";
import withAuthorization from "@/libs/authentication";
import AutoBreadcrumb from "@/components/util-compenents/Breadcrumb";
import FileCategoryConstant from "@/constants/FileCategoryConstant";
import { TaiLieuDinhKemType } from "@/types/taiLieuDinhKem/dto";
import SingleFileUploader from "@/components/upload-file/SingleFileUploader";

const { Title, Text, Paragraph } = Typography;

const PAGE_MAX_WIDTH = 1100;
const EMPTY_GUID = "00000000-0000-0000-0000-000000000000";

const codeBlockLight: React.CSSProperties = {
  background: "#f5f5f5",
  padding: 12,
  borderRadius: 6,
  fontSize: 13,
  marginTop: 8,
  whiteSpace: "pre-wrap",
  fontFamily: "Consolas, 'Courier New', monospace",
};

const codeBlockDark: React.CSSProperties = {
  background: "#1e1e1e",
  color: "#d4d4d4",
  padding: 16,
  borderRadius: 8,
  fontSize: 13,
  lineHeight: 1.6,
  fontFamily: "Consolas, 'Courier New', monospace",
  maxHeight: 480,
  overflow: "auto",
};

const cardTitle = (icon: React.ReactNode, title: string) => (
  <Space size={8}>
    {icon}
    <span>{title}</span>
  </Space>
);

const sectionGap: React.CSSProperties = { marginTop: 16 };

const UploadTestPage: React.FC = () => {
  return (
    <div style={{ maxWidth: PAGE_MAX_WIDTH, margin: "0 auto" }}>
      <div style={{ marginBottom: 12 }}>
        <AutoBreadcrumb items={[{ title: "Test upload file" }]} />
      </div>

      {/* ====== Playground ====== */}
      <PlaygroundCard />

      {/* ====== Cách lấy file đã upload để hiển thị ====== */}
      <RetrieveFileCard />

      {/* ====== Hướng dẫn sử dụng ====== */}
      <Card
        className="customCardShadow"
        style={sectionGap}
        title={cardTitle(<BookOutlined />, "Hướng dẫn sử dụng")}
      >
        <Alert
          type="info"
          showIcon
          message="Mục đích"
          description="Trang này test luồng upload — preview — xoá file qua Hinet.FileServer (port 5005), dùng API đã refactor theo spec lưu tài liệu của hệ thống AI Tool."
          style={{ marginBottom: 20 }}
        />

        <Title level={5}>1. Thao tác cơ bản</Title>
        <ul style={{ paddingLeft: 20, lineHeight: 1.9 }}>
          <li>
            <strong>Chưa có file</strong>: bấm{" "}
            <Tag color="blue">Tải lên tài liệu</Tag> → chọn file từ máy → tự
            upload.
          </li>
          <li>
            <strong>Đã có file</strong>: hiển thị box info + 2 nút —{" "}
            <Tag color="blue">Xem file</Tag> mở tab mới preview,{" "}
            <Tag color="red">Xoá file</Tag> bật modal xác nhận.
          </li>
          <li>
            <strong>Xoá xong</strong> → quay về trạng thái "Chưa có file", có
            thể upload mới.
          </li>
        </ul>

        <Divider />

        <Title level={5}>2. Component tái sử dụng</Title>
        <Paragraph>
          Toàn bộ logic upload — preview — xoá đã đóng gói trong{" "}
          <code>SingleFileUploader</code> (controlled component). Dùng ở bất kỳ
          page nào:
        </Paragraph>
        <pre style={codeBlockLight}>
          {`import SingleFileUploader from "@/components/upload-file/SingleFileUploader";
import FileCategoryConstant from "@/constants/FileCategoryConstant";

const [file, setFile] = useState(null);

<SingleFileUploader
  value={file}
  onChange={setFile}
  category={FileCategoryConstant.Platform}
  subCategory="NTThongBaoKD"
  taxCode="0100123456"
  itemId={platformId}
  maxMB={20}
  uploadLabel="Tải file đính kèm"
/>`}
        </pre>

        <Divider />

        <Title level={5}>3. Props chính của SingleFileUploader</Title>
        <ul style={{ paddingLeft: 20, lineHeight: 1.9 }}>
          <li>
            <code>value / onChange</code> — controlled file
            (TaiLieuDinhKemType | null)
          </li>
          <li>
            <code>category</code> (required) — 1 trong{" "}
            <code>FileCategoryConstant</code>
          </li>
          <li>
            <code>subCategory, taxCode, itemId</code> — context theo spec
          </li>
          <li>
            <code>accept</code> — extension whitelist (default
            jpg/png/pdf/doc/xls)
          </li>
          <li>
            <code>maxMB</code> — giới hạn FE (default 50)
          </li>
          <li>
            <code>readOnly</code> — disable upload + delete (view-only)
          </li>
          <li>
            <code>deleteOnConfirm</code> — true: gọi API delete khi xác nhận;
            false: chỉ clear state (file vẫn nằm server, xoá lúc save form).
          </li>
          <li>
            <code>onUploadSuccess / onDeleteSuccess</code> — callback tuỳ chọn
          </li>
        </ul>

        <Divider />

        <Title level={5}>4. Mapping context theo nghiệp vụ</Title>
        <Collapse
          style={{ marginTop: 12 }}
          items={[
            {
              key: "platform",
              label: (
                <Space>
                  <AppstoreOutlined />
                  <span>Nền tảng TMĐT (Platform)</span>
                </Space>
              ),
              children: (
                <pre style={codeBlockLight}>
                  {`<SingleFileUploader
  value={file} onChange={setFile}
  category={FileCategoryConstant.Platform}
  subCategory="NTThongBaoKD"
  taxCode="0100123456"
  itemId={platformId}
/>
// → wwwroot/uploads/platform/2026/05/20/0100123456/NTThongBaoKD/<platformId>/<file>`}
                </pre>
              ),
            },
            {
              key: "contract",
              label: (
                <Space>
                  <FileProtectOutlined />
                  <span>Chứng thực HĐĐT (Contract)</span>
                </Space>
              ),
              children: (
                <pre style={codeBlockLight}>
                  {`<SingleFileUploader
  value={file} onChange={setFile}
  category={FileCategoryConstant.Contract}
  taxCode="0100123456"
  itemId={contractId}
/>
// → wwwroot/uploads/contract/2026/05/20/0100123456/AuthenticationContract/<contractId>/<file>`}
                </pre>
              ),
            },
            {
              key: "company",
              label: (
                <Space>
                  <BankOutlined />
                  <span>Hồ sơ doanh nghiệp (Company)</span>
                </Space>
              ),
              children: (
                <pre style={codeBlockLight}>
                  {`<SingleFileUploader
  value={file} onChange={setFile}
  category={FileCategoryConstant.Company}
  subCategory="DKKD"
  taxCode="0100123456"
/>
// → wwwroot/uploads/company/2026/05/20/0100123456/DKKD/<file>`}
                </pre>
              ),
            },
            {
              key: "avatar",
              label: (
                <Space>
                  <PictureOutlined />
                  <span>Avatar người dùng (Avatar)</span>
                </Space>
              ),
              children: (
                <pre style={codeBlockLight}>
                  {`<SingleFileUploader
  value={file} onChange={setFile}
  category={FileCategoryConstant.Avatar}
  itemId={userId}
  accept=".jpg,.jpeg,.png"
  maxMB={5}
  uploadLabel="Đổi avatar"
/>
// → wwwroot/uploads/avatar/2026/05/20/<userId>/<file>`}
                </pre>
              ),
            },
          ]}
        />

        <Divider />

        <Title level={5}>5. Ràng buộc & lỗi BE có thể trả</Title>
        <ul style={{ paddingLeft: 20, lineHeight: 1.9 }}>
          <li>
            Sai <code>Category</code> → <em>"Category không hợp lệ..."</em>
          </li>
          <li>
            Thiếu <code>TaxCode</code> với Platform/Contract/Company →{" "}
            <em>"TaxCode là bắt buộc"</em>
          </li>
          <li>
            TaxCode chứa ký tự lạ (<code>/ \ .. *</code>) → reject regex
          </li>
          <li>
            Platform/Contract không có <code>ItemId</code> GUID hợp lệ → reject
          </li>
          <li>
            File vượt <code>FileServer.MaxFileSizeMB</code> trong appsettings →
            reject (default 50MB)
          </li>
        </ul>
      </Card>
    </div>
  );
};

export default withAuthorization(UploadTestPage, "");

// =====================================================================
// ===== Playground: chỉnh params live → sinh JSX → copy 1 click ======
// =====================================================================

type FieldRule = { show: boolean; required?: boolean; options?: string[] };

const CATEGORY_RULES: Record<
  string,
  {
    subCategory: FieldRule;
    taxCode: FieldRule;
    itemId: FieldRule;
    itemIdLabel?: string;
  }
> = {
  platform: {
    subCategory: {
      show: true,
      required: true,
      options: [
        "NTThongBaoKD",
        "NTDangKyKDNuocNgoai",
        "NTTichHop",
        "NTTichHopNuocNgoai",
      ],
    },
    taxCode: { show: true, required: true },
    itemId: { show: true, required: true },
    itemIdLabel: "itemId (GUID nền tảng)",
  },
  contract: {
    subCategory: { show: false },
    taxCode: { show: true, required: true },
    itemId: { show: true, required: true },
    itemIdLabel: "itemId (GUID hợp đồng)",
  },
  company: {
    subCategory: {
      show: true,
      required: false,
      options: ["DKKD", "UyQuyen", "KhacDangKy"],
    },
    taxCode: { show: true, required: true },
    itemId: { show: false },
  },
  avatar: {
    subCategory: { show: false },
    taxCode: { show: false },
    itemId: { show: true, required: true },
    itemIdLabel: "itemId (userId)",
  },
  general: {
    subCategory: { show: true, required: false },
    taxCode: { show: false },
    itemId: { show: false },
  },
};

const requiredMark = (label: string, required?: boolean) =>
  required ? (
    <>
      {label} <span style={{ color: "#ff4d4f" }}>*</span>
    </>
  ) : (
    label
  );

const PlaygroundCard: React.FC = () => {
  const [playFile, setPlayFile] = useState<TaiLieuDinhKemType | null>(null);
  const [category, setCategory] = useState<string>(FileCategoryConstant.General);
  const [subCategory, setSubCategory] = useState<string>("");
  const [taxCode, setTaxCode] = useState<string>("");
  const [itemId, setItemId] = useState<string>(EMPTY_GUID);
  const [accept, setAccept] = useState<string>(
    ".jpg,.jpeg,.png,.pdf,.doc,.docx,.xls,.xlsx",
  );
  const [maxMB, setMaxMB] = useState<number>(50);
  const [uploadLabel, setUploadLabel] = useState<string>("Tải lên tài liệu");
  const [readOnly, setReadOnly] = useState<boolean>(false);
  const [deleteOnConfirm, setDeleteOnConfirm] = useState<boolean>(true);

  const rule = CATEGORY_RULES[category] ?? CATEGORY_RULES.general;

  const generatedCode = useMemo(() => {
    const lines: string[] = [];
    lines.push("<SingleFileUploader");
    lines.push(`  value={file}`);
    lines.push(`  onChange={setFile}`);

    const constName =
      category === "platform"
        ? "Platform"
        : category === "contract"
          ? "Contract"
          : category === "company"
            ? "Company"
            : category === "avatar"
              ? "Avatar"
              : "General";
    lines.push(`  category={FileCategoryConstant.${constName}}`);

    if (subCategory) lines.push(`  subCategory="${subCategory}"`);
    if (taxCode) lines.push(`  taxCode="${taxCode}"`);
    if (itemId) lines.push(`  itemId="${itemId}"`);
    if (accept && accept !== ".jpg,.jpeg,.png,.pdf,.doc,.docx,.xls,.xlsx")
      lines.push(`  accept="${accept}"`);
    if (maxMB !== 50) lines.push(`  maxMB={${maxMB}}`);
    if (uploadLabel && uploadLabel !== "Tải lên tài liệu")
      lines.push(`  uploadLabel="${uploadLabel}"`);
    if (readOnly) lines.push(`  readOnly`);
    if (!deleteOnConfirm) lines.push(`  deleteOnConfirm={false}`);

    lines.push("/>");

    const imports = [
      `import SingleFileUploader from "@/components/upload-file/SingleFileUploader";`,
      `import FileCategoryConstant from "@/constants/FileCategoryConstant";`,
    ].join("\n");

    return `${imports}\n\nconst [file, setFile] = useState(null);\n\n${lines.join("\n")}`;
  }, [
    category,
    subCategory,
    taxCode,
    itemId,
    accept,
    maxMB,
    uploadLabel,
    readOnly,
    deleteOnConfirm,
  ]);

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(generatedCode);
      message.success("Đã copy snippet vào clipboard");
    } catch {
      message.error("Trình duyệt không cho phép copy");
    }
  };

  return (
    <Card
      className="customCardShadow"
      title={cardTitle(
        <ToolOutlined />,
        "Playground — tự cấu hình & copy snippet",
      )}
    >
      <Row gutter={[24, 24]}>
        {/* === Cột trái: form chỉnh params + preview component live === */}
        <Col xs={24} lg={12}>
          <Alert
            type="info"
            showIcon
            style={{ marginBottom: 16 }}
            message={
              <Space size={4} wrap>
                <span>
                  Rule cho <code>{category}</code>:
                </span>
                {rule.subCategory.show && (
                  <Tag color={rule.subCategory.required ? "red" : "default"}>
                    subCategory{" "}
                    {rule.subCategory.required ? "required" : "optional"}
                  </Tag>
                )}
                {rule.taxCode.show && (
                  <Tag color={rule.taxCode.required ? "red" : "default"}>
                    taxCode {rule.taxCode.required ? "required" : "optional"}
                  </Tag>
                )}
                {rule.itemId.show && (
                  <Tag color={rule.itemId.required ? "red" : "default"}>
                    itemId {rule.itemId.required ? "required" : "optional"}
                  </Tag>
                )}
                {!rule.subCategory.show &&
                  !rule.taxCode.show &&
                  !rule.itemId.show && <Tag>không cần field nào thêm</Tag>}
              </Space>
            }
          />

          <Form layout="vertical">
            <Form.Item label={requiredMark("category", true)}>
              <Select
                value={category}
                onChange={(v) => {
                  setCategory(v);
                  setSubCategory("");
                  const next =
                    CATEGORY_RULES[v] ?? CATEGORY_RULES.general;
                  if (!next.taxCode.show) setTaxCode("");
                  // Khi field itemId được hiện trở lại, reset về Guid.Empty cho dễ test.
                  if (!next.itemId.show) setItemId("");
                  else if (!itemId) setItemId(EMPTY_GUID);
                }}
                options={Object.keys(FileCategoryConstant.data).map((k) => ({
                  label: `${k} — ${FileCategoryConstant.getDisplayName(k)}`,
                  value: k,
                }))}
              />
            </Form.Item>

            {rule.subCategory.show && (
              <Form.Item
                label={requiredMark(
                  `subCategory ${rule.subCategory.options?.length ? "(whitelist BE)" : "(free)"}`,
                  rule.subCategory.required,
                )}
              >
                {rule.subCategory.options?.length ? (
                  <Select
                    value={subCategory || undefined}
                    onChange={setSubCategory}
                    allowClear
                    options={rule.subCategory.options.map((s) => ({
                      label: s,
                      value: s,
                    }))}
                    placeholder="Chọn subCategory"
                  />
                ) : (
                  <Input
                    value={subCategory}
                    onChange={(e) => setSubCategory(e.target.value)}
                    placeholder="Tuỳ chọn"
                    allowClear
                  />
                )}
              </Form.Item>
            )}

            {(rule.taxCode.show || rule.itemId.show) && (
              <Row gutter={12}>
                {rule.taxCode.show && (
                  <Col span={rule.itemId.show ? 12 : 24}>
                    <Form.Item
                      label={requiredMark("taxCode", rule.taxCode.required)}
                    >
                      <Input
                        value={taxCode}
                        onChange={(e) => setTaxCode(e.target.value)}
                        placeholder="0100123456"
                        allowClear
                      />
                    </Form.Item>
                  </Col>
                )}
                {rule.itemId.show && (
                  <Col span={rule.taxCode.show ? 12 : 24}>
                    <Form.Item
                      label={requiredMark(
                        rule.itemIdLabel ?? "itemId (GUID)",
                        rule.itemId.required,
                      )}
                    >
                      <Input
                        value={itemId}
                        onChange={(e) => setItemId(e.target.value)}
                        placeholder="xxxxxxxx-xxxx-..."
                        allowClear
                      />
                    </Form.Item>
                  </Col>
                )}
              </Row>
            )}

            <Form.Item label="accept">
              <Input
                value={accept}
                onChange={(e) => setAccept(e.target.value)}
                placeholder=".jpg,.png,..."
              />
            </Form.Item>

            <Row gutter={12}>
              <Col span={8}>
                <Form.Item label="maxMB">
                  <InputNumber
                    value={maxMB}
                    onChange={(v) => setMaxMB(Number(v ?? 50))}
                    min={1}
                    max={500}
                    style={{ width: "100%" }}
                  />
                </Form.Item>
              </Col>
              <Col span={16}>
                <Form.Item label="uploadLabel">
                  <Input
                    value={uploadLabel}
                    onChange={(e) => setUploadLabel(e.target.value)}
                  />
                </Form.Item>
              </Col>
            </Row>

            <Form.Item label="Tuỳ chọn boolean" style={{ marginBottom: 0 }}>
              <Space>
                <Tag
                  color={readOnly ? "blue" : "default"}
                  style={{ cursor: "pointer", padding: "4px 10px" }}
                  onClick={() => setReadOnly(!readOnly)}
                >
                  readOnly: {String(readOnly)}
                </Tag>
                <Tag
                  color={deleteOnConfirm ? "blue" : "default"}
                  style={{ cursor: "pointer", padding: "4px 10px" }}
                  onClick={() => setDeleteOnConfirm(!deleteOnConfirm)}
                >
                  deleteOnConfirm: {String(deleteOnConfirm)}
                </Tag>
              </Space>
            </Form.Item>
          </Form>

          <Divider>Preview component</Divider>

          <div
            style={{
              padding: 16,
              border: "1px dashed #d9d9d9",
              borderRadius: 8,
              minHeight: 80,
              background: "#fcfcfc",
            }}
          >
            <SingleFileUploader
              value={playFile}
              onChange={setPlayFile}
              category={category}
              subCategory={subCategory || undefined}
              taxCode={taxCode || undefined}
              itemId={itemId || undefined}
              accept={accept}
              maxMB={maxMB}
              uploadLabel={uploadLabel}
              readOnly={readOnly}
              deleteOnConfirm={deleteOnConfirm}
            />
          </div>
        </Col>

        {/* === Cột phải: code snippet live + nút copy === */}
        <Col xs={24} lg={12}>
          <div
            style={{
              display: "flex",
              justifyContent: "space-between",
              alignItems: "center",
              marginBottom: 12,
            }}
          >
            <Space>
              <CodeOutlined />
              <Text strong>Snippet sinh ra (sửa form → tự cập nhật)</Text>
            </Space>
            <Tooltip title="Copy vào clipboard">
              <Button
                type="primary"
                icon={<CopyOutlined />}
                onClick={handleCopy}
              >
                Copy
              </Button>
            </Tooltip>
          </div>
          <pre style={codeBlockDark}>{generatedCode}</pre>
          <Space size={6} style={{ marginTop: 8 }}>
            <BulbOutlined style={{ color: "#faad14" }} />
            <Text type="secondary" style={{ fontSize: 12 }}>
              Chỉ những props khác default mới hiện trong snippet để code gọn.
            </Text>
          </Space>
        </Col>
      </Row>
    </Card>
  );
};

// =====================================================================
// ===== RetrieveFileCard: hướng dẫn lấy file sau khi upload ===========
// =====================================================================

const SNIPPET_GET_URL = `import fileServerService from "@/libs/file-uploader/fileServer.service";

// 1 dòng. Render trực tiếp:
<Avatar src={await fileServerService.getUrlByItemId(userId)}>
  {hoTen?.[0]}
</Avatar>`;

const SNIPPET_AVATAR_HOOK = `import { useEffect, useState } from "react";
import { Avatar } from "antd";
import fileServerService from "@/libs/file-uploader/fileServer.service";

function UserAvatar({ userId, hoTen }: { userId: string; hoTen?: string }) {
  const [url, setUrl] = useState<string | null>(null);

  useEffect(() => {
    if (!userId) return;
    fileServerService.getUrlByItemId(userId).then(setUrl);
  }, [userId]);

  return <Avatar src={url ?? undefined}>{hoTen?.[0]}</Avatar>;
}`;

const SNIPPET_LATEST = `import fileServerService from "@/libs/file-uploader/fileServer.service";

// Lấy nguyên TaiLieuDinhKemType (cần id, tenTaiLieu, kichThuoc, extension...):
const tl = await fileServerService.getLatestByItemId(userId);
if (tl) {
  const url = fileServerService.getUrl(tl);
  // ... hiển thị tl.tenTaiLieu, tl.kichThuoc, link url
}`;

const SNIPPET_FILTER = `// Khi 1 itemId có thể chứa nhiều loại file (vd. user có cả avatar lẫn CCCD)
// → truyền loaiTaiLieu để lọc trước khi pick bản ghi mới nhất.
const avatarUrl = await fileServerService.getUrlByItemId(userId, "avatar");
const cccdUrl   = await fileServerService.getUrlByItemId(userId, "cccd");`;

const CopyableSnippet: React.FC<{ code: string; label?: string }> = ({
  code,
  label,
}) => {
  const onCopy = async () => {
    try {
      await navigator.clipboard.writeText(code);
      message.success(label ? `Đã copy: ${label}` : "Đã copy");
    } catch {
      message.error("Trình duyệt không cho phép copy");
    }
  };
  return (
    <div style={{ position: "relative", marginTop: 8 }}>
      <Button
        size="small"
        type="primary"
        icon={<CopyOutlined />}
        onClick={onCopy}
        style={{ position: "absolute", top: 8, right: 8, zIndex: 1 }}
      >
        Copy
      </Button>
      <pre style={codeBlockDark}>{code}</pre>
    </div>
  );
};

const RetrieveFileCard: React.FC = () => {
  return (
    <Card
      className="customCardShadow"
      style={sectionGap}
      title={cardTitle(
        <CloudDownloadOutlined />,
        "Lấy file đã upload để hiển thị (avatar / preview)",
      )}
    >
      <Alert
        type="info"
        showIcon
        message="Cơ chế"
        description={
          <span>
            File upload với <code>itemId = {`<userId>`}</code> sẽ được lưu vào
            bảng <code>TaiLieuDinhKem</code> với cột <code>ItemId</code> tương
            ứng. Khi cần render avatar / preview, truy ngược lại bằng helper
            mới <code>fileServerService.getUrlByItemId(itemId)</code> — internal
            gọi <code>/api/fileserver/getByItemId/{`{itemId}`}</code>, pick bản
            ghi mới nhất, build URL theo middleware{" "}
            <code>/fileserver/{`{id}`}/{`{tenTaiLieu}`}</code>.
          </span>
        }
        style={{ marginBottom: 20 }}
      />

      <Title level={5}>1. Cách nhanh nhất — 1 dòng lấy URL</Title>
      <Paragraph type="secondary" style={{ marginBottom: 0 }}>
        Dùng khi chỉ cần URL để gắn vào <code>{`<img>`}</code>,{" "}
        <code>{`<Avatar src>`}</code>, hoặc link <code>{`<a href>`}</code>.
      </Paragraph>
      <CopyableSnippet code={SNIPPET_GET_URL} label="getUrlByItemId" />

      <Divider />

      <Title level={5}>
        2. Pattern thực tế cho Avatar — component tái dùng
      </Title>
      <Paragraph type="secondary" style={{ marginBottom: 0 }}>
        Wrap trong <code>useEffect</code> để fetch async khi mount + refresh
        khi <code>userId</code> đổi.
      </Paragraph>
      <CopyableSnippet code={SNIPPET_AVATAR_HOOK} label="UserAvatar hook" />

      <Divider />

      <Title level={5}>3. Khi cần cả metadata (size, ext, ngày tạo…)</Title>
      <Paragraph type="secondary" style={{ marginBottom: 0 }}>
        Dùng <code>getLatestByItemId()</code> — trả nguyên{" "}
        <code>TaiLieuDinhKemType</code>.
      </Paragraph>
      <CopyableSnippet code={SNIPPET_LATEST} label="getLatestByItemId" />

      <Divider />

      <Title level={5}>4. Khi 1 itemId chứa nhiều loại file</Title>
      <Paragraph type="secondary" style={{ marginBottom: 0 }}>
        Truyền thêm <code>loaiTaiLieu</code> để lọc trước khi pick bản ghi mới
        nhất.
      </Paragraph>
      <CopyableSnippet code={SNIPPET_FILTER} label="filter by loaiTaiLieu" />

      <Divider />

      <Title level={5}>5. So sánh các API</Title>
      <ul style={{ paddingLeft: 20, lineHeight: 1.9 }}>
        <li>
          <code>getByItemId(itemId)</code> — trả{" "}
          <strong>toàn bộ list</strong> <code>TaiLieuDinhKemType[]</code> (đầy
          đủ metadata). Dùng khi cần hiển thị danh sách file đính kèm.
        </li>
        <li>
          <code>getLatestByItemId(itemId, loaiTaiLieu?)</code> —{" "}
          <strong>1 bản ghi mới nhất</strong> (sort theo{" "}
          <code>createdDate</code>). Dùng khi chỉ quan tâm file gần nhất, vẫn
          cần metadata.
        </li>
        <li>
          <code>getUrlByItemId(itemId, loaiTaiLieu?)</code> —{" "}
          <strong>chỉ string URL</strong>. Dùng cho Avatar / image preview
          đơn giản, không cần state phức tạp.
        </li>
      </ul>

      <Alert
        type="warning"
        showIcon
        style={{ marginTop: 12 }}
        message="Lưu ý hiệu năng"
        description={
          <span>
            Mỗi lần component mount mà gọi <code>getUrlByItemId</code> sẽ tốn 1
            request. Với list dài (table user), nên load 1 lần lúc fetch list
            (BE join sẵn <code>avatarUrl</code>), hoặc cache vào Redux. Helper
            này tối ưu cho profile page / header user duy nhất.
          </span>
        }
      />
    </Card>
  );
};
