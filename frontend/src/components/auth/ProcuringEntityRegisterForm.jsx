import React, { useState, useCallback } from 'react';
import { Link } from 'react-router-dom';
import toast from 'react-hot-toast';
import {
  Lock,
  Mail,
  User,
  Phone,
  Building2,
  FileSpreadsheet,
  MapPin,
  Eye,
  EyeOff,
  ArrowRight,
  Search,
  Loader2,
  CheckCircle2,
} from 'lucide-react';
import { authApi } from '../../api/authApi';
import FileUpload from '../common/FileUpload';
import { useTaxCodeLookup } from '../../hooks/useTaxCodeLookup';

const REQUIRED_PROCURING_FIELDS = [
  'fullName',
  'email',
  'password',
  'organizationName',
  'taxCode',
  'representativeName',
];

const ORGANIZATION_TYPE_OPTIONS = [
  { value: 'Ban Quản lý Dự án', label: 'Ban Quản lý Dự án (PMU)' },
  { value: 'Cơ quan hành chính nhà nước', label: 'Cơ quan nhà nước' },
  { value: 'Đơn vị sự nghiệp công lập', label: 'Đơn vị sự nghiệp công lập' },
  { value: 'Doanh nghiệp nhà nước', label: 'Doanh nghiệp nhà nước' },
  { value: 'Đơn vị mua sắm tập trung', label: 'Đơn vị mua sắm tập trung' },
  { value: 'Doanh nghiệp tư nhân (Chủ đầu tư)', label: 'Doanh nghiệp tư nhân (Chủ đầu tư)' },
];

const FORM_FIELD_MAP = [
  ['FullName', 'fullName', true],
  ['Email', 'email', true],
  ['Password', 'password', true],
  ['Phone', 'phone', false],
  ['OrganizationName', 'organizationName', true],
  ['OrganizationType', 'organizationType', true],
  ['TaxCode', 'taxCode', true],
  ['BudgetCode', 'budgetCode', false],
  ['Address', 'address', false],
  ['RepresentativeName', 'representativeName', true],
  ['RepresentativeTitle', 'representativeTitle', true],
  ['RepresentativePhone', 'representativePhone', false],
  ['OtpCode', 'otpCode', false],
];

