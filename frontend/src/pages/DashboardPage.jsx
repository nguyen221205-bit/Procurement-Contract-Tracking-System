import React, { useEffect, useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { reportApi } from '../api/reportApi';
import StatCard from '../components/dashboard/StatCard';
import FinancialCharts from '../components/dashboard/FinancialCharts';
import LoadingSpinner from '../components/common/LoadingSpinner';
import { formatVND, formatDate } from '../utils/formatters';
import {
  Coins,
  TrendingUp,
  FileCheck,
  CreditCard,
  Building2,
  Calendar,
  Layers,
  RefreshCw,
  Eye,
  ArrowRight,
  Activity,
  CheckCircle2,
  Clock,
  ExternalLink,
  ShieldAlert
} from 'lucide-react';

export const DashboardPage = () => {
  const navigate = useNavigate();
  const [stats, setStats] = useState(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState('');
  const [lastUpdated, setLastUpdated] = useState(new Date());

  const fetchDashboardData = async (isManualRefresh = false) => {
    if (isManualRefresh) {
      setRefreshing(true);
    } else {
      setLoading(true);
    }
    setError('');

    try {
      const response = await reportApi.getDashboard();
      if (response && response.success && response.data) {
        setStats(response.data);
        setLastUpdated(new Date());
      } else {
        setError(response?.message || 'Không thể tải dữ liệu báo cáo');
      }
    } catch (err) {
      setError(err.message || 'Lỗi kết nối tới máy chủ');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => {
    fetchDashboardData();
  }, []);

  if (loading) {
    return (
      <div className="py-24 flex justify-center">
        <LoadingSpinner size="lg" text="Đang tải dữ liệu tổng quan tài chính..." />
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-8 text-center bg-white rounded-2xl border border-rose-200 shadow-xs max-w-lg mx-auto space-y-4">
        <div className="w-12 h-12 rounded-full bg-rose-50 text-rose-600 mx-auto flex items-center justify-center">
          <ShieldAlert className="w-6 h-6" />
        </div>
        <div>
          <h3 className="text-base font-bold text-slate-800">Không thể tải dữ liệu điều hành</h3>
          <p className="text-xs text-rose-600 mt-1">{error}</p>
        </div>
        <button
          onClick={() => fetchDashboardData(false)}
          className="px-4 py-2 bg-slate-900 text-white rounded-xl text-xs font-semibold hover:bg-slate-800 transition cursor-pointer"
        >
          Tải lại dữ liệu
        </button>
      </div>
    );
  }

  const statusBadge = (status) => {
    const s = status?.toLowerCase();
    if (s === 'active') return 'bg-emerald-50 text-emerald-700 border-emerald-200';
    if (s === 'draft') return 'bg-slate-100 text-slate-700 border-slate-200';
    if (s === 'completed') return 'bg-blue-50 text-blue-700 border-blue-200';
    if (s === 'terminated') return 'bg-rose-50 text-rose-700 border-rose-200';
    return 'bg-gray-100 text-gray-700 border-gray-200';
  };

  const statusLabel = (status) => {
    const map = {
      Active: 'Đang thực hiện',
      Draft: 'Bản thảo',
      Completed: 'Đã hoàn thành',
      Terminated: 'Đã chấm dứt',
    };
    return map[status] || status;
  };

  const disbursementRate = Number(stats?.disbursementRate || 0);

  return (
    <div className="space-y-6">
      {/* 1. Header Điều hành & Hành động */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 pb-1">
        <div>
          <h2 className="text-2xl font-black text-slate-900 tracking-tight flex items-center space-x-2">
            <span>Tổng quan Điều hành & Tài chính</span>
          </h2>
          <div className="flex items-center space-x-2 text-xs text-slate-500 mt-1">
            <span>Chỉ số tài chính, tiến độ thực hiện và hiệu quả đấu thầu</span>
            <span>•</span>
            <span className="inline-flex items-center space-x-1 text-slate-400 font-mono">
              <Clock className="w-3 h-3" />
              <span>Cập nhật: {lastUpdated.toLocaleTimeString('vi-VN')}</span>
            </span>
          </div>
        </div>

        <div className="flex items-center space-x-2 self-start sm:self-center">
          <button
            type="button"
            onClick={() => fetchDashboardData(true)}
            disabled={refreshing}
            className="px-3.5 py-2 bg-white hover:bg-slate-50 text-slate-700 border border-slate-200 rounded-xl text-xs font-semibold shadow-xs transition flex items-center space-x-1.5 cursor-pointer disabled:opacity-50"
            title="Làm mới dữ liệu từ máy chủ"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${refreshing ? 'animate-spin text-sky-600' : 'text-slate-500'}`} />
            <span>{refreshing ? 'Đang làm mới...' : 'Làm mới'}</span>
          </button>

          <Link
            to="/contracts"
            className="px-3.5 py-2 bg-slate-900 hover:bg-slate-800 text-white rounded-xl text-xs font-semibold shadow-xs transition flex items-center space-x-1.5"
          >
            <span>Quản lý Hợp đồng</span>
            <ArrowRight className="w-3.5 h-3.5" />
          </Link>
        </div>
      </div>

      {/* 2. Bộ 4 Thẻ KPI Tài chính chính */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCard
          title="Ngân sách dự toán"
          value={formatVND(stats?.totalEstimatedBudget)}
          subtext={`${stats?.totalPackages || 0} gói thầu toàn hệ thống`}
          icon={Coins}
          color="sky"
        />

        <StatCard
          title="Tiết kiệm đấu thầu"
          value={formatVND(stats?.totalSavings)}
          badgeText={`Tiết kiệm ${stats?.savingsRate || 0}%`}
          icon={TrendingUp}
          color="emerald"
        />

        <StatCard
          title="Giá trị hợp đồng"
          value={formatVND(stats?.totalContractValue)}
          subtext={`${stats?.totalContracts || 0} hợp đồng đã ký`}
          icon={FileCheck}
          color="indigo"
        />

        <StatCard
          title="Đã giải ngân thực tế"
          value={formatVND(stats?.totalDisbursedAmount)}
          badgeText={`Giải ngân ${stats?.disbursementRate || 0}%`}
          icon={CreditCard}
          color="amber"
        />
      </div>

      {/* 3. Bộ 3 Thẻ Mini-KPI Hệ sinh thái & Tiến độ */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="bg-white p-4 rounded-2xl border border-slate-200 shadow-2xs flex items-center space-x-3.5">
          <div className="w-10 h-10 rounded-xl bg-purple-50 text-purple-600 flex items-center justify-center shrink-0">
            <Building2 className="w-5 h-5" />
          </div>
          <div>
            <p className="text-xs font-medium text-slate-500">Doanh nghiệp tham gia</p>
            <h4 className="text-lg font-bold text-slate-900 mt-0.5">{stats?.totalContractors || 0} nhà thầu</h4>
          </div>
        </div>

        <div className="bg-white p-4 rounded-2xl border border-slate-200 shadow-2xs flex items-center space-x-3.5">
          <div className="w-10 h-10 rounded-xl bg-teal-50 text-teal-600 flex items-center justify-center shrink-0">
            <Layers className="w-5 h-5" />
          </div>
          <div>
            <p className="text-xs font-medium text-slate-500">Hồ sơ tiếp nhận</p>
            <h4 className="text-lg font-bold text-slate-900 mt-0.5">{stats?.totalSubmissions || 0} hồ sơ</h4>
          </div>
        </div>

        <div className="bg-white p-4 rounded-2xl border border-slate-200 shadow-2xs flex items-center space-x-3.5">
          <div className="w-10 h-10 rounded-xl bg-rose-50 text-rose-600 flex items-center justify-center shrink-0">
            <Activity className="w-5 h-5" />
          </div>
          <div>
            <p className="text-xs font-medium text-slate-500">Mốc nghiệm thu</p>
            <h4 className="text-lg font-bold text-slate-900 mt-0.5">{stats?.completedMilestones || 0} / {stats?.totalMilestones || 0} mốc</h4>
          </div>
        </div>
      </div>

      {/* 4. Biểu đồ Tài chính Chart.js */}
      <FinancialCharts stats={stats} />

      {/* 5. Khối Theo dõi Tiến độ Giải ngân Dòng tiền Hợp đồng */}
      <div className="bg-white rounded-2xl border border-slate-200 shadow-xs p-6 space-y-4">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2">
          <div>
            <h4 className="text-sm font-bold text-slate-900 flex items-center space-x-2">
              <CreditCard className="w-4 h-4 text-sky-600" />
              <span>Tiến độ Giải ngân Dòng tiền Hợp đồng</span>
            </h4>
            <p className="text-xs text-slate-500 mt-0.5">
              Tỷ lệ giải ngân lũy kế theo các mốc công việc đã được nghiệm thu
            </p>
          </div>
          <div className="flex items-center space-x-2">
            <span className="text-xs font-semibold text-slate-600">Đã giải ngân:</span>
            <span className="px-2.5 py-1 rounded-lg text-xs font-bold bg-emerald-50 text-emerald-700 border border-emerald-200">
              {disbursementRate}% hoàn thành
            </span>
          </div>
        </div>

        {/* Thanh tiến độ trực quan */}
        <div className="space-y-1.5">
          <div className="w-full h-3.5 bg-slate-100 rounded-full overflow-hidden p-0.5">
            <div
              className="h-full bg-gradient-to-r from-emerald-500 to-teal-500 rounded-full transition-all duration-500"
              style={{ width: `${Math.min(100, Math.max(0, disbursementRate))}%` }}
            />
          </div>
          <div className="flex justify-between text-[11px] text-slate-400 font-mono">
            <span>0% (Khởi đầu)</span>
            <span>50%</span>
            <span>100% (Hoàn tất)</span>
          </div>
        </div>

        {/* 3 Hộp số liệu tóm tắt */}
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 pt-2">
          <div className="p-3.5 rounded-xl bg-slate-50 border border-slate-100">
            <span className="text-[11px] font-medium text-slate-500">Đã giải ngân thực tế</span>
            <p className="text-sm font-extrabold text-emerald-700 mt-0.5">
              {formatVND(stats?.totalDisbursedAmount)}
            </p>
            <span className="text-[10px] text-slate-400">
              {stats?.completedMilestones || 0} mốc đã nghiệm thu
            </span>
          </div>

          <div className="p-3.5 rounded-xl bg-slate-50 border border-slate-100">
            <span className="text-[11px] font-medium text-slate-500">Còn lại cần giải ngân</span>
            <p className="text-sm font-extrabold text-slate-800 mt-0.5">
              {formatVND(stats?.totalRemainingAmount)}
            </p>
            <span className="text-[10px] text-slate-400">
              {stats?.pendingMilestones || 0} mốc đang thực hiện / chờ nghiệm thu
            </span>
          </div>

          <div className="p-3.5 rounded-xl bg-slate-50 border border-slate-100">
            <span className="text-[11px] font-medium text-slate-500">Tổng giá trị hợp đồng ký kết</span>
            <p className="text-sm font-extrabold text-indigo-700 mt-0.5">
              {formatVND(stats?.totalContractValue)}
            </p>
            <span className="text-[10px] text-slate-400">
              {stats?.totalContracts || 0} hợp đồng kinh tế
            </span>
          </div>
        </div>
      </div>

      {/* 6. Bảng Hợp đồng gần đây (Tương tác cao) */}
      <div className="bg-white rounded-2xl border border-slate-200 shadow-xs overflow-hidden">
        <div className="p-5 border-b border-slate-100 flex items-center justify-between">
          <div>
            <h4 className="text-sm font-bold text-slate-900 flex items-center space-x-2">
              <FileCheck className="w-4 h-4 text-indigo-600" />
              <span>Hợp đồng kinh tế gần đây</span>
            </h4>
            <p className="text-xs text-slate-400 mt-0.5">
              {stats?.recentContracts?.length || 0} hợp đồng mới nhất phát sinh trên hệ thống
            </p>
          </div>

          <Link
            to="/contracts"
            className="text-xs font-semibold text-sky-600 hover:text-sky-700 hover:underline flex items-center space-x-1"
          >
            <span>Xem tất cả hợp đồng</span>
            <ArrowRight className="w-3.5 h-3.5" />
          </Link>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse text-xs">
            <thead>
              <tr className="bg-slate-50/70 border-b border-slate-100 text-slate-500 font-semibold uppercase tracking-wider text-[11px]">
                <th className="py-3 px-4">Số HĐ</th>
                <th className="py-3 px-4">Gói thầu</th>
                <th className="py-3 px-4">Nhà thầu</th>
                <th className="py-3 px-4 text-right">Giá trị hợp đồng</th>
                <th className="py-3 px-4 text-right">Tiết kiệm</th>
                <th className="py-3 px-4 text-center">Trạng thái</th>
                <th className="py-3 px-4 text-right">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 text-slate-700">
              {stats?.recentContracts?.length > 0 ? (
                stats.recentContracts.map((c) => (
                  <tr
                    key={c.contractId}
                    onClick={() => navigate(`/contracts/${c.contractId}`)}
                    className="hover:bg-slate-50/80 transition cursor-pointer group"
                  >
                    <td className="py-3 px-4 font-mono font-bold text-sky-700 group-hover:underline">
                      {c.contractNumber}
                    </td>
                    <td className="py-3 px-4 max-w-xs truncate" title={c.bidPackageName}>
                      <span className="font-semibold text-slate-800">{c.bidPackageName}</span>
                      <p className="text-[11px] text-slate-400 font-mono mt-0.5">{c.bidPackageCode}</p>
                    </td>
                    <td className="py-3 px-4 text-slate-700 font-medium">
                      {c.companyName}
                    </td>
                    <td className="py-3 px-4 text-right font-bold text-slate-900">
                      {formatVND(c.contractValue)}
                    </td>
                    <td className="py-3 px-4 text-right text-emerald-600 font-bold">
                      +{formatVND(c.savings)}
                    </td>
                    <td className="py-3 px-4 text-center">
                      <span className={`inline-block px-2.5 py-0.5 rounded-full text-[10px] font-bold border ${statusBadge(c.status)}`}>
                        {statusLabel(c.status)}
                      </span>
                    </td>
                    <td className="py-3 px-4 text-right">
                      <button
                        type="button"
                        onClick={(e) => {
                          e.stopPropagation();
                          navigate(`/contracts/${c.contractId}`);
                        }}
                        className="p-1.5 text-slate-400 hover:text-sky-600 hover:bg-sky-50 rounded-lg transition"
                        title="Mở trang chi tiết hợp đồng"
                      >
                        <Eye className="w-4 h-4" />
                      </button>
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={7} className="py-8 text-center text-slate-400">
                    Chưa có hợp đồng nào được ghi nhận trên hệ thống.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};

export default DashboardPage;
