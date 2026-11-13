import { useState, useCallback } from 'react';
import toast from 'react-hot-toast';
import { authApi } from '../api/authApi';

const TAX_CODE_PATTERN = /^(\d{10}(-\d{3})?|\d{13})$/;

/**
 * Custom Hook quản lý trạng thái và nghiệp vụ tra cứu Mã số thuế (MST) từ CSDL Quốc gia.
 * Loại bỏ việc nhân đôi state và hàm xử lý giữa form Nhà thầu và Bên mời thầu.
 */
export const useTaxCodeLookup = (onSuccessFill) => {
  const [taxLookupLoading, setTaxLookupLoading] = useState(false);
  const [taxVerified, setTaxVerified] = useState(null);

  const resetTaxVerification = useCallback(() => {
    setTaxVerified(null);
  }, []);

  const lookupTaxCode = useCallback(
    async (rawCode) => {
      const code = String(rawCode ?? '').trim();
      if (!code) {
        toast.error('Vui lòng nhập Mã số thuế để tra cứu');
        return;
      }

      const cleanCode = code.replace(/\s+/g, '');
      if (!TAX_CODE_PATTERN.test(cleanCode)) {
        toast.error('Mã số thuế gồm 10 hoặc 13 chữ số');
        return;
      }

      setTaxLookupLoading(true);
      try {
        const res = await authApi.lookupTaxCode(cleanCode);
        if (res?.success && res?.data) {
          setTaxVerified(res.data);
          onSuccessFill?.(cleanCode, res.data);
          toast.success(`Đã tự động điền: ${res.data.name}`);
          return;
        }

        setTaxVerified(false);
        toast.error(res?.message || 'Không tìm thấy MST trên CSDL Quốc gia');
      } catch (err) {
        setTaxVerified(false);
        toast.error(
          err?.response?.data?.message ||
            'Không tìm thấy MST trên CSDL Quốc gia. Bạn có thể tự nhập tay thông tin.'
        );
      } finally {
        setTaxLookupLoading(false);
      }
    },
    [onSuccessFill]
  );

  return {
    taxLookupLoading,
    taxVerified,
    resetTaxVerification,
    lookupTaxCode,
  };
};

export default useTaxCodeLookup;
