import {
  PlatformManageType,
} from "@/types/platformManage/dto";
import {
  PlatformManageSearchType,
  PlatformTransitionRequest,
} from "@/types/platformManage/request";
import { ApiResponse, PagedList, DropdownOption, DropdownOptionTree, Dictionary, DataImport} from "@/types/general";
import { apiService } from "../index";

class PlatformManageServiceGenerated {
  public async getData(
    search: PlatformManageSearchType
  ): Promise<ApiResponse<PagedList<PlatformManageType>>> {
    try {
      const response = await apiService.post<PagedList<PlatformManageType>>(
        `/PlatformManage/GetData`,
        search
      );
      return response;
    } catch (error) {
      throw error;
    }
  }

  public async get(
    id: string
  ): Promise<ApiResponse<PlatformManageType>> {
    try {
      const response = await apiService.get<PlatformManageType>(
        `/PlatformManage/Get/${id}`
      );
      return response;
    } catch (error) {
      throw error;
    }
  }

  public async transition(
    request: PlatformTransitionRequest
  ): Promise<ApiResponse<any>> {
    try {
      const response = await apiService.post<any>(
        `/PlatformManage/Transition`,
        request
      );
      return response;
    } catch (error) {
      throw error;
    }
  }

}

export default PlatformManageServiceGenerated;
