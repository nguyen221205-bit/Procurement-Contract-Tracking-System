import React, { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Bell,
  CheckCheck,
  Loader2,
  ArrowRight,
  AlertTriangle,
  Clock,
  AlertCircle,
  Info,
  BellOff
} from 'lucide-react';
import notificationApi from '../../api/notificationApi';
import { formatRelativeTime } from '../../utils/formatters';
import { useAuth } from '../../context/AuthContext';
import toast from 'react-hot-toast';
import * as signalR from '@microsoft/signalr';
import { playNotificationSound } from '../../utils/notificationSound';

export const NotificationBell = () => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [unreadCount, setUnreadCount] = useState(0);
  const [isOpen, setIsOpen] = useState(false);
  const [notifications, setNotifications] = useState([]);
  const [loading, setLoading] = useState(false);
  const dropdownRef = useRef(null);

  // Điều hướng thông minh theo ngữ cảnh nghiệp vụ của thông báo
  const getTargetRoute = (notif) => {
    if (!notif) return '/notifications';
    const text = `${notif.title || ''} ${notif.message || ''}`.toLowerCase();
    if (text.includes('hợp đồng') || text.includes('tiến độ') || text.includes('nghiệm thu')) {
      return '/contracts';
    }
    if (text.includes('tổ chuyên gia') || text.includes('chấm thầu') || text.includes('xét thầu') || text.includes('giám khảo')) {
      return '/evaluation';
    }
    if (text.includes('gói thầu') || text.includes('dự thầu') || text.includes('mở thầu') || text.includes('đóng thầu')) {
      return '/packages';
    }
    return '/notifications';
  };

  // Lấy số lượng thông báo chưa đọc
  const fetchUnreadCount = async () => {
    try {
      const res = await notificationApi.getUnreadCount();
      if (res && res.success && typeof res.data === 'number') {
        setUnreadCount(res.data);
      } else if (typeof res === 'number') {
        setUnreadCount(res);
      }
    } catch {
      // Bỏ qua lỗi polling im lặng
    }
  };

  // Lấy danh sách thông báo xem nhanh (6 thông báo mới nhất)
  const fetchRecentNotifications = async () => {
    setLoading(true);
    try {
      const res = await notificationApi.getNotifications(1, 6);
      if (res && res.success && res.data?.items) {
        setNotifications(res.data.items);
      } else if (Array.isArray(res?.items)) {
        setNotifications(res.items);
      } else if (Array.isArray(res?.data)) {
        setNotifications(res.data);
      }
    } catch (err) {
      toast.error('Không thể tải danh sách thông báo');
    } finally {
      setLoading(false);
    }
  };

  // Khởi tạo SignalR Real-time Connection và HTTP Polling dự phòng khi đã đăng nhập
  useEffect(() => {
    if (!user) {
      setUnreadCount(0);
      return;
    }

    fetchUnreadCount();

    // 1. Thiết lập kết nối SignalR thời gian thực (0 giây độ trễ)
    const hubUrl = 'http://localhost:5225/hubs/notifications';
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => sessionStorage.getItem('token') || '',
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.None)
      .build();

    connection.on('ReceiveNotification', (newNotif) => {
      // Tăng số lượng thông báo chưa đọc
      setUnreadCount((prev) => prev + 1);

      // Cập nhật danh sách thông báo xem nhanh nếu đang mở dropdown hoặc đã load
      setNotifications((prev) => [newNotif, ...prev.slice(0, 5)]);

      // Phát âm báo chuông nhẹ nhàng
      playNotificationSound();

      // Hiển thị Toast nổi bật tức thì
      toast.custom(
        (t) => (
          <div
            className={`${
              t.visible ? 'animate-enter' : 'animate-leave'
            } max-w-md w-full bg-slate-900 border border-emerald-500/50 shadow-2xl rounded-xl pointer-events-auto flex ring-1 ring-black ring-opacity-5 p-4 cursor-pointer hover:border-emerald-400 transition-all`}
            onClick={() => {
              toast.dismiss(t.id);
              const route = getTargetRoute(newNotif);
              navigate(route);
            }}
          >
            <div className="flex items-start gap-3 w-full">
              <div className="p-2 bg-emerald-500/20 text-emerald-400 rounded-lg shrink-0">
                <Bell className="w-5 h-5 animate-pulse" />
              </div>
              <div className="flex-1 min-w-0">
                <div className="flex items-center justify-between">
                  <p className="text-xs font-semibold text-emerald-400 uppercase tracking-wider">Thông báo mới</p>
                  <span className="text-[10px] text-slate-400">Vừa xong</span>
                </div>
                <p className="text-sm font-medium text-slate-100 truncate mt-0.5">{newNotif.title || 'Thông báo hệ thống'}</p>
                <p className="text-xs text-slate-400 line-clamp-2 mt-0.5">{newNotif.message}</p>
              </div>
            </div>
          </div>
        ),
        { duration: 6000, position: 'top-right' }
      );
    });

    connection
      .start()
      .catch((err) => {
        console.debug('SignalR kết nối không thành công, tiếp tục cơ chế HTTP Polling:', err);
      });

    // 2. HTTP Polling dự phòng (30s) đảm bảo đồng bộ trạng thái khi mất mạng
    const interval = setInterval(fetchUnreadCount, 30000);

    const handleVisibility = () => {
      if (document.visibilityState === 'visible') {
        fetchUnreadCount();
      }
    };
    document.addEventListener('visibilitychange', handleVisibility);

    return () => {
      connection.stop().catch(() => {});
      clearInterval(interval);
      document.removeEventListener('visibilitychange', handleVisibility);
    };
  }, [user, navigate]);

  // Xử lý đóng dropdown khi bấm ra ngoài
  useEffect(() => {
    const handleClickOutside = (event) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
        setIsOpen(false);
      }
    };
    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [isOpen]);

  // Khi mở dropdown -> nạp danh sách thông báo mới
  const handleToggle = () => {
    if (!isOpen) {
      fetchRecentNotifications();
    }
    setIsOpen(!isOpen);
  };

  // Đánh dấu 1 thông báo là đã đọc và điều hướng thông minh
  const handleItemClick = async (notif) => {
    if (!notif.isRead) {
      try {
        await notificationApi.markAsRead(notif.id);
        setNotifications((prev) =>
          prev.map((n) => (n.id === notif.id ? { ...n, isRead: true } : n))
        );
        setUnreadCount((c) => Math.max(0, c - 1));
      } catch {
        // Tiếp tục chuyển trang dù patch ngầm thất bại
      }
    }
    setIsOpen(false);
    const targetRoute = getTargetRoute(notif);
    navigate(targetRoute);
  };

  // Đánh dấu tất cả là đã đọc
  const handleMarkAllRead = async (e) => {
    e.stopPropagation();
    try {
      await notificationApi.markAllAsRead();
      setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })));
      setUnreadCount(0);
      toast.success('Đã đánh dấu tất cả thông báo là đã đọc');
    } catch {
      toast.error('Lỗi khi đánh dấu đã đọc');
    }
  };

  // Render icon theo loại thông báo
  const getIcon = (type) => {
    switch (type) {
      case 1: // Warning
        return <AlertTriangle className="w-4 h-4 text-amber-500" />;
      case 2: // ContractExpiring
      case 3: // DeadlineApproaching
        return <Clock className="w-4 h-4 text-rose-500" />;
      case 4: // ProgressOverdue
        return <AlertCircle className="w-4 h-4 text-rose-600" />;
      case 0: // Info
      default:
        return <Info className="w-4 h-4 text-sky-500" />;
    }
  };

  return (
    <div className="relative" ref={dropdownRef}>
      {/* Nút bấm Chuông */}
      <button
        type="button"
        onClick={handleToggle}
        className="relative p-2 rounded-lg text-slate-500 hover:text-slate-800 hover:bg-slate-100 transition focus:outline-none"
        title="Thông báo"
        aria-label="Thông báo"
      >
        <Bell className="w-5 h-5" />
        {unreadCount > 0 && (
          <span className="absolute top-1 right-1 flex items-center justify-center min-w-[18px] h-[18px] px-1 text-[10px] font-bold text-white bg-rose-500 rounded-full border-2 border-white shadow-xs">
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}
      </button>

      {/* Dropdown Popover */}
      {isOpen && (
        <div className="absolute right-0 mt-2 w-80 sm:w-96 bg-white border border-slate-200 rounded-2xl shadow-xl z-50 overflow-hidden animate-in fade-in slide-in-from-top-2 duration-150">
          {/* Header */}
          <div className="px-4 py-3 bg-slate-50/80 border-b border-slate-100 flex items-center justify-between">
            <div className="flex items-center space-x-2">
              <span className="text-sm font-bold text-slate-800">Thông báo</span>
              {unreadCount > 0 && (
                <span className="px-2 py-0.5 text-[11px] font-semibold bg-rose-100 text-rose-700 rounded-full">
                  {unreadCount} mới
                </span>
              )}
            </div>
            {unreadCount > 0 && (
              <button
                type="button"
                onClick={handleMarkAllRead}
                className="text-xs text-sky-600 hover:text-sky-700 font-medium flex items-center space-x-1"
                title="Đánh dấu tất cả đã đọc"
              >
                <CheckCheck className="w-3.5 h-3.5" />
                <span>Đã đọc tất cả</span>
              </button>
            )}
          </div>

          {/* Body */}
          <div className="max-h-[380px] overflow-y-auto divide-y divide-slate-100">
            {loading ? (
              <div className="py-10 flex flex-col items-center justify-center text-slate-400 space-y-2">
                <Loader2 className="w-5 h-5 animate-spin text-sky-500" />
                <span className="text-xs">Đang tải thông báo...</span>
              </div>
            ) : notifications.length === 0 ? (
              <div className="py-10 flex flex-col items-center justify-center text-slate-400 space-y-2">
                <BellOff className="w-8 h-8 text-slate-300 stroke-[1.5]" />
                <p className="text-xs font-medium text-slate-500">Bạn chưa có thông báo nào</p>
              </div>
            ) : (
              notifications.map((n) => (
                <div
                  key={n.id}
                  onClick={() => handleItemClick(n)}
                  className={`px-4 py-3 hover:bg-slate-50 cursor-pointer transition flex items-start space-x-3 text-left ${
                    !n.isRead ? 'bg-sky-50/40' : ''
                  }`}
                >
                  <div className="mt-0.5 p-1.5 rounded-lg bg-slate-100 flex-shrink-0">
                    {getIcon(n.type)}
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center justify-between gap-2">
                      <p
                        className={`text-xs truncate ${
                          !n.isRead ? 'font-bold text-slate-900' : 'font-semibold text-slate-700'
                        }`}
                      >
                        {n.title}
                      </p>
                      {!n.isRead && (
                        <span className="w-2 h-2 rounded-full bg-sky-500 flex-shrink-0" />
                      )}
                    </div>
                    <p className="text-[11px] text-slate-500 line-clamp-2 mt-0.5 leading-relaxed">
                      {n.message}
                    </p>
                    <span className="text-[10px] text-slate-400 mt-1 block">
                      {formatRelativeTime(n.createdAt)}
                    </span>
                  </div>
                </div>
              ))
            )}
          </div>

          {/* Footer */}
          <div className="p-2.5 bg-slate-50/80 border-t border-slate-100 text-center">
            <button
              type="button"
              onClick={() => {
                setIsOpen(false);
                navigate('/notifications');
              }}
              className="w-full py-1.5 px-3 text-xs font-semibold text-slate-700 hover:text-sky-600 hover:bg-white rounded-lg transition flex items-center justify-center space-x-1"
            >
              <span>Xem tất cả thông báo</span>
              <ArrowRight className="w-3.5 h-3.5" />
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

export default NotificationBell;
