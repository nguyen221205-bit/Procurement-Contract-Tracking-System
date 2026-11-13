import React from 'react';
import { FileCheck, Lock, Clock, Eye, Ban } from 'lucide-react';
import { formatCurrency, formatDate, formatDateTime } from '../../utils/formatters';

const PUBLIC_PRICE_STATUSES = new Set(['Awarded', 'Contracted', '3', '4']);

export const renderSubmissionBidPrice = ({
  pkgStatus,
  bidPrice,
  isEvaluator,
  isAssignedEvaluator,
  isAdmin,
  isProcurement,
}) => {
  const statusStr = String(pkgStatus ?? '');
  if (PUBLIC_PRICE_STATUSES.has(statusStr)) {
    return <span className="font-bold text-emerald-700">{bidPrice ? formatCurrency(bidPrice) : '-'}</span>;
  }

  const canEvaluatorView =
    (statusStr === 'Evaluating' || statusStr === '2') &&
    (isEvaluator || isAssignedEvaluator || isAdmin || isProcurement);

  if (canEvaluatorView) {
    return <span className="font-bold text-sky-700">{bidPrice ? formatCurrency(bidPrice) : '-'}</span>;
  }

  return (
    <span className="text-amber-600 font-medium text-[11px] inline-flex items-center space-x-1">
      <Lock className="w-3 h-3" />
      <span>Niêm phong</span>
    </span>
  );
};

