using CallCenter.Shared.DTOs;

namespace CallCenter.Api.Factories.Interfaces;

public interface ISlnDataImportFactory
{
    List<SlnDataImportTemplateDto> GetTemplates();
    byte[] BuildExcelTemplate(string importType, out string fileName);
    Task<SlnDataImportPreviewDto> PreviewAsync(string importType, Stream stream, string fileName, int customerId, int? branchId);
    Task<SlnDataImportPreviewDto> ImportAsync(string importType, Stream stream, string fileName, int customerId, int userId, int? branchId);
}
