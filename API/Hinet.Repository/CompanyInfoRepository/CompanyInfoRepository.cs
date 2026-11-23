using Microsoft.EntityFrameworkCore;
using Hinet.Model.Entities;

namespace Hinet.Repository.CompanyInfoRepository
{
    public class CompanyInfoRepository : Repository<CompanyInfo>, ICompanyInfoRepository
    {
        public CompanyInfoRepository(DbContext context) : base(context)
        {
        }
    }
}
