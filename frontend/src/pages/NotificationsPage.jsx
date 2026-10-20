import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Bell,
  CheckCheck,
  Check,
  RefreshCw,
  AlertTriangle,
  Clock,
  AlertCircle,
  Info,
  BellOff,
  ChevronLeft,
  ChevronRight,
  ArrowRight
} from 'lucide-react';
import notificationApi from '../api/notificationApi';
import { formatDateTime, formatRelativeTime } from '../utils/formatters';
import toast from 'react-hot-toast';

export const NotificationsPage = () => {
  const navigate = useNavigate();
  const [notifications, setNotifications] = useState([]);
  const [loading, setLoading] = useState(true);
  const [unreadCount, setUnreadCount] = useState(0);
  const [filterType, setFilterType] = useState('all'); // 'all' | 'unread'
  const [pageIndex, setPageIndex] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const pageSize = 15;

  // Lấy liên kết điều hướng thông minh dựa vào nội dung thông báo
  const getActionLink = (notif) => {
    if (!notif) return null;
    const text = `${notif.title || ''} ${notif.message || ''}`.toLowerCase();
    if (text.includes('hợp đồng') || text.includes('tiến độ') || text.includes('nghiệm thu')) {
      return { path: '/contracts', label: 'Xem danh sách hợp đồng' };
    }
    if (text.includes('tổ chuyên gia') || text.includes('chấm thầu') || text.includes('xét thầu') || text.includes('giám khảo')) {
      return { path: '/evaluation', label: 'Đến trang chấm thầu' };
    }
    if (text.includes('gói thầu') || text.includes('dự thầu') || text.includes('mở thầu') || text.includes('đóng thầu')) {
      return { path: '/packages', label: 'Xem danh sách gói thầu' };
    }
    return null;
  };

  // Lấy dữ liệu thông báo
  const fetchNotifications = useCallback(async () => {
    setLoading(true);
    try {
      const [notifsRes, unreadRes] = await Promise.all([
        notificationApi.getNotifications(pageIndex, pageSize),
        notificationApi.getUnreadCount(),
      ]);

      if (notifsRes && notifsRes.success && notifsRes.data) {
        setNotifications(notifsRes.data.items || []);
        setTotalPages(notifsRes.data.totalPages || 1);
        setTotalCount(notifsRes.data.totalCount || 0);
      } else if (notifsRes?.items) {
        setNotifications(notifsRes.items || []);
        setTotalPages(notifsRes.totalPages || 1);
        setTotalCount(notifsRes.totalCount || 0);
      }

      if (unreadRes && unreadRes.success && typeof unreadRes.data === 'number') {
        setUnreadCount(unreadRes.data);
      } else if (typeof unreadRes === 'number') {
        setUnreadCount(unreadRes);
      }
    } catch {
      toast.error('Không thể tải danh sách thông báo');
    } finally {
      setLoading(false);
    }
  }, [pageIndex, pageSize]);

  useEffect(() => {
    fetchNotifications();
  }, [fetchNotifications]);

  // Đánh dấu 1 thông báo là đã đọc
  const handleMarkAsRead = async (id) => {
    try {
      const res = await notificationApi.markAsRead(id);
      if (res?.success || res === true) {
        setNotifications((prev) =>
          prev.map((n) => (n.id === id ? { ...n, isRead: true } : n))
        );
        setUnreadCount((c) => Math.max(0, c - 1));
        toast.success('Đã đánh dấu thông báo là đã đọc');
      }
    } catch {
      toast.error('Lỗi khi cập nhật trạng thái thông báo');
    }
  };

  // Đánh dấu tất cả là đã đọc
  const handleMarkAllAsRead = async () => {
    try {
      const res = await notificationApi.markAllAsRead();
      if (res?.success || res === true) {
        setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })));
        setUnreadCount(0);
        toast.success('Đã đánh dấu tất cả thông báo là đã đọc');
      }
    } catch {
      toast.error('Lỗi khi đánh dấu tất cả thông báo');
    }
  };

  // Lọc thông báo hiển thị
  const filteredNotifications = notifications.filter((n) => {
    if (filterType === 'unread') return !n.isRead;
    return true;
  });

  // Render icon & badge theo loại thông báo
  const renderTypeInfo = (type) => {
    switch (type) {
      case 1: // Warning
        return {
          icon: <AlertTriangle className="w-5 h-5 text-amber-600" />,
          bg: 'bg-amber-50 border-amber-200 text-amber-800',
          badgeText: 'Cảnh báo',
          dotBg: 'bg-amber-500',
        };
      case 2: // ContractExpiring
        return {
          icon: <Clock className="w-5 h-5 text-rose-600" />,
          bg: 'bg-rose-50 border-rose-200 text-rose-800',
          badgeText: 'Hợp đồng sắp hết hạn',
          dotBg: 'bg-rose-500',
        };
      case 3: // DeadlineApproaching
        return {
          icon: <Clock className="w-5 h-5 text-amber-600" />,
          bg: 'bg-amber-50 border-amber-200 text-amber-800',
          badgeText: 'Sắp hết hạn thầu',
          dotBg: 'bg-amber-500',
        };
      case 4: // ProgressOverdue
        return {
          icon: <AlertCircle className="w-5 h-5 text-rose-600" />,
          bg: 'bg-rose-50 border-rose-200 text-rose-800',
          badgeText: 'Quá hạn tiến độ',
          dotBg: 'bg-rose-500',
        };
      case 0: // Info
      default:
        return {
          icon: <Info className="w-5 h-5 text-sky-600" />,
          bg: 'bg-sky-50 border-sky-200 text-sky-800',
          badgeText: 'Thông báo',
          dotBg: 'bg-sky-500',
        };
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="bg-white rounded-2xl border border-slate-200 p-6 shadow-xs flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div className="flex items-center space-x-3.5">
          <div className="w-12 h-12 rounded-xl bg-sky-50 text-sky-600 flex items-center justify-center border border-sky-100 flex-shrink-0">
            <Bell className="w-6 h-6" />
          </div>
          <div>
            <div className="flex items-center space-x-2.5">
              <h1 className="text-xl font-bold text-slate-900 tracking-tight">
                Trung tâm Thông báo
              </h1>
              {unreadCount > 0 && (
                <span className="px-2.5 py-0.5 text-xs font-semibold bg-rose-500 text-white rounded-full">
                  {unreadCount} chưa đọc
                </span>
              )}
            </div>
            <p className="text-xs text-slate-500 mt-1">
              Quản lý toàn bộ thông báo nghiệp vụ, kết quả xét thầu và cảnh báo thời hạn tự động
            </p>
          </div>
        </div>

        {/* Nút hành động */}
        <div className="flex items-center space-x-2">
          <button
            type="button"
            onClick={fetchNotifications}
            className="inline-flex items-center space-x-1.5 px-3.5 py-2 text-xs font-semibold text-slate-700 bg-white border border-slate-200 rounded-xl hover:bg-slate-50 transition shadow-xs"
            title="Làm mới"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />
            <span>Làm mới</span>
          </button>
          {unreadCount > 0 && (
            <button
              type="button"
              onClick={handleMarkAllAsRead}
              className="inline-flex items-center space-x-1.5 px-3.5 py-2 text-xs font-semibold text-sky-700 bg-sky-50 border border-sky-200 rounded-xl hover:bg-sky-100 transition shadow-xs"
              title="Đánh dấu tất cả đã đọc"
            >
              <CheckCheck className="w-4 h-4" />
              <span>Đã đọc tất cả</span>
            </button>
          )}
        </div>
      </div>

      {/* Bộ lọc tab */}
      <div className="flex items-center justify-between border-b border-slate-200 pb-2">
        <div className="flex items-center space-x-2">
          <button
            type="button"
            onClick={() => setFilterType('all')}
            className={`px-4 py-2 text-xs font-semibold rounded-lg transition ${
              filterType === 'all'
                ? 'bg-slate-900 text-white shadow-xs'
                : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
            }`}
          >
            Tất cả ({totalCount})
          </button>
          <button
            type="button"
            onClick={() => setFilterType('unread')}
            className={`px-4 py-2 text-xs font-semibold rounded-lg transition flex items-center space-x-1.5 ${
              filterType === 'unread'
                ? 'bg-slate-900 text-white shadow-xs'
                : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
            }`}
          >
            <span>Chưa đọc</span>
            {unreadCount > 0 && (
              <span
                className={`px-1.5 py-0.2 rounded-full text-[10px] font-bold ${
                  filterType === 'unread' ? 'bg-rose-500 text-white' : 'bg-rose-100 text-rose-700'
                }`}
              >
                {unreadCount}
              </span>
            )}
          </button>
        </div>

        <span className="text-xs text-slate-400 hidden sm:inline">
          Trang {pageIndex} / {totalPages || 1}
        </span>
      </div>

      {/* Danh sách thông báo */}
      {loading ? (
        <div className="bg-white rounded-2xl border border-slate-200 p-12 text-center">
          <div className="inline-block animate-spin w-8 h-8 border-4 border-sky-500 border-t-transparent rounded-full mb-3" />
          <p className="text-xs text-slate-500 font-medium">Đang tải danh sách thông báo...</p>
        </div>
      ) : filteredNotifications.length === 0 ? (
        <div className="bg-white rounded-2xl border border-slate-200 p-16 text-center shadow-xs">
          <div className="w-16 h-16 rounded-2xl bg-slate-50 text-slate-400 mx-auto flex items-center justify-center mb-4">
            <BellOff className="w-8 h-8 stroke-[1.5]" />
          </div>
          <h3 className="text-sm font-bold text-slate-800">Không có thông báo nào</h3>
          <p className="text-xs text-slate-400 mt-1 max-w-sm mx-auto">
            {filterType === 'unread'
              ? 'Tuyệt vời! Bạn đã đọc hết tất cả các thông báo trong hệ thống.'
              : 'Hiện tại bạn chưa nhận được thông báo mới nào từ hệ thống.'}
          </p>
        </div>
      ) : (
        <div className="space-y-3">
          {filteredNotifications.map((notif) => {
            const typeInfo = renderTypeInfo(notif.type);
            const actionLink = getActionLink(notif);
            return (
              <div
                key={notif.id}
                className={`bg-white rounded-2xl border p-5 transition shadow-xs flex items-start space-x-4 ${
                  !notif.isRead
                    ? 'border-sky-300 bg-sky-50/20'
                    : 'border-slate-200 hover:border-slate-300'
                }`}
              >
                {/* Icon phân loại */}
                <div className="p-2.5 rounded-xl bg-slate-50 border border-slate-100 flex-shrink-0 mt-0.5">
                  {typeInfo.icon}
                </div>

                {/* Nội dung thông báo */}
                <div className="flex-1 min-w-0">
                  <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-1">
                    <div className="flex items-center space-x-2">
                      <h4
                        className={`text-sm tracking-tight ${
                          !notif.isRead ? 'font-bold text-slate-900' : 'font-semibold text-slate-800'
                        }`}
                      >
                        {notif.title}
                      </h4>
                      <span
                        className={`text-[10px] font-semibold px-2 py-0.5 rounded-md border ${typeInfo.bg}`}
                      >
                        {typeInfo.badgeText}
                      </span>
                      {!notif.isRead && (
                        <span className="w-2 h-2 rounded-full bg-sky-500 inline-block" />
                      )}
                    </div>
                    <span
                      className="text-[11px] text-slate-400 sm:text-right"
                      title={formatDateTime(notif.createdAt)}
                    >
                      {formatRelativeTime(notif.createdAt)} ({formatDateTime(notif.createdAt)})
                    </span>
                  </div>

                  <p className="text-xs text-slate-600 mt-2 leading-relaxed whitespace-pre-line">
                    {notif.message}
                  </p>

                  {/* Hành động */}
                  <div className="mt-3.5 flex flex-wrap items-center gap-2">
                    {!notif.isRead && (
                      <button
                        type="button"
                        onClick={() => handleMarkAsRead(notif.id)}
                        className="inline-flex items-center space-x-1 px-2.5 py-1 text-[11px] font-semibold text-slate-600 hover:text-sky-600 bg-slate-100 hover:bg-sky-50 rounded-lg transition"
                      >
                        <Check className="w-3.5 h-3.5" />
                        <span>Đánh dấu đã đọc</span>
                      </button>
                    )}
                    {actionLink && (
                      <button
                        type="button"
                        onClick={() => {
                          if (!notif.isRead) {
                            handleMarkAsRead(notif.id);
                          }
                          navigate(actionLink.path);
                        }}
                        className="inline-flex items-center space-x-1 px-2.5 py-1 text-[11px] font-semibold text-sky-700 bg-sky-50 hover:bg-sky-100 rounded-lg transition"
                      >
                        <span>{actionLink.label}</span>
                        <ArrowRight className="w-3 h-3" />
                      </button>
                    )}
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Phân trang */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between bg-white rounded-xl border border-slate-200 px-4 py-3 shadow-xs">
          <span className="text-xs text-slate-500">
            Hiển thị trang {pageIndex} / {totalPages} (Tổng cộng {totalCount} thông báo)
          </span>
          <div className="flex items-center space-x-2">
            <button
              type="button"
              disabled={pageIndex <= 1}
              onClick={() => setPageIndex((p) => Math.max(1, p - 1))}
              className="p-1.5 rounded-lg border border-slate-200 text-slate-600 hover:bg-slate-50 disabled:opacity-40 disabled:cursor-not-allowed"
            >
              <ChevronLeft className="w-4 h-4" />
            </button>
            <span className="text-xs font-semibold text-slate-800 px-2">{pageIndex}</span>
            <button
              type="button"
              disabled={pageIndex >= totalPages}
              onClick={() => setPageIndex((p) => Math.min(totalPages, p + 1))}
              className="p-1.5 rounded-lg border border-slate-200 text-slate-600 hover:bg-slate-50 disabled:opacity-40 disabled:cursor-not-allowed"
            >
              <ChevronRight className="w-4 h-4" />
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

export default NotificationsPage;
