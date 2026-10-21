import React, { useState, useEffect } from 'react';
import toast from 'react-hot-toast';
import { X, Award, Star, CheckCircle, AlertTriangle, ShieldAlert, Sparkles } from 'lucide-react';
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
        setQualityScore(existingEvaluation.qualityScore || 0);
        setScheduleScore(existingEvaluation.scheduleScore || 0);
        setComplianceScore(existingEvaluation.complianceScore || 0);
        setComments(existingEvaluation.comments || '');
      } else {
        // Mặc định cho điểm khởi tạo tích cực nếu hoàn thành tốt
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
        desc: 'Nhà thầu đạt chất lượng cao, hoàn thành đúng hạn và tuân thủ chặt chẽ cam kết.',
        badgeBg: 'bg-emerald-50 text-emerald-800 border-emerald-300',
        starColor: 'text-emerald-600',
      };
    }
    if (score >= 70) {
      return {
        grade: 'B',
        label: 'Hạng B - Đạt yêu cầu',
        desc: 'Nhà thầu đáp ứng đầy đủ yêu cầu hợp đồng, đủ điều kiện tham gia các gói thầu tiếp theo.',
        badgeBg: 'bg-blue-50 text-blue-800 border-blue-300',
        starColor: 'text-blue-600',
      };
    }
    if (score >= 50) {
      return {
        grade: 'C',
        label: 'Hạng C - Cần cải thiện',
        desc: 'Có phát sinh chậm trễ hoặc tồn tại khiếm khuyết nhỏ đã được khắc phục.',
        badgeBg: 'bg-amber-50 text-amber-800 border-amber-300',
        starColor: 'text-amber-600',
      };
    }
    return {
      grade: 'D',
      label: 'Hạng D - Cảnh báo rủi ro',
      desc: 'Vi phạm tiến độ nghiêm trọng hoặc chất lượng không đạt, đưa vào danh sách theo dõi đặc biệt.',
      badgeBg: 'bg-rose-50 text-rose-800 border-rose-300',
      starColor: 'text-rose-600',
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
        onSuccess();
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
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-xs overflow-y-auto">
      <div className="bg-white rounded-2xl max-w-xl w-full border border-slate-200 shadow-2xl overflow-hidden my-8 animate-in fade-in zoom-in-95 duration-200">
        {/* Header */}
        <div className="px-6 py-4 bg-gradient-to-r from-amber-600 via-orange-600 to-amber-700 text-white flex items-center justify-between">
          <div className="flex items-center gap-2.5">
            <div className="p-2 bg-white/10 rounded-xl">
              <Award className="w-5 h-5 text-amber-200" />
            </div>
            <div>
              <h3 className="text-base font-bold">Đánh Giá Nghiệm Thu & Xếp Hạng Nhà Thầu</h3>
              <p className="text-xs text-amber-100">
                Đánh giá tổng kết sau khi kết thúc hợp đồng theo 3 trụ cột chuẩn hóa
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="p-1.5 hover:bg-white/20 rounded-lg text-white/80 hover:text-white transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Body */}
        <form onSubmit={handleSubmit} className="p-6 space-y-4">
          {/* Thông tin nhà thầu */}
          <div className="p-3 bg-amber-50/50 border border-amber-200 rounded-xl text-xs flex justify-between items-center">
            <div>
              <span className="text-slate-500">Nhà thầu:</span>{' '}
              <strong className="text-slate-900 text-sm block">{contract?.companyName}</strong>
            </div>
            <div className="text-right">
              <span className="text-slate-500">Hợp đồng:</span>{' '}
              <strong className="text-amber-900 block">{contract?.contractNumber}</strong>
            </div>
          </div>

          {/* Tổng quan Điểm & Xếp hạng */}
          <div className={`p-4 rounded-xl border flex items-center justify-between gap-4 ${gradeInfo.badgeBg}`}>
            <div>
              <div className="flex items-center gap-1.5">
                <Sparkles className={`w-4 h-4 ${gradeInfo.starColor}`} />
                <span className="text-xs font-bold uppercase tracking-wider">Kết quả xếp hạng:</span>
              </div>
              <h4 className="text-lg font-black mt-0.5">{gradeInfo.label}</h4>
              <p className="text-[11px] opacity-90 mt-0.5 leading-snug">{gradeInfo.desc}</p>
            </div>
            <div className="text-right shrink-0">
              <span className="text-[11px] font-bold block uppercase opacity-75">Tổng điểm:</span>
              <span className="text-3xl font-black">{totalScore}</span>
              <span className="text-xs font-semibold opacity-75"> / 100</span>
            </div>
          </div>

          {/* 3 Trụ cột chấm điểm */}
          <div className="space-y-3.5">
            {/* Trụ cột 1: Chất lượng (0 - 40) */}
            <div className="p-3.5 bg-slate-50 border border-slate-200 rounded-xl space-y-2">
              <div className="flex justify-between items-center">
                <label className="text-xs font-bold text-slate-800 flex items-center gap-1.5">
                  <span>1. Chất lượng công trình / sản phẩm bàn giao</span>
                  <span className="text-[11px] text-slate-400 font-normal">(Tối đa 40 điểm)</span>
                </label>
                <span className="text-sm font-black text-blue-700 bg-blue-50 px-2 py-0.5 rounded-lg border border-blue-200">
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
                className="w-full accent-blue-600 cursor-pointer"
              />
              <p className="text-[11px] text-slate-500">
                Đạt tiêu chuẩn kỹ thuật, quy cách nghiệm thu, hồ sơ hoàn công đầy đủ và không bị từ chối hạng mục.
              </p>
            </div>

            {/* Trụ cột 2: Tiến độ (0 - 40) */}
            <div className="p-3.5 bg-slate-50 border border-slate-200 rounded-xl space-y-2">
              <div className="flex justify-between items-center">
                <label className="text-xs font-bold text-slate-800 flex items-center gap-1.5">
                  <span>2. Tiến độ hoàn thành so với hợp đồng</span>
                  <span className="text-[11px] text-slate-400 font-normal">(Tối đa 40 điểm)</span>
                </label>
                <span className="text-sm font-black text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-lg border border-emerald-200">
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
                className="w-full accent-emerald-600 cursor-pointer"
              />
              <p className="text-[11px] text-slate-500">
                Tỷ lệ hoàn thành đúng hạn các mốc thanh toán nghiệm thu, chủ động báo cáo tiến độ tuần.
              </p>
            </div>

            {/* Trụ cột 3: Tuân thủ & Bảo hành (0 - 20) */}
            <div className="p-3.5 bg-slate-50 border border-slate-200 rounded-xl space-y-2">
              <div className="flex justify-between items-center">
                <label className="text-xs font-bold text-slate-800 flex items-center gap-1.5">
                  <span>3. An toàn lao động, pháp lý & cam kết bảo hành</span>
                  <span className="text-[11px] text-slate-400 font-normal">(Tối đa 20 điểm)</span>
                </label>
                <span className="text-sm font-black text-purple-700 bg-purple-50 px-2 py-0.5 rounded-lg border border-purple-200">
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
                className="w-full accent-purple-600 cursor-pointer"
              />
              <p className="text-[11px] text-slate-500">
                Tuân thủ quy định PCCC, an toàn vệ sinh lao động, nộp bảo lãnh đúng hạn, phản hồi bảo hành nhanh.
              </p>
            </div>
          </div>

          {/* Nhận xét / Đánh giá chi tiết */}
          <div className="space-y-1">
            <label className="text-xs font-bold text-slate-700">
              Nhận xét tổng kết & Khuyến nghị hợp tác tiếp theo
            </label>
            <textarea
              rows={3}
              value={comments}
              onChange={(e) => setComments(e.target.value)}
              placeholder="Nhận xét chi tiết về năng lực, tinh thần hợp tác và đề xuất ưu tiên mời thầu cho các dự án sau..."
              className="w-full px-3 py-2 text-xs border border-slate-300 rounded-xl focus:ring-2 focus:ring-amber-500/20 focus:border-amber-500"
            />
          </div>

          {/* Footer actions */}
          <div className="pt-3 border-t border-slate-100 flex justify-end gap-2.5">
            <button
              type="button"
              onClick={onClose}
              disabled={submitting}
              className="px-4 py-2 border border-slate-300 text-slate-700 text-xs font-bold rounded-xl hover:bg-slate-100 transition-colors"
            >
              Hủy
            </button>
            <button
              type="submit"
              disabled={submitting}
              className="px-5 py-2 bg-gradient-to-r from-amber-600 to-orange-600 hover:from-amber-700 hover:to-orange-700 disabled:opacity-50 text-white text-xs font-bold rounded-xl shadow-xs transition-colors flex items-center gap-1.5"
            >
              <Award className="w-4 h-4" />
              <span>{submitting ? 'Đang lưu...' : 'Lưu Kết Quả Đánh Giá'}</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default ContractorEvaluationModal;
