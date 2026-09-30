import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import toast from 'react-hot-toast';
import {
  FileText,
  Plus,
  Search,
  Filter,
  RefreshCw,
  Building2,
  Calendar,
  CheckCircle2,
  Clock,
  AlertCircle,
  TrendingUp,
  ArrowRight,
  DollarSign,
  Layers,
  ChevronLeft,
  ChevronRight,
  Eye,
  SlidersHorizontal
} from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import { contractApi } from '../api/contractApi';
import StatusBadge from '../components/common/StatusBadge';
import LoadingSpinner from '../components/common/LoadingSpinner';
import CreateContractModal from '../components/contracts/CreateContractModal';
import { formatCurrency, formatDate, formatNumber, formatPercent } from '../utils/formatters';

export const ContractsPage = () => {
  const { hasRole, user } = useAuth();
  const isAdminOrProcurement = hasRole(['Admin', 'Procurement']);

  // Data states
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [contractsData, setContractsData] = useState({
    items: [],
    pageIndex: 1,
    totalPages: 1,
    totalCount: 0,
    pageSize: 10,
  });

  // Filter states
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedStatus, setSelectedStatus] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  // Create Contract Modal state
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);

  // Fetch contracts
  const fetchContracts = async (isBackground = false) => {
    if (!isBackground) setLoading(true);
    else setRefreshing(true);

    try {
      const params = {
        pageIndex: page,
        pageSize,
      };

      if (searchTerm.trim()) {
        params.search = searchTerm.trim();
      }

      if (selectedStatus) {
        params.status = selectedStatus;
      }

      const res = await contractApi.getContracts(params);
      if (res?.data) {
        setContractsData(res.data);
      }
    } catch (err) {
      console.error('Lỗi khi tải danh sách hợp đồng:', err);
      toast.error('Không thể tải danh sách hợp đồng.');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => {
    fetchContracts();
  }, [page, pageSize, selectedStatus]);

  // Handle Search submit
  const handleSearchSubmit = (e) => {
    e.preventDefault();
    setPage(1);
    fetchContracts();
  };

  // Reset filters
  const handleResetFilters = () => {
    setSearchTerm('');
    setSelectedStatus('');
    setPage(1);
  };

  // Calculate high-level summary KPIs from items
  const items = contractsData.items || [];
  const totalValue = items.reduce((sum, c) => sum + (c.value || 0), 0);
  const totalDisbursed = items.reduce((sum, c) => sum + (c.totalDisbursedAmount || 0), 0);
  const overallRate = totalValue > 0 ? Math.round((totalDisbursed / totalValue) * 100) : 0;

  const activeCount = items.filter((c) => c.status === 'Active' || c.status === 1).length;
  const draftCount = items.filter((c) => c.status === 'Draft' || c.status === 0).length;
  const completedCount = items.filter((c) => c.status === 'Completed' || c.status === 2).length;

  return (
    <div className="space-y-6 pb-12">
      {/* HEADER SECTION */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-xl md:text-2xl font-black text-slate-900 tracking-tight flex items-center gap-2.5">
            Quản Lý Hợp Đồng
          </h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Theo dõi tình trạng thực hiện hợp đồng, tiến độ giải ngân & kiểm soát mốc thanh toán nghiệm thu
          </p>
        </div>

        <div className="flex items-center gap-2.5 flex-wrap">
          <button
            type="button"
            onClick={() => fetchContracts(true)}
            disabled={refreshing}
            className="p-2.5 bg-white border border-slate-200 hover:border-slate-300 rounded-xl text-slate-600 hover:text-slate-900 shadow-2xs transition-colors cursor-pointer"
            title="Làm mới dữ liệu"
          >
            <RefreshCw className={`w-4 h-4 ${refreshing ? 'animate-spin text-blue-600' : ''}`} />
          </button>

          {isAdminOrProcurement && (
            <button
              type="button"
              onClick={() => setIsCreateModalOpen(true)}
              className="inline-flex items-center gap-2 px-4 py-2.5 bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold rounded-xl shadow-xs transition-transform hover:scale-[1.02] cursor-pointer"
            >
              <Plus className="w-4 h-4" />
              <span>Lập Hợp Đồng Mới</span>
            </button>
          )}
        </div>
      </div>

      {/* KPI METRICS CARDS */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-3.5">
        {/* Total Contracts */}
        <div className="bg-white p-4 rounded-2xl border border-slate-200 shadow-2xs space-y-1">
          <div className="flex items-center justify-between">
            <span className="text-[11px] font-bold text-slate-500 uppercase tracking-wider">Tổng hợp đồng</span>
            <div className="p-2 bg-blue-50 text-blue-600 rounded-xl">
              <FileText className="w-4 h-4" />
            </div>
          </div>
          <p className="text-2xl font-black text-slate-900">{contractsData.totalCount || items.length}</p>
          <span className="text-[10px] text-slate-400 block">Hợp đồng đã thiết lập</span>
        </div>

        {/* Active Contracts */}
        <div className="bg-white p-4 rounded-2xl border border-slate-200 shadow-2xs space-y-1">
          <div className="flex items-center justify-between">
            <span className="text-[11px] font-bold text-slate-500 uppercase tracking-wider">Đang thực hiện</span>
            <div className="p-2 bg-emerald-50 text-emerald-600 rounded-xl">
              <Clock className="w-4 h-4" />
            </div>
          </div>
          <p className="text-2xl font-black text-emerald-600">{activeCount}</p>
          <span className="text-[10px] text-slate-400 block">Đang có hiệu lực (Active)</span>
        </div>

        {/* Draft Contracts */}
        <div className="bg-white p-4 rounded-2xl border border-slate-200 shadow-2xs space-y-1">
          <div className="flex items-center justify-between">
            <span className="text-[11px] font-bold text-slate-500 uppercase tracking-wider">Bản thảo (Draft)</span>
            <div className="p-2 bg-amber-50 text-amber-600 rounded-xl">
              <Layers className="w-4 h-4" />
            </div>
          </div>
          <p className="text-2xl font-black text-amber-600">{draftCount}</p>
          <span className="text-[10px] text-slate-400 block">Chờ kích hoạt ký kết</span>
        </div>

        {/* Total Contracted Value */}
        <div className="bg-white p-4 rounded-2xl border border-slate-200 shadow-2xs space-y-1">
          <div className="flex items-center justify-between">
            <span className="text-[11px] font-bold text-slate-500 uppercase tracking-wider">Tổng giá trị ký kết</span>
            <div className="p-2 bg-indigo-50 text-indigo-600 rounded-xl">
              <DollarSign className="w-4 h-4" />
            </div>
          </div>
          <p className="text-lg font-black text-slate-900 truncate" title={formatCurrency(totalValue)}>
            {formatCurrency(totalValue)}
          </p>
          <span className="text-[10px] text-slate-400 block">Giá trị các hợp đồng</span>
        </div>

        {/* Total Disbursed */}
        <div className="bg-white p-4 rounded-2xl border border-slate-200 shadow-2xs space-y-1">
          <div className="flex items-center justify-between">
            <span className="text-[11px] font-bold text-slate-500 uppercase tracking-wider">Đã giải ngân</span>
            <span className="text-xs font-black text-emerald-600 bg-emerald-50 px-2 py-0.5 rounded-full">
              {overallRate}%
            </span>
          </div>
          <p className="text-lg font-black text-emerald-600 truncate" title={formatCurrency(totalDisbursed)}>
            {formatCurrency(totalDisbursed)}
          </p>
          <span className="text-[10px] text-slate-400 block">Nghiệm thu thực tế</span>
        </div>
      </div>

      {/* FILTER & SEARCH BAR */}
      <div className="bg-white p-4 rounded-2xl border border-slate-200 shadow-2xs">
        <form onSubmit={handleSearchSubmit} className="flex flex-col md:flex-row gap-3 items-center justify-between">
          <div className="flex flex-col sm:flex-row items-center gap-3 w-full md:w-auto flex-1">
            {/* Search Input */}
            <div className="relative w-full sm:w-80">
              <Search className="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2" />
              <input
                type="text"
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                placeholder="Tìm số HĐ, gói thầu, tên nhà thầu..."
                className="w-full pl-9 pr-3.5 py-2 text-xs text-slate-800 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:ring-2 focus:ring-blue-500 focus:outline-hidden"
              />
            </div>

            {/* Status Dropdown */}
            <div className="w-full sm:w-48">
              <select
                value={selectedStatus}
                onChange={(e) => {
                  setSelectedStatus(e.target.value);
                  setPage(1);
                }}
                className="w-full px-3 py-2 text-xs font-semibold text-slate-700 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:ring-2 focus:ring-blue-500 focus:outline-hidden cursor-pointer"
              >
                <option value="">Tất cả trạng thái</option>
                <option value="Draft">Bản thảo (Draft)</option>
                <option value="Active">Đang thực hiện (Active)</option>
                <option value="Completed">Đã hoàn thành (Completed)</option>
                <option value="Terminated">Đã chấm dứt (Terminated)</option>
              </select>
            </div>

            <button
              type="submit"
              className="w-full sm:w-auto px-4 py-2 bg-slate-800 hover:bg-slate-900 text-white text-xs font-bold rounded-xl transition-colors cursor-pointer"
            >
              Tìm kiếm
            </button>

            {(searchTerm || selectedStatus) && (
              <button
                type="button"
                onClick={handleResetFilters}
                className="text-xs text-slate-500 hover:text-slate-800 font-semibold cursor-pointer underline"
              >
                Đặt lại
              </button>
            )}
          </div>

          <div className="text-xs text-slate-500 font-medium">
            Hiển thị <strong>{items.length}</strong> / <strong>{contractsData.totalCount || items.length}</strong> hợp đồng
          </div>
        </form>
      </div>

      {/* CONTRACTS TABLE */}
      <div className="bg-white rounded-2xl border border-slate-200 shadow-2xs overflow-hidden">
        {loading ? (
          <div className="py-20 flex items-center justify-center">
            <LoadingSpinner size="lg" message="Đang tải dữ liệu danh sách hợp đồng..." />
          </div>
        ) : items.length === 0 ? (
          <div className="py-16 text-center space-y-3">
            <div className="w-12 h-12 rounded-full bg-slate-100 text-slate-400 mx-auto flex items-center justify-center">
              <FileText className="w-6 h-6" />
            </div>
            <h3 className="text-sm font-bold text-slate-700">Chưa có hợp đồng nào</h3>
            <p className="text-xs text-slate-400 max-w-sm mx-auto">
              Không tìm thấy hợp đồng nào phù hợp với bộ lọc tìm kiếm hiện tại.
            </p>
            {isAdminOrProcurement && (
              <button
                type="button"
                onClick={() => setIsCreateModalOpen(true)}
                className="inline-flex items-center gap-1.5 px-4 py-2 bg-blue-50 hover:bg-blue-100 text-blue-700 text-xs font-bold rounded-xl transition-colors cursor-pointer"
              >
                <Plus className="w-3.5 h-3.5" />
                <span>Lập hợp đồng mới ngay</span>
              </button>
            )}
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="bg-slate-50 border-b border-slate-200 text-[11px] font-bold text-slate-500 uppercase tracking-wider">
                  <th className="py-3.5 px-4">Số hiệu HĐ & Gói thầu</th>
                  <th className="py-3.5 px-4">Nhà thầu thực hiện</th>
                  <th className="py-3.5 px-4 text-right">Giá trị hợp đồng</th>
                  <th className="py-3.5 px-4">Tiến độ giải ngân</th>
                  <th className="py-3.5 px-4 text-center" title="Số lượng mốc thanh toán nghiệm thu của hợp đồng">
                    Số mốc
                  </th>
                  <th className="py-3.5 px-4">Thời hạn thực hiện</th>
                  <th className="py-3.5 px-4 text-center">Trạng thái</th>
                  <th className="py-3.5 px-4 text-center">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 text-xs text-slate-700">
                {items.map((contract) => {
                  const rate =
                    contract.value > 0
                      ? Math.round((contract.totalDisbursedAmount / contract.value) * 100)
                      : 0;

                  return (
                    <tr
                      key={contract.id}
                      className="hover:bg-slate-50/80 transition-colors group cursor-pointer"
                    >
                      {/* Số HĐ & Gói thầu */}
                      <td className="py-3.5 px-4">
                        <Link
                          to={`/contracts/${contract.id}`}
                          className="font-black text-blue-600 hover:text-blue-800 hover:underline flex items-center gap-1.5"
                        >
                          <FileText className="w-4 h-4 text-blue-500 flex-shrink-0" />
                          <span>{contract.contractNumber}</span>
                        </Link>
                        <div className="text-[11px] text-slate-400 flex items-center gap-1 mt-0.5">
                          <span className="font-semibold text-slate-600">[{contract.bidPackageCode}]</span>
                        </div>
                      </td>

                      {/* Nhà thầu & MST */}
                      <td className="py-3.5 px-4 max-w-xs">
                        <div className="font-bold text-slate-900 line-clamp-1" title={contract.companyName}>
                          {contract.companyName}
                        </div>
                        <span className="text-[11px] text-slate-400 block">
                          MST: {contract.taxCode || '0300588569'}
                        </span>
                      </td>

                      {/* Giá trị HĐ */}
                      <td className="py-3.5 px-4 text-right font-black text-slate-900 whitespace-nowrap">
                        {formatCurrency(contract.value)}
                      </td>

                      {/* Tiến độ giải ngân */}
                      <td className="py-3.5 px-4 min-w-44">
                        <div className="space-y-1">
                          <div className="flex items-center justify-between text-[11px]">
                            <span className="font-bold text-slate-800">{rate}%</span>
                            <span className="text-slate-400">
                              {formatCurrency(contract.totalDisbursedAmount)}
                            </span>
                          </div>
                          <div className="w-full bg-slate-100 h-1.5 rounded-full overflow-hidden">
                            <div
                              className={`h-full rounded-full transition-all duration-500 ${
                                rate === 100
                                  ? 'bg-blue-600'
                                  : rate > 0
                                  ? 'bg-emerald-500'
                                  : 'bg-slate-300'
                              }`}
                              style={{ width: `${Math.min(rate, 100)}%` }}
                            />
                          </div>
                        </div>
                      </td>

                      {/* Số mốc */}
                      <td className="py-3.5 px-4 text-center">
                        <span className="inline-flex items-center justify-center w-6 h-6 rounded-full bg-slate-100 text-slate-700 font-bold text-xs">
                          {contract.milestoneCount || 0}
                        </span>
                      </td>

                      {/* Thời hạn */}
                      <td className="py-3.5 px-4 text-[11px] text-slate-600 whitespace-nowrap">
                        <div className="flex items-center gap-1">
                          <Calendar className="w-3.5 h-3.5 text-slate-400" />
                          <span>{formatDate(contract.startDate)}</span>
                          <span className="text-slate-400">→</span>
                          <span>{formatDate(contract.endDate)}</span>
                        </div>
                      </td>

                      {/* Trạng thái */}
                      <td className="py-3.5 px-4 text-center whitespace-nowrap">
                        <StatusBadge status={contract.status} type="contract" />
                      </td>

                      {/* Thao tác */}
                      <td className="py-3.5 px-4 text-center whitespace-nowrap">
                        <Link
                          to={`/contracts/${contract.id}`}
                          className="inline-flex items-center gap-1 px-3 py-1.5 bg-slate-100 hover:bg-blue-50 text-slate-700 hover:text-blue-700 text-xs font-bold rounded-xl transition-colors cursor-pointer"
                        >
                          <Eye className="w-3.5 h-3.5" />
                          <span>Chi tiết</span>
                        </Link>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}

        {/* PAGINATION */}
        {contractsData.totalPages > 1 && (
          <div className="p-4 border-t border-slate-200 flex flex-col sm:flex-row items-center justify-between gap-3 text-xs text-slate-500">
            <div>
              Trang <strong>{contractsData.pageIndex}</strong> / <strong>{contractsData.totalPages}</strong> (Tổng {contractsData.totalCount} bản ghi)
            </div>

            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={!contractsData.hasPreviousPage}
                className="p-1.5 bg-white border border-slate-200 rounded-lg disabled:opacity-40 disabled:cursor-not-allowed hover:bg-slate-50 transition-colors cursor-pointer"
              >
                <ChevronLeft className="w-4 h-4" />
              </button>

              <span className="font-bold text-slate-700 px-2">{contractsData.pageIndex}</span>

              <button
                type="button"
                onClick={() => setPage((p) => Math.min(contractsData.totalPages, p + 1))}
                disabled={!contractsData.hasNextPage}
                className="p-1.5 bg-white border border-slate-200 rounded-lg disabled:opacity-40 disabled:cursor-not-allowed hover:bg-slate-50 transition-colors cursor-pointer"
              >
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        )}
      </div>

      {/* CREATE CONTRACT MODAL */}
      <CreateContractModal
        isOpen={isCreateModalOpen}
        onClose={() => setIsCreateModalOpen(false)}
        onContractCreated={() => {
          fetchContracts(true);
        }}
      />
    </div>
  );
};

export default ContractsPage;
