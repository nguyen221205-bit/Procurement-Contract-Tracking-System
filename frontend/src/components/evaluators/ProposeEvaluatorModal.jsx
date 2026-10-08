import React, { useState } from 'react';
import { X, UserPlus, Upload, FileText, CheckCircle2, AlertCircle } from 'lucide-react';
import toast from 'react-hot-toast';
import { evaluatorProposalApi } from '../../api/evaluatorProposalApi';

const SPECIALIZATION_SUGGESTIONS = [
  'Kỹ thuật Phần mềm & CNTT',
  'Hạ tầng Mạng & Viễn thông',
  'An toàn Thông tin & Bảo mật',
  'Xây lắp Công trình Dân dụng',
  'Tài chính - Dự toán & Giá thầu',
  'Pháp lý & Quản trị Hợp đồng',
  'Thiết bị Y tế & Khoa học',
];

export const ProposeEvaluatorModal = ({ isOpen, onClose, onSuccess }) => {
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [specialization, setSpecialization] = useState('');
  const [workplace, setWorkplace] = useState('');
  const [experienceYears, setExperienceYears] = useState('');
  const [certificateName, setCertificateName] = useState('Chứng chỉ nghiệp vụ chuyên môn về đấu thầu (Luật Đấu thầu 2023)');
  const [certificateFile, setCertificateFile] = useState(null);
  const [notes, setNotes] = useState('');
  const [submitting, setSubmitting] = useState(false);

  if (!isOpen) return null;

  const handleFileChange = (e) => {
    if (e.target.files && e.target.files[0]) {
      const file = e.target.files[0];
      if (file.size > 25 * 1024 * 1024) {
        toast.error('Kích thước tệp không được vượt quá 25MB.');
        return;
      }
      setCertificateFile(file);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!fullName.trim()) {
      toast.error('Vui lòng nhập họ và tên chuyên gia / giám khảo.');
      return;
    }

    if (!email.trim()) {
      toast.error('Vui lòng nhập email chính thức của giám khảo.');
      return;
    }

    try {
      setSubmitting(true);

      const formData = new FormData();
      formData.append('fullName', fullName.trim());
      formData.append('email', email.trim());
      if (phone.trim()) formData.append('phone', phone.trim());
      if (specialization.trim()) formData.append('specialization', specialization.trim());
      if (workplace.trim()) formData.append('workplace', workplace.trim());
      if (experienceYears) formData.append('experienceYears', Number(experienceYears));
      if (certificateName.trim()) formData.append('certificateName', certificateName.trim());
      if (certificateFile) formData.append('certificateFile', certificateFile);
      if (notes.trim()) formData.append('notes', notes.trim());

      const res = await evaluatorProposalApi.createProposal(formData);

      if (res && res.success) {
        toast.success(res.message || 'Gửi đề xuất chỉ định Giám khảo thành công!');
        onSuccess?.();
        onClose();
      } else {
        toast.error(res?.message || 'Không thể gửi đề xuất.');
      }
    } catch (err) {
      toast.error(err.message || 'Lỗi khi gửi đề xuất giám khảo.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/50 backdrop-blur-xs flex items-center justify-center p-4 animate-in fade-in duration-150">
      <div
        className="bg-white rounded-2xl max-w-xl w-full p-6 shadow-2xl border border-slate-100 relative space-y-5 max-h-[90vh] overflow-y-auto"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Nút đóng */}
        <button
          onClick={onClose}
          disabled={submitting}
          className="absolute top-4 right-4 text-slate-400 hover:text-slate-600 p-1 rounded-lg hover:bg-slate-100 transition"
        >
          <X className="w-5 h-5" />
        </button>

        {/* Header Modal */}
        <div className="flex items-start space-x-3.5 pr-8">
          <div className="w-12 h-12 rounded-xl bg-gradient-to-tr from-indigo-600 to-sky-600 text-white flex items-center justify-center flex-shrink-0 shadow-sm">
            <UserPlus className="w-6 h-6" />
          </div>
          <div>
            <h3 className="text-base font-bold text-slate-900">
              Đề xuất Giám khảo / Thành viên Tổ chuyên gia
            </h3>
            <p className="text-xs text-slate-500">
              Bên mời thầu gửi thông tin đề xuất chuyên gia lên Quản trị viên (Admin) để thẩm định và cấp tài khoản
            </p>
          </div>
        </div>

        {/* Form nhập thông tin */}
        <form onSubmit={handleSubmit} className="space-y-4 text-xs">
          {/* Thông tin cơ bản */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="font-semibold text-slate-700 block">
                Họ và tên chuyên gia <span className="text-rose-500">*</span>
              </label>
              <input
                type="text"
                required
                value={fullName}
                onChange={(e) => setFullName(e.target.value)}
                placeholder="VD: TS. Nguyễn Văn An"
                className="w-full rounded-xl border border-slate-200 px-3 py-2 focus:outline-hidden focus:ring-2 focus:ring-sky-500 transition"
              />
            </div>

            <div className="space-y-1">
              <label className="font-semibold text-slate-700 block">
                Email chính thức (Tên đăng nhập) <span className="text-rose-500">*</span>
              </label>
              <input
                type="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="VD: an.nguyen@daihoc.edu.vn"
                className="w-full rounded-xl border border-slate-200 px-3 py-2 focus:outline-hidden focus:ring-2 focus:ring-sky-500 transition"
              />
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="font-semibold text-slate-700 block">Số điện thoại liên hệ</label>
              <input
                type="tel"
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
                placeholder="VD: 0912345678"
                className="w-full rounded-xl border border-slate-200 px-3 py-2 focus:outline-hidden focus:ring-2 focus:ring-sky-500 transition"
              />
            </div>

            <div className="space-y-1">
              <label className="font-semibold text-slate-700 block">Kinh nghiệm công tác (năm)</label>
              <input
                type="number"
                min="0"
                max="60"
                value={experienceYears}
                onChange={(e) => setExperienceYears(e.target.value)}
                placeholder="VD: 8"
                className="w-full rounded-xl border border-slate-200 px-3 py-2 focus:outline-hidden focus:ring-2 focus:ring-sky-500 transition"
              />
            </div>
          </div>

          <div className="space-y-1">
            <label className="font-semibold text-slate-700 block">Cơ quan / Đơn vị công tác & Chức vụ</label>
            <input
              type="text"
              value={workplace}
              onChange={(e) => setWorkplace(e.target.value)}
              placeholder="VD: Viện Công nghệ Thông tin - Trưởng phòng Nghiên cứu"
              className="w-full rounded-xl border border-slate-200 px-3 py-2 focus:outline-hidden focus:ring-2 focus:ring-sky-500 transition"
            />
          </div>

          <div className="space-y-1">
            <label className="font-semibold text-slate-700 block">Lĩnh vực chuyên môn chính</label>
            <input
              type="text"
              value={specialization}
              onChange={(e) => setSpecialization(e.target.value)}
              placeholder="VD: Kỹ thuật Phần mềm & CNTT"
              className="w-full rounded-xl border border-slate-200 px-3 py-2 focus:outline-hidden focus:ring-2 focus:ring-sky-500 transition"
            />
            {/* Gợi ý nhanh */}
            <div className="flex flex-wrap gap-1 pt-1">
              {SPECIALIZATION_SUGGESTIONS.map((item) => (
                <button
                  type="button"
                  key={item}
                  onClick={() => setSpecialization(item)}
                  className="px-2 py-0.5 rounded-md text-[10px] font-medium bg-slate-100 text-slate-600 hover:bg-slate-200 transition"
                >
                  {item}
                </button>
              ))}
            </div>
          </div>

          {/* Hồ sơ chứng chỉ nghiệp vụ */}
          <div className="bg-sky-50/60 rounded-xl p-3.5 border border-sky-100 space-y-2.5">
            <div className="space-y-1">
              <label className="font-semibold text-slate-800 block">
                Tên chứng chỉ / Bằng cấp chuyên môn theo luật định
              </label>
              <input
                type="text"
                value={certificateName}
                onChange={(e) => setCertificateName(e.target.value)}
                placeholder="VD: Chứng chỉ nghiệp vụ chuyên môn về đấu thầu..."
                className="w-full rounded-lg border border-sky-200 px-3 py-1.5 bg-white focus:outline-hidden focus:ring-2 focus:ring-sky-500 transition text-xs"
              />
            </div>

            <div className="space-y-1">
              <label className="font-semibold text-slate-800 block">
                Tệp đính kèm Chứng chỉ / Bằng cấp (PDF, Word, Ảnh)
              </label>
              <div className="flex items-center space-x-2">
                <label className="cursor-pointer inline-flex items-center space-x-1.5 px-3 py-1.5 rounded-lg border border-sky-300 bg-white hover:bg-sky-50 text-sky-700 font-semibold transition text-xs shadow-2xs">
                  <Upload className="w-3.5 h-3.5" />
                  <span>Chọn tệp chứng chỉ</span>
                  <input
                    type="file"
                    className="hidden"
                    accept=".pdf,.png,.jpg,.jpeg,.doc,.docx"
                    onChange={handleFileChange}
                  />
                </label>
                <span className="text-slate-500 truncate max-w-xs text-[11px]">
                  {certificateFile ? certificateFile.name : 'Chưa chọn tệp nào (hỗ trợ PDF tối đa 25MB)'}
                </span>
              </div>
            </div>
          </div>

          {/* Căn cứ đề xuất */}
          <div className="space-y-1">
            <label className="font-semibold text-slate-700 block">Lý do / Căn cứ đề xuất:</label>
            <textarea
              rows={2}
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder="VD: Chuyên gia có kinh nghiệm 8 năm thẩm định kỹ thuật phần mềm, phù hợp với yêu cầu của gói thầu..."
              className="w-full rounded-xl border border-slate-200 p-2.5 focus:outline-hidden focus:ring-2 focus:ring-sky-500 transition resize-none text-xs"
            />
          </div>

          {/* Nút hành động */}
          <div className="pt-2 flex items-center justify-end space-x-2 border-t border-slate-100">
            <button
              type="button"
              onClick={onClose}
              disabled={submitting}
              className="px-4 py-2 font-semibold text-slate-600 hover:bg-slate-100 rounded-xl transition border border-slate-200"
            >
              Hủy
            </button>
            <button
              type="submit"
              disabled={submitting}
              className="inline-flex items-center space-x-1.5 px-4 py-2 font-bold text-white bg-indigo-600 hover:bg-indigo-700 rounded-xl transition shadow-xs disabled:opacity-50"
            >
              <CheckCircle2 className="w-4 h-4" />
              <span>{submitting ? 'Đang gửi...' : 'Gửi đề xuất cho Admin'}</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default ProposeEvaluatorModal;
