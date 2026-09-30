import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import {
  FileText,
  X,
  Building2,
  Calendar,
  DollarSign,
  AlertCircle,
  CheckCircle,
  HelpCircle,
  Layers,
  ArrowRight,
  ShieldCheck,
  Briefcase
} from 'lucide-react';
import { contractApi } from '../../api/contractApi';
import { bidPackageApi } from '../../api/bidPackageApi';
import { formatCurrency, formatNumber } from '../../utils/formatters';

export const CreateContractModal = ({
  isOpen,
  onClose,
  onContractCreated,
  initialPackageId = null,
}) => {
  const navigate = useNavigate();

  // Package selection state
  const [packages, setPackages] = useState([]);
  const [selectedPkgId, setSelectedPkgId] = useState(initialPackageId || '');
  const [loadingPackages, setLoadingPackages] = useState(false);

  // Pre-fill data state
  const [awardedInfo, setAwardedInfo] = useState(null);
  const [loadingAwardedInfo, setLoadingAwardedInfo] = useState(false);
  const [infoError, setInfoError] = useState(null);

  // Form inputs
  const [contractNumber, setContractNumber] = useState('');
  const [value, setValue] = useState('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [terms, setTerms] = useState(
    '1. Nhà thầu cam kết thi công, cung cấp hàng hóa/dịch vụ theo đúng tiêu chuẩn kỹ thuật và tiến độ đã cam kết trong hồ sơ dự thầu.\n2. Chủ đầu tư nghiệm thu theo từng mốc khối lượng hoàn thành và giải ngân theo các điều khoản thanh toán.\n3. Thời hạn bảo hành tối thiểu 12 tháng kể từ ngày ký biên bản bàn giao nghiệm thu tổng thể.'
  );

  const [submitting, setSubmitting] = useState(false);

  // Set default dates (Start: today, End: +365 days)
  useEffect(() => {
    if (isOpen) {
      const today = new Date();
      const nextYear = new Date();
      nextYear.setDate(today.getDate() + 365);

      setStartDate(today.toISOString().split('T')[0]);
      setEndDate(nextYear.toISOString().split('T')[0]);

      if (initialPackageId) {
        setSelectedPkgId(initialPackageId);
        fetchAwardedBid(initialPackageId);
      } else {
        fetchPackagesList();
      }
    }
  }, [isOpen, initialPackageId]);

  // Fetch candidate packages for selection
  const fetchPackagesList = async () => {
    try {
      setLoadingPackages(true);
      const res = await bidPackageApi.getPackages({ pageSize: 50 });
      if (res?.data?.items) {
        setPackages(res.data.items);
      }
    } catch (err) {
      console.error('Lỗi khi tải danh sách gói thầu:', err);
    } finally {
      setLoadingPackages(false);
    }
  };

  // Fetch awarded bid pre-fill data for chosen package
  const fetchAwardedBid = async (pkgId) => {
    if (!pkgId) {
      setAwardedInfo(null);
      setInfoError(null);
      return;
    }

    try {
      setLoadingAwardedInfo(true);
      setInfoError(null);
      const res = await contractApi.getAwardedBidForContract(pkgId);
      if (res?.data) {
        const data = res.data;
        setAwardedInfo(data);

        // Pre-fill form fields
        if (data.suggestedContractNumber) {
          setContractNumber(data.suggestedContractNumber);
        }
        if (data.suggestedContractValue) {
          setValue(data.suggestedContractValue.toString());
        }
      }
    } catch (err) {
      const msg = err.message || 'Không thể lấy thông tin nhà thầu trúng thầu của gói thầu này.';
      setInfoError(msg);
      setAwardedInfo(null);
    } finally {
      setLoadingAwardedInfo(false);
    }
  };

  const handleSelectPackage = (e) => {
    const pkgId = Number(e.target.value);
    setSelectedPkgId(pkgId);
    fetchAwardedBid(pkgId);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!selectedPkgId) {
      toast.error('Vui lòng chọn gói thầu.');
      return;
    }

    if (!awardedInfo?.isAwarded) {
      toast.error('Gói thầu này chưa được phê duyệt trúng thầu.');
      return;
    }

    if (awardedInfo?.hasContract) {
      toast.error(`Gói thầu này đã có hợp đồng (Mã HĐ #${awardedInfo.existingContractId}).`);
      return;
    }

    if (!contractNumber.trim()) {
      toast.error('Vui lòng nhập số hợp đồng.');
      return;
    }

    const numValue = Number(value);
    if (!numValue || numValue <= 0) {
      toast.error('Giá trị hợp đồng phải lớn hơn 0.');
      return;
    }

    if (numValue > awardedInfo.estimatedBudget) {
      toast.error(`Giá trị hợp đồng (${formatCurrency(numValue)}) không được vượt quá dự toán gói thầu (${formatCurrency(awardedInfo.estimatedBudget)}).`);
      return;
    }

    if (!startDate || !endDate) {
      toast.error('Vui lòng chọn ngày bắt đầu và kết thúc.');
      return;
    }

    if (new Date(endDate) <= new Date(startDate)) {
      toast.error('Ngày kết thúc hợp đồng phải sau ngày bắt đầu.');
      return;
    }

    try {
      setSubmitting(true);
      const payload = {
        bidPackageId: Number(selectedPkgId),
        contractorId: awardedInfo.contractorId,
        contractNumber: contractNumber.trim(),
        value: numValue,
        terms: terms.trim(),
        startDate: new Date(startDate).toISOString(),
        endDate: new Date(endDate).toISOString(),
      };

      const res = await contractApi.createContract(payload);
      if (res?.data) {
        toast.success('Khởi tạo hợp đồng kinh tế thành công!');
        onClose();
        if (onContractCreated) {
          onContractCreated(res.data);
        }
        // Direct to contract detail
        navigate(`/contracts/${res.data.id}`);
      }
    } catch (err) {
      toast.error(err.message || 'Không thể tạo hợp đồng kinh tế.');
    } finally {
      setSubmitting(false);
    }
  };

  if (!isOpen) return null;

  const isEligible = awardedInfo?.isAwarded && !awardedInfo?.hasContract;

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/60 backdrop-blur-xs flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-2xl border border-slate-200 w-full max-w-2xl overflow-hidden animate-in fade-in zoom-in duration-200">
        {/* Header */}
        <div className="bg-gradient-to-r from-blue-600 to-indigo-700 px-6 py-4 text-white flex items-center justify-between">
          <div className="flex items-center space-x-3">
            <div className="p-2 bg-white/20 rounded-xl backdrop-blur-xs">
              <FileText className="w-6 h-6 text-white" />
            </div>
            <div>
              <h3 className="text-base font-bold">Lập Hợp Đồng Kinh Tế Mới</h3>
              <p className="text-xs text-blue-100">Kế thừa kết quả trúng thầu & tạo hợp đồng bản thảo (Draft)</p>
            </div>
          </div>
          <button
            onClick={onClose}
            disabled={submitting}
            className="text-white/80 hover:text-white p-1.5 rounded-lg hover:bg-white/10 transition-colors cursor-pointer"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Content Form */}
        <form onSubmit={handleSubmit} className="p-6 space-y-4 max-h-[80vh] overflow-y-auto">
          {/* 1. Chọn gói thầu */}
          <div className="space-y-1.5">
            <label className="text-xs font-bold text-slate-700 flex items-center gap-1.5">
              <Layers className="w-4 h-4 text-blue-600" />
              <span>Gói thầu liên kết <span className="text-rose-500">*</span></span>
            </label>
            {initialPackageId ? (
              <div className="p-3 bg-slate-50 border border-slate-200 rounded-xl text-xs font-bold text-slate-800">
                {awardedInfo ? `[${awardedInfo.packageCode}] ${awardedInfo.packageName}` : `Gói thầu #${initialPackageId}`}
              </div>
            ) : (
              <select
                value={selectedPkgId}
                onChange={handleSelectPackage}
                disabled={loadingPackages || loadingAwardedInfo}
                className="w-full text-xs font-medium text-slate-800 bg-white border border-slate-300 rounded-xl px-3.5 py-2.5 focus:ring-2 focus:ring-blue-500 focus:outline-hidden cursor-pointer"
              >
                <option value="">-- Chọn gói thầu đã trao thầu --</option>
                {packages.map((pkg) => (
                  <option key={pkg.id} value={pkg.id}>
                    [{pkg.code}] {pkg.name} ({pkg.status === 'Contracted' ? 'Đã ký HĐ' : pkg.isAwarded ? 'Đã trao thầu' : pkg.status})
                  </option>
                ))}
              </select>
            )}
          </div>

          {/* Loading status */}
          {loadingAwardedInfo && (
            <div className="py-6 text-center text-xs text-slate-500 flex items-center justify-center gap-2">
              <span className="w-4 h-4 border-2 border-blue-600 border-t-transparent rounded-full animate-spin"></span>
              Đang tra cứu kết quả trúng thầu của gói thầu...
            </div>
          )}

          {/* Error status */}
          {infoError && (
            <div className="p-3.5 bg-rose-50 border border-rose-200 rounded-xl text-xs text-rose-700 flex items-start gap-2.5">
              <AlertCircle className="w-4 h-4 text-rose-600 flex-shrink-0 mt-0.5" />
              <div>
                <p className="font-bold">Không thể lập hợp đồng cho gói thầu này</p>
                <p>{infoError}</p>
              </div>
            </div>
          )}

          {/* Pre-fill warning if not awarded or already has contract */}
          {awardedInfo && !awardedInfo.isAwarded && (
            <div className="p-3.5 bg-amber-50 border border-amber-200 rounded-xl text-xs text-amber-800 flex items-start gap-2.5">
              <AlertCircle className="w-4 h-4 text-amber-600 flex-shrink-0 mt-0.5" />
              <div>
                <p className="font-bold">Gói thầu chưa trao thầu</p>
                <p>Gói thầu này chưa hoàn tất bước chấm điểm và phê duyệt nhà thầu trúng thầu (Selected). Vui lòng hoàn tất thẩm định trước khi lập hợp đồng.</p>
              </div>
            </div>
          )}

          {awardedInfo && awardedInfo.hasContract && (
            <div className="p-3.5 bg-indigo-50 border border-indigo-200 rounded-xl text-xs text-indigo-900 flex items-start gap-2.5">
              <ShieldCheck className="w-4 h-4 text-indigo-600 flex-shrink-0 mt-0.5" />
              <div className="space-y-1">
                <p className="font-bold">Gói thầu đã có hợp đồng kinh tế</p>
                <p>Gói thầu này đã được lập Hợp đồng mã số <strong>#{awardedInfo.existingContractId}</strong>.</p>
                <button
                  type="button"
                  onClick={() => {
                    onClose();
                    navigate(`/contracts/${awardedInfo.existingContractId}`);
                  }}
                  className="text-xs font-bold text-indigo-600 hover:text-indigo-800 underline inline-flex items-center gap-1 cursor-pointer"
                >
                  <span>Chuyển tới xem hợp đồng hiện tại</span>
                  <ArrowRight className="w-3.5 h-3.5" />
                </button>
              </div>
            </div>
          )}

          {/* Thông tin nhà thầu trúng thầu (Pre-fill Card) */}
          {isEligible && (
            <div className="bg-gradient-to-r from-emerald-50 to-teal-50/50 border border-emerald-200 rounded-xl p-4 space-y-2">
              <div className="flex items-center justify-between">
                <span className="text-[11px] font-bold text-emerald-800 uppercase tracking-wider flex items-center gap-1">
                  <CheckCircle className="w-3.5 h-3.5 text-emerald-600" />
                  Đơn vị trúng thầu được chỉ định
                </span>
                {awardedInfo.totalScore && (
                  <span className="text-[11px] font-black bg-emerald-100 text-emerald-800 px-2 py-0.5 rounded-full">
                    Điểm: {formatNumber(awardedInfo.totalScore)} (Hạng #{awardedInfo.rank || 1})
                  </span>
                )}
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-2 text-xs">
                <div>
                  <span className="text-slate-500 block">Tên doanh nghiệp:</span>
                  <strong className="text-slate-900">{awardedInfo.companyName}</strong>
                </div>
                <div>
                  <span className="text-slate-500 block">Mã số thuế:</span>
                  <strong className="text-slate-900">{awardedInfo.taxCode || 'N/A'}</strong>
                </div>
                {awardedInfo.phone && (
                  <div>
                    <span className="text-slate-500 block">Điện thoại liên hệ:</span>
                    <span className="text-slate-800">{awardedInfo.phone}</span>
                  </div>
                )}
                <div>
                  <span className="text-slate-500 block">Dự toán gói thầu:</span>
                  <span className="text-slate-800 font-semibold">{formatCurrency(awardedInfo.estimatedBudget)}</span>
                </div>
              </div>
            </div>
          )}

          {/* Form Fields: Only active when eligible */}
          {isEligible && (
            <>
              {/* Số hợp đồng & Giá trị */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="space-y-1">
                  <label className="text-xs font-bold text-slate-700 flex items-center justify-between">
                    <span>Số hiệu Hợp đồng <span className="text-rose-500">*</span></span>
                    <span className="text-[10px] text-slate-400 font-normal">Gợi ý tự động</span>
                  </label>
                  <input
                    type="text"
                    required
                    value={contractNumber}
                    onChange={(e) => setContractNumber(e.target.value)}
                    placeholder="VD: CTR-2026-CLOUD-01"
                    className="w-full text-xs font-bold text-slate-800 bg-white border border-slate-300 rounded-xl px-3.5 py-2.5 focus:ring-2 focus:ring-blue-500 focus:outline-hidden"
                  />
                </div>

                <div className="space-y-1">
                  <label className="text-xs font-bold text-slate-700 flex items-center justify-between">
                    <span>Giá trị Hợp đồng (VNĐ) <span className="text-rose-500">*</span></span>
                    <span className="text-[10px] text-emerald-600 font-semibold">{formatCurrency(Number(value) || 0)}</span>
                  </label>
                  <input
                    type="number"
                    required
                    min={1}
                    max={awardedInfo.estimatedBudget}
                    value={value}
                    onChange={(e) => setValue(e.target.value)}
                    placeholder="Nhập giá trị hợp đồng..."
                    className="w-full text-xs font-bold text-slate-800 bg-white border border-slate-300 rounded-xl px-3.5 py-2.5 focus:ring-2 focus:ring-blue-500 focus:outline-hidden"
                  />
                </div>
              </div>

              {/* Thời hạn từ ngày - đến ngày */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="space-y-1">
                  <label className="text-xs font-bold text-slate-700 flex items-center gap-1.5">
                    <Calendar className="w-3.5 h-3.5 text-slate-500" />
                    <span>Ngày bắt đầu hiệu lực <span className="text-rose-500">*</span></span>
                  </label>
                  <input
                    type="date"
                    required
                    value={startDate}
                    onChange={(e) => setStartDate(e.target.value)}
                    className="w-full text-xs text-slate-800 bg-white border border-slate-300 rounded-xl px-3.5 py-2.5 focus:ring-2 focus:ring-blue-500 focus:outline-hidden"
                  />
                </div>

                <div className="space-y-1">
                  <label className="text-xs font-bold text-slate-700 flex items-center gap-1.5">
                    <Calendar className="w-3.5 h-3.5 text-slate-500" />
                    <span>Ngày kết thúc hiệu lực <span className="text-rose-500">*</span></span>
                  </label>
                  <input
                    type="date"
                    required
                    value={endDate}
                    onChange={(e) => setEndDate(e.target.value)}
                    className="w-full text-xs text-slate-800 bg-white border border-slate-300 rounded-xl px-3.5 py-2.5 focus:ring-2 focus:ring-blue-500 focus:outline-hidden"
                  />
                </div>
              </div>

              {/* Điều khoản hợp đồng */}
              <div className="space-y-1">
                <label className="text-xs font-bold text-slate-700 flex items-center justify-between">
                  <span>Điều khoản, thỏa thuận & bảo hành</span>
                  <span className="text-[10px] text-slate-400 font-normal">Tối đa 3000 ký tự</span>
                </label>
                <textarea
                  rows={4}
                  value={terms}
                  onChange={(e) => setTerms(e.target.value)}
                  placeholder="Ghi chú các điều khoản thanh toán, bảo hành và cam kết thi công..."
                  className="w-full text-xs text-slate-800 bg-white border border-slate-300 rounded-xl p-3 focus:ring-2 focus:ring-blue-500 focus:outline-hidden leading-relaxed"
                />
              </div>
            </>
          )}

          {/* Footer buttons */}
          <div className="pt-4 border-t border-slate-100 flex items-center justify-end gap-3">
            <button
              type="button"
              onClick={onClose}
              disabled={submitting}
              className="px-4 py-2.5 text-xs font-bold text-slate-600 hover:text-slate-800 hover:bg-slate-100 rounded-xl transition-colors cursor-pointer"
            >
              Hủy bỏ
            </button>
            <button
              type="submit"
              disabled={submitting || !isEligible}
              className="inline-flex items-center gap-2 px-6 py-2.5 text-xs font-bold text-white bg-blue-600 hover:bg-blue-700 disabled:bg-slate-300 disabled:cursor-not-allowed rounded-xl shadow-xs transition-transform hover:scale-[1.02] cursor-pointer"
            >
              {submitting ? (
                <>
                  <span className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                  <span>Đang khởi tạo...</span>
                </>
              ) : (
                <>
                  <FileText className="w-4 h-4" />
                  <span>Khởi tạo Hợp đồng (Bản thảo)</span>
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default CreateContractModal;
