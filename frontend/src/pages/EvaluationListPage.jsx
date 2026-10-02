import React, { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import {
  Scale,
  Search,
  Filter,
  ArrowRight,
  Building2,
  Calendar,
  Layers,
  AlertCircle,
  Clock,
  Award,
  CheckCircle2,
  ShieldCheck,
  UserCheck,
  RefreshCw
} from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import { ROLES } from '../utils/constants';
import { bidPackageApi } from '../api/bidPackageApi';
import StatusBadge from '../components/common/StatusBadge';
import LoadingSpinner from '../components/common/LoadingSpinner';
import { formatCurrency, formatDate } from '../utils/formatters';

const FILTER_TABS = [
  { id: 'ALL', label: 'Tất cả trạng thái' },
  { id: 'Evaluating', label: 'Đang chấm điểm' },
  { id: 'Closed', label: 'Chờ mở chấm' },
  { id: 'Awarded', label: 'Đã trao thầu' },
];

export const EvaluationListPage = () => {
  const { user, hasRole } = useAuth();
  const isEvaluator = hasRole([ROLES.EVALUATOR]);

  const [packages, setPackages] = useState([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedStatus, setSelectedStatus] = useState('ALL');
  const [scopeFilter, setScopeFilter] = useState('ALL'); // 'ALL' or 'ASSIGNED'

  const getPackageStatus = (pkg) => {
    if (pkg.status === 'Contracted' || pkg.status === 3) return 'Contracted';
    if (pkg.isAwarded || pkg.status === 'Awarded') return 'Awarded';
    return pkg.status;
  };

  const fetchPackages = useCallback(async (isBackground = false) => {
    try {
      if (!isBackground) setLoading(true);
      else setRefreshing(true);

      const res = await bidPackageApi.getPackages({ pageSize: 100 });
      if (res?.data) {
        const rawItems = res.data.items || res.data || [];
        // Phân hệ thẩm định hiển thị các gói thầu: Đã đóng nhận thầu, Đang chấm điểm, Đã trao thầu, Đã ký hợp đồng
        const items = rawItems.filter(
          (p) => p.status !== 'Open' && p.status !== 0
        );
        setPackages(items);
      }
    } catch (err) {
      console.error('Failed to load evaluation packages:', err);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useEffect(() => {
    fetchPackages();
  }, [fetchPackages]);

  const filteredPackages = packages.filter((pkg) => {
    const matchesSearch =
      pkg.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      pkg.code.toLowerCase().includes(searchTerm.toLowerCase());

    const displayStatus = getPackageStatus(pkg);
    const matchesStatus =
      selectedStatus === 'ALL'
        ? true
        : selectedStatus === 'Awarded'
        ? displayStatus === 'Awarded' || displayStatus === 'Contracted'
        : displayStatus === selectedStatus;

    const matchesScope =
      scopeFilter === 'ASSIGNED'
        ? pkg.evaluatorIds && pkg.evaluatorIds.includes(user?.id)
        : true;

    return matchesSearch && matchesStatus && matchesScope;
  });

  // Count by status
  const evaluatingCount = packages.filter((p) => getPackageStatus(p) === 'Evaluating').length;
  const closedCount = packages.filter((p) => getPackageStatus(p) === 'Closed').length;
  const awardedCount = packages.filter((p) => ['Awarded', 'Contracted'].includes(getPackageStatus(p))).length;
  const assignedCount = packages.filter((p) => p.evaluatorIds && p.evaluatorIds.includes(user?.id)).length;

  return (
    <div className="space-y-6 pb-12">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-black text-slate-900 tracking-tight flex items-center gap-2">
            <Scale className="w-6 h-6 text-blue-600" />
            <span>Trung Tâm Thẩm Định & Chấm Điểm Thầu</span>
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Quản lý và thực hiện chấm điểm hồ sơ dự thầu, xem bảng xếp hạng tự động và phê duyệt trao thầu
          </p>
        </div>

        <button
          type="button"
          onClick={() => fetchPackages(true)}
          disabled={refreshing}
          className="p-2.5 bg-white border border-slate-200 hover:border-slate-300 rounded-xl text-slate-600 hover:text-slate-900 shadow-2xs transition-colors cursor-pointer self-start sm:self-auto"
          title="Làm mới dữ liệu"
        >
          <RefreshCw className={`w-4 h-4 ${refreshing ? 'animate-spin text-blue-600' : ''}`} />
        </button>
      </div>

      {/* KPI Stats (Clickable Filter Cards) */}
      <div className={`grid grid-cols-1 sm:grid-cols-2 ${isEvaluator ? 'lg:grid-cols-4' : 'lg:grid-cols-3'} gap-4`}>
        {isEvaluator && (
          <div
            onClick={() => {
              setScopeFilter(scopeFilter === 'ASSIGNED' ? 'ALL' : 'ASSIGNED');
            }}
            className={`rounded-2xl p-4 border transition-all cursor-pointer flex items-center gap-3.5 ${
              scopeFilter === 'ASSIGNED'
                ? 'bg-purple-50/80 border-purple-300 ring-2 ring-purple-400/30 shadow-xs'
                : 'bg-white border-slate-200 hover:border-purple-200 hover:shadow-2xs'
            }`}
            title="Lọc các gói thầu bạn được phân công vào Tổ chuyên gia"
          >
            <div className="w-11 h-11 rounded-xl bg-purple-50 text-purple-600 flex items-center justify-center flex-shrink-0">
              <ShieldCheck className="w-5 h-5" />
            </div>
            <div>
              <span className="text-[11px] font-medium text-slate-500 block">
                Phân công cho tôi
              </span>
              <div className="flex items-baseline gap-1.5 mt-0.5">
                <span className="text-xl font-black text-purple-700">{assignedCount}</span>
                <span className="text-xs text-slate-500">gói thầu</span>
              </div>
            </div>
          </div>
        )}

        <div
          onClick={() => {
            setSelectedStatus(selectedStatus === 'Evaluating' ? 'ALL' : 'Evaluating');
          }}
          className={`rounded-2xl p-4 border transition-all cursor-pointer flex items-center gap-3.5 ${
            selectedStatus === 'Evaluating'
              ? 'bg-blue-50/80 border-blue-300 ring-2 ring-blue-400/30 shadow-xs'
              : 'bg-white border-slate-200 hover:border-blue-200 hover:shadow-2xs'
          }`}
          title="Lọc gói thầu đang chấm điểm"
        >
          <div className="w-11 h-11 rounded-xl bg-blue-50 text-blue-600 flex items-center justify-center flex-shrink-0">
            <Clock className="w-5 h-5" />
          </div>
          <div>
            <span className="text-[11px] font-medium text-slate-500 block">
              Đang chấm điểm
            </span>
            <div className="flex items-baseline gap-1.5 mt-0.5">
              <span className="text-xl font-black text-blue-700">{evaluatingCount}</span>
              <span className="text-xs text-slate-500">gói thầu</span>
            </div>
          </div>
        </div>

        <div
          onClick={() => {
            setSelectedStatus(selectedStatus === 'Closed' ? 'ALL' : 'Closed');
          }}
          className={`rounded-2xl p-4 border transition-all cursor-pointer flex items-center gap-3.5 ${
            selectedStatus === 'Closed'
              ? 'bg-amber-50/80 border-amber-300 ring-2 ring-amber-400/30 shadow-xs'
              : 'bg-white border-slate-200 hover:border-amber-200 hover:shadow-2xs'
          }`}
          title="Lọc gói thầu chờ mở chấm"
        >
          <div className="w-11 h-11 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center flex-shrink-0">
            <Layers className="w-5 h-5" />
          </div>
          <div>
            <span className="text-[11px] font-medium text-slate-500 block">
              Chờ mở chấm
            </span>
            <div className="flex items-baseline gap-1.5 mt-0.5">
              <span className="text-xl font-black text-amber-700">{closedCount}</span>
              <span className="text-xs text-slate-500">gói thầu</span>
            </div>
          </div>
        </div>

        <div
          onClick={() => {
            setSelectedStatus(selectedStatus === 'Awarded' ? 'ALL' : 'Awarded');
          }}
          className={`rounded-2xl p-4 border transition-all cursor-pointer flex items-center gap-3.5 ${
            selectedStatus === 'Awarded'
              ? 'bg-emerald-50/80 border-emerald-300 ring-2 ring-emerald-400/30 shadow-xs'
              : 'bg-white border-slate-200 hover:border-emerald-200 hover:shadow-2xs'
          }`}
          title="Lọc gói thầu đã trao thầu"
        >
          <div className="w-11 h-11 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center flex-shrink-0">
            <Award className="w-5 h-5" />
          </div>
          <div>
            <span className="text-[11px] font-medium text-slate-500 block">
              Đã trao thầu
            </span>
            <div className="flex items-baseline gap-1.5 mt-0.5">
              <span className="text-xl font-black text-emerald-700">{awardedCount}</span>
              <span className="text-xs text-slate-500">gói thầu</span>
            </div>
          </div>
        </div>
      </div>

      {/* Filter & Search Bar */}
      <div className="bg-white rounded-2xl p-4 border border-slate-200 shadow-2xs space-y-3">
        <div className="flex flex-col md:flex-row items-center justify-between gap-3">
          {/* Status Tabs */}
          <div className="flex items-center space-x-1.5 overflow-x-auto w-full md:w-auto pb-1 md:pb-0">
            {isEvaluator && (
              <button
                type="button"
                onClick={() => setScopeFilter(scopeFilter === 'ASSIGNED' ? 'ALL' : 'ASSIGNED')}
                className={`px-3 py-1.5 text-xs font-bold rounded-xl transition-all cursor-pointer whitespace-nowrap flex items-center gap-1.5 ${
                  scopeFilter === 'ASSIGNED'
                    ? 'bg-purple-600 text-white shadow-2xs'
                    : 'text-purple-700 bg-purple-50 hover:bg-purple-100'
                }`}
              >
                <ShieldCheck className="w-3.5 h-3.5" />
                <span>Phân công cho tôi ({assignedCount})</span>
              </button>
            )}

            {FILTER_TABS.map((tab) => (
              <button
                key={tab.id}
                type="button"
                onClick={() => setSelectedStatus(tab.id)}
                className={`px-3 py-1.5 text-xs font-bold rounded-xl transition-all cursor-pointer whitespace-nowrap ${
                  selectedStatus === tab.id
                    ? 'bg-blue-600 text-white shadow-2xs'
                    : 'text-slate-600 hover:bg-slate-100'
                }`}
              >
                {tab.label}
              </button>
            ))}
          </div>

          {/* Search Input */}
          <div className="relative w-full md:w-72">
            <Search className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              placeholder="Tìm theo mã hoặc tên gói thầu..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="w-full pl-9 pr-3 py-2 text-xs bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-hidden focus:ring-2 focus:ring-blue-500"
            />
          </div>
        </div>
      </div>

      {/* Package List */}
      {loading ? (
        <div className="py-20 flex justify-center">
          <LoadingSpinner size="lg" message="Đang tải danh sách gói thầu thẩm định..." />
        </div>
      ) : filteredPackages.length === 0 ? (
        <div className="bg-white rounded-2xl border border-slate-200 p-12 text-center space-y-3">
          <Scale className="w-12 h-12 text-slate-300 mx-auto" />
          <h4 className="text-base font-bold text-slate-800">Không có gói thầu phù hợp</h4>
          <p className="text-xs text-slate-500 max-w-md mx-auto">
            Không tìm thấy gói thầu nào trong giai đoạn thẩm định phù hợp với bộ lọc tìm kiếm hiện tại.
          </p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
          {filteredPackages.map((pkg) => {
            const displayStatus = getPackageStatus(pkg);
            const isFinished = displayStatus === 'Awarded' || displayStatus === 'Contracted';
            const isAssigned = pkg.evaluatorIds && pkg.evaluatorIds.includes(user?.id);

            return (
              <div
                key={pkg.id}
                className={`bg-white rounded-2xl border transition-all p-5 flex flex-col justify-between space-y-4 ${
                  isAssigned
                    ? 'border-purple-200/90 shadow-2xs hover:border-purple-300 hover:shadow-xs'
                    : 'border-slate-200 hover:border-slate-300 hover:shadow-sm'
                }`}
              >
                <div className="space-y-3">
                  <div className="flex items-start justify-between gap-2 flex-wrap">
                    <div className="flex items-center gap-1.5 flex-wrap">
                      <span className="text-[11px] font-mono font-bold text-slate-500 bg-slate-100 px-2 py-0.5 rounded-md">
                        {pkg.code}
                      </span>
                      {isAssigned && (
                        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-[10px] font-bold bg-purple-50 text-purple-700 border border-purple-200">
                          <ShieldCheck className="w-3 h-3 text-purple-600" />
                          <span>Tổ chuyên gia</span>
                        </span>
                      )}
                    </div>
                    <StatusBadge status={displayStatus} />
                  </div>

                  <h3
                    className="text-sm font-bold text-slate-800 line-clamp-2 hover:text-blue-600 transition-colors"
                    title={pkg.name}
                  >
                    <Link to={`/evaluation/${pkg.id}`}>{pkg.name}</Link>
                  </h3>

                  <div className="space-y-1.5 text-xs text-slate-600 pt-2 border-t border-slate-100">
                    <div className="flex justify-between">
                      <span className="text-slate-400">Ngân sách:</span>
                      <span className="font-bold text-emerald-700">{formatCurrency(pkg.budget)}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-slate-400">Hạn nộp thầu:</span>
                      <span className="font-medium text-slate-700">{formatDate(pkg.deadline || pkg.submissionDeadline)}</span>
                    </div>
                  </div>
                </div>

                <div className="pt-3 border-t border-slate-100 flex items-center justify-between">
                  <div className="text-[11px]">
                    {displayStatus === 'Evaluating' && <span className="text-amber-600 font-semibold">• Đang chấm điểm</span>}
                    {displayStatus === 'Awarded' && <span className="text-emerald-600 font-semibold">• Đã trao thầu</span>}
                    {displayStatus === 'Contracted' && <span className="text-blue-600 font-semibold">• Đã ký hợp đồng</span>}
                    {displayStatus === 'Closed' && <span className="text-slate-500 font-semibold">• Chờ chuyển chấm</span>}
                  </div>

                  <Link
                    to={`/evaluation/${pkg.id}`}
                    className={`inline-flex items-center gap-1.5 px-3.5 py-1.5 text-xs font-bold text-white rounded-xl shadow-2xs transition-colors cursor-pointer ${
                      isFinished
                        ? 'bg-slate-800 hover:bg-slate-900'
                        : isAssigned && displayStatus === 'Evaluating'
                        ? 'bg-indigo-600 hover:bg-indigo-700 ring-2 ring-indigo-400/20'
                        : 'bg-blue-600 hover:bg-blue-700'
                    }`}
                  >
                    <span>
                      {isFinished
                        ? 'Xem kết quả'
                        : isAssigned && displayStatus === 'Evaluating'
                        ? 'Vào chấm điểm'
                        : 'Mở bảng chấm'}
                    </span>
                    <ArrowRight className="w-3.5 h-3.5" />
                  </Link>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};

export default EvaluationListPage;
