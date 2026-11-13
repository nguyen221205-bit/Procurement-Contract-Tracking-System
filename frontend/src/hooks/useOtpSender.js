import { useState, useEffect, useCallback } from 'react';
import toast from 'react-hot-toast';
import { authApi } from '../api/authApi';

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/**
 * Custom Hook quản lý trạng thái gửi mã xác thực OTP qua Email kèm bộ đếm ngược Cooldown.
 */
export const useOtpSender = (purpose, defaultSuccessMessage) => {
  const [otpCooldown, setOtpCooldown] = useState(0);
  const [otpSending, setOtpSending] = useState(false);
  const [otpSent, setOtpSent] = useState(false);

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
        return;
      }

      setOtpSending(true);
      try {
        const res = await authApi.sendOtp(email, purpose);
        if (res?.success) {
          setOtpSent(true);
          setOtpCooldown(res.data?.cooldownSeconds || 60);
          toast.success(
            defaultSuccessMessage ||
              'Mã xác thực OTP đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư đến.',
            { duration: 6000 }
          );
          return;
        }

        toast.error(res?.message || 'Không thể gửi mã OTP');
      } catch (err) {
        toast.error(err?.response?.data?.message || err.message || 'Lỗi gửi mã OTP');
      } finally {
        setOtpSending(false);
      }
    },
    [purpose, defaultSuccessMessage]
  );

  return {
    otpCooldown,
    otpSending,
    otpSent,
    sendOtp,
  };
};

export default useOtpSender;
