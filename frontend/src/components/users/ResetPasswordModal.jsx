import React, { useState } from 'react';
import { X, KeyRound, Copy, Check, Eye, EyeOff, RefreshCw, AlertCircle, ShieldAlert } from 'lucide-react';
import toast from 'react-hot-toast';
import { userApi } from '../../api/userApi';

export const ResetPasswordModal = ({ isOpen, onClose, user, onSuccess }) => {
  const [customPassword, setCustomPassword] = useState('');
  const [useCustom, setUseCustom] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [resultData, setResultData] = useState(null);
  const [copied, setCopied] = useState(false);

  if (!isOpen || !user) return null;

  const handleGenerateRandom = () => {
    const chars = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%&*';
    let res = 'Reset@';
    for (let i = 0; i < 4; i++) {
      res += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    res += '!';
    setCustomPassword(res);
  };

  const handleCopy = (text) => {
    navigator.clipboard.writeText(text);
    setCopied(true);
    toast.success('Đã sao chép mật khẩu!');
    setTimeout(() => setCopied(false), 2000);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (useCustom && (!customPassword.trim() || customPassword.trim().length < 8)) {
      toast.error('Mật khẩu tự nhập phải có độ dài tối thiểu 8 ký tự.');
      return;
    }

    try {
      setLoading(true);
      const payload = useCustom && customPassword.trim() ? { newPassword: customPassword.trim() } : {};
      const res = await userApi.resetPassword(user.id, payload);

      if (res && res.success) {
        toast.success(res.message || 'Đặt lại mật khẩu thành công!');
        setResultData(res.data);
        onSuccess?.();
      } else {
        toast.error(res?.message || 'Không thể đặt lại mật khẩu.');
      }
    } catch (err) {
      toast.error(err.message || 'Lỗi khi đặt lại mật khẩu.');
    } finally {
      setLoading(false);
    }
  };

  const handleModalClose = () => {
    setResultData(null);
    setCustomPassword('');
    setUseCustom(false);
    onClose();
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/50 backdrop-blur-xs flex items-center justify-center p-4 animate-in fade-in duration-150">
      <div
        className="bg-white rounded-2xl max-w-md w-full p-6 shadow-2xl border border-slate-100 relative space-y-5"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Nút đóng */}
        <button
          onClick={handleModalClose}
          disabled={loading}
          className="absolute top-4 right-4 text-slate-400 hover:text-slate-600 p-1 rounded-lg hover:bg-slate-100 transition"
        >
          <X className="w-5 h-5" />
        </button>

        {/* Header Modal */}
        <div className="flex items-start space-x-3.5 pr-8">
          <div className="w-11 h-11 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center flex-shrink-0 shadow-2xs">
            <KeyRound className="w-5 h-5" />
          </div>
          <div>
            <h3 className="text-base font-bold text-slate-900">Đặt lại mật khẩu tài khoản</h3>
            <p className="text-xs text-slate-500 font-mono">{user.email}</p>
          </div>
        </div>

        {/* Trạng thái 1: Chưa submit - Hiển thị Form xác nhận */}
        {!resultData ? (
          <form onSubmit={handleSubmit} className="space-y-4 text-xs">
            {/* Khối thông tin người dùng */}
            <div className="p-3 bg-slate-50 rounded-xl border border-slate-200/80 space-y-1">
              <div className="flex justify-between">
                <span className="text-slate-500">Họ và tên:</span>
                <span className="font-semibold text-slate-800">{user.fullName}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-slate-500">Vai trò:</span>
                <span className="font-semibold text-slate-800">{user.roles?.join(', ') || 'Chưa gán'}</span>
              </div>
            </div>

            {/* Lựa chọn phương thức đặt mật khẩu */}
            <div className="space-y-2">
              <label className="font-semibold text-slate-800 block">Phương thức mật khẩu mới:</label>
              <div className="grid grid-cols-2 gap-2">
                <button
                  type="button"
                  onClick={() => {
                    setUseCustom(false);
                    setCustomPassword('');
                  }}
                  className={`p-2.5 rounded-xl border text-center transition ${
                    !useCustom
                      ? 'border-sky-500 bg-sky-50 text-sky-700 font-bold shadow-2xs'
                      : 'border-slate-200 text-slate-600 hover:bg-slate-50'
                  }`}
                >
                  Tự sinh ngẫu nhiên
                </button>
                <button
                  type="button"
                  onClick={() => {
                    setUseCustom(true);
                    if (!customPassword) handleGenerateRandom();
                  }}
                  className={`p-2.5 rounded-xl border text-center transition ${
                    useCustom
                      ? 'border-sky-500 bg-sky-50 text-sky-700 font-bold shadow-2xs'
                      : 'border-slate-200 text-slate-600 hover:bg-slate-50'
                  }`}
                >
                  Tự nhập mật khẩu
                </button>
              </div>
            </div>

            {/* Khung nhập mật khẩu tùy chọn */}
            {useCustom && (
              <div className="space-y-2 animate-in fade-in duration-150">
                <div className="flex items-center justify-between">
                  <label className="font-semibold text-slate-700">Mật khẩu mới:</label>
                  <button
                    type="button"
                    onClick={handleGenerateRandom}
                    className="inline-flex items-center space-x-1 text-[11px] text-sky-600 hover:underline"
                  >
                    <RefreshCw className="w-3 h-3" />
                    <span>Sinh ngẫu nhiên</span>
                  </button>
                </div>
                <div className="relative">
                  <input
                    type={showPassword ? 'text' : 'password'}
                    value={customPassword}
                    onChange={(e) => setCustomPassword(e.target.value)}
                    placeholder="Nhập tối thiểu 8 ký tự..."
                    className="w-full px-3.5 py-2 border border-slate-300 rounded-xl text-xs font-mono pr-16 focus:border-sky-500 outline-none"
                  />
                  <div className="absolute right-2 top-2 flex items-center space-x-1">
                    <button
                      type="button"
                      onClick={() => setShowPassword(!showPassword)}
                      className="p-1 text-slate-400 hover:text-slate-600"
                    >
                      {showPassword ? <EyeOff className="w-3.5 h-3.5" /> : <Eye className="w-3.5 h-3.5" />}
                    </button>
                    {customPassword && (
                      <button
                        type="button"
                        onClick={() => handleCopy(customPassword)}
                        className="p-1 text-slate-400 hover:text-sky-600"
                        title="Sao chép"
                      >
                        {copied ? <Check className="w-3.5 h-3.5 text-emerald-600" /> : <Copy className="w-3.5 h-3.5" />}
                      </button>
                    )}
                  </div>
                </div>
              </div>
            )}

            {/* Cảnh báo an toàn */}
            <div className="p-3 bg-amber-50 border border-amber-200/70 rounded-xl text-amber-800 text-[11px] flex items-start space-x-2">
              <ShieldAlert className="w-4 h-4 text-amber-600 shrink-0 mt-0.5" />
              <div>
                <strong>Lưu ý:</strong> Sau khi đặt lại mật khẩu, toàn bộ phiên đăng nhập hiện tại của người dùng sẽ bị thu hồi và họ bắt buộc phải đổi mật khẩu ở lần đăng nhập tới.
              </div>
            </div>

            {/* Action buttons */}
            <div className="pt-3 border-t border-slate-100 flex items-center justify-end space-x-2">
              <button
                type="button"
                onClick={handleModalClose}
                disabled={loading}
                className="px-4 py-2 text-slate-600 hover:bg-slate-100 rounded-xl transition"
              >
                Hủy
              </button>
              <button
                type="submit"
                disabled={loading}
                className="px-4 py-2 bg-amber-600 hover:bg-amber-700 text-white font-bold rounded-xl shadow-xs transition disabled:opacity-50 flex items-center space-x-1.5"
              >
                {loading ? 'Đang xử lý...' : 'Xác nhận đặt lại'}
              </button>
            </div>
          </form>
        ) : (
          /* Trạng thái 2: Đã Reset thành công - Hiển thị Mật khẩu mới để Admin copy */
          <div className="space-y-4 text-xs animate-in zoom-in-95 duration-150">
            <div className="p-4 bg-emerald-50 border border-emerald-200 rounded-2xl space-y-2.5">
              <div className="flex items-center space-x-2 text-emerald-800 font-bold">
                <Check className="w-4 h-4 text-emerald-600" />
                <span>Đặt lại mật khẩu thành công!</span>
              </div>
              <p className="text-slate-600 text-[11.5px]">
                Mật khẩu tạm thời mới đã được áp dụng. Vui lòng sao chép và gửi trực tiếp cho người dùng nếu email không thể nhận được:
              </p>

              <div className="p-3 bg-white border border-emerald-300 rounded-xl flex items-center justify-between">
                <span className="font-mono text-sm font-bold text-slate-900 select-all">
                  {resultData.newPassword}
                </span>
                <button
                  type="button"
                  onClick={() => handleCopy(resultData.newPassword)}
                  className="px-2.5 py-1 text-xs font-semibold text-emerald-700 bg-emerald-50 hover:bg-emerald-100 border border-emerald-200 rounded-lg transition flex items-center space-x-1"
                >
                  {copied ? <Check className="w-3.5 h-3.5" /> : <Copy className="w-3.5 h-3.5" />}
                  <span>{copied ? 'Đã chép' : 'Sao chép'}</span>
                </button>
              </div>

              {resultData.emailSent ? (
                <p className="text-[11px] text-emerald-700">✓ Đã gửi email thông báo kèm mật khẩu mới tới hộp thư người dùng.</p>
              ) : (
                <p className="text-[11px] text-amber-700">⚠ Chưa thể chuyển email đến hộp thư (vui lòng gửi mật khẩu thủ công).</p>
              )}
            </div>

            <div className="flex justify-end pt-2">
              <button
                type="button"
                onClick={handleModalClose}
                className="px-5 py-2 bg-slate-900 hover:bg-slate-800 text-white font-bold rounded-xl shadow-xs transition"
              >
                Đóng
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default ResetPasswordModal;
