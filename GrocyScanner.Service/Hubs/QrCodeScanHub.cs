using GrocyScanner.Service.Request;
using Microsoft.AspNetCore.SignalR;

namespace GrocyScanner.Service.Hubs;

public class QrCodeScanHub : Hub
{
    private readonly IHubContext<QrCodeScanHub> _hubContext;

    public QrCodeScanHub(IHubContext<QrCodeScanHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendBarcodeScan(string gtinBarcode)
    {
        await _hubContext.Clients.All.SendAsync("BarcodeScanned", gtinBarcode);
    }

    public async Task SendCamerasNotification(IEnumerable<CameraNotification> cameraNotifications)
    {
        await _hubContext.Clients.All.SendAsync("CamerasUpdated", cameraNotifications);
    }
}