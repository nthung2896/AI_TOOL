
import { SearchBase } from "@/types/general";
export interface PlatformManageSearchType extends SearchBase {
  keyword?: string;
  status?: number;
  platformManageTypeId?: string;
}

