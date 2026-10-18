import React, { useState, useEffect, useRef } from 'react';
import {
  X,
  Shield,
  User,
  Globe,
  Database,
  Code2,
  Columns,
  Copy,
  Check,
  FileCheck2,
} from 'lucide-react';
import { formatDateTime, formatRelativeTime } from '../../utils/formatters';
import toast from 'react-hot-toast';

// Hàm lấy màu sắc và nhãn theo Action
const getActionBadge = (action) => {
  const upper = (action || '').toUpperCase();
  if (upper.includes('CREATE') || upper.includes('ADD') || upper.includes('REGISTER')) {
    return {
      bg: 'bg-emerald-50 text-emerald-700 border-emerald-200',
      label: action,
    };
  }
  if (upper.includes('UPDATE') || upper.includes('EDIT') || upper.includes('MODIFY')) {
    return {
      bg: 'bg-blue-50 text-blue-700 border-blue-200',
      label: action,
    };
  }
  if (upper.includes('VERIFY') || upper.includes('APPROVE') || upper.includes('FINALIZE') || upper.includes('AWARD')) {
    return {
      bg: 'bg-indigo-50 text-indigo-700 border-indigo-200',
      label: action,
    };
  }
  if (upper.includes('DELETE') || upper.includes('REVOKE') || upper.includes('DISQUALIFY') || upper.includes('REJECT')) {
    return {
      bg: 'bg-rose-50 text-rose-700 border-rose-200',
      label: action,
    };
  }
  return {
    bg: 'bg-slate-100 text-slate-700 border-slate-200',
    label: action || 'UNKNOWN',
  };
};

