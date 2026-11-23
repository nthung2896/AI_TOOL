using Microsoft.EntityFrameworkCore;
using Hinet.Model.Entities;

namespace Hinet.Repository.PlatformManageRepository
{
    public class PlatformManageRepository : Repository<PlatformManage>, IPlatformManageRepository
    {
        public PlatformManageRepository(DbContext context) : base(context)
        {
        }
    }
}