export const ProcuringEntityRegisterForm = ({ onSuccess }) => {
  const [loading, setLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [establishmentFile, setEstablishmentFile] = useState(null);
  const [appointmentFile, setAppointmentFile] = useState(null);
  const [formData, setFormData] = useState({
    fullName: '',
    email: '',
    password: '',
    phone: '',
    organizationName: '',
    organizationType: 'Ban Quản lý Dự án',
    taxCode: '',
    budgetCode: '',
    address: '',
    representativeName: '',
    representativeTitle: 'Giám đốc Ban QLDA',
    representativePhone: '',
    otpCode: '',
  });

  const handleTaxResolved = useCallback((cleanCode, taxInfo) => {
    setFormData((prev) => ({
      ...prev,
      taxCode: cleanCode,
      organizationName: taxInfo.name || prev.organizationName,
      address: taxInfo.address || prev.address,
    }));
  }, []);

  const { taxLookupLoading, taxVerified, resetTaxVerification, lookupTaxCode } =
    useTaxCodeLookup(handleTaxResolved);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
    if (name === 'taxCode') {
      resetTaxVerification();
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    const hasMissingRequiredField = REQUIRED_PROCURING_FIELDS.some(
      (field) => !String(formData[field] ?? '').trim()
    );
    if (hasMissingRequiredField) {
      toast.error('Vui lòng điền đầy đủ các mục có dấu sao (*)');
      return;
    }

    if (!establishmentFile) {
      toast.error('Vui lòng tải lên Quyết định thành lập hoặc Giấy phép hoạt động');
      return;
    }

    setLoading(true);
    try {
      const payload = new FormData();
      FORM_FIELD_MAP.forEach(([formKey, stateKey, isRequired]) => {
        const val = String(formData[stateKey] ?? '').trim();
        if (isRequired || val) {
          payload.append(formKey, isRequired ? formData[stateKey] : val);
        }
      });
      payload.append('EstablishmentDecisionFile', establishmentFile);
      if (appointmentFile) {
        payload.append('AppointmentDecisionFile', appointmentFile);
      }

      const response = await authApi.registerProcuringEntity(payload);
      if (response?.success) {
        toast.success('Đăng ký thành công! Hồ sơ của bạn sẽ được kích hoạt sớm nhất.');
        onSuccess?.();
        return;
      }

      toast.error(response?.message || 'Đăng ký không thành công');
    } catch (error) {
      toast.error(error.message || 'Lỗi gửi yêu cầu đăng ký');
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-5">
      {/* NHÓM 1: THÔNG TIN ĐĂNG NHẬP */}
      <div className="space-y-3.5">
        <h3 className="text-xs font-bold uppercase tracking-wider text-sky-700 pb-1 border-b border-sky-100">
          1. Thông tin đăng nhập
        </h3>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
          <div className="sm:col-span-2">
            <label className="block text-xs font-semibold text-slate-700 mb-1.5">
              Họ và tên người đăng ký / phụ trách <span className="text-rose-500">*</span>
            </label>
            <div className="relative rounded-lg">
              <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                <User className="w-4 h-4" />
              </div>
              <input
                type="text"
                required
                name="fullName"
                value={formData.fullName}
                onChange={handleChange}
                placeholder="Nguyễn Thị Mai"
                className="block w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
              />
            </div>
          </div>

          <div className="sm:col-span-2">
            <label className="block text-xs font-semibold text-slate-700 mb-1.5">
              Email công vụ / đăng nhập <span className="text-rose-500">*</span>
            </label>
            <div className="relative rounded-lg">
              <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                <Mail className="w-4 h-4" />
              </div>
              <input
                type="email"
                required
                name="email"
                value={formData.email}
                onChange={handleChange}
                placeholder="mai.nguyen@donvi.gov.vn"
                className="block w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
              />
            </div>
          </div>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1.5">
              Mật khẩu <span className="text-rose-500">*</span>
            </label>
            <div className="relative rounded-lg">
              <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                <Lock className="w-4 h-4" />
              </div>
              <input
                type={showPassword ? 'text' : 'password'}
                required
                name="password"
                value={formData.password}
                onChange={handleChange}
                placeholder="••••••••"
                className="block w-full pl-9 pr-10 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
              />
              <button
                type="button"
                onClick={() => setShowPassword(!showPassword)}
                className="absolute inset-y-0 right-0 pr-3 flex items-center text-slate-400 hover:text-slate-600 transition"
              >
                {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
              </button>
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1.5">
              Số điện thoại
            </label>
            <div className="relative rounded-lg">
              <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                <Phone className="w-4 h-4" />
              </div>
              <input
                type="tel"
                name="phone"
                value={formData.phone}
                onChange={handleChange}
                placeholder="024 3822 5678"
                className="block w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
              />
            </div>
          </div>
        </div>
      </div>

      {/* NHÓM 2: THÔNG TIN ĐƠN VỊ */}
      <div className="space-y-3.5 pt-1">
        <div className="flex items-center justify-between pb-1 border-b border-sky-100">
          <h3 className="text-xs font-bold uppercase tracking-wider text-sky-700">
            2. Thông tin đơn vị
          </h3>
          <span className="text-[11px] text-sky-600 bg-sky-50 px-2 py-0.5 rounded-full font-medium">
            Hỗ trợ tra cứu tự động từ CSDL Thuế
          </span>
        </div>

        <div>
          <div className="flex items-center justify-between mb-1.5">
            <label className="block text-xs font-semibold text-slate-700">
              Mã số thuế đơn vị <span className="text-rose-500">*</span>
            </label>
            <span className="text-[11px] text-slate-400">10 hoặc 13 số</span>
          </div>
          <div className="flex gap-2">
            <div className="relative flex-1 rounded-lg">
              <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                <FileSpreadsheet className="w-4 h-4" />
              </div>
              <input
                type="text"
                required
                name="taxCode"
                value={formData.taxCode}
                onChange={handleChange}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') {
                    e.preventDefault();
                    lookupTaxCode(formData.taxCode);
                  }
                }}
                placeholder="Nhập MST cơ quan (Ví dụ: 0100109106...)"
                className="block w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
              />
            </div>
            <button
              type="button"
              disabled={taxLookupLoading || !formData.taxCode?.trim()}
              onClick={() => lookupTaxCode(formData.taxCode)}
              className="inline-flex items-center space-x-1.5 px-3.5 py-2 text-xs font-semibold text-white bg-sky-600 hover:bg-sky-700 disabled:opacity-50 disabled:cursor-not-allowed rounded-lg shadow-sm transition shrink-0"
            >
              {taxLookupLoading ? (
                <>
                  <Loader2 className="w-3.5 h-3.5 animate-spin" />
                  <span>Đang tra cứu...</span>
                </>
              ) : (
                <>
                  <Search className="w-3.5 h-3.5" />
                  <span>Tra cứu CSDL</span>
                </>
              )}
            </button>
          </div>

          {taxVerified && (
            <div className="mt-2 flex items-start space-x-2 p-2.5 bg-emerald-50 border border-emerald-200 rounded-lg text-emerald-800 text-xs">
              <CheckCircle2 className="w-4 h-4 text-emerald-600 mt-0.5 shrink-0" />
              <div>
                <span className="font-semibold">
                  Đã xác thực từ Cổng thông tin Doanh nghiệp/Đơn vị Quốc gia:
                </span>
                <p className="font-medium mt-0.5">{taxVerified.name}</p>
              </div>
            </div>
          )}
        </div>

        <div>
          <label className="block text-xs font-semibold text-slate-700 mb-1.5">
            Tên cơ quan / đơn vị <span className="text-rose-500">*</span>
          </label>
          <div className="relative rounded-lg">
            <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
              <Building2 className="w-4 h-4" />
            </div>
            <input
              type="text"
              required
              name="organizationName"
              value={formData.organizationName}
              onChange={handleChange}
              placeholder="Tên cơ quan (tự động điền khi tra cứu MST)"
              className="block w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
            />
          </div>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1.5">
              Loại hình đơn vị <span className="text-rose-500">*</span>
            </label>
            <select
              name="organizationType"
              value={formData.organizationType}
              onChange={handleChange}
              className="block w-full px-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
            >
              {ORGANIZATION_TYPE_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1.5">
              Mã đơn vị ngân sách
            </label>
            <input
              type="text"
              name="budgetCode"
              value={formData.budgetCode}
              onChange={handleChange}
              placeholder="1054321 (nếu có)"
              className="block w-full px-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
            />
          </div>
        </div>

        <div>
          <label className="block text-xs font-semibold text-slate-700 mb-1.5">
            Địa chỉ trụ sở
          </label>
          <div className="relative rounded-lg">
            <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
              <MapPin className="w-4 h-4" />
            </div>
            <input
              type="text"
              name="address"
              value={formData.address}
              onChange={handleChange}
              placeholder="Địa chỉ trụ sở (tự động điền khi tra cứu MST)"
              className="block w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
            />
          </div>
        </div>
      </div>

      {/* NHÓM 3: NGƯỜI ĐẠI DIỆN PHÁP LUẬT */}
      <div className="space-y-3.5 pt-1">
        <h3 className="text-xs font-bold uppercase tracking-wider text-sky-700 pb-1 border-b border-sky-100">
          3. Người đại diện pháp luật
        </h3>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1.5">
              Họ và tên <span className="text-rose-500">*</span>
            </label>
            <input
              type="text"
              required
              name="representativeName"
              value={formData.representativeName}
              onChange={handleChange}
              placeholder="Trần Văn Minh"
              className="block w-full px-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1.5">
              Chức vụ <span className="text-rose-500">*</span>
            </label>
            <input
              type="text"
              required
              name="representativeTitle"
              value={formData.representativeTitle}
              onChange={handleChange}
              placeholder="Giám đốc, Trưởng ban..."
              className="block w-full px-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
            />
          </div>
        </div>

        <div>
          <label className="block text-xs font-semibold text-slate-700 mb-1.5">
            Số điện thoại
          </label>
          <input
            type="tel"
            name="representativePhone"
            value={formData.representativePhone}
            onChange={handleChange}
            placeholder="0903 999 888 (nếu khác SĐT ở trên)"
            className="block w-full px-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
          />
        </div>
      </div>

      {/* NHÓM 4: HỒ SƠ ĐÍNH KÈM */}
      <div className="space-y-3.5 pt-1">
        <h3 className="text-xs font-bold uppercase tracking-wider text-sky-700 pb-1 border-b border-sky-100">
          4. Hồ sơ đính kèm
        </h3>

        <FileUpload
          file={establishmentFile}
          onFileSelect={(file) => setEstablishmentFile(file)}
          onFileRemove={() => setEstablishmentFile(null)}
          label="Quyết định thành lập hoặc Giấy phép hoạt động"
          required={true}
          accept=".pdf,.jpg,.jpeg,.png"
          maxSizeMB={15}
          helperText="File PDF hoặc ảnh scan rõ nét (Tối đa 15MB)"
        />

        <FileUpload
          file={appointmentFile}
          onFileSelect={(file) => setAppointmentFile(file)}
          onFileRemove={() => setAppointmentFile(null)}
          label="Quyết định bổ nhiệm người đứng đầu (nếu có)"
          required={false}
          accept=".pdf,.jpg,.jpeg,.png"
          maxSizeMB={15}
          helperText="File PDF hoặc ảnh scan văn bản bổ nhiệm / ủy quyền"
        />
      </div>

      <div className="flex items-center justify-end text-xs pt-1">
        <Link to="/login" className="font-semibold text-sky-600 hover:text-sky-700 transition">
          Đã có tài khoản? Đăng nhập →
        </Link>
      </div>

      <button
        type="submit"
        disabled={loading}
        className="w-full py-2.5 px-4 bg-sky-700 hover:bg-sky-800 text-white font-semibold text-sm rounded-lg shadow-sm hover:shadow focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-sky-700 transition flex items-center justify-center space-x-2 disabled:opacity-50"
      >
        {loading ? (
          <>
            <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
            <span>Đang xử lý...</span>
          </>
        ) : (
          <>
            <span>Đăng ký tài khoản</span>
            <ArrowRight className="w-4 h-4" />
          </>
        )}
      </button>
    </form>
  );
};

export default ProcuringEntityRegisterForm;
