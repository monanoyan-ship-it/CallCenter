using Microsoft.JSInterop;

namespace CallCenter.Web.Services;

public class ChartJsInterop
{
    private readonly IJSRuntime _js;

    public ChartJsInterop(IJSRuntime js) => _js = js;

    public async ValueTask CreateChartAsync(string canvasId, object config)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(config);
        await _js.InvokeVoidAsync("chartInterop.createChart", canvasId, json);
    }

    public async ValueTask UpdateChartAsync(string canvasId, object data)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        await _js.InvokeVoidAsync("chartInterop.updateChart", canvasId, json);
    }

    public async ValueTask DestroyChartAsync(string canvasId)
    {
        await _js.InvokeVoidAsync("chartInterop.destroyChart", canvasId);
    }
}
