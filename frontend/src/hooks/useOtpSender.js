import { useState, useEffect, useCallback, useRef } from 'react';
import toast from 'react-hot-toast';
import { authApi } from '../api/authApi';

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/**
 * Custom Hook quản lý trạng thái gửi mã xác thực OTP qua Email kèm bộ đếm ngược Cooldown
 * và cơ chế hủy cập nhật trạng thái khi người dùng đổi email trong lúc request đang gửi.
 */
export const useOtpSender = (purpose = 'Register', defaultSuccessMessage) => {
  const [otpCooldown, setOtpCooldown] = useState(0);
  const [otpSending, setOtpSending] = useState(false);
  const [otpSent, setOtpSent] = useState(false);
  const [sentEmail, setSentEmail] = useState('');
  const inFlightEmailRef = useRef('');

  useEffect(() => {
    if (otpCooldown <= 0) return undefined;
    const timer = setTimeout(() => setOtpCooldown((c) => c - 1), 1000);
    return () => clearTimeout(timer);
  }, [otpCooldown]);

  const sendOtp = useCallback(
    async (rawEmail) => {
      const email = String(rawEmail ?? '').trim();
      if (!email || !EMAIL_PATTERN.test(email)) {
        toast.error('Vui lòng nhập địa chỉ email hợp lệ trước khi lấy mã OTP');
        return false;
      }

      inFlightEmailRef.current = email;
      setOtpSending(true);
      try {
        const res = await authApi.sendOtp(email, purpose);

        // Nếu email đã bị đổi/reset trong khi chờ request, bỏ qua kết quả trả về muộn
        if (inFlightEmailRef.current !== email) {
          return false;
        }

        if (res?.success) {
          setOtpSent(true);
          setSentEmail(email);
          setOtpCooldown(res.data?.cooldownSeconds || 60);
          toast.success(
            defaultSuccessMessage ||
              'Mã xác thực OTP đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư đến.',
            { duration: 6000 }
          );
          return true;
        }

        toast.error(res?.message || 'Không thể gửi mã OTP');
        return false;
      } catch (err) {
        if (inFlightEmailRef.current === email) {
          toast.error(err?.response?.data?.message || err.message || 'Lỗi gửi mã OTP');
        }
        return false;
      } finally {
        if (inFlightEmailRef.current === email) {
          setOtpSending(false);
        }
      }
    },
    [purpose, defaultSuccessMessage]
  );

  const resetOtp = useCallback(() => {
    inFlightEmailRef.current = '';
    setOtpCooldown(0);
    setOtpSending(false);
    setOtpSent(false);
    setSentEmail('');
  }, []);

  return {
    otpCooldown,
    otpSending,
    otpSent,
    sentEmail,
    sendOtp,
    resetOtp,
  };
};

export default useOtpSender;
