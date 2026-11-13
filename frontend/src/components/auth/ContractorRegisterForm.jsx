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

const REQUIRED_CONTRACTOR_FIELDS = ['fullName', 'email', 'password', 'companyName', 'taxCode'];

const FORM_FIELD_MAP = [
  ['FullName', 'fullName', true],
  ['Email', 'email', true],
  ['Password', 'password', true],
  ['Phone', 'phone', false],
  ['CompanyName', 'companyName', true],
  ['TaxCode', 'taxCode', true],
  ['Address', 'address', false],
  ['OtpCode', 'otpCode', false],
];

export const ContractorRegisterForm = ({ onSuccess }) => {
  const [loading, setLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [licenseFile, setLicenseFile] = useState(null);
  const [formData, setFormData] = useState({
    fullName: '',
    email: '',
    password: '',
    phone: '',
    companyName: '',
    taxCode: '',
    address: '',
    otpCode: '',
  });

  const handleTaxResolved = useCallback((cleanCode, taxInfo) => {
    setFormData((prev) => ({
      ...prev,
      taxCode: cleanCode,
      companyName: taxInfo.name || prev.companyName,
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

    const hasMissingRequiredField = REQUIRED_CONTRACTOR_FIELDS.some(
      (field) => !String(formData[field] ?? '').trim()
    );
    if (hasMissingRequiredField) {
      toast.error('Vui lòng điền đầy đủ các mục có dấu sao (*)');
      return;
    }

    if (!licenseFile) {
      toast.error('Vui lòng tải lên Giấy phép kinh doanh');
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
      payload.append('BusinessLicenseFile', licenseFile);

      const response = await authApi.registerContractor(payload);
      if (response?.success) {
        toast.success('Đăng ký thành công! Hồ sơ của bạn đang được xét duyệt.');
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
      {/* NHÓM 1: THÔNG TIN TÀI KHOẢN */}
      <div className="space-y-3.5">
        <h3 className="text-xs font-bold uppercase tracking-wider text-slate-500 pb-1 border-b border-slate-100">
          1. Thông tin tài khoản
        </h3>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
          <div className="sm:col-span-2">
            <label className="block text-xs font-semibold text-slate-700 mb-1.5">
              Họ và tên người đại diện <span className="text-rose-500">*</span>
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
                placeholder="Nguyễn Văn A"
                className="block w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
              />
            </div>
          </div>

          <div className="sm:col-span-2">
            <label className="block text-xs font-semibold text-slate-700 mb-1.5">
              Email đăng nhập <span className="text-rose-500">*</span>
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
                placeholder="email@company.vn"
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
                placeholder="0912 345 678"
                className="block w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
              />
            </div>
          </div>
        </div>
      </div>

      {/* NHÓM 2: THÔNG TIN DOANH NGHIỆP */}
      <div className="space-y-3.5 pt-1">
        <div className="flex items-center justify-between pb-1 border-b border-slate-100">
          <h3 className="text-xs font-bold uppercase tracking-wider text-slate-500">
            2. Thông tin doanh nghiệp
          </h3>
          <span className="text-[11px] text-sky-600 bg-sky-50 px-2 py-0.5 rounded-full font-medium">
            Hỗ trợ tra cứu tự động từ CSDL Thuế
          </span>
        </div>

        <div>
          <div className="flex items-center justify-between mb-1.5">
            <label className="block text-xs font-semibold text-slate-700">
              Mã số thuế doanh nghiệp <span className="text-rose-500">*</span>
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
                placeholder="Nhập MST (Ví dụ: 0300588569...)"
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
                <span className="font-semibold">Đã xác thực từ Cổng thông tin Doanh nghiệp Quốc gia:</span>
                <p className="font-medium mt-0.5">{taxVerified.name}</p>
              </div>
            </div>
          )}
        </div>

        <div>
          <label className="block text-xs font-semibold text-slate-700 mb-1.5">
            Tên doanh nghiệp / công ty <span className="text-rose-500">*</span>
          </label>
          <div className="relative rounded-lg">
            <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
              <Building2 className="w-4 h-4" />
            </div>
            <input
              type="text"
              required
              name="companyName"
              value={formData.companyName}
              onChange={handleChange}
              placeholder="Tên công ty (tự động điền khi tra cứu MST)"
              className="block w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
            />
          </div>
        </div>

        <div>
          <label className="block text-xs font-semibold text-slate-700 mb-1.5">
            Địa chỉ trụ sở doanh nghiệp
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

        <FileUpload
          file={licenseFile}
          onFileSelect={(file) => setLicenseFile(file)}
          onFileRemove={() => setLicenseFile(null)}
          label="Giấy phép kinh doanh"
          required={true}
          accept=".pdf,.jpg,.jpeg,.png"
          maxSizeMB={10}
          helperText="File PDF hoặc ảnh chụp rõ nét (Tối đa 10MB)"
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
        className="w-full py-2.5 px-4 bg-slate-900 hover:bg-slate-800 text-white font-semibold text-sm rounded-lg shadow-sm hover:shadow focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-slate-900 transition flex items-center justify-center space-x-2 disabled:opacity-50"
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

export default ContractorRegisterForm;
