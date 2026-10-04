import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
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
  TrendingUp,
  AlertCircle,
  Scale,
  ShieldCheck,
  ChevronDown
} from 'lucide-react';
import { formatCurrency, formatDate, formatNumber } from '../../utils/formatters';

export const LeaderboardView = ({
  packageData,
  rankings = [],
  criteriaList = [],
  summary,
  onFinalizeClick,
  isAdminOrProcurement = false,
}) => {
  // Sort rankings by rank
  const sortedRankings = [...rankings].sort((a, b) => {
    if (a.rank && b.rank) return a.rank - b.rank;
    if (a.rank) return -1;
    if (b.rank) return 1;
    return (b.totalScore || 0) - (a.totalScore || 0);
  });

  const evaluatedSubmissions = sortedRankings.filter(
    (s) => s.totalScore !== null && s.totalScore !== undefined
  );

  const highestScore = evaluatedSubmissions.length > 0 ? evaluatedSubmissions[0].totalScore : null;
  const tiedWinners = evaluatedSubmissions.filter((s) => s.totalScore === highestScore);
  const hasTie = tiedWinners.length > 1;

  const rank1 = sortedRankings.find((s) => s.rank === 1) || sortedRankings[0];
  const rank2 = sortedRankings.find((s) => s.rank === 2) || (sortedRankings.length > 1 ? sortedRankings[1] : null);
  const rank3 = sortedRankings.find((s) => s.rank === 3) || (sortedRankings.length > 2 ? sortedRankings[2] : null);

  // State for user's chosen winner in the quick action block
  const [selectedWinnerId, setSelectedWinnerId] = useState(rank1?.submissionId || null);

  useEffect(() => {
    if (rank1?.submissionId && !selectedWinnerId) {
      setSelectedWinnerId(rank1.submissionId);
    }
  }, [rank1]);

  const currentlyChosenWinner =
    evaluatedSubmissions.find((s) => s.submissionId === Number(selectedWinnerId)) || rank1;

  const isPackageAwarded = packageData?.status === 'Awarded' || summary?.isFinalized;
  const isEvaluating = packageData?.status === 'Evaluating';

  return (
    <div className="space-y-6">
      {/* BANNER KHI ĐÃ TRAO THẦU CHÍNH THỨC */}
      {isPackageAwarded && (
        <div className="bg-gradient-to-r from-emerald-500 via-teal-600 to-emerald-700 rounded-2xl p-6 text-white shadow-md relative overflow-hidden">
          <div className="absolute right-0 top-0 translate-x-8 -translate-y-8 w-48 h-48 bg-white/10 rounded-full blur-2xl pointer-events-none" />
          <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-4 relative z-10">
            <div className="flex items-center gap-4">
              <div className="w-14 h-14 rounded-2xl bg-white/20 backdrop-blur-xs flex items-center justify-center flex-shrink-0 shadow-inner">
                <Crown className="w-8 h-8 text-amber-300" />
              </div>
              <div className="space-y-1">
                <div className="flex items-center gap-2">
                  <span className="px-2.5 py-0.5 rounded-full text-xs font-black bg-amber-400 text-amber-950 uppercase tracking-wider">
                    Đã trao thầu chính thức
                  </span>
                  <span className="text-xs text-emerald-100">
                    Mã hồ sơ: #{summary?.winningSubmissionId || rank1?.submissionId}
                  </span>
                </div>
                <h3 className="text-xl font-black tracking-tight">
                  {summary?.winningContractorName || rank1?.companyName}
                </h3>
                <p className="text-xs text-emerald-100 flex flex-wrap items-center gap-3">
                  <span>Điểm trúng thầu: <strong>{formatNumber(summary?.winningScore || rank1?.totalScore)}/100</strong></span>
                  <span>•</span>
                  <span>Giá trúng thầu: <strong className="text-amber-300 font-mono text-sm">{formatCurrency(summary?.winningBidPrice || rank1?.bidPrice || packageData?.budget || 0)}</strong></span>
                  <span>•</span>
                  <span>Ngân sách dự toán: <strong>{formatCurrency(packageData?.budget || 0)}</strong></span>
                </p>
              </div>
            </div>

            <div className="flex items-center gap-3">
              <Link
                to="/contracts"
                className="inline-flex items-center gap-2 px-5 py-2.5 text-xs font-bold text-emerald-900 bg-white hover:bg-emerald-50 rounded-xl shadow-xs transition-transform hover:scale-[1.02] cursor-pointer"
              >
                <FileCheck className="w-4 h-4 text-emerald-600" />
                <span>Xem phân hệ Hợp đồng</span>
                <ArrowRight className="w-3.5 h-3.5" />
              </Link>
            </div>
          </div>
        </div>
      )}

      {/* CẢNH BÁO TÌNH HUỐNG HÒA ĐIỂM (TIE-BREAKER NOTIFICATION) */}
      {hasTie && !isPackageAwarded && (
        <div className="bg-gradient-to-r from-indigo-50 to-blue-50 border border-indigo-200 rounded-2xl p-4 shadow-2xs flex items-center gap-3.5">
          <div className="p-2.5 bg-indigo-500 text-white rounded-xl flex-shrink-0">
            <Scale className="w-5 h-5" />
          </div>
          <div className="text-xs text-indigo-900 space-y-0.5">
            <p className="font-bold flex items-center gap-2">
              <span>⚡ Phát hiện tình huống hòa điểm cao nhất ({formatNumber(highestScore)} điểm)</span>
              <span className="bg-indigo-100 text-indigo-800 px-2 py-0.5 rounded-full text-[10px] font-black">
                {tiedWinners.length} nhà thầu bằng điểm
              </span>
            </p>
            <p className="text-slate-600">
              Có từ 2 nhà thầu đạt cùng điểm số. Theo quy định đấu thầu, Chủ đầu tư / Bên mời thầu có thẩm quyền xem xét các yếu tố bổ trợ (tiến độ, giải pháp, cam kết) để quyết định lựa chọn đơn vị trao thầu.
            </p>
          </div>
        </div>
      )}

      {/* KHỐI ĐIỀU KHIỂN TRAO THẦU LINH HOẠT (CHO PHÉP CHỦ ĐẦU TƯ CHỌN RANK 1 HOẶC RANK 2) */}
      {!isPackageAwarded && isEvaluating && evaluatedSubmissions.length > 0 && isAdminOrProcurement && (
        <div className="bg-gradient-to-r from-amber-50 via-orange-50/60 to-amber-50 border border-amber-300 rounded-2xl p-5 shadow-xs space-y-4">
          <div className="flex flex-col md:flex-row md:items-center justify-between gap-2 border-b border-amber-200/80 pb-3">
            <div className="flex items-center gap-2.5">
              <div className="p-2 bg-amber-500 text-white rounded-xl shadow-xs">
                <Trophy className="w-5 h-5" />
              </div>
              <div>
                <h4 className="text-sm font-bold text-slate-800">Trung Tâm Phê Duyệt Trao Thầu</h4>
                <p className="text-xs text-slate-600">
                  Lựa chọn nhà thầu trúng thầu chính thức để hoàn tất giai đoạn chấm điểm
                </p>
              </div>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-12 gap-4 items-center">
            {/* Bộ chọn Dropdown */}
            <div className="md:col-span-8 space-y-1.5">
              <label className="text-xs font-bold text-slate-700 flex items-center gap-1.5">
                <ShieldCheck className="w-4 h-4 text-amber-600" />
                <span>Chỉ định đơn vị được trao thầu:</span>
              </label>
              <div className="relative">
                <select
                  value={selectedWinnerId || ''}
                  onChange={(e) => setSelectedWinnerId(Number(e.target.value))}
                  className="w-full text-xs font-bold text-slate-800 bg-white border border-slate-300 hover:border-amber-400 rounded-xl px-3.5 py-2.5 focus:ring-2 focus:ring-amber-500 focus:outline-hidden appearance-none cursor-pointer pr-10 shadow-2xs"
                >
                  {evaluatedSubmissions.map((s) => {
                    const isTop1 = s.rank === 1;
                    const isTied = s.totalScore === highestScore;
                    return (
                      <option key={s.submissionId} value={s.submissionId}>
                        Hạng #{s.rank || '—'}: {s.companyName} — Điểm: {formatNumber(s.totalScore)}
                        {isTop1 ? ' (Đề xuất Hạng 1)' : isTied ? ' (Đồng Quán quân)' : ''}
                      </option>
                    );
                  })}
                </select>
                <ChevronDown className="w-4 h-4 text-slate-400 absolute right-3 top-1/2 -translate-y-1/2 pointer-events-none" />
              </div>
            </div>

            {/* Nút hành động Trao thầu */}
            <div className="md:col-span-4 flex items-end justify-start md:justify-end pt-2 md:pt-6">
              <button
                type="button"
                onClick={() => onFinalizeClick && onFinalizeClick(currentlyChosenWinner, hasTie)}
                className="w-full md:w-auto inline-flex items-center justify-center gap-2 px-6 py-2.5 text-xs font-bold text-white bg-gradient-to-r from-amber-600 to-amber-700 hover:from-amber-700 hover:to-amber-800 rounded-xl shadow-xs transition-transform hover:scale-[1.02] cursor-pointer flex-shrink-0"
              >
                <Award className="w-4 h-4 text-amber-200" />
                <span>Phê duyệt Trao thầu</span>
              </button>
            </div>
          </div>
        </div>
      )}

      {/* BỤC VINH DANH PODIUM (TOP 1, 2, 3) */}
      {sortedRankings.length > 0 && sortedRankings.some((s) => s.totalScore !== null) && (
        <div className="bg-white rounded-2xl border border-slate-200 p-6 shadow-xs">
          <div className="text-center max-w-md mx-auto mb-8 space-y-1">
            <h3 className="text-base font-bold text-slate-800 flex items-center justify-center gap-2">
              <Trophy className="w-5 h-5 text-amber-500" />
              Bục Vinh Danh Xếp Hạng Nhà Thầu
            </h3>
            <p className="text-xs text-slate-500">
              Thứ hạng tự động căn cứ trên điểm số tổng hợp trọng số của Hội đồng đánh giá
            </p>
          </div>

          {/* Podium layout */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 max-w-3xl mx-auto items-end pt-4">
            {/* Rank 2 - Silver (Left) */}
            <div className="order-2 md:order-1 flex flex-col items-center">
              {rank2 && rank2.totalScore ? (
                <div className="w-full text-center space-y-2">
                  <div className="w-12 h-12 rounded-full bg-slate-100 border-2 border-slate-300 text-slate-600 mx-auto flex items-center justify-center font-black text-sm shadow-xs">
                    🥈 2
                  </div>
                  <div className="px-2">
                    <p className="text-xs font-bold text-slate-800 line-clamp-1" title={rank2.companyName}>
                      {rank2.companyName}
                    </p>
                    <span className="text-[11px] text-slate-400 block">MST: {rank2.taxCode || 'N/A'}</span>
                  </div>
                  <div className="bg-gradient-to-t from-slate-200 to-slate-100 rounded-t-2xl p-4 h-28 flex flex-col justify-center border-t-2 border-slate-300 shadow-inner">
                    <span className="text-xs text-slate-500 font-medium">Hạng Nhì</span>
                    <span className="text-lg font-black text-slate-700">
                      {formatNumber(rank2.totalScore)}
                      <span className="text-xs font-normal text-slate-500"> đ</span>
                    </span>
                  </div>
                </div>
              ) : (
                <div className="w-full text-center p-6 border border-dashed border-slate-200 rounded-xl text-slate-300 text-xs">
                  Chưa xác định
                </div>
              )}
            </div>

            {/* Rank 1 - Gold (Center, Tallest) */}
            <div className="order-1 md:order-2 flex flex-col items-center">
              {rank1 && rank1.totalScore ? (
                <div className="w-full text-center space-y-2">
                  <div className="relative inline-block">
                    <Crown className="w-6 h-6 text-amber-500 absolute -top-4 left-1/2 -translate-x-1/2 animate-bounce" />
                    <div className="w-16 h-16 rounded-full bg-amber-100 border-2 border-amber-400 text-amber-800 mx-auto flex items-center justify-center font-black text-base shadow-md ring-4 ring-amber-200/50">
                      🥇 1
                    </div>
                  </div>
                  <div className="px-2">
                    <p className="text-sm font-black text-slate-900 line-clamp-1" title={rank1.companyName}>
                      {rank1.companyName}
                    </p>
                    <span className="text-[11px] text-slate-400 block">MST: {rank1.taxCode || 'N/A'}</span>
                  </div>
                  <div className="bg-gradient-to-t from-amber-200 via-amber-100 to-amber-50 rounded-t-2xl p-4 h-36 flex flex-col justify-center border-t-4 border-amber-400 shadow-md">
                    <span className="text-xs font-bold text-amber-800 uppercase tracking-wider">Quán Quân</span>
                    <span className="text-2xl font-black text-amber-900">
                      {formatNumber(rank1.totalScore)}
                      <span className="text-xs font-medium text-amber-700"> / 100</span>
                    </span>
                  </div>
                </div>
              ) : (
                <div className="w-full text-center p-8 border border-dashed border-amber-200 rounded-xl text-amber-400 text-xs">
                  Chờ chấm điểm
                </div>
              )}
            </div>

            {/* Rank 3 - Bronze (Right) */}
            <div className="order-3 md:order-3 flex flex-col items-center">
              {rank3 && rank3.totalScore ? (
                <div className="w-full text-center space-y-2">
                  <div className="w-12 h-12 rounded-full bg-amber-50 border-2 border-amber-600/40 text-amber-800 mx-auto flex items-center justify-center font-black text-sm shadow-xs">
                    🥉 3
                  </div>
                  <div className="px-2">
                    <p className="text-xs font-bold text-slate-800 line-clamp-1" title={rank3.companyName}>
                      {rank3.companyName}
                    </p>
                    <span className="text-[11px] text-slate-400 block">MST: {rank3.taxCode || 'N/A'}</span>
                  </div>
                  <div className="bg-gradient-to-t from-orange-100 to-amber-50/60 rounded-t-2xl p-4 h-24 flex flex-col justify-center border-t-2 border-amber-500/40 shadow-inner">
                    <span className="text-xs text-amber-800 font-medium">Hạng Ba</span>
                    <span className="text-lg font-black text-amber-900">
                      {formatNumber(rank3.totalScore)}
                      <span className="text-xs font-normal text-slate-500"> đ</span>
                    </span>
                  </div>
                </div>
              ) : (
                <div className="w-full text-center p-6 border border-dashed border-slate-200 rounded-xl text-slate-300 text-xs">
                  Chưa xác định
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* BẢNG SO SÁNH ĐỐI SOÁT CHI TIẾT & HÀNH ĐỘNG TRAO THẦU */}
      <div className="bg-white rounded-2xl border border-slate-200 shadow-xs overflow-hidden">
        <div className="p-5 border-b border-slate-200 flex items-center justify-between">
          <div>
            <h3 className="text-sm font-bold text-slate-800 flex items-center gap-2">
              <TrendingUp className="w-4 h-4 text-blue-600" />
              Bảng Tổng Hợp So Sánh Điểm Số
            </h3>
            <p className="text-xs text-slate-500">
              Chi tiết điểm số từng tiêu chí (tính trung bình cộng các giám khảo) và thao tác trao thầu trực tiếp
            </p>
          </div>
          <span className="text-xs font-semibold text-slate-500">
            {sortedRankings.length} nhà thầu tham gia
          </span>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs border-collapse">
            <thead>
              <tr className="bg-slate-50 border-b border-slate-200 text-slate-600 font-bold uppercase tracking-wider text-[11px]">
                <th className="py-3 px-4 w-16 text-center">Hạng</th>
                <th className="py-3 px-4 min-w-[200px]">Nhà thầu</th>
                <th className="py-3 px-4 text-right min-w-[130px]">Giá dự thầu</th>
                {criteriaList.map((crit) => (
                  <th
                    key={crit.id}
                    className="py-3 px-3 text-center min-w-[110px]"
                    title={`${crit.name} (Điểm bình quân các giám khảo)`}
                  >
                    <span className="block truncate">{crit.name}</span>
                    <span className="text-[10px] text-slate-400 font-normal">({crit.weight}%)</span>
                  </th>
                ))}
                <th className="py-3 px-4 text-right min-w-[110px] font-black">Tổng điểm</th>
                <th className="py-3 px-4 text-center w-24">Trạng thái</th>
                <th className="py-3 px-4 text-center w-28">Ngày nộp</th>
                {!isPackageAwarded && isEvaluating && isAdminOrProcurement && (
                  <th className="py-3 px-4 text-center w-36 font-black text-amber-800 bg-amber-50/60">
                    Quyết định Trao thầu
                  </th>
                )}
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {sortedRankings.map((sub) => {
                const isRank1 = sub.rank === 1;
                const isSelected = sub.status === 'Selected';
                const hasScore = sub.totalScore !== null && sub.totalScore !== undefined;

                return (
                  <tr
                    key={sub.submissionId}
                    className={`hover:bg-slate-50/80 transition-colors ${
                      isSelected
                        ? 'bg-amber-50/50'
                        : isRank1
                        ? 'bg-blue-50/30'
                        : ''
                    }`}
                  >
                    <td className="py-3.5 px-4 text-center">
                      {sub.rank === 1 ? (
                        <span className="inline-flex items-center justify-center w-7 h-7 rounded-full bg-amber-100 text-amber-800 font-black text-xs border border-amber-300">
                          🥇 1
                        </span>
                      ) : sub.rank === 2 ? (
                        <span className="inline-flex items-center justify-center w-7 h-7 rounded-full bg-slate-100 text-slate-700 font-black text-xs border border-slate-300">
                          🥈 2
                        </span>
                      ) : sub.rank === 3 ? (
                        <span className="inline-flex items-center justify-center w-7 h-7 rounded-full bg-amber-50 text-amber-900 font-black text-xs border border-amber-200">
                          🥉 3
                        </span>
                      ) : (
                        <span className="text-slate-400 font-semibold">
                          {sub.rank ? `#${sub.rank}` : '—'}
                        </span>
                      )}
                    </td>

                    <td className="py-3.5 px-4">
                      <p className="font-bold text-slate-800 text-xs flex items-center gap-1.5">
                        {sub.companyName}
                        {isSelected && (
                          <span className="px-1.5 py-0.2 bg-amber-100 text-amber-800 text-[10px] rounded font-bold">
                            Trúng thầu
                          </span>
                        )}
                      </p>
                      <span className="text-[11px] text-slate-400">MST: {sub.taxCode || 'N/A'}</span>
                    </td>

                    {/* Giá dự thầu */}
                    <td className="py-3.5 px-4 text-right font-mono">
                      {sub.bidPrice ? (
                        <div>
                          <span className="font-bold text-emerald-700 text-xs block">
                            {formatCurrency(sub.bidPrice)}
                          </span>
                          {packageData?.budget > 0 && (
                            <span className="text-[10px] text-slate-400 block">
                              {sub.bidPrice < packageData.budget ? (
                                <span className="text-emerald-600 font-semibold">
                                  Tiết kiệm {formatCurrency(packageData.budget - sub.bidPrice)}
                                </span>
                              ) : (
                                <span>Bằng dự toán</span>
                              )}
                            </span>
                          )}
                        </div>
                      ) : (
                        <span className="text-slate-400 font-mono text-[11px]">-</span>
                      )}
                    </td>

                    {/* Điểm từng tiêu chí (Điểm bình quân các giám khảo) */}
                    {criteriaList.map((crit) => {
                      const critScores = sub.scores?.filter((s) => s.criteriaId === crit.id) || [];
                      const avgScore = critScores.length > 0
                        ? critScores.reduce((sum, item) => sum + item.score, 0) / critScores.length
                        : null;
                      const breakdownTooltip = critScores.length > 0
                        ? critScores.map((s) => `${s.evaluatorName || 'Giám khảo'}: ${formatNumber(s.score)}đ`).join(' | ')
                        : '';

                      return (
                        <td
                          key={crit.id}
                          className="py-3.5 px-3 text-center"
                          title={breakdownTooltip ? `Chi tiết điểm: ${breakdownTooltip}` : ''}
                        >
                          {avgScore !== null ? (
                            <div className="cursor-help inline-block">
                              <span className="font-bold text-slate-700">{formatNumber(avgScore)}</span>
                              <span className="text-[10px] text-slate-400 block font-mono">
                                ({((avgScore * crit.weight) / 100).toFixed(1)})
                              </span>
                              {critScores.length > 1 && (
                                <span className="text-[9px] text-sky-600 font-semibold block">
                                  ({critScores.length} GK)
                                </span>
                              )}
                            </div>
                          ) : (
                            <span className="text-slate-300">—</span>
                          )}
                        </td>
                      );
                    })}

                    {/* Tổng điểm */}
                    <td className="py-3.5 px-4 text-right">
                      {hasScore ? (
                        <span className="text-sm font-extrabold text-blue-700">
                          {formatNumber(sub.totalScore)}
                          <span className="text-[10px] font-normal text-slate-400">/100</span>
                        </span>
                      ) : (
                        <span className="text-xs text-slate-400 font-medium">Chưa có điểm</span>
                      )}
                    </td>

                    {/* Trạng thái */}
                    <td className="py-3.5 px-4 text-center">
                      {sub.status === 'Selected' ? (
                        <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-bold bg-amber-100 text-amber-800">
                          Trúng thầu
                        </span>
                      ) : sub.status === 'Evaluated' ? (
                        <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-medium bg-emerald-100 text-emerald-800">
                          Đã chấm
                        </span>
                      ) : sub.status === 'Rejected' ? (
                        <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-medium bg-rose-100 text-rose-800">
                          Không đạt
                        </span>
                      ) : (
                        <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-medium bg-slate-100 text-slate-600">
                          Chờ chấm
                        </span>
                      )}
                    </td>

                    {/* Ngày nộp */}
                    <td className="py-3.5 px-4 text-center text-[11px] text-slate-500">
                      {formatDate(sub.submittedAt)}
                    </td>

                    {/* Nút Quyết định Trao thầu trực tiếp trên từng dòng */}
                    {!isPackageAwarded && isEvaluating && isAdminOrProcurement && (
                      <td className="py-3 px-4 text-center bg-amber-50/20">
                        {hasScore ? (
                          <button
                            type="button"
                            onClick={() => onFinalizeClick && onFinalizeClick(sub, hasTie)}
                            className={`inline-flex items-center gap-1 px-3 py-1.5 text-xs font-bold rounded-xl transition-all cursor-pointer shadow-2xs ${
                              isRank1
                                ? 'bg-amber-500 hover:bg-amber-600 text-white'
                                : 'bg-white hover:bg-amber-50 text-amber-800 border border-amber-300'
                            }`}
                          >
                            <Award className="w-3.5 h-3.5" />
                            <span>{isRank1 ? 'Trao thầu (Đề xuất)' : 'Chọn trao thầu'}</span>
                          </button>
                        ) : (
                          <span className="text-[11px] text-slate-400">Cần chấm điểm trước</span>
                        )}
                      </td>
                    )}
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};

export default LeaderboardView;
