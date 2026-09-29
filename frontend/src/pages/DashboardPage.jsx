import React, { useEffect, useState } from 'react';
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
  Layers
} from 'lucide-react';

export const DashboardPage = () => {
  const [stats, setStats] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const fetchDashboardData = async () => {
    setLoading(true);
    setError('');
    try {
      const response = await reportApi.getDashboard();
      if (response && response.success && response.data) {
        setStats(response.data);
      } else {
        setError(response?.message || 'Không thể tải dữ liệu báo cáo');
      }
    } catch (err) {
      setError(err.message || 'Lỗi kết nối tới máy chủ');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDashboardData();
  }, []);

  if (loading) {
    return (
      <div className="py-20 flex justify-center">
        <LoadingSpinner size="lg" text="Đang tải dữ liệu tổng quan..." />
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-8 text-center bg-white rounded-2xl border border-rose-200 shadow-xs max-w-lg mx-auto">
        <p className="text-sm font-semibold text-rose-600 mb-3">{error}</p>
        <button
          onClick={fetchDashboardData}
          className="px-4 py-2 bg-slate-900 text-white rounded-lg text-xs font-semibold hover:bg-slate-800 transition"
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
    return 'bg-gray-100 text-gray-700 border-gray-200';
  };

  return (
    <div className="space-y-6">
      {/* Tiêu đề trang */}
      <div>
        <h2 className="text-2xl font-bold text-slate-900 tracking-tight">
          Tổng quan
        </h2>
        <p className="text-xs text-slate-500 mt-1">
          Chỉ số tài chính và tiến độ thực hiện
        </p>
      </div>

      {/* 4 Thẻ KPI chính */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCard
          title="Ngân sách dự toán"
          value={formatVND(stats?.totalEstimatedBudget)}
          subtext={`${stats?.totalPackages || 0} gói thầu`}
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
          title="Đã giải ngân"
          value={formatVND(stats?.totalDisbursedAmount)}
          badgeText={`Giải ngân ${stats?.disbursementRate || 0}%`}
          icon={CreditCard}
          color="amber"
        />
      </div>

      {/* Biểu đồ tài chính Chart.js */}
      <FinancialCharts stats={stats} />

      {/* Bảng hợp đồng gần đây */}
      <div className="bg-white rounded-2xl border border-slate-200 shadow-xs overflow-hidden">
        <div className="p-5 border-b border-slate-100 flex items-center justify-between">
          <h4 className="text-sm font-bold text-slate-800">
            Hợp đồng gần đây
          </h4>
          <span className="text-xs text-slate-400">
            {stats?.recentContracts?.length || 0} hợp đồng mới nhất
          </span>
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
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 text-slate-700">
              {stats?.recentContracts?.length > 0 ? (
                stats.recentContracts.map((c) => (
                  <tr key={c.contractId} className="hover:bg-slate-50/60 transition">
                    <td className="py-3 px-4 font-mono font-semibold text-slate-900">
                      {c.contractNumber}
                    </td>
                    <td className="py-3 px-4 max-w-xs truncate" title={c.bidPackageName}>
                      <span className="font-medium text-slate-800">{c.bidPackageName}</span>
                      <p className="text-[11px] text-slate-400 font-mono">{c.bidPackageCode}</p>
                    </td>
                    <td className="py-3 px-4 text-slate-600 font-medium">
                      {c.companyName}
                    </td>
                    <td className="py-3 px-4 text-right font-semibold text-slate-900">
                      {formatVND(c.contractValue)}
                    </td>
                    <td className="py-3 px-4 text-right text-emerald-600 font-medium">
                      +{formatVND(c.savings)}
                    </td>
                    <td className="py-3 px-4 text-center">
                      <span className={`inline-block px-2 py-0.5 rounded text-[11px] font-medium border ${statusBadge(c.status)}`}>
                        {c.status}
                      </span>
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={6} className="py-8 text-center text-slate-400">
                    Chưa có hợp đồng nào được ghi nhận
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
