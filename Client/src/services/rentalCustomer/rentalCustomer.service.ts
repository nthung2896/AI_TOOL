import { accountBase } from "@/app/rental-assistant/CustomerSettings";

export type RentalCustomer = { id: string; name: string; userName: string; email: string; enabled: boolean; expiresAt: string; legacy: boolean; active: boolean };
export type RentalCustomerInput = Omit<RentalCustomer, "id" | "legacy" | "active"> & { id?: string; password?: string };
export type CustomerSearch = { keyword?: string; status?: string; pageIndex: number; pageSize: number };
export type CustomerPage = { items: RentalCustomer[]; totalCount: number; pageIndex: number; pageSize: number };
export type MenuEntry = { id: string; name: string; url: string; code: string; icon?: string; order: number };
export type Administration = { modules: { id: string; name: string; code: string; icon?: string; order: number; entries: MenuEntry[] }[]; entries: MenuEntry[]; customerActions: Record<string, boolean> };

async function request<T>(action: string, method = "GET", body?: unknown): Promise<T> {
  const token = sessionStorage.getItem("rental-access-token") || localStorage.getItem("AccessToken") || "";
  const response = await fetch(`${accountBase()}/api/RentalCustomer/${action}`, {
    method, cache: "no-store", credentials: "include", headers: { Authorization: `Bearer ${token}`, "Content-Type": "application/json" },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const payload = await response.json().catch(() => null);
  if (!response.ok || !payload?.status) throw new Error(payload?.message || (response.status === 403 ? "Bạn không có quyền thực hiện thao tác này." : "Không thực hiện được yêu cầu."));
  return payload.data as T;
}
const rentalCustomerService = {
  getData: (search: CustomerSearch) => request<CustomerPage>("GetData", "POST", search),
  get: (id: string) => request<RentalCustomer>(`Get/${id}`),
  create: (data: RentalCustomerInput) => request<RentalCustomer>("Create", "POST", data),
  update: (data: RentalCustomerInput) => request<RentalCustomer>("Update", "PUT", data),
  delete: (id: string) => request<void>(`Delete/${id}`, "DELETE"),
  resetPassword: (id: string, password: string) => request<void>(`ResetPassword/${id}`, "PUT", { password }),
};
export default rentalCustomerService;
