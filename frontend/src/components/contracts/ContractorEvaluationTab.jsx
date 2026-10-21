import React, { useState, useEffect } from 'react';
import toast from 'react-hot-toast';
import {
  Award,
  Star,
  CheckCircle,
  AlertTriangle,
  ShieldCheck,
  Building2,
  Calendar,
  Sparkles,
  TrendingUp,
  FileCheck,
  Edit3,
} from 'lucide-react';
import { contractApi } from '../../api/contractApi';
import { formatDate } from '../../utils/formatters';

const GRADE_CONFIG = {
  A: {
    label: 'Hạng A - Xuất sắc',
    badgeClass: 'bg-emerald-500 text-white shadow-emerald-200',
    cardBorder: 'border-emerald-300 bg-emerald-50/30',
    textColor: 'text-emerald-700',
    desc: 'Nhà thầu đạt chất lượng cao, hoàn thành đúng tiến độ cam kết và chấp hành quy chuẩn an toàn.',
  },
  B: {
    label: 'Hạng B - Đạt yêu cầu',
    badgeClass: 'bg-blue-600 text-white shadow-blue-200',
    cardBorder: 'border-blue-300 bg-blue-50/30',
    textColor: 'text-blue-700',
    desc: 'Nhà thầu hoàn thành đúng phạm vi hợp đồng, đáp ứng tiêu chuẩn nghiệm thu.',
  },
  C: {
    label: 'Hạng C - Cần cải thiện',
    badgeClass: 'bg-amber-500 text-white shadow-amber-200',
    cardBorder: 'border-amber-300 bg-amber-50/30',
    textColor: 'text-amber-800',
    desc: 'Có phát sinh chậm trễ tiến độ hoặc khiếm khuyết kỹ thuật nhỏ đã khắc phục.',
  },
  D: {
    label: 'Hạng D - Cảnh báo rủi ro',
    badgeClass: 'bg-rose-600 text-white shadow-rose-200',
    cardBorder: 'border-rose-300 bg-rose-50/30',
    textColor: 'text-rose-700',
    desc: 'Vi phạm tiến độ nghiêm trọng hoặc vi phạm nghĩa vụ hợp đồng, đưa vào danh sách cảnh báo.',
  },
};

