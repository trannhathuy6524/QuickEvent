using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace QuickEvent.Services
{
    public class WebSocketHub
    {
        // Lưu tất cả connections theo UserId
        private readonly ConcurrentDictionary<string, List<WebSocket>> _connections = new();
        private readonly ConcurrentDictionary<WebSocket, string> _userIds = new();

        // Thêm connection mới
        public async Task AddConnectionAsync(string userId, WebSocket webSocket)
        {
            if (string.IsNullOrEmpty(userId)) return;

            if (!_connections.TryGetValue(userId, out var sockets))
            {
                sockets = new List<WebSocket>();
                _connections[userId] = sockets;
            }

            lock (sockets)
            {
                sockets.Add(webSocket);
            }

            _userIds[webSocket] = userId;
            Console.WriteLine($"✅ [WebSocket] User {userId} connected. Total: {sockets.Count}");

            // Gửi message chào mừng
            await SendMessageToUserAsync(userId, new
            {
                type = "connected",
                message = "Kết nối WebSocket thành công",
                userId = userId,
                timestamp = DateTime.Now
            });
        }

        // Xóa connection
        public async Task RemoveConnectionAsync(WebSocket webSocket)
        {
            if (_userIds.TryRemove(webSocket, out var userId))
            {
                if (_connections.TryGetValue(userId, out var sockets))
                {
                    lock (sockets)
                    {
                        sockets.Remove(webSocket);
                        if (sockets.Count == 0)
                        {
                            _connections.TryRemove(userId, out _);
                        }
                    }
                }
                Console.WriteLine($"❌ [WebSocket] User {userId} disconnected");
            }

            if (webSocket.State == WebSocketState.Open)
            {
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Closed",
                    CancellationToken.None);
            }
        }

        // Gửi message đến user cụ thể
        public async Task SendMessageToUserAsync(string userId, object message)
        {
            if (!_connections.TryGetValue(userId, out var sockets)) return;

            var json = JsonSerializer.Serialize(message);
            var buffer = Encoding.UTF8.GetBytes(json);
            var deadSockets = new List<WebSocket>();

            foreach (var socket in sockets.ToList())
            {
                if (socket.State == WebSocketState.Open)
                {
                    try
                    {
                        await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    catch
                    {
                        deadSockets.Add(socket);
                    }
                }
                else
                {
                    deadSockets.Add(socket);
                }
            }

            foreach (var deadSocket in deadSockets)
            {
                await RemoveConnectionAsync(deadSocket);
            }
        }

        // Gửi message đến tất cả người dùng
        public async Task BroadcastAsync(object message)
        {
            var tasks = _connections.Keys.Select(userId => SendMessageToUserAsync(userId, message));
            await Task.WhenAll(tasks);
        }

        // Xử lý WebSocket connection từ client
        public async Task HandleConnectionAsync(string userId, WebSocket webSocket)
        {
            await AddConnectionAsync(userId, webSocket);

            var buffer = new byte[1024 * 4];
            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await RemoveConnectionAsync(webSocket);
                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Console.WriteLine($"📨 [WebSocket] Received from {userId}: {message}");

                        // Xử lý ping/pong
                        if (message.Trim().ToLower() == "ping")
                        {
                            await SendMessageToUserAsync(userId, new { type = "pong", timestamp = DateTime.Now });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [WebSocket] Error with user {userId}: {ex.Message}");
            }
            finally
            {
                await RemoveConnectionAsync(webSocket);
            }
        }

        // Gửi notification
        public async Task SendNotificationAsync(string userId, string type, string title, string body, object? data = null)
        {
            await SendMessageToUserAsync(userId, new
            {
                type = "notification",
                notificationType = type,
                title = title,
                body = body,
                data = data,
                timestamp = DateTime.Now
            });
        }

        // ========== CRUD REAL-TIME METHODS ==========

        // EVENT CRUD
        public async Task NotifyEventCreatedAsync(int eventId, object eventData)
        {
            await BroadcastAsync(new
            {
                type = "event_created",
                action = "create",
                eventId = eventId,
                data = eventData,
                timestamp = DateTime.Now
            });
            Console.WriteLine($"📢 [WebSocket] Event {eventId} created - broadcasted");
        }

        public async Task NotifyEventUpdatedAsync(int eventId, object eventData)
        {
            await BroadcastAsync(new
            {
                type = "event_updated",
                action = "update",
                eventId = eventId,
                data = eventData,
                timestamp = DateTime.Now
            });
            Console.WriteLine($"📢 [WebSocket] Event {eventId} updated - broadcasted");
        }

        public async Task NotifyEventDeletedAsync(int eventId, string reason)
        {
            await BroadcastAsync(new
            {
                type = "event_deleted",
                action = "delete",
                eventId = eventId,
                reason = reason,
                timestamp = DateTime.Now
            });
            Console.WriteLine($"📢 [WebSocket] Event {eventId} deleted - broadcasted");
        }

        // REGISTRATION CRUD
        public async Task NotifyRegistrationCreatedAsync(string organizerId, int registrationId, int eventId, string participantName, object registrationData)
        {
            // Gửi cho organizer
            await SendNotificationAsync(organizerId, "registration_created", "Đăng ký mới",
                $"{participantName} đã đăng ký", new { eventId, registrationId, participantName });

            // Broadcast update event
            await BroadcastAsync(new
            {
                type = "registration_created",
                action = "create",
                registrationId = registrationId,
                eventId = eventId,
                data = registrationData,
                timestamp = DateTime.Now
            });
        }

        public async Task NotifyRegistrationUpdatedAsync(int registrationId, int eventId, object registrationData)
        {
            await BroadcastAsync(new
            {
                type = "registration_updated",
                action = "update",
                registrationId = registrationId,
                eventId = eventId,
                data = registrationData,
                timestamp = DateTime.Now
            });
        }

        public async Task NotifyRegistrationCancelledAsync(string organizerId, int registrationId, int eventId, string participantName, string reason)
        {
            await SendNotificationAsync(organizerId, "registration_cancelled", "Hủy đăng ký",
                $"{participantName} đã hủy. Lý do: {reason}", new { eventId, registrationId });

            await BroadcastAsync(new
            {
                type = "registration_cancelled",
                action = "delete",
                registrationId = registrationId,
                eventId = eventId,
                reason = reason,
                timestamp = DateTime.Now
            });
        }

        // CHECK-IN CRUD
        public async Task NotifyCheckInAsync(string organizerId, string userId, int eventId, string participantName, object checkInData)
        {
            // Gửi cho organizer
            await SendNotificationAsync(organizerId, "check_in", "Check-in mới",
                $"{participantName} đã check-in", new { eventId, participantName });

            // Gửi cho user
            await SendNotificationAsync(userId, "check_in_success", "Check-in thành công",
                "Bạn đã check-in thành công", new { eventId });

            // Broadcast
            await BroadcastAsync(new
            {
                type = "check_in_created",
                action = "create",
                eventId = eventId,
                data = checkInData,
                timestamp = DateTime.Now
            });
        }

        // NOTIFICATION CRUD
        public async Task NotifyNotificationCreatedAsync(string userId, object notificationData)
        {
            await SendMessageToUserAsync(userId, new
            {
                type = "notification_created",
                action = "create",
                data = notificationData,
                timestamp = DateTime.Now
            });
        }

        public async Task NotifyNotificationReadAsync(string userId, int notificationId)
        {
            await SendMessageToUserAsync(userId, new
            {
                type = "notification_read",
                action = "update",
                notificationId = notificationId,
                timestamp = DateTime.Now
            });
        }

        // STATISTICS UPDATE
        public async Task NotifyStatisticsUpdatedAsync(string userId, object statistics)
        {
            await SendMessageToUserAsync(userId, new
            {
                type = "statistics_updated",
                action = "update",
                data = statistics,
                timestamp = DateTime.Now
            });
        }

        // ========== UTILITY METHODS =========="
        public int GetUserConnectionCount(string userId)
        {
            return _connections.TryGetValue(userId, out var sockets) ? sockets.Count : 0;
        }

        public int GetTotalConnectionCount()
        {
            return _connections.Values.Sum(sockets => sockets.Count);
        }

        public List<string> GetOnlineUsers()
        {
            return _connections.Keys.ToList();
        }
    }
}