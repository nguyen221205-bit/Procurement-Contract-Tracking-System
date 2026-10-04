import React, { useState, useRef } from 'react';
import {
  X,
  UploadCloud,
  FileText,
  FileSpreadsheet,
  FileArchive,
  File,
  Trash2,
  CheckCircle2,
  AlertCircle,
  Send,
  Clock,
  ShieldCheck,
  DollarSign,
  TrendingDown,
  Lock,
} from 'lucide-react';
import toast from 'react-hot-toast';
import { bidSubmissionApi } from '../../api/bidSubmissionApi';
import { formatCurrency } from '../../utils/formatters';

// Danh mục loại tài liệu theo enum SubmissionFileType của Backend
const SUBMISSION_FILE_TYPES = [
  { value: 0, label: 'Báo giá & Tài chính', desc: 'Bảng giá dự thầu, phân tích đơn giá' },
  { value: 1, label: 'Hồ sơ năng lực', desc: 'Hồ sơ pháp lý, kinh nghiệm, chứng chỉ' },
  { value: 2, label: 'Tiến độ & Biện pháp', desc: 'Biện pháp thi công, tiến độ cam kết' },
  { value: 3, label: 'Tài liệu khác', desc: 'Bảo lãnh dự thầu, tài liệu bổ trợ' },
];

