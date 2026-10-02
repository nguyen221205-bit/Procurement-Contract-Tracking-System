import React, { useState, useEffect } from 'react';
import { X, UserCheck, Search, AlertCircle, Shield, Check } from 'lucide-react';
import toast from 'react-hot-toast';
import { bidPackageApi } from '../../api/bidPackageApi';
import { userApi } from '../../api/userApi';
import LoadingSpinner from '../common/LoadingSpinner';

export const AssignEvaluatorModal = ({
  isOpen,
  onClose,
  packageId,
  existingEvaluators = [],
  onSuccess,
}) => {
  const [candidates, setCandidates] = useState([]);
  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedEvaluatorId, setSelectedEvaluatorId] = useState(null);

  useEffect(() => {
    if (isOpen) {
      loadCandidates();
      setSelectedEvaluatorId(null);
      setSearchTerm('');
    }
  }, [isOpen, packageId]);

  const loadCandidates = async () => {
    try {
      setLoading(true);
      // Lấy danh sách người dùng có vai trò Evaluator
      const res = await userApi.getUsers({ role: 'Evaluator', pageSize: 100 });
      const usersList = res?.data?.items || res?.data || [];
      setCandidates(usersList);
    } catch (err) {
      toast.error('Không thể tải danh sách giám khảo: ' + (err.message || 'Lỗi hệ thống'));
    } finally {
      setLoading(false);
    }
  };

  if (!isOpen) return null;

  const existingIds = new Set(existingEvaluators.map((e) => e.evaluatorId));

  const filteredCandidates = candidates.filter((c) => {
    const term = searchTerm.toLowerCase();
    return (
      c.fullName?.toLowerCase().includes(term) ||
      c.email?.toLowerCase().includes(term) ||
      c.phone?.includes(term)
    );
  });

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!selectedEvaluatorId) {
      toast.error('Vui lòng chọn một giám khảo để thêm vào Tổ chuyên gia.');
      return;
    }

    try {
      setSubmitting(true);
      await bidPackageApi.assignEvaluator(packageId, {
        evaluatorId: selectedEvaluatorId,
      });
      toast.success('Phân công giám khảo vào Tổ chuyên gia thành công!');
      onSuccess?.();
      onClose();
    } catch (err) {
      toast.error(err.message || 'Không thể phân công giám khảo.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-xs">
      <div className="bg-white w-full max-w-lg rounded-2xl shadow-2xl border border-slate-200 overflow-hidden flex flex-col max-h-[90vh] animate-in fade-in zoom-in-95 duration-150">
        {/* Header */}
        <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between bg-slate-50/50">
          <div className="flex items-center space-x-3">
            <div className="p-2 bg-sky-50 text-sky-600 rounded-xl">
              <UserCheck className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-slate-900">
                Phân công Giám khảo chấm thầu
              </h3>
              <p className="text-xs text-slate-500">
                Chọn giám khảo tham gia chấm điểm gói thầu
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-1.5 text-slate-400 hover:text-slate-600 rounded-lg hover:bg-slate-100 transition"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Search bar */}
        <div className="p-4 border-b border-slate-100 bg-white">
          <div className="relative">
            <Search className="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              placeholder="Tìm kiếm theo tên, email, số điện thoại..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="w-full pl-10 pr-4 py-2 text-xs bg-slate-50 border border-slate-200 rounded-xl focus:outline-hidden focus:ring-2 focus:ring-sky-500/20 focus:border-sky-500 transition"
            />
          </div>
        </div>

        {/* Content / Evaluators list */}
        <div className="p-4 overflow-y-auto flex-1 space-y-2">
          {loading ? (
            <div className="py-12 flex justify-center">
              <LoadingSpinner />
            </div>
          ) : filteredCandidates.length === 0 ? (
            <div className="py-8 text-center text-slate-400 text-xs">
              Không tìm thấy tài khoản giám khảo phù hợp.
            </div>
          ) : (
            filteredCandidates.map((evaluator) => {
              const isAssigned = existingIds.has(evaluator.id);
              const isSelected = selectedEvaluatorId === evaluator.id;

              return (
                <div
                  key={evaluator.id}
                  onClick={() => {
                    if (!isAssigned) {
                      setSelectedEvaluatorId(evaluator.id);
                    }
                  }}
                  className={`p-3.5 rounded-xl border text-left transition flex items-center justify-between ${
                    isAssigned
                      ? 'bg-slate-50 border-slate-200 opacity-60 cursor-not-allowed'
                      : isSelected
                      ? 'bg-sky-50/70 border-sky-500 ring-2 ring-sky-500/20 cursor-pointer'
                      : 'bg-white border-slate-200 hover:border-slate-300 hover:bg-slate-50/50 cursor-pointer'
                  }`}
                >
                  <div className="flex items-center space-x-3 truncate pr-2">
                    <div
                      className={`w-9 h-9 rounded-xl flex items-center justify-center font-bold text-xs flex-shrink-0 ${
                        isSelected
                          ? 'bg-sky-600 text-white'
                          : isAssigned
                          ? 'bg-slate-200 text-slate-500'
                          : 'bg-slate-100 text-slate-700'
                      }`}
                    >
                      {evaluator.fullName ? evaluator.fullName.charAt(0).toUpperCase() : 'G'}
                    </div>
                    <div className="truncate">
                      <div className="text-xs font-bold text-slate-900 flex items-center space-x-2">
                        <span className="truncate">{evaluator.fullName}</span>
                        {isAssigned && (
                          <span className="text-[10px] font-semibold text-slate-500 bg-slate-200 px-2 py-0.5 rounded-full">
                            Đã trong tổ
                          </span>
                        )}
                      </div>
                      <div className="text-[11px] text-slate-500 truncate mt-0.5">
                        {evaluator.email} {evaluator.phone && `• ${evaluator.phone}`}
                      </div>
                    </div>
                  </div>

                  <div className="flex-shrink-0">
                    {isAssigned ? (
                      <Check className="w-4 h-4 text-slate-400" />
                    ) : (
                      <div
                        className={`w-5 h-5 rounded-full border flex items-center justify-center transition ${
                          isSelected
                            ? 'border-sky-600 bg-sky-600 text-white'
                            : 'border-slate-300 bg-white'
                        }`}
                      >
                        {isSelected && <Check className="w-3 h-3 stroke-[3]" />}
                      </div>
                    )}
                  </div>
                </div>
              );
            })
          )}
        </div>

        {/* Footer info & Actions */}
        <div className="p-4 border-t border-slate-100 bg-slate-50/50 flex flex-col space-y-3">
          <div className="flex items-center space-x-2 text-[11px] text-amber-700 bg-amber-50 p-2.5 rounded-xl border border-amber-200/60">
            <AlertCircle className="w-4 h-4 flex-shrink-0 text-amber-600" />
            <span>
              Yêu cầu: Tổ chuyên gia cần tối thiểu <strong>3 thành viên</strong> và là <strong>số lẻ (3, 5, 7...)</strong> để mở chấm thầu.
            </span>
          </div>

          <div className="flex items-center justify-end space-x-2.5">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 text-xs font-semibold text-slate-600 hover:text-slate-800 hover:bg-slate-100 rounded-xl transition"
            >
              Hủy
            </button>
            <button
              type="button"
              disabled={!selectedEvaluatorId || submitting}
              onClick={handleSubmit}
              className="px-5 py-2 text-xs font-bold text-white bg-sky-600 hover:bg-sky-700 disabled:opacity-50 disabled:cursor-not-allowed rounded-xl shadow-xs transition flex items-center space-x-1.5"
            >
              {submitting ? (
                <>
                  <LoadingSpinner size="sm" />
                  <span>Đang xử lý...</span>
                </>
              ) : (
                <>
                  <UserCheck className="w-4 h-4" />
                  <span>Xác nhận phân công</span>
                </>
              )}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default AssignEvaluatorModal;
