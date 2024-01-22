using GrocyScanner.Service.Hubs;
using GrocyScanner.Service.Request;
using Microsoft.AspNetCore.Mvc;

namespace GrocyScanner.Service.Controllers;

[ApiController]
[Route("api/{controller}")]
public class BarcodeScanController : Controller
{
    private readonly QrCodeScanHub _qrCodeScanHub;

    public BarcodeScanController(QrCodeScanHub qrCodeScanHub)
    {
        _qrCodeScanHub = qrCodeScanHub;
    }

    [HttpPost]
    [Route("notify")]
    public async Task NotifyBarcodeScan([FromBody] BarcodeNotification barcodeNotification)
    {
        await _qrCodeScanHub.SendBarcodeScan(barcodeNotification.Gtin);
    }
    
    [HttpPost]
    [Route("notify-cameras")]
    public async Task NotifyCameras([FromBody] IEnumerable<CameraNotification> cameraNotifications)
    {
        await _qrCodeScanHub.SendCamerasNotification(cameraNotifications);
    }
}