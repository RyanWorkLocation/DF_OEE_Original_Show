using PMCDash.Repos;
using PMCDash.Models;
using System.Collections;
using System.Collections.Generic;

namespace PMCDash.Services
{
    public class ReportService
    {
        private readonly ReportRepo _reportRepo;

        public ReportService(ReportRepo reportRepo)
        {
            _reportRepo = reportRepo;
        }

        public IEnumerable<ReporterInformation> GetInformations()
        {
            return _reportRepo.GetImformation();
        }

        public ReporterInformation GetInformations(string orderID)
        {
            return _reportRepo.GetImformation(orderID);
        }
    }
}
