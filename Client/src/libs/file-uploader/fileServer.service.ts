import { apiService } from "@/services";
import { ApiResponse } from "@/types/general";
import { FileValidationType } from "@/types/pdf/dto";
import { TaiLieuDinhKemType } from "@/types/taiLieuDinhKem/dto";
import axios, { AxiosInstance, AxiosResponse } from "axios";

class FileServerService {
  private static _instance: FileServerService;
  public static get instance(): FileServerService {
    if (!FileServerService._instance) {
      FileServerService._instance = new FileServerService();
    }
    return FileServerService._instance;
  }
  private api: AxiosInstance;

  private constructor() {
    this.api = axios.create({
      baseURL: process.env.NEXT_PUBLIC_FILE_SERVER_URL,
      timeout: 0, // Không giới hạn thời gian chờ
    });

    this.api.interceptors.request.use((config) => {
      config.headers["x-client-area"] = window.__APP_AREA || "dashboard";
      return config;
    });

    this.api.interceptors.request.use(
      (config) => {
        const token = localStorage.getItem("AccessToken");
        if (token) {
          config.headers.Authorization = `Bearer ${token}`;
        }

        return config;
      },
      (error) => {
        return Promise.reject(error);
      },
    );

    this.api.interceptors.response.use(
      (response) => response,
      (error) => {
        const area = window.__APP_AREA || "dashboard";
        if (area == "dashboard" && error.response?.status === 401) {
          localStorage.removeItem("AccessToken");
          window.location.href = "/auth/login";
        }
        if (error.response?.status === 403) {
          console.warn("FileServer 403:", error.config?.url);
          return Promise.reject(error.response?.data?.title || "Forbidden");
        }
        // Trả về Error object để giữ stack + để caller đọc err.message ổn định.
        const data = error.response?.data;
        const msg =
          (typeof data === "string" && data) ||
          data?.title ||
          data?.message ||
          data?.detail ||
          error.message ||
          `HTTP ${error.response?.status ?? "error"}`;
        console.warn("FileServer error:", msg, error.response);
        return Promise.reject(new Error(msg));
      },
    );
  }

  public async upload(
    formData: FormData,
  ): Promise<ApiResponse<TaiLieuDinhKemType[]>> {
    try {
      const response: AxiosResponse<ApiResponse<TaiLieuDinhKemType[]>> =
        await this.api.post(`/api/fileserver/upload`, formData);
      return response.data;
    } catch (error) {
      throw error;
    }
  }

  // Helper upload có structured context, khớp với backend FilePathBuilder.
  // Dùng thay cho upload(formData) trực tiếp để đảm bảo sinh đúng đường dẫn theo spec
  // /upload/năm/tháng/ngày/MST/loại/itemId/file.
  public async uploadFiles(
    files: File[],
    opts: {
      category: string;        // 1 trong FileCategoryConstant
      subCategory?: string;    // PlatformManageType / DKKD / ...
      taxCode?: string;        // MST DN chủ quản (bắt buộc với platform/contract/company)
      itemId?: string;         // platformId / contractId / userId
      serialNumber?: string;
      requiredKySo?: boolean;
      includeExportInfo?: boolean;
    },
  ): Promise<ApiResponse<TaiLieuDinhKemType[]>> {
    const fd = new FormData();
    files.forEach((f) => fd.append("Files", f));
    fd.append("Category", opts.category);
    if (opts.subCategory) fd.append("SubCategory", opts.subCategory);
    if (opts.taxCode) fd.append("TaxCode", opts.taxCode);
    if (opts.itemId) fd.append("ItemId", opts.itemId);
    if (opts.serialNumber) fd.append("SerialNumber", opts.serialNumber);
    if (opts.requiredKySo) fd.append("RequiredKySo", "true");
    if (opts.includeExportInfo) fd.append("IncludeExportInfo", "true");
    return this.upload(fd);
  }

  public async get(id: string): Promise<ApiResponse<TaiLieuDinhKemType>> {
    try {
      const response: AxiosResponse<ApiResponse<TaiLieuDinhKemType>> =
        await this.api.get(`/api/fileserver/${id}`);
      return response.data;
    } catch (error) {
      throw error;
    }
  }
  public async getByItemId(
    itemId: string,
  ): Promise<ApiResponse<TaiLieuDinhKemType[]>> {
    try {
      const response: AxiosResponse<ApiResponse<TaiLieuDinhKemType[]>> =
        await this.api.get(`/api/fileserver/getByItemId/${itemId}`);

      return response.data;
    } catch (error) {
      throw error;
    }
  }

  // Lấy bản ghi mới nhất theo itemId (vd. avatar mới nhất của user).
  // Trả null nếu không có. Lọc thêm theo loaiTaiLieu nếu cần phân biệt nhiều
  // loại file cùng gắn 1 itemId.
  public async getLatestByItemId(
    itemId: string,
    loaiTaiLieu?: string,
  ): Promise<TaiLieuDinhKemType | null> {
    const res = await this.getByItemId(itemId);
    let list = res.data ?? [];
    if (loaiTaiLieu) list = list.filter((t) => t.loaiTaiLieu === loaiTaiLieu);
    if (!list.length) return null;
    return [...list].sort((a, b) => {
      const av = a.createdDate ? new Date(a.createdDate).getTime() : 0;
      const bv = b.createdDate ? new Date(b.createdDate).getTime() : 0;
      return bv - av;
    })[0];
  }

  // Helper cực gọn cho UI cần URL hiển thị (vd. <Avatar src={...}>).
  // Tránh phải dispatch + state nguyên cả TaiLieuDinhKemType chỉ để render 1 ảnh.
  public async getUrlByItemId(
    itemId: string,
    loaiTaiLieu?: string,
  ): Promise<string | null> {
    const t = await this.getLatestByItemId(itemId, loaiTaiLieu);
    return t ? this.getUrl(t) : null;
  }

  public async delete(ids: string[]): Promise<ApiResponse> {
    try {
      const response: AxiosResponse<ApiResponse> = await this.api.post(
        `/api/fileserver/delete`,
        { ids },
      );
      return response.data;
    } catch (error) {
      throw error;
    }
  }

  public getUrl(taiLieu: TaiLieuDinhKemType): string;
  public getUrl(id: string, fileName: string): string;
  public getUrl(param1: any, param2?: any): string {
    console.log(param1, param2);
    if (typeof param1 === "object" && param1.id && param1.tenTaiLieu) {
      return `${process.env.NEXT_PUBLIC_FILE_SERVER_URL}/fileserver/${param1.id}/${param1.tenTaiLieu}`;
    }
    if (typeof param1 === "string" && typeof param2 === "string") {
      return `${process.env.NEXT_PUBLIC_FILE_SERVER_URL}/fileserver/${param1}/${param2}`;
    }

    throw new Error("Invalid arguments");
  }
}

const fileServerService = FileServerService.instance;
export default fileServerService;
