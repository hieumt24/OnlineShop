using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Models.Db;

namespace OnlineShop.Hubs;

public class ProductHub : Hub
{
    private readonly OnlineShopContext _context;
    private static readonly ConcurrentDictionary<string, string> ConnectedUsers = new();

    public ProductHub(OnlineShopContext context)
    {
        _context = context;
    }

    // Khi client kết nối
    public override async Task OnConnectedAsync()
    {
        try
        {
            var userName = Context.User?.Identity?.Name ?? "Anonymous";
            ConnectedUsers.TryAdd(Context.ConnectionId, userName);

            // Lấy thống kê từ database
            var totalProducts = await _context.Products.CountAsync();
            var totalUsers = ConnectedUsers.Count;

            Console.WriteLine($"User connected: {userName}, Total products: {totalProducts}, Total users: {totalUsers}");

            // Gửi thống kê cho user vừa kết nối
            await Clients.Caller.SendAsync("StatisticsUpdated", new
            {
                TotalProducts = totalProducts,
                ActiveUsers = totalUsers,
                Status = "Connected"
            });

            // Thông báo cho tất cả về user mới kết nối (trừ chính user đó)
            await Clients.Others.SendAsync("UserConnected", new
            {
                ConnectionId = Context.ConnectionId,
                UserName = userName,
                TotalUsers = totalUsers
            });

            // Cập nhật số user cho tất cả clients
            await Clients.All.SendAsync("ActiveUsersUpdated", new
            {
                ActiveUsers = totalUsers
            });

            await base.OnConnectedAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in OnConnectedAsync: {ex.Message}");
        }
    }

    // Khi client ngắt kết nối
    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var totalUsers = Math.Max(0, Context.Items.Count - 1);
            
        await Clients.All.SendAsync("UserDisconnected", new
        {
            ConnectionId = Context.ConnectionId,
            TotalUsers = totalUsers
        });

        await base.OnDisconnectedAsync(exception);
    }
    // Method để refresh statistics cho tất cả clients
    public async Task RefreshStatistics()
    {
        try
        {
            var totalProducts = await _context.Products.CountAsync();
            var totalUsers = ConnectedUsers.Count;

            await Clients.All.SendAsync("StatisticsUpdated", new
            {
                TotalProducts = totalProducts,
                ActiveUsers = totalUsers,
                Status = "Connected"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in RefreshStatistics: {ex.Message}");
        }
    }

    // Tham gia group theo ProductId
    public async Task JoinProductGroup(string productId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Product_{productId}");
        await Clients.Client(Context.ConnectionId).SendAsync("JoinedGroup", $"Joined Product_{productId} group");
    }

    // Rời khỏi group theo ProductId
    public async Task LeaveProductGroup(string productId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Product_{productId}");
        await Clients.Client(Context.ConnectionId).SendAsync("LeftGroup", $"Left Product_{productId} group");
    }

    // Tham gia group admin
    public async Task JoinAdminGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "AdminGroup");
        await Clients.Client(Context.ConnectionId).SendAsync("JoinedAdminGroup", "Joined Admin group");
    }

    // Gửi thông báo về việc có người đang xem sản phẩm
    public async Task NotifyViewingProduct(int productId, string userName)
    {
        await Clients.Group($"Product_{productId}").SendAsync("UserViewingProduct", new
        {
            ProductId = productId,
            UserName = userName,
            ViewingAt = DateTime.Now
        });
    }

    // Gửi thông báo về việc có người đang chỉnh sửa sản phẩm
    public async Task NotifyEditingProduct(int productId, string userName)
    {
        await Clients.GroupExcept($"Product_{productId}", Context.ConnectionId).SendAsync("UserEditingProduct", new
        {
            ProductId = productId,
            UserName = userName,
            EditingAt = DateTime.Now
        });
    }

    // Gửi thông báo khi hoàn thành chỉnh sửa
    public async Task NotifyFinishedEditing(int productId, string userName)
    {
        await Clients.Group($"Product_{productId}").SendAsync("UserFinishedEditing", new
        {
            ProductId = productId,
            UserName = userName,
            FinishedAt = DateTime.Now
        });
    }

    // Lấy số lượng người đang online
    public async Task GetOnlineUsersCount()
    {
        // Đây là ví dụ đơn giản, bạn có thể implement phức tạp hơn
        await Clients.All.SendAsync("OnlineUsersCount", "Online users updated");
    }

    // Gửi thông báo realtime khi có thay đổi giá sản phẩm
    public async Task NotifyPriceChange(int productId, decimal oldPrice, decimal newPrice)
    {
        await Clients.All.SendAsync("PriceChanged", new
        {
            ProductId = productId,
            OldPrice = oldPrice,
            NewPrice = newPrice,
            ChangedAt = DateTime.Now
        });
    }

    // Gửi thông báo realtime khi có thay đổi kho hàng
    public async Task NotifyStockChange(int productId, int oldStock, int newStock)
    {
        await Clients.All.SendAsync("StockChanged", new
        {
            ProductId = productId,
            OldStock = oldStock,
            NewStock = newStock,
            ChangedAt = DateTime.Now
        });
    }

    // Gửi thông báo về trạng thái loading
    public async Task NotifyLoading(string message)
    {
        await Clients.Others.SendAsync("LoadingNotification", new
        {
            Message = message,
            User = Context.User?.Identity?.Name ?? "Anonymous",
            Timestamp = DateTime.Now
        });
    }

    // Broadcast thông báo chung
    public async Task BroadcastMessage(string message)
    {
        await Clients.All.SendAsync("BroadcastMessage", new
        {
            Message = message,
            SentAt = DateTime.Now,
            SenderId = Context.ConnectionId
        });
    }
}