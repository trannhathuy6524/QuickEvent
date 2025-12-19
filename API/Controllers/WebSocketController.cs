using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickEvent.Services;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;

namespace QuickEvent.API.Controllers
{
    [ApiController]
    [Route("api/ws")]
    public class WebSocketController : ControllerBase
    {
        private readonly WebSocketHub _webSocketHub;

        public WebSocketController(WebSocketHub webSocketHub)
        {
            _webSocketHub = webSocketHub;
        }

        /// <summary>
        /// WebSocket endpoint
        /// URL: ws://YOUR_IP:5217/api/ws?userId=USER_ID
        /// </summary>
        [HttpGet]
        public async Task Connect()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                // Lấy userId từ query string (cho Flutter)
                var userId = HttpContext.Request.Query["userId"].ToString();

                if (string.IsNullOrEmpty(userId))
                {
                    // Hoặc từ authentication (cho Web)
                    userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                }

                if (string.IsNullOrEmpty(userId))
                {
                    HttpContext.Response.StatusCode = 401;
                    await HttpContext.Response.WriteAsync("Unauthorized");
                    return;
                }

                var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await _webSocketHub.AddConnectionAsync(userId, webSocket);

                try
                {
                    await ReceiveMessagesAsync(webSocket, userId);
                }
                finally
                {
                    await _webSocketHub.RemoveConnectionAsync(webSocket);
                }
            }
            else
            {
                HttpContext.Response.StatusCode = 400;
            }
        }

        private async Task ReceiveMessagesAsync(WebSocket webSocket, string userId)
        {
            var buffer = new byte[1024 * 4];

            while (webSocket.State == WebSocketState.Open)
            {
                try
                {
                    var result = await webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                        // Handle ping/pong
                        if (message == "ping")
                        {
                            await _webSocketHub.SendMessageToUserAsync(userId, new
                            {
                                type = "pong",
                                timestamp = DateTime.Now
                            });
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                }
                catch (WebSocketException)
                {
                    break;
                }
            }
        }

        // ========== EVENT NOTIFICATIONS ==========

        /// <summary>
        /// Broadcast sự kiện được tạo
        /// POST: api/ws/notify/event/created
        /// </summary>
        [HttpPost("notify/event/created")]
        [Authorize]
        public async Task<IActionResult> NotifyEventCreated([FromBody] NotifyEventRequest request)
        {
            try
            {
                await _webSocketHub.NotifyEventCreatedAsync(request.EventId, request.EventData);
                return Ok(new { message = "Event created notification sent successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error sending notification", error = ex.Message });
            }
        }

        /// <summary>
        /// Broadcast sự kiện được cập nhật
        /// POST: api/ws/notify/event/updated
        /// </summary>
        [HttpPost("notify/event/updated")]
        [Authorize]
        public async Task<IActionResult> NotifyEventUpdated([FromBody] NotifyEventRequest request)
        {
            try
            {
                await _webSocketHub.NotifyEventUpdatedAsync(request.EventId, request.EventData);
                return Ok(new { message = "Event updated notification sent successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error sending notification", error = ex.Message });
            }
        }

        /// <summary>
        /// Broadcast sự kiện bị xóa
        /// POST: api/ws/notify/event/deleted
        /// </summary>
        [HttpPost("notify/event/deleted")]
        [Authorize]
        public async Task<IActionResult> NotifyEventDeleted([FromBody] NotifyEventDeletedRequest request)
        {
            try
            {
                await _webSocketHub.NotifyEventDeletedAsync(request.EventId, request.Reason);
                return Ok(new { message = "Event deleted notification sent successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error sending notification", error = ex.Message });
            }
        }

        // ========== REGISTRATION NOTIFICATIONS ==========

        /// <summary>
        /// Gửi thông báo đăng ký mới
        /// POST: api/ws/notify/registration/created
        /// </summary>
        [HttpPost("notify/registration/created")]
        [Authorize]
        public async Task<IActionResult> NotifyRegistrationCreated([FromBody] NotifyRegistrationCreatedRequest request)
        {
            try
            {
                await _webSocketHub.NotifyRegistrationCreatedAsync(
                    request.OrganizerId,
                    request.RegistrationId,
                    request.EventId,
                    request.ParticipantName,
                    request.RegistrationData);
                return Ok(new { message = "Registration created notification sent successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error sending notification", error = ex.Message });
            }
        }

        /// <summary>
        /// Gửi thông báo cập nhật đăng ký
        /// POST: api/ws/notify/registration/updated
        /// </summary>
        [HttpPost("notify/registration/updated")]
        [Authorize]
        public async Task<IActionResult> NotifyRegistrationUpdated([FromBody] NotifyRegistrationUpdatedRequest request)
        {
            try
            {
                await _webSocketHub.NotifyRegistrationUpdatedAsync(
                    request.RegistrationId,
                    request.EventId,
                    request.RegistrationData);
                return Ok(new { message = "Registration updated notification sent successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error sending notification", error = ex.Message });
            }
        }

        /// <summary>
        /// Gửi thông báo hủy đăng ký
        /// POST: api/ws/notify/registration/cancelled
        /// </summary>
        [HttpPost("notify/registration/cancelled")]
        [Authorize]
        public async Task<IActionResult> NotifyRegistrationCancelled([FromBody] NotifyRegistrationCancelledRequest request)
        {
            try
            {
                await _webSocketHub.NotifyRegistrationCancelledAsync(
                    request.OrganizerId,
                    request.RegistrationId,
                    request.EventId,
                    request.ParticipantName,
                    request.Reason);
                return Ok(new { message = "Registration cancelled notification sent successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error sending notification", error = ex.Message });
            }
        }

        // ========== CHECK-IN NOTIFICATIONS ==========

        /// <summary>
        /// Gửi thông báo check-in
        /// POST: api/ws/notify/checkin
        /// </summary>
        [HttpPost("notify/checkin")]
        [Authorize]
        public async Task<IActionResult> NotifyCheckIn([FromBody] NotifyCheckInRequest request)
        {
            try
            {
                await _webSocketHub.NotifyCheckInAsync(
                    request.OrganizerId,
                    request.UserId,
                    request.EventId,
                    request.ParticipantName,
                    request.CheckInData);
                return Ok(new { message = "Check-in notification sent successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error sending notification", error = ex.Message });
            }
        }

        // ========== NOTIFICATION MANAGEMENT ==========

        /// <summary>
        /// Gửi thông báo tạo notification mới
        /// POST: api/ws/notify/notification/created
        /// </summary>
        [HttpPost("notify/notification/created")]
        [Authorize]
        public async Task<IActionResult> NotifyNotificationCreated([FromBody] NotifyNotificationRequest request)
        {
            try
            {
                await _webSocketHub.NotifyNotificationCreatedAsync(request.UserId, request.NotificationData);
                return Ok(new { message = "Notification created notification sent successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error sending notification", error = ex.Message });
            }
        }

        /// <summary>
        /// Gửi thông báo đọc notification
        /// POST: api/ws/notify/notification/read
        /// </summary>
        [HttpPost("notify/notification/read")]
        [Authorize]
        public async Task<IActionResult> NotifyNotificationRead([FromBody] NotifyNotificationReadRequest request)
        {
            try
            {
                await _webSocketHub.NotifyNotificationReadAsync(request.UserId, request.NotificationId);
                return Ok(new { message = "Notification read notification sent successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error sending notification", error = ex.Message });
            }
        }

        // ========== STATISTICS ==========

        /// <summary>
        /// Gửi thông báo cập nhật thống kê
        /// POST: api/ws/notify/statistics
        /// </summary>
        [HttpPost("notify/statistics")]
        [Authorize]
        public async Task<IActionResult> NotifyStatisticsUpdated([FromBody] NotifyStatisticsRequest request)
        {
            try
            {
                await _webSocketHub.NotifyStatisticsUpdatedAsync(request.UserId, request.Statistics);
                return Ok(new { message = "Statistics notification sent successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error sending notification", error = ex.Message });
            }
        }

        // ========== BROADCAST ==========

        /// <summary>
        /// Broadcast message tới tất cả users
        /// POST: api/ws/broadcast
        /// </summary>
        [HttpPost("broadcast")]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<IActionResult> Broadcast([FromBody] BroadcastRequest request)
        {
            try
            {
                await _webSocketHub.BroadcastAsync(request.Message);
                return Ok(new { message = "Broadcast sent successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error broadcasting", error = ex.Message });
            }
        }

        /// <summary>
        /// Gửi message tới user cụ thể
        /// POST: api/ws/send
        /// </summary>
        [HttpPost("send")]
        [Authorize]
        public async Task<IActionResult> SendMessageToUser([FromBody] SendMessageRequest request)
        {
            try
            {
                await _webSocketHub.SendMessageToUserAsync(request.UserId, request.Message);
                return Ok(new { message = "Message sent successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error sending message", error = ex.Message });
            }
        }

        /// <summary>
        /// Gửi notification tới user
        /// POST: api/ws/notification
        /// </summary>
        [HttpPost("notification")]
        [Authorize]
        public async Task<IActionResult> SendNotification([FromBody] SendNotificationRequest request)
        {
            try
            {
                await _webSocketHub.SendNotificationAsync(
                    request.UserId,
                    request.Type,
                    request.Title,
                    request.Body,
                    request.Data);
                return Ok(new { message = "Notification sent successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error sending notification", error = ex.Message });
            }
        }

        // ========== UTILITY ENDPOINTS ==========

        /// <summary>
        /// Lấy số lượng kết nối của user
        /// GET: api/ws/connections/{userId}
        /// </summary>
        [HttpGet("connections/{userId}")]
        [Authorize]
        public IActionResult GetUserConnectionCount(string userId)
        {
            var count = _webSocketHub.GetUserConnectionCount(userId);
            return Ok(new { userId, connectionCount = count });
        }

        /// <summary>
        /// Lấy tổng số kết nối
        /// GET: api/ws/connections/total
        /// </summary>
        [HttpGet("connections/total")]
        [Authorize(Roles = "Admin,Organizer")]
        public IActionResult GetTotalConnectionCount()
        {
            var count = _webSocketHub.GetTotalConnectionCount();
            return Ok(new { totalConnections = count });
        }

        /// <summary>
        /// Lấy danh sách users online
        /// GET: api/ws/online-users
        /// </summary>
        [HttpGet("online-users")]
        [Authorize(Roles = "Admin,Organizer")]
        public IActionResult GetOnlineUsers()
        {
            var users = _webSocketHub.GetOnlineUsers();
            return Ok(new { onlineUsers = users, count = users.Count });
        }

        // ========== REST API ENDPOINTS FOR TESTING/MANAGEMENT ==========

        /// <summary>
        /// Get WebSocket connection info
        /// GET: /api/ws/info
        /// </summary>
        [HttpGet("info")]
        public IActionResult GetInfo()
        {
            return Ok(new
            {
                totalConnections = _webSocketHub.GetTotalConnectionCount(),
                onlineUsers = _webSocketHub.GetOnlineUsers(),
                timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// Test broadcast message to all users
        /// POST: /api/ws/test/broadcast
        /// </summary>
        [HttpPost("test/broadcast")]
        [Authorize]
        public async Task<IActionResult> TestBroadcast([FromBody] TestMessageRequest request)
        {
            await _webSocketHub.BroadcastAsync(new
            {
                type = "test_broadcast",
                message = request.Message ?? "Test broadcast message",
                timestamp = DateTime.Now
            });

            return Ok(new { message = "Broadcast sent", recipients = _webSocketHub.GetTotalConnectionCount() });
        }

        /// <summary>
        /// Test send message to specific user
        /// POST: /api/ws/test/send/{userId}
        /// </summary>
        [HttpPost("test/send/{userId}")]
        [Authorize]
        public async Task<IActionResult> TestSendToUser(string userId, [FromBody] TestMessageRequest request)
        {
            await _webSocketHub.SendMessageToUserAsync(userId, new
            {
                type = "test_message",
                message = request.Message ?? "Test message",
                timestamp = DateTime.Now
            });

            return Ok(new { message = $"Message sent to user {userId}" });
        }

        /// <summary>
        /// Test notification
        /// POST: /api/ws/test/notification/{userId}
        /// </summary>
        [HttpPost("test/notification/{userId}")]
        [Authorize]
        public async Task<IActionResult> TestNotification(string userId, [FromBody] TestNotificationRequest request)
        {
            await _webSocketHub.SendNotificationAsync(
                userId,
                request.Type ?? "test",
                request.Title ?? "Test Notification",
                request.Body ?? "This is a test notification",
                request.Data
            );

            return Ok(new { message = $"Notification sent to user {userId}" });
        }

        // ========== EVENT CRUD TEST ENDPOINTS ==========

        /// <summary>
        /// Test Event Created notification
        /// POST: /api/ws/test/event/created
        /// </summary>
        [HttpPost("test/event/created")]
        [Authorize]
        public async Task<IActionResult> TestEventCreated([FromBody] TestEventRequest request)
        {
            await _webSocketHub.NotifyEventCreatedAsync(
                request.EventId,
                new
                {
                    id = request.EventId,
                    title = request.Title ?? "Test Event",
                    description = request.Description,
                    location = request.Location
                }
            );

            return Ok(new { message = "Event created notification sent" });
        }

        /// <summary>
        /// Test Event Updated notification
        /// POST: /api/ws/test/event/updated
        /// </summary>
        [HttpPost("test/event/updated")]
        [Authorize]
        public async Task<IActionResult> TestEventUpdated([FromBody] TestEventRequest request)
        {
            await _webSocketHub.NotifyEventUpdatedAsync(
                request.EventId,
                new
                {
                    id = request.EventId,
                    title = request.Title ?? "Updated Event",
                    description = request.Description,
                    location = request.Location
                }
            );

            return Ok(new { message = "Event updated notification sent" });
        }

        /// <summary>
        /// Test Event Deleted notification
        /// POST: /api/ws/test/event/deleted
        /// </summary>
        [HttpPost("test/event/deleted")]
        [Authorize]
        public async Task<IActionResult> TestEventDeleted([FromBody] TestEventDeleteRequest request)
        {
            await _webSocketHub.NotifyEventDeletedAsync(
                request.EventId,
                request.Reason ?? "Test deletion"
            );

            return Ok(new { message = "Event deleted notification sent" });
        }

        // ========== REGISTRATION CRUD TEST ENDPOINTS ==========

        /// <summary>
        /// Test Registration Created notification
        /// POST: /api/ws/test/registration/created
        /// </summary>
        [HttpPost("test/registration/created")]
        [Authorize]
        public async Task<IActionResult> TestRegistrationCreated([FromBody] TestRegistrationRequest request)
        {
            await _webSocketHub.NotifyRegistrationCreatedAsync(
                request.OrganizerId,
                request.RegistrationId,
                request.EventId,
                request.ParticipantName ?? "Test User",
                new
                {
                    id = request.RegistrationId,
                    fullName = request.ParticipantName,
                    eventId = request.EventId
                }
            );

            return Ok(new { message = "Registration created notification sent" });
        }

        /// <summary>
        /// Test Registration Cancelled notification
        /// POST: /api/ws/test/registration/cancelled
        /// </summary>
        [HttpPost("test/registration/cancelled")]
        [Authorize]
        public async Task<IActionResult> TestRegistrationCancelled([FromBody] TestRegistrationCancelRequest request)
        {
            await _webSocketHub.NotifyRegistrationCancelledAsync(
                request.OrganizerId,
                request.RegistrationId,
                request.EventId,
                request.ParticipantName ?? "Test User",
                request.Reason ?? "Test cancellation"
            );

            return Ok(new { message = "Registration cancelled notification sent" });
        }

        // ========== CHECK-IN TEST ENDPOINT ==========

        /// <summary>
        /// Test Check-in notification
        /// POST: /api/ws/test/checkin
        /// </summary>
        [HttpPost("test/checkin")]
        [Authorize]
        public async Task<IActionResult> TestCheckIn([FromBody] TestCheckInRequest request)
        {
            await _webSocketHub.NotifyCheckInAsync(
                request.OrganizerId,
                request.UserId,
                request.EventId,
                request.ParticipantName ?? "Test User",
                new
                {
                    eventId = request.EventId,
                    participantName = request.ParticipantName,
                    checkInTime = DateTime.Now
                }
            );

            return Ok(new { message = "Check-in notification sent" });
        }

        // ========== STATISTICS TEST ENDPOINT ==========

        /// <summary>
        /// Test Statistics Update notification
        /// POST: /api/ws/test/statistics/{userId}
        /// </summary>
        [HttpPost("test/statistics/{userId}")]
        [Authorize]
        public async Task<IActionResult> TestStatistics(string userId, [FromBody] TestStatisticsRequest request)
        {
            await _webSocketHub.NotifyStatisticsUpdatedAsync(
                userId,
                new
                {
                    totalEvents = request.TotalEvents,
                    totalRegistrations = request.TotalRegistrations,
                    totalCheckIns = request.TotalCheckIns,
                    timestamp = DateTime.Now
                }
            );

            return Ok(new { message = $"Statistics update sent to user {userId}" });
        }
    }

    // ========== REQUEST MODELS ==========

    public class NotifyEventRequest
    {
        public int EventId { get; set; }
        public object EventData { get; set; } = new { };
    }

    public class NotifyEventDeletedRequest
    {
        public int EventId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class NotifyRegistrationCreatedRequest
    {
        public string OrganizerId { get; set; } = string.Empty;
        public int RegistrationId { get; set; }
        public int EventId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public object RegistrationData { get; set; } = new { };
    }

    public class NotifyRegistrationUpdatedRequest
    {
        public int RegistrationId { get; set; }
        public int EventId { get; set; }
        public object RegistrationData { get; set; } = new { };
    }

    public class NotifyRegistrationCancelledRequest
    {
        public string OrganizerId { get; set; } = string.Empty;
        public int RegistrationId { get; set; }
        public int EventId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class NotifyCheckInRequest
    {
        public string OrganizerId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public int EventId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public object CheckInData { get; set; } = new { };
    }

    public class NotifyNotificationRequest
    {
        public string UserId { get; set; } = string.Empty;
        public object NotificationData { get; set; } = new { };
    }

    public class NotifyNotificationReadRequest
    {
        public string UserId { get; set; } = string.Empty;
        public int NotificationId { get; set; }
    }

    public class NotifyStatisticsRequest
    {
        public string UserId { get; set; } = string.Empty;
        public object Statistics { get; set; } = new { };
    }

    public class BroadcastRequest
    {
        public object Message { get; set; } = new { };
    }

    public class SendMessageRequest
    {
        public string UserId { get; set; } = string.Empty;
        public object Message { get; set; } = new { };
    }

    public class SendNotificationRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public object? Data { get; set; }
    }

    public class TestMessageRequest
    {
        public string? Message { get; set; }
    }

    public class TestNotificationRequest
    {
        public string? Type { get; set; }
        public string? Title { get; set; }
        public string? Body { get; set; }
        public object? Data { get; set; }
    }

    public class TestEventRequest
    {
        public int EventId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
    }

    public class TestEventDeleteRequest
    {
        public int EventId { get; set; }
        public string? Reason { get; set; }
    }

    public class TestRegistrationRequest
    {
        public string OrganizerId { get; set; }
        public int RegistrationId { get; set; }
        public int EventId { get; set; }
        public string? ParticipantName { get; set; }
    }

    public class TestRegistrationCancelRequest
    {
        public string OrganizerId { get; set; }
        public int RegistrationId { get; set; }
        public int EventId { get; set; }
        public string? ParticipantName { get; set; }
        public string? Reason { get; set; }
    }

    public class TestCheckInRequest
    {
        public string OrganizerId { get; set; }
        public string UserId { get; set; }
        public int EventId { get; set; }
        public string? ParticipantName { get; set; }
    }

    public class TestStatisticsRequest
    {
        public int TotalEvents { get; set; }
        public int TotalRegistrations { get; set; }
        public int TotalCheckIns { get; set; }
    }
}