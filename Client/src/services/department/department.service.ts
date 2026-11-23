import DepartmentServiceGenerated from "../generated/departmentService.generated";

class DepartmentService extends DepartmentServiceGenerated {
  private static _instance: DepartmentService;
  public static get instance(): DepartmentService {
    if (!DepartmentService._instance) {
      DepartmentService._instance = new DepartmentService();
    }
    return DepartmentService._instance;
  }
}
const departmentService = DepartmentService.instance;
export default departmentService;
