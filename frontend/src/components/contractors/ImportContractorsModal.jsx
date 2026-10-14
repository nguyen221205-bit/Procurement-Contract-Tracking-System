import React, { useState, useRef } from 'react';
import { X, UploadCloud, FileSpreadsheet, Download, CheckCircle2, AlertTriangle, RefreshCw } from 'lucide-react';
import toast from 'react-hot-toast';
import { contractorApi } from '../../api/contractorApi';
import LoadingSpinner from '../common/LoadingSpinner';

export const ImportContractorsModal = ({ isOpen, onClose, onSuccess }) => {
  const [file, setFile] = useState(null);
  const [uploading, setUploading] = useState(false);
  const [downloadingTemplate, setDownloadingTemplate] = useState(false);
  const [importResult, setImportResult] = useState(null);
  const fileInputRef = useRef(null);

  if (!isOpen) return null;

  // 1. Tải file mẫu template Excel (.xlsx)
  const handleDownloadTemplate = async () => {
    try {
      setDownloadingTemplate(true);
      toast.loading('Đang tải tệp mẫu Excel...', { id: 'download-template' });
      const blobData = await contractorApi.getImportTemplate();
      const blob = new Blob([blobData], {
        type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      });
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = 'Mau_Nhap_Danh_Sach_Nha_Thau.xlsx';
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
      toast.success('Tải tệp mẫu thành công!', { id: 'download-template' });
    } catch (err) {
      toast.error('Không thể tải tệp mẫu: ' + (err.message || 'Lỗi mạng'), { id: 'download-template' });
    } finally {
      setDownloadingTemplate(false);
    }
  };

  // 2. Xử lý chọn tệp từ máy
  const handleFileChange = (e) => {
    const selected = e.target.files[0];
    if (selected) {
      if (!selected.name.match(/\.(xlsx|xls)$/i)) {
        toast.error('Vui lòng chọn định dạng file bảng tính Excel (.xlsx hoặc .xls)');
        return;
      }
      setFile(selected);
      setImportResult(null);
    }
  };

  // 3. Xử lý Drag and Drop
  const handleDragOver = (e) => {
    e.preventDefault();
    e.stopPropagation();
  };

  const handleDrop = (e) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      const selected = e.dataTransfer.files[0];
      if (!selected.name.match(/\.(xlsx|xls)$/i)) {
        toast.error('Vui lòng chọn định dạng file bảng tính Excel (.xlsx hoặc .xls)');
        return;
      }
      setFile(selected);
      setImportResult(null);
    }
  };

  // 4. Thực hiện Upload và Nhập dữ liệu
  const handleUpload = async () => {
    if (!file) {
      toast.error('Vui lòng chọn file Excel trước khi bấm nhập dữ liệu.');
      return;
    }

    try {
      setUploading(true);
      toast.loading('Đang xử lý và kiểm tra dữ liệu Excel...', { id: 'import-excel' });
      const res = await contractorApi.importExcel(file);

      if (res && res.success && res.data) {
        setImportResult(res.data);
        if (res.data.failureCount === 0) {
          toast.success(`Nhập thành công toàn bộ ${res.data.successCount} nhà thầu!`, { id: 'import-excel' });
          onSuccess?.();
        } else {
          toast.success(`Đã nhập ${res.data.successCount} nhà thầu, có ${res.data.failureCount} dòng lỗi.`, { id: 'import-excel' });
          onSuccess?.();
        }
      } else {
        toast.error(res?.message || 'Quá trình nhập dữ liệu không thành công.', { id: 'import-excel' });
      }
    } catch (err) {
      toast.error(err.message || 'Lỗi khi nhập dữ liệu từ file Excel.', { id: 'import-excel' });
    } finally {
      setUploading(false);
    }
  };

  const handleReset = () => {
    setFile(null);
    setImportResult(null);
    if (fileInputRef.current) fileInputRef.current.value = '';
  };

  const errorList = importResult?.errors || importResult?.errorMessages || [];

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-xs">
      <div className="bg-white w-full max-w-2xl rounded-2xl shadow-2xl border border-slate-200 overflow-hidden flex flex-col max-h-[90vh] animate-in fade-in zoom-in-95 duration-150">
        {/* Header */}
        <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between bg-slate-50/50">
          <div className="flex items-center space-x-3">
            <div className="p-2 bg-emerald-50 text-emerald-600 rounded-xl">
              <FileSpreadsheet className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-slate-900">
                Nhập Danh Sách Nhà Thầu Từ File Excel
              </h3>
              <p className="text-xs text-slate-500">
                Hỗ trợ thêm hàng loạt hồ sơ năng lực nhà thầu từ bảng tính chuẩn
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-1.5 text-slate-400 hover:text-slate-600 rounded-lg hover:bg-slate-100 transition cursor-pointer"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Body */}
        <div className="p-6 overflow-y-auto flex-1 space-y-5">
          {/* Bước 1: Tải template */}
          <div className="bg-sky-50/60 rounded-xl p-4 border border-sky-100 flex items-center justify-between gap-4">
            <div className="space-y-0.5">
              <h4 className="text-xs font-bold text-sky-900">1. Tải về tệp Excel mẫu chuẩn</h4>
              <p className="text-[11px] text-sky-700">Tệp mẫu đã bao gồm các cột tiêu đề và hướng dẫn định dạng dữ liệu bắt buộc.</p>
            </div>
            <button
              type="button"
              onClick={handleDownloadTemplate}
              disabled={downloadingTemplate}
              className="px-3 py-1.5 bg-white hover:bg-sky-50 text-sky-700 border border-sky-200 rounded-lg text-xs font-bold transition flex items-center space-x-1.5 flex-shrink-0 cursor-pointer shadow-2xs disabled:opacity-50"
            >
              <Download className="w-3.5 h-3.5" />
              <span>{downloadingTemplate ? 'Đang tải...' : 'Tải tệp mẫu (.xlsx)'}</span>
            </button>
          </div>

          {/* Bước 2: Kéo thả tải tệp lên */}
          <div className="space-y-2">
            <label className="text-xs font-bold text-slate-700">2. Chọn tệp bảng tính dữ liệu đã chuẩn bị</label>
            <div
              onClick={() => fileInputRef.current?.click()}
              onDragOver={handleDragOver}
              onDrop={handleDrop}
              className={`border-2 border-dashed rounded-2xl p-6 text-center cursor-pointer transition ${
                file ? 'border-emerald-300 bg-emerald-50/20' : 'border-slate-300 hover:border-sky-400 bg-slate-50/50 hover:bg-sky-50/20'
              }`}
            >
              <input
                ref={fileInputRef}
                type="file"
                accept=".xlsx, .xls"
                onChange={handleFileChange}
                className="hidden"
              />
              <UploadCloud className={`w-8 h-8 mx-auto mb-2 ${file ? 'text-emerald-500' : 'text-slate-400'}`} />
              {file ? (
                <div className="space-y-1">
                  <p className="text-xs font-bold text-slate-800">{file.name}</p>
                  <p className="text-[11px] text-slate-500 font-mono">{(file.size / 1024).toFixed(1)} KB</p>
                  <span className="text-[10px] text-emerald-600 font-semibold underline block">Bấm để chọn tệp khác</span>
                </div>
              ) : (
                <div className="space-y-1">
                  <p className="text-xs font-semibold text-slate-700">
                    Kéo thả tệp Excel vào đây hoặc <span className="text-sky-600 underline">bấm để duyệt tệp</span>
                  </p>
                  <p className="text-[11px] text-slate-400">Định dạng hỗ trợ: Microsoft Excel (.xlsx, .xls)</p>
                </div>
              )}
            </div>
          </div>

          {/* Bước 3: Bảng kết quả sau khi Import */}
          {importResult && (
            <div className="space-y-3 pt-2 border-t border-slate-100">
              <h4 className="text-xs font-bold text-slate-800 flex items-center space-x-1.5">
                <CheckCircle2 className="w-4 h-4 text-emerald-600" />
                <span>Kết quả xử lý nhập dữ liệu</span>
              </h4>

              <div className="grid grid-cols-3 gap-3 text-center">
                <div className="p-3 bg-slate-50 rounded-xl border border-slate-200">
                  <span className="text-[11px] text-slate-500 font-medium block">Tổng số dòng</span>
                  <span className="text-lg font-black font-mono text-slate-800">{importResult.totalRows}</span>
                </div>
                <div className="p-3 bg-emerald-50 rounded-xl border border-emerald-200">
                  <span className="text-[11px] text-emerald-700 font-medium block">Thành công</span>
                  <span className="text-lg font-black font-mono text-emerald-700">{importResult.successCount}</span>
                </div>
                <div className="p-3 bg-rose-50 rounded-xl border border-rose-200">
                  <span className="text-[11px] text-rose-700 font-medium block">Thất bại</span>
                  <span className="text-lg font-black font-mono text-rose-700">{importResult.failureCount}</span>
                </div>
              </div>

              {/* Danh sách lỗi theo dòng nếu có */}
              {errorList.length > 0 && (
                <div className="bg-rose-50/70 border border-rose-200 rounded-xl p-3.5 space-y-2">
                  <span className="text-xs font-bold text-rose-900 flex items-center space-x-1.5">
                    <AlertTriangle className="w-4 h-4 text-rose-600 flex-shrink-0" />
                    <span>Chi tiết các dòng bị từ chối ({errorList.length} lỗi):</span>
                  </span>
                  <div className="max-h-36 overflow-y-auto space-y-1.5 pr-1">
                    {errorList.map((errMsg, idx) => (
                      <div key={idx} className="text-[11px] text-rose-800 bg-white/80 p-2 rounded-lg border border-rose-100 font-mono">
                        {errMsg}
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="px-6 py-4 border-t border-slate-100 bg-slate-50/50 flex items-center justify-between">
          <button
            type="button"
            onClick={handleReset}
            disabled={uploading || (!file && !importResult)}
            className="px-3.5 py-2 text-xs font-semibold text-slate-600 hover:text-slate-800 hover:bg-slate-200/60 rounded-xl transition disabled:opacity-40 cursor-pointer"
          >
            Làm mới
          </button>

          <div className="flex items-center space-x-2">
            <button
              type="button"
              onClick={onClose}
              disabled={uploading}
              className="px-4 py-2 text-xs font-semibold text-slate-600 hover:bg-slate-100 rounded-xl transition cursor-pointer"
            >
              Đóng
            </button>
            <button
              type="button"
              onClick={handleUpload}
              disabled={!file || uploading}
              className="px-5 py-2 text-xs font-bold text-white bg-emerald-600 hover:bg-emerald-700 disabled:opacity-50 disabled:cursor-not-allowed rounded-xl shadow-xs transition flex items-center space-x-1.5 cursor-pointer"
            >
              {uploading ? (
                <>
                  <LoadingSpinner size="sm" />
                  <span>Đang nhập dữ liệu...</span>
                </>
              ) : (
                <>
                  <FileSpreadsheet className="w-4 h-4" />
                  <span>Bắt đầu nhập dữ liệu</span>
                </>
              )}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ImportContractorsModal;
