import AspNetUsersServiceGenerated from "../generated/aspNetUsersService.generated";
import { apiService } from "../index";

class UserService extends AspNetUsersServiceGenerated {
  private static _instance: UserService;

  public static get instance(): UserService {
    if (!UserService._instance) {
      UserService._instance = new UserService();
    }
    return UserService._instance;
  }

  public async createChuyenVien(model: any) {
    const res = await apiService.post<any>(
      `/AspNetUsers/CreateChuyenVien`,
      model,
    );
    return res;
  }
}

const userService = UserService.instance;
export default userService;
