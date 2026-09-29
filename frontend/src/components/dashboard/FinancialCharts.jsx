import React from 'react';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  BarElement,
  Title,
  Tooltip,
  Legend,
  ArcElement,
} from 'chart.js';
import { Bar, Doughnut } from 'react-chartjs-2';

ChartJS.register(
  CategoryScale,
  LinearScale,
  BarElement,
  Title,
  Tooltip,
  Legend,
  ArcElement
);

export const FinancialCharts = ({ stats }) => {
  if (!stats) return null;

  // 1. Bar Chart: So sánh Dòng tiền Tài chính (Đơn vị: Tỷ VNĐ)
  const barData = {
    labels: ['Ngân sách dự toán', 'Giá trị hợp đồng', 'Đã giải ngân'],
    datasets: [
      {
        label: 'Giá trị (VNĐ)',
        data: [
          stats.totalEstimatedBudget || 0,
          stats.totalContractValue || 0,
          stats.totalDisbursedAmount || 0,
        ],
        backgroundColor: [
          'rgba(2, 132, 199, 0.85)',   // Sky 600
          'rgba(99, 102, 241, 0.85)',  // Indigo 500
          'rgba(16, 185, 129, 0.85)',  // Emerald 500
        ],
        borderRadius: 8,
      },
    ],
  };

  const barOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        display: false,
      },
      tooltip: {
        callbacks: {
          label: (context) => {
            const val = context.raw || 0;
            return ` ${new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val)}`;
          },
        },
      },
    },
    scales: {
      y: {
        beginAtZero: true,
        ticks: {
          callback: (value) => {
            if (value >= 1e9) return `${(value / 1e9).toFixed(1)} tỷ`;
            if (value >= 1e6) return `${(value / 1e6).toFixed(0)} tr`;
            return value;
          },
        },
        grid: {
          color: '#f1f5f9',
        },
      },
      x: {
        grid: {
          display: false,
        },
      },
    },
  };

  // 2. Doughnut Chart: Phân bổ trạng thái Gói thầu
  const doughnutData = {
    labels: ['Đang mở thầu', 'Đang chấm điểm', 'Đã ký hợp đồng', 'Đã đóng'],
    datasets: [
      {
        data: [
          stats.openPackages || 0,
          stats.evaluatingPackages || 0,
          stats.contractedPackages || 0,
          stats.closedPackages || 0,
        ],
        backgroundColor: [
          '#10b981', // Emerald 500
          '#f59e0b', // Amber 500
          '#0284c7', // Sky 600
          '#94a3b8', // Slate 400
        ],
        borderWidth: 0,
      },
    ],
  };

  const doughnutOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'bottom',
        labels: {
          boxWidth: 12,
          font: {
            size: 11,
          },
        },
      },
    },
    cutout: '70%',
  };

  return (
    <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
      {/* Cột 1 & 2: Biểu đồ Dòng tiền */}
      <div className="lg:col-span-2 bg-white p-5 rounded-2xl border border-slate-200 shadow-xs space-y-4">
        <div className="flex items-center justify-between">
          <h4 className="text-sm font-bold text-slate-800">
            Dòng tiền & Hiệu quả đấu thầu
          </h4>
          <span className="text-xs text-slate-400">Đơn vị: VNĐ</span>
        </div>
        <div className="h-64">
          <Bar data={barData} options={barOptions} />
        </div>
      </div>

      {/* Cột 3: Biểu đồ Tròn Phân bổ trạng thái */}
      <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-xs space-y-4">
        <h4 className="text-sm font-bold text-slate-800">
          Phân bổ gói thầu
        </h4>
        <div className="h-64 flex items-center justify-center">
          <Doughnut data={doughnutData} options={doughnutOptions} />
        </div>
      </div>
    </div>
  );
};

export default FinancialCharts;
