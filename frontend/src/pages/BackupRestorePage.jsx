import React, { useState, useEffect, useCallback } from 'react';
import {
  Database,
  Archive,
  Download,
  RotateCcw,
  Trash2,
  Plus,
  RefreshCw,
  HardDrive,
  Calendar,
  ShieldCheck,
  AlertTriangle,
  CheckCircle2,
  FileCode,
  Copy,
  Clock,
  Loader2,
  Terminal,
} from 'lucide-react';
import backupApi from '../api/backupApi';
import { formatDateTime } from '../utils/formatters';
import toast from 'react-hot-toast';

export const BackupRestorePage = () => {
  const [loading, setLoading] = useState(true);
  const [overview, setOverview] = useState(null);
  const [activeTab, setActiveTab] = useState('database'); // 'database' | 'documents' | 'automation'

  // Action states
  const [creatingDb, setCreatingDb] = useState(false);
  const [creatingDocs, setCreatingDocs] = useState(false);
  const [deletingFile, setDeletingFile] = useState(null);

  // Restore Modal State
  const [restoreModalOpen, setRestoreModalOpen] = useState(false);
  const [selectedBackupForRestore, setSelectedBackupForRestore] = useState(null);
  const [confirmPhraseInput, setConfirmPhraseInput] = useState('');
  const [restoring, setRestoring] = useState(false);

  // Create Modal State
  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [backupDescription, setBackupDescription] = useState('');
  const [createType, setCreateType] = useState('database'); // 'database' | 'documents'

  const fetchOverview = useCallback(async () => {
    try {
      setLoading(true);
      const res = await backupApi.getOverview();
      if (res?.data?.success) {
        setOverview(res.data.data);
      } else {
        toast.error(res?.data?.message || 'Không thể tải thông tin bản sao lưu.');
      }
    } catch (err) {
      toast.error('Lỗi khi tải thông tin bản sao lưu: ' + (err.response?.data?.message || err.message));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchOverview();
  }, [fetchOverview]);

  // Handle Create Backup
  const handleOpenCreateModal = (type) => {
    setCreateType(type);
    setBackupDescription('');
    setCreateModalOpen(true);
  };

  const handleConfirmCreateBackup = async () => {
    setCreateModalOpen(false);
    if (createType === 'database') {
      try {
        setCreatingDb(true);
        toast.loading('Đang khởi tạo bản sao lưu CSDL...', { id: 'create-backup' });
        const res = await backupApi.createDatabaseBackup({ description: backupDescription });
        if (res?.data?.success) {
          toast.success(res.data.message || 'Tạo bản sao lưu CSDL thành công!', { id: 'create-backup' });
          fetchOverview();
        } else {
          toast.error(res?.data?.message || 'Sao lưu thất bại.', { id: 'create-backup' });
        }
      } catch (err) {
        toast.error('Lỗi khi sao lưu CSDL: ' + (err.response?.data?.message || err.message), { id: 'create-backup' });
      } finally {
        setCreatingDb(false);
      }
    } else {
      try {
        setCreatingDocs(true);
        toast.loading('Đang nén thư mục tệp đính kèm uploads...', { id: 'create-docs' });
        const res = await backupApi.createDocumentsBackup({ description: backupDescription });
        if (res?.data?.success) {
          toast.success(res.data.message || 'Tạo bản sao lưu tệp đính kèm thành công!', { id: 'create-docs' });
          fetchOverview();
        } else {
          toast.error(res?.data?.message || 'Sao lưu tệp thất bại.', { id: 'create-docs' });
        }
      } catch (err) {
        toast.error('Lỗi khi nén tệp đính kèm: ' + (err.response?.data?.message || err.message), { id: 'create-docs' });
      } finally {
        setCreatingDocs(false);
      }
    }
  };

  // Handle Download Backup
  const handleDownload = async (fileName) => {
    try {
      toast.loading(`Đang chuẩn bị tải về ${fileName}...`, { id: 'download' });
      const response = await backupApi.downloadBackup(fileName);
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', fileName);
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
      toast.success('Bắt đầu tải về tệp sao lưu!', { id: 'download' });
    } catch (err) {
      toast.error('Không thể tải về tệp: ' + (err.response?.data?.message || err.message), { id: 'download' });
    }
  };

  // Handle Delete Backup
  const handleDelete = async (fileName) => {
    if (!window.confirm(`Bạn có chắc chắn muốn xóa tệp sao lưu ${fileName} vĩnh viễn khỏi máy chủ?`)) {
      return;
    }

    try {
      setDeletingFile(fileName);
      const res = await backupApi.deleteBackup(fileName);
      if (res?.data?.success) {
        toast.success(res.data.message || `Đã xóa tệp ${fileName}`);
        fetchOverview();
      } else {
        toast.error(res?.data?.message || 'Xóa tệp thất bại.');
      }
    } catch (err) {
      toast.error('Lỗi khi xóa tệp: ' + (err.response?.data?.message || err.message));
    } finally {
      setDeletingFile(null);
    }
  };

  // Handle Restore
  const handleOpenRestoreModal = (backup) => {
    setSelectedBackupForRestore(backup);
    setConfirmPhraseInput('');
    setRestoreModalOpen(true);
  };

  const handleConfirmRestore = async () => {
    if (confirmPhraseInput.trim() !== 'RESTORE_DATABASE_CONFIRM') {
      toast.error('Vui lòng nhập chính xác cụm từ: RESTORE_DATABASE_CONFIRM');
      return;
    }

    try {
      setRestoring(true);
      toast.loading('Đang tiến hành phục hồi cơ sở dữ liệu. Quá trình có thể mất ít phút...', { id: 'restore' });
      const res = await backupApi.restoreDatabaseBackup({
        fileName: selectedBackupForRestore.fileName,
        confirmPhrase: confirmPhraseInput.trim(),
      });

      if (res?.data?.success) {
        toast.success(res.data.message || 'Phục hồi cơ sở dữ liệu thành công!', { id: 'restore', duration: 6000 });
        setRestoreModalOpen(false);
        fetchOverview();
      } else {
        toast.error(res?.data?.message || 'Phục hồi cơ sở dữ liệu thất bại.', { id: 'restore', duration: 8000 });
      }
    } catch (err) {
      toast.error('Lỗi khi phục hồi CSDL: ' + (err.response?.data?.message || err.message), { id: 'restore', duration: 8000 });
    } finally {
      setRestoring(false);
    }
  };

  const handleCopy = (text, label) => {
    navigator.clipboard.writeText(text);
    toast.success(`Đã sao chép ${label}!`);
  };

  const dbBackups = overview?.databaseBackups || [];
  const docBackups = overview?.documentBackups || [];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <div className="flex items-center gap-2">
            <div className="p-2 bg-blue-50 text-blue-600 rounded-lg">
              <Database className="w-5 h-5" />
            </div>
            <h1 className="text-xl font-bold text-slate-800">Quản lý Sao lưu & Phục hồi</h1>
          </div>
          <p className="text-xs text-slate-500 mt-1">
            Sao lưu toàn vẹn CSDL SQL Server và kho tài liệu tải lên phục vụ vận hành, bảo mật và phục hồi thảm họa.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={fetchOverview}
            disabled={loading}
            className="flex items-center gap-1.5 px-3 py-2 bg-white border border-slate-200 text-slate-700 text-xs font-semibold rounded-lg hover:bg-slate-50 transition"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />
            Làm mới
          </button>
        </div>
      </div>

      {/* Top Stats Overview */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm flex items-center justify-between">
          <div>
            <p className="text-xs font-medium text-slate-500">Tổng bản sao lưu</p>
            <p className="text-xl font-bold text-slate-800 mt-1">{overview?.totalBackups ?? 0}</p>
            <p className="text-[11px] text-slate-400 mt-0.5">CSDL: {dbBackups.length} | Tệp: {docBackups.length}</p>
          </div>
          <div className="p-3 bg-blue-50 text-blue-600 rounded-xl">
            <Archive className="w-5 h-5" />
          </div>
        </div>

        <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm flex items-center justify-between">
          <div>
            <p className="text-xs font-medium text-slate-500">Tổng dung lượng sao lưu</p>
            <p className="text-xl font-bold text-slate-800 mt-1">{overview?.totalSizeFormatted ?? '0 B'}</p>
            <p className="text-[11px] text-slate-400 mt-0.5">Lưu trữ cục bộ trên ổ đĩa</p>
          </div>
          <div className="p-3 bg-indigo-50 text-indigo-600 rounded-xl">
            <HardDrive className="w-5 h-5" />
          </div>
        </div>

        <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm flex items-center justify-between">
          <div>
            <p className="text-xs font-medium text-slate-500">Lần sao lưu gần nhất</p>
            <p className="text-xs font-bold text-slate-800 mt-1.5">
              {overview?.lastBackupAt ? formatDateTime(overview.lastBackupAt) : 'Chưa có bản sao lưu'}
            </p>
            <p className="text-[11px] text-slate-400 mt-0.5">Tự động đối chiếu mã băm</p>
          </div>
          <div className="p-3 bg-emerald-50 text-emerald-600 rounded-xl">
            <Clock className="w-5 h-5" />
          </div>
        </div>

        <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm flex items-center justify-between">
          <div>
            <p className="text-xs font-medium text-slate-500">Cơ chế bảo vệ toàn vẹn</p>
            <div className="flex items-center gap-1.5 mt-1">
              <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-semibold bg-emerald-100 text-emerald-800">
                <CheckCircle2 className="w-3 h-3 mr-1" />
                Checksum & Verify
              </span>
            </div>
            <p className="text-[11px] text-slate-400 mt-0.5">Kiểm tra hợp lệ trước khi ghi</p>
          </div>
          <div className="p-3 bg-purple-50 text-purple-600 rounded-xl">
            <ShieldCheck className="w-5 h-5" />
          </div>
        </div>
      </div>

      {/* Tabs */}
      <div className="border-b border-slate-200">
        <nav className="flex space-x-6 text-xs font-semibold">
          <button
            onClick={() => setActiveTab('database')}
            className={`pb-3 flex items-center gap-2 border-b-2 transition ${
              activeTab === 'database'
                ? 'border-blue-600 text-blue-600'
                : 'border-transparent text-slate-500 hover:text-slate-700'
            }`}
          >
            <Database className="w-4 h-4" />
            Cơ sở dữ liệu ({dbBackups.length})
          </button>
          <button
            onClick={() => setActiveTab('documents')}
            className={`pb-3 flex items-center gap-2 border-b-2 transition ${
              activeTab === 'documents'
                ? 'border-blue-600 text-blue-600'
                : 'border-transparent text-slate-500 hover:text-slate-700'
            }`}
          >
            <Archive className="w-4 h-4" />
            Tệp đính kèm Uploads ({docBackups.length})
          </button>
          <button
            onClick={() => setActiveTab('automation')}
            className={`pb-3 flex items-center gap-2 border-b-2 transition ${
              activeTab === 'automation'
                ? 'border-blue-600 text-blue-600'
                : 'border-transparent text-slate-500 hover:text-slate-700'
            }`}
          >
            <Terminal className="w-4 h-4" />
            Tự động hóa định kỳ (Scheduled Job)
          </button>
        </nav>
      </div>

      {/* Tab 1: Database Backups */}
      {activeTab === 'database' && (
        <div className="space-y-4">
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 bg-slate-50 p-4 rounded-xl border border-slate-200">
            <div>
              <h2 className="text-sm font-bold text-slate-800">Danh sách bản sao lưu CSDL SQL Server</h2>
              <p className="text-xs text-slate-500 mt-0.5">
                Các tệp sao lưu (.bak) được nén và kiểm định toàn vẹn bằng lệnh RESTORE VERIFYONLY.
              </p>
            </div>
            <button
              onClick={() => handleOpenCreateModal('database')}
              disabled={creatingDb}
              className="flex items-center justify-center gap-1.5 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-xs font-semibold rounded-lg shadow-sm transition disabled:opacity-50"
            >
              {creatingDb ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Plus className="w-3.5 h-3.5" />}
              Tạo bản sao lưu CSDL ngay
            </button>
          </div>

          <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full text-left text-xs">
                <thead className="bg-slate-50 border-b border-slate-200 text-slate-600 uppercase font-bold text-[11px]">
                  <tr>
                    <th className="px-4 py-3">Tên tệp sao lưu</th>
                    <th className="px-4 py-3">Thời gian tạo</th>
                    <th className="px-4 py-3">Dung lượng</th>
                    <th className="px-4 py-3">Mã băm SHA-256</th>
                    <th className="px-4 py-3 text-right">Thao tác</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {dbBackups.length === 0 ? (
                    <tr>
                      <td colSpan="5" className="px-4 py-8 text-center text-slate-400">
                        Chưa có bản sao lưu cơ sở dữ liệu nào. Nhấn "Tạo bản sao lưu CSDL ngay" để tạo bản đầu tiên.
                      </td>
                    </tr>
                  ) : (
                    dbBackups.map((b) => (
                      <tr key={b.fileName} className="hover:bg-slate-50/80 transition">
                        <td className="px-4 py-3 font-medium text-slate-800 flex items-center gap-2">
                          <Database className="w-4 h-4 text-blue-500 flex-shrink-0" />
                          <div>
                            <span>{b.fileName}</span>
                            {b.description && (
                              <p className="text-[11px] text-slate-400 font-normal">{b.description}</p>
                            )}
                          </div>
                        </td>
                        <td className="px-4 py-3 text-slate-600">
                          {formatDateTime(b.createdAt)}
                        </td>
                        <td className="px-4 py-3 font-semibold text-slate-700">
                          {b.fileSizeFormatted}
                        </td>
                        <td className="px-4 py-3 font-mono text-[11px] text-slate-500">
                          <div className="flex items-center gap-1.5">
                            <span className="truncate max-w-[120px]">{b.sha256Hash || 'N/A'}</span>
                            {b.sha256Hash && (
                              <button
                                onClick={() => handleCopy(b.sha256Hash, 'Mã băm SHA-256')}
                                className="text-slate-400 hover:text-slate-600 p-0.5 rounded"
                                title="Sao chép toàn bộ mã băm SHA-256"
                              >
                                <Copy className="w-3 h-3" />
                              </button>
                            )}
                          </div>
                        </td>
                        <td className="px-4 py-3 text-right">
                          <div className="flex items-center justify-end gap-1.5">
                            <button
                              onClick={() => handleDownload(b.fileName)}
                              className="p-1.5 text-blue-600 hover:bg-blue-50 rounded-lg transition"
                              title="Tải về tệp .bak"
                            >
                              <Download className="w-4 h-4" />
                            </button>
                            <button
                              onClick={() => handleOpenRestoreModal(b)}
                              className="p-1.5 text-amber-600 hover:bg-amber-50 rounded-lg transition"
                              title="Phục hồi cơ sở dữ liệu từ bản này"
                            >
                              <RotateCcw className="w-4 h-4" />
                            </button>
                            <button
                              onClick={() => handleDelete(b.fileName)}
                              disabled={deletingFile === b.fileName}
                              className="p-1.5 text-rose-600 hover:bg-rose-50 rounded-lg transition disabled:opacity-50"
                              title="Xóa bản sao lưu"
                            >
                              {deletingFile === b.fileName ? (
                                <Loader2 className="w-4 h-4 animate-spin" />
                              ) : (
                                <Trash2 className="w-4 h-4" />
                              )}
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* Tab 2: Document Backups */}
      {activeTab === 'documents' && (
        <div className="space-y-4">
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 bg-slate-50 p-4 rounded-xl border border-slate-200">
            <div>
              <h2 className="text-sm font-bold text-slate-800">Danh sách bản sao lưu Tệp đính kèm (Uploads ZIP)</h2>
              <p className="text-xs text-slate-500 mt-0.5">
                Các bản nén toàn bộ thư mục uploads bao gồm hồ sơ năng lực, HSMT, HSDT, bản scan hợp đồng và bảo lãnh.
              </p>
            </div>
            <button
              onClick={() => handleOpenCreateModal('documents')}
              disabled={creatingDocs}
              className="flex items-center justify-center gap-1.5 px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-xs font-semibold rounded-lg shadow-sm transition disabled:opacity-50"
            >
              {creatingDocs ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Plus className="w-3.5 h-3.5" />}
              Tạo bản sao lưu tệp (ZIP)
            </button>
          </div>

          <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full text-left text-xs">
                <thead className="bg-slate-50 border-b border-slate-200 text-slate-600 uppercase font-bold text-[11px]">
                  <tr>
                    <th className="px-4 py-3">Tên tệp nén</th>
                    <th className="px-4 py-3">Thời gian tạo</th>
                    <th className="px-4 py-3">Dung lượng</th>
                    <th className="px-4 py-3">Mã băm SHA-256</th>
                    <th className="px-4 py-3 text-right">Thao tác</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {docBackups.length === 0 ? (
                    <tr>
                      <td colSpan="5" className="px-4 py-8 text-center text-slate-400">
                        Chưa có bản sao lưu tệp đính kèm nào. Nhấn "Tạo bản sao lưu tệp (ZIP)" để tạo bản đầu tiên.
                      </td>
                    </tr>
                  ) : (
                    docBackups.map((b) => (
                      <tr key={b.fileName} className="hover:bg-slate-50/80 transition">
                        <td className="px-4 py-3 font-medium text-slate-800 flex items-center gap-2">
                          <Archive className="w-4 h-4 text-indigo-500 flex-shrink-0" />
                          <div>
                            <span>{b.fileName}</span>
                            {b.description && (
                              <p className="text-[11px] text-slate-400 font-normal">{b.description}</p>
                            )}
                          </div>
                        </td>
                        <td className="px-4 py-3 text-slate-600">
                          {formatDateTime(b.createdAt)}
                        </td>
                        <td className="px-4 py-3 font-semibold text-slate-700">
                          {b.fileSizeFormatted}
                        </td>
                        <td className="px-4 py-3 font-mono text-[11px] text-slate-500">
                          <div className="flex items-center gap-1.5">
                            <span className="truncate max-w-[120px]">{b.sha256Hash || 'N/A'}</span>
                            {b.sha256Hash && (
                              <button
                                onClick={() => handleCopy(b.sha256Hash, 'Mã băm SHA-256')}
                                className="text-slate-400 hover:text-slate-600 p-0.5 rounded"
                                title="Sao chép mã băm"
                              >
                                <Copy className="w-3 h-3" />
                              </button>
                            )}
                          </div>
                        </td>
                        <td className="px-4 py-3 text-right">
                          <div className="flex items-center justify-end gap-1.5">
                            <button
                              onClick={() => handleDownload(b.fileName)}
                              className="p-1.5 text-blue-600 hover:bg-blue-50 rounded-lg transition"
                              title="Tải về tệp .zip"
                            >
                              <Download className="w-4 h-4" />
                            </button>
                            <button
                              onClick={() => handleDelete(b.fileName)}
                              disabled={deletingFile === b.fileName}
                              className="p-1.5 text-rose-600 hover:bg-rose-50 rounded-lg transition disabled:opacity-50"
                              title="Xóa bản sao lưu"
                            >
                              {deletingFile === b.fileName ? (
                                <Loader2 className="w-4 h-4 animate-spin" />
                              ) : (
                                <Trash2 className="w-4 h-4" />
                              )}
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* Tab 3: Automation & Scheduled Job Runbook */}
      {activeTab === 'automation' && (
        <div className="space-y-4">
          <div className="bg-white p-6 rounded-xl border border-slate-200 shadow-sm space-y-4">
            <div className="flex items-center gap-2">
              <Terminal className="w-5 h-5 text-blue-600" />
              <h2 className="text-sm font-bold text-slate-800">Cấu hình Tự động sao lưu định kỳ (Scheduled Backup Runbook)</h2>
            </div>
            <p className="text-xs text-slate-600 leading-relaxed">
              Hệ thống đã chuẩn bị sẵn kịch bản PowerShell tự động tại{' '}
              <code className="px-2 py-0.5 bg-slate-100 rounded text-blue-700 font-mono text-[11px]">
                scripts/database-backup-cron.ps1
              </code>{' '}
              hỗ trợ sao lưu toàn vẹn CSDL SQL Server, nén thư mục uploads, tự động dọn dẹp các bản sao lưu cũ quá 14 ngày và ghi nhật ký hoạt động.
            </p>

            <div className="border border-slate-200 rounded-lg overflow-hidden">
              <div className="bg-slate-800 text-slate-200 px-4 py-2 flex items-center justify-between text-xs font-mono">
                <span>Lệnh chạy thủ công hoặc kiểm tra thử nghiệm (PowerShell)</span>
                <button
                  onClick={() =>
                    handleCopy(
                      'powershell -ExecutionPolicy Bypass -File scripts/database-backup-cron.ps1 -RetentionDays 14',
                      'Lệnh thực thi'
                    )
                  }
                  className="flex items-center gap-1 text-slate-300 hover:text-white transition"
                >
                  <Copy className="w-3.5 h-3.5" />
                  Sao chép
                </button>
              </div>
              <pre className="p-4 bg-slate-900 text-emerald-400 font-mono text-xs overflow-x-auto">
                powershell -ExecutionPolicy Bypass -File scripts/database-backup-cron.ps1 -RetentionDays 14
              </pre>
            </div>

            <div className="space-y-2 pt-2">
              <h3 className="text-xs font-bold text-slate-700 uppercase">Cài đặt lịch chạy tự động trên Windows Task Scheduler:</h3>
              <ol className="list-decimal list-inside text-xs text-slate-600 space-y-1.5">
                <li>Mở <strong>Task Scheduler</strong> trên máy chủ hoặc máy phát triển.</li>
                <li>Chọn <strong>Create Basic Task...</strong>, đặt tên là <code className="bg-slate-100 px-1 py-0.5 rounded text-slate-800 font-mono">ProcurementSystem_Daily_Backup</code>.</li>
                <li>Thiết lập tần suất <strong>Daily</strong> vào lúc <strong>00:00</strong> đêm hàng ngày.</li>
                <li>Tại mục Action, chọn <strong>Start a program</strong>:
                  <ul className="list-disc list-inside pl-4 mt-1 space-y-0.5">
                    <li>Program/script: <code className="bg-slate-100 px-1 py-0.5 rounded text-slate-800 font-mono">powershell.exe</code></li>
                    <li>Add arguments: <code className="bg-slate-100 px-1 py-0.5 rounded text-slate-800 font-mono">-ExecutionPolicy Bypass -File "D:\Thuc_Tap_backend\Procurement & Contract Tracking System\scripts\database-backup-cron.ps1"</code></li>
                  </ul>
                </li>
                <li>Nhấn <strong>Finish</strong> để hoàn tất kích hoạt.</li>
              </ol>
            </div>
          </div>
        </div>
      )}

      {/* Modal Create Backup */}
      {createModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm p-4">
          <div className="bg-white rounded-xl shadow-xl max-w-md w-full p-6 space-y-4">
            <h3 className="text-base font-bold text-slate-800">
              {createType === 'database' ? 'Tạo bản sao lưu CSDL SQL Server' : 'Tạo bản nén tệp tải lên (ZIP)'}
            </h3>
            <p className="text-xs text-slate-600">
              {createType === 'database'
                ? 'Hệ thống sẽ thực thi lệnh BACKUP DATABASE có CHECKSUM và nén COMPRESSION, sau đó kiểm tra toàn vẹn bằng RESTORE VERIFYONLY.'
                : 'Hệ thống sẽ nén toàn bộ thư mục uploads thành tệp ZIP lưu trữ.'}
            </p>

            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-slate-700">Mô tả / Ghi chú (tùy chọn):</label>
              <input
                type="text"
                value={backupDescription}
                onChange={(e) => setBackupDescription(e.target.value)}
                placeholder="VD: Sao lưu trước đợt nâng cấp hoặc bảo trì..."
                className="w-full text-xs px-3 py-2 border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>

            <div className="flex justify-end gap-2 pt-2">
              <button
                type="button"
                onClick={() => setCreateModalOpen(false)}
                className="px-4 py-2 border border-slate-200 text-slate-600 text-xs font-semibold rounded-lg hover:bg-slate-50 transition"
              >
                Hủy bỏ
              </button>
              <button
                type="button"
                onClick={handleConfirmCreateBackup}
                className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-xs font-semibold rounded-lg transition"
              >
                Bắt đầu sao lưu
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Modal Restore Database (Safety Guard) */}
      {restoreModalOpen && selectedBackupForRestore && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4">
          <div className="bg-white rounded-xl shadow-2xl max-w-lg w-full p-6 space-y-4 border-2 border-amber-300">
            <div className="flex items-center gap-2 text-amber-600">
              <AlertTriangle className="w-6 h-6 flex-shrink-0" />
              <h3 className="text-base font-bold text-slate-900">CẢNH BÁO: Phục hồi Cơ sở dữ liệu</h3>
            </div>

            <div className="p-3 bg-amber-50 border border-amber-200 rounded-lg text-xs text-amber-800 space-y-2">
              <p className="font-semibold">
                Thao tác này sẽ ghi đè toàn bộ dữ liệu hiện tại của hệ thống bằng bản sao lưu:
              </p>
              <p className="font-mono bg-white px-2 py-1 rounded border border-amber-200 text-slate-900">
                {selectedBackupForRestore.fileName}
              </p>
              <ul className="list-disc list-inside space-y-1 text-[11px]">
                <li>Mọi giao dịch và dữ liệu phát sinh sau thời điểm sao lưu sẽ bị thay thế hoàn toàn.</li>
                <li>CSDL sẽ chuyển sang chế độ đơn người dùng (SINGLE_USER) tạm thời để phục hồi.</li>
              </ul>
            </div>

            <div className="space-y-1.5">
              <label className="text-xs font-bold text-slate-700">
                Nhập chính xác cụm từ <span className="text-rose-600 font-mono">RESTORE_DATABASE_CONFIRM</span> để xác nhận:
              </label>
              <input
                type="text"
                value={confirmPhraseInput}
                onChange={(e) => setConfirmPhraseInput(e.target.value)}
                placeholder="RESTORE_DATABASE_CONFIRM"
                className="w-full text-xs px-3 py-2 border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-amber-500 font-mono"
              />
            </div>

            <div className="flex justify-end gap-2 pt-2">
              <button
                type="button"
                onClick={() => setRestoreModalOpen(false)}
                disabled={restoring}
                className="px-4 py-2 border border-slate-200 text-slate-600 text-xs font-semibold rounded-lg hover:bg-slate-50 transition"
              >
                Hủy bỏ
              </button>
              <button
                type="button"
                onClick={handleConfirmRestore}
                disabled={restoring || confirmPhraseInput.trim() !== 'RESTORE_DATABASE_CONFIRM'}
                className="flex items-center gap-1.5 px-4 py-2 bg-amber-600 hover:bg-amber-700 text-white text-xs font-semibold rounded-lg transition disabled:opacity-50"
              >
                {restoring ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <RotateCcw className="w-3.5 h-3.5" />}
                Xác nhận phục hồi CSDL
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default BackupRestorePage;
