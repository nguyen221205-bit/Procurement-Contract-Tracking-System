import React, { useState, useMemo } from 'react';
import {
  Trophy,
  Medal,
  Award,
  Crown,
  CheckCircle,
  FileCheck,
  Building2,
  Calendar,
  Sparkles,
  ArrowRight,
  TrendingDown,
  TrendingUp,
  AlertCircle,
  Scale,
  ShieldCheck,
  Columns3,
  Check,
  X,
  Printer,
  SlidersHorizontal,
  Star,
  Info
} from 'lucide-react';
import {
  formatCurrency,
  formatDate,
  formatDateTime,
  formatNumber,
  formatPercent
} from '../../utils/formatters';

export const BidComparisonView = ({
  packageData,
  rankings = [],
  criteriaList = [],
  summary,
  onFinalizeClick,
  isAdminOrProcurement = false,
}) => {
  // Sắp xếp danh sách nhà thầu theo thứ hạng (hoặc tổng điểm giảm dần)
  const sortedRankings = useMemo(() => {
    return [...rankings].sort((a, b) => {
      if (a.rank && b.rank) return a.rank - b.rank;
      if (a.rank) return -1;
      if (b.rank) return 1;
      return (b.totalScore || 0) - (a.totalScore || 0);
    });
  }, [rankings]);

  // Danh sách các hồ sơ hợp lệ (không bị rút hoặc bị loại)
  const validSubmissions = useMemo(() => {
    return sortedRankings.filter(
      (s) => s.status !== 'Withdrawn' && s.status !== 'Disqualified'
    );
  }, [sortedRankings]);

  // Khởi tạo các nhà thầu được chọn để so sánh (mặc định Top 3 hoặc Top 2)
  const [selectedIds, setSelectedIds] = useState(() => {
    const initial = validSubmissions.slice(0, 3).map((s) => s.submissionId);
    return initial.length > 0 ? initial : sortedRankings.slice(0, 3).map((s) => s.submissionId);
  });

  // Bộ lọc loại tiêu chí ('all' | 'technical' | 'price')
  const [criteriaFilter, setCriteriaFilter] = useState('all');

  // Lấy danh sách các nhà thầu đang được chọn so sánh
  const selectedSubmissions = useMemo(() => {
    return sortedRankings.filter((s) => selectedIds.includes(s.submissionId));
  }, [sortedRankings, selectedIds]);

  // Lọc danh sách tiêu chí theo bộ lọc
  const filteredCriteria = useMemo(() => {
    if (criteriaFilter === 'technical') {
      return criteriaList.filter((c) => !c.isPriceCriteria);
    }
    if (criteriaFilter === 'price') {
      return criteriaList.filter((c) => c.isPriceCriteria);
    }
    return criteriaList;
  }, [criteriaList, criteriaFilter]);

  // Bật/tắt chọn một nhà thầu để so sánh (tối đa 4)
  const handleToggleSubmission = (id) => {
    if (selectedIds.includes(id)) {
      if (selectedIds.length <= 1) return; // Giữ lại ít nhất 1
      setSelectedIds(selectedIds.filter((item) => item !== id));
    } else {
      if (selectedIds.length >= 4) {
        // Tối đa 4 nhà thầu để bảo đảm layout hiển thị đẹp mắt
        setSelectedIds([...selectedIds.slice(1), id]);
      } else {
        setSelectedIds([...selectedIds, id]);
      }
    }
  };

  // Các nút chọn nhanh
  const handleSelectTop = (count) => {
    const topIds = validSubmissions.slice(0, count).map((s) => s.submissionId);
    if (topIds.length > 0) {
      setSelectedIds(topIds);
    }
  };

  // Tính điểm trung bình của một tiêu chí cho một nhà thầu
  const getCriteriaAvgScore = (submission, criteriaId) => {
    if (!submission?.scores || submission.scores.length === 0) return null;
    const matching = submission.scores.filter((s) => s.criteriaId === criteriaId);
    if (matching.length === 0) return null;
    const total = matching.reduce((sum, s) => sum + (Number(s.score) || 0), 0);
    return total / matching.length;
  };

  // Tính điểm đóng góp theo trọng số: (Điểm / MaxScore) * Weight
  const getWeightedScore = (avgScore, maxScore, weight) => {
    if (avgScore === null || !maxScore || !weight) return 0;
    return (avgScore / maxScore) * weight;
  };

  // Xác định nhà thầu có điểm cao nhất ở từng tiêu chí để highlight
  const getBestCriteriaPerformerId = (criteriaId) => {
    let maxVal = -1;
    let bestId = null;
    selectedSubmissions.forEach((sub) => {
      const avg = getCriteriaAvgScore(sub, criteriaId);
      if (avg !== null && avg > maxVal) {
        maxVal = avg;
        bestId = sub.submissionId;
      }
    });
    return bestId;
  };

  // Xác định các chỉ số dẫn đầu tổng quan
  const budget = packageData?.budget || 0;

  // Nhà thầu có giá thấp nhất hợp lệ
  const lowestPriceSubmission = useMemo(() => {
    const withPrice = selectedSubmissions.filter((s) => s.bidPrice && s.bidPrice > 0);
    if (withPrice.length === 0) return null;
    return withPrice.reduce((min, s) => (s.bidPrice < min.bidPrice ? s : min), withPrice[0]);
  }, [selectedSubmissions]);

  // Nhà thầu có điểm cao nhất
  const highestScoreSubmission = useMemo(() => {
    const withScore = selectedSubmissions.filter((s) => s.totalScore !== null && s.totalScore !== undefined);
    if (withScore.length === 0) return null;
    return withScore.reduce((max, s) => (s.totalScore > max.totalScore ? s : max), withScore[0]);
  }, [selectedSubmissions]);

  // Màu sắc cột theo thứ tự nhà thầu
  const columnPalette = [
    {
      theme: 'blue',
      border: 'border-blue-300',
      bgLight: 'bg-blue-50/40',
      badge: 'bg-blue-100 text-blue-800 border-blue-200',
      bar: 'bg-blue-600',
      accentText: 'text-blue-700',
    },
    {
      theme: 'emerald',
      border: 'border-emerald-300',
      bgLight: 'bg-emerald-50/40',
      badge: 'bg-emerald-100 text-emerald-800 border-emerald-200',
      bar: 'bg-emerald-600',
      accentText: 'text-emerald-700',
    },
    {
      theme: 'purple',
      border: 'border-purple-300',
      bgLight: 'bg-purple-50/40',
      badge: 'bg-purple-100 text-purple-800 border-purple-200',
      bar: 'bg-purple-600',
      accentText: 'text-purple-700',
    },
    {
      theme: 'amber',
      border: 'border-amber-300',
      bgLight: 'bg-amber-50/40',
      badge: 'bg-amber-100 text-amber-800 border-amber-200',
      bar: 'bg-amber-600',
      accentText: 'text-amber-700',
    },
  ];

  // In bảng so sánh
  const handlePrint = () => {
    window.print();
  };

  // Trường hợp không đủ hồ sơ để so sánh
  if (sortedRankings.length < 2) {
    return (
      <div className="bg-white rounded-2xl border border-slate-200 p-12 text-center max-w-lg mx-auto space-y-4 my-8 shadow-xs">
        <div className="w-14 h-14 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mx-auto">
          <Columns3 className="w-7 h-7" />
        </div>
        <h3 className="text-base font-bold text-slate-800">
          Chưa đủ hồ sơ để so sánh trực quan
        </h3>
        <p className="text-xs text-slate-500 leading-relaxed">
          Gói thầu hiện có {sortedRankings.length} hồ sơ dự thầu. Tính năng So sánh Song song (Side-by-Side Comparison) yêu cầu tối thiểu 2 hồ sơ nộp thầu để thực hiện đối chiếu các tiêu chí kỹ thuật và tài chính.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* TIÊU ĐỀ IN ẤN CHUẨN HÀNH CHÍNH (CHỈ HIỂN THỊ KHI IN ẤN) */}
      <div className="hidden print:block text-center pb-4 border-b border-slate-300 mb-4">
        <h1 className="text-base font-bold uppercase">
          {isAwarded || summary?.isFinalized
            ? 'BẢNG SO SÁNH ĐỐI CHIẾU HỒ SƠ DỰ THẦU (CHÍNH THỨC)'
            : 'BẢNG SO SÁNH ĐỐI CHIẾU TIẾN ĐỘ HỒ SƠ DỰ THẦU (BẢN DỰ THẢO NỘI BỘ)'}
        </h1>
        <p className="text-xs text-slate-600 mt-0.5">
          Ngày kết xuất: {new Date().toLocaleDateString('vi-VN')} • Hệ thống Đấu thầu & Quản lý Hợp đồng
        </p>
        {!(isAwarded || summary?.isFinalized) && (
          <p className="text-[10px] text-amber-800 font-semibold italic mt-0.5">
            (*) Dữ liệu đối chiếu tiến độ phục vụ rà soát nội bộ của Tổ chuyên gia, chưa phải kết quả phê duyệt chính thức.
          </p>
        )}
      </div>

      {/* THANH ĐIỀU KHIỂN & BỘ CHỌN NHÀ THẦU */}
      <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-xs space-y-4 print:border-none print:shadow-none print:p-0">
        <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-4 border-b border-slate-100 pb-4">
          <div className="space-y-1">
            <div className="flex items-center gap-2">
              <span className="p-1.5 rounded-lg bg-blue-50 text-blue-600">
                <Columns3 className="w-4 h-4" />
              </span>
              <h2 className="text-sm font-bold text-slate-900 tracking-tight">
                So sánh Đối đầu Song song (Side-by-Side Comparison)
              </h2>
              <span className="px-2 py-0.5 rounded-full text-[11px] font-semibold bg-slate-100 text-slate-600">
                Đang chọn: {selectedSubmissions.length} / {sortedRankings.length} nhà thầu
              </span>
            </div>
            <p className="text-xs text-slate-500">
              Đối chiếu trực diện giá dự thầu, tỷ lệ tiết kiệm ngân sách, tổng điểm có trọng số và điểm chi tiết từng tiêu chí
            </p>
          </div>

          <div className="flex items-center flex-wrap gap-2">
            {/* Phím chọn nhanh */}
            <div className="inline-flex rounded-xl bg-slate-100 p-0.5 border border-slate-200 text-xs font-semibold">
              <button
                type="button"
                onClick={() => handleSelectTop(2)}
                className={`px-2.5 py-1 rounded-lg transition ${
                  selectedIds.length === 2 && selectedIds.every((id, idx) => id === validSubmissions[idx]?.submissionId)
                    ? 'bg-white text-blue-700 shadow-2xs font-bold'
                    : 'text-slate-600 hover:text-slate-900'
                }`}
              >
                Top 2
              </button>
              <button
                type="button"
                onClick={() => handleSelectTop(3)}
                className={`px-2.5 py-1 rounded-lg transition ${
                  selectedIds.length === 3 && selectedIds.every((id, idx) => id === validSubmissions[idx]?.submissionId)
                    ? 'bg-white text-blue-700 shadow-2xs font-bold'
                    : 'text-slate-600 hover:text-slate-900'
                }`}
              >
                Top 3
              </button>
              <button
                type="button"
                onClick={() => setSelectedIds(validSubmissions.slice(0, 4).map((s) => s.submissionId))}
                className={`px-2.5 py-1 rounded-lg transition ${
                  selectedIds.length === Math.min(4, validSubmissions.length)
                    ? 'bg-white text-blue-700 shadow-2xs font-bold'
                    : 'text-slate-600 hover:text-slate-900'
                }`}
              >
                Tất cả (tối đa 4)
              </button>
            </div>

            {/* Nút In ấn */}
            <button
              type="button"
              onClick={handlePrint}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-slate-700 bg-white border border-slate-200 rounded-xl hover:bg-slate-50 transition shadow-2xs"
              title="In bản so sánh"
            >
              <Printer className="w-3.5 h-3.5" />
              <span>In bảng</span>
            </button>
          </div>
        </div>

        {/* Danh sách chip chọn nhà thầu */}
        <div className="space-y-2">
          <span className="text-[11px] font-bold text-slate-400 uppercase tracking-wider block">
            Chọn nhà thầu tham gia so sánh (Tối đa 4):
          </span>
          <div className="flex flex-wrap gap-2">
            {sortedRankings.map((sub, idx) => {
              const isSelected = selectedIds.includes(sub.submissionId);
              const rank = sub.rank || idx + 1;
              return (
                <button
                  key={sub.submissionId}
                  type="button"
                  onClick={() => handleToggleSubmission(sub.submissionId)}
                  className={`inline-flex items-center gap-2 px-3 py-1.5 rounded-xl text-xs font-semibold transition border cursor-pointer ${
                    isSelected
                      ? 'bg-blue-50 border-blue-300 text-blue-900 shadow-2xs'
                      : 'bg-white border-slate-200 text-slate-600 hover:bg-slate-50 hover:border-slate-300'
                  }`}
                >
                  <span
                    className={`w-4 h-4 rounded-full flex items-center justify-center text-[10px] font-bold ${
                      rank === 1
                        ? 'bg-amber-400 text-amber-950'
                        : rank === 2
                        ? 'bg-slate-300 text-slate-800'
                        : rank === 3
                        ? 'bg-amber-700 text-white'
                        : 'bg-slate-200 text-slate-700'
                    }`}
                  >
                    {rank}
                  </span>
                  <span className="truncate max-w-[200px]">{sub.companyName}</span>
                  {isSelected ? (
                    <Check className="w-3.5 h-3.5 text-blue-600" />
                  ) : (
                    <span className="w-3.5 h-3.5 rounded-full border border-slate-300" />
                  )}
                </button>
              );
            })}
          </div>
        </div>
      </div>

      {/* KHU VỰC THẺ TỔNG QUAN ĐỐI ĐẦU (HEAD-TO-HEAD CARDS) */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
        {selectedSubmissions.map((sub, colIdx) => {
          const palette = columnPalette[colIdx % columnPalette.length];
          const rank = sub.rank || colIdx + 1;
          const isRank1 = rank === 1;
          const isLowestPrice = lowestPriceSubmission?.submissionId === sub.submissionId;
          const isHighestScore = highestScoreSubmission?.submissionId === sub.submissionId;
          const isWinning = sub.status === 'Selected' || summary?.winningSubmissionId === sub.submissionId;

          // Tính độ chênh lệch ngân sách
          const priceDiff = sub.bidPrice && budget > 0 ? budget - sub.bidPrice : 0;
          const savingsPercent = budget > 0 && sub.bidPrice ? ((budget - sub.bidPrice) / budget) * 100 : 0;

          return (
            <div
              key={sub.submissionId}
              className={`bg-white rounded-2xl border-2 p-5 shadow-xs transition-all relative flex flex-col justify-between ${
                isWinning
                  ? 'border-emerald-500 ring-2 ring-emerald-200'
                  : isRank1
                  ? 'border-amber-400'
                  : palette.border
              }`}
            >
              {/* Badge Xếp hạng trên cùng */}
              <div className="flex items-center justify-between gap-2 mb-3">
                <div className="flex items-center gap-1.5">
                  <span
                    className={`inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-bold ${
                      isRank1
                        ? 'bg-amber-100 text-amber-900 border border-amber-300'
                        : rank === 2
                        ? 'bg-slate-100 text-slate-800 border border-slate-300'
                        : rank === 3
                        ? 'bg-amber-50 text-amber-900 border border-amber-200'
                        : 'bg-slate-50 text-slate-700 border border-slate-200'
                    }`}
                  >
                    {isRank1 ? <Crown className="w-3.5 h-3.5 text-amber-600" /> : <Medal className="w-3.5 h-3.5" />}
                    <span>Hạng #{rank}</span>
                  </span>

                  {isWinning && (
                    <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-emerald-100 text-emerald-800 border border-emerald-300">
                      Trúng thầu
                    </span>
                  )}
                </div>

                <span className="text-[11px] text-slate-400 font-mono">
                  #{sub.submissionId}
                </span>
              </div>

              {/* Thông tin nhà thầu */}
              <div className="space-y-1">
                <h3 className="text-sm font-bold text-slate-900 leading-snug line-clamp-2" title={sub.companyName}>
                  {sub.companyName}
                </h3>
                <p className="text-[11px] text-slate-400 flex items-center gap-1.5">
                  <span>MST: {sub.taxCode || '—'}</span>
                  <span>•</span>
                  <span>Nộp: {formatDate(sub.submittedAt)}</span>
                </p>
              </div>

              {/* Điểm tổng hợp lớn */}
              <div className="my-4 p-3.5 rounded-xl bg-slate-50 border border-slate-100 space-y-2">
                <div className="flex items-baseline justify-between">
                  <span className="text-[11px] font-semibold text-slate-500 uppercase tracking-wider">
                    Tổng điểm có trọng số
                  </span>
                  <div className="flex items-baseline gap-0.5">
                    <span className="text-2xl font-black text-slate-900">
                      {sub.totalScore !== null && sub.totalScore !== undefined
                        ? formatNumber(sub.totalScore)
                        : '—'}
                    </span>
                    <span className="text-xs text-slate-400 font-bold">/100</span>
                  </div>
                </div>

                {/* Thanh tiến độ điểm */}
                <div className="w-full bg-slate-200 rounded-full h-2 overflow-hidden">
                  <div
                    className={`h-full transition-all duration-500 ${
                      (sub.totalScore || 0) >= 80
                        ? 'bg-emerald-500'
                        : (sub.totalScore || 0) >= 65
                        ? 'bg-blue-600'
                        : (sub.totalScore || 0) >= 50
                        ? 'bg-amber-500'
                        : 'bg-rose-500'
                    }`}
                    style={{ width: `${Math.min(100, sub.totalScore || 0)}%` }}
                  />
                </div>

                {isHighestScore && (
                  <div className="flex items-center gap-1 text-[11px] font-bold text-amber-700 bg-amber-50 px-2 py-0.5 rounded-md border border-amber-200">
                    <Star className="w-3 h-3 text-amber-500 fill-amber-500" />
                    <span>Điểm đánh giá cao nhất</span>
                  </div>
                )}
              </div>

              {/* Khối giá dự thầu & Ngân sách */}
              <div className="space-y-1.5 pt-2 border-t border-slate-100">
                <div className="flex items-baseline justify-between text-xs">
                  <span className="text-slate-500">Giá dự thầu:</span>
                  <span className="font-bold text-slate-900 font-mono text-sm">
                    {sub.bidPrice ? formatCurrency(sub.bidPrice) : '—'}
                  </span>
                </div>

                {budget > 0 && sub.bidPrice && (
                  <div className="flex items-center justify-between text-[11px]">
                    <span className="text-slate-400">So với dự toán:</span>
                    {priceDiff >= 0 ? (
                      <span className="font-semibold text-emerald-600 flex items-center gap-0.5">
                        <TrendingDown className="w-3 h-3" />
                        Tiết kiệm {formatPercent(savingsPercent)} ({formatCurrency(priceDiff)})
                      </span>
                    ) : (
                      <span className="font-semibold text-rose-600 flex items-center gap-0.5">
                        <TrendingUp className="w-3 h-3" />
                        Vượt {formatPercent(Math.abs(savingsPercent))}
                      </span>
                    )}
                  </div>
                )}

                {isLowestPrice && (
                  <div className="mt-1 flex items-center gap-1 text-[11px] font-bold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-md border border-emerald-200">
                    <CheckCircle className="w-3 h-3 text-emerald-600" />
                    <span>Giá dự thầu cạnh tranh nhất</span>
                  </div>
                )}
              </div>

              {/* Nút hành động Trao thầu nhanh */}
              {isAdminOrProcurement && packageData?.status === 'Evaluating' && !summary?.isFinalized && (
                <div className="mt-4 pt-3 border-t border-slate-100">
                  <button
                    type="button"
                    onClick={() => onFinalizeClick && onFinalizeClick(sub, false)}
                    className="w-full py-2 px-3 bg-blue-600 hover:bg-blue-700 text-white rounded-xl text-xs font-bold transition flex items-center justify-center gap-1.5 shadow-2xs cursor-pointer"
                  >
                    <Award className="w-3.5 h-3.5" />
                    <span>Xét trao thầu đơn vị này</span>
                  </button>
                </div>
              )}
            </div>
          );
        })}
      </div>

      {/* BẢNG SO SÁNH MA TRẬN TIÊU CHÍ CHI TIẾT (CRITERIA BREAKDOWN MATRIX) */}
      <div className="bg-white rounded-2xl border border-slate-200 shadow-xs overflow-hidden">
        {/* Header bảng & Bộ lọc tiêu chí */}
        <div className="px-6 py-4 bg-slate-50/80 border-b border-slate-200 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <Scale className="w-4 h-4 text-slate-700" />
            <h3 className="text-xs font-bold text-slate-800 uppercase tracking-wider">
              Bảng đối chiếu điểm số chi tiết từng tiêu chí đánh giá
            </h3>
          </div>

          <div className="flex items-center gap-2">
            <span className="text-[11px] text-slate-400 font-medium">Lọc tiêu chí:</span>
            <div className="inline-flex rounded-lg bg-white p-0.5 border border-slate-200 text-xs font-semibold">
              <button
                type="button"
                onClick={() => setCriteriaFilter('all')}
                className={`px-2 py-1 rounded-md transition ${
                  criteriaFilter === 'all' ? 'bg-slate-900 text-white' : 'text-slate-600 hover:text-slate-900'
                }`}
              >
                Tất cả ({criteriaList.length})
              </button>
              <button
                type="button"
                onClick={() => setCriteriaFilter('technical')}
                className={`px-2 py-1 rounded-md transition ${
                  criteriaFilter === 'technical' ? 'bg-slate-900 text-white' : 'text-slate-600 hover:text-slate-900'
                }`}
              >
                Kỹ thuật
              </button>
              <button
                type="button"
                onClick={() => setCriteriaFilter('price')}
                className={`px-2 py-1 rounded-md transition ${
                  criteriaFilter === 'price' ? 'bg-slate-900 text-white' : 'text-slate-600 hover:text-slate-900'
                }`}
              >
                Giá / Tài chính
              </button>
            </div>
          </div>
        </div>

        {/* Bảng dữ liệu tràn ngang linh hoạt */}
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse min-w-[700px]">
            <thead>
              <tr className="bg-slate-100/70 border-b border-slate-200 text-[11px] font-bold text-slate-600 uppercase tracking-wider">
                <th className="py-3.5 px-4 w-[280px]">Tiêu chí thẩm định</th>
                <th className="py-3.5 px-3 w-[100px] text-center">Trọng số</th>
                <th className="py-3.5 px-3 w-[100px] text-center">Thang điểm</th>
                {selectedSubmissions.map((sub, idx) => (
                  <th
                    key={sub.submissionId}
                    className="py-3.5 px-4 text-center min-w-[180px] border-l border-slate-200"
                  >
                    <div className="truncate font-bold text-slate-900" title={sub.companyName}>
                      {sub.companyName}
                    </div>
                    <span className="text-[10px] text-slate-400 font-normal">
                      Hạng #{sub.rank || idx + 1}
                    </span>
                  </th>
                ))}
              </tr>
            </thead>

            <tbody className="divide-y divide-slate-100 text-xs text-slate-700">
              {filteredCriteria.length === 0 ? (
                <tr>
                  <td colSpan={3 + selectedSubmissions.length} className="py-8 text-center text-slate-400 text-xs">
                    Không có tiêu chí nào phù hợp với bộ lọc.
                  </td>
                </tr>
              ) : (
                filteredCriteria.map((crit) => {
                  const bestPerformerId = getBestCriteriaPerformerId(crit.id);
                  return (
                    <tr key={crit.id} className="hover:bg-slate-50/70 transition-colors">
                      {/* Cột Tên tiêu chí */}
                      <td className="py-3 px-4">
                        <div className="flex items-start gap-2">
                          <span
                            className={`mt-0.5 px-1.5 py-0.5 rounded text-[10px] font-bold shrink-0 ${
                              crit.isPriceCriteria
                                ? 'bg-emerald-100 text-emerald-800 border border-emerald-200'
                                : 'bg-blue-100 text-blue-800 border border-blue-200'
                            }`}
                          >
                            {crit.isPriceCriteria ? 'Giá' : 'Kỹ thuật'}
                          </span>
                          <div>
                            <span className="font-semibold text-slate-900 block leading-tight">
                              {crit.name}
                            </span>
                            {crit.description && (
                              <span className="text-[11px] text-slate-400 line-clamp-1 mt-0.5">
                                {crit.description}
                              </span>
                            )}
                          </div>
                        </div>
                      </td>

                      {/* Trọng số */}
                      <td className="py-3 px-3 text-center font-bold text-slate-800">
                        {crit.weight}%
                      </td>

                      {/* Thang điểm tối đa */}
                      <td className="py-3 px-3 text-center text-slate-500 font-mono">
                        {formatNumber(crit.maxScore)}
                      </td>

                      {/* Điểm từng nhà thầu */}
                      {selectedSubmissions.map((sub) => {
                        const avg = getCriteriaAvgScore(sub, crit.id);
                        const isBest = bestPerformerId === sub.submissionId && avg !== null && avg > 0;
                        const weighted = getWeightedScore(avg, crit.maxScore, crit.weight);
                        const percentage = avg !== null && crit.maxScore ? (avg / crit.maxScore) * 100 : 0;

                        return (
                          <td
                            key={sub.submissionId}
                            className={`py-3 px-4 text-center border-l border-slate-100 transition-colors ${
                              isBest ? 'bg-emerald-50/30' : ''
                            }`}
                          >
                            {avg !== null ? (
                              <div className="space-y-1">
                                <div className="flex items-center justify-center gap-1.5">
                                  <span className="text-sm font-black text-slate-900">
                                    {formatNumber(avg)}
                                  </span>
                                  {isBest && (
                                    <span
                                      className="inline-flex items-center text-[10px] font-bold text-emerald-700 bg-emerald-100 px-1 rounded"
                                      title="Điểm cao nhất tiêu chí này"
                                    >
                                      ★ Top 1
                                    </span>
                                  )}
                                </div>

                                {/* Mini bar % đạt được */}
                                <div className="w-24 mx-auto bg-slate-200 rounded-full h-1.5 overflow-hidden">
                                  <div
                                    className={`h-full ${
                                      percentage >= 80
                                        ? 'bg-emerald-500'
                                        : percentage >= 60
                                        ? 'bg-blue-600'
                                        : 'bg-amber-500'
                                    }`}
                                    style={{ width: `${Math.min(100, percentage)}%` }}
                                  />
                                </div>

                                <span className="text-[10px] text-slate-400 block">
                                  Quy đổi: <strong>{formatNumber(weighted)}%</strong>
                                </span>
                              </div>
                            ) : (
                              <span className="text-xs text-slate-400 font-medium italic">
                                Chưa chấm
                              </span>
                            )}
                          </td>
                        );
                      })}
                    </tr>
                  );
                })
              )}

              {/* Hàng Tổng điểm tổng hợp */}
              <tr className="bg-slate-50/90 font-black border-t-2 border-slate-300 text-slate-900 text-xs">
                <td className="py-4 px-4 uppercase tracking-wider text-slate-800" colSpan={3}>
                  <div className="flex items-center gap-2">
                    <Trophy className="w-4 h-4 text-amber-500" />
                    <span>Tổng điểm đánh giá tổng hợp (Đã nhân trọng số 100%)</span>
                  </div>
                </td>
                {selectedSubmissions.map((sub) => {
                  const isTop = sub.rank === 1;
                  return (
                    <td
                      key={sub.submissionId}
                      className={`py-4 px-4 text-center border-l border-slate-200 ${
                        isTop ? 'bg-amber-50/50' : ''
                      }`}
                    >
                      <div className="flex items-baseline justify-center gap-1">
                        <span className="text-base font-black text-slate-900">
                          {sub.totalScore !== null && sub.totalScore !== undefined
                            ? formatNumber(sub.totalScore)
                            : '—'}
                        </span>
                        <span className="text-[10px] text-slate-500 font-bold">/ 100</span>
                      </div>
                    </td>
                  );
                })}
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      {/* BIỂU ĐỒ THANH TRỰC QUAN ĐỐI SÁNH TIÊU CHÍ (VISUAL BAR COMPARISON) */}
      <div className="bg-white rounded-2xl border border-slate-200 p-6 shadow-xs space-y-5">
        <div className="flex items-center gap-2 border-b border-slate-100 pb-3">
          <Sparkles className="w-4 h-4 text-blue-600" />
          <h3 className="text-xs font-bold text-slate-800 uppercase tracking-wider">
            Biểu đồ so sánh trực quan điểm số theo từng tiêu chí
          </h3>
        </div>

        <div className="space-y-4">
          {filteredCriteria.map((crit) => {
            return (
              <div key={crit.id} className="space-y-1.5 p-3 rounded-xl bg-slate-50/60 border border-slate-100">
                <div className="flex items-center justify-between text-xs">
                  <div className="flex items-center gap-2">
                    <span className="font-bold text-slate-800">{crit.name}</span>
                    <span className="text-[11px] text-slate-400">
                      (Trọng số: {crit.weight}%, Tối đa: {crit.maxScore}đ)
                    </span>
                  </div>
                </div>

                {/* Các thanh của từng nhà thầu đối đầu nhau */}
                <div className="space-y-1.5 pt-1">
                  {selectedSubmissions.map((sub, idx) => {
                    const avg = getCriteriaAvgScore(sub, crit.id);
                    const percentage = avg !== null && crit.maxScore ? (avg / crit.maxScore) * 100 : 0;
                    const palette = columnPalette[idx % columnPalette.length];

                    return (
                      <div key={sub.submissionId} className="flex items-center gap-3 text-xs">
                        <span className="w-36 truncate font-medium text-slate-600 text-[11px]" title={sub.companyName}>
                          {sub.companyName}:
                        </span>
                        <div className="flex-1 bg-slate-200 rounded-full h-2.5 overflow-hidden">
                          <div
                            className={`h-full rounded-full transition-all duration-500 ${palette.bar}`}
                            style={{ width: `${Math.min(100, percentage)}%` }}
                          />
                        </div>
                        <span className="w-14 text-right font-mono font-bold text-slate-800 text-[11px]">
                          {avg !== null ? `${formatNumber(avg)}đ` : '—'}
                        </span>
                      </div>
                    );
                  })}
                </div>
              </div>
            );
          })}
        </div>
      </div>

      {/* TỔNG KẾT ĐÁNH GIÁ ƯU - NHƯỢC ĐIỂM (COMPARATIVE INSIGHTS) */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {/* Card: Phân tích Tài chính & Giá */}
        <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-xs space-y-3">
          <div className="flex items-center gap-2 text-emerald-700">
            <TrendingDown className="w-4 h-4" />
            <h4 className="text-xs font-bold uppercase tracking-wider">
              Khía cạnh Hiệu quả Kinh tế & Giá dự thầu
            </h4>
          </div>
          <div className="space-y-2 text-xs text-slate-600">
            {lowestPriceSubmission ? (
              <p className="leading-relaxed">
                Đơn vị có giá dự thầu thấp nhất là <strong className="text-slate-900">{lowestPriceSubmission.companyName}</strong> với mức giá <strong className="text-emerald-700 font-mono">{formatCurrency(lowestPriceSubmission.bidPrice)}</strong>.
                {budget > 0 && lowestPriceSubmission.bidPrice < budget && (
                  <> Giúp chủ đầu tư tiết kiệm <strong className="text-emerald-700">{formatCurrency(budget - lowestPriceSubmission.bidPrice)}</strong> ({formatPercent(((budget - lowestPriceSubmission.bidPrice) / budget) * 100)}) so với ngân sách dự toán.</>
                )}
              </p>
            ) : (
              <p className="italic text-slate-400">Chưa có số liệu giá dự thầu hợp lệ.</p>
            )}
          </div>
        </div>

        {/* Card: Phân tích Kỹ thuật & Tổng thể */}
        <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-xs space-y-3">
          <div className="flex items-center gap-2 text-blue-700">
            <Trophy className="w-4 h-4" />
            <h4 className="text-xs font-bold uppercase tracking-wider">
              Khía cạnh Năng lực Kỹ thuật & Tổng hợp
            </h4>
          </div>
          <div className="space-y-2 text-xs text-slate-600">
            {highestScoreSubmission ? (
              <p className="leading-relaxed">
                Đơn vị dẫn đầu bảng xếp hạng tổng hợp là <strong className="text-slate-900">{highestScoreSubmission.companyName}</strong> với tổng điểm đạt <strong className="text-blue-700">{formatNumber(highestScoreSubmission.totalScore)} / 100 điểm</strong>.
                Hồ sơ này đáp ứng tiêu chuẩn cao nhất sau khi Tổ chuyên gia chấm điểm và quy đổi theo trọng số.
              </p>
            ) : (
              <p className="italic text-slate-400">Chưa có số liệu điểm chấm hoàn tất.</p>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default BidComparisonView;
