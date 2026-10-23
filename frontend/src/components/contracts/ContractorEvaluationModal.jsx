import React, { useState, useEffect } from 'react';
import toast from 'react-hot-toast';
import { X, Award, CheckCircle, Sparkles } from 'lucide-react';
import { contractApi } from '../../api/contractApi';

export const ContractorEvaluationModal = ({ isOpen, onClose, contract, existingEvaluation, onSuccess }) => {
  const [submitting, setSubmitting] = useState(false);
  const [qualityScore, setQualityScore] = useState(36);
  const [scheduleScore, setScheduleScore] = useState(35);
  const [complianceScore, setComplianceScore] = useState(18);
  const [comments, setComments] = useState('');

  useEffect(() => {
    if (isOpen) {
      if (existingEvaluation) {
        setQualityScore(existingEvaluation.qualityScore ?? 35);
        setScheduleScore(existingEvaluation.scheduleScore ?? 35);
        setComplianceScore(existingEvaluation.complianceScore ?? 18);
        setComments(existingEvaluation.comments || '');
      } else {
        setQualityScore(35);
        setScheduleScore(35);
        setComplianceScore(18);
        setComments('Nhà thầu chấp hành nghiêm túc tiến độ và quy chuẩn kỹ thuật theo thỏa thuận hợp đồng.');
      }
    }
  }, [isOpen, existingEvaluation]);

  if (!isOpen) return null;

  const totalScore = Number(qualityScore) + Number(scheduleScore) + Number(complianceScore);

  const getGradeInfo = (score) => {
    if (score >= 85) {
      return {
        grade: 'A',
        label: 'Hạng A - Xuất sắc',
        desc: 'Đạt chất lượng cao, hoàn thành đúng hạn và tuân thủ chặt chẽ cam kết.',
        badgeBg: 'bg-emerald-50 text-emerald-900 border-emerald-200',
        starColor: 'text-emerald-600',
        scoreColor: 'text-emerald-700',
      };
    }
    if (score >= 70) {
      return {
        grade: 'B',
        label: 'Hạng B - Đạt yêu cầu',
        desc: 'Đáp ứng đầy đủ yêu cầu hợp đồng, đủ điều kiện tham gia các gói tiếp theo.',
        badgeBg: 'bg-blue-50 text-blue-900 border-blue-200',
        starColor: 'text-blue-600',
        scoreColor: 'text-blue-700',
      };
    }
    if (score >= 50) {
      return {
        grade: 'C',
        label: 'Hạng C - Cần cải thiện',
        desc: 'Có phát sinh chậm trễ hoặc khiếm khuyết nhỏ đã được khắc phục.',
        badgeBg: 'bg-amber-50 text-amber-900 border-amber-200',
        starColor: 'text-amber-600',
        scoreColor: 'text-amber-700',
      };
    }
    return {
      grade: 'D',
      label: 'Hạng D - Cảnh báo rủi ro',
      desc: 'Vi phạm tiến độ nghiêm trọng hoặc chất lượng không đạt thỏa thuận.',
      badgeBg: 'bg-rose-50 text-rose-900 border-rose-200',
      starColor: 'text-rose-600',
      scoreColor: 'text-rose-700',
    };
  };

  const gradeInfo = getGradeInfo(totalScore);

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (qualityScore < 0 || qualityScore > 40) {
      toast.error('Điểm chất lượng phải từ 0 đến 40');
      return;
    }
    if (scheduleScore < 0 || scheduleScore > 40) {
      toast.error('Điểm tiến độ phải từ 0 đến 40');
      return;
    }
    if (complianceScore < 0 || complianceScore > 20) {
      toast.error('Điểm tuân thủ & bảo hành phải từ 0 đến 20');
      return;
    }

    setSubmitting(true);
    try {
      const payload = {
        qualityScore: Number(qualityScore),
        scheduleScore: Number(scheduleScore),
        complianceScore: Number(complianceScore),
        comments: comments.trim() || null,
      };

      const res = await contractApi.evaluateContractor(contract.id, payload);

      if (res?.success) {
        toast.success(`Đánh giá nhà thầu thành công! Xếp hạng: ${gradeInfo.label}`);
        onSuccess?.();
        onClose();
      } else {
        toast.error(res?.message || 'Không thể lưu đánh giá nhà thầu');
      }
    } catch (err) {
      console.error('Lỗi lưu đánh giá nhà thầu:', err);
      toast.error(err.response?.data?.message || err.message || 'Lỗi khi lưu kết quả đánh giá');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-3 sm:p-4 bg-slate-900/60 backdrop-blur-xs animate-in fade-in duration-150">
      <div className="relative w-full max-w-lg bg-white rounded-2xl border border-slate-200 shadow-2xl overflow-hidden flex flex-col max-h-[90vh] sm:max-h-[86vh]">
        {/* 1. Header cố định (Luôn hiển thị ở đỉnh, không bị trôi) */}
        <div className="px-5 py-3.5 bg-slate-50 border-b border-slate-100 flex items-center justify-between shrink-0">
          <div className="flex items-center gap-2.5 min-w-0">
            <div className="p-2 bg-amber-100 border border-amber-200 text-amber-700 rounded-xl shrink-0 shadow-2xs">
              <Award className="w-5 h-5" />
            </div>
            <div className="min-w-0">
              <h3 className="text-sm sm:text-base font-bold text-slate-900 truncate">
                Đánh Giá Nghiệm Thu & Xếp Hạng Nhà Thầu
              </h3>
              <p className="text-[11px] text-slate-500 truncate mt-0.5">
                {contract?.companyName} • <span className="font-mono text-slate-700 font-semibold">{contract?.contractNumber}</span>
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="p-1.5 text-slate-400 hover:text-slate-600 hover:bg-slate-200/60 rounded-xl transition-colors cursor-pointer shrink-0 ml-2"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* 2. Form Body (Cuộn mượt mà bên trong, không bao giờ làm tràn modal) */}
        <form id="eval-form" onSubmit={handleSubmit} className="flex-1 overflow-y-auto p-4 sm:p-5 space-y-3.5">
          {/* Tổng quan Điểm & Xếp hạng (Compact Metric Banner) */}
          <div className={`p-3 sm:p-3.5 rounded-xl border flex items-center justify-between gap-3 ${gradeInfo.badgeBg} shadow-2xs`}>
            <div className="min-w-0">
              <div className="flex items-center gap-1.5">
                <Sparkles className={`w-3.5 h-3.5 ${gradeInfo.starColor}`} />
                <span className="text-[10px] font-bold uppercase tracking-wider opacity-80">Kết quả xếp hạng:</span>
              </div>
              <h4 className="text-sm sm:text-base font-black mt-0.5 leading-tight">{gradeInfo.label}</h4>
              <p className="text-[11px] opacity-85 mt-0.5 line-clamp-1">{gradeInfo.desc}</p>
            </div>

            <div className="text-right shrink-0 pl-2 border-l border-current/15">
              <span className="text-[10px] font-bold block uppercase opacity-75">Tổng điểm:</span>
              <div className="flex items-baseline justify-end gap-0.5">
                <span className="text-2xl font-black">{totalScore}</span>
                <span className="text-xs font-semibold opacity-75">/100</span>
              </div>
            </div>
          </div>

          {/* 3 Trụ cột chấm điểm tinh gọn */}
          <div className="space-y-2.5">
            {/* Trụ cột 1: Chất lượng (0 - 40) */}
            <div className="p-3 bg-slate-50/80 hover:bg-slate-50 border border-slate-200/80 rounded-xl space-y-1.5 transition-colors">
              <div className="flex justify-between items-center text-xs">
                <label className="font-bold text-slate-800 flex items-center gap-1">
                  <span>1. Chất lượng sản phẩm / công trình</span>
                  <span className="text-[10px] text-slate-400 font-normal">(Tối đa 40)</span>
                </label>
                <span className="text-xs font-black text-blue-700 bg-blue-50 px-2 py-0.5 rounded-md border border-blue-200">
                  {qualityScore} / 40
                </span>
              </div>
              <input
                type="range"
                min="0"
                max="40"
                step="1"
                value={qualityScore}
                onChange={(e) => setQualityScore(e.target.value)}
                className="w-full h-1.5 bg-slate-200 rounded-lg appearance-none cursor-pointer accent-blue-600"
              />
              <p className="text-[10px] text-slate-500 line-clamp-1">
                Quy cách nghiệm thu, hồ sơ hoàn công đầy đủ, không bị từ chối hạng mục.
              </p>
            </div>

            {/* Trụ cột 2: Tiến độ (0 - 40) */}
            <div className="p-3 bg-slate-50/80 hover:bg-slate-50 border border-slate-200/80 rounded-xl space-y-1.5 transition-colors">
              <div className="flex justify-between items-center text-xs">
                <label className="font-bold text-slate-800 flex items-center gap-1">
                  <span>2. Tiến độ hoàn thành các mốc</span>
                  <span className="text-[10px] text-slate-400 font-normal">(Tối đa 40)</span>
                </label>
                <span className="text-xs font-black text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-md border border-emerald-200">
                  {scheduleScore} / 40
                </span>
              </div>
              <input
                type="range"
                min="0"
                max="40"
                step="1"
                value={scheduleScore}
                onChange={(e) => setScheduleScore(e.target.value)}
                className="w-full h-1.5 bg-slate-200 rounded-lg appearance-none cursor-pointer accent-emerald-600"
              />
              <p className="text-[10px] text-slate-500 line-clamp-1">
                Tỷ lệ hoàn thành đúng hạn các mốc nghiệm thu thanh toán theo tiến độ.
              </p>
            </div>

            {/* Trụ cột 3: Tuân thủ & Bảo hành (0 - 20) */}
            <div className="p-3 bg-slate-50/80 hover:bg-slate-50 border border-slate-200/80 rounded-xl space-y-1.5 transition-colors">
              <div className="flex justify-between items-center text-xs">
                <label className="font-bold text-slate-800 flex items-center gap-1">
                  <span>3. An toàn, pháp lý & cam kết bảo hành</span>
                  <span className="text-[10px] text-slate-400 font-normal">(Tối đa 20)</span>
                </label>
                <span className="text-xs font-black text-purple-700 bg-purple-50 px-2 py-0.5 rounded-md border border-purple-200">
                  {complianceScore} / 20
                </span>
              </div>
              <input
                type="range"
                min="0"
                max="20"
                step="1"
                value={complianceScore}
                onChange={(e) => setComplianceScore(e.target.value)}
                className="w-full h-1.5 bg-slate-200 rounded-lg appearance-none cursor-pointer accent-purple-600"
              />
              <p className="text-[10px] text-slate-500 line-clamp-1">
                An toàn lao động, nộp bảo lãnh đúng hạn, phản hồi và xử lý bảo hành nhanh.
              </p>
            </div>
          </div>

          {/* Nhận xét & Khuyến nghị */}
          <div className="space-y-1">
            <label className="text-xs font-bold text-slate-700 flex items-center justify-between">
              <span>Nhận xét tổng kết & Đề xuất hợp tác tiếp theo</span>
              <span className="text-[10px] text-slate-400 font-normal">(Tùy chọn)</span>
            </label>
            <textarea
              rows={2}
              value={comments}
              onChange={(e) => setComments(e.target.value)}
              placeholder="Nhận xét chi tiết về năng lực, tinh thần hợp tác và đề xuất ưu tiên mời thầu..."
              className="w-full px-3 py-2 text-xs border border-slate-300 rounded-xl focus:ring-2 focus:ring-amber-500/20 focus:border-amber-500 resize-none shadow-2xs"
            />
          </div>
        </form>

        {/* 3. Footer cố định (Luôn nhìn thấy nút bấm, không bị che lấp) */}
        <div className="px-5 py-3 bg-slate-50 border-t border-slate-100 flex items-center justify-end gap-2.5 shrink-0">
          <button
            type="button"
            onClick={onClose}
            disabled={submitting}
            className="px-4 py-2 border border-slate-300 bg-white text-slate-700 text-xs font-bold rounded-xl hover:bg-slate-100 transition-colors cursor-pointer shadow-2xs"
          >
            Đóng
          </button>
          <button
            type="submit"
            form="eval-form"
            disabled={submitting}
            className="px-5 py-2 bg-gradient-to-r from-amber-600 to-orange-600 hover:from-amber-700 hover:to-orange-700 disabled:opacity-50 text-white text-xs font-bold rounded-xl shadow-xs transition-colors flex items-center gap-1.5 cursor-pointer"
          >
            <CheckCircle className="w-4 h-4" />
            <span>{submitting ? 'Đang lưu...' : 'Lưu Kết Quả Đánh Giá'}</span>
          </button>
        </div>
      </div>
    </div>
  );
};

export default ContractorEvaluationModal;