export const ContractorEvaluationTab = ({
  contract,
  isAdminOrProcurement,
  onOpenEvaluationModal,
}) => {
  const evaluation = contract?.evaluation;
  const isContractFinished = contract?.status === 'Completed' || contract?.status === 'Terminated' || contract?.status === 2 || contract?.status === 3;
  const [creditProfile, setCreditProfile] = useState(null);
  const [loadingProfile, setLoadingProfile] = useState(false);

  useEffect(() => {
    if (contract?.contractorId) {
      const fetchProfile = async () => {
        setLoadingProfile(true);
        try {
          const res = await contractApi.getContractorCreditProfile(contract.contractorId);
          if (res?.data) {
            setCreditProfile(res.data);
          }
        } catch (err) {
          console.error('Lỗi lấy hồ sơ tín nhiệm nhà thầu:', err);
        } finally {
          setLoadingProfile(false);
        }
      };
      fetchProfile();
    }
  }, [contract?.contractorId]);

  const grade = evaluation?.ratingGrade || 'B';
  const gradeDetails = GRADE_CONFIG[grade] || GRADE_CONFIG.B;

  return (
    <div className="space-y-5">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-slate-100 pb-3">
        <div>
          <h3 className="text-sm font-bold text-slate-900 flex items-center gap-2">
            <Award className="w-4 h-4 text-amber-600" />
            <span>Đánh Giá Uy Tín & Nghiệm Thu Nhà Thầu (Vendor Performance Rating)</span>
          </h3>
          <p className="text-xs text-slate-500">
            Hồ sơ đánh giá nghiệm thu 3 trụ cột và điểm tín nhiệm tổng lũy kế của nhà thầu
          </p>
        </div>

        {isAdminOrProcurement && isContractFinished && (
          <button
            type="button"
            onClick={onOpenEvaluationModal}
            className="inline-flex items-center gap-1.5 px-4 py-2 bg-gradient-to-r from-amber-600 to-orange-600 hover:from-amber-700 hover:to-orange-700 text-white text-xs font-bold rounded-xl shadow-xs transition-colors cursor-pointer shrink-0"
          >
            {evaluation ? <Edit3 className="w-3.5 h-3.5" /> : <Award className="w-3.5 h-3.5" />}
            <span>{evaluation ? 'Cập Nhật Đánh Giá' : 'Đánh Giá Nghiệm Thu Ngay'}</span>
          </button>
        )}
      </div>

      {/* Trường hợp ĐÃ CÓ ĐÁNH GIÁ */}
      {evaluation ? (
        <div className={`p-5 rounded-2xl border ${gradeDetails.cardBorder} space-y-4 shadow-xs`}>
          {/* Card Top: Huy hiệu to & Tổng điểm */}
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-slate-200/60 pb-4">
            <div className="flex items-center gap-3.5">
              <div
                className={`w-14 h-14 rounded-2xl flex items-center justify-center font-black text-2xl shadow-md ${gradeDetails.badgeClass}`}
              >
                {grade}
              </div>
              <div>
                <span className="text-[10px] font-bold uppercase tracking-wider text-slate-500 block">
                  Xếp hạng nghiệm thu hợp đồng
                </span>
                <h4 className="text-base font-black text-slate-900">{gradeDetails.label}</h4>
                <p className="text-xs text-slate-600 max-w-md">{gradeDetails.desc}</p>
              </div>
            </div>

            <div className="text-left sm:text-right bg-white p-3.5 rounded-xl border border-slate-200 shadow-2xs">
              <span className="text-[11px] font-bold text-slate-500 block uppercase">
                Tổng điểm đánh giá
              </span>
              <div className="flex items-baseline gap-1 sm:justify-end">
                <span className="text-3xl font-black text-slate-900">{evaluation.totalScore}</span>
                <span className="text-xs font-semibold text-slate-500">/ 100</span>
              </div>
              <span className="text-[10px] text-slate-400 block">
                Ngày đánh giá: {formatDate(evaluation.evaluatedAt)}
              </span>
            </div>
          </div>

          {/* 3 Trụ cột chi tiết */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <div className="p-3.5 bg-white rounded-xl border border-slate-200 shadow-2xs space-y-1">
              <span className="text-[11px] font-bold text-blue-700 block">1. Chất lượng sản phẩm / DV:</span>
              <div className="flex items-baseline gap-1">
                <span className="text-xl font-black text-slate-900">{evaluation.qualityScore}</span>
                <span className="text-xs text-slate-500">/ 40 điểm</span>
              </div>
              <div className="w-full bg-slate-100 h-1.5 rounded-full overflow-hidden">
                <div
                  className="h-full bg-blue-600 rounded-full"
                  style={{ width: `${(evaluation.qualityScore / 40) * 100}%` }}
                />
              </div>
            </div>

            <div className="p-3.5 bg-white rounded-xl border border-slate-200 shadow-2xs space-y-1">
              <span className="text-[11px] font-bold text-emerald-700 block">2. Tiến độ thực hiện:</span>
              <div className="flex items-baseline gap-1">
                <span className="text-xl font-black text-slate-900">{evaluation.scheduleScore}</span>
                <span className="text-xs text-slate-500">/ 40 điểm</span>
              </div>
              <div className="w-full bg-slate-100 h-1.5 rounded-full overflow-hidden">
                <div
                  className="h-full bg-emerald-600 rounded-full"
                  style={{ width: `${(evaluation.scheduleScore / 40) * 100}%` }}
                />
              </div>
            </div>

            <div className="p-3.5 bg-white rounded-xl border border-slate-200 shadow-2xs space-y-1">
              <span className="text-[11px] font-bold text-purple-700 block">3. An toàn & Bảo hành:</span>
              <div className="flex items-baseline gap-1">
                <span className="text-xl font-black text-slate-900">{evaluation.complianceScore}</span>
                <span className="text-xs text-slate-500">/ 20 điểm</span>
              </div>
              <div className="w-full bg-slate-100 h-1.5 rounded-full overflow-hidden">
                <div
                  className="h-full bg-purple-600 rounded-full"
                  style={{ width: `${(evaluation.complianceScore / 20) * 100}%` }}
                />
              </div>
            </div>
          </div>

          {/* Nhận xét */}
          {evaluation.comments && (
            <div className="p-3.5 bg-white rounded-xl border border-slate-200 text-xs space-y-1">
              <span className="font-bold text-slate-800 flex items-center gap-1.5">
                <FileCheck className="w-4 h-4 text-emerald-600" />
                Nhận xét của Bên mời thầu / Hội đồng nghiệm thu:
              </span>
              <p className="text-slate-700 leading-relaxed italic bg-slate-50 p-2.5 rounded-lg border border-slate-100">
                "{evaluation.comments}"
              </p>
              <span className="text-[10px] text-slate-400 block pt-0.5">
                Người đánh giá: <strong>{evaluation.evaluatorName || 'Bên mời thầu'}</strong>
              </span>
            </div>
          )}
        </div>
      ) : (
        /* Trường hợp CHƯA CÓ ĐÁNH GIÁ */
        <div className="py-10 border-2 border-dashed border-slate-200 rounded-2xl text-center space-y-3 p-4">
          <Award className="w-10 h-10 text-amber-400 mx-auto" />
          <h4 className="text-xs font-bold text-slate-800">
            {isContractFinished
              ? 'Hợp đồng đã hoàn tất nhưng chưa được đánh giá nghiệm thu'
              : 'Đánh giá nhà thầu sẽ mở khi hợp đồng chuyển sang Completed'}
          </h4>
          <p className="text-xs text-slate-500 max-w-md mx-auto">
            {isContractFinished
              ? 'Thực hiện đánh giá tổng kết để ghi nhận điểm uy tín vào hồ sơ năng lực của nhà thầu trong hệ thống.'
              : 'Sau khi tất cả các mốc nghiệm thu hoàn thành và hợp đồng chuyển trạng thái Completed, Bên mời thầu sẽ tiến hành đánh giá 3 trụ cột (Chất lượng, Tiến độ, An toàn).'}
          </p>

          {isAdminOrProcurement && isContractFinished && (
            <button
              type="button"
              onClick={onOpenEvaluationModal}
              className="inline-flex items-center gap-1.5 px-4 py-2 bg-gradient-to-r from-amber-600 to-orange-600 hover:from-amber-700 hover:to-orange-700 text-white text-xs font-bold rounded-xl shadow-xs transition-colors cursor-pointer"
            >
              <Award className="w-4 h-4" />
              <span>Đánh Giá Nghiệm Thu Nhà Thầu</span>
            </button>
          )}
        </div>
      )}

      {/* Khu vực Hồ Sơ Uy Tín Nhà Thầu Tổng Lũy Kế */}
      {creditProfile && (
        <div className="bg-slate-50 border border-slate-200 rounded-2xl p-5 space-y-3.5">
          <div className="flex items-center justify-between border-b border-slate-200 pb-2.5">
            <h4 className="text-xs font-bold text-slate-800 uppercase tracking-wider flex items-center gap-2">
              <Building2 className="w-4 h-4 text-blue-600" />
              <span>Hồ Sơ Năng Lực & Tín Nhiệm Nhà Thầu Lũy Kế</span>
            </h4>
            <span className="text-[11px] font-bold text-slate-600">
              {creditProfile.companyName} (MST: {creditProfile.taxCode || 'N/A'})
            </span>
          </div>

          <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 text-xs">
            <div className="p-3 bg-white rounded-xl border border-slate-200 space-y-0.5">
              <span className="text-slate-500 text-[11px] block">Điểm tín nhiệm tích lũy:</span>
              <p className="text-lg font-black text-amber-600">
                {creditProfile.rating?.toFixed(1) || '0.0'}{' '}
                <span className="text-xs text-slate-400 font-semibold">/ 100</span>
              </p>
            </div>

            <div className="p-3 bg-white rounded-xl border border-slate-200 space-y-0.5">
              <span className="text-slate-500 text-[11px] block">Phân hạng năng lực:</span>
              <p className="text-sm font-black text-blue-700">{creditProfile.ratingGradeName}</p>
            </div>

            <div className="p-3 bg-white rounded-xl border border-slate-200 space-y-0.5">
              <span className="text-slate-500 text-[11px] block">Hợp đồng đã thực hiện:</span>
              <p className="text-sm font-black text-slate-800">
                {creditProfile.totalContractsCount} hợp đồng
              </p>
            </div>

            <div className="p-3 bg-white rounded-xl border border-slate-200 space-y-0.5">
              <span className="text-slate-500 text-[11px] block">Tỷ lệ hoàn thành tốt:</span>
              <p className="text-sm font-black text-emerald-700">
                {creditProfile.totalContractsCount > 0
                  ? Math.round(
                      (creditProfile.completedContractsCount / creditProfile.totalContractsCount) * 100
                    )
                  : 100}
                %
              </p>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default ContractorEvaluationTab;