export const AuditLogDetailModal = ({ log, isOpen, onClose }) => {
  const [viewMode, setViewMode] = useState('diff'); // 'diff' | 'raw'
  const [copiedOld, setCopiedOld] = useState(false);
  const [copiedNew, setCopiedNew] = useState(false);

  // REV-06: Dọn dẹp timer bằng useRef khi unmount
  const copyOldTimeoutRef = useRef(null);
  const copyNewTimeoutRef = useRef(null);

  useEffect(() => {
    return () => {
      if (copyOldTimeoutRef.current) clearTimeout(copyOldTimeoutRef.current);
      if (copyNewTimeoutRef.current) clearTimeout(copyNewTimeoutRef.current);
    };
  }, []);

  // REV-07: Lắng nghe phím Escape để đóng modal
  useEffect(() => {
    if (!isOpen) return;
    const handleKeyDown = (e) => {
      if (e.key === 'Escape') {
        onClose();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => {
      window.removeEventListener('keydown', handleKeyDown);
    };
  }, [isOpen, onClose]);

  if (!isOpen || !log) return null;

  // REV-05: Kiểm tra chính xác Plain Object (loại trừ Array và null)
  const isPlainObject = (val) =>
    typeof val === 'object' && val !== null && !Array.isArray(val);

  let parsedOld = null;
  let parsedNew = null;
  let isOldObject = false;
  let isNewObject = false;

  if (log.oldValues) {
    try {
      parsedOld = JSON.parse(log.oldValues);
      isOldObject = isPlainObject(parsedOld);
    } catch {
      parsedOld = log.oldValues;
      isOldObject = false;
    }
  }

  if (log.newValues) {
    try {
      parsedNew = JSON.parse(log.newValues);
      isNewObject = isPlainObject(parsedNew);
    } catch {
      parsedNew = log.newValues;
      isNewObject = false;
    }
  }

  // Cho phép so sánh trường nếu cả 2 là Object hoặc 1 bên là Object và bên kia rỗng
  const canCompareFields =
    (isOldObject && isNewObject) ||
    (isOldObject && !log.newValues) ||
    (isNewObject && !log.oldValues);

  // Tập hợp danh sách các key nếu thỏa điều kiện
  const allKeys = canCompareFields
    ? Array.from(
        new Set([
          ...(isOldObject ? Object.keys(parsedOld) : []),
          ...(isNewObject ? Object.keys(parsedNew) : []),
        ])
      )
    : [];

  const handleCopy = (text, type) => {
    if (!text) return;
    navigator.clipboard.writeText(text);
    if (type === 'old') {
      setCopiedOld(true);
      if (copyOldTimeoutRef.current) clearTimeout(copyOldTimeoutRef.current);
      copyOldTimeoutRef.current = setTimeout(() => setCopiedOld(false), 2000);
    } else {
      setCopiedNew(true);
      if (copyNewTimeoutRef.current) clearTimeout(copyNewTimeoutRef.current);
      copyNewTimeoutRef.current = setTimeout(() => setCopiedNew(false), 2000);
    }
    toast.success('Đã sao chép vào bộ nhớ tạm');
  };

  const actionStyle = getActionBadge(log.action);

  const renderValue = (val) => {
    if (val === undefined || val === null) {
      return <span className="text-slate-400 italic">null</span>;
    }
    if (typeof val === 'boolean') {
      return (
        <span
          className={`font-semibold ${
            val ? 'text-emerald-600' : 'text-rose-600'
          }`}
        >
          {val ? 'true' : 'false'}
        </span>
      );
    }
    if (typeof val === 'object') {
      return (
        <pre className="text-xs bg-slate-100 p-1.5 rounded-md overflow-x-auto text-slate-800 font-mono">
          {JSON.stringify(val, null, 2)}
        </pre>
      );
    }
    return <span className="text-slate-800 break-all">{String(val)}</span>;
  };

  return (
    // REV-07: Click backdrop để đóng modal
    <div
      onClick={onClose}
      className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/50 backdrop-blur-xs flex items-center justify-center p-4 animate-in fade-in duration-150"
    >
      {/* REV-08: Thêm role="dialog", aria-modal="true", aria-labelledby */}
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="audit-detail-title"
        className="bg-white rounded-2xl max-w-4xl w-full max-h-[90vh] shadow-2xl border border-slate-200 flex flex-col overflow-hidden relative"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="px-6 py-4 bg-slate-50 border-b border-slate-200 flex items-center justify-between">
          <div className="flex items-center space-x-3">
            <div className="w-10 h-10 rounded-xl bg-slate-900 text-sky-400 flex items-center justify-center shadow-xs">
              <Shield className="w-5 h-5" />
            </div>
            <div>
              <div className="flex items-center space-x-2">
                <h3 id="audit-detail-title" className="text-base font-bold text-slate-900">
                  Chi tiết Nhật ký Kiểm toán #{log.id}
                </h3>
                <span
                  className={`text-xs px-2.5 py-0.5 rounded-full border font-semibold ${actionStyle.bg}`}
                >
                  {actionStyle.label}
                </span>
              </div>
              <p className="text-xs text-slate-500 mt-0.5">
                Ghi nhận vào {formatDateTime(log.timestamp)} ({formatRelativeTime(log.timestamp)})
              </p>
            </div>
          </div>

          {/* REV-08: aria-label="Đóng cửa sổ" */}
          <button
            type="button"
            onClick={onClose}
            aria-label="Đóng cửa sổ"
            className="p-1.5 rounded-lg text-slate-400 hover:text-slate-600 hover:bg-slate-200 transition"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Content Body */}
        <div className="p-6 overflow-y-auto space-y-6">
          {/* Metadata Cards */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            {/* Actor Card */}
            <div className="bg-slate-50/70 border border-slate-200 rounded-xl p-4 flex items-start space-x-3">
              <div className="p-2 rounded-lg bg-white border border-slate-200 text-slate-600 shadow-2xs">
                <User className="w-4 h-4 text-sky-600" />
              </div>
              <div className="min-w-0 flex-1">
                <p className="text-xs text-slate-500 font-medium">Người thực hiện</p>
                <p className="text-sm font-bold text-slate-900 truncate">
                  {log.userFullName || 'Hệ thống tự động'}
                </p>
                <p className="text-xs text-slate-500 truncate">
                  {log.userEmail || `User ID: ${log.userId ?? 'N/A'}`}
                </p>
              </div>
            </div>

            {/* Target Entity Card */}
            <div className="bg-slate-50/70 border border-slate-200 rounded-xl p-4 flex items-start space-x-3">
              <div className="p-2 rounded-lg bg-white border border-slate-200 text-slate-600 shadow-2xs">
                <Database className="w-4 h-4 text-indigo-600" />
              </div>
              <div className="min-w-0 flex-1">
                <p className="text-xs text-slate-500 font-medium">Thực thể bị tác động</p>
                <p className="text-sm font-bold text-slate-900 truncate">
                  {log.entityType || 'Chung'}
                </p>
                <p className="text-xs text-slate-600 font-mono">
                  {log.entityId ? `ID: #${log.entityId}` : 'Không có ID thực thể'}
                </p>
              </div>
            </div>

            {/* Network / Timestamp Card */}
            <div className="bg-slate-50/70 border border-slate-200 rounded-xl p-4 flex items-start space-x-3">
              <div className="p-2 rounded-lg bg-white border border-slate-200 text-slate-600 shadow-2xs">
                <Globe className="w-4 h-4 text-emerald-600" />
              </div>
              <div className="min-w-0 flex-1">
                <p className="text-xs text-slate-500 font-medium">Địa chỉ IP & Thiết bị</p>
                <p className="text-sm font-bold font-mono text-slate-800 truncate">
                  {log.ipAddress || 'Internal / N/A'}
                </p>
                <p className="text-xs text-slate-500">
                  {log.ipAddress === '::1' ? 'Localhost (IPv6)' : 'Máy khách'}
                </p>
              </div>
            </div>
          </div>

          {/* Diff / Value Comparison Section */}
          <div className="space-y-3">
            <div className="flex items-center justify-between border-b border-slate-200 pb-2">
              <div className="flex items-center space-x-2">
                <FileCheck2 className="w-4 h-4 text-slate-700" />
                <h4 className="text-sm font-bold text-slate-900">
                  Chi tiết thay đổi dữ liệu (Audit Trail Diff)
                </h4>
              </div>

              {/* View mode toggle */}
              <div className="flex items-center space-x-1 bg-slate-100 p-1 rounded-lg">
                <button
                  type="button"
                  onClick={() => setViewMode('diff')}
                  className={`flex items-center space-x-1.5 px-2.5 py-1 rounded-md text-xs font-semibold transition ${
                    viewMode === 'diff'
                      ? 'bg-white text-slate-900 shadow-2xs'
                      : 'text-slate-600 hover:text-slate-900'
                  }`}
                >
                  <Columns className="w-3.5 h-3.5" />
                  <span>So sánh trường</span>
                </button>
                <button
                  type="button"
                  onClick={() => setViewMode('raw')}
                  className={`flex items-center space-x-1.5 px-2.5 py-1 rounded-md text-xs font-semibold transition ${
                    viewMode === 'raw'
                      ? 'bg-white text-slate-900 shadow-2xs'
                      : 'text-slate-600 hover:text-slate-900'
                  }`}
                >
                  <Code2 className="w-3.5 h-3.5" />
                  <span>JSON Thô</span>
                </button>
              </div>
            </div>

            {/* View Mode: Field-by-Field Diff */}
            {viewMode === 'diff' && (
              <div>
                {canCompareFields && allKeys.length > 0 ? (
                  <div className="border border-slate-200 rounded-xl overflow-hidden shadow-2xs">
                    <table className="w-full text-left text-xs border-collapse">
                      <thead>
                        <tr className="bg-slate-100/80 border-b border-slate-200 text-slate-700">
                          <th className="py-2.5 px-4 font-bold w-1/4">Trường dữ liệu (Field)</th>
                          <th className="py-2.5 px-4 font-bold w-3/8 text-rose-700 bg-rose-50/50">
                            Trước thay đổi (Old Value)
                          </th>
                          <th className="py-2.5 px-4 font-bold w-3/8 text-emerald-700 bg-emerald-50/50">
                            Sau thay đổi (New Value)
                          </th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-slate-100 font-sans">
                        {allKeys.map((key) => {
                          const oldVal = isOldObject ? parsedOld[key] : undefined;
                          const newVal = isNewObject ? parsedNew[key] : undefined;
                          const isChanged =
                            JSON.stringify(oldVal) !== JSON.stringify(newVal);

                          return (
                            <tr
                              key={key}
                              className={`transition ${
                                isChanged
                                  ? 'bg-amber-50/30 hover:bg-amber-50/50'
                                  : 'hover:bg-slate-50'
                              }`}
                            >
                              <td className="py-2.5 px-4 font-semibold text-slate-800 font-mono text-xs align-top">
                                {key}
                                {isChanged && (
                                  <span className="ml-1.5 inline-block w-1.5 h-1.5 rounded-full bg-amber-500" />
                                )}
                              </td>
                              <td className="py-2.5 px-4 bg-rose-50/20 align-top">
                                {renderValue(oldVal)}
                              </td>
                              <td className="py-2.5 px-4 bg-emerald-50/20 align-top">
                                {renderValue(newVal)}
                              </td>
                            </tr>
                          );
                        })}
                      </tbody>
                    </table>
                  </div>
                ) : (
                  // REV-05: Hiển thị an toàn cho Array hoặc Text thuần không phải Object
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    {/* Old Values */}
                    <div className="border border-slate-200 rounded-xl p-4 bg-slate-50/50">
                      <p className="text-xs font-bold text-slate-700 mb-2">Trước thay đổi</p>
                      {log.oldValues ? (
                        <pre className="text-xs font-mono text-slate-800 whitespace-pre-wrap break-all bg-white p-3 rounded-lg border border-slate-200 max-h-60 overflow-y-auto">
                          {typeof parsedOld === 'object' && parsedOld !== null
                            ? JSON.stringify(parsedOld, null, 2)
                            : String(log.oldValues)}
                        </pre>
                      ) : (
                        <p className="text-xs text-slate-400 italic">
                          Không có dữ liệu (Hành động tạo mới hoặc không ghi nhận trạng thái cũ)
                        </p>
                      )}
                    </div>

                    {/* New Values */}
                    <div className="border border-slate-200 rounded-xl p-4 bg-slate-50/50">
                      <p className="text-xs font-bold text-slate-700 mb-2">Sau thay đổi</p>
                      {log.newValues ? (
                        <pre className="text-xs font-mono text-slate-800 whitespace-pre-wrap break-all bg-white p-3 rounded-lg border border-slate-200 max-h-60 overflow-y-auto">
                          {typeof parsedNew === 'object' && parsedNew !== null
                            ? JSON.stringify(parsedNew, null, 2)
                            : String(log.newValues)}
                        </pre>
                      ) : (
                        <p className="text-xs text-slate-400 italic">
                          Không có dữ liệu (Hành động xóa hoặc không ghi nhận trạng thái mới)
                        </p>
                      )}
                    </div>
                  </div>
                )}
              </div>
            )}

            {/* View Mode: Raw JSON */}
            {viewMode === 'raw' && (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {/* Old Values Raw */}
                <div className="border border-slate-200 rounded-xl overflow-hidden bg-slate-900 text-slate-200">
                  <div className="px-4 py-2 bg-slate-800/80 border-b border-slate-700 flex items-center justify-between">
                    <span className="text-xs font-bold text-rose-300">OldValues Raw</span>
                    {log.oldValues && (
                      <button
                        type="button"
                        onClick={() => handleCopy(log.oldValues, 'old')}
                        className="text-xs flex items-center space-x-1 text-slate-400 hover:text-white transition"
                      >
                        {copiedOld ? (
                          <Check className="w-3.5 h-3.5 text-emerald-400" />
                        ) : (
                          <Copy className="w-3.5 h-3.5" />
                        )}
                        <span>{copiedOld ? 'Đã chép' : 'Sao chép'}</span>
                      </button>
                    )}
                  </div>
                  <pre className="p-4 text-xs font-mono overflow-x-auto max-h-60 leading-relaxed">
                    {log.oldValues
                      ? typeof parsedOld === 'object' && parsedOld !== null
                        ? JSON.stringify(parsedOld, null, 2)
                        : log.oldValues
                      : '// Không có dữ liệu cũ'}
                  </pre>
                </div>

                {/* New Values Raw */}
                <div className="border border-slate-200 rounded-xl overflow-hidden bg-slate-900 text-slate-200">
                  <div className="px-4 py-2 bg-slate-800/80 border-b border-slate-700 flex items-center justify-between">
                    <span className="text-xs font-bold text-emerald-300">NewValues Raw</span>
                    {log.newValues && (
                      <button
                        type="button"
                        onClick={() => handleCopy(log.newValues, 'new')}
                        className="text-xs flex items-center space-x-1 text-slate-400 hover:text-white transition"
                      >
                        {copiedNew ? (
                          <Check className="w-3.5 h-3.5 text-emerald-400" />
                        ) : (
                          <Copy className="w-3.5 h-3.5" />
                        )}
                        <span>{copiedNew ? 'Đã chép' : 'Sao chép'}</span>
                      </button>
                    )}
                  </div>
                  <pre className="p-4 text-xs font-mono overflow-x-auto max-h-60 leading-relaxed">
                    {log.newValues
                      ? typeof parsedNew === 'object' && parsedNew !== null
                        ? JSON.stringify(parsedNew, null, 2)
                        : log.newValues
                      : '// Không có dữ liệu mới'}
                  </pre>
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Footer */}
        <div className="px-6 py-3.5 bg-slate-50 border-t border-slate-200 flex items-center justify-end">
          <button
            type="button"
            onClick={onClose}
            aria-label="Đóng cửa sổ chi tiết"
            className="px-5 py-2 bg-slate-900 hover:bg-slate-800 text-white text-xs font-semibold rounded-xl shadow-xs transition"
          >
            Đóng cửa sổ
          </button>
        </div>
      </div>
    </div>
  );
};

export default AuditLogDetailModal;
