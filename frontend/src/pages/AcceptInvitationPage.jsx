import React, { useState, useEffect } from 'react';
import { useSearchParams, useNavigate, Link } from 'react-router-dom';
import { procuringEntityApi } from '../api/procuringEntityApi';
import toast from 'react-hot-toast';
import {
  Building2,
  UserCheck,
  ShieldCheck,
  AlertCircle,
  CheckCircle2,
  Lock,
  User,
  Mail,
  ArrowRight,
  Clock,
  Sparkles
} from 'lucide-react';
import LoadingSpinner from '../components/common/LoadingSpinner';

export const AcceptInvitationPage = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const token = searchParams.get('token');

  const [loading, setLoading] = useState(true);
  const [invitation, setInvitation] = useState(null);
  const [error, setError] = useState(null);

  // Form state cho tài khoản mới
  const [fullName, setFullName] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [isSuccess, setIsSuccess] = useState(false);

  useEffect(() => {
    if (!token) {
      setError('Đường dẫn kích hoạt không hợp lệ hoặc thiếu mã xác thực (token).');
      setLoading(false);
      return;
    }

    const fetchDetails = async () => {
      try {
        setLoading(true);
        const res = await procuringEntityApi.getInvitationDetails(token);
        if (res && res.success && res.data) {
          setInvitation(res.data);
        } else {
          setError(res?.message || 'Không tìm thấy thông tin lời mời.');
        }
      } catch (err) {
        setError(err.message || 'Không thể tải thông tin lời mời hoặc liên kết đã hết hạn.');
      } finally {
        setLoading(false);
      }
    };

    fetchDetails();
  }, [token]);

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!invitation.hasExistingAccount) {
      if (!fullName.trim()) {
        toast.error('Vui lòng nhập họ và tên của bạn.');
        return;
      }
      if (!password || password.length < 6) {
        toast.error('Mật khẩu phải có độ dài tối thiểu 6 ký tự.');
        return;
      }
      if (password !== confirmPassword) {
        toast.error('Mật khẩu xác nhận không khớp.');
        return;
      }
    }

    try {
      setSubmitting(true);
      const payload = {
        token,
        fullName: invitation.hasExistingAccount ? undefined : fullName.trim(),
        password: invitation.hasExistingAccount ? undefined : password
      };

      const res = await procuringEntityApi.acceptInvitation(payload);
      if (res && res.success) {
        setIsSuccess(true);
        toast.success('Gia nhập Đơn vị thành công!');
      } else {
        toast.error(res?.message || 'Không thể chấp nhận lời mời.');
      }
    } catch (err) {
      toast.error(err.message || 'Có lỗi xảy ra khi xử lý lời mời.');
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-slate-50 flex flex-col items-center justify-center p-4">
        <LoadingSpinner />
        <p className="mt-4 text-sm text-slate-600 font-medium">Đang kiểm tra mã mời và quyền truy cập...</p>
      </div>
    );
  }

  if (error || !invitation) {
    return (
      <div className="min-h-screen bg-slate-50 flex items-center justify-center p-4">
        <div className="bg-white rounded-3xl shadow-xl border border-slate-200 max-w-md w-full p-8 text-center space-y-5">
          <div className="w-16 h-16 bg-rose-50 text-rose-600 rounded-2xl flex items-center justify-center mx-auto border border-rose-100 shadow-2xs">
            <AlertCircle className="w-8 h-8" />
          </div>
          <div className="space-y-2">
            <h2 className="text-xl font-black text-slate-900">Liên kết không khả dụng</h2>
            <p className="text-xs text-slate-600 leading-relaxed">
              {error || 'Lời mời có thể đã hết hạn (quá 7 ngày), đã được sử dụng hoặc bị hủy bởi Quản lý đơn vị.'}
            </p>
          </div>
          <Link
            to="/login"
            className="inline-flex items-center justify-center w-full px-5 py-3 bg-slate-900 hover:bg-slate-800 text-white text-xs font-bold rounded-xl transition shadow-xs"
          >
            Quay lại trang Đăng nhập
          </Link>
        </div>
      </div>
    );
  }

  if (isSuccess) {
    return (
      <div className="min-h-screen bg-slate-50 flex items-center justify-center p-4">
        <div className="bg-white rounded-3xl shadow-xl border border-slate-200 max-w-md w-full p-8 text-center space-y-6">
          <div className="w-16 h-16 bg-emerald-50 text-emerald-600 rounded-2xl flex items-center justify-center mx-auto border border-emerald-100 shadow-2xs">
            <CheckCircle2 className="w-8 h-8" />
          </div>
          <div className="space-y-2">
            <h2 className="text-xl font-black text-slate-900">Chúc mừng bạn!</h2>
            <p className="text-xs text-slate-600 leading-relaxed">
              Bạn đã chính thức gia nhập <strong>{invitation.organizationName}</strong> với vai trò{' '}
              <span className="font-bold text-blue-600">{invitation.roleInEntityName}</span>.
            </p>
          </div>
          <div className="p-4 bg-slate-50 rounded-2xl border border-slate-100 text-left space-y-1 text-xs text-slate-600">
            <div>• Đăng nhập bằng email: <strong>{invitation.email}</strong></div>
            <div>• Kiểm soát công việc theo phân quyền Maker - Checker.</div>
          </div>
          <button
            type="button"
            onClick={() => navigate('/login')}
            className="w-full flex items-center justify-center space-x-2 px-5 py-3 bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold rounded-xl transition shadow-xs cursor-pointer"
          >
            <span>Đăng nhập ngay</span>
            <ArrowRight className="w-4 h-4" />
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-100 via-blue-50/40 to-slate-100 flex items-center justify-center p-4 sm:p-6">
      <div className="bg-white rounded-3xl shadow-2xl border border-slate-200 max-w-lg w-full overflow-hidden">
        {/* Banner Header */}
        <div className="bg-gradient-to-r from-blue-700 via-indigo-700 to-sky-700 p-7 text-white space-y-3 relative overflow-hidden">
          <div className="absolute right-0 top-0 translate-x-4 -translate-y-4 opacity-10 pointer-events-none">
            <Building2 className="w-48 h-48" />
          </div>
          <div className="inline-flex items-center space-x-1.5 px-3 py-1 bg-white/20 backdrop-blur-md rounded-full text-[11px] font-bold">
            <Sparkles className="w-3.5 h-3.5 text-amber-300" />
            <span>Thư Mời Tham Gia Đơn Vị</span>
          </div>
          <div>
            <h1 className="text-xl sm:text-2xl font-black tracking-tight">{invitation.organizationName}</h1>
            <p className="text-xs text-blue-100 mt-1 flex items-center space-x-2">
              <span>Mã số thuế: {invitation.taxCode || '---'}</span>
              <span>•</span>
              <span>Người mời: {invitation.invitedByName}</span>
            </p>
          </div>
        </div>

        {/* Thẻ Vai Trò Được Gán */}
        <div className="p-6 sm:p-8 space-y-6">
          <div className="p-4 rounded-2xl bg-slate-50 border border-slate-200/80 space-y-2">
            <div className="flex items-center justify-between">
              <span className="text-xs font-bold text-slate-500 uppercase tracking-wider">Vị trí đảm nhận</span>
              <span className="inline-flex items-center space-x-1 px-2.5 py-0.5 rounded-full text-xs font-black bg-blue-100 text-blue-800 border border-blue-200">
                <ShieldCheck className="w-3.5 h-3.5" />
                <span>{invitation.roleInEntityName}</span>
              </span>
            </div>
            <p className="text-xs text-slate-600 leading-relaxed">
              {invitation.roleInEntity === 'Officer' &&
                'Vai trò Cán bộ Kỹ thuật Hiện trường (Maker): Trực tiếp kiểm tra, lập biên bản xác nhận mốc thanh toán nghiệm thu trước khi chuyển cấp thẩm quyền giải ngân.'}
              {invitation.roleInEntity === 'Signer' &&
                'Vai trò Lãnh đạo / Người có thẩm quyền ký duyệt (Checker): Thẩm tra biên bản hiện trường và ký quyết định giải ngân tài chính cho nhà thầu.'}
              {invitation.roleInEntity === 'Finance' &&
                'Vai trò Ban Tài chính / Kế toán: Kiểm soát hóa đơn, chứng từ bảo lãnh và theo dõi dòng tiền giải ngân hợp đồng.'}
              {invitation.roleInEntity === 'Manager' &&
                'Vai trò Quản lý Đơn vị: Toàn quyền quản trị nhân sự, mời thành viên và điều hành các gói thầu của đơn vị.'}
            </p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-4">
            {/* Email (Readonly) */}
            <div className="space-y-1">
              <label className="text-xs font-bold text-slate-700">Email nhận lời mời</label>
              <div className="relative">
                <Mail className="w-4 h-4 text-slate-400 absolute left-3.5 top-3" />
                <input
                  type="email"
                  value={invitation.email}
                  disabled
                  className="w-full pl-10 pr-4 py-2.5 bg-slate-100 border border-slate-200 rounded-xl text-xs font-semibold text-slate-600 cursor-not-allowed"
                />
              </div>
            </div>

            {/* Nếu đã có tài khoản: Chỉ cần nút bấm kích hoạt */}
            {invitation.hasExistingAccount ? (
              <div className="p-4 bg-emerald-50/60 border border-emerald-200 rounded-2xl text-xs text-emerald-800 space-y-1">
                <div className="font-bold flex items-center space-x-1">
                  <UserCheck className="w-4 h-4 text-emerald-600" />
                  <span>Email này đã có tài khoản trên hệ thống</span>
                </div>
                <p>Nhấp vào nút bên dưới để liên kết quyền hạn của bạn với đơn vị mà không cần tạo mật khẩu mới.</p>
              </div>
            ) : (
              /* Nếu chưa có tài khoản: Nhập Họ tên và Mật khẩu mới */
              <>
                <div className="space-y-1">
                  <label className="text-xs font-bold text-slate-700">Họ và tên của bạn</label>
                  <div className="relative">
                    <User className="w-4 h-4 text-slate-400 absolute left-3.5 top-3" />
                    <input
                      type="text"
                      required
                      placeholder="VD: Nguyễn Văn B"
                      value={fullName}
                      onChange={(e) => setFullName(e.target.value)}
                      className="w-full pl-10 pr-4 py-2.5 bg-white border border-slate-200 rounded-xl text-xs focus:ring-2 focus:ring-blue-500 focus:outline-none transition"
                    />
                  </div>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <div className="space-y-1">
                    <label className="text-xs font-bold text-slate-700">Mật khẩu mới</label>
                    <div className="relative">
                      <Lock className="w-4 h-4 text-slate-400 absolute left-3.5 top-3" />
                      <input
                        type="password"
                        required
                        placeholder="Tối thiểu 6 ký tự"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        className="w-full pl-10 pr-4 py-2.5 bg-white border border-slate-200 rounded-xl text-xs focus:ring-2 focus:ring-blue-500 focus:outline-none transition"
                      />
                    </div>
                  </div>

                  <div className="space-y-1">
                    <label className="text-xs font-bold text-slate-700">Xác nhận mật khẩu</label>
                    <div className="relative">
                      <Lock className="w-4 h-4 text-slate-400 absolute left-3.5 top-3" />
                      <input
                        type="password"
                        required
                        placeholder="Nhập lại mật khẩu"
                        value={confirmPassword}
                        onChange={(e) => setConfirmPassword(e.target.value)}
                        className="w-full pl-10 pr-4 py-2.5 bg-white border border-slate-200 rounded-xl text-xs focus:ring-2 focus:ring-blue-500 focus:outline-none transition"
                      />
                    </div>
                  </div>
                </div>
              </>
            )}

            <button
              type="submit"
              disabled={submitting}
              className="w-full py-3 px-5 bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white text-xs font-bold rounded-xl transition shadow-md hover:shadow-lg flex items-center justify-center space-x-2 cursor-pointer mt-4"
            >
              <UserCheck className="w-4 h-4" />
              <span>
                {submitting
                  ? 'Đang xử lý...'
                  : invitation.hasExistingAccount
                  ? 'Chấp nhận & Gia nhập Đơn vị'
                  : 'Kích hoạt Tài khoản & Gia nhập Đơn vị'}
              </span>
            </button>
          </form>

          <div className="pt-4 border-t border-slate-100 flex items-center justify-between text-[11px] text-slate-500">
            <span className="flex items-center space-x-1">
              <Clock className="w-3.5 h-3.5 text-slate-400" />
              <span>Hạn kích hoạt: 7 ngày kể từ ngày gửi</span>
            </span>
            <Link to="/login" className="text-blue-600 hover:underline font-semibold">
              Trang đăng nhập
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
};

export default AcceptInvitationPage;
