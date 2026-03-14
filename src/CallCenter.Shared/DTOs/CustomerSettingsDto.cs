namespace CallCenter.Shared.DTOs;

public class CustomerSettingsDto
{
    public bool AutoRecordCalls { get; set; }
    public bool IsCallbackManagementEnabled { get; set; }
}

public class UpdateCustomerSettingsRequest
{
    public bool? AutoRecordCalls { get; set; }
    public bool? IsCallbackManagementEnabled { get; set; }
}
