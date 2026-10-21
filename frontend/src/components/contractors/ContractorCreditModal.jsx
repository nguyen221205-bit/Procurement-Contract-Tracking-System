import React, { useState, useEffect } from 'react';
import { X, Award, Star, Building2, Calendar, FileText, CheckCircle, AlertTriangle } from 'lucide-react';
import { contractApi } from '../../api/contractApi';
import LoadingSpinner from '../common/LoadingSpinner';
import { formatDate } from '../../utils/formatters';

const GRADE_STYLES = {
  A: 'bg-emerald-500 text-white',
  B: 'bg-blue-600 text-white',
  C: 'bg-amber-500 text-white',
  D: 'bg-rose-600 text-white',
};

export const ContractorCreditModal = ({ isOpen, onClose, contractorId, companyName }) => {
  const [profile, setProfile] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (isOpen && contractorId) {
      const fetchProfile = async () => {
        setLoading(true);
        try {
          const res = await contractApi.getContractorCreditProfile(contractorId);
          if (res?.data) {
            setProfile(res.data);
          }
        } catch (err) {
          console.error('Lỗi lấy hồ sơ tín nhiệm:', err);
        } finally {
          setLoading(false);
        }
      };
      fetchProfile();
    }
  }, [isOpen, contractorId]);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-xs overflow-y-auto">
      <div className="bg-white rounded-2xl max-w-2xl w-full border border-slate-200 shadow-2xl overflow-hidden my-8 animate-in fade-in zoom-in-95 duration-200">
        {/* Header */}
        <div className="px-6 py-4 bg-gradient-to-r from-blue-700 to-indigo-800 text-white flex items-center justify-between">
          <div className="flex items-center gap-2.5">
            <div className="p-2 bg-white/10 rounded-xl">
              <Award className="w-5 h-5 text-blue-200" />
            </div>
            <div>
              <h3 className="text-base font-bold">Hồ Sơ Năng Lực & Tín Nhiệm Nhà Thầu</h3>
              <p className="text-xs text-blue-100">{companyName}</p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="p-1.5 hover:bg-white/20 rounded-lg text-white/80 hover:text-white transition-colors cursor-pointer"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Content */}
        <div className="p-6 space-y-5">
          {loading ? (
            <div className="py-12 flex justify-center">
              <LoadingSpinner text="Đang tải hồ sơ tín nhiệm nhà thầu..." />
            </div>
          ) : !profile ? (
            <p className="text-xs text-slate-500 text-center py-8">Chưa có dữ liệu tín nhiệm cho nhà thầu này.</p>
          ) : (
            <>
              {/* Card Điểm tổng quan */}
              <div className="p-4 bg-slate-50 rounded-2xl border border-slate-200 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                <div className="flex items-center gap-3.5">
                  <div
                    className={`w-14 h-14 rounded-2xl flex items-center justify-center font-black text-2xl shadow-md ${
                      GRADE_STYLES[profile.ratingGrade] || 'bg-slate-500 text-white'
                    }`}
                  >
                    {profile.ratingGrade}
                  </div>
                  <div>
                    <span className="text-[10px] font-bold uppercase tracking-wider text-slate-400 block">
                      Phân hạng năng lực
                    </span>
                    <h4 className="text-base font-black text-slate-900">{profile.ratingGradeName}</h4>
                    <p className="text-xs text-slate-500">Mã số thuế: {profile.taxCode || 'N/A'}</p>
                  </div>
                </div>

                <div className="text-left sm:text-right bg-white p-3 rounded-xl border border-slate-200 shadow-2xs">
                  <span className="text-[11px] font-bold text-slate-500 block uppercase">
                    Điểm tín nhiệm tích lũy
                  </span>
                  <span className="text-3xl font-black text-amber-600">
                    {profile.rating?.toFixed(1) || '0.0'}
                  </span>
                  <span className="text-xs text-slate-400 font-semibold"> / 100</span>
                </div>
              </div>

              {/* Thống kê 3 trụ cột */}
              <div className="grid grid-cols-3 gap-3 text-xs">
                <div className="p-3 bg-blue-50/50 border border-blue-100 rounded-xl space-y-1">
                  <span className="text-blue-800 font-semibold block text-[11px]">Chất lượng TB:</span>
                  <p className="text-base font-black text-blue-900">
                    {profile.averageQualityScore} <span className="text-xs text-slate-400 font-normal">/ 40</span>
                  </p>
                </div>
                <div className="p-3 bg-emerald-50/50 border border-emerald-100 rounded-xl space-y-1">
                  <span className="text-emerald-800 font-semibold block text-[11px]">Tiến độ TB:</span>
                  <p className="text-base font-black text-emerald-900">
                    {profile.averageScheduleScore} <span className="text-xs text-slate-400 font-normal">/ 40</span>
                  </p>
                </div>
                <div className="p-3 bg-purple-50/50 border border-purple-100 rounded-xl space-y-1">
                  <span className="text-purple-800 font-semibold block text-[11px]">Tuân thủ & BH TB:</span>
                  <p className="text-base font-black text-purple-900">
                    {profile.averageComplianceScore} <span className="text-xs text-slate-400 font-normal">/ 20</span>
                  </p>
                </div>
              </div>

              {/* Thống kê hợp đồng */}
              <div className="grid grid-cols-3 gap-3 text-center text-xs">
                <div className="p-2.5 bg-slate-50 rounded-xl border border-slate-200">
                  <span className="text-slate-500 text-[11px] block">Tổng HĐ tham gia</span>
                  <strong className="text-base text-slate-800">{profile.totalContractsCount}</strong>
                </div>
                <div className="p-2.5 bg-emerald-50/60 rounded-xl border border-emerald-200">
                  <span className="text-emerald-700 text-[11px] block">HĐ hoàn thành tốt</span>
                  <strong className="text-base text-emerald-800">{profile.completedContractsCount}</strong>
                </div>
                <div className="p-2.5 bg-rose-50/60 rounded-xl border border-rose-200">
                  <span className="text-rose-700 text-[11px] block">HĐ bị chấm dứt</span>
                  <strong className="text-base text-rose-800">{profile.terminatedContractsCount}</strong>
                </div>
              </div>

              {/* Danh sách các lần đánh giá hợp đồng */}
              <div className="space-y-2">
                <h5 className="text-xs font-bold text-slate-800 uppercase tracking-wider">
                  Lịch sử đánh giá nghiệm thu qua các hợp đồng ({profile.evaluations?.length || 0})
                </h5>

                {profile.evaluations?.length === 0 ? (
                  <p className="text-xs text-slate-400 italic py-3 text-center bg-slate-50 rounded-xl border border-dashed border-slate-200">
                    Chưa có biên bản đánh giá hoàn thành hợp đồng nào.
                  </p>
                ) : (
                  <div className="space-y-2 max-h-56 overflow-y-auto pr-1">
                    {profile.evaluations.map((ev) => (
                      <div
                        key={ev.id}
                        className="p-3 bg-white rounded-xl border border-slate-200 shadow-2xs text-xs space-y-1.5"
                      >
                        <div className="flex justify-between items-center">
                          <span className="font-bold text-slate-900">Hợp đồng: {ev.contractNumber}</span>
                          <span
                            className={`px-2 py-0.5 rounded-full font-black text-[10px] ${
                              GRADE_STYLES[ev.ratingGrade] || 'bg-slate-500 text-white'
                            }`}
                          >
                            Hạng {ev.ratingGrade} ({ev.totalScore}đ)
                          </span>
                        </div>
                        {ev.bidPackageName && (
                          <p className="text-[11px] text-slate-500 truncate">{ev.bidPackageName}</p>
                        )}
                        {ev.comments && (
                          <p className="text-[11px] text-slate-600 italic bg-slate-50 p-2 rounded-lg">
                            "{ev.comments}"
                          </p>
                        )}
                        <span className="text-[10px] text-slate-400 block">
                          Đánh giá ngày: {formatDate(ev.evaluatedAt)} bởi {ev.evaluatorName}
                        </span>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </>
          )}

          <div className="pt-2 border-t border-slate-100 flex justify-end">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-bold rounded-xl transition-colors cursor-pointer"
            >
              Đóng
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ContractorCreditModal;
