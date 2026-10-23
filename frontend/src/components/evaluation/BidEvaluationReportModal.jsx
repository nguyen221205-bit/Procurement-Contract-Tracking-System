import React, { useState, useEffect, useRef } from 'react';
import {
  X,
  Printer,
  Download,
  FileText,
  ZoomIn,
  ZoomOut,
  Maximize2,
  CheckCircle,
  Award,
  Building2,
  Calendar,
  DollarSign,
  Users,
  ShieldCheck,
  Clock,
  AlertCircle,
  ExternalLink,
} from 'lucide-react';
import evaluationApi from '../../api/evaluationApi';
import LoadingSpinner from '../common/LoadingSpinner';

export default function BidEvaluationReportModal({ isOpen, onClose, packageId, fallbackData }) {
  const [report, setReport] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [zoomLevel, setZoomLevel] = useState(100);
  const reportRef = useRef(null);

  useEffect(() => {
    if (isOpen && packageId) {
      fetchReportData();
    }
  }, [isOpen, packageId]);

  // Lắng nghe phím Escape
  useEffect(() => {
    const handleKeyDown = (e) => {
      if (e.key === 'Escape') onClose();
    };
    if (isOpen) {
      window.addEventListener('keydown', handleKeyDown);
    }
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose]);

  const fetchReportData = async () => {
    try {
      setLoading(true);
      setError(null);
      const res = await evaluationApi.getEvaluationReport(packageId);
      if (res && res.success && res.data) {
        setReport(res.data);
      } else {
        // Fallback sang dữ liệu có sẵn nếu API chưa trả
        if (fallbackData) {
          setReport(buildFallbackReport(fallbackData));
        } else {
          setError(res?.message || 'Không thể tải dữ liệu báo cáo đánh giá.');
        }
      }
    } catch (err) {
      console.warn('API getEvaluationReport error, falling back to local data if available:', err);
      if (fallbackData) {
        setReport(buildFallbackReport(fallbackData));
      } else {
        setError(err.message || 'Lỗi kết nối khi tải báo cáo đánh giá.');
      }
    } finally {
      setLoading(false);
    }
  };

  // Helper chuẩn hóa Unicode NFC chống lỗi dãn dấu Tiếng Việt
  const nfc = (text) => (typeof text === 'string' ? text.normalize('NFC') : text);

  // Helper chuyển đổi dữ liệu fallback nếu cần
  const buildFallbackReport = (data) => {
    const pkg = data.packageData || {};
    const rankings = data.rankings || [];
    const criteria = data.criteriaList || [];
    const evaluators = data.evaluators || [];
    const summary = data.summary || {};

    const validSubmissions = rankings.filter(
      (r) => r.status !== 'Withdrawn' && r.status !== 'Disqualified'
    );
    const nonPriceCriteria = criteria.filter((c) => !c.isPriceCriteria);
    const totalCriteriaToScore = validSubmissions.length * nonPriceCriteria.length;

    const committeeMembers = evaluators.map((e, idx) => {
      const scoredCount = (e.scores || []).length;
      const hasCompleted = totalCriteriaToScore > 0 && scoredCount >= totalCriteriaToScore;
      const statusText =
        totalCriteriaToScore === 0
          ? 'Chưa có HSDT cần chấm'
          : hasCompleted
          ? 'Hoàn tất'
          : scoredCount > 0
          ? `Đang chấm (${scoredCount}/${totalCriteriaToScore})`
          : 'Chưa chấm điểm';

      return {
        evaluatorId: e.evaluatorId || e.id,
        fullName: nfc(e.evaluatorName || e.fullName || `Chuyên gia ${idx + 1}`),
        email: e.email || '',
        phone: e.phone || '',
        committeeRole:
          idx === 0
            ? 'Tổ trưởng Tổ chuyên gia'
            : idx % 2 === 1
            ? 'Thành viên - Chuyên gia Đánh giá Kỹ thuật'
            : 'Thành viên - Chuyên gia Thẩm định Tài chính',
        hasCompletedScoring: hasCompleted,
        scoredCriteriaCount: scoredCount,
        totalCriteriaCount: totalCriteriaToScore,
        scoringStatusText: statusText,
      };
    });

    const completedEvaluatorsCount = committeeMembers.filter((m) => m.hasCompletedScoring).length;
    const allEvaluatorsCompleted = committeeMembers.length > 0 && completedEvaluatorsCount === committeeMembers.length;
    const isAwarded = pkg.status === 'Awarded';
    const isFinalized = Boolean(summary.isFinalized || isAwarded || rankings.some((r) => r.status === 'Selected'));
    const canFinalizeExport = isAwarded || (allEvaluatorsCompleted && validSubmissions.length > 0);

    const winningSub = rankings.find(
      (r) => r.status === 'Selected' || (r.rank === 1 && r.totalScore)
    );

    return {
      reportNumber: `BC-ĐGHSDT-${pkg.code || 'SYS'}`,
      generatedAt: new Date().toISOString(),
      location: 'Hà Nội',
      packageId: pkg.id,
      packageCode: pkg.code,
      packageName: nfc(pkg.name),
      budget: pkg.budget || 0,
      packageStatus: pkg.status,
      deadline: pkg.deadline,
      procuringEntityName: nfc(pkg.creator?.fullName || 'Ban Quản lý Dự án & Đấu thầu'),
      procuringEntityTaxCode: '0101234567',
      biddingMethod: 'Đấu thầu rộng rãi qua mạng (E-Bidding)',
      biddingProcedure: 'Một giai đoạn một túi hồ sơ',
      committeeMembers,
      criteria: criteria.map((c) => ({
        id: c.id,
        name: nfc(c.name),
        maxScore: c.maxScore,
        weight: c.weight,
        isPriceCriteria: c.isPriceCriteria,
      })),
      totalSubmissions: rankings.length,
      evaluatedSubmissions: rankings.filter(
        (r) => r.status === 'Evaluated' || r.status === 'Selected'
      ).length,
      rankings: rankings.map((r) => ({
        submissionId: r.submissionId || r.id,
        contractorId: r.contractorId,
        companyName: nfc(r.companyName),
        taxCode: r.taxCode,
        bidPrice: r.bidPrice,
        priceSavings: pkg.budget && r.bidPrice ? pkg.budget - r.bidPrice : null,
        priceSavingsPercent:
          pkg.budget && r.bidPrice
            ? Math.round(((pkg.budget - r.bidPrice) / pkg.budget) * 100 * 100) / 100
            : null,
        totalScore: r.totalScore,
        rank: r.rank,
        status: r.status,
        scores: r.scores || [],
      })),
      isFinalized: Boolean(canFinalizeExport && isFinalized),
      allEvaluatorsCompleted,
      totalEvaluatorsCount: committeeMembers.length,
      completedEvaluatorsCount,
      evaluationStage: isAwarded
        ? 'Đã phê duyệt trao thầu & hoàn tất thẩm định'
        : allEvaluatorsCompleted
        ? 'Đã hoàn thành chấm điểm (Chờ phê duyệt kết quả)'
        : completedEvaluatorsCount > 0
        ? `Đang chấm điểm (Đã có ${completedEvaluatorsCount}/${committeeMembers.length} giám khảo hoàn tất)`
        : 'Chưa bắt đầu chấm điểm (Chờ Tổ chuyên gia đánh giá)',
      canFinalizeExport,
      reportStatusLabel: canFinalizeExport
        ? 'BÁO CÁO ĐÁNH GIÁ E-HSDT VÀ KẾT QUẢ LỰA CHỌN NHÀ THẦU'
        : 'BÁO CÁO TIẾN ĐỘ ĐÁNH GIÁ E-HSDT (DỰ THẢO / NỘI BỘ)',
      winningContractorName: canFinalizeExport ? summary.winningContractorName || winningSub?.companyName : null,
      winningContractorTaxCode: canFinalizeExport ? summary.winningContractorTaxCode || winningSub?.taxCode : null,
      winningBidPrice: canFinalizeExport ? summary.winningBidPrice || winningSub?.bidPrice : null,
      winningTotalScore: canFinalizeExport ? summary.winningScore || winningSub?.totalScore : null,
      decisionReason: canFinalizeExport
        ? (winningSub?.selectionReason || 'Đạt tổng điểm đánh giá cao nhất, đáp ứng đầy đủ yêu cầu kỹ thuật và có hiệu quả tài chính tối ưu.')
        : 'Gói thầu đang trong giai đoạn chấm điểm E-HSDT. Kết quả lựa chọn nhà thầu sẽ được công bố chính thức sau khi toàn bộ Tổ chuyên gia hoàn thành đánh giá và cấp có thẩm quyền phê duyệt.',
      conclusionSummary: canFinalizeExport && winningSub
        ? `Tổ chuyên gia xét thầu thống nhất kiến nghị Bên mời thầu / Người có thẩm quyền phê duyệt kết quả lựa chọn nhà thầu cho đơn vị: ${
            winningSub.companyName
          } với giá trúng thầu ${formatCurrency(winningSub.bidPrice)}.`
        : `Hội đồng chuyên gia đang trong quá trình đánh giá và tổng hợp kết quả thẩm định E-HSDT (Hiện tại có ${completedEvaluatorsCount}/${committeeMembers.length} giám khảo hoàn tất). Chưa có kết quả xếp hạng và quyết định lựa chọn nhà thầu trúng thầu chính thức.`,
    };
  };

  const formatCurrency = (val) => {
    if (val === null || val === undefined || isNaN(val)) return '—';
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val);
  };

  const formatDate = (dateStr) => {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
    return isNaN(d.getTime()) ? dateStr : d.toLocaleDateString('vi-VN');
  };

  const formatDateTime = (dateStr) => {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
    return isNaN(d.getTime())
      ? dateStr
      : `${d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })} ngày ${d.toLocaleDateString('vi-VN')}`;
  };

  // Kích hoạt In ấn / Xuất PDF qua Iframe độc lập (không bị cắt cụt trang, không dính thanh menu web)
  const handlePrint = () => {
    if (!reportRef.current) return;

    // Tạo hidden iframe chuyên dụng cho in ấn
    const iframe = document.createElement('iframe');
    iframe.style.position = 'fixed';
    iframe.style.right = '0';
    iframe.style.bottom = '0';
    iframe.style.width = '0';
    iframe.style.height = '0';
    iframe.style.border = '0';
    iframe.setAttribute('aria-hidden', 'true');
    document.body.appendChild(iframe);

    const iframeDoc = iframe.contentDocument || iframe.contentWindow?.document;
    if (!iframeDoc) {
      window.print();
      return;
    }

    // Lấy toàn bộ stylesheet từ trang web chính (bao gồm Tailwind CSS)
    const styleTags = Array.from(document.querySelectorAll('link[rel="stylesheet"], style'))
      .map((tag) => tag.outerHTML)
      .join('\n');

    const content = reportRef.current.innerHTML;
    const isOfficial = report?.canFinalizeExport;

    iframeDoc.open();
    iframeDoc.write(`
      <!DOCTYPE html>
      <html lang="vi">
      <head>
        <meta charset="utf-8">
        <title>${isOfficial ? 'BaoCao_DanhGia_HSDT_ChinhThuc' : 'BaoCao_TienDo_DanhGia_DuThao'}_${report?.packageCode || 'GoiThau'}</title>
        ${styleTags}
        <style>
          @page {
            size: A4 portrait;
            margin: 20mm 15mm 20mm 20mm;
          }
          * {
            box-sizing: border-box;
            -webkit-print-color-adjust: exact !important;
            print-color-adjust: exact !important;
          }
          html, body {
            margin: 0 !important;
            padding: 0 !important;
            background: #ffffff !important;
            color: #000000 !important;
            font-family: 'Times New Roman', Times, serif !important;
            font-size: 13pt !important;
            line-height: 1.45 !important;
          }
          .print\\:hidden, .no-print {
            display: none !important;
          }
          /* Tháo bỏ toàn bộ ràng buộc kích thước cố định để trang in tự do ngắt trang A4 */
          .report-paper {
            width: 100% !important;
            min-height: auto !important;
            margin: 0 !important;
            padding: 0 !important;
            border: none !important;
            box-shadow: none !important;
            transform: none !important;
            overflow: visible !important;
          }
          table {
            width: 100% !important;
            border-collapse: collapse !important;
            margin: 10pt 0 !important;
            page-break-inside: auto;
          }
          tr {
            page-break-inside: avoid !important;
            page-break-after: auto;
          }
          thead {
            display: table-header-group;
          }
          tfoot {
            display: table-footer-group;
          }
          th, td {
            border: 1pt solid #000000 !important;
            padding: 6pt 8pt !important;
            font-size: 11pt !important;
          }
          th {
            background-color: #f1f5f9 !important;
            font-weight: bold !important;
            text-align: center !important;
          }
          .grid-cols-2 {
            display: flex !important;
            justify-content: space-between !important;
            gap: 20pt !important;
          }
          .grid-cols-2 > div {
            flex: 1 !important;
          }
          .grid-cols-3 {
            display: flex !important;
            justify-content: space-between !important;
            gap: 15pt !important;
          }
          .grid-cols-3 > div {
            flex: 1 !important;
          }
          .break-inside-avoid {
            break-inside: avoid !important;
            page-break-inside: avoid !important;
          }
          .text-center { text-align: center !important; }
          .text-left { text-align: left !important; }
          .text-right { text-align: right !important; }
          .text-justify { text-align: justify !important; }
          .font-bold { font-weight: bold !important; }
          .font-semibold { font-weight: 600 !important; }
          .italic { font-style: italic !important; }
          .uppercase { text-transform: uppercase !important; }
          .indent-6 { text-indent: 1.5em !important; }
          
          /* Watermark khi in qua iframe: cố định ở giữa mỗi trang in */
          .watermark-container {
            position: fixed !important;
            top: 36% !important;
            left: 5% !important;
            right: 5% !important;
            transform: rotate(-30deg) !important;
            opacity: 0.08 !important;
            text-align: center !important;
            pointer-events: none !important;
            z-index: 0 !important;
          }
        </style>
      </head>
      <body>
        <div class="report-paper">
          ${content}
        </div>
      </body>
      </html>
    `);
    iframeDoc.close();

    // Chờ iframe phân tích tài liệu và font rồi mở hộp thoại in
    setTimeout(() => {
      try {
        iframe.contentWindow?.focus();
        iframe.contentWindow?.print();
      } catch (err) {
        console.error('Lỗi khi kích hoạt in qua iframe:', err);
        window.print();
      } finally {
        setTimeout(() => {
          if (document.body.contains(iframe)) {
            document.body.removeChild(iframe);
          }
        }, 1200);
      }
    }, 400);
  };

  // Tải tài liệu về dạng file Microsoft Word (.doc) với định dạng chuẩn Office HTML / WordML
  const handleDownloadDoc = () => {
    if (!reportRef.current) return;
    const content = reportRef.current.innerHTML;
    const isOfficial = report?.canFinalizeExport;
    const fileName = `${
      isOfficial ? 'BaoCao_DanhGia_HSDT_ChinhThuc' : 'BaoCao_TienDo_DanhGia_DuThao'
    }_${report?.packageCode || 'GoiThau'}.doc`;

    const wordHtml = `
      <html xmlns:o='urn:schemas-microsoft-com:office:office'
            xmlns:w='urn:schemas-microsoft-com:office:word'
            xmlns='http://www.w3.org/TR/REC-html40'>
      <head>
        <meta charset='utf-8'>
        <title>${isOfficial ? 'Báo cáo Đánh giá HSDT' : 'Báo cáo Tiến độ Đánh giá HSDT'}</title>
        <!--[if gte mso 9]>
        <xml>
          <w:WordDocument>
            <w:View>Print</w:View>
            <w:Zoom>100</w:Zoom>
            <w:DoNotOptimizeForBrowser/>
          </w:WordDocument>
        </xml>
        <![endif]-->
        <style>
          @page Section1 {
            size: 595.3pt 841.9pt; /* Chuẩn A4 */
            margin: 70.85pt 56.7pt 70.85pt 85.05pt; /* Trên 2.5cm, Phải 2.0cm, Dưới 2.5cm, Trái 3.0cm */
            mso-header-margin: 35.4pt;
            mso-footer-margin: 35.4pt;
            mso-paper-source: 0;
          }
          div.Section1 { page: Section1; }
          body {
            font-family: 'Times New Roman', Times, serif;
            font-size: 13pt;
            line-height: 1.4;
            color: #000000;
          }
          table {
            width: 100%;
            border-collapse: collapse;
            margin: 10pt 0;
            mso-table-lspace: 0pt;
            mso-table-rspace: 0pt;
          }
          th, td {
            border: 1pt solid #000000;
            padding: 5pt 7pt;
            font-size: 11pt;
            vertical-align: middle;
          }
          th {
            background-color: #f2f2f2;
            font-weight: bold;
            text-align: center;
          }
          .text-center { text-align: center; }
          .text-left { text-align: left; }
          .text-right { text-align: right; }
          .text-justify { text-align: justify; }
          .font-bold { font-weight: bold; }
          .font-semibold { font-weight: 600; }
          .italic { font-style: italic; }
          .uppercase { text-transform: uppercase; }
          .indent-6 { text-indent: 24pt; }
          .grid-cols-2 {
            display: flex;
            justify-content: space-between;
          }
          .grid-cols-2 > div {
            width: 48%;
          }
          .grid-cols-3 {
            display: flex;
            justify-content: space-between;
          }
          .grid-cols-3 > div {
            width: 31%;
          }
        </style>
      </head>
      <body>
        <div class="Section1">
          ${content}
        </div>
      </body>
      </html>
    `;

    // Khởi tạo thẻ a để tải file với UTF-8 BOM chống lỗi font tiếng Việt trong MS Word
    const blob = new Blob(['\ufeff' + wordHtml], { type: 'application/msword;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };

  if (!isOpen) return null;

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="report-modal-title"
      className="fixed inset-0 z-50 flex items-center justify-center p-2 sm:p-4 bg-slate-900/60 backdrop-blur-xs animate-in fade-in duration-200"
    >
      {/* Container chính của Modal */}
      <div className="bg-slate-100 rounded-2xl w-full max-w-5xl h-[92vh] flex flex-col shadow-2xl border border-slate-300 overflow-hidden">
        {/* THANH CÔNG CỤ (TOOLBAR) - Ẩn khi In ấn */}
        <div className="print:hidden bg-white px-5 py-3 border-b border-slate-200 flex flex-wrap items-center justify-between gap-3 shrink-0">
          <div className="flex items-center gap-3">
            <div className={`w-9 h-9 rounded-xl flex items-center justify-center font-bold ${
              report?.canFinalizeExport ? 'bg-emerald-50 text-emerald-700' : 'bg-amber-50 text-amber-700'
            }`}>
              <FileText className="w-5 h-5" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h2 id="report-modal-title" className="text-sm font-bold text-slate-800">
                  {report?.canFinalizeExport
                    ? 'Biên bản & Báo cáo Đánh giá E-HSDT (Chính Thức)'
                    : 'Báo cáo Tiến độ Đánh giá E-HSDT (Bản Dự Thảo)'}
                </h2>
                <span
                  className={`px-2 py-0.5 rounded-full text-[10px] font-bold uppercase tracking-wider ${
                    report?.canFinalizeExport
                      ? 'bg-emerald-100 text-emerald-800 border border-emerald-300'
                      : 'bg-amber-100 text-amber-800 border border-amber-300'
                  }`}
                >
                  {report?.canFinalizeExport ? 'Chính thức' : 'Bản dự thảo'}
                </span>
              </div>
              <p className="text-[11px] text-slate-500">
                Gói thầu: <span className="font-semibold text-slate-700">{report?.packageCode || packageId}</span> • Căn cứ Luật Đấu thầu 22/2023/QH15
              </p>
            </div>
          </div>

          <div className="flex items-center gap-2">
            {/* Bộ điều khiển Zoom */}
            <div className="hidden sm:flex items-center gap-1 bg-slate-100 p-1 rounded-xl border border-slate-200 text-xs text-slate-600">
              <button
                type="button"
                onClick={() => setZoomLevel((prev) => Math.max(prev - 10, 70))}
                className="p-1 hover:bg-white rounded-lg transition-colors cursor-pointer"
                title="Thu nhỏ"
              >
                <ZoomOut className="w-3.5 h-3.5 text-slate-600" />
              </button>
              <span className="px-1.5 font-medium min-w-[40px] text-center">{zoomLevel}%</span>
              <button
                type="button"
                onClick={() => setZoomLevel((prev) => Math.min(prev + 10, 140))}
                className="p-1 hover:bg-white rounded-lg transition-colors cursor-pointer"
                title="Phóng to"
              >
                <ZoomIn className="w-3.5 h-3.5 text-slate-600" />
              </button>
              <button
                type="button"
                onClick={() => setZoomLevel(100)}
                className="p-1 hover:bg-white rounded-lg transition-colors text-[10px] cursor-pointer"
                title="Mặc định 100%"
              >
                100%
              </button>
            </div>

            {/* Nút Tải Word/Doc */}
            <button
              type="button"
              onClick={handleDownloadDoc}
              disabled={loading || !report}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-slate-100 hover:bg-slate-200 text-slate-700 border border-slate-300 rounded-xl text-xs font-semibold transition-colors cursor-pointer disabled:opacity-50"
              title="Tải văn bản về dạng .doc (Microsoft Word)"
            >
              <Download className="w-3.5 h-3.5 text-slate-600" />
              <span className="hidden sm:inline">Tải về (.doc)</span>
            </button>

            {/* Nút In ấn / Xuất PDF */}
            <button
              type="button"
              onClick={handlePrint}
              disabled={loading || !report}
              className={`inline-flex items-center gap-1.5 px-4 py-1.5 rounded-xl text-xs font-bold transition-all shadow-xs hover:shadow-sm cursor-pointer disabled:opacity-50 ${
                report?.canFinalizeExport
                  ? 'bg-blue-600 hover:bg-blue-700 text-white'
                  : 'bg-amber-600 hover:bg-amber-700 text-white'
              }`}
              title={report?.canFinalizeExport ? 'In / Xuất PDF Báo cáo chính thức' : 'In / Xuất bản dự thảo tiến độ'}
            >
              <Printer className="w-3.5 h-3.5" />
              <span>{report?.canFinalizeExport ? 'In / Xuất PDF' : 'In bản Dự thảo (PDF)'}</span>
            </button>

            {/* Nút Đóng */}
            <button
              type="button"
              onClick={onClose}
              className="p-1.5 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-xl transition-colors cursor-pointer"
              title="Đóng (Escape)"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* NỘI DUNG VĂN BẢN (KHUNG XEM TRƯỚC KHỔ GIẤY A4) */}
        <div className="flex-1 overflow-y-auto p-4 sm:p-8 flex justify-center bg-slate-200/70 print:p-0 print:bg-white print:overflow-visible">
          {loading ? (
            <div className="py-24 flex items-center justify-center">
              <LoadingSpinner size="lg" message="Đang kết xuất Báo cáo đánh giá chuẩn mẫu hành chính..." />
            </div>
          ) : error ? (
            <div className="bg-white rounded-2xl border border-rose-200 p-8 text-center max-w-md m-auto space-y-3">
              <AlertCircle className="w-10 h-10 text-rose-500 mx-auto" />
              <h3 className="text-sm font-bold text-slate-800">Không thể kết xuất báo cáo</h3>
              <p className="text-xs text-slate-500">{error}</p>
              <button
                type="button"
                onClick={fetchReportData}
                className="px-4 py-1.5 bg-blue-600 text-white text-xs font-bold rounded-xl"
              >
                Thử lại
              </button>
            </div>
          ) : (
            /* TRANG VĂN BẢN KHỔ A4 (Standard Vietnamese Administrative Form) */
            <div
              ref={reportRef}
              style={{
                transform: `scale(${zoomLevel / 100})`,
                transformOrigin: 'top center',
                fontFamily: "'Times New Roman', Times, serif",
                letterSpacing: 'normal',
              }}
              className="report-paper relative bg-white w-[210mm] min-h-[297mm] p-[25mm_20mm_25mm_30mm] shadow-xl border border-slate-300 text-slate-900 leading-relaxed transition-transform duration-150 print:m-0 print:p-0 print:border-none print:shadow-none print:w-full print:scale-100 text-[13pt] overflow-visible print:overflow-visible"
            >
              {/* WATERMARK DỰ THẢO - HIỂN THỊ KHI CHƯA HOÀN TẤT ĐÁNH GIÁ (CẢ TRÊN WEB VÀ KHI IN) */}
              {!report?.canFinalizeExport && (
                <div
                  aria-hidden="true"
                  className="watermark-container pointer-events-none select-none absolute inset-0 flex items-center justify-center overflow-hidden z-0"
                >
                  <div className="rotate-[-32deg] text-center opacity-[0.06] print:opacity-[0.10] space-y-4">
                    <p className="text-5xl sm:text-6xl font-black uppercase tracking-widest text-slate-900 border-y-4 border-slate-900 py-3">
                      DỰ THẢO • ĐANG ĐÁNH GIÁ
                    </p>
                    <p className="text-xl sm:text-2xl font-bold uppercase tracking-wider text-slate-800">
                      CHƯA CÓ HIỆU LỰC PHÁP LÝ CHÍNH THỨC
                    </p>
                  </div>
                </div>
              )}

              {/* BANNER THÔNG BÁO TIẾN ĐỘ THỰC TẾ (KHI LÀ DỰ THẢO) */}
              {!report?.canFinalizeExport && (
                <div className="relative z-10 mb-4 p-3 bg-amber-50/90 border-2 border-dashed border-amber-300 rounded-xl text-amber-950 text-[10.5pt] space-y-1 print:border-slate-400 print:bg-slate-50">
                  <div className="flex items-center gap-2 font-bold text-amber-900">
                    <AlertCircle className="w-4 h-4 text-amber-600 print:hidden shrink-0" />
                    <span>THÔNG BÁO TIẾN ĐỘ ĐÁNH GIÁ (BẢN DỰ THẢO NỘI BỘ):</span>
                  </div>
                  <p className="text-justify leading-relaxed">
                    Gói thầu đang trong giai đoạn chấm điểm E-HSDT. Hiện tại đã có{' '}
                    <strong>{report?.completedEvaluatorsCount ?? 0}/{report?.totalEvaluatorsCount ?? report?.committeeMembers?.length ?? 0}</strong>{' '}
                    thành viên Tổ chuyên gia hoàn thành đánh giá. Báo cáo này phản ánh tiến độ thực tế phục vụ theo dõi nội bộ,{' '}
                    <strong className="underline">chưa phải là Báo cáo Đánh giá chính thức</strong> và{' '}
                    <strong>chưa có giá trị pháp lý</strong> để phê duyệt kết quả lựa chọn nhà thầu.
                  </p>
                </div>
              )}

              {/* PHẦN ĐẦU VĂN BẢN: QUỐC HIỆU, TIÊU NGỮ & ĐƠN VỊ BAN HÀNH */}
              <div className="relative z-10 grid grid-cols-2 gap-4 pb-4">
                {/* Cột trái: Đơn vị ban hành */}
                <div className="text-center space-y-0.5">
                  <p className="text-[12pt] font-bold uppercase">
                    {report?.procuringEntityName || 'BÊN MỜI THẦU'}
                  </p>
                  <p className="text-[11pt] font-bold uppercase">
                    TỔ CHUYÊN GIA ĐÁNH GIÁ E-HSDT
                  </p>
                  <div className="w-24 h-[1px] bg-black mx-auto my-1"></div>
                  <p className="text-[11pt] italic pt-1">
                    Số: {report?.reportNumber || '... /BC-TĐHSDT'}
                  </p>
                </div>

                {/* Cột phải: Quốc hiệu & Tiêu ngữ */}
                <div className="text-center space-y-0.5">
                  <p className="text-[12pt] font-bold uppercase">
                    CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM
                  </p>
                  <p className="text-[12pt] font-bold">
                    Độc lập - Tự do - Hạnh phúc
                  </p>
                  <div className="w-36 h-[1px] bg-black mx-auto my-1"></div>
                  <p className="text-[11pt] italic pt-1">
                    {report?.location || 'Hà Nội'}, ngày {new Date().getDate()} tháng {new Date().getMonth() + 1} năm {new Date().getFullYear()}
                  </p>
                </div>
              </div>

              {/* TIÊU ĐỀ BÁO CÁO */}
              <div className="relative z-10 text-center py-5 space-y-1.5">
                <h1 className="text-[15pt] sm:text-[16pt] font-bold uppercase">
                  {report?.canFinalizeExport
                    ? 'BÁO CÁO ĐÁNH GIÁ HỒ SƠ DỰ THẦU'
                    : 'BÁO CÁO TIẾN ĐỘ ĐÁNH GIÁ HỒ SƠ DỰ THẦU (DỰ THẢO)'}
                </h1>
                <p className="text-[12pt] sm:text-[13pt] font-bold">
                  Gói thầu: {report?.packageName}
                </p>
                <p className="text-[11pt] italic text-slate-700">
                  (Mã định danh gói thầu: {report?.packageCode} • Đấu thầu qua mạng)
                </p>
                {!report?.canFinalizeExport && (
                  <div className="pt-1">
                    <span className="inline-block px-3 py-0.5 bg-amber-100 text-amber-900 border border-amber-300 rounded-full text-[10pt] font-bold uppercase">
                      Tiến độ: {report?.evaluationStage || 'Đang trong quá trình đánh giá'}
                    </span>
                  </div>
                )}
              </div>

              {/* PHẦN I: THÔNG TIN CHUNG VÀ CĂN CỨ PHÁP LÝ */}
              <div className="space-y-2 pt-2">
                <h2 className="text-[13pt] font-bold uppercase">
                  I. THÔNG TIN CHUNG VÀ CĂN CỨ PHÁP LÝ
                </h2>
                <div className="text-[12pt] space-y-1 pl-2">
                  <p className="text-justify indent-6">
                    – Căn cứ Luật Đấu thầu số 22/2023/QH15 ngày 23 tháng 6 năm 2023 của Quốc hội nước Cộng hòa xã hội chủ nghĩa Việt Nam;
                  </p>
                  <p className="text-justify indent-6">
                    – Căn cứ Nghị định số 24/2024/NĐ-CP ngày 27 tháng 02 năm 2024 của Chính phủ quy định chi tiết một số điều và biện pháp thi hành Luật Đấu thầu về lựa chọn nhà thầu;
                  </p>
                  <p className="text-justify indent-6">
                    – Căn cứ Hồ sơ mời thầu (E-HSMT) gói thầu: <strong>{report?.packageName}</strong> đã được phê duyệt và phát hành trên Hệ thống mạng đấu thầu quốc gia;
                  </p>
                  <p className="text-justify indent-6">
                    – Căn cứ Quyết định thành lập Tổ chuyên gia xét thầu của Bên mời thầu;
                  </p>
                </div>

                {/* Bảng tóm tắt thông số gói thầu */}
                <table className="w-full border-collapse border border-black my-3 text-[11pt]">
                  <tbody>
                    <tr>
                      <td className="border border-black p-2 font-bold w-1/3 bg-slate-50">Tên gói thầu:</td>
                      <td className="border border-black p-2 font-semibold">{report?.packageName}</td>
                    </tr>
                    <tr>
                      <td className="border border-black p-2 font-bold bg-slate-50">Mã gói thầu:</td>
                      <td className="border border-black p-2">{report?.packageCode}</td>
                    </tr>
                    <tr>
                      <td className="border border-black p-2 font-bold bg-slate-50">Giá dự toán / Ngân sách gói thầu:</td>
                      <td className="border border-black p-2 font-bold text-slate-900">
                        {formatCurrency(report?.budget)}
                      </td>
                    </tr>
                    <tr>
                      <td className="border border-black p-2 font-bold bg-slate-50">Hình thức & Phương thức:</td>
                      <td className="border border-black p-2">
                        {report?.biddingMethod} • {report?.biddingProcedure}
                      </td>
                    </tr>
                    <tr>
                      <td className="border border-black p-2 font-bold bg-slate-50">Thời điểm đóng thầu / Mở thầu:</td>
                      <td className="border border-black p-2">{formatDateTime(report?.deadline)}</td>
                    </tr>
                  </tbody>
                </table>
              </div>

              {/* PHẦN II: THÀNH PHẦN TỔ CHUYÊN GIA ĐÁNH GIÁ */}
              <div className="relative z-10 space-y-2 pt-4">
                <h2 className="text-[13pt] font-bold uppercase">
                  II. THÀNH PHẦN TỔ CHUYÊN GIA ĐÁNH GIÁ
                </h2>
                <p className="text-justify indent-6 text-[12pt]">
                  Tổ chuyên gia xét thầu thực hiện công tác đánh giá E-HSDT bảo đảm nguyên tắc độc lập, khách quan, trung thực và tuân thủ các quy định hiện hành:
                </p>

                <table className="w-full border-collapse border border-black my-2 text-[11pt]">
                  <thead>
                    <tr className="bg-slate-100 font-bold text-center">
                      <th className="border border-black p-2 w-12">STT</th>
                      <th className="border border-black p-2">Họ và tên chuyên gia</th>
                      <th className="border border-black p-2">Vai trò trong Tổ chuyên gia</th>
                      <th className="border border-black p-2 w-28">Nhiệm vụ</th>
                      <th className="border border-black p-2 w-32">Tình trạng</th>
                    </tr>
                  </thead>
                  <tbody>
                    {report?.committeeMembers?.length > 0 ? (
                      report.committeeMembers.map((member, idx) => (
                        <tr key={member.evaluatorId || idx} className="text-center">
                          <td className="border border-black p-1.5">{idx + 1}</td>
                          <td className="border border-black p-1.5 font-bold text-left">{member.fullName}</td>
                          <td className="border border-black p-1.5 text-left">{member.committeeRole}</td>
                          <td className="border border-black p-1.5">Chấm điểm & Thẩm định</td>
                          <td className="border border-black p-1.5">
                            {member.hasCompletedScoring ? (
                              <span className="font-bold text-emerald-800">Hoàn tất</span>
                            ) : member.scoredCriteriaCount > 0 ? (
                              <span className="font-semibold text-amber-800">
                                {member.scoringStatusText || `Đang chấm (${member.scoredCriteriaCount}/${member.totalCriteriaCount})`}
                              </span>
                            ) : (
                              <span className="italic text-slate-500">
                                {member.scoringStatusText || 'Chưa chấm điểm'}
                              </span>
                            )}
                          </td>
                        </tr>
                      ))
                    ) : (
                      <tr>
                        <td colSpan="5" className="border border-black p-2 text-center italic text-slate-500">
                          Chưa phân công danh sách thành viên Tổ chuyên gia
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>

              {/* PHẦN III: TIÊU CHUẨN ĐÁNH GIÁ E-HSDT */}
              <div className="relative z-10 space-y-2 pt-4">
                <h2 className="text-[13pt] font-bold uppercase">
                  III. TIÊU CHUẨN ĐÁNH GIÁ HỒ SƠ DỰ THẦU
                </h2>
                <p className="text-justify indent-6 text-[12pt]">
                  Việc đánh giá hồ sơ dự thầu được thực hiện căn cứ theo thang điểm 100 với các tiêu chí định lượng và định tính cụ thể như sau:
                </p>

                <table className="w-full border-collapse border border-black my-2 text-[11pt]">
                  <thead>
                    <tr className="bg-slate-100 font-bold text-center">
                      <th className="border border-black p-2 w-12">STT</th>
                      <th className="border border-black p-2">Nội dung tiêu chuẩn đánh giá</th>
                      <th className="border border-black p-2 w-28">Phân loại</th>
                      <th className="border border-black p-2 w-24">Trọng số</th>
                      <th className="border border-black p-2 w-24">Điểm tối đa</th>
                    </tr>
                  </thead>
                  <tbody>
                    {report?.criteria?.length > 0 ? (
                      report.criteria.map((crit, idx) => (
                        <tr key={crit.id || idx}>
                          <td className="border border-black p-1.5 text-center">{idx + 1}</td>
                          <td className="border border-black p-1.5 font-semibold">{crit.name}</td>
                          <td className="border border-black p-1.5 text-center">
                            {crit.isPriceCriteria ? 'Tài chính / Giá' : 'Giải pháp Kỹ thuật'}
                          </td>
                          <td className="border border-black p-1.5 text-center">{crit.weight}%</td>
                          <td className="border border-black p-1.5 text-center font-bold">{crit.maxScore} đ</td>
                        </tr>
                      ))
                    ) : (
                      <tr>
                        <td colSpan="5" className="border border-black p-2 text-center italic text-slate-500">
                          Chưa cấu hình tiêu chuẩn đánh giá
                        </td>
                      </tr>
                    )}
                    <tr className="bg-slate-50 font-bold text-center">
                      <td colSpan="3" className="border border-black p-2 text-right uppercase">
                        Tổng cộng thang điểm đánh giá:
                      </td>
                      <td className="border border-black p-2">
                        {report?.criteria?.reduce((sum, c) => sum + (c.weight || 0), 0)}%
                      </td>
                      <td className="border border-black p-2">100.00 đ</td>
                    </tr>
                  </tbody>
                </table>
              </div>

              {/* PHẦN IV: BẢNG TỔNG HỢP KẾT QUẢ ĐÁNH GIÁ & XẾP HẠNG NHÀ THẦU */}
              <div className="relative z-10 space-y-2 pt-4">
                <h2 className="text-[13pt] font-bold uppercase">
                  IV. BẢNG TỔNG HỢP KẾT QUẢ ĐÁNH GIÁ & XẾP HẠNG NHÀ THẦU
                </h2>
                <p className="text-justify indent-6 text-[12pt]">
                  {report?.canFinalizeExport
                    ? 'Sau khi tổng hợp điểm số từ toàn bộ các thành viên Tổ chuyên gia và áp dụng công thức tính điểm tài chính theo quy định, kết quả thẩm định và xếp hạng các nhà thầu được ghi nhận như sau:'
                    : 'Tổng hợp tiến độ điểm số đánh giá hiện tại của các thành viên Tổ chuyên gia (kết quả tạm tính phục vụ theo dõi nội bộ, chưa chốt xếp hạng chính thức):'}
                </p>

                <table className="w-full border-collapse border border-black my-2 text-[11pt]">
                  <thead>
                    <tr className="bg-slate-100 font-bold text-center">
                      <th className="border border-black p-2 w-20">Xếp hạng</th>
                      <th className="border border-black p-2">Tên nhà thầu tham dự</th>
                      <th className="border border-black p-2 w-28">Mã số thuế</th>
                      <th className="border border-black p-2">Giá dự thầu (VND)</th>
                      <th className="border border-black p-2 w-24">Tiết kiệm NS</th>
                      <th className="border border-black p-2 w-24">Tổng điểm</th>
                      <th className="border border-black p-2 w-28">Kết luận</th>
                    </tr>
                  </thead>
                  <tbody>
                    {report?.rankings?.length > 0 ? (
                      report.rankings.map((r, idx) => {
                        const isWinner = Boolean(report.canFinalizeExport && (r.rank === 1 || r.status === 'Selected') && r.totalScore > 0);
                        return (
                          <tr
                            key={r.submissionId || idx}
                            className={`text-center ${isWinner ? 'bg-emerald-50/60 font-medium' : ''}`}
                          >
                            <td className="border border-black p-2 font-bold">
                              {report.canFinalizeExport && r.rank
                                ? `Hạng #${r.rank}`
                                : r.rank
                                ? `Hạng #${r.rank} *`
                                : '—'}
                            </td>
                            <td className="border border-black p-2 text-left font-bold">
                              {r.companyName}
                              {isWinner && (
                                <span className="block text-[10pt] font-semibold text-emerald-800 italic">
                                  (Đề xuất trúng thầu)
                                </span>
                              )}
                            </td>
                            <td className="border border-black p-2 font-mono text-[10pt]">
                              {r.taxCode || '—'}
                            </td>
                            <td className="border border-black p-2 text-right font-bold">
                              {formatCurrency(r.bidPrice)}
                            </td>
                            <td className="border border-black p-2 text-center text-emerald-900">
                              {r.priceSavingsPercent !== null ? `${r.priceSavingsPercent}%` : '—'}
                            </td>
                            <td className="border border-black p-2 font-bold text-blue-900">
                              {r.totalScore !== null
                                ? `${Number(r.totalScore).toFixed(2)} đ${!report.canFinalizeExport ? ' *' : ''}`
                                : '—'}
                            </td>
                            <td className="border border-black p-2 font-bold">
                              {isWinner ? (
                                <span className="text-emerald-800">Trúng thầu</span>
                              ) : r.status === 'Disqualified' ? (
                                <span className="text-rose-700">Không đạt</span>
                              ) : !report.canFinalizeExport ? (
                                <span className="text-amber-800 italic">Đang đánh giá</span>
                              ) : (
                                <span className="text-slate-600">Đạt yêu cầu</span>
                              )}
                            </td>
                          </tr>
                        );
                      })
                    ) : (
                      <tr>
                        <td colSpan="7" className="border border-black p-2 text-center italic text-slate-500">
                          Chưa có hồ sơ dự thầu được đánh giá
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
                {!report?.canFinalizeExport && (
                  <p className="text-[10pt] italic text-slate-600 pt-0.5">
                    (*) Điểm số và thứ hạng mang tính chất tạm tính theo các tiêu chí đã chấm đến thời điểm xuất báo cáo.
                  </p>
                )}
              </div>

              {/* PHẦN V: KẾT LUẬN VÀ KIẾN NGHỊ CỦA TỔ CHUYÊN GIA */}
              <div className="relative z-10 space-y-2 pt-4">
                <h2 className="text-[13pt] font-bold uppercase">
                  V. KẾT LUẬN VÀ KIẾN NGHỊ CỦA TỔ CHUYÊN GIA
                </h2>
                <div className="text-[12pt] space-y-2 pl-2 text-justify">
                  <p className="indent-6">
                    {report?.conclusionSummary ||
                      'Căn cứ kết quả đánh giá hồ sơ dự thầu nêu trên, Tổ chuyên gia xét thầu đã tiến hành rà soát kỹ lưỡng và thống nhất kết luận:'}
                  </p>
                  
                  {report?.canFinalizeExport && report?.winningContractorName && (
                    <div className="border-l-4 border-black pl-4 py-1 space-y-1 bg-slate-50 my-2 text-[12pt]">
                      <p>
                        1. <strong>Đơn vị được đề nghị lựa chọn trúng thầu:</strong>{' '}
                        <span className="uppercase font-bold">{report.winningContractorName}</span>
                      </p>
                      <p>
                        2. <strong>Mã số doanh nghiệp / Mã số thuế:</strong>{' '}
                        {report.winningContractorTaxCode || 'Theo hồ sơ pháp nhân'}
                      </p>
                      <p>
                        3. <strong>Giá đề nghị trúng thầu:</strong>{' '}
                        <span className="font-bold text-slate-900">{formatCurrency(report.winningBidPrice)}</span>{' '}
                        (Đã bao gồm các loại thuế, phí và lệ phí theo quy định)
                      </p>
                      <p>
                        4. <strong>Tổng điểm đánh giá tổng hợp:</strong>{' '}
                        <span className="font-bold">{report.winningTotalScore ? `${Number(report.winningTotalScore).toFixed(2)} / 100 điểm` : '—'}</span>
                      </p>
                      <p>
                        5. <strong>Lý do lựa chọn:</strong>{' '}
                        {report.decisionReason}
                      </p>
                    </div>
                  )}

                  {report?.canFinalizeExport ? (
                    <p className="indent-6">
                      Kính đề nghị Bên mời thầu xem xét, thẩm định và trình Người có thẩm quyền phê duyệt kết quả lựa chọn nhà thầu gói thầu <strong>{report?.packageName}</strong> theo đúng trình tự pháp luật quy định.
                    </p>
                  ) : (
                    <p className="indent-6 italic text-slate-700">
                      Tổ chuyên gia đang tiếp tục hoàn tất các nội dung thẩm định độc lập còn lại. Sau khi hoàn thành toàn bộ công tác đánh giá, Tổ chuyên gia sẽ hoàn thiện Báo cáo Đánh giá chính thức để trình cấp có thẩm quyền phê duyệt theo quy định.
                    </p>
                  )}
                </div>
              </div>

              {/* PHẦN VI: CHỮ KÝ XÁC NHẬN CỦA CÁC BÊN */}
              <div className="relative z-10 grid grid-cols-2 gap-8 pt-8 text-center text-[12pt] break-inside-avoid">
                {/* Chữ ký Đại diện Bên mời thầu */}
                <div className="space-y-20">
                  <div>
                    <p className="font-bold uppercase">ĐẠI DIỆN BÊN MỜI THẦU</p>
                    <p className="italic text-[10pt] text-slate-600">
                      {report?.canFinalizeExport ? '(Ký duyệt, ghi rõ họ tên và đóng dấu)' : '(Ký nhận tiến độ theo dõi nội bộ)'}
                    </p>
                  </div>
                  <div className="pt-8">
                    <p className="font-bold uppercase">{report?.procuringEntityName || 'Chủ đầu tư / Bên mời thầu'}</p>
                  </div>
                </div>

                {/* Chữ ký Tổ trưởng & Thành viên Tổ chuyên gia */}
                <div className="space-y-20">
                  <div>
                    <p className="font-bold uppercase">TỔ TRƯỞNG TỔ CHUYÊN GIA</p>
                    <p className="italic text-[10pt] text-slate-600">
                      {report?.canFinalizeExport ? '(Ký, ghi rõ họ tên)' : '(Ký xác nhận tiến độ đánh giá)'}
                    </p>
                  </div>
                  <div className="pt-8">
                    <p className="font-bold uppercase">
                      {report?.committeeMembers?.[0]?.fullName || 'Chuyên gia xét thầu'}
                    </p>
                  </div>
                </div>
              </div>

              {/* Danh sách chữ ký các thành viên chuyên gia khác */}
              {report?.committeeMembers?.length > 1 && (
                <div className="relative z-10 pt-10 border-t border-dashed border-slate-300 mt-8 text-[11pt]">
                  <p className="font-bold italic pb-4 text-center">
                    {report?.canFinalizeExport
                      ? 'CÁC THÀNH VIÊN TỔ CHUYÊN GIA ĐỒNG XÁC NHẬN KẾT QUẢ ĐÁNH GIÁ:'
                      : 'TIẾN ĐỘ THẨM ĐỊNH CỦA CÁC THÀNH VIÊN TỔ CHUYÊN GIA:'}
                  </p>
                  <div className="grid grid-cols-3 gap-6 text-center">
                    {report.committeeMembers.slice(1).map((m, idx) => (
                      <div key={m.evaluatorId || idx} className="space-y-12">
                        <div>
                          <p className="font-semibold text-[11pt]">{m.committeeRole}</p>
                          <p className="text-[10pt] italic text-slate-600">
                            {m.hasCompletedScoring ? '(Đã hoàn tất chấm điểm)' : '(Đang chấm điểm)'}
                          </p>
                        </div>
                        <p className="font-bold">{m.fullName}</p>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
