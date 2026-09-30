import React, { useState, useEffect } from 'react';
import toast from 'react-hot-toast';
import {
  FileText,
  UserCheck,
  CheckCircle2,
  AlertCircle,
  Save,
  Calculator,
  Building2,
  FileDown,
  Clock,
  Award,
  ChevronRight,
  Info,
  Lock
} from 'lucide-react';
import { evaluationApi } from '../../api/evaluationApi';
import { bidSubmissionApi } from '../../api/bidSubmissionApi';
import { formatCurrency, formatDate, formatNumber } from '../../utils/formatters';

export const ScorecardForm = ({
  packageData,
  criteriaList = [],
  submissions = [],
  onScoreSaved,
  readOnly = false,
}) => {
  const [selectedSubmissionId, setSelectedSubmissionId] = useState(
    submissions.length > 0 ? submissions[0].submissionId : null
  );
  const [scoresMap, setScoresMap] = useState({}); // { [criteriaId]: { score: number, comment: string } }
  const [saving, setSaving] = useState(false);
  const [submissionFiles, setSubmissionFiles] = useState([]);
  const [loadingFiles, setLoadingFiles] = useState(false);
  const [showFilesModal, setShowFilesModal] = useState(false);

  // Sync selected submission when submissions list changes
  useEffect(() => {
    if (submissions.length > 0 && !selectedSubmissionId) {
      setSelectedSubmissionId(submissions[0].submissionId);
    }
  }, [submissions]);

  // Current selected submission
  const currentSubmission = submissions.find(
    (s) => s.submissionId === selectedSubmissionId
  );

  // When switching submission, load existing scores if any
  useEffect(() => {
    if (!currentSubmission || !criteriaList.length) return;

    const initialScores = {};
    criteriaList.forEach((c) => {
      // Find existing score from currentSubmission.scores
      const existing = currentSubmission.scores?.find((es) => es.criteriaId === c.id);
      initialScores[c.id] = {
        score: existing ? existing.score : '',
        comment: existing ? existing.comment || '' : '',
      };
    });
    setScoresMap(initialScores);
  }, [selectedSubmissionId, currentSubmission, criteriaList]);

  // Fetch files for current submission
  useEffect(() => {
    const fetchFiles = async () => {
      if (!currentSubmission?.submissionId) return;
      try {
        setLoadingFiles(true);
        const res = await bidSubmissionApi.getSubmissionById(currentSubmission.submissionId);
        if (res?.data) {
          setSubmissionFiles(res.data.files || []);
        }
      } catch (err) {
        console.error('Failed to load submission files:', err);
      } finally {
        setLoadingFiles(false);
      }
    };

    fetchFiles();
  }, [currentSubmission?.submissionId]);

  // Handle score change
  const handleScoreChange = (criteriaId, val, maxScore) => {
    if (readOnly) return;
    const num = val === '' ? '' : parseFloat(val);
    if (num !== '' && (isNaN(num) || num < 0 || num > maxScore)) {
      return; // Out of bounds
    }
    setScoresMap((prev) => ({
      ...prev,
      [criteriaId]: {
        ...prev[criteriaId],
        score: val === '' ? '' : num,
      },
    }));
  };

  // Handle comment change
  const handleCommentChange = (criteriaId, text) => {
    if (readOnly) return;
    setScoresMap((prev) => ({
      ...prev,
      [criteriaId]: {
        ...prev[criteriaId],
        comment: text,
      },
    }));
  };

  // Calculate projected weighted total score
  const calculateTotalWeightedScore = () => {
    let sum = 0;
    criteriaList.forEach((c) => {
      const item = scoresMap[c.id];
      const score = item?.score !== '' && !isNaN(item?.score) ? Number(item.score) : 0;
      sum += (score * c.weight) / 100;
    });
    return Math.round(sum * 100) / 100;
  };

  // Count criteria with scores entered
  const criteriaFilledCount = criteriaList.filter((c) => {
    const s = scoresMap[c.id]?.score;
    return s !== '' && s !== undefined && !isNaN(s);
  }).length;

  const isComplete = criteriaList.length > 0 && criteriaFilledCount === criteriaList.length;

  // Submit Scorecard
  const handleSaveScorecard = async () => {
    if (readOnly || !currentSubmission) return;

    if (!isComplete) {
      toast.error(`Vui lòng nhập điểm đầy đủ cho toàn bộ ${criteriaList.length} tiêu chí.`);
      return;
    }

    const payload = {
      scores: criteriaList.map((c) => ({
        criteriaId: c.id,
        score: Number(scoresMap[c.id]?.score || 0),
        comment: scoresMap[c.id]?.comment?.trim() || null,
      })),
    };

    try {
      setSaving(true);
      const res = await evaluationApi.scoreSubmission(currentSubmission.submissionId, payload);
      if (res?.success) {
        toast.success(`Đã lưu phiếu chấm điểm cho ${currentSubmission.companyName}!`);
        if (onScoreSaved) {
          onScoreSaved();
        }
      } else {
        toast.error(res?.message || 'Có lỗi xảy ra khi lưu phiếu chấm.');
      }
    } catch (err) {
      const errMsg = err.message || 'Không thể lưu phiếu chấm điểm.';
      toast.error(errMsg);
    } finally {
      setSaving(false);
    }
  };

  const getStatusBadge = (status, totalScore) => {
    switch (status) {
      case 'Selected':
        return (
          <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-bold bg-amber-100 text-amber-800 border border-amber-300">
            <Award className="w-3 h-3 mr-1 text-amber-600" /> Trúng thầu
          </span>
        );
      case 'Evaluated':
        return (
          <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-semibold bg-emerald-100 text-emerald-800 border border-emerald-300">
            <CheckCircle2 className="w-3 h-3 mr-1 text-emerald-600" /> Đã chấm ({formatNumber(totalScore)})
          </span>
        );
      case 'Rejected':
        return (
          <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-semibold bg-rose-100 text-rose-800 border border-rose-300">
            Không trúng thầu
          </span>
        );
      default:
        return (
          <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-semibold bg-slate-100 text-slate-700 border border-slate-300">
            <Clock className="w-3 h-3 mr-1 text-slate-500" /> Chờ chấm điểm
          </span>
        );
    }
  };

  if (!submissions || submissions.length === 0) {
    return (
      <div className="bg-white rounded-2xl border border-slate-200 p-12 text-center space-y-3">
        <Building2 className="w-12 h-12 text-slate-300 mx-auto" />
        <h4 className="text-base font-bold text-slate-800">Chưa có hồ sơ dự thầu</h4>
        <p className="text-xs text-slate-500 max-w-md mx-auto">
          Gói thầu này hiện chưa có nhà thầu nào nộp hồ sơ hoặc chưa có hồ sơ hợp lệ để tiến hành chấm điểm.
        </p>
      </div>
    );
  }

  return (
    <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
      {/* CỘT TRÁI: DANH SÁCH NHÀ THẦU */}
      <div className="lg:col-span-4 space-y-3">
        <div className="flex items-center justify-between px-1">
          <h3 className="text-xs font-bold uppercase tracking-wider text-slate-500 flex items-center gap-1.5">
            <Building2 className="w-4 h-4 text-slate-400" />
            Nhà thầu tham gia ({submissions.length})
          </h3>
          <span className="text-[11px] text-slate-400">Chọn để chấm điểm</span>
        </div>

        <div className="space-y-2">
          {submissions.map((sub, index) => {
            const isSelected = sub.submissionId === selectedSubmissionId;
            const hasScore = sub.totalScore !== null && sub.totalScore !== undefined;

            return (
              <button
                key={sub.submissionId}
                onClick={() => setSelectedSubmissionId(sub.submissionId)}
                type="button"
                className={`w-full text-left p-4 rounded-xl border transition-all cursor-pointer relative ${
                  isSelected
                    ? 'bg-blue-50/60 border-blue-500 shadow-xs ring-1 ring-blue-500/20'
                    : 'bg-white border-slate-200 hover:border-slate-300 hover:bg-slate-50/50'
                }`}
              >
                <div className="flex items-start justify-between gap-2">
                  <div className="space-y-1 flex-1 min-w-0">
                    <div className="flex items-center gap-1.5">
                      <span className="w-5 h-5 rounded-full bg-slate-100 text-slate-600 text-[11px] font-bold flex items-center justify-center flex-shrink-0">
                        {sub.rank ? `#${sub.rank}` : index + 1}
                      </span>
                      <h4 className="text-xs font-bold text-slate-800 truncate" title={sub.companyName}>
                        {sub.companyName}
                      </h4>
                    </div>
                    <div className="text-[11px] text-slate-500 flex items-center gap-2 pl-6">
                      <span>MST: {sub.taxCode || 'N/A'}</span>
                      <span>•</span>
                      <span>Nộp: {formatDate(sub.submittedAt)}</span>
                    </div>
                  </div>
                  <ChevronRight
                    className={`w-4 h-4 transition-transform flex-shrink-0 mt-1 ${
                      isSelected ? 'text-blue-600 translate-x-0.5' : 'text-slate-300'
                    }`}
                  />
                </div>

                <div className="mt-3 pt-2.5 border-t border-slate-100 flex items-center justify-between text-xs pl-6">
                  <div>{getStatusBadge(sub.status, sub.totalScore)}</div>
                  {hasScore && (
                    <div className="text-right">
                      <span className="text-[10px] text-slate-400 block uppercase font-medium">Điểm tổng</span>
                      <span className="text-sm font-extrabold text-blue-700">
                        {formatNumber(sub.totalScore)}
                        <span className="text-[10px] font-normal text-slate-400">/100</span>
                      </span>
                    </div>
                  )}
                </div>
              </button>
            );
          })}
        </div>
      </div>

      {/* CỘT PHẢI: MA TRẬN PHIẾU CHẤM ĐIỂM */}
      <div className="lg:col-span-8 space-y-4">
        {currentSubmission ? (
          <div className="bg-white rounded-2xl border border-slate-200 shadow-xs overflow-hidden">
            {/* Header thông tin nhà thầu được chọn */}
            <div className="p-5 bg-gradient-to-r from-slate-50 to-blue-50/40 border-b border-slate-200 flex flex-wrap items-center justify-between gap-4">
              <div>
                <div className="flex items-center gap-2">
                  <span className="text-[11px] font-bold uppercase tracking-wider text-blue-600 bg-blue-100 px-2 py-0.5 rounded-md">
                    Phiếu chấm thẩm định
                  </span>
                  <span className="text-xs text-slate-400">Mã hồ sơ: #{currentSubmission.submissionId}</span>
                </div>
                <h3 className="text-base font-bold text-slate-800 mt-1">
                  {currentSubmission.companyName}
                </h3>
                <p className="text-xs text-slate-500">Mã số thuế: {currentSubmission.taxCode || 'Chưa cung cấp'}</p>
              </div>

              {/* Nút xem tài liệu hồ sơ */}
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={() => setShowFilesModal(true)}
                  className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-slate-700 bg-white border border-slate-300 hover:bg-slate-50 rounded-xl shadow-2xs transition-colors cursor-pointer"
                >
                  <FileText className="w-3.5 h-3.5 text-blue-600" />
                  <span>Xem tài liệu nộp ({submissionFiles.length})</span>
                </button>
              </div>
            </div>

            {/* Bảng nhập điểm tiêu chí */}
            <div className="p-5 space-y-4">
              {readOnly ? (
                <div className="bg-amber-50/90 border border-amber-200 rounded-xl p-3.5 text-xs text-amber-900 flex items-start gap-2.5">
                  <Lock className="w-4 h-4 text-amber-600 flex-shrink-0 mt-0.5" />
                  <div className="leading-relaxed">
                    <strong>Bảng điểm đã được khóa:</strong> Gói thầu đã hoàn tất trao thầu hoặc tài khoản của bạn ở chế độ chỉ đọc. Toàn bộ điểm số và nhận xét chuyên môn được lưu trữ cố định để bảo toàn tính toàn vẹn hồ sơ.
                  </div>
                </div>
              ) : (
                <div className="bg-blue-50/50 border border-blue-200/60 rounded-xl p-3.5 text-xs text-blue-800 flex items-start gap-2.5">
                  <Info className="w-4 h-4 text-blue-600 flex-shrink-0 mt-0.5" />
                  <div className="leading-relaxed">
                    <strong>Quy tắc chấm điểm chuẩn:</strong> Thang điểm từ 0 đến Điểm tối đa (MaxScore). Điểm quy đổi trọng số được tính tự động:{' '}
                    <code>Điểm quy đổi = Điểm số × Trọng số / 100</code>.
                  </div>
                </div>
              )}

              {/* Bảng danh sách tiêu chí */}
              <div className="border border-slate-200 rounded-xl overflow-hidden">
                <table className="w-full text-left text-xs border-collapse">
                  <thead>
                    <tr className="bg-slate-50 border-b border-slate-200 text-slate-600 font-bold uppercase tracking-wider text-[11px]">
                      <th className="py-3 px-4 w-12 text-center">STT</th>
                      <th className="py-3 px-4">Tiêu chí đánh giá</th>
                      <th className="py-3 px-4 text-center w-28">Trọng số (%)</th>
                      <th className="py-3 px-4 text-center w-36">Điểm chấm (0-Max)</th>
                      <th className="py-3 px-4 text-right w-28">Điểm quy đổi</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {criteriaList.map((crit, idx) => {
                      const item = scoresMap[crit.id] || { score: '', comment: '' };
                      const scoreVal = item.score !== '' && !isNaN(item.score) ? Number(item.score) : 0;
                      const weightedScore = (scoreVal * crit.weight) / 100;

                      return (
                        <React.Fragment key={crit.id}>
                          <tr className="hover:bg-slate-50/60 transition-colors">
                            <td className="py-3.5 px-4 text-center font-bold text-slate-400">
                              {idx + 1}
                            </td>
                            <td className="py-3.5 px-4">
                              <p className="font-bold text-slate-800 text-xs">{crit.name}</p>
                              <span className="text-[11px] text-slate-400">
                                Thang điểm tối đa: {formatNumber(crit.maxScore)} điểm
                              </span>
                            </td>
                            <td className="py-3.5 px-4 text-center">
                              <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-bold bg-slate-100 text-slate-700">
                                {crit.weight}%
                              </span>
                            </td>
                            <td className="py-3.5 px-4 text-center">
                              <div className="relative inline-block w-28">
                                <input
                                  type="number"
                                  min="0"
                                  max={crit.maxScore}
                                  step="0.5"
                                  placeholder="0.0"
                                  disabled={readOnly}
                                  value={item.score}
                                  onChange={(e) => handleScoreChange(crit.id, e.target.value, crit.maxScore)}
                                  className="w-full px-3 py-1.5 text-center text-xs font-bold text-slate-800 bg-white border border-slate-300 rounded-lg focus:outline-hidden focus:ring-2 focus:ring-blue-500 focus:border-blue-500 disabled:bg-slate-100 disabled:text-slate-500"
                                />
                              </div>
                            </td>
                            <td className="py-3.5 px-4 text-right font-bold text-blue-700">
                              {item.score !== '' ? formatNumber(weightedScore) : '—'}
                            </td>
                          </tr>
                          {/* Dòng nhận xét cho tiêu chí */}
                          <tr className="bg-slate-50/30">
                            <td colSpan={5} className="px-4 pb-3 pt-0">
                              <div className="pl-8">
                                <input
                                  type="text"
                                  placeholder="Ghi chú / Nhận xét chuyên môn cho tiêu chí này (tùy chọn)..."
                                  disabled={readOnly}
                                  value={item.comment}
                                  onChange={(e) => handleCommentChange(crit.id, e.target.value)}
                                  className="w-full px-3 py-1 text-[11px] text-slate-600 bg-white/70 border border-slate-200 rounded-md focus:bg-white focus:outline-hidden focus:ring-1 focus:ring-blue-400 disabled:bg-transparent disabled:border-transparent"
                                />
                              </div>
                            </td>
                          </tr>
                        </React.Fragment>
                      );
                    })}
                  </tbody>
                </table>
              </div>

              {/* Khối Tổng kết điểm dự kiến */}
              <div className="bg-slate-50 rounded-xl p-4 border border-slate-200 flex flex-wrap items-center justify-between gap-4">
                <div className="space-y-1">
                  <div className="flex items-center gap-2">
                    <Calculator className="w-4 h-4 text-slate-500" />
                    <span className="text-xs font-semibold text-slate-700">Tổng điểm quy đổi dự kiến:</span>
                    <span className="text-xs text-slate-400">
                      ({criteriaFilledCount}/{criteriaList.length} tiêu chí đã nhập)
                    </span>
                  </div>
                  <p className="text-[11px] text-slate-500">
                    Điểm tổng sẽ được hệ thống tính toán chính xác và xếp hạng tự động sau khi lưu phiếu.
                  </p>
                </div>

                <div className="text-right">
                  <span className="text-2xl font-black text-blue-700">
                    {formatNumber(calculateTotalWeightedScore())}
                  </span>
                  <span className="text-xs font-medium text-slate-400"> / 100 điểm</span>
                </div>
              </div>

              {/* Nút Hành động */}
              {!readOnly && (
                <div className="flex items-center justify-end gap-3 pt-2">
                  <button
                    type="button"
                    onClick={handleSaveScorecard}
                    disabled={saving || !isComplete}
                    className="inline-flex items-center gap-2 px-5 py-2.5 text-xs font-bold text-white bg-blue-600 hover:bg-blue-700 rounded-xl shadow-xs transition-colors cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    {saving ? (
                      <>
                        <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                        <span>Đang lưu phiếu chấm...</span>
                      </>
                    ) : (
                      <>
                        <Save className="w-4 h-4" />
                        <span>Lưu phiếu chấm điểm</span>
                      </>
                    )}
                  </button>
                </div>
              )}
            </div>
          </div>
        ) : null}
      </div>

      {/* Modal xem danh sách tệp đính kèm */}
      {showFilesModal && (
        <div className="fixed inset-0 z-50 bg-slate-900/50 backdrop-blur-2xs flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl shadow-xl border border-slate-200 w-full max-w-lg overflow-hidden animate-in fade-in zoom-in duration-150">
            <div className="p-4 bg-slate-50 border-b border-slate-200 flex items-center justify-between">
              <h4 className="text-sm font-bold text-slate-800 flex items-center gap-2">
                <FileText className="w-4 h-4 text-blue-600" />
                Tài liệu dự thầu - {currentSubmission?.companyName}
              </h4>
              <button
                type="button"
                onClick={() => setShowFilesModal(false)}
                className="text-slate-400 hover:text-slate-600 p-1 rounded-lg"
              >
                ✕
              </button>
            </div>
            <div className="p-4 max-h-96 overflow-y-auto space-y-2">
              {loadingFiles ? (
                <div className="text-center py-6 text-xs text-slate-400">Đang tải danh sách tài liệu...</div>
              ) : submissionFiles.length === 0 ? (
                <div className="text-center py-6 text-xs text-slate-400">Không tìm thấy tài liệu đính kèm.</div>
              ) : (
                submissionFiles.map((file, i) => (
                  <div
                    key={file.id || i}
                    className="flex items-center justify-between p-3 bg-slate-50 rounded-xl border border-slate-200 text-xs"
                  >
                    <div className="min-w-0 flex-1 mr-2">
                      <p className="font-semibold text-slate-700 truncate">{file.fileName || file.fileType}</p>
                      <span className="text-[10px] text-slate-400">{file.fileType || 'Hồ sơ kỹ thuật'}</span>
                    </div>
                    {file.fileUrl && (
                      <a
                        href={file.fileUrl}
                        target="_blank"
                        rel="noreferrer"
                        className="inline-flex items-center gap-1 text-[11px] font-bold text-blue-600 hover:text-blue-800 bg-blue-50 px-2.5 py-1 rounded-lg hover:bg-blue-100 transition-colors"
                      >
                        <FileDown className="w-3.5 h-3.5" />
                        Tải về
                      </a>
                    )}
                  </div>
                ))
              )}
            </div>
            <div className="p-3 bg-slate-50 border-t border-slate-200 text-right">
              <button
                type="button"
                onClick={() => setShowFilesModal(false)}
                className="px-4 py-1.5 text-xs font-semibold text-slate-600 hover:bg-slate-200 rounded-lg"
              >
                Đóng
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default ScorecardForm;