export const PackageSubmissionsTab = ({
  pkg,
  submissions = [],
  isOpen = false,
  canManageStatus = false,
  isEvaluator = false,
  isAssignedEvaluator = false,
  isAdmin = false,
  isProcurement = false,
  onOpenStatusModal,
  onViewSubmissionDetail,
  onSelectDisqualifyTarget,
}) => {
  return (
    <div className="bg-white rounded-2xl border border-slate-200 shadow-xs p-6 space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-sm font-bold text-slate-900 flex items-center space-x-2">
            <FileCheck className="w-4 h-4 text-sky-600" />
            <span>Danh sách hồ sơ dự thầu đã tiếp nhận</span>
          </h3>
          <p className="text-xs text-slate-500 mt-0.5">
            Hồ sơ kỹ thuật và tài chính do các nhà thầu gửi lên hệ thống.
          </p>
        </div>
        {isOpen ? (
          <span className="text-xs font-bold px-2.5 py-1 bg-amber-50 text-amber-700 border border-amber-200 rounded-lg flex items-center space-x-1">
            <Lock className="w-3.5 h-3.5" />
            <span>Đang niêm phong</span>
          </span>
        ) : (
          <span className="text-xs font-bold px-2.5 py-1 bg-slate-100 text-slate-700 rounded-lg">
            {submissions.length} hồ sơ
          </span>
        )}
      </div>

      {isOpen ? (
        <div className="p-6 bg-gradient-to-br from-amber-50/80 via-amber-50/40 to-white rounded-2xl border border-amber-200/90 text-amber-900 space-y-4">
          <div className="flex items-start space-x-3.5">
            <div className="p-2.5 bg-amber-100 text-amber-700 rounded-xl mt-0.5 shrink-0 shadow-xs">
              <Lock className="w-5 h-5" />
            </div>
            <div className="space-y-1.5 flex-1">
              <div className="flex items-center space-x-2">
                <h4 className="text-sm font-bold text-amber-900">
                  Hồ sơ dự thầu đang trong trạng thái niêm phong bảo mật
                </h4>
                <span className="text-[10px] px-2 py-0.5 bg-amber-200/70 text-amber-800 rounded-full font-bold">
                  Sealed Bids
                </span>
              </div>
              <p className="text-xs text-amber-800 leading-relaxed">
                Theo quy định tại <strong>Luật Đấu thầu</strong> và nguyên tắc bảo mật thông tin, khi gói thầu đang trong giai đoạn tiếp nhận hồ sơ (<strong>Đang mở</strong>), toàn bộ hồ sơ do các nhà thầu gửi lên được <strong>niêm phong điện tử tự động</strong>. Bên mời thầu, Giám khảo hay Quản trị viên đều không được phép mở xem trước danh sách nhằm chống lộ giá thầu và đảm bảo tính công bằng, minh bạch.
              </p>
              <p className="text-xs text-amber-900/90 font-medium">
                Hệ thống đã ghi nhận hồ sơ nộp thành công vào cơ sở dữ liệu. Sau khi kết thúc thời gian nhận hồ sơ, Bên mời thầu thực hiện thao tác <strong>"Đóng nhận hồ sơ thầu"</strong> để mở niêm phong và truy cập danh sách chấm điểm.
              </p>
            </div>
          </div>

          {canManageStatus && (
            <div className="pt-3 border-t border-amber-200/60 flex flex-wrap items-center justify-between gap-3">
              <div className="flex items-center space-x-1.5 text-xs text-amber-700">
                <Clock className="w-3.5 h-3.5 text-amber-600" />
                <span>
                  Hạn nộp thầu: <strong>{formatDate(pkg?.deadline)}</strong>
                </span>
              </div>
              <button
                onClick={onOpenStatusModal}
                className="px-4 py-2 bg-amber-600 hover:bg-amber-700 active:scale-95 text-white text-xs font-bold rounded-xl shadow-xs transition flex items-center space-x-2"
              >
                <Lock className="w-3.5 h-3.5" />
                <span>Đóng nhận hồ sơ thầu (Mở niêm phong ngay)</span>
              </button>
            </div>
          )}
        </div>
      ) : submissions.length > 0 ? (
        <div className="border border-slate-100 rounded-xl overflow-hidden">
          <table className="w-full text-left text-xs">
            <thead className="bg-slate-50 text-slate-600 border-b border-slate-100 font-semibold">
              <tr>
                <th className="py-3 px-4 w-12 text-center">STT</th>
                <th className="py-3 px-4">Doanh nghiệp / Nhà thầu</th>
                <th className="py-3 px-4">Thời gian nộp</th>
                <th className="py-3 px-4 text-right">Giá dự thầu</th>
                <th className="py-3 px-4 text-center">Số tệp</th>
                <th className="py-3 px-4 text-center">Trạng thái</th>
                <th className="py-3 px-4 text-center">Điểm / Hạng</th>
                <th className="py-3 px-4 text-right">Chi tiết</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 text-slate-700">
              {submissions.map((sub, idx) => (
                <tr key={sub.id} className="hover:bg-slate-50/70 transition">
                  <td className="py-3 px-4 text-center font-mono text-slate-400">{idx + 1}</td>
                  <td className="py-3 px-4 font-bold text-slate-800">
                    {sub.companyName || `Nhà thầu #${sub.contractorId}`}
                  </td>
                  <td className="py-3 px-4 text-slate-500 font-mono text-[11px]">
                    {formatDateTime(sub.submittedAt)}
                  </td>
                  <td className="py-3 px-4 text-right font-mono">
                    {renderSubmissionBidPrice({
                      pkgStatus: pkg?.status,
                      bidPrice: sub.bidPrice,
                      isEvaluator,
                      isAssignedEvaluator,
                      isAdmin,
                      isProcurement,
                    })}
                  </td>
                  <td className="py-3 px-4 text-center font-mono font-medium">{sub.fileCount} tệp</td>
                  <td className="py-3 px-4 text-center">
                    <span className="px-2 py-0.5 rounded-full text-[10px] font-semibold bg-sky-50 text-sky-700 border border-sky-200">
                      {sub.status || 'Đã nộp'}
                    </span>
                  </td>
                  <td className="py-3 px-4 text-center font-mono">
                    {sub.totalScore != null ? (
                      <span className="font-bold text-slate-900">
                        {sub.totalScore} đ
                        {sub.rank && (
                          <span className="ml-1 text-sky-600 font-extrabold">(# {sub.rank})</span>
                        )}
                      </span>
                    ) : (
                      <span className="text-slate-400">-</span>
                    )}
                  </td>
                  <td className="py-3 px-4 text-right space-x-1">
                    <button
                      onClick={() => onViewSubmissionDetail(sub.id)}
                      className="p-1.5 text-slate-500 hover:text-sky-600 hover:bg-sky-50 rounded-lg transition cursor-pointer"
                      title="Xem chi tiết tệp hồ sơ"
                    >
                      <Eye className="w-4 h-4" />
                    </button>
                    {canManageStatus && sub.status !== 'Disqualified' && sub.status !== 'Withdrawn' && (
                      <button
                        onClick={() => onSelectDisqualifyTarget(sub)}
                        className="p-1.5 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition cursor-pointer"
                        title="Loại bỏ hồ sơ không hợp lệ (Disqualify)"
                      >
                        <Ban className="w-4 h-4" />
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : (
        <div className="py-8 text-center text-xs text-slate-400 bg-slate-50/50 rounded-xl border border-dashed border-slate-200">
          Chưa có nhà thầu nào nộp hồ sơ cho gói thầu này.
        </div>
      )}
    </div>
  );
};

export default PackageSubmissionsTab;
