import React, { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { authApi } from '../api/authApi';
import FileUpload from '../components/common/FileUpload';
import toast from 'react-hot-toast';
import {
  ShieldCheck,
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
  Briefcase,
  Search,
  Loader2,
  CheckCircle2,
  KeyRound,
  Clock,
  Send
} from 'lucide-react';

export const RegisterPage = () => {
  const navigate = useNavigate();
  const [roleType, setRoleType] = useState('contractor'); // 'contractor' | 'procuring_entity'
  const [loading, setLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);

  // Dữ liệu form Nhà thầu
  const [contractorData, setContractorData] = useState({
    fullName: '',
    email: '',
    password: '',
    phone: '',
    companyName: '',
    taxCode: '',
    address: '',
    otpCode: '',
  });
  const [licenseFile, setLicenseFile] = useState(null);

  // Dữ liệu form Bên mời thầu
  const [procuringData, setProcuringData] = useState({
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
  const [establishmentFile, setEstablishmentFile] = useState(null);
  const [appointmentFile, setAppointmentFile] = useState(null);

  // Trạng thái xác thực Email OTP
  const [otpCooldownContractor, setOtpCooldownContractor] = useState(0);
  const [otpSendingContractor, setOtpSendingContractor] = useState(false);
  const [otpSentContractor, setOtpSentContractor] = useState(false);

  const [otpCooldownProcuring, setOtpCooldownProcuring] = useState(0);
  const [otpSendingProcuring, setOtpSendingProcuring] = useState(false);
  const [otpSentProcuring, setOtpSentProcuring] = useState(false);

  // Đếm ngược Cooldown
  useEffect(() => {
    if (otpCooldownContractor > 0) {
      const timer = setTimeout(() => setOtpCooldownContractor((c) => c - 1), 1000);
      return () => clearTimeout(timer);
    }
  }, [otpCooldownContractor]);

  useEffect(() => {
    if (otpCooldownProcuring > 0) {
      const timer = setTimeout(() => setOtpCooldownProcuring((c) => c - 1), 1000);
      return () => clearTimeout(timer);
    }
  }, [otpCooldownProcuring]);

  // Trạng thái tra cứu MST
  const [taxLookupLoadingContractor, setTaxLookupLoadingContractor] = useState(false);
  const [taxVerifiedContractor, setTaxVerifiedContractor] = useState(null);

  const [taxLookupLoadingProcuring, setTaxLookupLoadingProcuring] = useState(false);
  const [taxVerifiedProcuring, setTaxVerifiedProcuring] = useState(null);

  const handleContractorChange = (e) => {
    const { name, value } = e.target;
    setContractorData((prev) => ({ ...prev, [name]: value }));
    if (name === 'taxCode') {
      setTaxVerifiedContractor(null);
    }
  };

  const handleProcuringChange = (e) => {
    const { name, value } = e.target;
    setProcuringData((prev) => ({ ...prev, [name]: value }));
    if (name === 'taxCode') {
      setTaxVerifiedProcuring(null);
    }
  };

  // Hàm tra cứu MST Nhà thầu
  const handleLookupTaxContractor = async (customCode) => {
    const code = (typeof customCode === 'string' ? customCode : contractorData.taxCode)?.trim();
    if (!code) {
      toast.error('Vui lòng nhập Mã số thuế để tra cứu');
      return;
    }
    const cleanCode = code.replace(/\s+/g, '');
    if (!/^\d{10}(-\d{3})?$/.test(cleanCode) && !/^\d{13}$/.test(cleanCode)) {
      toast.error('Mã số thuế gồm 10 hoặc 13 chữ số');
      return;
    }

    setTaxLookupLoadingContractor(true);
    try {
      const res = await authApi.lookupTaxCode(cleanCode);
      if (res && res.success && res.data) {
        setContractorData((prev) => ({
          ...prev,
          taxCode: cleanCode,
          companyName: res.data.name || prev.companyName,
          address: res.data.address || prev.address,
        }));
        setTaxVerifiedContractor(res.data);
        toast.success(`Đã tự động điền: ${res.data.name}`);
      } else {
        setTaxVerifiedContractor(false);
        toast.error(res?.message || 'Không tìm thấy MST trên CSDL Quốc gia');
      }
    } catch (err) {
      setTaxVerifiedContractor(false);
      toast.error(err?.response?.data?.message || 'Không tìm thấy MST trên CSDL Quốc gia. Bạn có thể tự nhập tay thông tin.');
    } finally {
      setTaxLookupLoadingContractor(false);
    }
  };

  // Hàm tra cứu MST Bên mời thầu
  const handleLookupTaxProcuring = async (customCode) => {
    const code = (typeof customCode === 'string' ? customCode : procuringData.taxCode)?.trim();
    if (!code) {
      toast.error('Vui lòng nhập Mã số thuế để tra cứu');
      return;
    }
    const cleanCode = code.replace(/\s+/g, '');
    if (!/^\d{10}(-\d{3})?$/.test(cleanCode) && !/^\d{13}$/.test(cleanCode)) {
      toast.error('Mã số thuế gồm 10 hoặc 13 chữ số');
      return;
    }

    setTaxLookupLoadingProcuring(true);
    try {
      const res = await authApi.lookupTaxCode(cleanCode);
      if (res && res.success && res.data) {
        setProcuringData((prev) => ({
          ...prev,
          taxCode: cleanCode,
          organizationName: res.data.name || prev.organizationName,
          address: res.data.address || prev.address,
        }));
        setTaxVerifiedProcuring(res.data);
        toast.success(`Đã tự động điền: ${res.data.name}`);
      } else {
        setTaxVerifiedProcuring(false);
        toast.error(res?.message || 'Không tìm thấy MST trên CSDL Quốc gia');
      }
    } catch (err) {
      setTaxVerifiedProcuring(false);
      toast.error(err?.response?.data?.message || 'Không tìm thấy MST trên CSDL Quốc gia. Bạn có thể tự nhập tay thông tin.');
    } finally {
      setTaxLookupLoadingProcuring(false);
    }
  };

  // Gửi OTP Nhà thầu
  const handleSendOtpContractor = async () => {
    const email = contractorData.email?.trim();
    if (!email || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
      toast.error('Vui lòng nhập địa chỉ email hợp lệ trước khi lấy mã OTP');
      return;
    }
    setOtpSendingContractor(true);
    try {
      const res = await authApi.sendOtp(email, 'RegisterContractor');
      if (res && res.success) {
        setOtpSentContractor(true);
        setOtpCooldownContractor(res.data?.cooldownSeconds || 60);
        if (res.data?.devOtpCode) {
          toast.success(`Mã OTP đã gửi! [Dev: ${res.data.devOtpCode}]`, { duration: 6000 });
          setContractorData((prev) => ({ ...prev, otpCode: res.data.devOtpCode }));
        } else {
          toast.success('Mã xác thực OTP 6 số đã được gửi đến email của bạn.');
        }
      } else {
        toast.error(res?.message || 'Không thể gửi mã OTP');
      }
    } catch (err) {
      toast.error(err?.response?.data?.message || err.message || 'Lỗi gửi mã OTP');
    } finally {
      setOtpSendingContractor(false);
    }
  };

  // Gửi OTP Bên mời thầu
  const handleSendOtpProcuring = async () => {
    const email = procuringData.email?.trim();
    if (!email || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
      toast.error('Vui lòng nhập địa chỉ email hợp lệ trước khi lấy mã OTP');
      return;
    }
    setOtpSendingProcuring(true);
    try {
      const res = await authApi.sendOtp(email, 'RegisterProcuringEntity');
      if (res && res.success) {
        setOtpSentProcuring(true);
        setOtpCooldownProcuring(res.data?.cooldownSeconds || 60);
        if (res.data?.devOtpCode) {
          toast.success(`Mã OTP đã gửi! [Dev: ${res.data.devOtpCode}]`, { duration: 6000 });
          setProcuringData((prev) => ({ ...prev, otpCode: res.data.devOtpCode }));
        } else {
          toast.success('Mã xác thực OTP 6 số đã được gửi đến email công vụ của bạn.');
        }
      } else {
        toast.error(res?.message || 'Không thể gửi mã OTP');
      }
    } catch (err) {
      toast.error(err?.response?.data?.message || err.message || 'Lỗi gửi mã OTP');
    } finally {
      setOtpSendingProcuring(false);
    }
  };

  // Submit Nhà thầu
  const handleContractorSubmit = async (e) => {
    e.preventDefault();

    if (!contractorData.fullName || !contractorData.email || !contractorData.password || !contractorData.companyName || !contractorData.taxCode) {
      toast.error('Vui lòng điền đầy đủ các mục có dấu sao (*)');
      return;
    }

    if (!contractorData.otpCode?.trim()) {
      toast.error('Vui lòng nhập mã OTP xác thực email');
      return;
    }

    if (!licenseFile) {
      toast.error('Vui lòng tải lên Giấy phép kinh doanh');
      return;
    }

    setLoading(true);

    try {
      const data = new FormData();
      data.append('FullName', contractorData.fullName);
      data.append('Email', contractorData.email);
      data.append('Password', contractorData.password);
      if (contractorData.phone) data.append('Phone', contractorData.phone);
      data.append('CompanyName', contractorData.companyName);
      data.append('TaxCode', contractorData.taxCode);
      if (contractorData.address) data.append('Address', contractorData.address);
      data.append('OtpCode', contractorData.otpCode.trim());
      data.append('BusinessLicenseFile', licenseFile);

      const response = await authApi.registerContractor(data);

      if (response && response.success) {
        toast.success('Đăng ký thành công! Hồ sơ của bạn đang được xét duyệt.');
        navigate('/login');
      } else {
        toast.error(response?.message || 'Đăng ký không thành công');
      }
    } catch (error) {
      toast.error(error.message || 'Lỗi gửi yêu cầu đăng ký');
    } finally {
      setLoading(false);
    }
  };

  // Submit Bên mời thầu
  const handleProcuringSubmit = async (e) => {
    e.preventDefault();

    if (!procuringData.fullName || !procuringData.email || !procuringData.password || !procuringData.organizationName || !procuringData.taxCode || !procuringData.representativeName) {
      toast.error('Vui lòng điền đầy đủ các mục có dấu sao (*)');
      return;
    }

    if (!procuringData.otpCode?.trim()) {
      toast.error('Vui lòng nhập mã OTP xác thực email');
      return;
    }

    if (!establishmentFile) {
      toast.error('Vui lòng tải lên Quyết định thành lập hoặc Giấy phép hoạt động');
      return;
    }

    setLoading(true);

    try {
      const data = new FormData();
      data.append('FullName', procuringData.fullName);
      data.append('Email', procuringData.email);
      data.append('Password', procuringData.password);
      if (procuringData.phone) data.append('Phone', procuringData.phone);
      data.append('OrganizationName', procuringData.organizationName);
      data.append('OrganizationType', procuringData.organizationType);
      data.append('TaxCode', procuringData.taxCode);
      if (procuringData.budgetCode) data.append('BudgetCode', procuringData.budgetCode);
      if (procuringData.address) data.append('Address', procuringData.address);
      data.append('RepresentativeName', procuringData.representativeName);
      data.append('RepresentativeTitle', procuringData.representativeTitle);
      if (procuringData.representativePhone) data.append('RepresentativePhone', procuringData.representativePhone);
      data.append('OtpCode', procuringData.otpCode.trim());
      data.append('EstablishmentDecisionFile', establishmentFile);
      if (appointmentFile) data.append('AppointmentDecisionFile', appointmentFile);

      const response = await authApi.registerProcuringEntity(data);

      if (response && response.success) {
        toast.success('Đăng ký thành công! Hồ sơ của bạn sẽ được kích hoạt sớm nhất.');
        navigate('/login');
      } else {
        toast.error(response?.message || 'Đăng ký không thành công');
      }
    } catch (error) {
      toast.error(error.message || 'Lỗi gửi yêu cầu đăng ký');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col justify-center items-center py-10 px-4 sm:px-6">
      <div className="max-w-xl w-full space-y-6">
        {/* Tiêu đề ứng dụng */}
        <div className="text-center space-y-2">
          <div className="inline-flex items-center justify-center w-12 h-12 rounded-xl bg-slate-900 text-sky-400 shadow-sm">
            <ShieldCheck className="w-6 h-6" />
          </div>
          <h1 className="text-base font-bold text-slate-700 tracking-tight">
            Procurement & Contract Tracking System
          </h1>
        </div>

        {/* Khung nội dung đăng ký */}
        <div className="bg-white p-7 sm:p-9 rounded-2xl border border-slate-200 shadow-sm space-y-6">
          <div>
            <h2 className="text-2xl font-bold text-slate-900">
              Đăng ký tài khoản
            </h2>
            <p className="text-xs text-slate-500 mt-1">
              Chọn loại tài khoản phù hợp với đơn vị của bạn
            </p>
          </div>

          {/* Tab chọn loại tài khoản */}
          <div className="grid grid-cols-2 p-1 bg-slate-100 rounded-xl gap-1">
            <button
              type="button"
              onClick={() => setRoleType('contractor')}
              className={`py-2 px-3 text-xs font-semibold rounded-lg transition flex items-center justify-center space-x-2 ${
                roleType === 'contractor'
                  ? 'bg-white text-slate-900 shadow-sm'
                  : 'text-slate-500 hover:text-slate-800'
              }`}
            >
              <Building2 className="w-4 h-4" />
              <span>Nhà thầu</span>
            </button>
            <button
              type="button"
              onClick={() => setRoleType('procuring_entity')}
              className={`py-2 px-3 text-xs font-semibold rounded-lg transition flex items-center justify-center space-x-2 ${
                roleType === 'procuring_entity'
                  ? 'bg-white text-sky-700 shadow-sm'
                  : 'text-slate-500 hover:text-slate-800'
              }`}
            >
              <Briefcase className="w-4 h-4" />
              <span>Bên mời thầu</span>
            </button>
          </div>

          {/* ============================================================== */}
          {/* FORM NHÀ THẦU */}
          {/* ============================================================== */}
          {roleType === 'contractor' && (
            <form onSubmit={handleContractorSubmit} className="space-y-5">
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
                        value={contractorData.fullName}
                        onChange={handleContractorChange}
                        placeholder="Nguyễn Văn A"
                        className="block w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
                      />
                    </div>
                  </div>

                  {/* Email + Nút gửi OTP */}
                  <div className="sm:col-span-2">
                    <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                      Email đăng nhập <span className="text-rose-500">*</span>
                    </label>
                    <div className="flex gap-2">
                      <div className="relative flex-1 rounded-lg">
                        <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                          <Mail className="w-4 h-4" />
                        </div>
                        <input
                          type="email"
                          required
                          name="email"
                          value={contractorData.email}
                          onChange={handleContractorChange}
                          placeholder="email@company.vn"
                          className="block w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
                        />
                      </div>
                      <button
                        type="button"
                        disabled={otpSendingContractor || otpCooldownContractor > 0 || !contractorData.email?.trim()}
                        onClick={handleSendOtpContractor}
                        className="inline-flex items-center space-x-1.5 px-3 py-2 text-xs font-semibold text-sky-700 bg-sky-50 border border-sky-200 hover:bg-sky-100 disabled:opacity-50 disabled:cursor-not-allowed rounded-lg shadow-sm transition shrink-0"
                      >
                        {otpSendingContractor ? (
                          <>
                            <Loader2 className="w-3.5 h-3.5 animate-spin" />
                            <span>Đang gửi...</span>
                          </>
                        ) : otpCooldownContractor > 0 ? (
                          <>
                            <Clock className="w-3.5 h-3.5 text-sky-600" />
                            <span>Gửi lại ({otpCooldownContractor}s)</span>
                          </>
                        ) : (
                          <>
                            <Send className="w-3.5 h-3.5" />
                            <span>{otpSentContractor ? 'Gửi lại OTP' : 'Gửi mã OTP'}</span>
                          </>
                        )}
                      </button>
                    </div>
                  </div>

                  {/* Mã xác thực OTP */}
                  <div className="sm:col-span-2">
                    <div className="flex items-center justify-between mb-1.5">
                      <label className="block text-xs font-semibold text-slate-700">
                        Mã xác thực OTP (Email) <span className="text-rose-500">*</span>
                      </label>
                      <span className="text-[11px] text-slate-400">6 chữ số (hiệu lực 5 phút)</span>
                    </div>
                    <div className="relative rounded-lg">
                      <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                        <KeyRound className="w-4 h-4" />
                      </div>
                      <input
                        type="text"
                        required
                        maxLength={6}
                        name="otpCode"
                        value={contractorData.otpCode}
                        onChange={handleContractorChange}
                        placeholder="Nhập 6 số OTP (Ví dụ: 123456)"
                        className="block w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 font-mono tracking-widest focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
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
                        value={contractorData.password}
                        onChange={handleContractorChange}
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
                        value={contractorData.phone}
                        onChange={handleContractorChange}
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

                {/* Ô Mã số thuế + Nút Tra cứu */}
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
                        value={contractorData.taxCode}
                        onChange={handleContractorChange}
                        onKeyDown={(e) => {
                          if (e.key === 'Enter') {
                            e.preventDefault();
                            handleLookupTaxContractor();
                          }
                        }}
                        placeholder="Nhập MST (Ví dụ: 0300588569...)"
                        className="block w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
                      />
                    </div>
                    <button
                      type="button"
                      disabled={taxLookupLoadingContractor || !contractorData.taxCode?.trim()}
                      onClick={() => handleLookupTaxContractor()}
                      className="inline-flex items-center space-x-1.5 px-3.5 py-2 text-xs font-semibold text-white bg-sky-600 hover:bg-sky-700 disabled:opacity-50 disabled:cursor-not-allowed rounded-lg shadow-sm transition shrink-0"
                    >
                      {taxLookupLoadingContractor ? (
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

                  {/* Badge hiển thị kết quả xác thực */}
                  {taxVerifiedContractor && (
                    <div className="mt-2 flex items-start space-x-2 p-2.5 bg-emerald-50 border border-emerald-200 rounded-lg text-emerald-800 text-xs">
                      <CheckCircle2 className="w-4 h-4 text-emerald-600 mt-0.5 shrink-0" />
                      <div>
                        <span className="font-semibold">Đã xác thực từ Cổng thông tin Doanh nghiệp Quốc gia:</span>
                        <p className="font-medium mt-0.5">{taxVerifiedContractor.name}</p>
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
                      value={contractorData.companyName}
                      onChange={handleContractorChange}
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
                      value={contractorData.address}
                      onChange={handleContractorChange}
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
          )}

          {/* ============================================================== */}
          {/* FORM BÊN MỜI THẦU / CHỦ ĐẦU TƯ */}
          {/* ============================================================== */}
          {roleType === 'procuring_entity' && (
            <form onSubmit={handleProcuringSubmit} className="space-y-5">
              {/* NHÓM 1: THÔNG TIN TÀI KHOẢN */}
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
                        value={procuringData.fullName}
                        onChange={handleProcuringChange}
                        placeholder="Nguyễn Thị Mai"
                        className="block w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
                      />
                    </div>
                  </div>

                  {/* Email + Nút gửi OTP */}
                  <div className="sm:col-span-2">
                    <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                      Email công vụ / đăng nhập <span className="text-rose-500">*</span>
                    </label>
                    <div className="flex gap-2">
                      <div className="relative flex-1 rounded-lg">
                        <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                          <Mail className="w-4 h-4" />
                        </div>
                        <input
                          type="email"
                          required
                          name="email"
                          value={procuringData.email}
                          onChange={handleProcuringChange}
                          placeholder="mai.nguyen@donvi.gov.vn"
                          className="block w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
                        />
                      </div>
                      <button
                        type="button"
                        disabled={otpSendingProcuring || otpCooldownProcuring > 0 || !procuringData.email?.trim()}
                        onClick={handleSendOtpProcuring}
                        className="inline-flex items-center space-x-1.5 px-3 py-2 text-xs font-semibold text-sky-700 bg-sky-50 border border-sky-200 hover:bg-sky-100 disabled:opacity-50 disabled:cursor-not-allowed rounded-lg shadow-sm transition shrink-0"
                      >
                        {otpSendingProcuring ? (
                          <>
                            <Loader2 className="w-3.5 h-3.5 animate-spin" />
                            <span>Đang gửi...</span>
                          </>
                        ) : otpCooldownProcuring > 0 ? (
                          <>
                            <Clock className="w-3.5 h-3.5 text-sky-600" />
                            <span>Gửi lại ({otpCooldownProcuring}s)</span>
                          </>
                        ) : (
                          <>
                            <Send className="w-3.5 h-3.5" />
                            <span>{otpSentProcuring ? 'Gửi lại OTP' : 'Gửi mã OTP'}</span>
                          </>
                        )}
                      </button>
                    </div>
                  </div>

                  {/* Mã xác thực OTP */}
                  <div className="sm:col-span-2">
                    <div className="flex items-center justify-between mb-1.5">
                      <label className="block text-xs font-semibold text-slate-700">
                        Mã xác thực OTP (Email công vụ) <span className="text-rose-500">*</span>
                      </label>
                      <span className="text-[11px] text-slate-400">6 chữ số (hiệu lực 5 phút)</span>
                    </div>
                    <div className="relative rounded-lg">
                      <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
                        <KeyRound className="w-4 h-4" />
                      </div>
                      <input
                        type="text"
                        required
                        maxLength={6}
                        name="otpCode"
                        value={procuringData.otpCode}
                        onChange={handleProcuringChange}
                        placeholder="Nhập 6 số OTP (Ví dụ: 123456)"
                        className="block w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 font-mono tracking-widest focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
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
                        value={procuringData.password}
                        onChange={handleProcuringChange}
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
                        value={procuringData.phone}
                        onChange={handleProcuringChange}
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

                {/* Ô Mã số thuế + Nút Tra cứu */}
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
                        value={procuringData.taxCode}
                        onChange={handleProcuringChange}
                        onKeyDown={(e) => {
                          if (e.key === 'Enter') {
                            e.preventDefault();
                            handleLookupTaxProcuring();
                          }
                        }}
                        placeholder="Nhập MST cơ quan (Ví dụ: 0100109106...)"
                        className="block w-full pl-9 pr-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
                      />
                    </div>
                    <button
                      type="button"
                      disabled={taxLookupLoadingProcuring || !procuringData.taxCode?.trim()}
                      onClick={() => handleLookupTaxProcuring()}
                      className="inline-flex items-center space-x-1.5 px-3.5 py-2 text-xs font-semibold text-white bg-sky-600 hover:bg-sky-700 disabled:opacity-50 disabled:cursor-not-allowed rounded-lg shadow-sm transition shrink-0"
                    >
                      {taxLookupLoadingProcuring ? (
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

                  {/* Badge hiển thị kết quả xác thực */}
                  {taxVerifiedProcuring && (
                    <div className="mt-2 flex items-start space-x-2 p-2.5 bg-emerald-50 border border-emerald-200 rounded-lg text-emerald-800 text-xs">
                      <CheckCircle2 className="w-4 h-4 text-emerald-600 mt-0.5 shrink-0" />
                      <div>
                        <span className="font-semibold">Đã xác thực từ Cổng thông tin Doanh nghiệp/Đơn vị Quốc gia:</span>
                        <p className="font-medium mt-0.5">{taxVerifiedProcuring.name}</p>
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
                      value={procuringData.organizationName}
                      onChange={handleProcuringChange}
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
                      value={procuringData.organizationType}
                      onChange={handleProcuringChange}
                      className="block w-full px-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
                    >
                      <option value="Ban Quản lý Dự án">Ban Quản lý Dự án (PMU)</option>
                      <option value="Cơ quan hành chính nhà nước">Cơ quan nhà nước</option>
                      <option value="Đơn vị sự nghiệp công lập">Đơn vị sự nghiệp công lập</option>
                      <option value="Doanh nghiệp nhà nước">Doanh nghiệp nhà nước</option>
                      <option value="Đơn vị mua sắm tập trung">Đơn vị mua sắm tập trung</option>
                      <option value="Doanh nghiệp tư nhân (Chủ đầu tư)">Doanh nghiệp tư nhân (Chủ đầu tư)</option>
                    </select>
                  </div>

                  <div>
                    <label className="block text-xs font-semibold text-slate-700 mb-1.5">
                      Mã đơn vị ngân sách
                    </label>
                    <input
                      type="text"
                      name="budgetCode"
                      value={procuringData.budgetCode}
                      onChange={handleProcuringChange}
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
                      value={procuringData.address}
                      onChange={handleProcuringChange}
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
                      value={procuringData.representativeName}
                      onChange={handleProcuringChange}
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
                      value={procuringData.representativeTitle}
                      onChange={handleProcuringChange}
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
                    value={procuringData.representativePhone}
                    onChange={handleProcuringChange}
                    placeholder="0903 999 888 (nếu khác SĐT ở trên)"
                    className="block w-full px-3 py-2 text-sm bg-white border border-slate-300 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
                  />
                </div>
              </div>

              {/* NHÓM 4: TÀI LIỆU ĐÍNH KÈM */}
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
          )}
        </div>
      </div>
    </div>
  );
};

export default RegisterPage;