export const SubmitBidModal = ({ isOpen, onClose, pkg, onSuccess }) => {
  const [bidPrice, setBidPrice] = useState('');
  const [filesList, setFilesList] = useState([]);
  const [agreed, setAgreed] = useState(false);
  const [loading, setLoading] = useState(false);
  const [dragActive, setDragActive] = useState(false);
  const fileInputRef = useRef(null);

  if (!isOpen || !pkg) return null;

  // Tự động suy đoán loại tài liệu thông minh dựa theo tên file
  const inferFileType = (fileName) => {
    const lower = fileName.toLowerCase();
    // 1. Báo giá & Tài chính
    if (
      lower.includes('bao gia') ||
      lower.includes('bao_gia') ||
      lower.includes('bao-gia') ||
      lower.includes('bang gia') ||
      lower.includes('bang_gia') ||
      lower.includes('bang-gia') ||
      lower.includes('don gia') ||
      lower.includes('tai chinh') ||
      lower.includes('tai-chinh') ||
      lower.includes('du toan') ||
      lower.includes('price') ||
      lower.includes('quote')
    ) {
      return 0; // Quotation
    }
    // 2. Hồ sơ năng lực & Pháp lý
    if (
      lower.includes('nang luc') ||
      lower.includes('nang_luc') ||
      lower.includes('nang-luc') ||
      lower.includes('kinh nghiem') ||
      lower.includes('capability') ||
      lower.includes('phap ly') ||
      lower.includes('gpdkkd') ||
      lower.includes('dang ky') ||
      lower.includes('dang-ky') ||
      lower.includes('dang_ky') ||
      lower.includes('chung nhan') ||
      lower.includes('chung-nhan') ||
      lower.includes('giay-chung-nhan') ||
      lower.includes('giay phep')
    ) {
      return 1; // Capability
    }
    // 3. Tiến độ & Biện pháp thi công
    if (
      lower.includes('tien do') ||
      lower.includes('tien_do') ||
      lower.includes('tien-do') ||
      lower.includes('thi cong') ||
      lower.includes('thi_cong') ||
      lower.includes('thi-cong') ||
      lower.includes('schedule') ||
      lower.includes('bien phap') ||
      lower.includes('bien_phap')
    ) {
      return 2; // Schedule
    }
    return 3; // Mặc định là Tài liệu khác nếu không nhận diện được
  };

  // Format kích thước tệp
  const formatFileSize = (bytes) => {
    if (!bytes || bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  };

  // Icon hiển thị theo định dạng tệp
  const getFileIcon = (fileName) => {
    const ext = fileName.split('.').pop()?.toLowerCase();
    if (ext === 'pdf') return <FileText className="w-4 h-4 text-rose-500" />;
    if (['doc', 'docx'].includes(ext)) return <FileText className="w-4 h-4 text-blue-500" />;
    if (['xls', 'xlsx', 'csv'].includes(ext)) return <FileSpreadsheet className="w-4 h-4 text-emerald-500" />;
    if (['zip', 'rar', '7z'].includes(ext)) return <FileArchive className="w-4 h-4 text-amber-500" />;
    return <File className="w-4 h-4 text-slate-500" />;
  };

  // Xử lý nạp file vào danh sách
  const handleAddFiles = (newFiles) => {
    const validFiles = Array.from(newFiles).filter((file) => {
      // Giới hạn 25MB
      if (file.size > 25 * 1024 * 1024) {
        toast.error(`Tệp "${file.name}" vượt quá dung lượng tối đa 25MB.`);
        return false;
      }
      return true;
    });

    if (validFiles.length === 0) return;

    const mapped = validFiles.map((file) => ({
      id: `${file.name}-${file.size}-${Date.now()}-${Math.random()}`,
      file,
      fileType: inferFileType(file.name),
    }));

    setFilesList((prev) => [...prev, ...mapped]);
  };

  // Kéo thả Drag & Drop
  const handleDrag = (e) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.type === 'dragenter' || e.type === 'dragover') {
      setDragActive(true);
    } else if (e.type === 'dragleave') {
      setDragActive(false);
    }
  };

  const handleDrop = (e) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);
    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      handleAddFiles(e.dataTransfer.files);
    }
  };

  // Đổi loại tài liệu cho từng file
  const handleTypeChange = (id, newType) => {
    setFilesList((prev) =>
      prev.map((item) => (item.id === id ? { ...item, fileType: Number(newType) } : item))
    );
  };

  // Xóa tệp khỏi danh sách
  const handleRemoveFile = (id) => {
    setFilesList((prev) => prev.filter((item) => item.id !== id));
  };

  // Kiểm tra trùng loại tài liệu khi có nhiều tệp (> 1)
  const typeCounts = filesList.reduce((acc, item) => {
    acc[item.fileType] = (acc[item.fileType] || 0) + 1;
    return acc;
  }, {});

  const duplicateTypes = Object.entries(typeCounts)
    .filter(([_, count]) => count > 1)
    .map(([type]) => Number(type));

  const duplicateNames = duplicateTypes
    .map((t) => SUBMISSION_FILE_TYPES.find((x) => x.value === t)?.label)
    .filter(Boolean)
    .join(', ');

  const budget = pkg.budget || 0;
  const isOverBudget = bidPrice && budget > 0 && Number(bidPrice) > budget;
  const costSaving = bidPrice && budget > 0 && Number(bidPrice) <= budget ? budget - Number(bidPrice) : 0;
  const costSavingPercent = budget > 0 && costSaving > 0 ? ((costSaving / budget) * 100).toFixed(1) : 0;

  // Submit Form nộp hồ sơ
  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!bidPrice || Number(bidPrice) <= 0) {
      toast.error('Vui lòng nhập Giá dự thầu hợp lệ lớn hơn 0.');
      return;
    }

    if (budget > 0 && Number(bidPrice) > budget) {
      toast.error(`Giá chào thầu không được vượt quá Ngân sách dự toán (${formatCurrency(budget)}).`);
      return;
    }

    if (filesList.length === 0) {
      toast.error('Vui lòng đính kèm ít nhất 1 tệp hồ sơ dự thầu.');
      return;
    }

    if (!agreed) {
      toast.error('Vui lòng tích xác nhận cam kết trước khi nộp hồ sơ.');
      return;
    }

    try {
      setLoading(true);

      const formData = new FormData();
      formData.append('bidPrice', bidPrice);
      filesList.forEach((item) => {
        formData.append('files', item.file);
        formData.append('fileTypes', item.fileType);
      });

      await bidSubmissionApi.submitBid(pkg.id, formData);

      toast.success('Nộp hồ sơ dự thầu thành công!');
      onSuccess?.();
      onClose();
    } catch (error) {
      toast.error(error.message || 'Không thể nộp hồ sơ dự thầu.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/50 backdrop-blur-xs flex items-center justify-center p-4 animate-in fade-in duration-150">
      <div
        className="bg-white rounded-2xl max-w-2xl w-full shadow-2xl border border-slate-100 relative overflow-hidden flex flex-col max-h-[92vh]"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header Modal */}
        <div className="px-6 py-4.5 border-b border-slate-100 flex items-center justify-between bg-slate-50/70">
          <div className="flex items-center space-x-2.5">
            <div className="p-2 bg-sky-50 text-sky-600 rounded-xl">
              <Send className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-slate-900">Nộp hồ sơ dự thầu</h3>
              <p className="text-xs text-slate-500 font-mono truncate max-w-md">
                {pkg.code} - {pkg.name}
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            disabled={loading}
            className="text-slate-400 hover:text-slate-600 p-1.5 rounded-lg hover:bg-slate-200/60 transition disabled:opacity-50"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Thân Form */}
        <form onSubmit={handleSubmit} className="p-6 overflow-y-auto space-y-4 text-xs flex-1">
          {/* Thông tin tóm tắt gói thầu */}
          <div className="p-3 bg-sky-50/50 border border-sky-100 rounded-xl flex items-center justify-between text-xs">
            <span className="text-slate-600 font-medium">Thời hạn nộp thầu:</span>
            <span className="font-semibold text-sky-800 flex items-center space-x-1">
              <Clock className="w-3.5 h-3.5 text-sky-600" />
              <span>{new Date(pkg.deadline).toLocaleDateString('vi-VN')}</span>
            </span>
          </div>

          {/* Khối Nhập Giá dự thầu (VNĐ) */}
          <div className="p-4 bg-slate-50 border border-slate-200 rounded-2xl space-y-3">
            <div className="flex items-center justify-between">
              <label className="font-bold text-slate-800 flex items-center space-x-1.5">
                <DollarSign className="w-4 h-4 text-emerald-600" />
                <span>Giá dự thầu đề xuất (VNĐ) <span className="text-rose-500">*</span></span>
              </label>
              <div className="text-right">
                <span className="text-[11px] text-slate-500">Ngân sách dự toán: </span>
                <span className="text-xs font-bold text-slate-800">{formatCurrency(pkg.budget || 0)}</span>
              </div>
            </div>

            <div className="relative">
              <input
                type="text"
                value={bidPrice ? Number(bidPrice).toLocaleString('vi-VN') : ''}
                onChange={(e) => {
                  const raw = e.target.value.replace(/\D/g, '');
                  setBidPrice(raw ? Number(raw) : '');
                }}
                placeholder="Nhập số tiền dự thầu (ví dụ: 4.800.000.000)..."
                className={`w-full px-4 py-2.5 bg-white border rounded-xl text-sm font-bold text-slate-900 transition outline-none ${
                  isOverBudget
                    ? 'border-rose-400 focus:border-rose-500 focus:ring-1 focus:ring-rose-500 bg-rose-50/20'
                    : 'border-slate-300 focus:border-sky-500 focus:ring-1 focus:ring-sky-500'
                }`}
              />
              <span className="absolute right-3.5 top-2.5 text-xs font-bold text-slate-400">VNĐ</span>
            </div>

            {/* Phân tích & Cảnh báo Giá thầu */}
            {isOverBudget ? (
              <div className="p-2.5 bg-rose-50 border border-rose-200 rounded-xl flex items-center space-x-2 text-rose-700 text-[11px]">
                <AlertCircle className="w-4 h-4 text-rose-600 shrink-0" />
                <span>
                  <strong>Cảnh báo:</strong> Giá chào thầu ({formatCurrency(bidPrice)}) đang vượt quá Ngân sách dự toán ({formatCurrency(pkg.budget)}). Theo quy định, giá chào không được vượt giá gói thầu!
                </span>
              </div>
            ) : bidPrice && pkg.budget && Number(bidPrice) <= pkg.budget ? (
              <div className="p-2.5 bg-emerald-50 border border-emerald-200 rounded-xl flex items-center justify-between text-emerald-800 text-[11px]">
                <span className="flex items-center space-x-1.5">
                  <TrendingDown className="w-4 h-4 text-emerald-600 shrink-0" />
                  <span>Tiết kiệm dự kiến: <strong>{formatCurrency(costSaving)}</strong> ({costSavingPercent}%) so với dự toán</span>
                </span>
                <span className="text-[10px] text-emerald-600 font-semibold flex items-center space-x-1">
                  <Lock className="w-3 h-3" />
                  <span>Niêm phong bảo mật</span>
                </span>
              </div>
            ) : (
              <p className="text-[10.5px] text-slate-500 flex items-center space-x-1">
                <Lock className="w-3.5 h-3.5 text-slate-400 shrink-0" />
                <span>Giá dự thầu sẽ được niêm phong điện tử tự động và bảo mật tuyệt đối cho đến giai đoạn thẩm định.</span>
              </p>
            )}
          </div>

          {/* Hướng dẫn 4 nhóm tệp */}
          <div className="space-y-1.5">
            <label className="font-semibold text-slate-800">
              Đính kèm hồ sơ dự thầu <span className="text-rose-500">*</span>
            </label>
            <p className="text-[11px] text-slate-500">
              Hỗ trợ tệp PDF, Word, Excel, ZIP (tối đa 25MB/tệp). Bạn có thể chọn nhiều tệp cùng lúc.
            </p>
          </div>

          {/* Vùng kéo thả Dropzone */}
          <div
            onDragEnter={handleDrag}
            onDragLeave={handleDrag}
            onDragOver={handleDrag}
            onDrop={handleDrop}
            onClick={() => fileInputRef.current?.click()}
            className={`border-2 border-dashed rounded-2xl p-6 text-center cursor-pointer transition ${
              dragActive
                ? 'border-sky-500 bg-sky-50/50'
                : 'border-slate-200 hover:border-sky-400 hover:bg-slate-50/60'
            }`}
          >
            <input
              ref={fileInputRef}
              type="file"
              multiple
              accept=".pdf,.doc,.docx,.xls,.xlsx,.zip,.rar"
              onChange={(e) => handleAddFiles(e.target.files)}
              className="hidden"
            />
            <div className="flex flex-col items-center justify-center space-y-2">
              <div className="p-3 bg-sky-50 text-sky-600 rounded-2xl">
                <UploadCloud className="w-6 h-6" />
              </div>
              <p className="text-xs font-semibold text-slate-700">
                Kéo thả tệp vào đây hoặc <span className="text-sky-600 underline">chọn tệp từ máy tính</span>
              </p>
              <p className="text-[10px] text-slate-400">PDF, DOCX, XLSX, ZIP (Tối đa 25MB)</p>
            </div>
          </div>

          {/* Danh sách tệp đã chọn */}
          {filesList.length > 0 && (
            <div className="space-y-2 pt-2">
              <div className="flex items-center justify-between">
                <span className="font-bold text-slate-800">
                  Tệp đính kèm đã chọn ({filesList.length}):
                </span>
                <button
                  type="button"
                  onClick={() => setFilesList([])}
                  className="text-[11px] text-rose-600 hover:underline"
                >
                  Xóa tất cả
                </button>
              </div>

              <div className="divide-y divide-slate-100 border border-slate-200 rounded-xl overflow-hidden shadow-2xs">
                {filesList.map((item) => (
                  <div
                    key={item.id}
                    className="p-3 bg-white flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2.5 hover:bg-slate-50/60 transition"
                  >
                    <div className="flex items-center space-x-2.5 truncate flex-1 min-w-0">
                      <div className="p-1.5 bg-slate-50 rounded-lg flex-shrink-0">
                        {getFileIcon(item.file.name)}
                      </div>
                      <div className="truncate">
                        <p className="text-xs font-semibold text-slate-800 truncate" title={item.file.name}>
                          {item.file.name}
                        </p>
                        <p className="text-[10px] text-slate-400 font-mono">
                          {formatFileSize(item.file.size)}
                        </p>
                      </div>
                    </div>

                    <div className="flex items-center space-x-2 flex-shrink-0">
                      {/* Dropdown chọn loại tài liệu */}
                      <select
                        value={item.fileType}
                        onChange={(e) => handleTypeChange(item.id, e.target.value)}
                        className={`px-2.5 py-1.5 border rounded-lg text-[11px] font-medium transition outline-none ${
                          duplicateTypes.includes(item.fileType)
                            ? 'border-amber-400 bg-amber-50 text-amber-900 focus:border-amber-500'
                            : 'bg-slate-50 border-slate-200 text-slate-700 focus:border-sky-500 focus:bg-white'
                        }`}
                      >
                        {SUBMISSION_FILE_TYPES.map((t) => (
                          <option key={t.value} value={t.value}>
                            {t.label}
                          </option>
                        ))}
                      </select>

                      <button
                        type="button"
                        onClick={() => handleRemoveFile(item.id)}
                        className="p-1.5 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition"
                        title="Xóa tệp"
                      >
                        <Trash2 className="w-3.5 h-3.5" />
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Cảnh báo khi người dùng chọn > 1 hồ sơ nhưng bị trùng loại */}
          {filesList.length > 1 && duplicateTypes.length > 0 && (
            <div className="p-3 bg-amber-50 border border-amber-200 rounded-xl text-amber-800 text-[11px] flex items-center space-x-2">
              <AlertCircle className="w-4 h-4 text-amber-600 flex-shrink-0" />
              <span>
                Đang có nhiều tệp được xếp cùng loại <strong>{duplicateNames}</strong>. Vui lòng kiểm tra lại để phân loại đúng hồ sơ.
              </span>
            </div>
          )}

          {/* Cam kết nhà thầu */}
          <div className="pt-2">
            <label className="flex items-start space-x-2.5 p-3 rounded-xl bg-slate-50 border border-slate-200/80 cursor-pointer hover:bg-slate-100/70 transition">
              <input
                type="checkbox"
                checked={agreed}
                onChange={(e) => setAgreed(e.target.checked)}
                className="mt-0.5 w-4 h-4 rounded text-sky-600 focus:ring-sky-500 border-slate-300"
              />
              <span className="text-[11px] text-slate-600 leading-relaxed">
                Tôi xác nhận các tài liệu đính kèm là chính xác, hợp lệ và cam kết tuân thủ đầy đủ quy định của Hồ sơ mời thầu.
              </span>
            </label>
          </div>

          {/* Footer nút hành động */}
          <div className="pt-3 border-t border-slate-100 flex items-center justify-end space-x-2.5">
            <button
              type="button"
              onClick={onClose}
              disabled={loading}
              className="px-4 py-2 text-xs font-semibold text-slate-600 hover:bg-slate-100 rounded-xl transition border border-slate-200 disabled:opacity-50"
            >
              Hủy
            </button>
            <button
              type="submit"
              disabled={loading || !bidPrice || isOverBudget || filesList.length === 0 || !agreed}
              className="px-5 py-2 text-xs font-bold text-white bg-sky-600 hover:bg-sky-700 rounded-xl shadow-xs transition disabled:opacity-50 disabled:cursor-not-allowed flex items-center space-x-1.5"
            >
              {loading && (
                <svg className="animate-spin -ml-1 mr-1.5 h-3.5 w-3.5 text-white" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
              )}
              <span>Nộp hồ sơ dự thầu</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default SubmitBidModal;
