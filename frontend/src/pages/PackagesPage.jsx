import React, { useEffect, useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { bidPackageApi } from '../api/bidPackageApi';
import PackageCard from '../components/packages/PackageCard';
import CreatePackageModal from '../components/packages/CreatePackageModal';
import LoadingSpinner from '../components/common/LoadingSpinner';
import { ROLES, PACKAGE_STATUS } from '../utils/constants';
import toast from 'react-hot-toast';
import {
  Search,
  Plus,
  Filter,
  FolderSearch,
  ChevronLeft,
  ChevronRight,
  RotateCcw
} from 'lucide-react';

const FILTER_TABS = [
  { label: 'Tất cả', value: '' },
  { label: 'Đang mở thầu', value: PACKAGE_STATUS.OPEN },
  { label: 'Đang chấm điểm', value: PACKAGE_STATUS.EVALUATING },
  { label: 'Đã ký hợp đồng', value: PACKAGE_STATUS.CONTRACTED },
  { label: 'Đã đóng thầu', value: PACKAGE_STATUS.CLOSED },
];

export const PackagesPage = () => {
  const navigate = useNavigate();
  const { hasRole } = useAuth();
  const canCreate = hasRole([ROLES.ADMIN, ROLES.PROCUREMENT]);

  const [packages, setPackages] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);

  // Bộ lọc & Phân trang
  const [activeTab, setActiveTab] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedType, setSelectedType] = useState('');
  const [pageIndex, setPageIndex] = useState(1);
  const [pageSize] = useState(6);
  const [paginationInfo, setPaginationInfo] = useState({
    totalCount: 0,
    totalPages: 1,
    hasPreviousPage: false,
    hasNextPage: false,
  });

  const fetchPackages = useCallback(async () => {
    setLoading(true);
    setError('');

    try {
      const params = {
        pageIndex,
        pageSize,
      };

      if (activeTab) params.status = activeTab;
      if (searchTerm.trim()) params.search = searchTerm.trim();
      if (selectedType) params.type = selectedType;

      const response = await bidPackageApi.getPackages(params);

      if (response && response.success && response.data) {
        setPackages(response.data.items || []);
        setPaginationInfo({
          totalCount: response.data.totalCount || 0,
          totalPages: response.data.totalPages || 1,
          hasPreviousPage: response.data.hasPreviousPage || false,
          hasNextPage: response.data.hasNextPage || false,
        });
      } else {
        setError(response?.message || 'Không thể tải danh sách gói thầu');
      }
    } catch (err) {
      setError(err.message || 'Lỗi kết nối máy chủ');
    } finally {
      setLoading(false);
    }
  }, [pageIndex, pageSize, activeTab, searchTerm, selectedType]);

  useEffect(() => {
    fetchPackages();
  }, [fetchPackages]);

  const handleTabChange = (tabValue) => {
    setActiveTab(tabValue);
    setPageIndex(1);
  };

  const handleSearchSubmit = (e) => {
    e.preventDefault();
    setPageIndex(1);
    fetchPackages();
  };

  const handleResetFilter = () => {
    setActiveTab('');
    setSearchTerm('');
    setSelectedType('');
    setPageIndex(1);
  };

  const handleCreateClick = () => {
    setIsCreateModalOpen(true);
  };

  const handleCreateSuccess = (createdPackage) => {
    setIsCreateModalOpen(false);
    if (createdPackage?.id) {
      navigate(`/packages/${createdPackage.id}`);
    } else {
      fetchPackages();
    }
  };

  return (
    <div className="space-y-6">
      {/* Tiêu đề & Nút Tạo gói thầu */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h2 className="text-2xl font-bold text-slate-900 tracking-tight">
            Gói thầu
          </h2>
          <p className="text-xs text-slate-500 mt-1">
            Danh sách hồ sơ mời thầu
          </p>
        </div>

        {canCreate && (
          <button
            onClick={handleCreateClick}
            className="inline-flex items-center space-x-1.5 px-4 py-2 bg-slate-900 hover:bg-slate-800 text-white rounded-xl text-xs font-semibold shadow-xs hover:shadow transition"
          >
            <Plus className="w-4 h-4" />
            <span>Tạo gói thầu mới</span>
          </button>
        )}
      </div>

      {/* Dải Tabs trạng thái */}
      <div className="flex items-center space-x-1 border-b border-slate-200 overflow-x-auto pb-px">
        {FILTER_TABS.map((tab) => {
          const isActive = activeTab === tab.value;
          return (
            <button
              key={tab.label}
              onClick={() => handleTabChange(tab.value)}
              className={`px-4 py-2 text-xs font-semibold whitespace-nowrap border-b-2 transition ${
                isActive
                  ? 'border-slate-900 text-slate-900'
                  : 'border-transparent text-slate-500 hover:text-slate-800 hover:border-slate-300'
              }`}
            >
              {tab.label}
            </button>
          );
        })}
      </div>

      {/* Thanh tìm kiếm & lọc nâng cao */}
      <div className="bg-white p-4 rounded-2xl border border-slate-200 shadow-xs flex flex-col sm:flex-row items-center gap-3">
        <form onSubmit={handleSearchSubmit} className="flex-1 w-full relative">
          <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
            <Search className="w-4 h-4" />
          </div>
          <input
            type="text"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            placeholder="Tìm theo tên hoặc mã gói thầu..."
            className="w-full pl-9 pr-3 py-2 text-xs bg-slate-50 border border-slate-200 rounded-xl text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-sky-500 focus:border-sky-500 transition"
          />
        </form>

        <div className="flex items-center space-x-2 w-full sm:w-auto">
          {/* Hộp chọn loại gói thầu */}
          <div className="relative flex-1 sm:w-40">
            <select
              value={selectedType}
              onChange={(e) => {
                setSelectedType(e.target.value);
                setPageIndex(1);
              }}
              className="w-full py-2 pl-3 pr-8 text-xs bg-slate-50 border border-slate-200 rounded-xl text-slate-800 focus:outline-none focus:ring-2 focus:ring-sky-500 transition appearance-none cursor-pointer"
            >
              <option value="">Tất cả loại</option>
              <option value="Goods">Hàng hóa</option>
              <option value="Construction">Xây lắp</option>
              <option value="Service">Dịch vụ</option>
            </select>
            <div className="absolute inset-y-0 right-0 pr-2.5 flex items-center pointer-events-none text-slate-400">
              <Filter className="w-3.5 h-3.5" />
            </div>
          </div>

          {/* Nút reset bộ lọc */}
          {(searchTerm || activeTab || selectedType) && (
            <button
              onClick={handleResetFilter}
              className="p-2 text-slate-400 hover:text-slate-600 hover:bg-slate-100 rounded-xl transition"
              title="Đặt lại bộ lọc"
            >
              <RotateCcw className="w-4 h-4" />
            </button>
          )}
        </div>
      </div>

      {/* Nội dung danh sách */}
      {loading ? (
        <div className="py-20 flex justify-center">
          <LoadingSpinner size="lg" text="Đang tải danh sách gói thầu..." />
        </div>
      ) : error ? (
        <div className="p-8 text-center bg-white rounded-2xl border border-rose-200 shadow-xs max-w-lg mx-auto">
          <p className="text-sm font-semibold text-rose-600 mb-3">{error}</p>
          <button
            onClick={fetchPackages}
            className="px-4 py-2 bg-slate-900 text-white rounded-lg text-xs font-semibold hover:bg-slate-800 transition"
          >
            Tải lại
          </button>
        </div>
      ) : packages.length === 0 ? (
        <div className="bg-white p-12 rounded-2xl border border-slate-200 shadow-xs text-center space-y-3">
          <div className="w-12 h-12 bg-slate-100 text-slate-400 rounded-full flex items-center justify-center mx-auto">
            <FolderSearch className="w-6 h-6" />
          </div>
          <h3 className="text-sm font-bold text-slate-800">
            Không tìm thấy gói thầu phù hợp
          </h3>
          <p className="text-xs text-slate-500 max-w-sm mx-auto">
            Không có gói thầu nào khớp với tiêu chí tìm kiếm hoặc bộ lọc hiện tại.
          </p>
          <button
            onClick={handleResetFilter}
            className="mt-2 px-3.5 py-1.5 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-lg text-xs font-semibold transition"
          >
            Xóa bộ lọc
          </button>
        </div>
      ) : (
        <>
          {/* Lưới thẻ gói thầu */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
            {packages.map((pkg) => (
              <PackageCard key={pkg.id} pkg={pkg} />
            ))}
          </div>

          {/* Thanh phân trang */}
          <div className="bg-white px-5 py-3.5 rounded-2xl border border-slate-200 shadow-xs flex items-center justify-between text-xs text-slate-600">
            <span>
              Tổng số <strong className="text-slate-900 font-bold">{paginationInfo.totalCount}</strong> gói thầu
            </span>

            <div className="flex items-center space-x-2">
              <span className="text-slate-500 mr-2">
                Trang {pageIndex} / {paginationInfo.totalPages || 1}
              </span>

              <button
                disabled={!paginationInfo.hasPreviousPage}
                onClick={() => setPageIndex((prev) => Math.max(prev - 1, 1))}
                className="p-1.5 rounded-lg border border-slate-200 text-slate-600 hover:bg-slate-50 disabled:opacity-40 disabled:cursor-not-allowed transition"
                title="Trang trước"
              >
                <ChevronLeft className="w-4 h-4" />
              </button>

              <button
                disabled={!paginationInfo.hasNextPage}
                onClick={() => setPageIndex((prev) => prev + 1)}
                className="p-1.5 rounded-lg border border-slate-200 text-slate-600 hover:bg-slate-50 disabled:opacity-40 disabled:cursor-not-allowed transition"
                title="Trang sau"
              >
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        </>
      )}

      {/* Modal tạo gói thầu mới */}
      <CreatePackageModal
        isOpen={isCreateModalOpen}
        onClose={() => setIsCreateModalOpen(false)}
        onSuccess={handleCreateSuccess}
      />
    </div>
  );
};

export default PackagesPage;
